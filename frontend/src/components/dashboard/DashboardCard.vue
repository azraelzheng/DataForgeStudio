<template>
  <div
    class="dashboard-card"
    :class="[`dashboard-card--${theme}`, cardClasses]"
    :style="cardStyles"
  >
    <!-- 卡片标题 -->
    <div class="dashboard-card__header">
      <span class="dashboard-card__title">{{ title }}</span>
      <el-icon v-if="icon" class="dashboard-card__icon" :style="iconStyle">
        <component :is="icon" />
      </el-icon>
    </div>

    <!-- 主要数值区域 -->
    <div class="dashboard-card__body">
      <span class="dashboard-card__prefix" v-if="computedPrefix">{{ computedPrefix }}</span>
      <span class="dashboard-card__value" :style="valueStyle">{{ formattedValue }}</span>
      <span class="dashboard-card__suffix" v-if="computedSuffix">{{ computedSuffix }}</span>
    </div>

    <!-- 趋势显示区域 -->
    <div class="dashboard-card__footer" v-if="showTrend">
      <span
        class="dashboard-card__trend"
        :class="`dashboard-card__trend--${trendDirection}`"
      >
        <el-icon class="dashboard-card__trend-icon">
          <CaretTop v-if="trendDirection === 'up'" />
          <CaretBottom v-else-if="trendDirection === 'down'" />
          <Minus v-else />
        </el-icon>
        <span class="dashboard-card__trend-value">{{ formattedTrend }}</span>
      </span>
      <span class="dashboard-card__trend-label" v-if="trendLabel">{{ trendLabel }}</span>
    </div>

    <!-- 加载状态 -->
    <div class="dashboard-card__loading" v-if="loading">
      <el-icon class="is-loading"><Loading /></el-icon>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { CaretTop, CaretBottom, Minus, Loading } from '@element-plus/icons-vue'
import type { StyleRule, ComparisonOperator, StyleActionType } from './types'

// ============================================================================
// Props 定义
// ============================================================================

interface DashboardCardRule {
  /** 比较运算符 */
  operator: ComparisonOperator
  /** 比较值 */
  value: number | string
  /** 动作类型 */
  actionType: StyleActionType
  /** 动作值 (颜色、类名等) */
  actionValue: string
  /** 优先级 (数字越大优先级越高) */
  priority?: number
}

const props = withDefaults(
  defineProps<{
    /** 卡片标题 */
    title: string
    /** 数值 */
    value: number | string
    /** 前缀 (如 ¥、$) */
    prefix?: string
    /** 后缀 (如 %、件、元) */
    suffix?: string
    /** 小数位数 */
    decimals?: number
    /** 是否启用千分位分隔符 */
    useThousandSeparator?: boolean
    /** 趋势值 (正数表示上升，负数表示下降) */
    trend?: number
    /** 趋势标签 (如 "较上周") */
    trendLabel?: string
    /** 自定义趋势方向 (覆盖自动判断) */
    trendDirection?: 'up' | 'down' | 'neutral'
    /** 自定义颜色 (主色调) */
    color?: string
    /** 主题 */
    theme?: 'dark' | 'light'
    /** 图标 (Element Plus 图标组件) */
    icon?: any
    /** 图标颜色 */
    iconColor?: string
    /** 条件样式规则 */
    rules?: DashboardCardRule[]
    /** 加载状态 */
    loading?: boolean
    /** 自定义类名 */
    customClass?: string
  }>(),
  {
    title: '',
    value: 0,
    prefix: '',
    suffix: '',
    decimals: 0,
    useThousandSeparator: true,
    trend: undefined,
    trendLabel: '',
    trendDirection: undefined,
    color: '',
    theme: 'dark',
    icon: undefined,
    iconColor: '',
    rules: () => [],
    loading: false,
    customClass: ''
  }
)

// ============================================================================
// 数值格式化
// ============================================================================

/**
 * 格式化数值 (支持千分位和小数位)
 */
