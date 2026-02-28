/**
 * Dashboard Internal Types - 内部类型定义
 * @module dashboard/types/internal
 */

/**
 * Ref 元素类型
 */
export type RefElement = HTMLElement | undefined

/**
 * 组件 Props 基础类型
 */
export interface BaseWidgetProps {
  widgetId: string
  title?: string
  dataSource?: import('./dashboard').DataSourceConfig
  showRefreshButton?: boolean
  refreshInterval?: number
}

/**
 * 配置面板基础类型
 */
export interface BaseConfigProps {
  config: Record<string, unknown>
}

/**
 * 组件实例基础类型
 */
export interface BaseWidgetInstance {
  id: string
  type: string
  name: string
  position: import('./dashboard').GridPosition
  config: Record<string, unknown>
}
