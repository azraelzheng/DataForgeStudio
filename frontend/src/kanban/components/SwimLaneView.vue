<template>
  <div class="swimlane-view">
    <!-- 泳道列表 -->
    <div class="swimlane-list">
      <!-- 每个泳道 -->
      <div
        v-for="group in groupedCards"
        :key="group.id"
        class="swimlane-row"
      >
        <!-- 泳道头 -->
        <div class="swimlane-header">
          <div class="swimlane-title">
            <el-avatar :size="32" :src="group.avatar">
              {{ group.name.charAt(0) }}
            </el-avatar>
            <span>{{ group.name }}</span>
            <span class="card-count">({{ getGroupCardCount(group) }})</span>
          </div>
        </div>

        <!-- 泳道内容 - 状态列 -->
        <div class="swimlane-body">
          <div
            v-for="column in columns"
            :key="`${group.id}-${column.id}`"
            :class="['swimlane-cell', `status-${column.id}`]"
          >
            <!-- 卡片列表 -->
            <div class="cell-cards">
              <KanbanCard
                v-for="card in getCardsByGroupAndStatus(group.id, column.id)"
                :key="card.id"
                :card="card"
                :compact="compact"
                :readonly="readonly"
                :show-priority="showPriority"
                @click="handleCardClick"
              />
            </div>

            <!-- 空状态 -->
            <div v-if="getCardsByGroupAndStatus(group.id, column.id).length === 0" class="empty-cell">
              <span>-</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 列头 (状态) -->
    <div class="column-headers">
      <div class="header-spacer"></div>
      <div class="header-list">
        <div
          v-for="column in columns"
          :key="`header-${column.id}`"
          class="column-header-cell"
        >
          <span v-if="column.color" class="status-dot" :style="{ background: column.color }"></span>
          <span>{{ column.title }}</span>
          <span class="column-count">({{ getColumnCardCount(column.id) }})</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * SwimLaneView - 泳道视图组件
 * 按负责人/车间/产线分组显示看板卡片
 */

import { computed, type PropType } from 'vue'
import KanbanCard from './KanbanCard.vue'
import type { KanbanCard as KanbanCardType, KanbanColumn, SwimLaneGroup } from '../types/kanban'

// Props
const props = defineProps({
  /** 看板列配置 */
  columns: {
    type: Array as PropType<KanbanColumn[]>,
    required: true
  },
  /** 所有卡片 */
  cards: {
    type: Array as PropType<KanbanCardType[]>,
    required: true
  },
  /** 分组方式 */
  groupBy: {
    type: String as PropType<'assignee' | 'workshop' | 'production_line' | 'custom'>,
    default: 'assignee'
  },
  /** 是否紧凑模式 */
  compact: {
    type: Boolean,
    default: false
  },
  /** 是否只读模式 */
  readonly: {
    type: Boolean,
    default: false
  },
  /** 是否显示优先级 */
  showPriority: {
    type: Boolean,
    default: true
  }
})

