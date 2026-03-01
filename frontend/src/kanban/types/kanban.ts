/**
 * Kanban Types - 看板视图 TypeScript 类型定义
 * @module kanban/types
 */

// ============================================
// 卡片相关类型
// ============================================

/**
 * 优先级级别
 */
export type CardPriority = 'low' | 'medium' | 'high' | 'urgent'

/**
 * 卡片状态
 */
export type CardStatus = string

/**
 * 看板卡片
 */
export interface KanbanCard {
  /** 卡片 ID */
  id: string
  /** 卡片标题 */
  title: string
  /** 卡片描述 */
  description?: string
  /** 状态 */
  status: CardStatus
  /** 优先级 */
  priority: CardPriority
  /** 负责人 ID */
  assigneeId?: string
  /** 负责人名称 */
  assigneeName?: string
  /** 负责人头像 */
  assigneeAvatar?: string
  /** 截止日期 */
  dueDate?: Date | string
  /** 标签 */
  tags?: string[]
  /** 自定义字段 */
  customFields?: Record<string, unknown>
  /** 附件数量 */
  attachmentCount?: number
  /** 评论数量 */
  commentCount?: number
  /** 在列中的位置 */
  order: number
  /** 创建时间 */
  createdTime: Date | string
  /** 更新时间 */
  updatedTime?: Date | string
  /** 创建者 */
  createdBy?: string
}

// ============================================
// 列相关类型
// ============================================

/**
 * 看板列配置
 */
export interface KanbanColumn {
  /** 列 ID (状态值) */
  id: string
  /** 列标题 */
  title: string
  /** 列颜色 */
  color?: string
  /** WIP 限制 */
  wipLimit?: number
  /** 是否可拖入卡片 */
  allowDrop?: boolean
  /** 排序顺序 */
  order: number
}

// ============================================
// 看板相关类型
// ============================================

/**
 * 看板配置
 */
export interface KanbanConfig {
  /** 看板 ID */
  id: string
  /** 看板名称 */
  name: string
  /** 描述 */
  description?: string
  /** 列配置 */
  columns: KanbanColumn[]
  /** 数据源配置 */
  dataSource?: KanbanDataSource
  /** 是否启用泳道 */
  enableSwimLanes?: boolean
  /** 泳道分组字段 */
  swimLaneBy?: 'assignee' | 'category' | 'custom'
  /** 自定义泳道字段 */
  customSwimLaneField?: string
}

/**
 * 看板数据源配置
 */
export interface KanbanDataSource {
  /** 数据源类型 */
  type: 'report' | 'sql'
  /** 报表 ID */
  reportId?: string
  /** SQL 语句 */
  sql?: string
  /** 数据库连接 ID */
  connectionId?: string

  /** 字段映射 */
  fieldMapping: {
    /** 卡片ID字段 */
    id: string
    /** 标题字段 */
    title: string
    /** 状态字段 */
    status: string
    /** 优先级字段 (可选) */
    priority?: string
    /** 负责人字段 (可选) */
    assignee?: string
    /** 截止日期字段 (可选) */
    dueDate?: string
    /** 描述字段 (可选) */
    description?: string
  }
}

// ============================================
// 拖拽相关类型
// ============================================

/**
 * 拖拽上下文
 */
export interface DragContext {
  /** 拖拽的卡片 */
  card: KanbanCard
  /** 源列 ID */
  sourceColumnId: string
  /** 源位置索引 */
  sourceIndex: number
}

/**
 * 放置目标
 */
export interface DropTarget {
  /** 目标列 ID */
  columnId: string
  /** 目标位置索引 */
  index: number
}

/**
 * 移动操作请求
 */
export interface MoveCardRequest {
  /** 卡片 ID */
  cardId: string
  /** 源状态 */
  fromStatus: string
  /** 目标状态 */
  toStatus: string
  /** 新位置 */
  newOrder: number
}

// ============================================
// 筛选相关类型
// ============================================

/**
 * 看板筛选条件
 */
export interface KanbanFilters {
  /** 搜索关键词 */
  search?: string
  /** 优先级筛选 */
  priorities?: CardPriority[]
  /** 负责人筛选 */
  assignees?: string[]
  /** 标签筛选 */
  tags?: string[]
  /** 截止日期筛选 */
  dueDateFilter?: 'overdue' | 'today' | 'week' | 'month' | 'none'
}

// ============================================
// 泳道相关类型
// ============================================

/**
 * 泳道配置
 */
export interface SwimLaneConfig {
  /** 分组方式 */
  groupBy: 'assignee' | 'workshop' | 'production_line' | 'custom'
  /** 状态列表 */
  statuses: string[]
  /** 自定义字段名 (groupBy=custom 时) */
  customField?: string
}

/**
 * 泳道组
 */
export interface SwimLaneGroup {
  /** 组 ID */
  id: string
  /** 组名称 */
  name: string
  /** 卡片列表 (按状态分组) */
  cards: Record<string, KanbanCard[]>
}

// ============================================
// 表单相关类型
// ============================================

/**
 * 卡片表单数据
 */
export interface CardFormData {
  id?: string
  title: string
  description?: string
  status: CardStatus
  priority: CardPriority
  assigneeId?: string
  dueDate?: Date | string
  tags?: string[]
  customFields?: Record<string, unknown>
}

// ============================================
// WIP 限制相关
// ============================================

/**
 * WIP 限制状态
 */
export interface WipLimitStatus {
  /** 是否超出限制 */
  isExceeded: boolean
  /** 当前数量 */
  current: number
  /** 限制数量 */
  limit: number
  /** 是否接近限制 (80%+) */
  isNearLimit: boolean
}

// ============================================
// API 响应类型
// ============================================

/**
 * 获取卡片列表响应
 */
export interface GetCardsResponse {
  /** 卡片列表 */
  cards: KanbanCard[]
  /** 总数 */
  total: number
}

/**
 * 移动卡片响应
 */
export interface MoveCardResponse {
  /** 是否成功 */
  success: boolean
  /** 新的卡片顺序 */
  newOrder?: number
  /** 影响的卡片 ID 列表 */
  affectedCards?: string[]
}
