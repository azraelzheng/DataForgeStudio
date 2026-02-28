/**
 * Dashboard Types - 看板系统 TypeScript 类型定义
 * @module dashboard/types
 */

// ============================================
// 布局相关类型
// ============================================

/**
 * 网格位置
 */
export interface GridPosition {
  /** X 坐标 (列索引, 0-based) */
  x: number
  /** Y 坐标 (行索引, 0-based) */
  y: number
  /** 宽度 (列数) */
  width: number
  /** 高度 (行数) */
  height: number
}

/**
 * 像素位置
 */
export interface PixelPosition {
  left: number
  top: number
  width: number
  height: number
}

/**
 * 布局配置
 */
export interface LayoutConfig {
  /** 网格列数 */
  columns: number
  /** 行高 (px) */
  rowHeight: number
  /** 间隙 (px) */
  gap: number
  /** 最大行数 */
  maxRows?: number
}

/**
 * 碰撞检测结果
 */
export interface CollisionResult {
  widgetId1: string
  widgetId2: string
  position: GridPosition
}

// ============================================
// 组件相关类型
// ============================================

/**
 * 组件类型枚举
 */
export type WidgetType =
  | 'chart'      // 图表
  | 'card'       // 数字卡片
  | 'table'      // 数据表格
  | 'progress'   // 进度条
  | 'status'     // 状态指示灯
  | 'kanban'     // 看板视图

/**
 * 组件类别
 */
export type WidgetCategory = 'visualization' | 'display' | 'interaction'

/**
 * 组件定义 (注册到 Registry)
 */
export interface WidgetDefinition {
  /** 组件类型标识 */
  type: WidgetType
  /** 显示名称 */
  name: string
  /** 图标名称 (Element Plus) */
  icon: string
  /** 组件类别 */
  category: WidgetCategory
  /** 默认尺寸 */
  defaultSize: GridPosition
  /** 最小尺寸 */
  minSize: GridPosition
  /** 最大尺寸 */
  maxSize: GridPosition
  /** 配置 Schema (JSON Schema) */
  configSchema?: Record<string, unknown>
  /** 动态导入组件 */
  component: () => Promise<unknown>
}

/**
 * 组件实例 (看板中的组件)
 */
export interface WidgetInstance {
  /** 实例 ID */
  id: string
  /** 组件类型 */
  type: WidgetType
  /** 组件名称 */
  name: string
  /** 位置配置 */
  position: GridPosition
  /** 组件配置 */
  config: Record<string, unknown>
  /** 数据绑定配置 */
  dataBinding?: DataBindingConfig
  /** 显示顺序 */
  displayOrder: number
  /** 看板 ID */
  dashboardId?: string
}

// ============================================
// 数据绑定相关类型
// ============================================

/**
 * 数据源类型
 */
export type DataSourceType = 'report' | 'sql'

/**
 * 数据源配置
 */
export interface DataSourceConfig {
  /** 数据源类型 */
  type: DataSourceType
  /** 报表 ID (type=report 时) */
  reportId?: string
  /** SQL 语句 (type=sql 时) */
  sql?: string
  /** 数据库连接 ID */
  connectionId?: string
}

/**
 * 数据绑定配置
 */
export interface DataBindingConfig {
  /** 数据源 ID */
  sourceId: string
  /** 字段映射 */
  fieldMapping: Record<string, string>
  /** 刷新间隔 (秒) */
  refreshInterval?: number
}

/**
 * 绑定状态
 */
export interface BindingState {
  isLoading: boolean
  lastRefresh: Date | null
  error: Error | null
  data: unknown
}

// ============================================
// 看板相关类型
// ============================================

/**
 * 看板配置
 */
export interface DashboardConfig {
  /** 看板 ID */
  id: string
  /** 看板名称 */
  name: string
  /** 描述 */
  description?: string
  /** 分类 */
  category?: string
  /** 布局配置 */
  layout: LayoutConfig
  /** 数据源配置 (标准版绑定报表) */
  dataSource?: DataSourceConfig
  /** 独立 SQL (专业版) */
  sqlStatement?: string
  /** 数据刷新间隔 (秒) */
  refreshInterval: number
  /** 是否发布 */
  isPublished: boolean
  /** 创建者 ID */
  createdBy: number
  /** 创建时间 */
  createdTime: Date
  /** 更新时间 */
  updatedTime?: Date
}

/**
 * 看板完整数据 (含组件)
 */
export interface Dashboard extends DashboardConfig {
  /** 组件列表 */
  widgets: WidgetInstance[]
}

/**
 * 显示模式
 */
export type DisplayMode = 'edit' | 'preview' | 'fullscreen'

// ============================================
// 状态管理相关类型
// ============================================

/**
 * 看板状态
 */
export interface DashboardState {
  /** 当前看板 */
  currentDashboard: Dashboard | null
  /** 组件映射 */
  widgets: Map<string, WidgetInstance>
  /** 选中的组件 ID */
  selectedWidgetId: string | null
  /** 是否编辑模式 */
  isEditing: boolean
  /** 显示模式 */
  displayMode: DisplayMode
  /** 是否加载中 */
  isLoading: boolean
  /** 错误信息 */
  error: string | null
}

// ============================================
// API 响应类型
// ============================================

/**
 * API 响应
 */
export interface ApiResponse<T> {
  success: boolean
  message: string
  data: T
  errorCode: string | null
  timestamp: number
}

/**
 * 分页请求
 */
export interface PaginationParams {
  page: number
  pageSize: number
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

/**
 * 分页响应
 */
export interface PaginatedResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}
