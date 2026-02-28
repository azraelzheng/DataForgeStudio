<template>
  <div
    :class="columnClasses"
    @dragover.prevent="handleDragOver"
    @dragleave="handleDragLeave"
    @drop="handleDrop"
  >
    <!-- 列头 -->
    <div class="column-header">
      <div class="column-title-row">
        <div class="column-title">
          <span v-if="column.color" class="status-dot" :style="{ background: column.color }"></span>
          <span>{{ column.title }}</span>
          <span class="card-count">{{ cardCount }}</span>
        </div>

        <!-- 列操作菜单 -->
        <el-dropdown trigger="click" @command="handleCommand">
          <el-icon class="more-icon"><MoreFilled /></el-icon>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="addCard">添加卡片</el-dropdown-item>
              <el-dropdown-item command="editColumn">编辑列</el-dropdown-item>
              <el-dropdown-item command="clearColumn" :disabled="cards.length === 0">
                清空列
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>

      <!-- WIP 限制指示器 -->
      <div v-if="column.wipLimit" :class="['wip-indicator', wipStatus.class]">
        <el-icon v-if="wipStatus.isExceeded"><Warning /></el-icon>
        <span>{{ cardCount }} / {{ column.wipLimit }}</span>
      </div>
    </div>

    <!-- 卡片列表 -->
    <div class="column-body" ref="columnBodyRef">
      <!-- 放置占位符 -->
      <div
        v-if="isDragOver"
        class="drop-placeholder"
        :style="{ top: placeholderPosition + 'px' }"
      >
        <div class="placeholder-line"></div>
      </div>

      <!-- 卡片 -->
      <KanbanCard
        v-for="(card, index) in cards"
        :key="card.id"
        :card="card"
        :compact="compact"
        :readonly="readonly"
        :show-priority="showPriority"
        @dragstart="handleCardDragStart"
        @dragend="handleCardDragEnd"
        @click="handleCardClick"
      />

      <!-- 空状态 -->
      <div v-if="cards.length === 0" class="empty-state">
        <el-icon><Plus /></el-icon>
        <span>拖拽卡片到此处</span>
      </div>

      <!-- 快速添加按钮 -->
      <el-button
        v-if="!readonly && showQuickAdd"
        class="quick-add-btn"
        text
        @click="handleQuickAdd"
      >
        <el-icon><Plus /></el-icon>
        添加卡片
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * KanbanColumn - 看板列组件
 * 显示单个状态列，包含卡片列表和 WIP 限制
 */

import { ref, computed, nextTick, type PropType } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { MoreFilled, Warning, Plus } from '@element-plus/icons-vue'
import KanbanCard from './KanbanCard.vue'
import type { KanbanColumn as KanbanColumnType, KanbanCard, WipLimitStatus } from '../types/kanban'

// Props
const props = defineProps({
  /** 列配置 */
  column: {
    type: Object as PropType<KanbanColumnType>,
    required: true
  },
  /** 列中的卡片 */
  cards: {
    type: Array as PropType<KanbanCard[]>,
    default: () => []
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
  },
  /** 是否显示快速添加按钮 */
  showQuickAdd: {
    type: Boolean,
    default: true
  }
})

