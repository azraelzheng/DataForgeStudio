/**
 * usePerformanceDegradation - 性能降级策略 Hook
 * 根据性能指标自动调整视觉效果，确保大屏在低性能设备上流畅运行
 * @module display/composables/usePerformanceDegradation
 */

import { computed, type Ref, type ComputedRef } from 'vue'

/**
 * 降级配置
 */
export interface DegradationConfig {
  /** 禁用粒子效果 */
  disableParticles: boolean
  /** 禁用发光边框 */
  disableGlow: boolean
  /** 禁用模糊背景 */
  disableBlur: boolean
  /** 禁用动画 */
  disableAnimations: boolean
  /** 降低图表细节 */
  reduceChartDetail: boolean
  /** 禁用阴影效果 */
  disableShadows: boolean
  /** 降低渐变复杂度 */
  reduceGradients: boolean
}

/**
 * CSS 样式对象
 */
export type CSSStyle = Record<string, string | number | undefined>

/**
 * ECharts 配置对象
 */
export type EChartsOption = Record<string, unknown>

/**
 * 降级等级
 */
export type DegradationLevel = 'none' | 'light' | 'medium' | 'heavy'

/**
 * 降级策略配置选项
 */
export interface DegradationOptions {
  /** 自定义降级等级阈值 */
  thresholds?: {
    /** 轻度降级的 FPS 阈值 */
    light: number
    /** 中度降级的 FPS 阈值 */
    medium: number
    /** 重度降级的 FPS 阈值 */
    heavy: number
  }
}

/**
 * 默认降级阈值
 */
const DEFAULT_THRESHOLDS = {
  light: 45,
  medium: 30,
  heavy: 20
}

/**
 * 性能降级 Hook
 * 根据性能指标自动调整视觉效果
 *
 * @param isLowPerformance - 是否处于低性能模式
 * @param shouldReduceMotion - 是否应减少动画
 * @param options - 降级策略配置选项
 * @returns 降级配置和辅助方法
 *
 * @example
 * ```typescript
 * const { fps, isLowPerformance, shouldReduceMotion } = usePerformanceMonitor()
 * const { config, getDegradedStyle, getDegradedChartOption } = usePerformanceDegradation(
 *   isLowPerformance,
 *   shouldReduceMotion
 * )
 *
 * // 在模板中使用
 * <div :style="getDegradedStyle(baseStyle)">内容</div>
 *
 * // 在图表配置中使用
 * const chartOption = getDegradedChartOption(baseChartOption)
 * ```
 */
