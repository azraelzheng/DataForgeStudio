/**
 * DataBinder - 数据绑定系统
 * 负责管理数据源和组件之间的绑定关系，支持自动刷新
 * 使用 requestAnimationFrame 替代 setInterval，实现与浏览器刷新率同步
 * @module dashboard/core/DataBinder
 */

import { ref, type Ref, readonly } from 'vue'
import { RAFTimerController } from '../../display/composables/useAnimationFrame'
import type {
  DataSourceConfig,
  DataBindingConfig,
  BindingState
} from '../types/dashboard'

/**
 * 数据源注册信息
 */
interface DataSource {
  id: string
  config: DataSourceConfig
  fetcher: () => Promise<unknown>
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
   */
  registerSource(config: DataSourceConfig & { id: string }, fetcher: () => Promise<unknown>): void {
    if (this.sources.has(config.id)) {
      console.warn(`[DataBinder] 数据源 "${config.id}" 已存在，将被覆盖`)
    }

    this.sources.set(config.id, {
      id: config.id,
      config,
      fetcher
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
   */
  bind(config: DataBindingConfig & { widgetId: string }): void {
    const { widgetId, sourceId, fieldMapping, refreshInterval } = config

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
      state
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
   */
  async refresh(widgetId: string): Promise<void> {
    const binding = this.bindings.get(widgetId)
    if (!binding) return

    const source = this.sources.get(binding.sourceId)
    if (!source) return

    binding.state.value.isLoading = true
    binding.state.value.error = null

    try {
      const data = await source.fetcher()
      binding.state.value.data = this.applyFieldMapping(data, binding.fieldMapping)
      binding.state.value.lastRefresh = new Date()
    } catch (error) {
      binding.state.value.error = error instanceof Error ? error : new Error(String(error))
      console.error(`[DataBinder] 刷新组件 "${widgetId}" 数据失败:`, error)
    } finally {
      binding.state.value.isLoading = false
    }
  }

  /**
   * 刷新数据源的所有绑定组件
   *
   * @param sourceId - 数据源 ID
   */
  async refreshSource(sourceId: string): Promise<void> {
    const promises: Promise<void>[] = []

    this.bindings.forEach((binding, widgetId) => {
      if (binding.sourceId === sourceId) {
        promises.push(this.refresh(widgetId))
      }
    })

    await Promise.allSettled(promises)
  }

  /**
   * 刷新所有绑定组件
   */
  async refreshAll(): Promise<void> {
    const promises = Array.from(this.bindings.keys()).map(widgetId => this.refresh(widgetId))
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
