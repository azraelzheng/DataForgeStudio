/**
 * Dashboard 核心类型定义
 *
 * 该文件定义了仪表盘系统的所有核心 TypeScript 接口
 * 用于组件布局、数据绑定、样式规则等功能的类型安全
 */

// ============================================================================
// 组件位置与布局
// ============================================================================

/**
 * 组件位置信息
 * 用于定义组件在网格布局中的位置和尺寸
 */
export interface WidgetPosition {
  /** 组件唯一标识符 */
  id: string
  /** 网格列位置 (0-based) */
  x: number
  /** 网格行位置 (0-based) */
  y: number
  /** 跨列数 */
  width: number
  /** 跨行数 */
  height: number
}

/**
 * 布局配置
 * 定义网格布局的全局配置
 */
export interface LayoutConfig {
  /** 最大列数 (默认: 12) */
  columns: number
  /** 每行像素高度 */
  rowHeight: number
  /** 组件间距 (像素) */
  gap: number
  /** 响应式断点配置 { 断点名称: 最小宽度 } */
  breakpoints: Record<string, number>
}

/**
 * 组件尺寸定义
 */
export interface WidgetSize {
  /** 宽度 (跨列数) */
  width: number
  /** 高度 (跨行数) */
  height: number
}

// ============================================================================
// 组件定义
// ============================================================================

/**
 * 组件类型枚举
 */
export type WidgetType =
  | 'stat-card'      // 统计卡片
  | 'chart'          // 图表
  | 'table'          // 数据表格
  | 'gauge'          // 仪表盘
  | 'map'            // 地图
  | 'text'           // 文本
  | 'image'          // 图片
  | 'iframe'         // 内嵌页面
  | 'custom'         // 自定义组件

/**
 * 组件定义
 * 描述一个可用的组件模板
 */
export interface WidgetDefinition {
  /** 唯一组件类型标识 */
  type: string
  /** 显示名称 */
  name: string
  /** 图标 (Element Plus 图标名称) */
  icon: string
  /** 分类名称 */
  category: string
  /** 默认尺寸 */
  defaultSize: WidgetSize
  /** 最小尺寸 */
  minSize: WidgetSize
  /** 最大尺寸 */
  maxSize: WidgetSize
  /** 配置表单 schema (JSON Schema 格式) */
  configSchema: Record<string, unknown>
  /** 组件描述 */
  description?: string
}

/**
 * 组件实例
 * 仪表盘中实际使用的组件实例
 */
export interface WidgetInstance {
  /** 实例唯一标识 */
  id: string
  /** 组件类型 */
  type: string
  /** 位置信息 */
  position: WidgetPosition
  /** 组件配置 */
  config: Record<string, unknown>
  /** 数据绑定 ID */
  dataSourceId?: string
  /** 字段映射配置 */
  fieldMappings?: Record<string, string>
  /** 条件样式规则 */
  styleRules?: StyleRule[]
  /** 是否可见 */
  visible: boolean
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt: string
}

// ============================================================================
// 数据源与绑定
// ============================================================================

/**
 * 数据源类型
 */
export type DataSourceType = 'report' | 'sql' | 'api'

/**
 * 数据源配置
 */
export interface DataSourceConfig {
  /** 报表 ID (当 type 为 'report' 时) */
  reportId?: string
  /** SQL 查询语句 (当 type 为 'sql' 时) */
  sql?: string
  /** API URL (当 type 为 'api' 时) */
  url?: string
  /** 数据刷新间隔 (毫秒, 0 表示不自动刷新) */
  refreshInterval?: number
  /** 请求参数 */
  params?: Record<string, unknown>
  /** 请求头 */
  headers?: Record<string, string>
}

/**
 * 数据源定义
 */
export interface DataSource {
  /** 数据源唯一标识 */
  id: string
  /** 数据源类型 */
  type: DataSourceType
  /** 数据源名称 */
  name: string
  /** 数据源配置 */
  config: DataSourceConfig
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt: string
}

/**
 * 数据绑定配置
 * 定义组件与数据源之间的映射关系
 */
export interface BindingConfig {
  /** 组件 ID */
  widgetId: string
  /** 数据源 ID */
  dataSourceId: string
  /** 字段映射 { 组件字段: 数据源字段 } */
  fieldMappings: Record<string, string>
  /** 数据转换函数 (JavaScript 表达式) */
  transform?: string
}

