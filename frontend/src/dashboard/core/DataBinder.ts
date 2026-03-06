/**
 * DataBinder - 数据绑定系统
 * 负责管理数据源和组件之间的绑定关系，支持自动刷新
 * 使用 requestAnimationFrame 替代 setInterval，实现与浏览器刷新率同步
 * 支持局部更新，避免完整重绘图表
 * 集成数据缓存，支持 TTL 过期、请求去重、LRU 淘汰
 * @module dashboard/core/DataBinder
 */

import { ref, type Ref, readonly } from 'vue'
import { RAFTimerController } from '../../display/composables/useAnimationFrame'
import { globalDataCache, type DataCacheOptions } from './DataCache'
import type {
  DataSourceConfig,
  DataBindingConfig,
  BindingState
} from '../types/dashboard'

/**
 * 数据变更回调类型
 */
export type DataChangeCallback = (
  newData: unknown,
  oldData: unknown,
  changes?: DataDiffResult
) => void

/**
 * 数据差异结果
 */
export interface DataDiffResult {
  /** 是否有变化 */
  hasChanges: boolean
  /** 新增的数据点 */
  added: unknown[]
  /** 删除的数据点 */
  removed: unknown[]
  /** 修改的数据点 */
  modified: Array<{ index: number; oldValue: unknown; newValue: unknown }>
  /** 是否建议使用增量更新 */
  shouldPartialUpdate: boolean
}

/**
 * 数据源注册信息
 */
interface DataSource {
  id: string
  config: DataSourceConfig
  fetcher: () => Promise<unknown>
  /** 是否启用缓存 */
  enableCache?: boolean
  /** 缓存 TTL（毫秒） */
  cacheTtl?: number
}

/**
 * 绑定关系
 */
interface Binding {
  sourceId: string
  widgetId: string
  fieldMapping: Record<string, string>
  refreshInterval?: number
  state: Ref<BindingState>
  /** 上一次的数据（用于增量更新） */
  previousData: unknown
  /** 数据变更回调（用于局部更新） */
  onDataChange?: DataChangeCallback
  /** 是否启用局部更新 */
  enablePartialUpdate: boolean
}

/**
 * 数据绑定系统
 *
 * @example
 * ```typescript
 * const binder = new DataBinder()
 *
 * // 注册数据源
 * binder.registerSource({
 *   id: 'sales-report',
 *   type: 'report',
 *   reportId: '123'
 * }, fetchSalesData)
 *
 * // 绑定组件
 * binder.bind({
 *   widgetId: 'chart-1',
 *   sourceId: 'sales-report',
 *   fieldMapping: { value: 'total_sales', label: 'month' },
 *   refreshInterval: 60
 * })
 *
 * // 获取绑定数据
 * const data = binder.getData('chart-1')
 *
 * // 开始自动刷新
 * binder.startAutoRefresh()
 * ```
 */
export class DataBinder {
  private sources: Map<string, DataSource> = new Map()
  private bindings: Map<string, Binding> = new Map()
  // 使用 rAF 定时器替代 setInterval
  private refreshTimers: Map<string, RAFTimerController> = new Map()
  private globalRefreshTimer: RAFTimerController | null = null

  /**
   * 注册数据源
   *
   * @param config - 数据源配置
   * @param fetcher - 数据获取函数
   * @param options - 可选配置（缓存相关）
   */
  registerSource(
    config: DataSourceConfig & { id: string },
    fetcher: () => Promise<unknown>,
    options?: {
      /** 是否启用缓存，默认 true */
      enableCache?: boolean
      /** 缓存 TTL（毫秒），默认 60000 */
      cacheTtl?: number
    }
  ): void {
    if (this.sources.has(config.id)) {
      console.warn(`[DataBinder] 数据源 "${config.id}" 已存在，将被覆盖`)
    }

    this.sources.set(config.id, {
      id: config.id,
      config,
      fetcher,
      enableCache: options?.enableCache ?? true,
      cacheTtl: options?.cacheTtl ?? 60000
    })
  }

  /**
   * 移除数据源
   *
   * @param sourceId - 数据源 ID
   */
  removeSource(sourceId: string): void {
    // 停止相关绑定的刷新
    this.bindings.forEach((binding, widgetId) => {
      if (binding.sourceId === sourceId) {
        this.unbind(widgetId)
      }
    })

    this.sources.delete(sourceId)
  }

  /**
   * 获取所有数据源
   */
  getSources(): DataSource[] {
    return Array.from(this.sources.values())
  }

