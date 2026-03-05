<template>
  <div class="image-config-panel">
    <el-form :model="config" label-width="80px" size="small">
      <!-- 图片源 -->
      <el-form-item label="图片地址">
        <el-input
          v-model="config.imageUrl"
          placeholder="请输入图片URL或上传图片"
          clearable
          @change="handleChange"
        >
          <template #append>
            <el-button :icon="Upload" @click="handleUpload" />
          </template>
        </el-input>
      </el-form-item>

      <!-- 图片上传（隐藏的input） -->
      <input
        ref="fileInput"
        type="file"
        accept="image/*"
        style="display: none"
        @change="handleFileChange"
      />

      <!-- 图片预览 -->
      <el-form-item v-if="config.imageUrl" label="预览">
        <div class="image-preview">
          <img :src="config.imageUrl" :alt="config.alt" />
        </div>
      </el-form-item>

      <!-- 替代文本 -->
      <el-form-item label="替代文本">
        <el-input
          v-model="config.alt"
          placeholder="图片描述（用于无障碍访问）"
          @change="handleChange"
        />
      </el-form-item>

      <!-- 图片说明 -->
      <el-form-item label="图片说明">
        <el-input
          v-model="config.caption"
          placeholder="显示在图片下方的说明文字"
          @change="handleChange"
        />
      </el-form-item>

      <!-- 显示设置 -->
      <el-divider>显示设置</el-divider>

      <el-form-item label="适配模式">
        <el-select v-model="config.objectFit" @change="handleChange">
          <el-option label="填充" value="fill">
            <span>填充</span>
            <span class="option-desc">拉伸图片填满容器</span>
          </el-option>
          <el-option label="包含" value="contain">
            <span>包含</span>
            <span class="option-desc">保持比例完整显示</span>
          </el-option>
          <el-option label="覆盖" value="cover">
            <span>覆盖</span>
            <span class="option-desc">保持比例填满容器</span>
          </el-option>
          <el-option label="无" value="none">
            <span>无</span>
            <span class="option-desc">原始尺寸</span>
          </el-option>
          <el-option label="缩放" value="scale-down">
            <span>缩放</span>
            <span class="option-desc">自适应缩放</span>
          </el-option>
        </el-select>
      </el-form-item>

      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="透明度">
            <el-slider
              v-model="config.opacity"
              :min="0"
              :max="1"
              :step="0.1"
              show-input
              @change="handleChange"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="边框圆角">
            <el-input
              v-model="config.borderRadius"
              placeholder="如: 8px"
              @change="handleChange"
            />
          </el-form-item>
        </el-col>
      </el-row>

      <el-form-item label="背景颜色">
        <el-color-picker
          v-model="config.backgroundColor"
          show-alpha
          @change="handleChange"
        />
      </el-form-item>

      <el-form-item label="边框">
        <el-input
          v-model="config.border"
          placeholder="如: 1px solid #00d4ff"
          @change="handleChange"
        />
      </el-form-item>

      <!-- 交互设置 -->
      <el-divider>交互设置</el-divider>

      <el-form-item label="点击放大">
        <el-switch v-model="config.clickable" @change="handleChange" />
      </el-form-item>

      <el-form-item label="链接地址">
        <el-input
          v-model="config.link"
          placeholder="点击图片后跳转的链接"
          @change="handleChange"
        />
      </el-form-item>

      <el-form-item v-if="config.link" label="新窗口打开">
        <el-switch v-model="config.linkNewWindow" @change="handleChange" />
      </el-form-item>

      <!-- 自动刷新 -->
      <el-divider>自动刷新</el-divider>

      <el-form-item label="刷新间隔">
        <el-input-number
          v-model="config.refreshInterval"
          :min="0"
          :max="3600"
          :step="10"
          @change="handleChange"
        />
        <span class="form-tip">秒（0表示不刷新，适用于动态图片源）</span>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { reactive, watch, ref, type PropType } from 'vue'
import { Upload } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'

/**
 * 图片适配模式
 */
export type ObjectFit = 'fill' | 'contain' | 'cover' | 'none' | 'scale-down'

/**
 * 图片配置数据接口
 */
export interface ImageConfigData {
  imageUrl: string
  alt: string
  caption: string
  objectFit: ObjectFit
  backgroundColor: string
  borderRadius: string
  border: string
  opacity: number
  clickable: boolean
  link: string
  linkNewWindow: boolean
  refreshInterval: number
}

// Props
const props = defineProps({
  modelValue: {
    type: Object as PropType<Partial<ImageConfigData>>,
    default: () => ({})
  }
})

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: ImageConfigData): void
  (e: 'change', value: ImageConfigData): void
}>()

// 文件输入引用
const fileInput = ref<HTMLInputElement | null>(null)

// 默认配置
const defaultConfig: ImageConfigData = {
  imageUrl: '',
  alt: '图片',
  caption: '',
  objectFit: 'cover',
  backgroundColor: 'transparent',
  borderRadius: '4px',
  border: 'none',
  opacity: 1,
  clickable: false,
  link: '',
  linkNewWindow: true,
  refreshInterval: 0
}

// 本地配置状态
const config = reactive<ImageConfigData>({
  ...defaultConfig,
  ...props.modelValue
})

// 监听外部值变化
watch(
  () => props.modelValue,
  (newVal) => {
    Object.assign(config, { ...defaultConfig, ...newVal })
  },
  { deep: true }
)

// 处理配置变更
function handleChange(): void {
  emit('update:modelValue', { ...config })
  emit('change', { ...config })
}

// 触发文件上传
function handleUpload(): void {
  fileInput.value?.click()
}

// 处理文件选择
async function handleFileChange(event: Event): Promise<void> {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return

  // 检查文件大小（限制5MB）
  if (file.size > 5 * 1024 * 1024) {
    ElMessage.warning('图片大小不能超过5MB')
    return
  }

  // 检查文件类型
  if (!file.type.startsWith('image/')) {
    ElMessage.error('请选择图片文件')
    return
  }

  try {
    // 转换为Base64
    const reader = new FileReader()
    reader.onload = (e) => {
      const result = e.target?.result as string
      if (result) {
        config.imageUrl = result
        config.alt = file.name.replace(/\.[^/.]+$/, '')
        handleChange()
        ElMessage.success('图片上传成功')
      }
    }
    reader.readAsDataURL(file)
  } catch (error) {
    ElMessage.error('图片处理失败')
    console.error('图片处理失败:', error)
  }

  // 清空input值，允许重复选择同一文件
  target.value = ''
}
</script>

<style scoped lang="scss">
.image-config-panel {
  padding: 12px;
}

.el-divider {
  margin: 16px 0;
}

.form-tip {
  font-size: 11px;
  color: #909399;
  margin-top: 4px;
  display: block;
}

.image-preview {
  width: 100%;
  max-height: 150px;
  border-radius: 4px;
  overflow: hidden;
  background: rgba(0, 20, 40, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;

  img {
    max-width: 100%;
    max-height: 150px;
    object-fit: contain;
  }
}

.option-desc {
  font-size: 11px;
  color: #909399;
  margin-left: 8px;
}

:deep(.el-form-item) {
  margin-bottom: 16px;
}

:deep(.el-form-item__label) {
  font-size: 12px;
  color: #909399;
}

:deep(.el-select-dropdown__item) {
  display: flex;
  flex-direction: column;
  line-height: 1.4;
  padding: 8px 12px;
}
</style>