export function usePerformanceDegradation(
  isLowPerformance: Ref<boolean>,
  shouldReduceMotion: Ref<boolean>,
  fps?: Ref<number>,
  options: DegradationOptions = {}
): {
  /** 降级配置（计算属性） */
  config: ComputedRef<DegradationConfig>
  /** 当前降级等级 */
  degradationLevel: ComputedRef<DegradationLevel>
  /** 获取降级后的样式 */
  getDegradedStyle: (baseStyle: CSSStyle) => CSSStyle
  /** 获取降级后的 ECharts 配置 */
  getDegradedChartOption: (baseOption: EChartsOption) => EChartsOption
  /** 获取降级后的动画时长 */
  getDegradedAnimationDuration: (baseDuration: number) => number
  /** 检查是否应禁用特定效果 */
  shouldDisable: (effect: keyof DegradationConfig) => boolean
} {
  const thresholds = {
    ...DEFAULT_THRESHOLDS,
    ...options.thresholds
  }

  /**
   * 计算当前降级等级
   */
  const degradationLevel = computed<DegradationLevel>(() => {
    if (!isLowPerformance.value && !shouldReduceMotion.value) {
      return 'none'
    }

    const currentFps = fps?.value ?? 30

    if (currentFps < thresholds.heavy) {
      return 'heavy'
    } else if (currentFps < thresholds.medium) {
      return 'medium'
    } else if (currentFps < thresholds.light) {
      return 'light'
    }

    return 'none'
  })

  /**
   * 计算降级配置
   */
  const config = computed<DegradationConfig>(() => {
    const level = degradationLevel.value

    // 无降级
    if (level === 'none') {
      return {
        disableParticles: false,
        disableGlow: false,
        disableBlur: false,
        disableAnimations: false,
        reduceChartDetail: false,
        disableShadows: false,
        reduceGradients: false
      }
    }

    // 轻度降级
    if (level === 'light') {
      return {
        disableParticles: false,
        disableGlow: false,
        disableBlur: false,
        disableAnimations: false,
        reduceChartDetail: false,
        disableShadows: true,
        reduceGradients: true
      }
    }

    // 中度降级
    if (level === 'medium') {
      return {
        disableParticles: true,
        disableGlow: true,
        disableBlur: shouldReduceMotion.value,
        disableAnimations: shouldReduceMotion.value,
        reduceChartDetail: true,
        disableShadows: true,
        reduceGradients: true
      }
    }

    // 重度降级
    return {
      disableParticles: true,
      disableGlow: true,
      disableBlur: true,
      disableAnimations: true,
      reduceChartDetail: true,
      disableShadows: true,
      reduceGradients: true
    }
  })

  /**
   * 获取降级后的样式
   * 移除或简化耗性能的 CSS 效果
   *
   * @param baseStyle - 基础样式对象
   * @returns 降级后的样式对象
   */
  function getDegradedStyle(baseStyle: CSSStyle): CSSStyle {
    const result: CSSStyle = { ...baseStyle }
    const cfg = config.value

    // 禁用阴影
    if (cfg.disableShadows) {
      delete result.boxShadow
      delete result.textShadow
      delete result['box-shadow']
      delete result['text-shadow']
    }

    // 禁用模糊背景
    if (cfg.disableBlur) {
      delete result.backdropFilter
      delete result['-webkit-backdrop-filter']
      delete result['backdrop-filter']
    }

    // 禁用动画
    if (cfg.disableAnimations) {
      result.animation = 'none'
      result.transition = 'none'
    }

    // 简化渐变
    if (cfg.reduceGradients) {
      // 如果存在渐变背景，替换为纯色
      if (typeof result.background === 'string' && result.background.includes('gradient')) {
        delete result.background
      }
      if (typeof result.backgroundImage === 'string' && result.backgroundImage.includes('gradient')) {
        delete result.backgroundImage
      }
    }

    return result
  }

  /**
   * 获取降级后的 ECharts 配置
   * 减少图表渲染复杂度
   *
   * @param baseOption - 基础图表配置
   * @returns 降级后的图表配置
   */
  function getDegradedChartOption(baseOption: EChartsOption): EChartsOption {
    const cfg = config.value

    // 无需降级
    if (!cfg.reduceChartDetail && !cfg.disableAnimations) {
      return baseOption
    }

    const result: EChartsOption = { ...baseOption }

    // 禁用图表动画
    if (cfg.disableAnimations) {
      result.animation = false
    }

    // 降低图表细节
    if (cfg.reduceChartDetail) {
      // 处理系列配置
      if (Array.isArray(result.series)) {
        result.series = result.series.map((series: Record<string, unknown>) => {
          const degradedSeries = { ...series }

          // 禁用折线图平滑（提升性能）
          if (series.type === 'line') {
            degradedSeries.smooth = false
          }

          // 简化散点图
          if (series.type === 'scatter') {
            degradedSeries.symbol = 'circle'
            degradedSeries.symbolSize = 4
          }

          // 简化饼图
          if (series.type === 'pie') {
            // 禁用饼图的玫瑰图效果
            degradedSeries.roseType = false
          }

          // 禁用系列动画
          if (cfg.disableAnimations) {
            degradedSeries.animation = false
          }

          return degradedSeries
        })
      }

      // 简化提示框
      if (result.tooltip && typeof result.tooltip === 'object') {
        result.tooltip = {
          ...result.tooltip as Record<string, unknown>,
          animation: false,
          transitionDuration: 0
        }
      }
    }

    return result
  }

  /**
   * 获取降级后的动画时长
   * 根据性能状态调整动画速度
   *
   * @param baseDuration - 基础动画时长（毫秒）
   * @returns 调整后的动画时长
   */
  function getDegradedAnimationDuration(baseDuration: number): number {
    if (config.value.disableAnimations) {
      return 0
    }

    const level = degradationLevel.value

    switch (level) {
      case 'light':
        return Math.round(baseDuration * 0.5)
      case 'medium':
        return Math.round(baseDuration * 0.25)
      case 'heavy':
        return 0
      default:
        return baseDuration
    }
  }

  /**
   * 检查是否应禁用特定效果
   *
   * @param effect - 效果名称
   * @returns 是否应禁用
   */
  function shouldDisable(effect: keyof DegradationConfig): boolean {
    return config.value[effect] === true
  }

  return {
    config,
    degradationLevel,
    getDegradedStyle,
    getDegradedChartOption,
    getDegradedAnimationDuration,
    shouldDisable
  }
}

/**
 * 预设降级配置
 * 可用于快速配置不同的降级策略
 */
export const DEGRADATION_PRESETS = {
  /** 无降级 */
  none: {
    disableParticles: false,
    disableGlow: false,
    disableBlur: false,
    disableAnimations: false,
    reduceChartDetail: false,
    disableShadows: false,
    reduceGradients: false
  } as DegradationConfig,

  /** 轻度降级 */
  light: {
    disableParticles: false,
    disableGlow: false,
    disableBlur: false,
    disableAnimations: false,
    reduceChartDetail: false,
    disableShadows: true,
    reduceGradients: true
  } as DegradationConfig,

  /** 中度降级 */
  medium: {
    disableParticles: true,
    disableGlow: true,
    disableBlur: true,
    disableAnimations: false,
    reduceChartDetail: true,
    disableShadows: true,
    reduceGradients: true
  } as DegradationConfig,

  /** 重度降级 */
  heavy: {
    disableParticles: true,
    disableGlow: true,
    disableBlur: true,
    disableAnimations: true,
    reduceChartDetail: true,
    disableShadows: true,
    reduceGradients: true
  } as DegradationConfig
} as const

/**
 * 合并自定义降级配置
 *
 * @param base - 基础配置
 * @param custom - 自定义覆盖
 * @returns 合并后的配置
 */
export function mergeDegradationConfig(
  base: DegradationConfig,
  custom: Partial<DegradationConfig>
): DegradationConfig {
  return { ...base, ...custom }
}
