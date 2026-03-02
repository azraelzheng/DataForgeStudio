<template>
  <div class="dashboard-progress" :class="{ 'is-animated': animated }">
    <!-- 标题区域 -->
    <div v-if="showLabels && title" class="progress-header">
      <span class="progress-title">{{ title }}</span>
      <span class="progress-value">{{ displayValue }}</span>
    </div>

    <!-- 进度条容器 -->
    <div class="progress-bar-container" :style="containerStyle">
      <!-- 进度条背景 -->
      <div class="progress-bar-bg" :style="bgStyle">
        <!-- 进度条填充 -->
        <div
          class="progress-bar-fill"
          :class="{ 'with-animation': animated }"
          :style="fillStyle"
        >
          <!-- 条纹动画效果 -->
          <div v-if="striped" class="progress-bar-stripes" :class="{ 'striped-animated': stripedAnimated }"></div>
        </div>
      </div>

      <!-- 内部标签（当 showLabels 为 true 且没有标题时显示在进度条内） -->
      <div v-if="showLabels && !title && showInnerText" class="progress-inner-label">
        {{ displayValue }}
      </div>
    </div>

    <!-- 底部标签 -->
    <div v-if="showLabels && showBottomLabels" class="progress-footer">
      <span class="progress-min">0%</span>
      <span class="progress-max">100%</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch, onMounted } from 'vue'
import type { StyleRule, ComparisonOperator } from './types'

// ============================================================================
// Props 定义
// ============================================================================

/**
 * 颜色规则接口
 */
interface ColorRule {
  /** 比较运算符 */
  operator: ComparisonOperator
  /** 比较值 */
  value: number
  /** 匹配时显示的颜色 */
  color: string
}

const props = withDefaults(
  defineProps<{
    /** 进度值 (0-100) */
    value: number
    /** 标题 */
    title?: string
    /** 是否显示动画 */
    animated?: boolean
    /** 是否显示标签 */
    showLabels?: boolean
    /** 固定颜色 (优先级低于 rules) */
    color?: string
    /** 颜色规则 (根据值动态决定颜色) */
    rules?: ColorRule[]
    /** 进度条高度 (像素或 CSS 值) */
    height?: number | string
    /** 进度条宽度 (像素或 CSS 值，默认 100%) */
    width?: number | string
    /** 是否显示条纹 */
    striped?: boolean
    /** 条纹是否动画 */
    stripedAnimated?: boolean
    /** 是否显示底部刻度标签 */
    showBottomLabels?: boolean
    /** 是否在进度条内部显示数值 */
    showInnerText?: boolean
    /** 进度条圆角 */
    borderRadius?: number | string
    /** 自定义格式化函数 */
    formatter?: (value: number) => string
    /** 主题 */
    theme?: 'dark' | 'light'
  }>(),
  {
    value: 0,
    title: '',
    animated: true,
    showLabels: true,
    color: '',
    rules: () => [],
    height: 12,
    width: '100%',
    striped: false,
    stripedAnimated: false,
    showBottomLabels: false,
    showInnerText: true,
    borderRadius: 6,
    formatter: undefined,
    theme: 'dark'
  }
)

// ============================================================================
// 响应式状态
// ============================================================================

const currentValue = ref(0)

// ============================================================================
// 计算属性
// ============================================================================

/**
 * 格式化后的显示值
 */
const displayValue = computed(() => {
  if (props.formatter) {
    return props.formatter(props.value)
  }
  return `${Math.round(props.value)}%`
})

/**
 * 归一化的进度值 (0-100)
 */
const normalizedValue = computed(() => {
  return Math.max(0, Math.min(100, props.value))
})

/**
 * 根据规则计算当前颜色
 */
const computedColor = computed(() => {
  // 如果有规则，根据规则匹配颜色
  if (props.rules && props.rules.length > 0) {
    // 按优先级排序（后面的规则优先级更高）
    const sortedRules = [...props.rules].sort((a, b) => {
      const priorityMap: Record<string, number> = {
        'lt': 1, 'lte': 2, 'gt': 3, 'gte': 4, 'eq': 5, 'neq': 6
      }
      return (priorityMap[a.operator] || 0) - (priorityMap[b.operator] || 0)
    })

    for (const rule of sortedRules) {
      if (matchRule(rule)) {
        return rule.color
      }
    }
  }

  // 如果有固定颜色
  if (props.color) {
    return props.color
  }

  // 默认颜色渐变（根据值自动选择）
  return getDefaultColor(normalizedValue.value)
})

/**
 * 容器样式
 */
const containerStyle = computed(() => ({
  width: typeof props.width === 'number' ? `${props.width}px` : props.width
}))

/**
 * 背景样式
 */
const bgStyle = computed(() => {
  const bgColor = props.theme === 'dark' ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.1)'
  return {
    backgroundColor: bgColor,
    borderRadius: typeof props.borderRadius === 'number' ? `${props.borderRadius}px` : props.borderRadius
  }
})

/**
 * 进度条填充样式
 */
