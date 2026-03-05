/**
 * Dashboard Widgets - 组件注册
 * 导出所有组件和组件定义，用于注册到 ComponentRegistry
 * @module dashboard/widgets
 */

import { componentRegistry } from '../core/ComponentRegistry'
import type { WidgetDefinitionInternal } from '../core/ComponentRegistry'
import type { WidgetType, WidgetCategory, GridPosition } from '../types/dashboard'

// ============================================
// 组件导入
// ============================================

// 图表组件
export { default as ChartWidget } from './ChartWidget.vue'
export type { ChartType, ColorScheme } from './ChartWidget.vue'

// 数字卡片组件
export { default as NumberCardWidget } from './NumberCardWidget.vue'
export type { ColorScheme as NumberCardColorScheme } from './NumberCardWidget.vue'

// 数据表格组件
export { default as DataTableWidget } from './DataTableWidget.vue'
export type { TableColumnConfig, SummaryType } from './DataTableWidget.vue'

// 进度条组件
export { default as ProgressBarWidget } from './ProgressBarWidget.vue'
export type { ProgressType, ProgressColorScheme, ThresholdConfig } from './ProgressBarWidget.vue'

// 状态指示灯组件
export { default as StatusIndicatorWidget } from './StatusIndicatorWidget.vue'
export type { StatusMapping, StatusLayout, StatusMappingItem } from './StatusIndicatorWidget.vue'

// 文本组件
export { default as TextWidget } from './TextWidget.vue'
export type { TextAlign, TextDecoration } from './TextWidget.vue'

// 图片组件
export { default as ImageWidget } from './ImageWidget.vue'
export type { ObjectFit } from './ImageWidget.vue'

// 组件包装器
export { default as WidgetWrapper } from './WidgetWrapper.vue'

// 配置面板
export { default as ChartConfigPanel } from './config/ChartConfigPanel.vue'
export type { ChartConfigData } from './config/ChartConfigPanel.vue'

export { default as NumberCardConfig } from './config/NumberCardConfig.vue'
export type { NumberCardConfigData } from './config/NumberCardConfig.vue'

export { default as TableConfig } from './config/TableConfig.vue'
export type { TableConfigData } from './config/TableConfig.vue'

export { default as TextConfig } from './config/TextConfig.vue'
export type { TextConfigData } from './config/TextConfig.vue'

export { default as ImageConfig } from './config/ImageConfig.vue'
export type { ImageConfigData } from './config/ImageConfig.vue'

// ============================================
// 组件定义
// ============================================

/**
 * 图表组件定义
 */
export const chartWidgetDefinition: WidgetDefinitionInternal = {
  type: 'chart' as WidgetType,
  name: '图表',
  icon: 'TrendCharts',
  category: 'visualization' as WidgetCategory,
  defaultSize: { x: 0, y: 0, width: 6, height: 4 } as GridPosition,
  minSize: { x: 0, y: 0, width: 4, height: 3 } as GridPosition,
  maxSize: { x: 0, y: 0, width: 12, height: 8 } as GridPosition,
  component: () => import('./ChartWidget.vue')
}

/**
 * 数字卡片组件定义
 */
export const numberCardWidgetDefinition: WidgetDefinitionInternal = {
  type: 'card' as WidgetType,
  name: '数字卡片',
  icon: 'DataLine',
  category: 'display' as WidgetCategory,
  defaultSize: { x: 0, y: 0, width: 3, height: 2 } as GridPosition,
  minSize: { x: 0, y: 0, width: 2, height: 2 } as GridPosition,
  maxSize: { x: 0, y: 0, width: 6, height: 4 } as GridPosition,
  component: () => import('./NumberCardWidget.vue')
}

/**
 * 数据表格组件定义
 */
export const dataTableWidgetDefinition: WidgetDefinitionInternal = {
  type: 'table' as WidgetType,
  name: '数据表格',
  icon: 'Grid',
  category: 'display' as WidgetCategory,
  defaultSize: { x: 0, y: 0, width: 8, height: 5 } as GridPosition,
  minSize: { x: 0, y: 0, width: 4, height: 3 } as GridPosition,
  maxSize: { x: 0, y: 0, width: 12, height: 10 } as GridPosition,
  component: () => import('./DataTableWidget.vue')
}

