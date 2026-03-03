<template>
  <div class="dashboard-gauge" ref="gaugeRef">
    <v-chart
      v-if="ready"
      :option="chartOption"
      :autoresize="true"
      :theme="computedTheme"
      class="gauge"
    />
    <div v-else class="gauge-loading">
      <el-icon class="is-loading"><Loading /></el-icon>
      <span>加载中...</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, watch } from 'vue'
import VChart from 'vue-echarts'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { GaugeChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  SeriesComponent
} from 'echarts/components'
import { Loading } from '@element-plus/icons-vue'

// 注册 ECharts 组件
use([
  CanvasRenderer,
  GaugeChart,
  TitleComponent,
  TooltipComponent,
  SeriesComponent
])

// ============================================================================
// 类型定义
// ============================================================================

/**
 * 仪表盘颜色规则
 * 用于定义不同数值范围的颜色区段
 */
export interface GaugeColorRule {
  /** 起始值 */
  min: number
  /** 结束值 */
  max: number
  /** 颜色 (CSS 颜色值) */
  color: string
  /** 可选标签 */
  label?: string
}

// ============================================================================
// Props 定义
// ============================================================================

const props = withDefaults(
  defineProps<{
    /** 当前值 */
    value?: number
    /** 最小值 */
    min?: number
    /** 最大值 */
    max?: number
    /** 标题 */
    title?: string
    /** 单位 */
    unit?: string
    /** 主题颜色 (用于指针和主色调) */
    color?: string
    /** 颜色规则数组 */
    rules?: GaugeColorRule[]
    /** 主题 (dark/light) */
    theme?: 'dark' | 'light'
    /** 小数位数 */
    decimals?: number
    /** 是否显示刻度 */
    showAxisTick?: boolean
    /** 是否显示刻度标签 */
    showAxisLabel?: boolean
    /** 是否显示分割线 */
    splitLine?: boolean
    /** 分割段数 */
    splitNumber?: number
    /** 仪表盘半径 (百分比或像素) */
    radius?: string | number
    /** 加载状态 */
    loading?: boolean
  }>(),
  {
    value: 0,
    min: 0,
    max: 100,
    title: '',
    unit: '%',
    color: '#3b82f6',
    rules: () => [],
    theme: 'dark',
    decimals: 0,
    showAxisTick: true,
    showAxisLabel: true,
    splitLine: true,
    splitNumber: 10,
    radius: '75%',
    loading: false
  }
)

// ============================================================================
// 响应式状态
// ============================================================================

const ready = ref(false)
const gaugeRef = ref<HTMLElement | null>(null)

// ============================================================================
// 主题配置
// ============================================================================

// 暗色主题配置
const darkTheme = {
  backgroundColor: 'transparent',
  textStyle: {
    color: '#a1a1aa'
  },
  title: {
    textStyle: {
      color: '#ffffff'
    },
    subtextStyle: {
      color: '#a1a1aa'
    }
  }
}

// 亮色主题配置
const lightTheme = {
  backgroundColor: 'transparent',
  textStyle: {
    color: '#374151'
  },
  title: {
    textStyle: {
      color: '#111827'
    },
    subtextStyle: {
      color: '#6b7280'
    }
  }
}

// 计算当前主题
const computedTheme = computed(() => {
  return props.theme === 'dark' ? darkTheme : lightTheme
})

// ============================================================================
// 颜色规则处理
// ============================================================================

/**
 * 默认颜色规则 (绿-黄-红 渐变)
 */
const defaultColorRules: GaugeColorRule[] = [
  { min: 0, max: 60, color: '#22c55e', label: '正常' },
  { min: 60, max: 80, color: '#f59e0b', label: '警告' },
  { min: 80, max: 100, color: '#ef4444', label: '危险' }
]

/**
 * 根据数值获取当前颜色
 */
const getColorByValue = (value: number, rules: GaugeColorRule[]): string => {
  const activeRules = rules.length > 0 ? rules : defaultColorRules
  for (const rule of activeRules) {
    if (value >= rule.min && value <= rule.max) {
      return rule.color
    }
  }
  // 如果超出规则范围，返回最后一个规则的颜色
  return activeRules[activeRules.length - 1]?.color || props.color
}

/**
 * 生成 ECharts axisLine 配置
 */
const generateAxisLineStyle = (rules: GaugeColorRule[]) => {
  const activeRules = rules.length > 0 ? rules : defaultColorRules
  const totalRange = props.max - props.min

  return {
    lineStyle: {
      width: 15,
      color: activeRules.map((rule) => {
        // 计算每个区段在整个范围内的位置 (0-1)
        const offset = (rule.min - props.min) / totalRange
        return [offset, rule.color]
      })
    }
  }
}

// ============================================================================
// 图表配置生成
// ============================================================================

/**
 * 格式化数值显示
 */
