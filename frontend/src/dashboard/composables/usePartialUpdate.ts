/**
 * usePartialUpdate - 图表局部更新 Hook
 * 只更新数据部分，避免完整重绘，提高性能
 * @module dashboard/composables/usePartialUpdate
 */

import { ref, type Ref } from 'vue'
import type { ECharts } from 'echarts'

/**
 * 局部更新选项
 */
export interface PartialUpdateOptions {
  /** 是否启用局部更新 */
  enabled: boolean
  /** 更新模式: 'replace' | 'append' | 'merge' */
  mode: 'replace' | 'append' | 'merge'
  /** 最大数据点数（超过则裁剪） */
  maxDataPoints?: number
}

/**
 * 默认局部更新选项
 */
const DEFAULT_OPTIONS: PartialUpdateOptions = {
  enabled: true,
  mode: 'merge'
}

/**
 * 数据变更点
 */
export interface DataChange {
  /** 数据索引 */
  index: number
  /** 新值 */
  value: unknown
}

/**
 * 系列数据更新结果
 */
export interface UpdateResult {
  /** 是否成功 */
  success: boolean
  /** 更新的数据点数量 */
  count: number
  /** 错误信息 */
  error?: string
}

/**
 * 图表局部更新 Hook
 * 只更新数据部分，避免完整重绘
 *
 * @param chartInstance - ECharts 实例引用
 * @param options - 局部更新选项
 *
 * @example
 * ```typescript
 * const chartInstance = ref<ECharts>()
 * const { updateSeriesData, appendSeriesData, deltaUpdate } = usePartialUpdate(chartInstance)
 *
 * // 更新单个系列数据
 * updateSeriesData(0, [10, 20, 30, 40])
 *
 * // 追加数据
 * appendSeriesData(0, [50, 60])
 *
 * // 增量更新特定数据点
 * deltaUpdate([{ index: 0, value: 15 }, { index: 2, value: 35 }])
 * ```
 */
