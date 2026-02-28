/**
 * Dashboard Core Module - 看板核心模块导出
 * @module dashboard/core
 */

// 布局引擎
export { LayoutEngine, layoutEngine } from './LayoutEngine'

// 组件注册中心
export { ComponentRegistry, componentRegistry } from './ComponentRegistry'
export type { WidgetDefinitionInternal } from './ComponentRegistry'

// 数据绑定系统
export { DataBinder, dataBinder } from './DataBinder'

// 状态管理
export { useDashboardStore } from './StateStore'
export type { DashboardStore } from './StateStore'
