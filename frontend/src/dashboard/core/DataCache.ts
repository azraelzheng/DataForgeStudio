/**
 * DataCache - 数据缓存管理器
 * 支持 TTL 过期、请求去重、LRU 淘汰策略
 * @module dashboard/core/DataCache
 */

/**
 * 缓存条目
 */
interface CacheEntry<T> {
  /** 缓存数据 */
  data: T
  /** 缓存时间戳 */
  timestamp: number
  /** 生存时间（毫秒） */
  ttl: number
}

/**
 * 待处理请求
 */
interface PendingRequest<T> {
  /** 请求 Promise */
  promise: Promise<T>
  /** 请求时间戳 */
  timestamp: number
}

/**
 * 缓存配置
 */
export interface DataCacheOptions {
  /** 最大缓存数量 */
  maxSize?: number
  /** 默认 TTL（毫秒） */
  defaultTtl?: number
  /** 待处理请求超时时间（毫秒） */
  pendingTimeout?: number
}

/**
 * 缓存统计信息
 */
export interface CacheStats {
  /** 当前缓存数量 */
  size: number
  /** 待处理请求数量 */
  pendingCount: number
  /** 所有缓存键 */
  keys: string[]
  /** 命中次数 */
  hits: number
  /** 未命中次数 */
  misses: number
}

/**
 * 数据缓存管理器
 * 支持 TTL、请求去重、LRU 淘汰
 *
 * @example
 * ```typescript
 * const cache = new DataCache<{ id: number; name: string }>()
 *
 * // 获取数据（自动缓存）
 * const data = await cache.fetch(
 *   'user:123',
 *   () => fetchUser(123),
 *   60000 // 1 分钟 TTL
 * )
 *
 * // 直接设置缓存
 * cache.set('user:456', { id: 456, name: 'Test' }, 30000)
 *
 * // 获取缓存统计
 * const stats = cache.getStats()
 * console.log(`命中率: ${stats.hits / (stats.hits + stats.misses)}`)
 * ```
 */
export class DataCache<T = unknown> {
  /** 缓存存储 */
  private cache = new Map<string, CacheEntry<T>>()
  /** 待处理请求存储 */
  private pending = new Map<string, PendingRequest<T>>()
  /** LRU 访问顺序 */
  private accessOrder: string[] = []
  /** 最大缓存数量 */
  private readonly maxSize: number
  /** 默认 TTL */
  private readonly defaultTtl: number
  /** 待处理请求超时时间 */
  private readonly pendingTimeout: number
  /** 命中次数 */
  private hits = 0
  /** 未命中次数 */
  private misses = 0

  constructor(options: DataCacheOptions = {}) {
    this.maxSize = options.maxSize ?? 100
    this.defaultTtl = options.defaultTtl ?? 60000 // 默认 1 分钟
    this.pendingTimeout = options.pendingTimeout ?? 30000 // 默认 30 秒
  }

  /**
   * 获取缓存数据或执行请求
   * 自动处理缓存命中、请求去重
   *
   * @param key - 缓存键
   * @param fetcher - 数据获取函数
   * @param ttl - 生存时间（毫秒），默认使用 defaultTtl
   * @returns 数据
   */
  async fetch(
    key: string,
    fetcher: () => Promise<T>,
    ttl?: number
  ): Promise<T> {
    const actualTtl = ttl ?? this.defaultTtl

    // 1. 检查缓存是否有效
    const cached = this.cache.get(key)
    if (cached && this.isValid(cached)) {
      this.hits++
      this.updateAccessOrder(key)
      return cached.data
    }

    // 2. 检查是否有相同的请求正在进行（去重）
    const pendingRequest = this.pending.get(key)
    if (pendingRequest && Date.now() - pendingRequest.timestamp < this.pendingTimeout) {
      this.hits++
      return pendingRequest.promise
    }

    // 3. 未命中
    this.misses++

    // 4. 发起新请求
    const promise = fetcher()
      .then(data => {
        this.set(key, data, actualTtl)
        this.pending.delete(key)
        return data
      })
      .catch(err => {
        this.pending.delete(key)
        throw err
      })

    this.pending.set(key, { promise, timestamp: Date.now() })
    return promise
  }

