<template>
  <el-drawer
    :model-value="modelValue"
    :title="card?.title || '卡片详情'"
    size="500px"
    @update:model-value="handleClose"
  >
    <div v-if="card" class="card-detail">
      <!-- 卡片标题 -->
      <div class="detail-section">
        <div class="detail-label">标题</div>
        <div class="detail-value">{{ card.title }}</div>
      </div>

      <!-- 卡片描述 -->
      <div v-if="card.description" class="detail-section">
        <div class="detail-label">描述</div>
        <div class="detail-value description">{{ card.description }}</div>
      </div>

      <!-- 属性列表 -->
      <div class="detail-section">
        <div class="detail-label">属性</div>
        <div class="property-grid">
          <!-- 状态 -->
          <div class="property-item">
            <span class="property-label">状态</span>
            <el-tag :type="getStatusType(card.status)">
              {{ getStatusLabel(card.status) }}
            </el-tag>
          </div>

          <!-- 优先级 -->
          <div class="property-item">
            <span class="property-label">优先级</span>
            <el-tag :type="getPriorityType(card.priority)">
              {{ getPriorityLabel(card.priority) }}
            </el-tag>
          </div>

          <!-- 负责人 -->
          <div class="property-item">
            <span class="property-label">负责人</span>
            <div v-if="card.assigneeName" class="assignee-display">
              <el-avatar :size="24" :src="card.assigneeAvatar">
                {{ card.assigneeName.charAt(0) }}
              </el-avatar>
              <span>{{ card.assigneeName }}</span>
            </div>
            <span v-else class="text-secondary">未分配</span>
          </div>

          <!-- 截止日期 -->
          <div class="property-item">
            <span class="property-label">截止日期</span>
            <span v-if="card.dueDate" :class="getDueDateClass(card.dueDate)">
              {{ formatDueDate(card.dueDate) }}
            </span>
            <span v-else class="text-secondary">未设置</span>
          </div>

          <!-- 创建时间 -->
          <div class="property-item">
            <span class="property-label">创建时间</span>
            <span class="text-secondary">{{ formatCreatedTime(card.createdTime) }}</span>
          </div>

          <!-- 创建者 -->
          <div class="property-item">
            <span class="property-label">创建者</span>
            <span class="text-secondary">{{ card.createdBy || 'Unknown' }}</span>
          </div>
        </div>
      </div>

      <!-- 标签 -->
      <div v-if="card.tags && card.tags.length > 0" class="detail-section">
        <div class="detail-label">标签</div>
        <div class="tags-list">
          <el-tag v-for="tag in card.tags" :key="tag" size="small">
            {{ tag }}
          </el-tag>
        </div>
      </div>

      <!-- 自定义字段 -->
      <div v-if="card.customFields && Object.keys(card.customFields).length > 0" class="detail-section">
        <div class="detail-label">自定义字段</div>
        <div class="custom-fields">
          <div v-for="(value, key) in card.customFields" :key="key" class="custom-field-item">
            <span class="field-name">{{ key }}</span>
            <span class="field-value">{{ String(value) }}</span>
          </div>
        </div>
      </div>

      <!-- 活动历史 -->
      <div class="detail-section">
        <div class="detail-label">活动</div>
        <div class="activity-list">
          <div v-for="activity in activities" :key="activity.id" class="activity-item">
            <el-avatar :size="32" :src="activity.userAvatar">
              {{ activity.userName.charAt(0) }}
            </el-avatar>
            <div class="activity-content">
              <div class="activity-text">
                <strong>{{ activity.userName }}</strong>
                {{ activity.action }}
              </div>
              <div class="activity-time">{{ formatRelativeTime(activity.time) }}</div>
            </div>
          </div>
          <div v-if="activities.length === 0" class="empty-activities">
            暂无活动记录
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="drawer-footer">
        <el-button @click="handleClose">关闭</el-button>
        <el-button v-if="!readonly" type="primary" @click="handleEdit">
          编辑
        </el-button>
        <el-button v-if="!readonly" type="danger" @click="handleDelete">
          删除
        </el-button>
      </div>
    </template>
  </el-drawer>
</template>

<script setup lang="ts">
/**
 * CardDetail - 卡片详情弹窗组件
 * 显示卡片的完整信息和活动历史
 */

import { ref, type PropType } from 'vue'
import { ElMessageBox } from 'element-plus'
import type { KanbanCard, CardPriority } from '../types/kanban'

// Props
const props = defineProps({
  /** 是否显示弹窗 */
  modelValue: {
    type: Boolean,
    required: true
  },
  /** 卡片数据 */
  card: {
    type: Object as PropType<KanbanCard | null>,
    default: null
  },
  /** 是否只读模式 */
  readonly: {
    type: Boolean,
    default: false
  }
})