  /**
   * 绑定组件到数据源
   *
   * @param config - 绑定配置
   * @param options - 额外选项
   */
  bind(
    config: DataBindingConfig & { widgetId: string },
    options?: {
      /** 是否启用局部更新 */
      enablePartialUpdate?: boolean
      /** 数据变更回调 */
      onDataChange?: DataChangeCallback
    }
  ): void {
    const { widgetId, sourceId, fieldMapping, refreshInterval } = config
    const { enablePartialUpdate = true, onDataChange } = options || {}

    // 验证数据源存在
    if (!this.sources.has(sourceId)) {
      throw new Error(`[DataBinder] 数据源 "${sourceId}" 不存在`)
    }

    // 如果已有绑定，先解绑
    if (this.bindings.has(widgetId)) {
      this.unbind(widgetId)
    }

    // 创建绑定状态
    const state = ref<BindingState>({
      isLoading: false,
      lastRefresh: null,
      error: null,
      data: null
    })

    this.bindings.set(widgetId, {
      sourceId,
      widgetId,
      fieldMapping,
      refreshInterval,
      state,
      previousData: null,
      onDataChange,
      enablePartialUpdate
    })

    // 设置单独刷新定时器（如果有自定义刷新间隔）
    if (refreshInterval && refreshInterval > 0) {
      this.startWidgetRefresh(widgetId, refreshInterval)
    }
  }

  /**
   * 解绑组件
   *
   * @param widgetId - 组件 ID
   */
  unbind(widgetId: string): void {
    this.stopWidgetRefresh(widgetId)
    this.bindings.delete(widgetId)
  }

  /**
   * 获取组件的绑定状态（只读）
   *
   * @param widgetId - 组件 ID
   * @returns 绑定状态
   */
  getBindingState(widgetId: string): Readonly<Ref<BindingState>> | null {
    const binding = this.bindings.get(widgetId)
    return binding ? readonly(binding.state) : null
  }

  /**
   * 获取组件的绑定数据
   *
   * @param widgetId - 组件 ID
   * @returns 数据
   */
  getData(widgetId: string): unknown {
    const binding = this.bindings.get(widgetId)
    return binding?.state.value.data
  }

  /**
   * 刷新单个组件的数据
   *
   * @param widgetId - 组件 ID
   * @param forceFullUpdate - 是否强制完整更新（忽略局部更新优化）
   * @param forceRefresh - 是否强制刷新（忽略缓存）
   */
  async refresh(widgetId: string, forceFullUpdate: boolean = false, forceRefresh: boolean = false): Promise<void> {
    const binding = this.bindings.get(widgetId)
    if (!binding) return

    const source = this.sources.get(binding.sourceId)
    if (!source) return

    binding.state.value.isLoading = true
    binding.state.value.error = null

    try {
      // 生成缓存键
      const cacheKey = this.generateCacheKey(binding.sourceId, binding.fieldMapping)

      // 获取数据（使用缓存或直接请求）
      let data: unknown

      if (source.enableCache && !forceRefresh) {
        // 使用缓存
        data = await globalDataCache.fetch(
          cacheKey,
          source.fetcher,
          source.cacheTtl
        )
      } else {
        // 直接请求（不使用缓存）
        if (forceRefresh) {
          // 强制刷新时清除缓存
          globalDataCache.clear(cacheKey)
        }
        data = await source.fetcher()
      }

      const mappedData = this.applyFieldMapping(data, binding.fieldMapping)

      // 检测数据变化并触发局部更新
      if (binding.enablePartialUpdate && !forceFullUpdate && binding.previousData !== null) {
        const diff = this.diffData(binding.previousData, mappedData)

        if (diff.hasChanges && binding.onDataChange) {
          // 调用数据变更回调，让组件决定如何更新
          binding.onDataChange(mappedData, binding.previousData, diff)
        }
      }

      // 更新状态
      binding.state.value.data = mappedData
      binding.state.value.lastRefresh = new Date()

      // 保存当前数据用于下次比较
      binding.previousData = this.cloneData(mappedData)
    } catch (error) {
      binding.state.value.error = error instanceof Error ? error : new Error(String(error))
      console.error(`[DataBinder] 刷新组件 "${widgetId}" 数据失败:`, error)
    } finally {
      binding.state.value.isLoading = false
    }
  }

  /**
   * 生成缓存键
   *
   * @param sourceId - 数据源 ID
   * @param fieldMapping - 字段映射
   * @returns 缓存键
   */
  private generateCacheKey(sourceId: string, fieldMapping: Record<string, string>): string {
    // 使用稳定的键排序确保相同映射生成相同键
    const sortedMapping = Object.keys(fieldMapping)
      .sort()
      .map(key => `${key}:${fieldMapping[key]}`)
      .join(',')

    return `${sourceId}:{${sortedMapping}}`
  }

