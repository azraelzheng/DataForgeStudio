<template>
  <el-dialog
    :model-value="modelValue"
    :title="card?.id ? '编辑卡片' : '添加卡片'"
    width="600px"
    @update:model-value="handleClose"
  >
    <el-form
      ref="formRef"
      :model="formData"
      :rules="rules"
      label-width="100px"
      @submit.prevent="handleSubmit"
    >
      <!-- 标题 -->
      <el-form-item label="标题" prop="title">
        <el-input
          v-model="formData.title"
          placeholder="请输入卡片标题"
          maxlength="100"
          show-word-limit
        />
      </el-form-item>

      <!-- 描述 -->
      <el-form-item label="描述" prop="description">
        <el-input
          v-model="formData.description"
          type="textarea"
          :rows="4"
          placeholder="请输入卡片描述"
          maxlength="1000"
          show-word-limit
        />
      </el-form-item>

      <!-- 状态 -->
      <el-form-item label="状态" prop="status">
        <el-select v-model="formData.status" placeholder="请选择状态">
          <el-option
            v-for="column in columns"
            :key="column.id"
            :label="column.title"
            :value="column.id"
          >
            <div class="status-option">
              <span
                v-if="column.color"
                class="status-color"
                :style="{ background: column.color }"
              ></span>
              {{ column.title }}
            </div>
          </el-option>
        </el-select>
      </el-form-item>

      <!-- 优先级 -->
      <el-form-item label="优先级" prop="priority">
        <el-radio-group v-model="formData.priority">
          <el-radio-button label="low">低</el-radio-button>
          <el-radio-button label="medium">中</el-radio-button>
          <el-radio-button label="high">高</el-radio-button>
          <el-radio-button label="urgent">紧急</el-radio-button>
        </el-radio-group>
      </el-form-item>

      <!-- 负责人 -->
      <el-form-item label="负责人" prop="assigneeId">
        <el-select
          v-model="formData.assigneeId"
          placeholder="请选择负责人"
          clearable
          filterable
        >
          <el-option
            v-for="assignee in availableAssignees"
            :key="assignee.id"
            :label="assignee.name"
            :value="assignee.id"
          >
            <div class="assignee-option">
              <el-avatar :size="24" :src="assignee.avatar">
                {{ assignee.name.charAt(0) }}
              </el-avatar>
              <span>{{ assignee.name }}</span>
            </div>
          </el-option>
        </el-select>
      </el-form-item>

      <!-- 截止日期 -->
      <el-form-item label="截止日期" prop="dueDate">
        <el-date-picker
          v-model="formData.dueDate"
          type="datetime"
          placeholder="请选择截止日期"
          format="YYYY-MM-DD HH:mm"
          value-format="YYYY-MM-DD HH:mm:ss"
          :disabled-date="disabledDate"
        />
      </el-form-item>

      <!-- 标签 -->
      <el-form-item label="标签" prop="tags">
        <div class="tags-input">
          <el-tag
            v-for="tag in formData.tags"
            :key="tag"
            closable
            @close="removeTag(tag)"
          >
            {{ tag }}
          </el-tag>
          <el-input
            v-if="inputTagVisible"
            ref="inputTagRef"
            v-model="inputTagValue"
            size="small"
            style="width: 100px"
            @blur="handleInputTagConfirm"
            @keyup.enter="handleInputTagConfirm"
          />
          <el-button
            v-else
            size="small"
            @click="showInputTag"
          >
            + 添加标签
          </el-button>
        </div>
      </el-form-item>

      <!-- 自定义字段 -->
      <template v-if="customFieldsConfig.length > 0">
        <el-divider>自定义字段</el-divider>
        <el-form-item
          v-for="field in customFieldsConfig"
          :key="field.name"
          :label="field.label"
          :prop="`customFields.${field.name}`"
        >
          <!-- 文本输入 -->
          <el-input
            v-if="field.type === 'text'"
            v-model="formData.customFields![field.name]"
            :placeholder="`请输入${field.label}`"
          />
          <!-- 数字输入 -->
          <el-input-number
            v-else-if="field.type === 'number'"
            v-model="formData.customFields![field.name] as number"
            :placeholder="`请输入${field.label}`"
          />
          <!-- 日期选择 -->
          <el-date-picker
            v-else-if="field.type === 'date'"
            v-model="formData.customFields![field.name]"
            type="date"
            :placeholder="`请选择${field.label}`"
          />
          <!-- 下拉选择 -->
          <el-select
            v-else-if="field.type === 'select'"
            v-model="formData.customFields![field.name]"
            :placeholder="`请选择${field.label}`"
          >
            <el-option
              v-for="option in field.options"
              :key="option.value"
              :label="option.label"
              :value="option.value"
            />
          </el-select>
        </el-form-item>
      </template>
    </el-form>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleClose">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">
          保存
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
/**
 * CardForm - 卡片编辑表单组件
 * 用于新建或编辑看板卡片
 */

import { ref, reactive, watch, nextTick, type PropType } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import type {
  KanbanCard,
  KanbanColumn,
  CardFormData,
  CardPriority
} from '../types/kanban'

