<!--
  TextWidget - 文本组件
  用于显示自定义文本内容，支持富文本格式
-->
<template>
  <div class="text-widget" :style="containerStyle">
    <!-- 标题栏 -->
    <div v-if="title" class="text-header">
      <h3 class="text-title">{{ title }}</h3>
    </div>

    <!-- 文本内容 -->
    <div class="text-content" :style="contentStyle">
      <div v-if="richContent" class="rich-text" v-html="sanitizedContent"></div>
      <div v-else class="plain-text" :style="textStyle">{{ content }}</div>
    </div>

    <!-- 滚动提示（跑马灯效果） -->
    <div v-if="scrolling" class="text-scrolling">
      <div class="scrolling-content" :style="scrollingStyle">
        {{ content }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, type CSSProperties, type PropType } from 'vue'
import DOMPurify from 'dompurify'

/**
 * 文本对齐方式
 */
export type TextAlign = 'left' | 'center' | 'right' | 'justify'

/**
 * 文本装饰
 */
export type TextDecoration = 'none' | 'underline' | 'line-through' | 'overline'

/**
 * Props 接口
 */
interface TextWidgetProps {
  /** 组件 ID */
  widgetId: string
  /** 标题 */
  title?: string
  /** 文本内容 */
  content?: string
  /** 是否富文本 */
  richContent?: boolean
  /** 字体大小 */
  fontSize?: number
  /** 字体粗细 */
  fontWeight?: number | string
  /** 字体颜色 */
  fontColor?: string
  /** 字体族 */
  fontFamily?: string
  /** 文本对齐 */
  textAlign?: TextAlign
  /** 文本装饰 */
  textDecoration?: TextDecoration
  /** 行高 */
  lineHeight?: number
  /** 字间距 */
  letterSpacing?: number
  /** 背景颜色 */
  backgroundColor?: string
  /** 内边距 */
  padding?: string
  /** 边框圆角 */
  borderRadius?: string
  /** 是否滚动（跑马灯） */
  scrolling?: boolean
  /** 滚动速度（秒） */
  scrollSpeed?: number
  /** 超链接 */
  link?: string
  /** 是否新窗口打开链接 */
  linkNewWindow?: boolean
}

// Props 定义
const props = withDefaults(defineProps<TextWidgetProps>(), {
  title: '',
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
})

// Emits 定义
const emit = defineEmits<{
  /** 点击事件 */
  click: []
}>()

/**
 * 容器样式
 */
const containerStyle = computed<CSSProperties>(() => ({
  backgroundColor: props.backgroundColor,
  borderRadius: props.borderRadius,
  padding: props.title ? '0' : props.padding,
  height: '100%',
  display: 'flex',
  flexDirection: 'column',
  cursor: props.link ? 'pointer' : 'default'
}))

/**
 * 内容样式
 */
const contentStyle = computed<CSSProperties>(() => ({
  padding: props.title ? props.padding : '0',
  flex: 1,
  overflow: 'hidden'
}))

/**
 * 文本样式
 */
const textStyle = computed<CSSProperties>(() => ({
  fontSize: `${props.fontSize}px`,
  fontWeight: String(props.fontWeight),
  color: props.fontColor,
  fontFamily: props.fontFamily,
  textAlign: props.textAlign,
  textDecoration: props.textDecoration,
  lineHeight: props.lineHeight,
  letterSpacing: `${props.letterSpacing}px`,
  wordBreak: 'break-word',
  whiteSpace: 'pre-wrap'
}))

/**
 * 滚动样式
 */
const scrollingStyle = computed<CSSProperties>(() => ({
  animationDuration: `${props.scrollSpeed}s`
}))

/**
 * 净化富文本内容（XSS 防护）
 */
const sanitizedContent = computed(() => {
  if (!props.content) return ''
  return DOMPurify.sanitize(props.content, {
    ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'u', 'span', 'a', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'ul', 'ol', 'li'],
    ALLOWED_ATTR: ['style', 'href', 'target']
  })
})

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
</script>

<style scoped lang="scss">
.text-widget {
  width: 100%;
  height: 100%;
  overflow: hidden;
  box-sizing: border-box;
}

.text-header {
  padding: 12px 16px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  flex-shrink: 0;

  .text-title {
    margin: 0;
    font-size: 14px;
    font-weight: 500;
    color: #e0f7ff;
  }
}

.text-content {
  flex: 1;
  overflow: auto;

  &::-webkit-scrollbar {
    width: 4px;
    height: 4px;
  }

  &::-webkit-scrollbar-thumb {
    background: rgba(0, 212, 255, 0.3);
    border-radius: 2px;
  }

  &::-webkit-scrollbar-track {
    background: transparent;
  }
}

.plain-text {
  width: 100%;
  height: 100%;
}

.rich-text {
  width: 100%;
  height: 100%;

  :deep(p) {
    margin: 0 0 8px 0;
  }

  :deep(a) {
    color: #00d4ff;
    text-decoration: none;

    &:hover {
      text-decoration: underline;
    }
  }
}

.text-scrolling {
  overflow: hidden;
  white-space: nowrap;

  .scrolling-content {
    display: inline-block;
    animation: scrollText linear infinite;
    padding-right: 50px;
  }
}

@keyframes scrollText {
  0% {
    transform: translateX(100%);
  }
  100% {
    transform: translateX(-100%);
  }
}
</style>
