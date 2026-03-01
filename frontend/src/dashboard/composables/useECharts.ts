/**
 * useECharts - ECharts Composable
 * 负责 ECharts 实例的初始化、配置、响应式调整和销毁
 * @module dashboard/composables/useECharts
 */

import { ref, onMounted, onUnmounted, watch, type Ref } from 'vue'
import * as echarts from 'echarts/core'
import type {
  EChartsOption,
  ECharts,
  SetOptionOpts,
  CustomChartOption
} from 'echarts/core'
import type { RefElement } from '../types/internal'

/**
 * 主题类型
 */
export type ThemeType = 'default' | 'light' | 'dark' | string

/**
 * useECharts 配置选项
 */
export interface UseEChartsOptions {
  /** 主题 */
  theme?: ThemeType
  /** 初始化选项 */
  initOpts?: {
    renderer?: 'canvas' | 'svg'
    width?: number | string
    height?: number | string
  }
  /** 是否自动监听大小变化 */
  autoResize?: boolean
  /** 防抖延迟 (ms) */
  resizeDelay?: number
  /** 销毁时是否清除实例 */
  disposeOnUnmount?: boolean
}

/**
 * useECharts 返回值
 */
export interface UseEChartsReturn {
  /** DOM 元素引用 */
  chartRef: Ref<HTMLElement | undefined>
  /** ECharts 实例 */
  chartInstance: Ref<ECharts | undefined>
  /** 是否加载中 */
  loading: Ref<boolean>
  /** 是否已初始化 */
  isInitialized: Ref<boolean>
  /** 初始化图表 */
  init: () => void
  /** 设置图表配置 */
  setOption: (option: EChartsOption, notMerge?: boolean, lazyUpdate?: boolean) => void
  /** 显示加载动画 */
  showLoading: (type?: string, opts?: Record<string, unknown>) => void
  /** 隐藏加载动画 */
  hideLoading: () => void
  /** 调整图表大小 */
  resize: (opts?: { width?: number | string; height?: number | string }) => void
  /** 销毁图表实例 */
  dispose: () => void
  /** 获取图表截图 */
  getDataURL: (opts?: {
    type?: string
    pixelRatio?: number
    backgroundColor?: string
  }) => string
}

/**
 * Resize 监听器管理
 */
class ResizeManager {
  private timer: ReturnType<typeof setTimeout> | null = null
  private observer: ResizeObserver | null = null
  private callback: () => void
  private delay: number

  constructor(callback: () => void, delay: number = 200) {
    this.callback = callback
    this.delay = delay
  }

  observe(element: HTMLElement): void {
    // 使用 ResizeObserver 监听容器大小变化
    if (window.ResizeObserver) {
      this.observer = new ResizeObserver(() => {
        this.schedule()
      })
      this.observer.observe(element)
    } else {
      // 降级使用 window.resize
      window.addEventListener('resize', this.schedule)
    }
  }

  unobserve(): void {
    if (this.observer) {
      this.observer.disconnect()
      this.observer = null
    }
    window.removeEventListener('resize', this.schedule)
    if (this.timer) {
      clearTimeout(this.timer)
      this.timer = null
    }
  }

  private schedule = (): void => {
    if (this.timer) {
      clearTimeout(this.timer)
    }
    this.timer = setTimeout(() => {
      this.callback()
      this.timer = null
    }, this.delay)
  }
}

/**
 * ECharts Composable
 *
 * @example
 * ```vue
 * <script setup lang="ts">
 * import { useECharts } from '@/dashboard/composables/useECharts'
 *
 * const { chartRef, chartInstance, setOption, resize } = useECharts({
 *   theme: 'dark',
 *   autoResize: true
 * })
 *
 * onMounted(() => {
 *   setOption({
 *     xAxis: { type: 'category', data: ['Mon', 'Tue', 'Wed'] },
 *     yAxis: { type: 'value' },
 *     series: [{ type: 'bar', data: [120, 200, 150] }]
 *   })
 * })
 * </script>
 *
 * <template>
 *   <div ref="chartRef" style="width: 100%; height: 400px"></div>
 * </template>
 * ```
 */