// ============================================================================
// 条件样式
// ============================================================================

/**
 * 比较运算符
 */
export type ComparisonOperator =
  | 'lt'      // 小于 (<)
  | 'lte'     // 小于等于 (<=)
  | 'gt'      // 大于 (>)
  | 'gte'     // 大于等于 (>=)
  | 'eq'      // 等于 (==)
  | 'neq'     // 不等于 (!=)
  | 'contains' // 包含
  | 'startsWith' // 开始于
  | 'endsWith'   // 结束于

/**
 * 样式动作类型
 */
export type StyleActionType =
  | 'setColor'   // 设置颜色
  | 'setIcon'    // 设置图标
  | 'showText'   // 显示文本
  | 'setBgColor' // 设置背景色
  | 'setClass'   // 设置 CSS 类

/**
 * 条件样式规则
 * 根据数据值动态改变组件样式
 */
export interface StyleRule {
  /** 规则唯一标识 */
  id?: string
  /** 要比较的字段名 */
  field: string
  /** 比较运算符 */
  operator: ComparisonOperator
  /** 比较值 */
  value: string
  /** 动作类型 */
  actionType: StyleActionType
  /** 动作值 (颜色值、图标名、文本等) */
  actionValue: string
  /** 优先级 (数字越大优先级越高) */
  priority: number
  /** 是否启用 */
  enabled?: boolean
}

// ============================================================================
// 仪表盘配置
// ============================================================================

/**
 * 仪表盘配置
 * 完整的仪表盘定义
 */
export interface DashboardConfig {
  /** 仪表盘唯一标识 */
  id: string
  /** 仪表盘名称 */
  name: string
  /** 仪表盘描述 */
  description?: string
  /** 布局配置 */
  layout: LayoutConfig
  /** 组件实例列表 */
  widgets: WidgetInstance[]
  /** 数据源列表 */
  dataSources: DataSource[]
  /** 全局样式配置 */
  globalStyles?: Record<string, unknown>
  /** 是否启用自动刷新 */
  autoRefresh?: boolean
  /** 全局刷新间隔 (毫秒) */
  refreshInterval?: number
  /** 创建者 ID */
  createdBy?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt: string
}

/**
 * 仪表盘摘要信息
 * 用于列表展示
 */
export interface DashboardSummary {
  /** 仪表盘 ID */
  id: string
  /** 名称 */
  name: string
  /** 描述 */
  description?: string
  /** 组件数量 */
  widgetCount: number
  /** 创建者 */
  createdBy?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt: string
}

// ============================================================================
// 编辑器状态
// ============================================================================

/**
 * 编辑器模式
 */
export type EditorMode = 'view' | 'edit' | 'preview'

/**
 * 编辑器状态
 */
export interface EditorState {
  /** 当前模式 */
  mode: EditorMode
  /** 当前选中的组件 ID */
  selectedWidgetId: string | null
  /** 是否显示组件面板 */
  showWidgetPanel: boolean
  /** 是否显示数据源面板 */
  showDataSourcePanel: boolean
  /** 是否显示属性面板 */
  showPropertyPanel: boolean
  /** 缩放比例 */
  zoom: number
  /** 是否有未保存的更改 */
  isDirty: boolean
  /** 历史记录索引 */
  historyIndex: number
  /** 历史记录栈 */
  historyStack: DashboardConfig[]
}

// ============================================================================
// API 响应类型
// ============================================================================

/**
 * 分页请求参数
 */
export interface PaginationParams {
  /** 当前页码 (1-based) */
  page: number
  /** 每页数量 */
  pageSize: number
  /** 排序字段 */
  sortBy?: string
  /** 排序方向 */
  sortOrder?: 'asc' | 'desc'
}

/**
 * 分页响应
 */
export interface PaginatedResponse<T> {
  /** 数据列表 */
  items: T[]
  /** 总数量 */
  total: number
  /** 当前页码 */
  page: number
  /** 每页数量 */
  pageSize: number
  /** 总页数 */
  totalPages: number
}

// ============================================================================
// 图表配置
// ============================================================================

/**
 * 图表类型
 */
export type ChartType =
  | 'line'     // 折线图
  | 'bar'      // 柱状图
  | 'pie'      // 饼图
  | 'scatter'  // 散点图
  | 'area'     // 面积图
  | 'radar'    // 雷达图
  | 'gauge'    // 仪表盘
  | 'funnel'   // 漏斗图