const formattedValue = computed(() => {
  const numValue = typeof props.value === 'string' ? parseFloat(props.value) : props.value

  if (isNaN(numValue)) {
    return props.value
  }

  // 配置格式化选项
  const options: Intl.NumberFormatOptions = {
    minimumFractionDigits: props.decimals,
    maximumFractionDigits: props.decimals
  }

  // 启用千分位
  if (props.useThousandSeparator) {
    options.useGrouping = true
  }

  return new Intl.NumberFormat('zh-CN', options).format(numValue)
})

/**
 * 格式化趋势值
 */
const formattedTrend = computed(() => {
  if (props.trend === undefined) return ''

  const absTrend = Math.abs(props.trend)
  const options: Intl.NumberFormatOptions = {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  }

  return `${new Intl.NumberFormat('zh-CN', options).format(absTrend)}%`
})

/**
 * 计算趋势方向
 */
const trendDirection = computed((): 'up' | 'down' | 'neutral' => {
  // 如果指定了趋势方向，使用指定的值
  if (props.trendDirection) {
    return props.trendDirection
  }

  // 根据 trend 值自动判断
  if (props.trend === undefined || props.trend === 0) {
    return 'neutral'
  }

  return props.trend > 0 ? 'up' : 'down'
})

/**
 * 是否显示趋势区域
 */
const showTrend = computed(() => {
  return props.trend !== undefined
})

// ============================================================================
// 条件样式计算
// ============================================================================

/**
 * 比较运算符实现
 */
const compareValues = (actual: number | string, operator: ComparisonOperator, expected: number | string): boolean => {
  const actualNum = typeof actual === 'string' ? parseFloat(actual) : actual
  const expectedNum = typeof expected === 'string' ? parseFloat(expected) : expected

  // 如果无法转换为数字，进行字符串比较
  const actualStr = String(actual)
  const expectedStr = String(expected)

  switch (operator) {
    case 'lt':
      return actualNum < expectedNum
    case 'lte':
      return actualNum <= expectedNum
    case 'gt':
      return actualNum > expectedNum
    case 'gte':
      return actualNum >= expectedNum
    case 'eq':
      return actualNum === expectedNum || actualStr === expectedStr
    case 'neq':
      return actualNum !== expectedNum && actualStr !== expectedStr
    case 'contains':
      return actualStr.includes(expectedStr)
    case 'startsWith':
      return actualStr.startsWith(expectedStr)
    case 'endsWith':
      return actualStr.endsWith(expectedStr)
    default:
      return false
  }
}

/**
 * 获取匹配的样式规则
 */
const matchedRules = computed(() => {
  if (!props.rules || props.rules.length === 0) {
    return []
  }

  // 按优先级排序 (优先级高的在前)
  const sortedRules = [...props.rules].sort((a, b) => {
    const priorityA = a.priority ?? 0
    const priorityB = b.priority ?? 0
    return priorityB - priorityA
  })

  // 找出所有匹配的规则
  return sortedRules.filter(rule => {
    return compareValues(props.value, rule.operator, rule.value)
  })
})

/**
 * 获取条件颜色
 */
const conditionalColor = computed(() => {
  const colorRule = matchedRules.value.find(rule => rule.actionType === 'setColor')
  return colorRule?.actionValue || ''
})

/**
 * 获取条件背景色
 */
const conditionalBgColor = computed(() => {
  const bgRule = matchedRules.value.find(rule => rule.actionType === 'setBgColor')
  return bgRule?.actionValue || ''
})

/**
 * 获取条件类名
 */
const conditionalClass = computed(() => {
  const classRule = matchedRules.value.find(rule => rule.actionType === 'setClass')
  return classRule?.actionValue || ''
})

// ============================================================================
// 样式计算
// ============================================================================

/**
 * 计算前缀 (支持规则替换)
 */
const computedPrefix = computed(() => {
  const textRule = matchedRules.value.find(rule => rule.actionType === 'showText')
  if (textRule && textRule.actionValue.startsWith('prefix:')) {
    return textRule.actionValue.replace('prefix:', '')
  }
  return props.prefix
})

/**
 * 计算后缀 (支持规则替换)
 */
const computedSuffix = computed(() => {
  const textRule = matchedRules.value.find(rule => rule.actionType === 'showText')
  if (textRule && textRule.actionValue.startsWith('suffix:')) {
    return textRule.actionValue.replace('suffix:', '')
  }
  return props.suffix
})