/**
 * 进度条组件定义
 */
export const progressBarWidgetDefinition: WidgetDefinitionInternal = {
  type: 'progress' as WidgetType,
  name: '进度条',
  icon: 'Histogram',
  category: 'display' as WidgetCategory,
  defaultSize: { x: 0, y: 0, width: 4, height: 2 } as GridPosition,
  minSize: { x: 0, y: 0, width: 2, height: 2 } as GridPosition,
  maxSize: { x: 0, y: 0, width: 12, height: 4 } as GridPosition,
  component: () => import('./ProgressBarWidget.vue')
}

/**
 * 状态指示灯组件定义
 */
export const statusIndicatorWidgetDefinition: WidgetDefinitionInternal = {
  type: 'status' as WidgetType,
  name: '状态指示灯',
  icon: 'CircleCheck',
  category: 'display' as WidgetCategory,
  defaultSize: { x: 0, y: 0, width: 4, height: 3 } as GridPosition,
  minSize: { x: 0, y: 0, width: 2, height: 2 } as GridPosition,
  maxSize: { x: 0, y: 0, width: 12, height: 6 } as GridPosition,
  component: () => import('./StatusIndicatorWidget.vue')
}

/**
 * 文本组件定义
 */
export const textWidgetDefinition: WidgetDefinitionInternal = {
  type: 'text' as WidgetType,
  name: '文本',
  icon: 'Document',
  category: 'display' as WidgetCategory,
  defaultSize: { x: 0, y: 0, width: 4, height: 3 } as GridPosition,
  minSize: { x: 0, y: 0, width: 2, height: 2 } as GridPosition,
  maxSize: { x: 0, y: 0, width: 12, height: 8 } as GridPosition,
  component: () => import('./TextWidget.vue')
}

/**
 * 图片组件定义
 */
export const imageWidgetDefinition: WidgetDefinitionInternal = {
  type: 'image' as WidgetType,
  name: '图片',
  icon: 'Picture',
  category: 'display' as WidgetCategory,
  defaultSize: { x: 0, y: 0, width: 4, height: 4 } as GridPosition,
  minSize: { x: 0, y: 0, width: 2, height: 2 } as GridPosition,
  maxSize: { x: 0, y: 0, width: 12, height: 10 } as GridPosition,
  component: () => import('./ImageWidget.vue')
}

/**
 * 所有组件定义列表
 */
export const allWidgetDefinitions: WidgetDefinitionInternal[] = [
  chartWidgetDefinition,
  numberCardWidgetDefinition,
  dataTableWidgetDefinition,
  progressBarWidgetDefinition,
  statusIndicatorWidgetDefinition,
  textWidgetDefinition,
  imageWidgetDefinition
]

// ============================================
// 注册函数
// ============================================

/**
 * 注册所有组件到 ComponentRegistry
 *
 * @example
 * ```typescript
 * import { registerAllWidgets } from '@/dashboard/widgets'
 *
 * // 在应用初始化时调用
 * registerAllWidgets()
 * ```
 */
export function registerAllWidgets(): void {
  componentRegistry.registerAll(allWidgetDefinitions)
  console.log('[Dashboard Widgets] 已注册所有组件:', {
    count: componentRegistry.size,
    types: componentRegistry.getTypes()
  })
}

/**
 * 注册单个组件
 *
 * @param definition 组件定义
 */
export function registerWidget(definition: WidgetDefinitionInternal): void {
  componentRegistry.register(definition)
}

/**
 * 获取组件定义
 *
 * @param type 组件类型
 */
export function getWidgetDefinition(type: WidgetType): WidgetDefinitionInternal | undefined {
  return componentRegistry.get(type)
}

/**
 * 获取所有组件定义
 */
export function getAllWidgetDefinitions(): WidgetDefinitionInternal[] {
  return componentRegistry.getAll()
}

/**
 * 按类别获取组件定义
 *
 * @param category 组件类别
 */
export function getWidgetsByCategory(category: WidgetCategory): WidgetDefinitionInternal[] {
  return componentRegistry.getByCategory(category)
}

// 自动注册（开发环境）
if (import.meta.env.DEV) {
  registerAllWidgets()
}
