/**
 * LayoutEngine - 网格布局引擎
 * 负责计算组件在网格中的位置，处理碰撞检测和自动排列
 * @module dashboard/core/LayoutEngine
 */

import type { GridPosition, PixelPosition, LayoutConfig, CollisionResult, WidgetInstance } from '../types/dashboard'

/**
 * 默认布局配置
 */
const DEFAULT_LAYOUT_CONFIG: LayoutConfig = {
  columns: 12,
  rowHeight: 60,
  gap: 8,
  maxRows: 100
}

/**
 * 网格布局引擎
 *
 * @example
 * ```typescript
 * const engine = new LayoutEngine({ columns: 12, rowHeight: 60, gap: 8 })
 *
 * // 计算像素位置
 * const pixelPos = engine.calcPixelPosition({ x: 0, y: 0, width: 4, height: 2 }, 1200)
 *
 * // 检测碰撞
 * const collisions = engine.detectCollisions(widgets)
 *
 * // 自动排列
 * const arranged = engine.autoArrange(widgets)
 * ```
 */
export class LayoutEngine {
  private config: LayoutConfig

  constructor(config: Partial<LayoutConfig> = {}) {
    this.config = { ...DEFAULT_LAYOUT_CONFIG, ...config }
  }

  /**
   * 获取当前配置
   */
  getConfig(): LayoutConfig {
    return { ...this.config }
  }

  /**
   * 更新配置
   */
  updateConfig(config: Partial<LayoutConfig>): void {
    this.config = { ...this.config, ...config }
  }

  /**
   * 将网格位置转换为像素位置
   */
  calcPixelPosition(pos: GridPosition, containerWidth: number): PixelPosition {
    const { columns, rowHeight, gap } = this.config
    const colWidth = (containerWidth - (columns - 1) * gap) / columns

    return {
      left: Math.round(pos.x * (colWidth + gap)),
      top: Math.round(pos.y * (rowHeight + gap)),
      width: Math.round(pos.width * colWidth + (pos.width - 1) * gap),
      height: Math.round(pos.height * (rowHeight + gap) - gap)
    }
  }

  /**
   * 将像素坐标转换为网格位置
   */
  pixelToGrid(left: number, top: number, containerWidth: number): { x: number; y: number } {
    const { columns, rowHeight, gap } = this.config
    const colWidth = (containerWidth - (columns - 1) * gap) / columns

    return {
      x: Math.round(left / (colWidth + gap)),
      y: Math.round(top / (rowHeight + gap))
    }
  }

  /**
   * 将坐标吸附到网格
   */
  snapToGrid(x: number, y: number, containerWidth: number): { x: number; y: number } {
    const gridPos = this.pixelToGrid(x, y, containerWidth)
    return {
      x: Math.max(0, Math.min(gridPos.x, this.config.columns - 1)),
      y: Math.max(0, gridPos.y)
    }
  }

  /**
   * 检测两个组件是否碰撞
   */
  detectCollision(a: GridPosition, b: GridPosition): boolean {
    return !(
      a.x + a.width <= b.x ||
      b.x + b.width <= a.x ||
      a.y + a.height <= b.y ||
      b.y + b.height <= a.y
    )
  }

  /**
   * 检测组件列表中的所有碰撞
   */
  detectCollisions(widgets: WidgetInstance[]): CollisionResult[] {
    const collisions: CollisionResult[] = []
    const positions = widgets.map(w => ({ id: w.id, pos: w.position }))

    for (let i = 0; i < positions.length; i++) {
      for (let j = i + 1; j < positions.length; j++) {
        if (this.detectCollision(positions[i].pos, positions[j].pos)) {
          collisions.push({
            widgetId1: positions[i].id,
            widgetId2: positions[j].id,
            position: positions[i].pos
          })
        }
      }
    }

    return collisions
  }

  /**
   * 检查组件是否超出网格边界
   */
  isValidPosition(pos: GridPosition): boolean {
    const { columns, maxRows } = this.config

    if (pos.x < 0 || pos.y < 0 || pos.width < 1 || pos.height < 1) {
      return false
    }

    if (pos.x + pos.width > columns) {
      return false
    }

    if (maxRows && pos.y + pos.height > maxRows) {
      return false
    }

    return true
  }

  /**
   * 查找组件可以放置的位置（不产生碰撞）
   */
  findAvailablePosition(
    pos: GridPosition,
    widgets: WidgetInstance[],
    excludeId?: string
  ): GridPosition | null {
    const { columns, maxRows } = this.config
    const otherWidgets = widgets.filter(w => w.id !== excludeId)

    for (let y = pos.y; y < (maxRows || 100); y++) {
      for (let x = 0; x <= columns - pos.width; x++) {
        const testPos = { ...pos, x, y }

        if (!this.isValidPosition(testPos)) {
          continue
        }

        const hasCollision = otherWidgets.some(w => this.detectCollision(testPos, w.position))

        if (!hasCollision) {
          return testPos
        }
      }
    }

    return null
  }

  /**
   * 自动排列组件（紧凑布局）
   */
  autoArrange(widgets: WidgetInstance[]): Map<string, GridPosition> {
    const result = new Map<string, GridPosition>()
    const { columns } = this.config

    const sorted = [...widgets].sort((a, b) => {
      if (a.position.y !== b.position.y) return a.position.y - b.position.y
      return a.position.x - b.position.x
    })

    const occupied = new Set<string>()

    const isOccupied = (pos: GridPosition): boolean => {
      for (let dy = 0; dy < pos.height; dy++) {
        for (let dx = 0; dx < pos.width; dx++) {
          if (occupied.has(`${pos.x + dx},${pos.y + dy}`)) {
            return true
          }
        }
      }
      return false
    }

    const markOccupied = (pos: GridPosition): void => {
      for (let dy = 0; dy < pos.height; dy++) {
        for (let dx = 0; dx < pos.width; dx++) {
          occupied.add(`${pos.x + dx},${pos.y + dy}`)
        }
      }
    }

    for (const widget of sorted) {
      const size = { width: widget.position.width, height: widget.position.height }
      let placed = false

      for (let y = 0; y < 100 && !placed; y++) {
        for (let x = 0; x <= columns - size.width && !placed; x++) {
          const testPos = { ...size, x, y }

          if (x + size.width <= columns && !isOccupied(testPos)) {
            result.set(widget.id, testPos)
            markOccupied(testPos)
            placed = true
          }
        }
      }
    }

    return result
  }

  /**
   * 计算组件占据的总行数
   */
  calculateTotalRows(widgets: WidgetInstance[]): number {
    if (widgets.length === 0) return 0
    return Math.max(...widgets.map(w => w.position.y + w.position.height))
  }

  /**
   * 调整组件大小
   */
  resizeWidget(
    widget: WidgetInstance,
    newSize: { width: number; height: number },
    widgets: WidgetInstance[]
  ): GridPosition | null {
    const minSize = { width: 1, height: 1 }
    const maxSize = { width: 12, height: 10 }

    const width = Math.max(minSize.width, Math.min(newSize.width, maxSize.width))
    const height = Math.max(minSize.height, Math.min(newSize.height, maxSize.height))

    const newPos: GridPosition = {
      x: widget.position.x,
      y: widget.position.y,
      width,
      height
    }

    if (!this.isValidPosition(newPos)) {
      return null
    }

    const hasCollision = widgets
      .filter(w => w.id !== widget.id)
      .some(w => this.detectCollision(newPos, w.position))

    if (hasCollision) {
      return null
    }

    return newPos
  }
}

// 导出默认实例
export const layoutEngine = new LayoutEngine()