export function useECharts(options: UseEChartsOptions = {}): UseEChartsReturn {
  const {
    theme = 'default',
    initOpts,
    autoResize = true,
    resizeDelay = 200,
    disposeOnUnmount = true
  } = options

  // 状态
  const chartRef: Ref<HTMLElement | undefined> = ref()
  const chartInstance: Ref<ECharts | undefined> = ref()
  const loading = ref(false)
  const isInitialized = ref(false)

  // Resize 管理器
  let resizeManager: ResizeManager | null = null

  /**
   * 初始化图表
   */
  function init(): void {
    if (!chartRef.value) {
      console.warn('[useECharts] chartRef 未挂载，无法初始化')
      return
    }

    if (chartInstance.value) {
      console.warn('[useECharts] 图表实例已存在')
      return
    }

    try {
      // 初始化 ECharts 实例
      chartInstance.value = echarts.init(chartRef.value, theme, initOpts)
      isInitialized.value = true

      // 设置自动调整大小
      if (autoResize) {
        resizeManager = new ResizeManager(() => {
          resize()
        }, resizeDelay)
        resizeManager.observe(chartRef.value)
      }

      console.log('[useECharts] 图表初始化成功')
    } catch (error) {
      console.error('[useECharts] 图表初始化失败:', error)
    }
  }

  /**
   * 设置图表配置
   */
  function setOption(option: EChartsOption, notMerge?: boolean, lazyUpdate?: boolean): void {
    if (!chartInstance.value || !isInitialized.value) {
      console.warn('[useECharts] 图表未初始化，无法设置配置')
      return
    }

    const opts: SetOptionOpts = {
      notMerge: notMerge ?? false,
      lazyUpdate: lazyUpdate ?? false
    }

    try {
      chartInstance.value.setOption(option, opts)
    } catch (error) {
      console.error('[useECharts] 设置配置失败:', error)
    }
  }

  /**
   * 显示加载动画
   */
  function showLoading(type: string = 'default', opts: Record<string, unknown> = {}): void {
    if (!chartInstance.value) return

    loading.value = true
    const defaultOpts = {
      text: '加载中...',
      color: '#409EFF',
      textColor: '#333',
      maskColor: 'rgba(255, 255, 255, 0.8)',
      zlevel: 0
    }
    chartInstance.value.showLoading(type, { ...defaultOpts, ...opts })
  }

  /**
   * 隐藏加载动画
   */
  function hideLoading(): void {
    if (!chartInstance.value) return

    loading.value = false
    chartInstance.value.hideLoading()
  }

  /**
   * 调整图表大小
   */
  function resize(opts?: { width?: number | string; height?: number | string }): void {
    if (!chartInstance.value) return

    try {
      chartInstance.value.resize(opts)
    } catch (error) {
      console.error('[useECharts] 调整大小失败:', error)
    }
  }

  /**
   * 销毁图表实例
   */
  function dispose(): void {
    if (resizeManager) {
      resizeManager.unobserve()
      resizeManager = null
    }

    if (chartInstance.value) {
      try {
        chartInstance.value.dispose()
        chartInstance.value = undefined
        isInitialized.value = false
        console.log('[useECharts] 图表实例已销毁')
      } catch (error) {
        console.error('[useECharts] 销毁图表失败:', error)
      }
    }
  }

  /**
   * 获取图表截图
   */
  function getDataURL(opts: {
    type?: string
    pixelRatio?: number
    backgroundColor?: string
  } = {}): string {
    if (!chartInstance.value) {
      console.warn('[useECharts] 图表未初始化，无法获取截图')
      return ''
    }

    const defaultOpts = {
      type: 'png',
      pixelRatio: window.devicePixelRatio || 1,
      backgroundColor: '#fff'
    }

    return chartInstance.value.getDataURL({ ...defaultOpts, ...opts })
  }

  // 组件挂载时初始化
  onMounted(() => {
    if (chartRef.value) {
      init()
    }
  })

  // 组件卸载时销毁
  onUnmounted(() => {
    if (disposeOnUnmount) {
      dispose()
    }
  })

  // 监听 chartRef 变化
  watch(chartRef, (newRef) => {
    if (newRef && !chartInstance.value) {
      init()
    }
  })

  return {
    chartRef,
    chartInstance,
    loading,
    isInitialized,
    init,
    setOption,
    showLoading,
    hideLoading,
    resize,
    dispose,
    getDataURL
  }
}

/**
 * 图表配置构建器 - 用于快速构建常见图表配置
 */
export class ChartConfigBuilder {
  private option: EChartsOption = {}

  /**
   * 设置标题
   */
  setTitle(title: string, subtext?: string): this {
    this.option.title = {
      text: title,
      subtext: subtext || '',
      left: 'center',
      textStyle: {
        fontSize: 16,
        fontWeight: 'normal'
      }
    }
    return this
  }

