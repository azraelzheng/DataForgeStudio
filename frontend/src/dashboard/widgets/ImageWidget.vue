<!--
  ImageWidget - 图片组件
  用于显示图片，支持多种显示模式和动态图片源
-->
<template>
  <div class="image-widget" :style="containerStyle">
    <!-- 标题栏 -->
    <div v-if="title" class="image-header">
      <h3 class="image-title">{{ title }}</h3>
    </div>

    <!-- 图片容器 -->
    <div class="image-container" :style="imageContainerStyle">
      <!-- 加载中 -->
      <div v-if="loading" class="image-loading">
        <el-icon class="is-loading" :size="32">
          <Loading />
        </el-icon>
        <span>加载中...</span>
      </div>

      <!-- 加载失败 -->
      <div v-else-if="error" class="image-error">
        <el-icon :size="48" color="#909399">
          <Picture />
        </el-icon>
        <span>图片加载失败</span>
        <el-button size="small" @click="retryLoad">重试</el-button>
      </div>

      <!-- 无图片 -->
      <div v-else-if="!imageUrl" class="image-empty">
        <el-icon :size="48" color="#909399">
          <Picture />
        </el-icon>
        <span>请设置图片地址</span>
      </div>

      <!-- 图片显示 -->
      <img
        v-else
        :src="imageUrl"
        :alt="alt"
        :style="imageStyle"
        @load="handleLoad"
        @error="handleError"
        @click="handleClick"
      />
    </div>

    <!-- 图片说明 -->
    <div v-if="caption" class="image-caption">
      {{ caption }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, type CSSProperties, type PropType } from 'vue'
import { Loading, Picture } from '@element-plus/icons-vue'

/**
 * 图片适配模式
 */
export type ObjectFit = 'fill' | 'contain' | 'cover' | 'none' | 'scale-down'

/**
 * Props 接口
 */
interface ImageWidgetProps {
  /** 组件 ID */
  widgetId: string
  /** 标题 */
  title?: string
  /** 图片地址 */
  imageUrl?: string
  /** 替代文本 */
  alt?: string
  /** 图片说明 */
  caption?: string
  /** 适配模式 */
  objectFit?: ObjectFit
  /** 背景颜色 */
  backgroundColor?: string
  /** 边框圆角 */
  borderRadius?: string
  /** 边框 */
  border?: string
  /** 透明度 */
  opacity?: number
  /** 是否可点击放大 */
  clickable?: boolean
  /** 链接地址 */
  link?: string
  /** 是否新窗口打开链接 */
  linkNewWindow?: boolean
  /** 刷新间隔（秒），0表示不刷新 */
  refreshInterval?: number
}

// Props 定义
const props = withDefaults(defineProps<ImageWidgetProps>(), {
  title: '',
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
})

// Emits 定义
const emit = defineEmits<{
  /** 点击事件 */
  click: []
  /** 加载完成 */
  load: []
  /** 加载失败 */
  error: []
}>()

// 状态
const loading = ref(false)
const error = ref(false)
const refreshTimer = ref<number | null>(null)

/**
 * 容器样式
 */
const containerStyle = computed<CSSProperties>(() => ({
  backgroundColor: props.backgroundColor,
  borderRadius: props.borderRadius,
  height: '100%',
  display: 'flex',
  flexDirection: 'column',
  overflow: 'hidden'
}))

/**
 * 图片容器样式
 */
const imageContainerStyle = computed<CSSProperties>(() => ({
  flex: 1,
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  overflow: 'hidden',
  position: 'relative'
}))

/**
 * 图片样式
 */
const imageStyle = computed<CSSProperties>(() => ({
  width: '100%',
  height: '100%',
  objectFit: props.objectFit,
  borderRadius: props.title ? '0' : props.borderRadius,
  border: props.border,
  opacity: props.opacity,
  cursor: (props.clickable || props.link) ? 'pointer' : 'default',
  transition: 'transform 0.3s ease'
}))

/**
 * 处理图片加载完成
 */
function handleLoad(): void {
  loading.value = false
  error.value = false
  emit('load')
}

/**
 * 处理图片加载失败
 */
function handleError(): void {
  loading.value = false
  error.value = true
  emit('error')
}

/**
 * 重试加载
 */
function retryLoad(): void {
  if (!props.imageUrl) return

  error.value = false
  loading.value = true

  // 添加时间戳强制刷新
  const url = new URL(props.imageUrl, window.location.origin)
  url.searchParams.set('_t', Date.now().toString())
  // 触发重新加载
  props.imageUrl = url.toString()
}

/**
 * 处理点击
 */
function handleClick(): void {
  emit('click')

  if (props.link) {
    if (props.linkNewWindow) {
      window.open(props.link, '_blank')
    } else {
      window.location.href = props.link
    }
  }
}

/**
 * 设置自动刷新
 */
function setupAutoRefresh(): void {
  if (refreshTimer.value) {
    clearInterval(refreshTimer.value)
  }

  if (props.refreshInterval > 0 && props.imageUrl) {
    refreshTimer.value = window.setInterval(() => {
      retryLoad()
    }, props.refreshInterval * 1000)
  }
}

// 监听图片地址变化
watch(() => props.imageUrl, (newUrl) => {
  if (newUrl) {
    loading.value = true
    error.value = false
  }
}, { immediate: true })

// 监听刷新间隔变化
watch(() => props.refreshInterval, setupAutoRefresh, { immediate: true })
</script>

<style scoped lang="scss">
.image-widget {
  width: 100%;
  height: 100%;
  box-sizing: border-box;
}

.image-header {
  padding: 12px 16px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  flex-shrink: 0;

  .image-title {
    margin: 0;
    font-size: 14px;
    font-weight: 500;
    color: #e0f7ff;
  }
}

.image-container {
  position: relative;
}

.image-loading,
.image-error,
.image-empty {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  color: #909399;
  font-size: 14px;
  background: rgba(0, 20, 40, 0.5);
}

.image-loading .el-icon {
  color: #00d4ff;
}

.image-caption {
  padding: 8px 16px;
  font-size: 12px;
  color: #8ab4c8;
  text-align: center;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  flex-shrink: 0;
}

img {
  display: block;
  max-width: 100%;
  max-height: 100%;

  &:hover {
    transform: scale(1.02);
  }
}
</style>
