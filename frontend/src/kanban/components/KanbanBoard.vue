<template>
  <div class="kanban-board">
    <!-- 工具栏 -->
    <div class="kanban-toolbar">
      <div class="toolbar-left">
        <!-- 搜索 -->
        <el-input
          v-model="searchKeyword"
          placeholder="搜索卡片..."
          :prefix-icon="Search"
          clearable
          class="search-input"
          @input="handleSearch"
        />

        <!-- 筛选器 -->
        <el-dropdown trigger="click" @command="handleFilterCommand">
          <el-button :type="hasActiveFilters ? 'primary' : 'default'">
            <el-icon><Filter /></el-icon>
            筛选
            <el-badge v-if="activeFilterCount > 0" :value="activeFilterCount" type="danger" />
          </el-button>
          <template #dropdown>
            <el-dropdown-menu>
              <!-- 优先级筛选 -->
              <el-dropdown-item disabled>
                <div class="filter-header">优先级</div>
              </el-dropdown-item>
              <el-dropdown-item
                v-for="priority in priorities"
                :key="priority.value"
                :command="`priority:${priority.value}`"
                :class="{ 'is-selected': isPrioritySelected(priority.value) }"
              >
                <el-icon :color="priority.color"><Flag /></el-icon>
                {{ priority.label }}
                <el-icon v-if="isPrioritySelected(priority.value)" class="check-icon"><Check /></el-icon>
              </el-dropdown-item>

              <el-dropdown-item divided />

              <!-- 负责人筛选 -->
              <el-dropdown-item disabled>
                <div class="filter-header">负责人</div>
              </el-dropdown-item>
              <el-dropdown-item
                v-for="assignee in availableAssignees"
                :key="assignee.id"
                :command="`assignee:${assignee.id}`"
                :class="{ 'is-selected': isAssigneeSelected(assignee.id) }"
              >
                <el-avatar :size="20" :src="assignee.avatar">
                  {{ assignee.name.charAt(0) }}
                </el-avatar>
                {{ assignee.name }}
                <el-icon v-if="isAssigneeSelected(assignee.id)" class="check-icon"><Check /></el-icon>
              </el-dropdown-item>

              <el-dropdown-item divided />

              <!-- 截止日期筛选 -->
              <el-dropdown-item disabled>
                <div class="filter-header">截止日期</div>
              </el-dropdown-item>
              <el-dropdown-item
                v-for="option in dueDateOptions"
                :key="option.value"
                :command="`dueDate:${option.value}`"
                :class="{ 'is-selected': dueDateFilter === option.value }"
              >
                <el-icon><Calendar /></el-icon>
                {{ option.label }}
                <el-icon v-if="dueDateFilter === option.value" class="check-icon"><Check /></el-icon>
              </el-dropdown-item>

              <el-dropdown-item divided />

              <!-- 清除筛选 -->
              <el-dropdown-item
                :disabled="!hasActiveFilters"
                :divided="true"
                command="clear"
              >
                <el-icon><Close /></el-icon>
                清除所有筛选
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>

        <!-- 视图切换 -->
        <el-button-group>
          <el-button
            :type="viewMode === 'board' ? 'primary' : ''"
            @click="setViewMode('board')"
          >
            <el-icon><Grid /></el-icon>
            看板
          </el-button>
          <el-button
            :type="viewMode === 'swimlane' ? 'primary' : ''"
            @click="setViewMode('swimlane')"
          >
            <el-icon><Operation /></el-icon>
            泳道
          </el-button>
        </el-button-group>
      </div>

      <div class="toolbar-right">
        <!-- 刷新 -->
        <el-button :icon="Refresh" @click="handleRefresh" :loading="isLoading">
          刷新
        </el-button>

        <!-- 添加卡片 -->
        <el-button type="primary" :icon="Plus" @click="handleAddCard">
          添加卡片
        </el-button>
      </div>
    </div>

    <!-- 筛选状态栏 -->
    <div v-if="hasActiveFilters" class="filter-status-bar">
      <div class="filter-tags">
        <el-tag
          v-for="priority in selectedPriorities"
          :key="`priority-${priority}`"
          closable
          @close="togglePriority(priority)"
        >
          优先级: {{ getPriorityLabel(priority) }}
        </el-tag>
        <el-tag
          v-for="assignee in selectedAssignees"
          :key="`assignee-${assignee.id}`"
          closable
          @close="toggleAssignee(assignee.id)"
        >
          {{ assignee.name }}
        </el-tag>
        <el-tag v-if="dueDateFilter" closable @close="dueDateFilter = undefined">
          {{ getDueDateLabel(dueDateFilter) }}
        </el-tag>
      </div>
      <div class="filter-stats">
        显示 {{ filteredCardCount }} / {{ totalCardCount }} 张卡片
      </div>
    </div>

    <!-- 看板视图 -->
    <div v-if="viewMode === 'board'" class="kanban-columns-wrapper">
      <KanbanColumn
        v-for="column in columns"
        :key="column.id"
        :column="column"
        :cards="getCardsByStatus(column.id)"
        :compact="compact"
        :readonly="readonly"
        :show-priority="showPriority"
        @card-drag-start="handleCardDragStart"
        @card-drag-end="handleCardDragEnd"
        @card-drop="handleCardDrop"
        @card-click="handleCardClick"
        @add-card="handleAddCardToColumn"
        @edit-column="handleEditColumn"
        @clear-column="handleClearColumn"
      />
    </div>

    <!-- 泳道视图 -->
    <SwimLaneView
      v-else
      :columns="columns"
      :cards="filteredCards"
      :group-by="swimLaneGroupBy"
      :compact="compact"
      @card-click="handleCardClick"
    />

    <!-- 卡片详情弹窗 -->
    <CardDetail
      v-model="showDetail"
      :card="selectedCard"
      :readonly="readonly"
      @save="handleSaveCard"
      @delete="handleDeleteCard"
    />

    <!-- 卡片表单弹窗 -->
    <CardForm
      v-model="showForm"
      :card="editingCard"
      :columns="columns"
      @save="handleSaveCard"
    />
  </div>