  /**
   * 设置提示框
   */
  setTooltip(trigger: 'item' | 'axis' = 'item'): this {
    this.option.tooltip = {
      trigger,
      triggerOn: 'mousemove',
      backgroundColor: 'rgba(50, 50, 50, 0.9)',
      borderColor: '#333',
      borderWidth: 0,
      textStyle: {
        color: '#fff'
      }
    }
    return this
  }

  /**
   * 设置图例
   */
  setLegend(data: string[], orient: 'horizontal' | 'vertical' = 'horizontal'): this {
    this.option.legend = {
      data,
      orient,
      left: 'center',
      top: orient === 'horizontal' ? 'bottom' : 'center'
    }
    return this
  }

  /**
   * 设置 X 轴
   */
  setXAxis(data: string[], name?: string): this {
    this.option.xAxis = {
      type: 'category',
      data,
      name: name || '',
      axisLine: {
        lineStyle: { color: '#999' }
      },
      axisLabel: {
        color: '#666'
      }
    }
    return this
  }

  /**
   * 设置 Y 轴
   */
  setYAxis(name?: string, min?: number | string, max?: number | string): this {
    this.option.yAxis = {
      type: 'value',
      name: name || '',
      min: min ?? 'dataMin',
      max: max ?? 'dataMax',
      axisLine: {
        lineStyle: { color: '#999' }
      },
      axisLabel: {
        color: '#666'
      },
      splitLine: {
        lineStyle: {
          color: '#eee',
          type: 'dashed'
        }
      }
    }
    return this
  }

  /**
   * 添加系列
   */
  addSeries(
    name: string,
    type: string,
    data: unknown[],
    color?: string,
    yAxisIndex?: number
  ): this {
    if (!Array.isArray(this.option.series)) {
      this.option.series = []
    }

    const series: Record<string, unknown> = {
      name,
      type,
      data,
      yAxisIndex
    }

    if (color) {
      series.color = color
    }

    // 根据类型设置特定配置
    switch (type) {
      case 'bar':
        series.barMaxWidth = 50
        series.itemStyle = {
          borderRadius: [4, 4, 0, 0]
        }
        break
      case 'line':
        series.smooth = true
        series.symbolSize = 8
        series.lineStyle = {
          width: 2
        }
        break
      case 'pie':
      case 'doughnut':
        series.radius = type === 'doughnut' ? ['40%', '70%'] : '70%'
        series.emphasis = {
          itemStyle: {
            shadowBlur: 10,
            shadowOffsetX: 0,
            shadowColor: 'rgba(0, 0, 0, 0.5)'
          }
        }
        break
    }

    this.option.series.push(series as CustomChartOption)
    return this
  }

  /**
   * 设置颜色方案
   */
  setColors(colors: string[]): this {
    this.option.color = colors
    return this
  }

  /**
   * 设置网格配置
   */
  setGrid(config: {
    left?: string | number
    right?: string | number
    top?: string | number
    bottom?: string | number
  } = {}): this {
    this.option.grid = {
      left: config.left || '3%',
      right: config.right || '4%',
      top: config.top || '15%',
      bottom: config.bottom || '10%',
      containLabel: true
    }
    return this
  }

  /**
   * 构建配置
   */
  build(): EChartsOption {
    return this.option
  }

  /**
   * 重置构建器
   */
  reset(): this {
    this.option = {}
    return this
  }
}

/**
 * 常用颜色方案
 */
export const ColorSchemes = {
  /** 默认蓝色系 */
  default: ['#5470C6', '#91CC75', '#FAC858', '#EE6666', '#73C0DE', '#3BA272', '#FC8452', '#9A60B4'],
  /** 商务风格 */
  business: ['#1890FF', '#52C41A', '#FAAD14', '#F5222D', '#722ED1', '#13C2C2', '#EB2F96', '#FA8C16'],
  /** 暖色系 */
  warm: ['#FF6B6B', '#FF8E72', '#FFB347', '#FFCC5C', '#FFD93D', '#FFE066'],
  /** 冷色系 */
  cool: ['#4ECDC4', '#45B7D1', '#5DADE2', '#3498DB', '#2E86C1', '#2874A6'],
  /** 单色渐变 */
  monochrome: ['#E8F4F8', '#D1E8F0', '#A9D6E5', '#81C7D4', '#54A0B8', '#2878A0', '#18537E']
}

/**
 * 导出类型
 */
export type { EChartsOption, ECharts }