  /**
   * 比较数据差异
   *
   * @param oldData - 旧数据
   * @param newData - 新数据
   * @returns 数据差异结果
   */
  private diffData(oldData: unknown, newData: unknown): DataDiffResult {
    const result: DataDiffResult = {
      hasChanges: false,
      added: [],
      removed: [],
      modified: [],
      shouldPartialUpdate: true
    }

    // 如果类型不同，需要完整更新
    if (typeof oldData !== typeof newData) {
      result.hasChanges = true
      result.shouldPartialUpdate = false
      return result
    }

    // 如果是数组
    if (Array.isArray(oldData) && Array.isArray(newData)) {
      // 如果长度差异太大，建议完整更新
      const lengthDiff = Math.abs(oldData.length - newData.length)
      if (lengthDiff > oldData.length * 0.5) {
        result.hasChanges = true
        result.shouldPartialUpdate = false
        return result
      }

      // 比较数组元素
      const maxLen = Math.max(oldData.length, newData.length)
      for (let i = 0; i < maxLen; i++) {
        if (i >= oldData.length) {
          // 新增的元素
          result.added.push(newData[i])
          result.hasChanges = true
        } else if (i >= newData.length) {
          // 删除的元素
          result.removed.push(oldData[i])
          result.hasChanges = true
        } else if (!this.isEqual(oldData[i], newData[i])) {
          // 修改的元素
          result.modified.push({
            index: i,
            oldValue: oldData[i],
            newValue: newData[i]
          })
          result.hasChanges = true
        }
      }

      // 如果修改超过 50%，建议完整更新
      if (result.modified.length > maxLen * 0.5) {
        result.shouldPartialUpdate = false
      }

      return result
    }

    // 如果是对象
    if (typeof oldData === 'object' && typeof newData === 'object' && oldData !== null && newData !== null) {
      const oldObj = oldData as Record<string, unknown>
      const newObj = newData as Record<string, unknown>
      const allKeys = new Set([...Object.keys(oldObj), ...Object.keys(newObj)])

      let changeCount = 0
      allKeys.forEach(key => {
        if (!(key in oldObj)) {
          result.added.push({ key, value: newObj[key] })
          result.hasChanges = true
          changeCount++
        } else if (!(key in newObj)) {
          result.removed.push({ key, value: oldObj[key] })
          result.hasChanges = true
          changeCount++
        } else if (!this.isEqual(oldObj[key], newObj[key])) {
          result.modified.push({
            index: 0,
            oldValue: { key, value: oldObj[key] },
            newValue: { key, value: newObj[key] }
          })
          result.hasChanges = true
          changeCount++
        }
      })

      // 如果修改超过 50%，建议完整更新
      if (changeCount > allKeys.size * 0.5) {
        result.shouldPartialUpdate = false
      }

      return result
    }

    // 简单类型比较
    result.hasChanges = oldData !== newData
    result.shouldPartialUpdate = false
    return result
  }

  /**
   * 深度比较两个值是否相等
   */
  private isEqual(a: unknown, b: unknown): boolean {
    if (a === b) return true
    if (typeof a !== typeof b) return false
    if (a === null || b === null) return a === b

    if (Array.isArray(a) && Array.isArray(b)) {
      if (a.length !== b.length) return false
      return a.every((item, index) => this.isEqual(item, b[index]))
    }

    if (typeof a === 'object' && typeof b === 'object') {
      const objA = a as Record<string, unknown>
      const objB = b as Record<string, unknown>
      const keysA = Object.keys(objA)
      const keysB = Object.keys(objB)

      if (keysA.length !== keysB.length) return false
      return keysA.every(key => this.isEqual(objA[key], objB[key]))
    }

    return false
  }

  /**
   * 深度克隆数据
   */
  private cloneData(data: unknown): unknown {
    if (data === null || data === undefined) {
      return data
    }

    if (Array.isArray(data)) {
      return data.map(item => this.cloneData(item))
    }

    if (typeof data === 'object') {
      const result: Record<string, unknown> = {}
      for (const key in data) {
        if (Object.prototype.hasOwnProperty.call(data, key)) {
          result[key] = this.cloneData((data as Record<string, unknown>)[key])
        }
      }
      return result
    }

    return data
  }

  /**
   * 刷新数据源的所有绑定组件
   *
   * @param sourceId - 数据源 ID
   * @param forceFullUpdate - 是否强制完整更新
   * @param forceRefresh - 是否强制刷新（忽略缓存）
   */
  async refreshSource(sourceId: string, forceFullUpdate: boolean = false, forceRefresh: boolean = false): Promise<void> {
    const promises: Promise<void>[] = []

    this.bindings.forEach((binding, widgetId) => {
      if (binding.sourceId === sourceId) {
        promises.push(this.refresh(widgetId, forceFullUpdate, forceRefresh))
      }
    })

    await Promise.allSettled(promises)
  }

