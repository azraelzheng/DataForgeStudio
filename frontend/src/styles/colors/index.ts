// =============================================================================
// 统一色彩字典 - Unified Color Dictionary
// =============================================================================
// 遵循大屏设计规范：色相应被视为功能语言而非装饰
// 全屏色相不超过6-8种，建立语义化色彩字典
// =============================================================================

/**
 * 语义化状态颜色
 * 用于表示数据状态、操作结果等
 */
export const semanticColors = {
  /** 成功/健康/达成/正常 */
  success: '#22c55e',
  /** 警告/待观察/风险/注意 */
  warning: '#f59e0b',
  /** 危险/警告/异常/错误 */
  danger: '#ef4444',
  /** 信息/中性/默认 */
  info: '#3b82f6',
  /** 主色/品牌色 */
  primary: '#3b82f6',
  /** 次要/辅助色 */
  secondary: '#6b7280'
} as const

/**
 * 图表专用色板
 * 限制为6-8色，符合大屏设计规范
 */
export const chartColors = {
  /**
   * 主色板（6色）
   * - 蓝色: 主数据/核心指标
   * - 绿色: 正向数据/健康状态
   * - 橙色: 警告数据/需关注
   * - 红色: 负向数据/异常状态
   * - 紫色: 辅助数据/对比指标
   * - 青色: 对比数据/参考值
   */
  primary: [
    '#3b82f6', // 蓝色 - 主数据
    '#22c55e', // 绿色 - 正向数据
    '#f59e0b', // 橙色 - 警告数据
    '#ef4444', // 红色 - 负向数据
    '#8b5cf6', // 紫色 - 辅助数据
    '#06b6d4'  // 青色 - 对比数据
  ],

  /**
   * 扩展色板（8色）
   * 用于需要更多颜色区分的场景
   */
  extended: [
    '#3b82f6', // 蓝色
    '#22c55e', // 绿色
    '#f59e0b', // 橙色
    '#ef4444', // 红色
    '#8b5cf6', // 紫色
    '#06b6d4', // 青色
    '#ec4899', // 粉色 - 特殊标记
    '#14b8a6'  // 蓝绿色 - 补充对比
  ]
} as const

/**
 * 颜色类型定义
 */
export type SemanticColorName = keyof typeof semanticColors
export type ChartColorPalette = keyof typeof chartColors

/**
 * 根据数值和阈值获取对应的状态颜色
 * @param value - 当前数值
 * @param thresholds - 阈值配置 { warning: 警告阈值, danger: 危险阈值 }
 * @returns 对应的状态颜色
 *
 * @example
 * // 获取进度状态颜色
 * const color = getStatusColor(85, { warning: 60, danger: 30 })
 * // 返回 '#22c55e' (success)，因为 85 >= 60
 *
 * @example
 * // 获取错误率状态颜色（反向阈值）
 * const color = getStatusColor(15, { warning: 10, danger: 20 })
 * // 返回 '#f59e0b' (warning)，因为 15 >= 10 且 < 20
 */
export function getStatusColor(
  value: number,
  thresholds: { warning: number; danger: number }
): string {
  if (value >= thresholds.danger) return semanticColors.danger
  if (value >= thresholds.warning) return semanticColors.warning
  return semanticColors.success
}

/**
 * 获取指定索引的图表颜色
 * @param index - 颜色索引（从0开始）
 * @param extended - 是否使用扩展色板，默认使用主色板
 * @returns 对应的图表颜色
 */
export function getChartColor(index: number, extended: boolean = false): string {
  const palette = extended ? chartColors.extended : chartColors.primary
  return palette[index % palette.length]
}

/**
 * 获取图表色板数组
 * @param extended - 是否使用扩展色板（8色），默认使用主色板（6色）
 * @returns 图表色板数组
 */
export function getChartPalette(extended: boolean = false): readonly string[] {
  return extended ? chartColors.extended : chartColors.primary
}

// 默认导出
export default {
  semanticColors,
  chartColors,
  getStatusColor,
  getChartColor,
  getChartPalette
}