  /**
   * 设置缓存
   *
   * @param key - 缓存键
   * @param data - 缓存数据
   * @param ttl - 生存时间（毫秒），默认使用 defaultTtl
   */
  set(key: string, data: T, ttl?: number): void {
    const actualTtl = ttl ?? this.defaultTtl

    // LRU 淘汰：如果缓存已满且不是更新现有键
    if (this.cache.size >= this.maxSize && !this.cache.has(key)) {
      this.evictOldest()
    }

    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl: actualTtl
    })
    this.updateAccessOrder(key)
  }

  /**
   * 获取缓存（不触发请求）
   *
   * @param key - 缓存键
   * @returns 缓存数据，如果不存在或已过期则返回 undefined
   */
  get(key: string): T | undefined {
    const entry = this.cache.get(key)
    if (!entry) {
      this.misses++
      return undefined
    }

    if (!this.isValid(entry)) {
      this.cache.delete(key)
      this.removeFromAccessOrder(key)
      this.misses++
      return undefined
    }

    this.hits++
    this.updateAccessOrder(key)
    return entry.data
  }

  /**
   * 检查缓存是否存在且有效
   *
   * @param key - 缓存键
   * @returns 是否存在有效缓存
   */
  has(key: string): boolean {
    const entry = this.cache.get(key)
    if (!entry) return false

    if (!this.isValid(entry)) {
      this.cache.delete(key)
      this.removeFromAccessOrder(key)
      return false
    }

    return true
  }

  /**
   * 清除指定缓存或全部缓存
   *
   * @param key - 可选，指定要清除的缓存键
   */
  clear(key?: string): void {
    if (key) {
      this.cache.delete(key)
      this.removeFromAccessOrder(key)
      this.pending.delete(key)
    } else {
      this.cache.clear()
      this.pending.clear()
      this.accessOrder = []
    }
  }

  /**
   * 清除过期缓存
   *
   * @returns 清除的缓存数量
   */
  clearExpired(): number {
    let cleared = 0
    const now = Date.now()

    this.cache.forEach((entry, key) => {
      if (now - entry.timestamp >= entry.ttl) {
        this.cache.delete(key)
        this.removeFromAccessOrder(key)
        cleared++
      }
    })

    return cleared
  }

  /**
   * 获取缓存统计信息
   *
   * @returns 缓存统计
   */
  getStats(): CacheStats {
    return {
      size: this.cache.size,
      pendingCount: this.pending.size,
      keys: Array.from(this.cache.keys()),
      hits: this.hits,
      misses: this.misses
    }
  }

  /**
   * 获取缓存命中率
   *
   * @returns 命中率（0-1）
   */
  getHitRate(): number {
    const total = this.hits + this.misses
    return total === 0 ? 0 : this.hits / total
  }

  /**
   * 重置统计信息
   */
  resetStats(): void {
    this.hits = 0
    this.misses = 0
  }

  /**
   * 检查缓存条目是否有效
   */
  private isValid(entry: CacheEntry<T>): boolean {
    return Date.now() - entry.timestamp < entry.ttl
  }

  /**
   * 更新访问顺序（LRU）
   */
  private updateAccessOrder(key: string): void {
    this.removeFromAccessOrder(key)
    this.accessOrder.push(key)
  }

  /**
   * 从访问顺序中移除
   */
  private removeFromAccessOrder(key: string): void {
    const index = this.accessOrder.indexOf(key)
    if (index !== -1) {
      this.accessOrder.splice(index, 1)
    }
  }

  /**
   * 淘汰最久未使用的缓存
   */
  private evictOldest(): void {
    const oldest = this.accessOrder.shift()
    if (oldest) {
      this.cache.delete(oldest)
    }
  }
}

/**
 * 全局数据缓存实例
 * 默认配置：最大 100 条，TTL 60 秒
 */
export const globalDataCache = new DataCache({
  maxSize: 100,
  defaultTtl: 60000,
  pendingTimeout: 30000
})

/**
 * 创建专用的数据缓存实例
 *
 * @param options - 缓存配置
 * @returns 数据缓存实例
 */
export function createDataCache<T>(options?: DataCacheOptions): DataCache<T> {
  return new DataCache<T>(options)
}