  /**
   * 刷新所有绑定组件
   *
   * @param forceFullUpdate - 是否强制完整更新
   */
  async refreshAll(forceFullUpdate: boolean = false): Promise<void> {
    const promises = Array.from(this.bindings.keys()).map(widgetId =>
      this.refresh(widgetId, forceFullUpdate)
    )
    await Promise.allSettled(promises)
  }

  /**
   * 开始全局自动刷新
   *
   * @param interval - 刷新间隔（秒）
   */
  startAutoRefresh(interval: number = 60): void {
    this.stopAutoRefresh()

    if (interval <= 0) return

    // 使用 rAF 定时器替代 setInterval
    this.globalRefreshTimer = new RAFTimerController(() => {
      this.refreshAll()
    }, interval)
    this.globalRefreshTimer.start()
  }

  /**
   * 停止全局自动刷新
   */
  stopAutoRefresh(): void {
    if (this.globalRefreshTimer) {
      this.globalRefreshTimer.stop()
      this.globalRefreshTimer = null
    }
  }

  /**
   * 开始组件单独刷新
   */
  private startWidgetRefresh(widgetId: string, interval: number): void {
    this.stopWidgetRefresh(widgetId)

    if (interval <= 0) return

    // 使用 rAF 定时器替代 setInterval
    const timer = new RAFTimerController(() => {
      this.refresh(widgetId)
    }, interval)
    timer.start()
    this.refreshTimers.set(widgetId, timer)
  }

  /**
   * 停止组件单独刷新
   */
  private stopWidgetRefresh(widgetId: string): void {
    const timer = this.refreshTimers.get(widgetId)
    if (timer) {
      timer.stop()
      this.refreshTimers.delete(widgetId)
    }
  }

  /**
   * 应用字段映射
   */
  private applyFieldMapping(data: unknown, mapping: Record<string, string>): unknown {
    if (!data || !mapping || Object.keys(mapping).length === 0) {
      return data
    }

    // 如果是数组，映射每个元素
    if (Array.isArray(data)) {
      return data.map(item => this.mapObject(item, mapping))
    }

    // 如果是对象，直接映射
    if (typeof data === 'object') {
      return this.mapObject(data as Record<string, unknown>, mapping)
    }

    return data
  }

  /**
   * 映射对象字段
   */
  private mapObject(obj: Record<string, unknown>, mapping: Record<string, string>): Record<string, unknown> {
    const result: Record<string, unknown> = {}

    for (const [targetField, sourceField] of Object.entries(mapping)) {
      if (sourceField in obj) {
        result[targetField] = obj[sourceField]
      }
    }

    // 保留原始字段
    return { ...obj, ...result }
  }

  /**
   * 获取绑定数量
   */
  get bindingCount(): number {
    return this.bindings.size
  }

  /**
   * 获取数据源数量
   */
  get sourceCount(): number {
    return this.sources.size
  }

  /**
   * 获取缓存统计信息
   *
   * @returns 缓存统计
   */
  getCacheStats() {
    return globalDataCache.getStats()
  }

  /**
   * 清除指定数据源的缓存
   *
   * @param sourceId - 数据源 ID
   */
  clearSourceCache(sourceId: string): void {
    // 清除该数据源相关的所有缓存
    const keysToDelete: string[] = []
    globalDataCache.getStats().keys.forEach(key => {
      if (key.startsWith(`${sourceId}:`)) {
        keysToDelete.push(key)
      }
    })
    keysToDelete.forEach(key => globalDataCache.clear(key))
  }

  /**
   * 清除所有缓存
   */
  clearAllCache(): void {
    globalDataCache.clear()
  }

  /**
   * 强制刷新数据源（忽略缓存）
   *
   * @param sourceId - 数据源 ID
   */
  async forceRefreshSource(sourceId: string): Promise<void> {
    // 先清除该数据源的缓存
    this.clearSourceCache(sourceId)
    // 然后刷新
    await this.refreshSource(sourceId, false, true)
  }

  /**
   * 清理所有绑定和数据源
   */
  destroy(): void {
    this.stopAutoRefresh()
    this.refreshTimers.forEach((timer, widgetId) => {
      this.stopWidgetRefresh(widgetId)
    })
    this.bindings.clear()
    this.sources.clear()
  }
}

// 导出全局单例
export const dataBinder = new DataBinder()