// Emits
const emit = defineEmits<{
  /** 卡片拖拽开始 */
  (event: 'cardDragStart', card: KanbanCard, columnId: string): void
  /** 卡片拖拽结束 */
  (event: 'cardDragEnd', card: KanbanCard): void
  /** 卡片放置 */
  (event: 'cardDrop', card: KanbanCard, toColumnId: string, toIndex: number): void
  /** 点击卡片 */
  (event: 'cardClick', card: KanbanCard): void
  /** 添加卡片 */
  (event: 'addCard', columnId: string): void
  /** 编辑列 */
  (event: 'editColumn', column: KanbanColumnType): void
  /** 清空列 */
  (event: 'clearColumn', columnId: string): void
}>(")

// ============================================
// 状态
// ============================================

const columnBodyRef = ref<HTMLElement>()
const isDragOver = ref(false)
const placeholderPosition = ref(0)

// ============================================
// 计算属性
// ============================================

/** 列样式类 */
const columnClasses = computed(() => ({
  'kanban-column': true,
  'is-compact': props.compact,
  'is-readonly': props.readonly,
  'is-drag-over': isDragOver.value,
  'has-wip-limit': !!props.column.wipLimit
}))

/** 卡片数量 */
const cardCount = computed(() => props.cards.length)

/** WIP 限制状态 */
const wipStatus = computed((): WipLimitStatus => {
  if (!props.column.wipLimit) {
    return { isExceeded: false, current: cardCount.value, limit: 0, isNearLimit: false }
  }

  const limit = props.column.wipLimit
  const current = cardCount.value
  const ratio = current / limit

  return {
    isExceeded: current > limit,
    current,
    limit,
    isNearLimit: ratio >= 0.8
  }
})

// ============================================
// 拖拽处理
// ============================================

/** 处理拖拽悬停 */
function handleDragOver(event: DragEvent): void {
  if (!columnBodyRef.value || props.readonly) return

  isDragOver.value = true

  // 计算放置位置
  const rect = columnBodyRef.value.getBoundingClientRect()
  const scrollTop = columnBodyRef.value.scrollTop
  const cards = columnBodyRef.value.querySelectorAll('.kanban-card')
  const offsetY = event.clientY - rect.top + scrollTop

  let position = offsetY
  for (const card of cards) {
    const cardRect = card.getBoundingClientRect()
    const cardTop = cardRect.top - rect.top + scrollTop
    if (offsetY < cardTop + cardRect.height / 2) {
      position = cardTop
      break
    }
  }

  placeholderPosition.value = position
}

/** 处理拖拽离开 */
function handleDragLeave(event: DragEvent): void {
  const rect = columnBodyRef.value?.getBoundingClientRect()
  if (rect) {
    const { clientX, clientY } = event
    if (
      clientX < rect.left ||
      clientX > rect.right ||
      clientY < rect.top ||
      clientY > rect.bottom
    ) {
      isDragOver.value = false
    }
  }
}

/** 处理放置 */
function handleDrop(event: DragEvent): void {
  event.preventDefault()
  isDragOver.value = false

  if (props.readonly) return

  const cardId = event.dataTransfer?.getData('text/plain')
  if (!cardId) return

  // 计算插入位置索引
  const cards = columnBodyRef.value?.querySelectorAll('.kanban-card')
  let insertIndex = props.cards.length

  if (cards && cards.length > 0) {
    for (let i = 0; i < cards.length; i++) {
      const cardRect = cards[i].getBoundingClientRect()
      if (event.clientY < cardRect.top + cardRect.height / 2) {
        insertIndex = i
        break
      }
    }
  }

  // 触发放置事件 (由父组件处理实际逻辑)
  emit('cardDrop', { id: cardId } as KanbanCard, props.column.id, insertIndex)
}

// ============================================
// 卡片事件
// ============================================

/** 处理卡片拖拽开始 */
function handleCardDragStart(card: KanbanCard): void {
  emit('cardDragStart', card, props.column.id)
}

/** 处理卡片拖拽结束 */
function handleCardDragEnd(card: KanbanCard): void {
  isDragOver.value = false
  emit('cardDragEnd', card)
}

/** 处理卡片点击 */
function handleCardClick(card: KanbanCard): void {
  emit('cardClick', card)
}

// ============================================
// 列操作
// ============================================

/** 处理命令菜单 */
function handleCommand(command: string): void {
  switch (command) {
    case 'addCard':
      handleQuickAdd()
      break
    case 'editColumn':
      emit('editColumn', props.column)
      break
    case 'clearColumn':
      handleClearColumn()
      break
  }
}

/** 快速添加卡片 */
function handleQuickAdd(): void {
  emit('addCard', props.column.id)
}

/** 清空列 */
async function handleClearColumn(): Promise<void> {
  try {
    await ElMessageBox.confirm(
      `确定要清空"${props.column.title}"列中的所有卡片吗？`,
      '确认操作',
      {
        type: 'warning',
        confirmButtonText: '确定',
        cancelButtonText: '取消'
      }
    )
    emit('clearColumn', props.column.id)
  } catch {
    // 用户取消
  }
}

// 监听 WIP 限制超出警告
import { watch } from 'vue'
watch(wipStatus, (newStatus) => {
  if (newStatus.isExceeded) {
    ElMessage.warning(
      `"${props.column.title}" 列的卡片数量 (${newStatus.current}) 已超过 WIP 限制 (${newStatus.limit})`
    )
  }
}, { immediate: false })
</script>

<style scoped>
.kanban-column {
  display: flex;
  flex-direction: column;
  background: var(--el-fill-color-lighter);
  border-radius: 8px;
  min-width: 280px;
  max-width: 400px;
  height: 100%;
}

.kanban-column.is-compact {
  min-width: 220px;
  max-width: 300px;
}

.kanban-column.is-drag-over {
  background: var(--el-color-primary-light-9);
}

/* 列头 */
.column-header {
  padding: 12px;
  border-bottom: 1px solid var(--el-border-color-light);
}

.column-title-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.column-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.card-count {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  background: var(--el-fill-color);
  padding: 2px 8px;
  border-radius: 12px;
}

.more-icon {
  font-size: 16px;
  color: var(--el-text-color-secondary);
  cursor: pointer;
}

.more-icon:hover {
  color: var(--el-text-color-primary);
}

/* WIP 指示器 */
.wip-indicator {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  padding: 4px 8px;
  border-radius: 4px;
  background: var(--el-fill-color);
  color: var(--el-text-color-regular);
}

.wip-indicator.isNearLimit {
  background: var(--el-color-warning-light-9);
  color: var(--el-color-warning);
}

.wip-indicator.isExceeded {
  background: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
}

/* 列体 */
.column-body {
  flex: 1;
  padding: 8px;
  overflow-y: auto;
  position: relative;
}

.column-body::-webkit-scrollbar {
  width: 4px;
}

.column-body::-webkit-scrollbar-track {
  background: transparent;
}

.column-body::-webkit-scrollbar-thumb {
  background: var(--el-border-color);
  border-radius: 2px;
}

/* 放置占位符 */
.drop-placeholder {
  position: absolute;
  left: 8px;
  right: 8px;
  height: 2px;
  pointer-events: none;
  z-index: 10;
}

.placeholder-line {
  height: 100%;
  background: var(--el-color-primary);
  border-radius: 1px;
}

/* 空状态 */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 32px 16px;
  color: var(--el-text-color-placeholder);
  font-size: 12px;
}

.empty-state .el-icon {
  font-size: 24px;
  margin-bottom: 8px;
}

/* 快速添加按钮 */
.quick-add-btn {
  width: 100%;
  margin-top: 8px;
  color: var(--el-text-color-secondary);
  border: 1px dashed var(--el-border-color);
}

.quick-add-btn:hover {
  color: var(--el-color-primary);
  border-color: var(--el-color-primary);
}
</style>
