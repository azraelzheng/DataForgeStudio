/**
 * ComponentRegistry - 组件注册中心
 * 负责管理所有可用的看板组件类型
 * @module dashboard/core/ComponentRegistry
 */

import type {
  WidgetType,
  WidgetCategory,
  WidgetDefinition,
  GridPosition
} from '../types/dashboard'

/**
 * 组件定义接口（扩展版，包含 Vue 组件）
 */
export interface WidgetDefinitionInternal extends Omit<WidgetDefinition, 'component'> {
  /** 动态导入组件 */
  component: () => Promise<{ default: unknown }>
}

/**
 * 组件注册中心
 *
 * @example
 * ```typescript
 * const registry = new ComponentRegistry()
 *
 * // 注册组件
 * registry.register({
 *   type: 'chart',
 *   name: '图表组件',
 *   icon: 'TrendCharts',
 *   category: 'visualization',
 *   defaultSize: { x: 0, y: 0, width: 4, height: 3 },
 *   minSize: { x: 0, y: 0, width: 2, height: 2 },
 *   maxSize: { x: 0, y: 0, width: 12, height: 8 },
 *   component: () => import('../widgets/ChartWidget.vue')
 * })
 *
 * // 获取组件
 * const def = registry.get('chart')
 *
 * // 按类别获取
 * const vizWidgets = registry.getByCategory('visualization')
 * ```
 */
export class ComponentRegistry {
  private definitions: Map<WidgetType, WidgetDefinitionInternal> = new Map()

  /**
   * 注册组件
   *
   * @param definition - 组件定义
   * @throws 如果组件类型已存在
   */
  register(definition: WidgetDefinitionInternal): void {
    if (this.definitions.has(definition.type)) {
      console.warn(`[ComponentRegistry] 组件类型 "${definition.type}" 已存在，将被覆盖`)
    }

    // 验证必要字段
    if (!definition.type || !definition.name || !definition.component) {
      throw new Error('[ComponentRegistry] 组件定义缺少必要字段')
    }

    // 设置默认尺寸限制
    const defaultSize = definition.defaultSize || { x: 0, y: 0, width: 4, height: 3 }
    const minSize = definition.minSize || { x: 0, y: 0, width: 1, height: 1 }
    const maxSize = definition.maxSize || { x: 0, y: 0, width: 12, height: 10 }

    this.definitions.set(definition.type, {
      ...definition,
      defaultSize,
      minSize,
      maxSize
    })
  }

  /**
   * 批量注册组件
   *
   * @param definitions - 组件定义数组
   */
  registerAll(definitions: WidgetDefinitionInternal[]): void {
    definitions.forEach(def => this.register(def))
  }

  /**
   * 获取组件定义
   *
   * @param type - 组件类型
   * @returns 组件定义，不存在则返回 undefined
   */
  get(type: WidgetType): WidgetDefinitionInternal | undefined {
    return this.definitions.get(type)
  }

  /**
   * 检查组件类型是否存在
   *
   * @param type - 组件类型
   */
  has(type: WidgetType): boolean {
    return this.definitions.has(type)
  }

  /**
   * 按类别获取组件
   *
   * @param category - 组件类别
   * @returns 组件定义数组
   */
  getByCategory(category: WidgetCategory): WidgetDefinitionInternal[] {
    return Array.from(this.definitions.values()).filter(
      def => def.category === category
    )
  }

  /**
   * 获取所有组件定义
   *
   * @returns 组件定义数组
   */
  getAll(): WidgetDefinitionInternal[] {
    return Array.from(this.definitions.values())
  }

  /**
   * 获取所有组件类型
   *
   * @returns 组件类型数组
   */
  getTypes(): WidgetType[] {
    return Array.from(this.definitions.keys())
  }

  /**
   * 获取组件数量
   */
  get size(): number {
    return this.definitions.size
  }

  /**
   * 移除组件
   *
   * @param type - 组件类型
   * @returns 是否成功移除
   */
  remove(type: WidgetType): boolean {
    return this.definitions.delete(type)
  }

  /**
   * 清空所有组件
   */
  clear(): void {
    this.definitions.clear()
  }

  /**
   * 获取组件的默认尺寸
   *
   * @param type - 组件类型
   * @returns 默认尺寸
   */
  getDefaultSize(type: WidgetType): GridPosition {
    const def = this.get(type)
    return def?.defaultSize || { x: 0, y: 0, width: 4, height: 3 }
  }

  /**
   * 验证组件尺寸是否在允许范围内
   *
   * @param type - 组件类型
   * @param size - 尺寸
   * @returns 是否有效
   */
  isValidSize(type: WidgetType, size: { width: number; height: number }): boolean {
    const def = this.get(type)
    if (!def) return false

    const { minSize, maxSize } = def

    return (
      size.width >= minSize.width &&
      size.height >= minSize.height &&
      size.width <= maxSize.width &&
      size.height <= maxSize.height
    )
  }

  /**
   * 调整尺寸到允许范围内
   *
   * @param type - 组件类型
   * @param size - 原始尺寸
   * @returns 调整后的尺寸
   */
  clampSize(type: WidgetType, size: { width: number; height: number }): GridPosition {
    const def = this.get(type)
    if (!def) return { x: 0, y: 0, ...size }

    const { minSize, maxSize } = def

    return {
      x: 0,
      y: 0,
      width: Math.max(minSize.width, Math.min(size.width, maxSize.width)),
      height: Math.max(minSize.height, Math.min(size.height, maxSize.height))
    }
  }

  /**
   * 获取组件配置 Schema
   *
   * @param type - 组件类型
   * @returns JSON Schema 或 undefined
   */
  getConfigSchema(type: WidgetType): Record<string, unknown> | undefined {
    const def = this.get(type)
    return def?.configSchema
  }
}

// 导出全局单例
export const componentRegistry = new ComponentRegistry()

// 导出类型
export type { WidgetType, WidgetCategory, WidgetDefinition }
