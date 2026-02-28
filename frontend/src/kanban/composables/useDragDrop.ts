/**
 * useDragDrop - 看板拖拽逻辑 Composable
 * 负责处理卡片的拖拽操作
 * @module kanban/composables/useDragDrop
 */

import { ref, type Ref } from 'vue'
import type { KanbanCard, DragContext, DropTarget, MoveCardRequest } from '../types/kanban'

/**
 * 拖拽状态
 */
interface DragState {
  /** 拖拽中的卡片 */
  draggedCard: Ref<KanbanCard | null>
  /** 源列 ID */
  sourceColumnId: Ref<string | null>
  /** 源位置索引 */
  sourceIndex: Ref<number | null>
  /** 是否正在拖拽 */
  isDragging: Ref<boolean>
}

/**
 * 使用拖拽功能
 *
 * @example
 * ```typescript
 * const { draggedCard, startDrag, onDrop, cancelDrag } = useDragDrop({
 *   onMove: async (request) => {
 *     await api.moveCard(request)
 *   }
 * })
 * ```
 */
export function useDragDrop(options?: {
  /** 移动卡片回调 */
  onMove?: (request: MoveCardRequest) => Promise<void>
  /** 移动前验证 */
  canMove?: (card: KanbanCard, toStatus: string) => boolean
}) {
  // ============================================
  // 状态
  // ============================================

  const draggedCard: Ref<KanbanCard | null> = ref(null)
  const sourceColumnId: Ref<string | null> = ref(null)
  const sourceIndex: Ref<number | null> = ref(null)
  const dropTarget: Ref<DropTarget | null> = ref(null)
  const isDragging: Ref<boolean> = ref(false)

  // ============================================
  // 拖拽开始
  // ============================================

  /**
   * 开始拖拽
   */
  function startDrag(card: KanbanCard, columnId: string, index: number): void {
    draggedCard.value = card
    sourceColumnId.value = columnId
    sourceIndex.value = index
    isDragging.value = true
    dropTarget.value = null

    console.debug('[useDragDrop] 开始拖拽:', {
      card: card.id,
      title: card.title,
      from: columnId,
      index
    })
  }

  // ============================================
  // 拖拽过程
  // ============================================

  /**
   * 设置放置目标
   */
  function setDropTarget(columnId: string, index: number): void {
    dropTarget.value = { columnId, index }
  }

  /**
   * 清除放置目标
   */
  function clearDropTarget(): void {
    dropTarget.value = null
  }

  /**
   * 检查是否可以放置
   */
  function canDrop(columnId: string): boolean {
    if (!draggedCard.value) return false

    // 同列拖拽检查
    if (sourceColumnId.value === columnId) {
      return true
    }

    // 自定义验证
    if (options?.canMove) {
      return options.canMove(draggedCard.value, columnId)
    }

    return true
  }

  // ============================================
  // 放置操作
  // ============================================

  /**
   * 处理放置
   */
  async function onDrop(columnId: string, index: number): Promise<boolean> {
    if (!draggedCard.value || !sourceColumnId.value) {
      console.warn('[useDragDrop] 放置失败: 无拖拽上下文')
      return false
    }

    if (!canDrop(columnId)) {
      console.warn('[useDragDrop] 放置失败: 不允许放置到目标列')
      cancelDrag()
      return false
    }

    const request: MoveCardRequest = {
      cardId: draggedCard.value.id,
      fromStatus: sourceColumnId.value,
      toStatus: columnId,
      newOrder: index
    }

    try {
      // 调用移动回调
      if (options?.onMove) {
        await options.onMove(request)
      }

      console.debug('[useDragDrop] 放置成功:', request)

      // 本地更新卡片状态（乐观更新）
      updateCardLocally(draggedCard.value, columnId, index)

      return true
    } catch (error) {
      console.error('[useDragDrop] 放置失败:', error)
      return false
    } finally {
      cancelDrag()
    }
  }

  /**
   * 本地更新卡片状态
   */
  function updateCardLocally(card: KanbanCard, toStatus: string, newOrder: number): void {
    card.status = toStatus
    card.order = newOrder
  }

  /**
   * 取消拖拽
   */
  function cancelDrag(): void {
    draggedCard.value = null
    sourceColumnId.value = null
    sourceIndex.value = null
    dropTarget.value = null
    isDragging.value = false

    console.debug('[useDragDrop] 取消拖拽')
  }

  // ============================================
  // 辅助方法
  // ============================================

  /**
   * 获取拖拽样式类
   */
  function getDragClass(isTarget: boolean, isOver: boolean): Record<string, boolean> {
    return {
      'is-dragging': isDragging.value,
      'is-drag-source': isTarget && sourceColumnId.value !== null,
      'is-drag-over': isOver,
      'can-drop': isTarget && canDrop(dropTarget.value?.columnId || '')
    }
  }

  /**
   * 检查是否拖拽源
   */
  function isDragSource(columnId: string, index: number): boolean {
    return sourceColumnId.value === columnId && sourceIndex.value === index
  }

  return {
    // 状态
    draggedCard,
    sourceColumnId,
    sourceIndex,
    dropTarget,
    isDragging,

    // 操作
    startDrag,
    setDropTarget,
    clearDropTarget,
    canDrop,
    onDrop,
    cancelDrag,

    // 辅助方法
    getDragClass,
    isDragSource
  }
}