// Emits
const emit = defineEmits<{
  /** 更新显示状态 */
  (event: 'update:modelValue', value: boolean): void
  /** 编辑卡片 */
  (event: 'edit', card: KanbanCard): void
  /** 保存卡片 */
  (event: 'save', card: Partial<KanbanCard>): void
  /** 删除卡片 */
  (event: 'delete', card: KanbanCard): void
}>(")

// ============================================
// 状态
// ============================================

/** 活动历史 (模拟数据) */
const activities = ref([
  {
    id: '1',
    userName: '张三',
    userAvatar: '',
    action: '创建了此卡片',
    time: new Date(Date.now() - 1000 * 60 * 30) // 30分钟前
  },
  {
    id: '2',
    userName: '李四',
    userAvatar: '',
    action: '将优先级设置为 高',
    time: new Date(Date.now() - 1000 * 60 * 60 * 2) // 2小时前
  }
])

// ============================================
// 方法
// ============================================

/** 获取优先级类型 */
function getPriorityType(priority: CardPriority): 'success' | 'info' | 'warning' | 'danger' {
  const types: Record<CardPriority, 'success' | 'info' | 'warning' | 'danger'> = {
    low: 'info',
    medium: 'success',
    high: 'warning',
    urgent: 'danger'
  }
  return types[priority] || 'info'
}

/** 获取优先级标签 */
function getPriorityLabel(priority: CardPriority): string {
  const labels: Record<CardPriority, string> = {
    low: '低',
    medium: '中',
    high: '高',
    urgent: '紧急'
  }
  return labels[priority] || '中'
}

/** 获取状态类型 */
function getStatusType(status: string): string {
  // 可以根据实际状态值返回对应的 Element Plus 标签类型
  const statusMap: Record<string, string> = {
    'todo': 'info',
    'in-progress': 'warning',
    'done': 'success',
    'blocked': 'danger'
  }
  return statusMap[status] || 'info'
}

/** 获取状态标签 */
function getStatusLabel(status: string): string {
  const statusMap: Record<string, string> = {
    'todo': '待处理',
    'in-progress': '进行中',
    'done': '已完成',
    'blocked': '已阻挡'
  }
  return statusMap[status] || status
}

/** 获取截止日期样式类 */
function getDueDateClass(dueDate: Date | string): string {
  const date = new Date(dueDate)
  const today = new Date()
  today.setHours(0, 0, 0, 0)

  if (date < today) return 'text-danger'
  if (date.toDateString() === today.toDateString()) return 'text-warning'
  return ''
}

/** 格式化截止日期 */
function formatDueDate(dueDate: Date | string): string {
  const date = new Date(dueDate)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

/** 格式化创建时间 */
function formatCreatedTime(time: Date | string): string {
  const date = new Date(time)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

/** 格式化相对时间 */
function formatRelativeTime(time: Date): string {
  const now = Date.now()
  const diff = now - time.getTime()

  const minutes = Math.floor(diff / 60000)
  const hours = Math.floor(diff / 3600000)
  const days = Math.floor(diff / 86400000)

  if (minutes < 60) return `${minutes} 分钟前`
  if (hours < 24) return `${hours} 小时前`
  if (days < 7) return `${days} 天前`
  return time.toLocaleDateString('zh-CN')
}

/** 处理关闭 */
function handleClose(): void {
  emit('update:modelValue', false)
}

/** 处理编辑 */
function handleEdit(): void {
  if (props.card) {
    emit('edit', props.card)
    emit('update:modelValue', false)
  }
}

/** 处理删除 */
async function handleDelete(): Promise<void> {
  if (!props.card) return

  try {
    await ElMessageBox.confirm(
      `确定要删除卡片"${props.card.title}"吗？`,
      '确认删除',
      {
        type: 'warning',
        confirmButtonText: '删除',
        cancelButtonText: '取消'
      }
    )
    emit('delete', props.card)
    emit('update:modelValue', false)
  } catch {
    // 用户取消
  }
}
</script>

<style scoped>
.card-detail {
  padding: 0 8px;
}

/* 详情区块 */
.detail-section {
  margin-bottom: 24px;
}

.detail-label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-bottom: 8px;
  font-weight: 600;
}

.detail-value {
  color: var(--el-text-color-primary);
  line-height: 1.6;
}

.detail-value.description {
  white-space: pre-wrap;
  padding: 12px;
  background: var(--el-fill-color-light);
  border-radius: 4px;
}

/* 属性网格 */
.property-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16px;
}

.property-item {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.property-label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.assignee-display {
  display: flex;
  align-items: center;
  gap: 8px;
}

/* 文本颜色 */
.text-secondary {
  color: var(--el-text-color-secondary);
}

.text-danger {
  color: var(--el-color-danger);
}

.text-warning {
  color: var(--el-color-warning);
}

/* 标签列表 */
.tags-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

/* 自定义字段 */
.custom-fields {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.custom-field-item {
  display: flex;
  justify-content: space-between;
  padding: 8px 12px;
  background: var(--el-fill-color-light);
  border-radius: 4px;
}

.field-name {
  font-size: 13px;
  color: var(--el-text-color-regular);
}

.field-value {
  font-size: 13px;
  color: var(--el-text-color-primary);
  font-weight: 500;
}

/* 活动列表 */
.activity-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.activity-item {
  display: flex;
  gap: 12px;
}

.activity-content {
  flex: 1;
}

.activity-text {
  font-size: 14px;
  color: var(--el-text-color-primary);
  line-height: 1.5;
}

.activity-time {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
}

.empty-activities {
  text-align: center;
  padding: 24px;
  color: var(--el-text-color-placeholder);
  font-size: 13px;
}

/* 底部操作栏 */
.drawer-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>
