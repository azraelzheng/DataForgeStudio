<template>
  <div
    :class="cardClasses"
    :draggable="!readonly"
    @dragstart="handleDragStart"
    @dragend="handleDragEnd"
    @click="handleClick"
  >
    <!-- 优先级指示条 -->
    <div v-if="showPriority" :class="['priority-bar', `priority-${card.priority}`]"></div>

    <!-- 卡片内容 -->
    <div class="card-content">
      <!-- 卡片头部：标题和标签 -->
      <div class="card-header">
        <div class="card-title">{{ card.title }}</div>
        <el-tag v-if="showPriority" :type="priorityType" size="small">
          {{ priorityLabel }}
        </el-tag>
      </div>

      <!-- 卡片描述 -->
      <div v-if="!compact && card.description" class="card-description">
        {{ truncatedDescription }}
      </div>

      <!-- 标签列表 -->
      <div v-if="!compact && card.tags && card.tags.length > 0" class="card-tags">
        <el-tag
          v-for="tag in card.tags.slice(0, 3)"
          :key="tag"
          size="small"
          class="tag-item"
        >
          {{ tag }}
        </el-tag>
        <span v-if="card.tags.length > 3" class="more-tags">
          +{{ card.tags.length - 3 }}
        </span>
      </div>

      <!-- 卡片底部：负责人和截止日期 -->
      <div class="card-footer">
        <!-- 负责人 -->
        <div v-if="card.assigneeName" class="card-assignee">
          <el-avatar :size="24" :src="card.assigneeAvatar">
            {{ card.assigneeName?.charAt(0) }}
          </el-avatar>
          <span class="assignee-name">{{ card.assigneeName }}</span>
        </div>

        <!-- 截止日期 -->
        <div v-if="card.dueDate" :class="['card-due-date', dueDateClass]">
          <el-icon><Calendar /></el-icon>
          <span>{{ formattedDueDate }}</span>
        </div>
      </div>

      <!-- 附件和评论计数 -->
      <div v-if="!compact && (card.attachmentCount || card.commentCount)" class="card-meta">
        <span v-if="card.attachmentCount" class="meta-item">
          <el-icon><Paperclip /></el-icon>
          {{ card.attachmentCount }}
        </span>
        <span v-if="card.commentCount" class="meta-item">
          <el-icon><ChatDotRound /></el-icon>
          {{ card.commentCount }}
        </span>
      </div>
    </div>

    <!-- 阻挡标记 -->
    <div v-if="isBlocked" class="blocked-indicator">
      <el-tooltip content="任务被阻挡">
        <el-icon color="red"><Warning /></el-icon>
      </el-tooltip>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * KanbanCard - 看板卡片组件
 * 显示单个任务卡片，支持拖拽和点击
 */

import { computed, type PropType } from 'vue'
import { Calendar, Paperclip, ChatDotRound, Warning } from '@element-plus/icons-vue'
import type { KanbanCard, CardPriority } from '../types/kanban'

// Props
const props = defineProps({
  /** 卡片数据 */
  card: {
    type: Object as PropType<KanbanCard>,
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
  },
  /** 是否被阻挡 */
  isBlocked: {
    type: Boolean,
    default: false
  }
})

// Emits
const emit = defineEmits<{
  /** 拖拽开始 */
  (event: 'dragstart', card: KanbanCard): void
  /** 拖拽结束 */
  (event: 'dragend', card: KanbanCard): void
  /** 点击卡片 */
  (event: 'click', card: KanbanCard): void
}>")

// ============================================
// 计算属性
// ============================================

/** 卡片样式类 */
const cardClasses = computed(() => ({
  'kanban-card': true,
  'is-compact': props.compact,
  'is-readonly': props.readonly,
  'is-overdue': isOverdue.value,
  'is-due-today': isDueToday.value,
  'is-due-soon': isDueSoon.value,
  [`priority-${props.card.priority}`]: true
}))

/** 优先级类型 (Element Plus tag 类型) */
const priorityType = computed(() => {
  const types: Record<CardPriority, 'success' | 'info' | 'warning' | 'danger'> = {
    low: 'info',
    medium: 'success',
    high: 'warning',
    urgent: 'danger'
  }
  return types[props.card.priority] || 'info'
})

/** 优先级标签 */
const priorityLabel = computed(() => {
  const labels: Record<CardPriority, string> = {
    low: '低',
    medium: '中',
    high: '高',
    urgent: '紧急'
  }
  return labels[props.card.priority] || '中'
})

/** 截止日期格式化 */
const formattedDueDate = computed(() => {
  if (!props.card.dueDate) return ''

  const date = new Date(props.card.dueDate)
  const today = new Date()
  const tomorrow = new Date(today)
  tomorrow.setDate(tomorrow.getDate() + 1)

  if (date.toDateString() === today.toDateString()) {
    return '今天'
  } else if (date.toDateString() === tomorrow.toDateString()) {
    return '明天'
  } else {
    return `${date.getMonth() + 1}/${date.getDate()}`
  }
})

