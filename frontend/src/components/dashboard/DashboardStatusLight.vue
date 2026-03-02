<template>
  <div class="status-light" :class="sizeClass">
    <div
      class="light"
      :class="[
        colorClass,
        { animated: animated, 'breathing': animated && animationType === 'breathing' }
      ]"
      :title="tooltip"
    />
    <span v-if="label" class="label">{{ label }}</span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { ComparisonOperator } from './types'

// ============================================================================
// 类型定义
// ============================================================================

/**
 * 状态灯颜色
 */
export type StatusLightColor = 'green' | 'yellow' | 'red' | 'gray'

/**
 * 状态灯尺寸
 */
export type StatusLightSize = 'small' | 'medium' | 'large'

/**
 * 动画类型
 */
export type AnimationType = 'blink' | 'breathing'

/**
 * 状态规则定义
 */
export interface StatusRule {
  /** 比较运算符 */
  operator: ComparisonOperator
  /** 比较值 */
  value: number
  /** 匹配时显示的颜色 */
  color: StatusLightColor
  /** 优先级 (数字越大优先级越高) */
  priority?: number
}

// ============================================================================
// Props 定义
// ============================================================================

const props = withDefaults(
  defineProps<{
    /** 当前值 */
    value: number
    /** 标签文字 */
    label?: string
    /** 状态规则 */
    rules?: StatusRule[]
    /** 是否启用动画 */
    animated?: boolean
    /** 动画类型 (blink: 闪烁, breathing: 呼吸灯) */
    animationType?: AnimationType
    /** 尺寸 */
    size?: StatusLightSize
  }>(),
  {
    value: 0,
    label: '',
    rules: () => [],
    animated: false,
    animationType: 'breathing',
    size: 'medium'
  }
)

// ============================================================================
// 默认规则
// ============================================================================

/**
 * 默认状态规则
 * - lt70: 黄灯 (值 < 70)
 * - lt100: 红灯 (70 <= 值 < 100)
 * - gte100: 绿灯 (值 >= 100)
 */
const defaultRules: StatusRule[] = [
  { operator: 'lt', value: 70, color: 'yellow', priority: 1 },
  { operator: 'lt', value: 100, color: 'red', priority: 2 },
  { operator: 'gte', value: 100, color: 'green', priority: 3 }
]

// ============================================================================
// 工具函数
// ============================================================================

/**
 * 比较函数
 */
const compare = (actual: number, operator: ComparisonOperator, expected: number): boolean => {
  switch (operator) {
    case 'lt':
      return actual < expected
    case 'lte':
      return actual <= expected
    case 'gt':
      return actual > expected
    case 'gte':
      return actual >= expected
    case 'eq':
      return actual === expected
    case 'neq':
      return actual !== expected
    default:
      return false
  }
}

// ============================================================================
// 计算属性
// ============================================================================

/**
 * 获取当前规则 (使用自定义规则或默认规则)
 */
const activeRules = computed(() => {
  return props.rules.length > 0 ? props.rules : defaultRules
})

/**
 * 计算当前灯的颜色
 * 按优先级从高到低排序，返回第一个匹配的规则颜色
 */
const currentColor = computed((): StatusLightColor => {
  const sortedRules = [...activeRules.value].sort(
    (a, b) => (b.priority || 0) - (a.priority || 0)
  )

  for (const rule of sortedRules) {
    if (compare(props.value, rule.operator, rule.value)) {
      return rule.color
    }
  }

  // 默认返回灰色 (无匹配规则)
  return 'gray'
})

/**
 * 灯的颜色类名
 */
const colorClass = computed(() => `light--${currentColor.value}`)

/**
 * 尺寸类名
 */
const sizeClass = computed(() => `status-light--${props.size}`)

/**
 * 提示文字
 */
const tooltip = computed(() => {
  return `${props.label}: ${props.value} (${currentColor.value})`
})

// ============================================================================
// Expose
// ============================================================================

defineExpose({
  currentColor
})
</script>

<style scoped>
.status-light {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

/* 尺寸变体 */
.status-light--small .light {
  width: 8px;
  height: 8px;
}

.status-light--small .label {
  font-size: 12px;
}

.status-light--medium .light {
  width: 12px;
  height: 12px;
}

.status-light--medium .label {
  font-size: 14px;
}

.status-light--large .light {
  width: 18px;
  height: 18px;
}

.status-light--large .label {
  font-size: 16px;
}

/* 灯的基础样式 */
.light {
  border-radius: 50%;
  transition: background-color 0.3s ease;
  flex-shrink: 0;
}

/* 颜色变体 */
.light--green {
  background-color: #22c55e;
  box-shadow: 0 0 8px rgba(34, 197, 94, 0.6);
}

.light--yellow {
  background-color: #f59e0b;
  box-shadow: 0 0 8px rgba(245, 158, 11, 0.6);
}

.light--red {
  background-color: #ef4444;
  box-shadow: 0 0 8px rgba(239, 68, 68, 0.6);
}

.light--gray {
  background-color: #6b7280;
  box-shadow: none;
}

/* 闪烁动画 */
.light.animated:not(.breathing) {
  animation: blink 1s ease-in-out infinite;
}

@keyframes blink {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.3;
  }
}

/* 呼吸灯动画 */
.light.animated.breathing {
  animation: breathing 2s ease-in-out infinite;
}

@keyframes breathing {
  0%, 100% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.15);
    opacity: 0.7;
  }
}

/* 呼吸灯颜色特定的光晕效果 */
.light.animated.breathing.light--green {
  box-shadow: 0 0 12px rgba(34, 197, 94, 0.8);
}

.light.animated.breathing.light--yellow {
  box-shadow: 0 0 12px rgba(245, 158, 11, 0.8);
}

.light.animated.breathing.light--red {
  box-shadow: 0 0 12px rgba(239, 68, 68, 0.8);
}

/* 标签样式 */
.label {
  color: #a1a1aa;
  white-space: nowrap;
  user-select: none;
}
</style>
