/**
 * useResponsiveFont - 响应式字体 Composable
 * 基于人机工程学 1-3-7 米规则计算字体大小
 * @module display/composables/useResponsiveFont
 */

import { computed, ref, watch, isRef, type Ref } from 'vue'

export type ViewingDistance = 'near' | 'medium' | 'far'

export interface ResponsiveFontOptions {
  /** 观看距离 */
  distance?: ViewingDistance | Ref<ViewingDistance>
  /** 基准字号（近距时的字号） */
  baseFontSize?: number
  /** 设计稿宽度 */
  designWidth?: number
}

/**
 * 响应式字体 Hook
 * 基于人机工程学 1-3-7 米规则计算字体大小
 *
 * 规则说明：
 * - 近距离 (1-3米): 个人控制台，最小字符高度 4-6mm
 * - 中距离 (3-7米): 会议室看板，最小字符高度 6-12mm
 * - 远距离 (7米+): 展厅/指挥中心，最小字符高度 12mm+
 *
 * 经验公式：每1米观看距离对应1厘米字体高度
 */
export function useResponsiveFont(
  options: ResponsiveFontOptions = {},
  viewportWidth?: Ref<number>
) {
  const {
    distance = 'medium',
    baseFontSize = 16,
    designWidth = 1920
  } = options

  // 支持响应式 distance
  const distanceRef = isRef(distance) ? distance : ref<ViewingDistance>(distance)

  // 距离对应的字号缩放因子
  const distanceScale: Record<ViewingDistance, number> = {
    near: 1,      // 基准
    medium: 1.5,  // 1.5倍
    far: 2.25     // 2.25倍
  }

  // 计算视口缩放比例
  const viewportScale = computed(() => {
    if (!viewportWidth?.value) return 1
    const width = designWidth || 1920
    if (width <= 0) return 1 // 防止除零
    return Math.min(viewportWidth.value / width, 1)
  })

  // 计算最终字号（响应式 distance）
  const scale = computed(() => {
    return distanceScale[distanceRef.value] * viewportScale.value
  })

  // 字体大小预设
  const fontSize = computed(() => ({
    // 基础文本
    xs: `${Math.round(baseFontSize * 0.75 * scale.value)}px`,
    sm: `${Math.round(baseFontSize * 0.875 * scale.value)}px`,
    base: `${Math.round(baseFontSize * scale.value)}px`,
    lg: `${Math.round(baseFontSize * 1.125 * scale.value)}px`,
    xl: `${Math.round(baseFontSize * 1.25 * scale.value)}px`,

    // 标题
    '2xl': `${Math.round(baseFontSize * 1.5 * scale.value)}px`,
    '3xl': `${Math.round(baseFontSize * 1.875 * scale.value)}px`,
    '4xl': `${Math.round(baseFontSize * 2.25 * scale.value)}px`,
    '5xl': `${Math.round(baseFontSize * 3 * scale.value)}px`,

    // 大屏专用
    kpi: `${Math.round(baseFontSize * 3.5 * scale.value)}px`,      // KPI 数字
    kpiLabel: `${Math.round(baseFontSize * 1.25 * scale.value)}px`, // KPI 标签
    title: `${Math.round(baseFontSize * 2 * scale.value)}px`,       // 组件标题
    subtitle: `${Math.round(baseFontSize * 1.5 * scale.value)}px`   // 副标题
  }))

  // 行高预设
  const lineHeight = computed(() => ({
    tight: 1.25 * scale.value,
    normal: 1.5 * scale.value,
    relaxed: 1.75 * scale.value
  }))

  /**
   * 获取特定用途的字体样式
   */
  function getFontStyle(purpose: 'kpi' | 'label' | 'title' | 'table' | 'chart') {
    const styles: Record<string, Record<string, string>> = {
      kpi: {
        fontSize: fontSize.value.kpi,
        fontWeight: 'bold',
        lineHeight: String(lineHeight.value.tight)
      },
      label: {
        fontSize: fontSize.value.kpiLabel,
        fontWeight: '500',
        lineHeight: String(lineHeight.value.normal)
      },
      title: {
        fontSize: fontSize.value.title,
        fontWeight: '600',
        lineHeight: String(lineHeight.value.tight)
      },
      table: {
        fontSize: fontSize.value.sm,
        fontWeight: 'normal',
        lineHeight: String(lineHeight.value.normal)
      },
      chart: {
        fontSize: fontSize.value.base,
        fontWeight: 'normal',
        lineHeight: String(lineHeight.value.normal)
      }
    }

    return styles[purpose]
  }

  return {
    fontSize,
    lineHeight,
    scale,
    getFontStyle
  }
}

/**
 * 基于物理距离自动选择字体大小的 Hook
 */
export function useAutoDistanceFont(
  containerRef: Ref<HTMLElement | undefined>,
  options: Omit<ResponsiveFontOptions, 'distance'> = {}
) {
  const distance = ref<ViewingDistance>('medium')

  // 简单的距离估算（基于容器宽度）
  watch(containerRef, (el) => {
    if (!el) return

    const width = el.clientWidth
    // 基于容器宽度估算观看距离
    if (width >= 3840) {
      distance.value = 'far'
    } else if (width >= 2560) {
      distance.value = 'medium'
    } else {
      distance.value = 'near'
    }
  })

  const responsiveFont = useResponsiveFont({ ...options, distance: distance.value })

  return {
    ...responsiveFont,
    distance
  }
}
