<template>
  <div class="text-config-panel">
    <el-form :model="config" label-width="80px" size="small">
      <!-- 文本内容 -->
      <el-form-item label="文本内容">
        <el-input
          v-model="config.content"
          type="textarea"
          :rows="4"
          placeholder="请输入文本内容"
          @input="handleChange"
        />
      </el-form-item>

      <!-- 富文本模式 -->
      <el-form-item label="富文本模式">
        <el-switch v-model="config.richContent" @change="handleChange" />
      </el-form-item>

      <!-- 字体设置 -->
      <el-divider>字体设置</el-divider>

      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="字体大小">
            <el-input-number
              v-model="config.fontSize"
              :min="10"
              :max="72"
              :step="2"
              @change="handleChange"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="行高">
            <el-input-number
              v-model="config.lineHeight"
              :min="1"
              :max="3"
              :step="0.1"
              :precision="1"
              @change="handleChange"
            />
          </el-form-item>
        </el-col>
      </el-row>

      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="字体颜色">
            <el-color-picker v-model="config.fontColor" @change="handleChange" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="字体粗细">
            <el-select v-model="config.fontWeight" @change="handleChange">
              <el-option label="细体" :value="300" />
              <el-option label="常规" :value="400" />
              <el-option label="中等" :value="500" />
              <el-option label="粗体" :value="700" />
              <el-option label="特粗" :value="900" />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>

      <!-- 文本样式 -->
      <el-divider>文本样式</el-divider>

      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="文本对齐">
            <el-select v-model="config.textAlign" @change="handleChange">
              <el-option label="左对齐" value="left" />
              <el-option label="居中" value="center" />
              <el-option label="右对齐" value="right" />
              <el-option label="两端对齐" value="justify" />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="文本装饰">
            <el-select v-model="config.textDecoration" @change="handleChange">
              <el-option label="无" value="none" />
              <el-option label="下划线" value="underline" />
              <el-option label="删除线" value="line-through" />
              <el-option label="上划线" value="overline" />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>

      <el-form-item label="字间距">
        <el-slider
          v-model="config.letterSpacing"
          :min="0"
          :max="20"
          :step="1"
          show-input
          @change="handleChange"
        />
      </el-form-item>

      <!-- 背景设置 -->
      <el-divider>背景设置</el-divider>

      <el-form-item label="背景颜色">
        <el-color-picker
          v-model="config.backgroundColor"
          show-alpha
          @change="handleChange"
        />
      </el-form-item>

      <el-form-item label="内边距">
        <el-input
          v-model="config.padding"
          placeholder="如: 16px 或 16px 8px"
          @change="handleChange"
        />
      </el-form-item>

      <el-form-item label="边框圆角">
        <el-input
          v-model="config.borderRadius"
          placeholder="如: 4px 或 8px"
          @change="handleChange"
        />
      </el-form-item>

      <!-- 跑马灯效果 -->
      <el-divider>动态效果</el-divider>

      <el-form-item label="滚动效果">
        <el-switch v-model="config.scrolling" @change="handleChange" />
      </el-form-item>

      <el-form-item v-if="config.scrolling" label="滚动速度">
        <el-slider
          v-model="config.scrollSpeed"
          :min="1"
          :max="30"
          :step="1"
          show-input
          @change="handleChange"
        />
        <span class="form-tip">单位：秒（数值越小速度越快）</span>
      </el-form-item>

      <!-- 链接设置 -->
      <el-divider>链接设置</el-divider>

      <el-form-item label="链接地址">
        <el-input
          v-model="config.link"
          placeholder="点击文本后跳转的链接"
          @change="handleChange"
        />
      </el-form-item>

      <el-form-item v-if="config.link" label="新窗口打开">
        <el-switch v-model="config.linkNewWindow" @change="handleChange" />
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { reactive, watch, type PropType } from 'vue'

/**
 * 文本配置数据接口
 */
export interface TextConfigData {
  content: string
  richContent: boolean
  fontSize: number
  fontWeight: number
  fontColor: string
  fontFamily: string
  textAlign: 'left' | 'center' | 'right' | 'justify'
  textDecoration: 'none' | 'underline' | 'line-through' | 'overline'
  lineHeight: number
  letterSpacing: number
  backgroundColor: string
  padding: string
  borderRadius: string
  scrolling: boolean
  scrollSpeed: number
  link: string
  linkNewWindow: boolean
}

// Props
const props = defineProps({
  modelValue: {
    type: Object as PropType<Partial<TextConfigData>>,
    default: () => ({})
  }
})

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: TextConfigData): void
  (e: 'change', value: TextConfigData): void
}>()

// 默认配置
const defaultConfig: TextConfigData = {
  content: '请输入文本内容',
  richContent: false,
  fontSize: 14,
  fontWeight: 400,
  fontColor: '#e0f7ff',
  fontFamily: 'Microsoft YaHei, sans-serif',
  textAlign: 'left',
  textDecoration: 'none',
  lineHeight: 1.6,
  letterSpacing: 0,
  backgroundColor: 'transparent',
  padding: '16px',
  borderRadius: '4px',
  scrolling: false,
  scrollSpeed: 10,
  link: '',
  linkNewWindow: true
}

// 本地配置状态
const config = reactive<TextConfigData>({
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
</script>

<style scoped lang="scss">
.text-config-panel {
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

:deep(.el-form-item) {
  margin-bottom: 16px;
}

:deep(.el-form-item__label) {
  font-size: 12px;
  color: #909399;
}
</style>
