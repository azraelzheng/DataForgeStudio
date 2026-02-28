/**
 * Display Mode Types - 车间大屏显示模式 TypeScript 类型定义
 * @module display/types
 */

// ============================================
// 大屏配置类型
// ============================================

/**
 * 转场效果类型
 */
export type TransitionType = 'fade' | 'slide' | 'none'

/**
 * 大屏配置
 */
export interface DisplayConfig {
  /** 配置 ID */
  id: string
  /** 配置名称 */
  name: string
  /** 描述 */
  description?: string
  /** 看板 ID 列表 */
  dashboardIds: string[]
  /** 轮播间隔（秒） */
  interval: number
  /** 数据刷新间隔（秒） */
  autoRefresh: number
  /** 转场效果 */
  transition: TransitionType
  /** 是否显示时钟 */
  showClock: boolean
  /** 是否显示看板名称 */
  showDashboardName: boolean
  /** 是否循环播放 */
  loop: boolean
  /** 是否悬停暂停 */
  pauseOnHover: boolean
  /** 创建者 ID */
  createdBy: number
  /** 创建时间 */
  createdTime: Date
  /** 更新时间 */
  updatedTime?: Date
}

/**
 * 大屏配置创建请求
 */
export interface DisplayConfigCreateRequest {
  name: string
  description?: string
  dashboardIds: string[]
  interval?: number
  autoRefresh?: number
  transition?: TransitionType
  showClock?: boolean
  showDashboardName?: boolean
  loop?: boolean
  pauseOnHover?: boolean
}

/**
 * 大屏配置更新请求
 */
export interface DisplayConfigUpdateRequest {
  id: string
  name?: string
  description?: string
  dashboardIds?: string[]
  interval?: number
  autoRefresh?: number
  transition?: TransitionType
  showClock?: boolean
  showDashboardName?: boolean
  loop?: boolean
  pauseOnHover?: boolean
}

// ============================================
// 轮播相关类型
// ============================================

/**
 * 轮播状态
 */
export interface CarouselState {
  /** 当前索引 */
  currentIndex: number
  /** 是否转场中 */
  isTransitioning: boolean
  /** 是否暂停 */
  isPaused: boolean
  /** 是否循环 */
  loop: boolean
}

/**
 * 轮播选项
 */
export interface CarouselOptions {
  /** 是否循环 */
  loop?: boolean
  /** 是否悬停暂停 */
  pauseOnHover?: boolean
  /** 转场时长（毫秒） */
  transitionDuration?: number
}

// ============================================
// 全屏相关类型
// ============================================

/**
 * 全屏状态
 */
export interface FullscreenState {
  /** 是否全屏 */
  isFullscreen: boolean
  /** 全屏元素 */
  element: HTMLElement | null
}

/**
 * 全屏选项
 */
export interface FullscreenOptions {
  /** 退出回调 */
  onExit?: () => void
  /** 进入回调 */
  onEnter?: () => void
}

// ============================================
// 自动刷新相关类型
// ============================================

/**
 * 刷新状态
 */
export interface RefreshState {
  /** 是否加载中 */
  isLoading: boolean
  /** 最后刷新时间 */
  lastRefresh: Date | null
  /** 错误信息 */
  error: Error | null
}

/**
 * 刷新选项
 */
export interface RefreshOptions {
  /** 是否立即执行 */
  immediate?: boolean
  /** 是否错误重试 */
  retryOnError?: boolean
  /** 最大重试次数 */
  maxRetries?: number
}

// ============================================
// 许可证相关类型
// ============================================

/**
 * 许可证状态
 */
export interface LicenseStatus {
  /** 是否有效 */
  isValid: boolean
  /** 是否过期 */
  isExpired: boolean
  /** 到期日期 */
  expiryDate: Date | null
  /** 功能列表 */
  features: string[]
  /** 错误信息 */
  error?: string
}

// ============================================
// API 响应类型
// ============================================

/**
 * 大屏配置列表响应
 */
export interface DisplayConfigListResponse {
  items: DisplayConfig[]
  total: number
}

/**
 * 大屏数据响应（聚合多个看板数据）
 */
export interface DisplayDataResponse {
  /** 看板数据映射 */
  dashboards: Record<string, {
    id: string
    name: string
    data: unknown
  }>
  /** 刷新时间 */
  timestamp: number
}