// Props
const props = defineProps({
  /** 是否显示对话框 */
  modelValue: {
    type: Boolean,
    required: true
  },
  /** 卡片数据 (编辑模式) */
  card: {
    type: Object as PropType<Partial<KanbanCard> | null>,
    default: null
  },
  /** 看板列配置 */
  columns: {
    type: Array as PropType<KanbanColumn[]>,
    default: () => []
  }
})

// Emits
const emit = defineEmits<{
  /** 更新显示状态 */
  (event: 'update:modelValue', value: boolean): void
  /** 保存卡片 */
  (event: 'save', card: Partial<KanbanCard>): void
}>(")

// ============================================
// 状态
// ============================================

const formRef = ref<FormInstance>()
const submitting = ref(false)

// 表单数据
const formData = reactive<CardFormData>({
  title: '',
  description: '',
  status: '',
  priority: 'medium',
  assigneeId: undefined,
  dueDate: undefined,
  tags: [],
  customFields: {}
})

// 表单验证规则
const rules: FormRules = {
  title: [
    { required: true, message: '请输入卡片标题', trigger: 'blur' },
    { min: 1, max: 100, message: '标题长度为 1-100 个字符', trigger: 'blur' }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ],
  priority: [
    { required: true, message: '请选择优先级', trigger: 'change' }
  ]
}

// 标签输入
const inputTagVisible = ref(false)
const inputTagValue = ref('')
const inputTagRef = ref()

// 自定义字段配置 (可以从 props 或 API 获取)
const customFieldsConfig = ref([
  // 示例配置
  // { name: 'estimate', label: '预估工时', type: 'number' },
  // { name: 'category', label: '分类', type: 'select', options: [...] }
])

// 模拟可用负责人 (实际应从 API 获取)
const availableAssignees = ref([
  { id: '1', name: '张三', avatar: '' },
  { id: '2', name: '李四', avatar: '' },
  { id: '3', name: '王五', avatar: '' }
])

// ============================================
// 方法
// ============================================

/** 禁用过去的日期 */
function disabledDate(time: Date): boolean {
  // 可选择今天及以后的日期
  return time.getTime() < Date.now() - 86400000
}

/** 重置表单 */
function resetForm(): void {
  formRef.value?.resetFields()
  Object.assign(formData, {
    title: '',
    description: '',
    status: props.columns[0]?.id || '',
    priority: 'medium' as CardPriority,
    assigneeId: undefined,
    dueDate: undefined,
    tags: [],
    customFields: {}
  })
}

/** 加载卡片数据 */
function loadCardData(card: Partial<KanbanCard>): void {
  Object.assign(formData, {
    title: card.title || '',
    description: card.description || '',
    status: card.status || props.columns[0]?.id || '',
    priority: card.priority || 'medium',
    assigneeId: card.assigneeId,
    dueDate: card.dueDate ? new Date(card.dueDate) : undefined,
    tags: card.tags ? [...card.tags] : [],
    customFields: card.customFields ? { ...card.customFields } : {}
  })
}

/** 移除标签 */
function removeTag(tag: string): void {
  const index = formData.tags?.indexOf(tag)
  if (index !== undefined && index > -1) {
    formData.tags?.splice(index, 1)
  }
}

/** 显示标签输入框 */
function showInputTag(): void {
  inputTagVisible.value = true
  nextTick(() => {
    inputTagRef.value?.focus()
  })
}

/** 确认添加标签 */
function handleInputTagConfirm(): void {
  const value = inputTagValue.value.trim()
  if (value) {
    if (!formData.tags) {
      formData.tags = []
    }
    if (!formData.tags.includes(value)) {
      formData.tags.push(value)
    }
  }
  inputTagVisible.value = false
  inputTagValue.value = ''
}

/** 处理关闭 */
function handleClose(): void {
  emit('update:modelValue', false)
  resetForm()
}

/** 处理提交 */
async function handleSubmit(): Promise<void> {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
    submitting.value = true

    // 构建卡片数据
    const cardData: Partial<KanbanCard> = {
      ...formData,
      id: props.card?.id
    }

    emit('save', cardData)
    handleClose()
  } catch (error) {
    console.error('表单验证失败:', error)
  } finally {
    submitting.value = false
  }
}

// ============================================
// 监听
// ============================================

// 监听 dialog 显示状态
watch(() => props.modelValue, (visible) => {
  if (visible) {
    if (props.card?.id) {
      loadCardData(props.card)
    } else {
      resetForm()
    }
  }
})

// 监听卡片数据变化
watch(() => props.card, (card) => {
  if (card && props.modelValue) {
    loadCardData(card)
  }
}, { deep: true })
</script>

<style scoped>
.status-option {
  display: flex;
  align-items: center;
  gap: 8px;
}

.status-color {
  width: 12px;
  height: 12px;
  border-radius: 50%;
}

.assignee-option {
  display: flex;
  align-items: center;
  gap: 8px;
}

.tags-input {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>
