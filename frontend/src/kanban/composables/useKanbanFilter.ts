/**
 * useKanbanFilter - 看板筛选/搜索 Composable
 * 负责处理看板的筛选和搜索逻辑
 * @module kanban/composables/useKanbanFilter
 */

import { ref, computed, type Ref, type ComputedRef } from 'vue'
import type { KanbanCard, KanbanFilters, CardPriority } from '../types/kanban'

/**
 * 使用看板筛选
 *
 * @example
 * ```typescript
 * const { filters, filteredCards, setFilter, resetFilters } = useKanbanFilter(cards)
 *
 * // 设置搜索关键词
 * setFilter('search', 'bug')
 *
 * // 设置优先级筛选
 * setFilter('priorities', ['high', 'urgent'])
 * ```
 */
export function useKanbanFilter(cards: Ref<KanbanCard[]>) {
  // ============================================
  // 状态
  // ============================================

  /** 筛选条件 */
  const filters: Ref<KanbanFilters> = ref({
    search: '',
    priorities: [],
    assignees: [],
    tags: [],
    dueDateFilter: undefined
  })

  // ============================================
  // 筛选逻辑
  // ============================================

  /** 筛选后的卡片 */
  const filteredCards: ComputedRef<KanbanCard[]> = computed(() => {
    let result = cards.value

    // 搜索关键词筛选
    if (filters.value.search) {
      const keyword = filters.value.search.toLowerCase()
      result = result.filter(card =>
        card.title.toLowerCase().includes(keyword) ||
        card.description?.toLowerCase().includes(keyword) ||
        card.tags?.some(tag => tag.toLowerCase().includes(keyword))
      )
    }

    // 优先级筛选
    if (filters.value.priorities && filters.value.priorities.length > 0) {
      result = result.filter(card =>
        filters.value.priorities!.includes(card.priority)
      )
    }

    // 负责人筛选
    if (filters.value.assignees && filters.value.assignees.length > 0) {
      result = result.filter(card =>
        card.assigneeId && filters.value.assignees!.includes(card.assigneeId)
      )
    }

    // 标签筛选
    if (filters.value.tags && filters.value.tags.length > 0) {
      result = result.filter(card =>
        card.tags && filters.value.tags!.some(tag => card.tags!.includes(tag))
      )
    }

    // 截止日期筛选
    if (filters.value.dueDateFilter) {
      result = result.filter(card => {
        if (!card.dueDate) return false

        const dueDate = new Date(card.dueDate)
        const today = new Date()
        today.setHours(0, 0, 0, 0)

        switch (filters.value.dueDateFilter) {
          case 'overdue':
            return dueDate < today
          case 'today':
            return dueDate.toDateString() === today.toDateString()
          case 'week': {
            const weekEnd = new Date(today)
            weekEnd.setDate(weekEnd.getDate() + 7)
            return dueDate >= today && dueDate <= weekEnd
          }
          case 'month': {
            const monthEnd = new Date(today)
            monthEnd.setMonth(monthEnd.getMonth() + 1)
            return dueDate >= today && dueDate <= monthEnd
          }
          case 'none':
            return !card.dueDate
          default:
            return true
        }
      })
    }

    return result
  })

  /** 是否有活跃筛选 */
  const hasActiveFilters: ComputedRef<boolean> = computed(() => {
    return !!(
      filters.value.search ||
      (filters.value.priorities && filters.value.priorities.length > 0) ||
      (filters.value.assignees && filters.value.assignees.length > 0) ||
      (filters.value.tags && filters.value.tags.length > 0) ||
      filters.value.dueDateFilter
    )
  })

  /** 筛选统计 */
  const filterStats: ComputedRef<{
    total: number
    filtered: number
    hidden: number
  }> = computed(() => {
    const total = cards.value.length
    const filtered = filteredCards.value.length
    return {
      total,
      filtered,
      hidden: total - filtered
    }
  })

  // ============================================
  // 筛选操作
  // ============================================

  /**
   * 设置筛选条件
   */
  function setFilter<K extends keyof KanbanFilters>(key: K, value: KanbanFilters[K]): void {
    filters.value[key] = value
  }

  /**
   * 批量设置筛选条件
   */
  function setFilters(newFilters: Partial<KanbanFilters>): void {
    filters.value = { ...filters.value, ...newFilters }
  }

  /**
   * 重置筛选条件
   */
  function resetFilters(): void {
    filters.value = {
      search: '',
      priorities: [],
      assignees: [],
      tags: [],
      dueDateFilter: undefined
    }
  }

  /**
   * 切换优先级筛选
   */
  function togglePriority(priority: CardPriority): void {
    const priorities = filters.value.priorities || []
    const index = priorities.indexOf(priority)

    if (index === -1) {
      filters.value.priorities = [...priorities, priority]
    } else {
      filters.value.priorities = priorities.filter(p => p !== priority)
    }
  }

  /**
   * 切换负责人筛选
   */
  function toggleAssignee(assigneeId: string): void {
    const assignees = filters.value.assignees || []
    const index = assignees.indexOf(assigneeId)

    if (index === -1) {
      filters.value.assignees = [...assignees, assigneeId]
    } else {
      filters.value.assignees = assignees.filter(a => a !== assigneeId)
    }
  }

  /**
   * 切换标签筛选
   */
  function toggleTag(tag: string): void {
    const tags = filters.value.tags || []
    const index = tags.indexOf(tag)

    if (index === -1) {
      filters.value.tags = [...tags, tag]
    } else {
      filters.value.tags = tags.filter(t => t !== tag)
    }
  }

  // ============================================
  // 辅助方法
  // ============================================

  /**
   * 获取所有可用的负责人
   */
  const availableAssignees: ComputedRef<Array<{ id: string; name: string; avatar?: string }>> = computed(() => {
    const assigneeMap = new Map<string, { name: string; avatar?: string }>()

    for (const card of cards.value) {
      if (card.assigneeId && !assigneeMap.has(card.assigneeId)) {
        assigneeMap.set(card.assigneeId, {
          name: card.assigneeName || 'Unknown',
          avatar: card.assigneeAvatar
        })
      }
    }

    return Array.from(assigneeMap.entries()).map(([id, info]) => ({
      id,
      ...info
    }))
  })

  /**
   * 获取所有可用的标签
   */
  const availableTags: ComputedRef<string[]> = computed(() => {
    const tagSet = new Set<string>()

    for (const card of cards.value) {
      if (card.tags) {
        for (const tag of card.tags) {
          tagSet.add(tag)
        }
      }
    }

    return Array.from(tagSet).sort()
  })

  /**
   * 检查卡片是否匹配筛选
   */
  function cardMatchesFilters(card: KanbanCard): boolean {
    return filteredCards.value.includes(card)
  }

  /**
   * 获取卡片在筛选结果中的索引
   */
  function getFilteredIndex(card: KanbanCard): number {
    return filteredCards.value.findIndex(c => c.id === card.id)
  }

  return {
    // 状态
    filters,
    filteredCards,
    hasActiveFilters,
    filterStats,
    availableAssignees,
    availableTags,

    // 操作
    setFilter,
    setFilters,
    resetFilters,
    togglePriority,
    toggleAssignee,
    toggleTag,
    cardMatchesFilters,
    getFilteredIndex
  }
}
