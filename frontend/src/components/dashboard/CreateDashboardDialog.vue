<template>
  <el-dialog
    v-model="visible"
    title="新建大屏"
    width="700px"
    :close-on-click-modal="false"
    @closed="handleReset"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
      <!-- 大屏名称 -->
      <el-form-item label="大屏名称" prop="name">
        <el-input v-model="form.name" placeholder="请输入大屏名称" maxlength="50" />
      </el-form-item>

      <!-- 大屏描述 -->
      <el-form-item label="描述">
        <el-input
          v-model="form.description"
          type="textarea"
          :rows="2"
          placeholder="请输入描述（可选）"
          maxlength="200"
        />
      </el-form-item>

      <!-- 模板选择 -->
      <el-form-item label="选择模板">
        <div class="template-grid">
          <div
            v-for="tpl in templates"
            :key="tpl.id"
            class="template-card"
            :class="{ active: form.templateId === tpl.id }"
            @click="form.templateId = tpl.id"
          >
            <div class="template-icon">{{ tpl.icon }}</div>
            <div class="template-name">{{ tpl.name }}</div>
          </div>
        </div>
      </el-form-item>

      <!-- 画布尺寸 -->
      <el-form-item label="画布尺寸">
        <el-radio-group v-model="form.sizePreset" @change="handleSizePresetChange">
          <el-radio value="1920x1080">1920x1080</el-radio>
          <el-radio value="2560x1440">2560x1440</el-radio>
          <el-radio value="3840x2160">3840x2160</el-radio>
          <el-radio value="custom">自定义</el-radio>
        </el-radio-group>
      </el-form-item>

      <el-form-item v-if="form.sizePreset === 'custom'" label="自定义尺寸">
        <el-col :span="11">
          <el-input-number
            v-model="form.width"
            :min="800"
            :max="7680"
            placeholder="宽度"
            style="width: 100%"
          />
        </el-col>
        <el-col :span="2" style="text-align: center; line-height: 32px;">x</el-col>
        <el-col :span="11">
          <el-input-number
            v-model="form.height"
            :min="600"
            :max="4320"
            placeholder="高度"
            style="width: 100%"
          />
        </el-col>
      </el-form-item>

      <!-- 主题选择 -->
      <el-form-item label="主题风格">
        <el-radio-group v-model="form.theme">
          <el-radio value="dark">
            <el-icon><Moon /></el-icon> 深色
          </el-radio>
          <el-radio value="light">
            <el-icon><Sunny /></el-icon> 浅色
          </el-radio>
        </el-radio-group>
      </el-form-item>

      <!-- 背景颜色 -->
      <el-form-item label="背景颜色">
        <el-color-picker v-model="form.backgroundColor" show-alpha />
        <el-input
          v-model="form.backgroundColor"
          style="width: 150px; margin-left: 8px"
          placeholder="#0a1628"
        />
      </el-form-item>

      <!-- 刷新间隔 -->
      <el-form-item label="刷新间隔">
        <el-select v-model="form.refreshInterval" style="width: 200px">
          <el-option :value="0" label="不刷新" />
          <el-option :value="10" label="10 秒" />
          <el-option :value="30" label="30 秒" />
          <el-option :value="60" label="1 分钟" />
          <el-option :value="300" label="5 分钟" />
        </el-select>
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="visible = false">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="loading">创建大屏</el-button>
    </template>
  </el-dialog>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Moon, Sunny } from '@element-plus/icons-vue'
import { createDashboard } from '../../api/dashboard'

const props = defineProps({
  modelValue: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'success'])

const router = useRouter()
const formRef = ref(null)
const loading = ref(false)

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

// 预置模板列表
const templates = [
  { id: 'blank', name: '空白', icon: '📄' },
  { id: 'production', name: '生产监控', icon: '🏭' },
  { id: 'quality', name: '质检看板', icon: '✓' },
  { id: 'process', name: '工序进度', icon: '📊' },
  { id: 'order', name: '订单进度', icon: '📦' },
  { id: 'equipment', name: '设备状态', icon: '⚙' }
]

// 表单数据
const form = reactive({
  name: '',
  description: '',
  templateId: 'blank',
  sizePreset: '1920x1080',
  width: 1920,
  height: 1080,
  theme: 'dark',
  backgroundColor: '#0a1628',
  refreshInterval: 30
})

// 表单验证规则
const rules = {
  name: [
    { required: true, message: '请输入大屏名称', trigger: 'blur' },
    { min: 2, max: 50, message: '长度在 2 到 50 个字符', trigger: 'blur' }
  ]
}

// 监听主题变化，自动调整背景颜色
watch(() => form.theme, (newTheme) => {
  if (newTheme === 'dark' && form.backgroundColor === '#ffffff') {
    form.backgroundColor = '#0a1628'
  } else if (newTheme === 'light' && form.backgroundColor === '#0a1628') {
    form.backgroundColor = '#ffffff'
  }
})

// 处理预设尺寸变化
const handleSizePresetChange = (val) => {
  if (val !== 'custom') {
    const [width, height] = val.split('x').map(Number)
    form.width = width
    form.height = height
  }
}

// 重置表单
const handleReset = () => {
  formRef.value?.resetFields()
  form.name = ''
  form.description = ''
  form.templateId = 'blank'
  form.sizePreset = '1920x1080'
  form.width = 1920
  form.height = 1080
  form.theme = 'dark'
  form.backgroundColor = '#0a1628'
  form.refreshInterval = 30
}

// 提交创建
const handleSubmit = async () => {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
  } catch {
    return
  }

  loading.value = true
  try {
    const data = {
      name: form.name,
      description: form.description,
      width: form.width,
      height: form.height,
      backgroundColor: form.backgroundColor,
      settings: {
        theme: form.theme,
        refreshInterval: form.refreshInterval,
        templateId: form.templateId
      }
    }

    const res = await createDashboard(data)
    if (res.success) {
      ElMessage.success('创建成功')
      visible.value = false
      // 发送成功事件
      emit('success', res.data)
      // 跳转到设计器页面
      const dashboardId = res.data?.dashboardId || res.data?.id
      if (dashboardId) {
        router.push(`/dashboard/designer/${dashboardId}`)
      }
    } else {
      ElMessage.error(res.message || '创建失败')
    }
  } catch {
    ElMessage.error('创建失败')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.template-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 12px;
  width: 100%;
}

.template-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 16px 8px;
  border: 2px solid #e4e7ed;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
  background-color: #fff;
}

.template-card:hover {
  border-color: #409eff;
  background-color: #f5f7fa;
}

.template-card.active {
  border-color: #409eff;
  background-color: #ecf5ff;
}

.template-icon {
  font-size: 28px;
  margin-bottom: 8px;
  line-height: 1;
}

.template-name {
  font-size: 12px;
  color: #606266;
  text-align: center;
}

.template-card.active .template-name {
  color: #409eff;
  font-weight: 500;
}

/* 深色主题下的样式调整 */
@media (prefers-color-scheme: dark) {
  .template-card {
    background-color: #1d1d1d;
    border-color: #4c4d4f;
  }

  .template-card:hover {
    background-color: #2d2d2d;
  }

  .template-card.active {
    background-color: #1a3a5c;
  }

  .template-name {
    color: #a3a6ad;
  }

  .template-card.active .template-name {
    color: #409eff;
  }
}
</style>