// Emits
const emit = defineEmits<{
  /** 点击卡片 */
  (event: 'cardClick', card: KanbanCardType): void
}>(")

// ============================================
// 计算属性
// ============================================

/** 按分组方式整理的卡片 */
const groupedCards = computed((): SwimLaneGroup[] => {
  const groups = new Map<string, SwimLaneGroup>()

  // 初始化所有卡片到对应组
  for (const card of props.cards) {
    const groupId = getGroupId(card)
    const groupName = getGroupName(card)

    if (!groups.has(groupId)) {
      groups.set(groupId, {
        id: groupId,
        name: groupName,
        cards: {}
      })
    }

    const group = groups.get(groupId)!
    if (!group.cards[card.status]) {
      group.cards[card.status] = []
    }
    group.cards[card.status].push(card)
  }

  // 确保所有组都有所有状态列
  for (const group of groups.values()) {
    for (const column of props.columns) {
      if (!group.cards[column.id]) {
        group.cards[column.id] = []
      }
    }
  }

  return Array.from(groups.values())
})

/** 获取分组 ID */
function getGroupId(card: KanbanCardType): string {
  switch (props.groupBy) {
    case 'assignee':
      return card.assigneeId || 'unassigned'
    case 'workshop':
      return (card.customFields?.workshop as string) || 'default'
    case 'production_line':
      return (card.customFields?.productionLine as string) || 'default'
    case 'custom':
      return (card.customFields?.[props.groupBy] as string) || 'default'
    default:
      return 'default'
  }
}

/** 获取分组名称 */
function getGroupName(card: KanbanCardType): string {
  switch (props.groupBy) {
    case 'assignee':
      return card.assigneeName || '未分配'
    case 'workshop':
      return (card.customFields?.workshop as string) || '默认车间'
    case 'production_line':
      return (card.customFields?.productionLine as string) || '默认产线'
    case 'custom':
      return String(card.customFields?.[props.groupBy] || '默认分组')
    default:
      return '默认分组'
  }
}

/** 获取指定组的卡片数量 */
function getGroupCardCount(group: SwimLaneGroup): number {
  return Object.values(group.cards).flat().length
}

/** 获取指定组和状态的卡片 */
function getCardsByGroupAndStatus(groupId: string, status: string): KanbanCardType[] {
  const group = groupedCards.value.find(g => g.id === groupId)
  return group?.cards[status] || []
}

/** 获取指定状态的卡片总数 */
function getColumnCardCount(status: string): number {
  return props.cards.filter(c => c.status === status).length
}

/** 处理卡片点击 */
function handleCardClick(card: KanbanCardType): void {
  emit('cardClick', card)
}
</script>

<style scoped>
.swimlane-view {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}

/* 泳道列表 */
.swimlane-list {
  flex: 1;
  overflow-y: auto;
}

.swimlane-row {
  display: flex;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.swimlane-row:last-child {
  border-bottom: none;
}

/* 泳道头 */
.swimlane-header {
  width: 200px;
  min-width: 200px;
  padding: 12px;
  background: var(--el-fill-color-light);
  border-right: 1px solid var(--el-border-color-lighter);
  display: flex;
  align-items: center;
}

.swimlane-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.card-count {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  font-weight: 400;
}

/* 泳道体 */
.swimlane-body {
  flex: 1;
  display: flex;
  overflow-x: auto;
}

.swimlane-cell {
  flex: 1;
  min-width: 200px;
  padding: 12px;
  border-right: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color);
}

.swimlane-cell:last-child {
  border-right: none;
}

.cell-cards {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-height: 60px;
}

.empty-cell {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 60px;
  color: var(--el-text-color-placeholder);
  font-size: 12px;
  background: var(--el-fill-color-lighter);
  border-radius: 4px;
}

/* 列头 */
.column-headers {
  display: flex;
  background: var(--el-fill-color);
  border-bottom: 1px solid var(--el-border-color);
}

.header-spacer {
  width: 200px;
  min-width: 200px;
  padding: 12px;
  font-weight: 600;
  color: var(--el-text-color-secondary);
}

.header-list {
  flex: 1;
  display: flex;
}

.column-header-cell {
  flex: 1;
  min-width: 200px;
  padding: 12px;
  text-align: center;
  font-weight: 600;
  color: var(--el-text-color-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.column-count {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  font-weight: 400;
}

/* 滚动条样式 */
.swimlane-body::-webkit-scrollbar,
.swimlane-list::-webkit-scrollbar {
  height: 8px;
  width: 8px;
}

.swimlane-body::-webkit-scrollbar-track,
.swimlane-list::-webkit-scrollbar-track {
  background: var(--el-fill-color-lighter);
}

.swimlane-body::-webkit-scrollbar-thumb,
.swimlane-list::-webkit-scrollbar-thumb {
  background: var(--el-border-color);
  border-radius: 4px;
}
</style>
