/**
 * useKanbanState - 看板状态管理 Composable
 * 负责管理看板的数据和状态
 * @module kanban/composables/useKanbanState
 */

import { ref, computed, type Ref, type ComputedRef } from 'vue'
import type { KanbanCard, KanbanColumn, KanbanConfig, CardStatus } from '../types/kanban'

/**
 * 看板状态
 */
export interface KanbanState {
  /** 看板配置 */
  config: KanbanConfig | null
  /** 所有卡片 */
  cards: KanbanCard[]
  /** 加载中 */
  isLoading: boolean
  /** 错误信息 */
  error: string | null
}

/**
 * 使用看板状态管理
 *
 * @example
 * ```typescript
 * const { cardsByStatus, loadCards, addCard, updateCard, deleteCard } = useKanbanState()
 *
 * // 加载卡片
 * await loadCards('dashboard-123')
 *
 * // 按状态获取卡片
 * const todoCards = cardsByStatus.value('todo')
 * ```
 */
export function useKanbanState(initialConfig?: KanbanConfig) {
  // ============================================
  // 状态
  // ============================================

  /** 看板配置 */
  const config: Ref<KanbanConfig | null> = ref(initialConfig || null)

  /** 所有卡片 */
  const cards: Ref<KanbanCard[]> = ref([])

  /** 加载中 */
  const isLoading: Ref<boolean> = ref(false)

  /** 错误信息 */
  const error: Ref<string | null> = ref(null)

  /** 选中的卡片 */
  const selectedCardId: Ref<string | null> = ref(null)

  // ============================================
  // 计算属性
  // ============================================

  /** 看板列 */
  const columns: ComputedRef<KanbanColumn[]> = computed(() => {
    return config.value?.columns || []
  })

  /** 按状态分组的卡片 */
  const cardsByStatus: ComputedRef<Map<CardStatus, KanbanCard[]>> = computed(() => {
    const map = new Map<CardStatus, KanbanCard[]>()

    // 初始化所有状态列
    for (const column of columns.value) {
      map.set(column.id, [])
    }

    // 分组卡片
    for (const card of cards.value) {
      const statusCards = map.get(card.status)
      if (statusCards) {
        statusCards.push(card)
      }
    }

    // 按顺序排序
    for (const [status, statusCards] of map) {
      map.set(status, statusCards.sort((a, b) => a.order - b.order))
    }

    return map
  })

  /** 获取指定状态的卡片 */
  function getCardsByStatus(status: CardStatus): KanbanCard[] {
    return cardsByStatus.value.get(status) || []
  }

  /** 卡片总数 */
  const totalCards: ComputedRef<number> = computed(() => {
    return cards.value.length
  })

  /** 选中的卡片 */
  const selectedCard: ComputedRef<KanbanCard | null> = computed(() => {
    if (!selectedCardId.value) return null
    return cards.value.find(c => c.id === selectedCardId.value) || null
  })

  // ============================================
  // 加载操作
  // ============================================

  /**
   * 设置看板配置
   */
  function setConfig(kanbanConfig: KanbanConfig): void {
    config.value = kanbanConfig
  }

  /**
   * 加载卡片列表
   */
  async function loadCards(dashboardId: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      const response = await fetch(`/api/kanban/${dashboardId}/cards`)
      const result = await response.json()

      if (result.success) {
        cards.value = result.data
      } else {
        error.value = result.message
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : '加载卡片失败'
    } finally {
      isLoading.value = false
    }
  }

  /**
   * 刷新卡片列表
   */
  async function refreshCards(dashboardId: string): Promise<void> {
    await loadCards(dashboardId)
  }

  // ============================================
  // 卡片操作
  // ============================================

  /**
   * 添加卡片
   */
  async function addCard(dashboardId: string, card: Partial<KanbanCard>): Promise<KanbanCard | null> {
    try {
      const response = await fetch(`/api/kanban/${dashboardId}/cards`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(card)
      })
      const result = await response.json()

      if (result.success) {
        const newCard = result.data
        cards.value.push(newCard)
        return newCard
      } else {
        error.value = result.message
        return null
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : '添加卡片失败'
      return null
    }
  }

  /**
   * 更新卡片
   */
  async function updateCard(dashboardId: string, cardId: string, updates: Partial<KanbanCard>): Promise<boolean> {
    try {
      const response = await fetch(`/api/kanban/${dashboardId}/cards/${cardId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(updates)
      })
      const result = await response.json()

      if (result.success) {
        // 更新本地卡片
        const index = cards.value.findIndex(c => c.id === cardId)
        if (index !== -1) {
          cards.value[index] = { ...cards.value[index], ...updates }
        }
        return true
      } else {
        error.value = result.message
        return false
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : '更新卡片失败'
      return false
    }
  }

  /**
   * 删除卡片
   */
  async function deleteCard(dashboardId: string, cardId: string): Promise<boolean> {
    try {
      const response = await fetch(`/api/kanban/${dashboardId}/cards/${cardId}`, {
        method: 'DELETE'
      })
      const result = await response.json()

      if (result.success) {
        // 从本地列表中移除
        const index = cards.value.findIndex(c => c.id === cardId)
        if (index !== -1) {
          cards.value.splice(index, 1)
        }
        return true
      } else {
        error.value = result.message
        return false
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : '删除卡片失败'
      return false
    }
  }

  /**
   * 移动卡片
   */
  async function moveCard(dashboardId: string, request: {
    cardId: string
    fromStatus: string
    toStatus: string
    newOrder: number
  }): Promise<boolean> {
    try {
      const response = await fetch(`/api/kanban/${dashboardId}/cards/move`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request)
      })
      const result = await response.json()

      if (result.success) {
        // 更新本地卡片
        const card = cards.value.find(c => c.id === request.cardId)
        if (card) {
          card.status = request.toStatus
          card.order = request.newOrder
        }
        return true
      } else {
        error.value = result.message
        return false
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : '移动卡片失败'
      return false
    }
  }

  // ============================================
  // 选择操作
  // ============================================

  /**
   * 选中卡片
   */
  function selectCard(cardId: string | null): void {
    selectedCardId.value = cardId
  }

  /**
   * 清除选择
   */
  function clearSelection(): void {
    selectedCardId.value = null
  }

  // ============================================
  // 本地更新
  // ============================================

  /**
   * 本地添加卡片（乐观更新）
   */
  function addCardLocally(card: KanbanCard): void {
    cards.value.push(card)
  }

  /**
   * 本地更新卡片（乐观更新）
   */
  function updateCardLocally(cardId: string, updates: Partial<KanbanCard>): void {
    const index = cards.value.findIndex(c => c.id === cardId)
    if (index !== -1) {
      cards.value[index] = { ...cards.value[index], ...updates }
    }
  }

  /**
   * 本地删除卡片（乐观更新）
   */
  function deleteCardLocally(cardId: string): void {
    const index = cards.value.findIndex(c => c.id === cardId)
    if (index !== -1) {
      cards.value.splice(index, 1)
    }
  }

  /**
   * 本地移动卡片（乐观更新）
   */
  function moveCardLocally(cardId: string, toStatus: string, newOrder: number): void {
    const card = cards.value.find(c => c.id === cardId)
    if (card) {
      card.status = toStatus
      card.order = newOrder
    }
  }

  // ============================================
  // 重置
  // ============================================

  /**
   * 重置状态
   */
  function reset(): void {
    config.value = null
    cards.value = []
    isLoading.value = false
    error.value = null
    selectedCardId.value = null
  }

  return {
    // 状态
    config,
    cards,
    isLoading,
    error,
    selectedCardId,

    // 计算属性
    columns,
    cardsByStatus,
    totalCards,
    selectedCard,

    // 加载操作
    setConfig,
    loadCards,
    refreshCards,

    // 卡片操作
    addCard,
    updateCard,
    deleteCard,
    moveCard,
    getCardsByStatus,

    // 选择操作
    selectCard,
    clearSelection,

    // 本地更新
    addCardLocally,
    updateCardLocally,
    deleteCardLocally,
    moveCardLocally,

    // 工具方法
    reset
  }
}