export function usePartialUpdate(
  chartInstance: Ref<ECharts | undefined>,
  options: PartialUpdateOptions = DEFAULT_OPTIONS
) {
  const isUpdating = ref(false)
  const lastUpdateTime = ref<Date | null>(null)

  // 合并默认选项
  const opts: PartialUpdateOptions = {
    ...DEFAULT_OPTIONS,
    ...options
  }

  /**
   * 更新单个系列的数据
   *
   * @param seriesIndex - 系列索引
   * @param data - 新数据
   * @returns 更新结果
   */
  function updateSeriesData(
    seriesIndex: number,
    data: unknown[]
  ): UpdateResult {
    if (!chartInstance.value) {
      return { success: false, count: 0, error: '图表实例不存在' }
    }

    if (isUpdating.value) {
      return { success: false, count: 0, error: '正在更新中' }
    }

    if (!Array.isArray(data)) {
      return { success: false, count: 0, error: '数据必须是数组' }
    }

    isUpdating.value = true

    try {
      const option = chartInstance.value.getOption() as Record<string, unknown>
      const series = option.series as Record<string, unknown>[] | undefined

      if (!series || !series[seriesIndex]) {
        return { success: false, count: 0, error: `系列索引 ${seriesIndex} 不存在` }
      }

      // 只更新数据，不改变其他配置
      series[seriesIndex] = {
        ...series[seriesIndex],
        data
      }

      chartInstance.value.setOption(
        { series },
        { notMerge: false, replaceMerge: ['series'] }
      )

      lastUpdateTime.value = new Date()

      return { success: true, count: data.length }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error)
      console.error('[usePartialUpdate] 更新系列数据失败:', errorMessage)
      return { success: false, count: 0, error: errorMessage }
    } finally {
      isUpdating.value = false
    }
  }

  /**
   * 追加数据到系列末尾
   *
   * @param seriesIndex - 系列索引
   * @param newData - 新数据（单个或数组）
   * @param maxPoints - 可选的最大数据点数（覆盖全局设置）
   * @returns 更新结果
   */
  function appendSeriesData(
    seriesIndex: number,
    newData: unknown | unknown[],
    maxPoints?: number
  ): UpdateResult {
    if (!chartInstance.value) {
      return { success: false, count: 0, error: '图表实例不存在' }
    }

    const option = chartInstance.value.getOption() as Record<string, unknown>
    const series = option.series as Record<string, unknown>[] | undefined

    if (!series || !series[seriesIndex]) {
      return { success: false, count: 0, error: `系列索引 ${seriesIndex} 不存在` }
    }

    const currentData = (series[seriesIndex].data as unknown[]) || []
    const dataToAdd = Array.isArray(newData) ? newData : [newData]

    let updatedData = [...currentData, ...dataToAdd]

    // 裁剪超出最大数据点的数据
    const effectiveMaxPoints = maxPoints ?? opts.maxDataPoints
    if (effectiveMaxPoints && updatedData.length > effectiveMaxPoints) {
      updatedData = updatedData.slice(-effectiveMaxPoints)
    }

    return updateSeriesData(seriesIndex, updatedData)
  }

  /**
   * 增量更新（只更新变化的数据点）
   * 适用于只需要修改少量数据点的场景
   *
   * @param changes - 数据变更列表
   * @param seriesIndex - 系列索引，默认为 0
   * @returns 更新结果
   */
  function deltaUpdate(
    changes: DataChange[],
    seriesIndex: number = 0
  ): UpdateResult {
    if (!chartInstance.value) {
      return { success: false, count: 0, error: '图表实例不存在' }
    }

    if (!changes || changes.length === 0) {
      return { success: false, count: 0, error: '变更列表为空' }
    }

    const option = chartInstance.value.getOption() as Record<string, unknown>
    const series = option.series as Record<string, unknown>[] | undefined

    if (!series || !series[seriesIndex]) {
      return { success: false, count: 0, error: `系列索引 ${seriesIndex} 不存在` }
    }

    const data = (series[seriesIndex].data as unknown[]) || []
    let updateCount = 0

    // 应用变更
    changes.forEach(({ index, value }) => {
      if (index >= 0 && index < data.length) {
        data[index] = value
        updateCount++
      }
    })

    if (updateCount === 0) {
      return { success: false, count: 0, error: '没有有效的变更' }
    }

    try {
      chartInstance.value.setOption({ series }, { notMerge: false })
      lastUpdateTime.value = new Date()
      return { success: true, count: updateCount }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error)
      console.error('[usePartialUpdate] 增量更新失败:', errorMessage)
      return { success: false, count: 0, error: errorMessage }
    }
  }

  /**
   * 替换指定范围的数据
   *
   * @param seriesIndex - 系列索引
   * @param startIndex - 起始索引
   * @param newData - 新数据
   * @returns 更新结果
   */
  function replaceRange(
    seriesIndex: number,
    startIndex: number,
    newData: unknown[]
  ): UpdateResult {
    if (!chartInstance.value) {
      return { success: false, count: 0, error: '图表实例不存在' }
    }

    if (startIndex < 0) {
      return { success: false, count: 0, error: '起始索引不能为负数' }
    }

    const option = chartInstance.value.getOption() as Record<string, unknown>
    const series = option.series as Record<string, unknown>[] | undefined

    if (!series || !series[seriesIndex]) {
      return { success: false, count: 0, error: `系列索引 ${seriesIndex} 不存在` }
    }

    const data = (series[seriesIndex].data as unknown[]) || []

    // 替换指定范围
    for (let i = 0; i < newData.length; i++) {
      const targetIndex = startIndex + i
      if (targetIndex < data.length) {
        data[targetIndex] = newData[i]
      }
    }

    return updateSeriesData(seriesIndex, data)
  }

  /**
   * 清空系列数据
   *
   * @param seriesIndex - 系列索引
   * @returns 更新结果
   */
  function clearSeriesData(seriesIndex: number): UpdateResult {
    return updateSeriesData(seriesIndex, [])
  }

  /**
   * 获取当前系列数据
   *
   * @param seriesIndex - 系列索引
   * @returns 当前数据或 null
   */
  function getSeriesData(seriesIndex: number): unknown[] | null {
    if (!chartInstance.value) {
      return null
    }

    const option = chartInstance.value.getOption() as Record<string, unknown>
    const series = option.series as Record<string, unknown>[] | undefined

    if (!series || !series[seriesIndex]) {
      return null
    }

    return (series[seriesIndex].data as unknown[]) || []
  }

  /**
   * 批量更新多个系列
   *
   * @param updates - 系列更新列表
   * @returns 所有更新结果
   */
  function batchUpdate(
    updates: Array<{ seriesIndex: number; data: unknown[] }>
  ): UpdateResult[] {
    return updates.map(({ seriesIndex, data }) =>
      updateSeriesData(seriesIndex, data)
    )
  }

  return {
    /** 是否正在更新 */
    isUpdating,
    /** 上次更新时间 */
    lastUpdateTime,
    /** 更新单个系列数据 */
    updateSeriesData,
    /** 追加数据到系列末尾 */
    appendSeriesData,
    /** 增量更新特定数据点 */
    deltaUpdate,
    /** 替换指定范围的数据 */
    replaceRange,
    /** 清空系列数据 */
    clearSeriesData,
    /** 获取当前系列数据 */
    getSeriesData,
    /** 批量更新多个系列 */
    batchUpdate
  }
}

/**
 * 导出类型
 */
export type {
  PartialUpdateOptions as PartialUpdateOptionsType,
  DataChange as DataChangeType,
  UpdateResult as UpdateResultType
}