/**
 * 图表配置
 */
export interface ChartConfig {
  /** 图表类型 */
  chartType: ChartType
  /** 标题 */
  title?: string
  /** X 轴字段 */
  xField?: string
  /** Y 轴字段列表 */
  yFields?: string[]
  /** 系列配置 */
  series?: ChartSeriesConfig[]
  /** 图例配置 */
  legend?: Record<string, unknown>
  /** 坐标轴配置 */
  axis?: Record<string, unknown>
  /** 颜色主题 */
  colorScheme?: string[]
  /** 是否显示工具提示 */
  showTooltip?: boolean
  /** 是否显示数据标签 */
  showDataLabels?: boolean
}

/**
 * 图表系列配置
 */
export interface ChartSeriesConfig {
  /** 系列名称 */
  name: string
  /** 数据字段 */
  field: string
  /** 系列类型 (用于混合图表) */
  type?: ChartType
  /** 系列样式 */
  style?: Record<string, unknown>
}

// ============================================================================
// 统计卡片配置
// ============================================================================

/**
 * 统计卡片配置
 */
export interface StatCardConfig {
  /** 标题 */
  title: string
  /** 数值字段 */
  valueField: string
  /** 前缀 */
  prefix?: string
  /** 后缀 */
  suffix?: string
  /** 数值格式化 (数字格式) */
  format?: string
  /** 图标 */
  icon?: string
  /** 图标颜色 */
  iconColor?: string
  /** 趋势字段 */
  trendField?: string
  /** 趋势类型 */
  trendType?: 'increase' | 'decrease' | 'neutral'
  /** 比较值字段 */
  compareField?: string
}

// ============================================================================
// 表格配置
// ============================================================================

/**
 * 表格列配置
 */
export interface TableColumnConfig {
  /** 列标识 */
  key: string
  /** 列标题 */
  title: string
  /** 数据字段 */
  field: string
  /** 列宽度 */
  width?: number | string
  /** 对齐方式 */
  align?: 'left' | 'center' | 'right'
  /** 是否可排序 */
  sortable?: boolean
  /** 是否可筛选 */
  filterable?: boolean
  /** 格式化函数 */
  formatter?: string
  /** 条件样式规则 */
  styleRules?: StyleRule[]
}

/**
 * 表格溢出处理模式
 */
export type TableOverflowMode = 'auto-page' | 'scroll' | 'none'

/**
 * 表格配置
 */
export interface TableConfig {
  /** 列配置 */
  columns: TableColumnConfig[]
  /** 是否显示边框 */
  bordered?: boolean
  /** 是否显示条纹 */
  striped?: boolean
  /** 是否显示分页 */
  showPagination?: boolean
  /** 每页数量选项 */
  pageSizes?: number[]
  /** 默认每页数量 */
  defaultPageSize?: number
  /** 行高 */
  rowHeight?: number
  /** 是否固定表头 */
  fixedHeader?: boolean
  /** 溢出处理模式 */
  overflowMode?: TableOverflowMode
  /** 自动翻页间隔 (秒) */
  pageInterval?: number
  /** 每页显示条数 */
  pageSize?: number
  /** 滚动速度 (像素/秒) */
  scrollSpeed?: number
}

/**
 * 条件样式规则 (简化版，用于表格)
 */
export interface TableStyleRule {
  /** 要比较的字段名 */
  field: string
  /** 比较运算符 */
  operator: ComparisonOperator
  /** 比较值 */
  value: string
  /** 背景颜色 */
  backgroundColor?: string
  /** 文字颜色 */
  textColor?: string
}

// ============================================================================
// 默认值常量
// ============================================================================

/**
 * 默认布局配置
 */
export const DEFAULT_LAYOUT_CONFIG: LayoutConfig = {
  columns: 12,
  rowHeight: 60,
  gap: 16,
  breakpoints: {
    xs: 0,
    sm: 576,
    md: 768,
    lg: 992,
    xl: 1200,
    xxl: 1400
  }
}

/**
 * 默认编辑器状态
 */
export const DEFAULT_EDITOR_STATE: EditorState = {
  mode: 'view',
  selectedWidgetId: null,
  showWidgetPanel: true,
  showDataSourcePanel: false,
  showPropertyPanel: true,
  zoom: 1,
  isDirty: false,
  historyIndex: -1,
  historyStack: []
}