/** 是否逾期 */
const isOverdue = computed(() => {
  if (!props.card.dueDate) return false
  const dueDate = new Date(props.card.dueDate)
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  return dueDate < today
})

/** 是否今天到期 */
const isDueToday = computed(() => {
  if (!props.card.dueDate) return false
  const dueDate = new Date(props.card.dueDate)
  const today = new Date()
  return dueDate.toDateString() === today.toDateString()
})

/** 是否即将到期 (3天内) */
const isDueSoon = computed(() => {
  if (!props.card.dueDate) return false
  const dueDate = new Date(props.card.dueDate)
  const today = new Date()
  const threeDaysLater = new Date(today)
  threeDaysLater.setDate(threeDaysLater.getDate() + 3)
  threeDaysLater.setHours(23, 59, 59, 999)
  return dueDate > today && dueDate <= threeDaysLater
})

/** 截止日期样式类 */
const dueDateClass = computed(() => ({
  'is-overdue': isOverdue.value,
  'is-due-today': isDueToday.value,
  'is-due-soon': isDueSoon.value
}))

/** 截断的描述文本 */
const truncatedDescription = computed(() => {
  if (!props.card.description) return ''
  const maxLength = props.compact ? 50 : 100
  return props.card.description.length > maxLength
    ? props.card.description.slice(0, maxLength) + '...'
    : props.card.description
})

// ============================================
// 事件处理
// ============================================

/** 处理拖拽开始 */
function handleDragStart(event: DragEvent): void {
  if (!event.dataTransfer) return

  // 设置拖拽数据
  event.dataTransfer.effectAllowed = 'move'
  event.dataTransfer.setData('text/plain', props.card.id)

  // 添加拖拽样式
  if (event.target instanceof HTMLElement) {
    event.target.classList.add('is-dragging')
  }

  emit('dragstart', props.card)
}

/** 处理拖拽结束 */
function handleDragEnd(event: DragEvent): void {
  // 移除拖拽样式
  if (event.target instanceof HTMLElement) {
    event.target.classList.remove('is-dragging')
  }

  emit('dragend', props.card)
}

/** 处理点击 */
function handleClick(): void {
  if (!props.readonly) {
    emit('click', props.card)
  }
}
</script>

<style scoped>
.kanban-card {
  position: relative;
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color);
  border-radius: 6px;
  padding: 12px;
  margin-bottom: 8px;
  cursor: grab;
  transition: all 0.2s;
  user-select: none;
}

.kanban-card:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

.kanban-card.is-compact {
  padding: 8px;
  margin-bottom: 4px;
}

.kanban-card.is-readonly {
  cursor: pointer;
}

.kanban-card.is-dragging {
  opacity: 0.5;
  cursor: grabbing;
}

/* 优先级指示条 */
.priority-bar {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 3px;
  border-radius: 6px 0 0 6px;
}

.priority-bar.priority-low { background: #909399; }
.priority-bar.priority-medium { background: #67c23a; }
.priority-bar.priority-high { background: #e6a23c; }
.priority-bar.priority-urgent { background: #f56c6c; }

/* 卡片内容 */
.card-content {
  margin-left: 6px;
}

/* 卡片头部 */
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 8px;
  margin-bottom: 8px;
}

.card-title {
  flex: 1;
  font-weight: 500;
  color: var(--el-text-color-primary);
  word-break: break-word;
  line-height: 1.4;
}

/* 卡片描述 */
.card-description {
  font-size: 12px;
  color: var(--el-text-color-regular);
  margin-bottom: 8px;
  line-height: 1.4;
}

.is-compact .card-description {
  display: none;
}

/* 标签列表 */
.card-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin-bottom: 8px;
}

.tag-item {
  font-size: 11px;
}

.more-tags {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  padding: 2px 6px;
  background: var(--el-fill-color-light);
  border-radius: 4px;
}

/* 卡片底部 */
.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
}

.card-assignee {
  display: flex;
  align-items: center;
  gap: 6px;
}

.assignee-name {
  font-size: 12px;
  color: var(--el-text-color-regular);
}

.card-due-date {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.card-due-date.is-overdue {
  color: var(--el-color-danger);
}

.card-due-date.is-due-today {
  color: var(--el-color-warning);
}

.card-due-date.is-due-soon {
  color: var(--el-color-warning);
}

/* 附件和评论 */
.card-meta {
  display: flex;
  gap: 12px;
  margin-top: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 4px;
}

/* 阻挡指示器 */
.blocked-indicator {
  position: absolute;
  top: 8px;
  right: 8px;
}

/* 优先级样式 (无指示条时) */
.kanban-card.priority-low { border-left: 3px solid #909399; }
.kanban-card.priority-medium { border-left: 3px solid #67c23a; }
.kanban-card.priority-high { border-left: 3px solid #e6a23c; }
.kanban-card.priority-urgent { border-left: 3px solid #f56c6c; }
</style>