const fillStyle = computed(() => ({
  width: `${currentValue.value}%`,
  backgroundColor: computedColor.value,
  borderRadius: typeof props.borderRadius === 'number' ? `${props.borderRadius}px` : props.borderRadius
}))

// ============================================================================
// 方法
// ============================================================================

/**
 * 检查规则是否匹配
 */
const matchRule = (rule: ColorRule): boolean => {
  const val = normalizedValue.value
  const ruleValue = rule.value

  switch (rule.operator) {
    case 'lt':
      return val < ruleValue
    case 'lte':
      return val <= ruleValue
    case 'gt':
      return val > ruleValue
    case 'gte':
      return val >= ruleValue
    case 'eq':
      return val === ruleValue
    case 'neq':
      return val !== ruleValue
    default:
      return false
  }
}

/**
 * 获取默认颜色（根据值自动渐变）
 */
const getDefaultColor = (value: number): string => {
  if (value < 30) {
    // 红色系 - 低进度
    return '#ef4444'
  } else if (value < 60) {
    // 橙色系 - 中等进度
    return '#f59e0b'
  } else if (value < 80) {
    // 蓝色系 - 良好进度
    return '#3b82f6'
  } else {
    // 绿色系 - 高进度
    return '#22c55e'
  }
}

/**
 * 动画更新当前值
 */
const animateValue = (targetValue: number) => {
  if (!props.animated) {
    currentValue.value = targetValue
    return
  }

  const duration = 300 // 动画持续时间 (ms)
  const startTime = performance.now()
  const startValue = currentValue.value
  const change = targetValue - startValue

  const animate = (currentTime: number) => {
    const elapsed = currentTime - startTime
    const progress = Math.min(elapsed / duration, 1)

    // 使用 easeOutCubic 缓动函数
    const easeProgress = 1 - Math.pow(1 - progress, 3)
    currentValue.value = startValue + change * easeProgress

    if (progress < 1) {
      requestAnimationFrame(animate)
    }
  }

  requestAnimationFrame(animate)
}

// ============================================================================
// 监听器
// ============================================================================

watch(
  () => props.value,
  (newVal) => {
    animateValue(Math.max(0, Math.min(100, newVal)))
  },
  { immediate: false }
)

// ============================================================================
// 生命周期
// ============================================================================

onMounted(() => {
  // 初始化时设置值（带动画）
  requestAnimationFrame(() => {
    animateValue(normalizedValue.value)
  })
})
</script>

<style scoped>
.dashboard-progress {
  width: 100%;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

/* 标题区域 */
.progress-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
  padding: 0 2px;
}

.progress-title {
  font-size: 14px;
  font-weight: 500;
  color: v-bind('theme === "dark" ? "#e4e4e7" : "#374151"');
}

.progress-value {
  font-size: 14px;
  font-weight: 600;
  color: v-bind('theme === "dark" ? "#ffffff" : "#111827"');
  min-width: 48px;
  text-align: right;
}

/* 进度条容器 */
.progress-bar-container {
  position: relative;
  width: 100%;
}

/* 进度条背景 */
.progress-bar-bg {
  width: 100%;
  height: v-bind('typeof height === "number" ? height + "px" : height');
  overflow: hidden;
  position: relative;
}

/* 进度条填充 */
.progress-bar-fill {
  height: 100%;
  position: relative;
  transition: width 0.1s ease-out;
}

.progress-bar-fill.with-animation {
  transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

/* 条纹效果 */
.progress-bar-stripes {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-image: linear-gradient(
    45deg,
    rgba(255, 255, 255, 0.15) 25%,
    transparent 25%,
    transparent 50%,
    rgba(255, 255, 255, 0.15) 50%,
    rgba(255, 255, 255, 0.15) 75%,
    transparent 75%,
    transparent
  );
  background-size: 20px 20px;
}

.progress-bar-stripes.striped-animated {
  animation: stripe-move 1s linear infinite;
}

@keyframes stripe-move {
  from {
    background-position: 0 0;
  }
  to {
    background-position: 20px 0;
  }
}

/* 内部标签 */
.progress-inner-label {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  font-size: 12px;
  font-weight: 600;
  color: #ffffff;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
  white-space: nowrap;
  pointer-events: none;
  z-index: 1;
}

/* 底部标签 */
.progress-footer {
  display: flex;
  justify-content: space-between;
  margin-top: 4px;
  padding: 0 2px;
}

.progress-min,
.progress-max {
  font-size: 12px;
  color: v-bind('theme === "dark" ? "#71717a" : "#9ca3af"');
}

/* 动画状态 */
.dashboard-progress.is-animated .progress-bar-fill {
  will-change: width;
}

/* 暗色主题调整 */
.dashboard-progress[data-theme="dark"] {
  --progress-text-color: #e4e4e7;
  --progress-muted-color: #71717a;
}

/* 亮色主题调整 */
.dashboard-progress[data-theme="light"] {
  --progress-text-color: #374151;
  --progress-muted-color: #9ca3af;
}
</style>