</template>

<script setup lang="ts">
/**
 * KanbanBoard - 看板主体组件
 * Trello 风格的看板视图，支持拖拽、筛选、搜索等功能
 */

import { ref, computed, onMounted, watch, type PropType } from 'vue'
import { ElMessage } from 'element-plus'
import {
  Search, Filter, Refresh, Plus, Grid, Operation,
  Flag, Check, Calendar, Close
} from '@element-plus/icons-vue'
import KanbanColumn from './KanbanColumn.vue'
import CardDetail from './CardDetail.vue'
import CardForm from './CardForm.vue'
import SwimLaneView from './SwimLaneView.vue'
import { useDragDrop } from '../composables/useDragDrop'
import { useKanbanState } from '../composables/useKanbanState'
import { useKanbanFilter } from '../composables/useKanbanFilter'
import type { KanbanConfig, KanbanCard, KanbanColumn as KanbanColumnType, CardPriority } from '../types/kanban'

// Props
const props = defineProps({
  /** 看板 ID */
  dashboardId: {
    type: String,
    required: true
  },
  /** 看板配置 */
  config: {
    type: Object as PropType<KanbanConfig>,
    required: true
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
  /** 加载卡片 */
  (event: 'load', dashboardId: string): void
  /** 刷新卡片 */
  (event: 'refresh', dashboardId: string): void
  /** 卡片变更 */
  (event: 'change'): void
}>(")

// ============================================
// Composables
// ============================================

const state = useKanbanState(props.config)
const { draggedCard, startDrag, onDrop, cancelDrag } = useDragDrop({
  onMove: async (request) => {
    await state.moveCard(props.dashboardId, request)
    emit('change')
  }
})

const allCards = ref<KanbanCard[]>([])
const filter = useKanbanFilter(allCards)

// ============================================
// 状态
// ============================================

const viewMode = ref<'board' | 'swimlane'>('board')
const swimLaneGroupBy = ref<'assignee' | 'workshop' | 'custom'>('assignee')
const showDetail = ref(false)
const showForm = ref(false)
const selectedCard = ref<KanbanCard | null>(null)
const editingCard = ref<Partial<KanbanCard> | null>(null)
const isLoading = ref(false)

// ============================================
// 计算属性
// ============================================

/** 看板列 */
const columns = computed(() => props.config.columns || [])

/** 筛选后的卡片 */
const filteredCards = computed(() => filter.filteredCards.value)

/** 筛选条件 */
const searchKeyword = computed({
  get: () => filter.filters.value.search || '',
  set: (value) => filter.setFilter('search', value || undefined)
})

const selectedPriorities = computed(() => filter.filters.value.priorities || [])
const selectedAssignees = computed(() => {
  const assigneeIds = filter.filters.value.assignees || []
  return filter.availableAssignees.value.filter(a => assigneeIds.includes(a.id))
})
const dueDateFilter = computed({
  get: () => filter.filters.value.dueDateFilter,
  set: (value) => filter.setFilter('dueDateFilter', value)
})

const hasActiveFilters = computed(() => filter.hasActiveFilters.value)
const activeFilterCount = computed(() => {
  let count = 0
  if (selectedPriorities.value.length > 0) count++
  if (selectedAssignees.value.length > 0) count++
  if (dueDateFilter.value) count++
  if (searchKeyword.value) count++
  return count
})

const filteredCardCount = computed(() => filteredCards.value.length)
const totalCardCount = computed(() => allCards.value.length)

const availableAssignees = computed(() => filter.availableAssignees.value)

// ============================================
// 筛选选项
// ============================================

const priorities = [
  { value: 'urgent' as CardPriority, label: '紧急', color: '#f56c6c' },
  { value: 'high' as CardPriority, label: '高', color: '#e6a23c' },
  { value: 'medium' as CardPriority, label: '中', color: '#67c23a' },
  { value: 'low' as CardPriority, label: '低', color: '#909399' }
]

const dueDateOptions = [
  { value: 'overdue', label: '已逾期' },
  { value: 'today', label: '今天到期' },
  { value: 'week', label: '一周内' },
  { value: 'month', label: '一月内' },
  { value: 'none', label: '无截止日期' }
]

// ============================================
// 方法
// ============================================

/** 获取指定状态的卡片 */
function getCardsByStatus(status: string): KanbanCard[] {
  return filteredCards.value.filter(card => card.status === status)
}

/** 处理搜索 */
function handleSearch(): void {
  // 搜索由 computed 自动处理
}

/** 处理筛选命令 */
function handleFilterCommand(command: string): void {
  const [type, value] = command.split(':')

  switch (type) {
    case 'priority':
      togglePriority(value as CardPriority)
      break
    case 'assignee':
      toggleAssignee(value)
      break
    case 'dueDate':
      filter.setFilter('dueDateFilter', value === 'none' ? 'none' : value as any)
      break
    case 'clear':
      filter.resetFilters()
      break
  }
}

/** 切换优先级筛选 */
function togglePriority(priority: CardPriority): void {
  filter.togglePriority(priority)
}

/** 切换负责人筛选 */
function toggleAssignee(assigneeId: string): void {
  filter.toggleAssignee(assigneeId)
}

/** 检查优先级是否选中 */
function isPrioritySelected(priority: CardPriority): boolean {
  return selectedPriorities.value.includes(priority)
}

/** 检查负责人是否选中 */
function isAssigneeSelected(assigneeId: string): boolean {
  return selectedAssignees.value.some(a => a.id === assigneeId)
}

/** 获取优先级标签 */
function getPriorityLabel(priority: CardPriority): string {
  return priorities.find(p => p.value === priority)?.label || ''
}

/** 获取截止日期标签 */
function getDueDateLabel(value: string): string {
  return dueDateOptions.find(o => o.value === value)?.label || ''
}

/** 设置视图模式 */
function setViewMode(mode: 'board' | 'swimlane'): void {
  viewMode.value = mode
}

/** 处理刷新 */
async function handleRefresh(): Promise<void> {
  isLoading.value = true
  try {
    await state.refreshCards(props.dashboardId)
    allCards.value = state.cards.value
    emit('refresh', props.dashboardId)
    ElMessage.success('刷新成功')
  } finally {
    isLoading.value = false
  }
}

/** 处理添加卡片 */
function handleAddCard(): void {
  editingCard.value = {
    status: columns.value[0]?.id || '',
    priority: 'medium',
    order: filteredCards.value.length
  }
  showForm.value = true
}

/** 处理添加卡片到指定列 */
function handleAddCardToColumn(columnId: string): void {
  editingCard.value = {
    status: columnId,
    priority: 'medium',
    order: getCardsByStatus(columnId).length
  }
  showForm.value = true
}

/** 处理卡片点击 */
function handleCardClick(card: KanbanCard): void {
  selectedCard.value = card
  showDetail.value = true
}

/** 处理卡片拖拽开始 */
function handleCardDragStart(card: KanbanCard, columnId: string): void {
  const index = getCardsByStatus(columnId).findIndex(c => c.id === card.id)
  startDrag(card, columnId, index)
}

/** 处理卡片拖拽结束 */
function handleCardDragEnd(card: KanbanCard): void {
  cancelDrag()
}

/** 处理卡片放置 */
async function handleCardDrop(card: KanbanCard, toColumnId: string, toIndex: number): Promise<void> {
  if (!draggedCard.value) return

  const fromStatus = columns.value.find(c => c.id === card.id)?.id || card.status

  await onDrop(toColumnId, toIndex)
  emit('change')
}

/** 处理保存卡片 */
async function handleSaveCard(card: Partial<KanbanCard>): Promise<void> {
  if (card.id) {
    await state.updateCard(props.dashboardId, card.id, card)
  } else {
    await state.addCard(props.dashboardId, card)
  }
  showForm.value = false
  showDetail.value = false
  emit('change')
}

/** 处理删除卡片 */
async function handleDeleteCard(card: KanbanCard): Promise<void> {
  await state.deleteCard(props.dashboardId, card.id)
  showDetail.value = false
  emit('change')
}

/** 处理编辑列 */
function handleEditColumn(column: KanbanColumnType): void {
  // TODO: 实现列编辑
  ElMessage.info('编辑列功能开发中')
}

/** 处理清空列 */
async function handleClearColumn(columnId: string): void {
  const cards = getCardsByStatus(columnId)
  for (const card of cards) {
    await state.deleteCard(props.dashboardId, card.id)
  }
  emit('change')
}

// ============================================
// 生命周期
// ============================================

onMounted(async () => {
  await state.loadCards(props.dashboardId)
  allCards.value = state.cards.value
  emit('load', props.dashboardId)
})

// 监听状态变化
watch(() => state.cards, (newCards) => {
  allCards.value = newCards
}, { deep: true })
</script>

<style scoped>
.kanban-board {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: var(--el-bg-color-page);
}

/* 工具栏 */
.kanban-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px;
  background: var(--el-bg-color);
  border-bottom: 1px solid var(--el-border-color-light);
  gap: 16px;
}

.toolbar-left,
.toolbar-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.search-input {
  width: 240px;
}

/* 筛选状态栏 */
.filter-status-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 16px;
  background: var(--el-fill-color-light);
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.filter-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.filter-stats {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

/* 看板列容器 */
.kanban-columns-wrapper {
  display: flex;
  gap: 16px;
  padding: 16px;
  overflow-x: auto;
  flex: 1;
}

.kanban-columns-wrapper::-webkit-scrollbar {
  height: 8px;
}

.kanban-columns-wrapper::-webkit-scrollbar-track {
  background: var(--el-fill-color-lighter);
}

.kanban-columns-wrapper::-webkit-scrollbar-thumb {
  background: var(--el-border-color);
  border-radius: 4px;
}

/* 筛选菜单样式 */
.filter-header {
  font-weight: 600;
  color: var(--el-text-color-secondary);
}

.el-dropdown-menu__item.is-selected {
  background: var(--el-fill-color-light);
}

.check-icon {
  margin-left: auto;
  color: var(--el-color-primary);
}
</style>