const formatValue = (value: number): string => {
  return value.toFixed(props.decimals)
}

/**
 * 生成完整的 ECharts 配置
 */
const chartOption = computed(() => {
  const rules = props.rules || []
  const activeRules = rules.length > 0 ? rules : defaultColorRules
  const currentColor = getColorByValue(props.value, rules)

  return {
    series: [
      {
        type: 'gauge',
        center: ['50%', '60%'],
        radius: props.radius,
        min: props.min,
        max: props.max,
        splitNumber: props.splitNumber,
        // 轴线配置 (颜色区段)
        axisLine: generateAxisLineStyle(activeRules),
        // 刻度配置
        axisTick: {
          show: props.showAxisTick,
          length: 8,
          lineStyle: {
            color: props.theme === 'dark' ? '#52525b' : '#9ca3af',
            width: 1
          }
        },
        // 刻度标签配置
        axisLabel: {
          show: props.showAxisLabel,
          distance: 20,
          color: props.theme === 'dark' ? '#a1a1aa' : '#6b7280',
          fontSize: 12,
          formatter: (value: number) => {
            // 只显示主要刻度值
            if (value === props.min || value === props.max) {
              return value.toString()
            }
            return value.toString()
          }
        },
        // 分割线配置
        splitLine: {
          show: props.splitLine,
          length: 15,
          lineStyle: {
            color: props.theme === 'dark' ? '#71717a' : '#9ca3af',
            width: 2
          }
        },
        // 指针配置
        pointer: {
          icon: 'path://M12,2C13.1,2 14,2.9 14,4C14,4.1 14,4.19 13.98,4.29L16,17H8L10.02,4.29C10,4.19 10,4.1 10,4C10,2.9 10.9,2 12,2Z',
          length: '60%',
          width: 8,
          offsetCenter: [0, '-10%'],
          itemStyle: {
            color: currentColor,
            shadowColor: props.theme === 'dark' ? 'rgba(0, 0, 0, 0.5)' : 'rgba(0, 0, 0, 0.2)',
            shadowBlur: 5,
            shadowOffsetY: 2
          }
        },
        // 中心文字配置
        detail: {
          valueAnimation: true,
          formatter: (value: number) => {
            const formattedValue = formatValue(value)
            return `${formattedValue}${props.unit}`
          },
          color: props.theme === 'dark' ? '#ffffff' : '#111827',
          fontSize: 24,
          fontWeight: 'bold',
          offsetCenter: [0, '30%']
        },
        // 标题配置
        title: {
          show: !!props.title,
          offsetCenter: [0, '70%'],
          fontSize: 14,
          color: props.theme === 'dark' ? '#a1a1aa' : '#6b7280'
        },
        // 数据
        data: [
          {
            value: props.value,
            name: props.title
          }
        ],
        // 动画配置
        animation: true,
        animationDuration: 1000,
        animationEasing: 'cubicOut'
      }
    ],
    // 工具提示
    tooltip: {
      show: true,
      formatter: (params: { value: number; name: string }) => {
        const formattedValue = formatValue(params.value)
        const currentRule = activeRules.find(
          (r) => params.value >= r.min && params.value <= r.max
        )
        const label = currentRule?.label || ''
        return `
          <div style="padding: 8px;">
            <div style="font-weight: bold; margin-bottom: 4px;">${props.title || '仪表盘'}</div>
            <div style="display: flex; align-items: center; gap: 8px;">
              <span style="display: inline-block; width: 10px; height: 10px; background: ${currentColor}; border-radius: 50%;"></span>
              <span>${formattedValue}${props.unit}</span>
              ${label ? `<span style="color: ${currentColor};">(${label})</span>` : ''}
            </div>
          </div>
        `
      },
      backgroundColor: props.theme === 'dark' ? 'rgba(0, 0, 0, 0.8)' : 'rgba(255, 255, 255, 0.9)',
      borderColor: props.theme === 'dark' ? '#3f3f46' : '#e5e7eb',
      textStyle: {
        color: props.theme === 'dark' ? '#ffffff' : '#374151'
      }
    }
  }
})

// ============================================================================
// 生命周期
// ============================================================================

onMounted(() => {
  // 延迟渲染以确保容器尺寸正确
  requestAnimationFrame(() => {
    ready.value = true
  })
})

// 监听加载状态
watch(
  () => props.loading,
  (loading) => {
    // 可以在这里添加 loading 遮罩逻辑
  }
)
</script>

<style scoped>
.dashboard-gauge {
  width: 100%;
  height: 100%;
  min-height: 200px;
  position: relative;
}

.gauge {
  width: 100%;
  height: 100%;
}

.gauge-loading {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #a1a1aa;
  gap: 8px;
}

.gauge-loading .el-icon {
  font-size: 24px;
}

.gauge-loading .el-icon.is-loading {
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
</style>