/**
 * 数值颜色 (优先级: 条件颜色 > 自定义颜色 > 默认)
 */
const valueStyle = computed(() => {
  const color = conditionalColor.value || props.color
  return color ? { color } : {}
})

/**
 * 图标样式
 */
const iconStyle = computed(() => {
  return props.iconColor ? { color: props.iconColor } : {}
})

/**
 * 卡片动态类名
 */
const cardClasses = computed(() => {
  const classes: string[] = []

  if (props.customClass) {
    classes.push(props.customClass)
  }

  if (conditionalClass.value) {
    classes.push(conditionalClass.value)
  }

  if (props.loading) {
    classes.push('dashboard-card--loading')
  }

  return classes
})

/**
 * 卡片动态样式
 */
const cardStyles = computed(() => {
  const styles: Record<string, string> = {}

  if (conditionalBgColor.value) {
    styles.backgroundColor = conditionalBgColor.value
  }

  return styles
})
</script>

<style scoped>
.dashboard-card {
  position: relative;
  padding: 16px 20px;
  border-radius: 12px;
  overflow: hidden;
  transition: all 0.3s ease;
  min-height: 120px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

/* 暗色主题 */
.dashboard-card--dark {
  background: linear-gradient(135deg, #1f1f23 0%, #2d2d35 100%);
  border: 1px solid #3f3f46;
  color: #ffffff;
}

.dashboard-card--dark:hover {
  border-color: #52525b;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}

/* 亮色主题 */
.dashboard-card--light {
  background: linear-gradient(135deg, #ffffff 0%, #f9fafb 100%);
  border: 1px solid #e5e7eb;
  color: #111827;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.dashboard-card--light:hover {
  border-color: #d1d5db;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

/* 加载状态 */
.dashboard-card--loading {
  pointer-events: none;
  opacity: 0.7;
}

.dashboard-card__loading {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: inherit;
  border-radius: inherit;
  z-index: 10;
}

.dashboard-card__loading .el-icon {
  font-size: 24px;
  color: #3b82f6;
}

.dashboard-card__loading .el-icon.is-loading {
  animation: rotating 2s linear infinite;
}

@keyframes rotating {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

/* 卡片头部 */
.dashboard-card__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.dashboard-card__title {
  font-size: 14px;
  font-weight: 500;
  opacity: 0.8;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.dashboard-card__icon {
  font-size: 20px;
  flex-shrink: 0;
  margin-left: 8px;
}

/* 卡片主体 */
.dashboard-card__body {
  display: flex;
  align-items: baseline;
  gap: 4px;
  flex: 1;
  min-height: 36px;
}

.dashboard-card__prefix {
  font-size: 18px;
  font-weight: 500;
  opacity: 0.9;
}

.dashboard-card__value {
  font-size: 32px;
  font-weight: 700;
  line-height: 1.2;
  letter-spacing: -0.02em;
}

.dashboard-card__suffix {
  font-size: 14px;
  font-weight: 500;
  opacity: 0.8;
}

/* 卡片底部 */
.dashboard-card__footer {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: auto;
}

.dashboard-card__trend {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 500;
}

.dashboard-card__trend--up {
  color: #22c55e;
  background: rgba(34, 197, 94, 0.15);
}

.dashboard-card__trend--down {
  color: #ef4444;
  background: rgba(239, 68, 68, 0.15);
}

.dashboard-card__trend--neutral {
  color: #a1a1aa;
  background: rgba(161, 161, 170, 0.15);
}

.dashboard-card__trend-icon {
  font-size: 12px;
}

.dashboard-card__trend-value {
  font-weight: 600;
}

.dashboard-card__trend-label {
  font-size: 12px;
  opacity: 0.6;
}

/* 亮色主题下的趋势样式调整 */
.dashboard-card--light .dashboard-card__trend--up {
  background: rgba(34, 197, 94, 0.1);
}

.dashboard-card--light .dashboard-card__trend--down {
  background: rgba(239, 68, 68, 0.1);
}

.dashboard-card--light .dashboard-card__trend--neutral {
  background: rgba(107, 114, 128, 0.1);
}
</style>
