<template>
  <div class="dashboard-chart" ref="chartRef">
    <v-chart
      v-if="ready"
      :option="chartOption"
      :autoresize="true"
      :theme="computedTheme"
      class="chart"
    />
    <div v-else class="chart-loading">
      <el-icon class="is-loading"><Loading /></el-icon>
      <span>加载中...</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, watch } from 'vue'
import VChart from 'vue-echarts'
import * as echarts from 'echarts'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { BarChart, LineChart, PieChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DatasetComponent,
  TransformComponent,
  ToolboxComponent
} from 'echarts/components'
import { Loading } from '@element-plus/icons-vue'
import type { ChartType, ChartConfig, ChartSeriesConfig } from './types'
import { chartColors } from '@/styles/colors'

// 注册 ECharts 组件
use([
  CanvasRenderer,
  BarChart,
  LineChart,
  PieChart,
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DatasetComponent,
  TransformComponent,
  ToolboxComponent
])

// ============================================================================
// Props 定义
// ============================================================================

interface ChartDataItem {
  [key: string]: string | number
}

const props = withDefaults(
  defineProps<{
    /** 图表类型 */
    type?: ChartType
    /** 图表数据 */
    data?: ChartDataItem[]
    /** 图表配置 */
    config?: ChartConfig
    /** 主题 (dark/light) */
    theme?: 'dark' | 'light'
    /** 图表标题 */
    title?: string
    /** 额外的 ECharts 配置，用于覆盖默认配置 */
    overrides?: Record<string, unknown>
    /** 是否显示工具箱 */
    showToolbox?: boolean
    /** 加载状态 */
    loading?: boolean
  }>(),
  {
    type: 'bar',
    data: () => [],
    config: () => ({}),
    theme: 'dark',
    title: '',
    overrides: () => ({}),
    showToolbox: false,
    loading: false
  }
)

// ============================================================================
// 响应式状态
// ============================================================================

const ready = ref(false)
const chartRef = ref<HTMLElement | null>(null)

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
  },
  legend: {
    textStyle: {
      color: '#a1a1aa'
    }
  },
  xAxis: {
    axisLine: {
      lineStyle: {
        color: '#3f3f46'
      }
    },
    axisLabel: {
      color: '#a1a1aa'
    },
    splitLine: {
      lineStyle: {
        color: '#3f3f46'
      }
    }
  },
  yAxis: {
    axisLine: {
      lineStyle: {
        color: '#3f3f46'
      }
    },
    axisLabel: {
      color: '#a1a1aa'
    },
    splitLine: {
      lineStyle: {
        color: '#3f3f46'
      }
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
  },
  legend: {
    textStyle: {
      color: '#374151'
    }
  },
  xAxis: {
    axisLine: {
      lineStyle: {
        color: '#e5e7eb'
      }
    },
    axisLabel: {
      color: '#374151'
    },
    splitLine: {
      lineStyle: {
        color: '#e5e7eb'
      }
    }
  },
  yAxis: {
    axisLine: {
      lineStyle: {
        color: '#e5e7eb'
      }
    },
    axisLabel: {
      color: '#374151'
    },
    splitLine: {
      lineStyle: {
        color: '#e5e7eb'
      }
    }
  }
}

// 计算当前主题
const computedTheme = computed(() => {
  return props.theme === 'dark' ? darkTheme : lightTheme
})

// ============================================================================
// 颜色配置（使用统一色彩字典）
// ============================================================================

// 使用统一的图表色板（6色，符合大屏设计规范）
const defaultColorScheme = chartColors.primary

// ============================================================================
// 图表配置生成
// ============================================================================

/**
 * 生成基础配置
 */
const generateBaseOption = () => {
  const config = props.config

  return {
    color: config.colorScheme || defaultColorScheme,
    title: {
      text: props.title || config.title || '',
      left: 'center',
      top: 10,
      textStyle: {
        fontSize: 16,
        fontWeight: 500
      }
    },
    tooltip: {
      show: config.showTooltip !== false,
      trigger: props.type === 'pie' ? 'item' : 'axis',
      backgroundColor: props.theme === 'dark' ? 'rgba(0, 0, 0, 0.8)' : 'rgba(255, 255, 255, 0.9)',
      borderColor: props.theme === 'dark' ? '#3f3f46' : '#e5e7eb',
      textStyle: {
        color: props.theme === 'dark' ? '#ffffff' : '#374151'
      }
    },
    legend: {
      show: true,
      bottom: 10,
      ...config.legend
    },
    toolbox: props.showToolbox
      ? {
          show: true,
          feature: {
            saveAsImage: { title: '保存图片' },
            dataView: { title: '数据视图', readOnly: true },
            restore: { title: '还原' }
          }
        }
      : undefined,
    grid: {
      left: '3%',
      right: '4%',
      bottom: props.type === 'pie' ? '15%' : '10%',
      top: props.title ? '60' : '40',
      containLabel: true
    }
  }
}

/**
 * 从数据中提取字段名
 */
const extractFields = (data: ChartDataItem[]) => {
  if (!data || data.length === 0) {
    return { xField: '', yFields: [] }
  }

  const keys = Object.keys(data[0])
  // 假设第一个非数字字段是 X 轴，其他数字字段是 Y 轴
  const xField = keys.find((key) => typeof data[0][key] === 'string') || keys[0]
  const yFields = keys.filter((key) => key !== xField && typeof data[0][key] === 'number')

  return { xField, yFields }
}

/**
 * 生成柱状图/折线图系列
 */
const generateCartesianSeries = (data: ChartDataItem[], xField: string, yFields: string[]) => {
  const config = props.config
  const seriesConfigs = config.series || []

  return yFields.map((field, index) => {
    const customConfig = seriesConfigs.find((s) => s.field === field) || {}
    const seriesType = customConfig.type || props.type

    const series: Record<string, unknown> = {
      name: customConfig.name || field,
      type: seriesType,
      data: data.map((item) => item[field]),
      smooth: seriesType === 'line' ? true : undefined,
      label: {
        show: config.showDataLabels || false,
        position: seriesType === 'bar' ? 'top' : 'top',
        fontSize: 12
      },
      ...customConfig.style
    }

    // 面积图特殊处理
    if (seriesType === 'area' || (seriesType === 'line' && customConfig.style?.areaStyle)) {
      series.type = 'line'
      series.areaStyle = customConfig.style?.areaStyle || {}
    }

    return series
  })
}

/**
 * 生成饼图系列
 */
const generatePieSeries = (data: ChartDataItem[], xField: string, yFields: string[]) => {
  const config = props.config
  const valueField = yFields[0] || Object.keys(data[0] || {})[1]

  return [
    {
      name: config.title || '数据',
      type: 'pie',
      radius: ['40%', '70%'],
      center: ['50%', '50%'],
      avoidLabelOverlap: true,
      itemStyle: {
        borderRadius: 8,
        borderColor: props.theme === 'dark' ? '#1f1f23' : '#ffffff',
        borderWidth: 2
      },
      label: {
        show: config.showDataLabels !== false,
        formatter: '{b}: {d}%',
        fontSize: 12
      },
      emphasis: {
        label: {
          show: true,
          fontSize: 14,
          fontWeight: 'bold'
        }
      },
      labelLine: {
        show: true
      },
      data: data.map((item) => ({
        name: item[xField],
        value: item[valueField]
      }))
    }
  ]
}

/**
 * 生成完整的 ECharts 配置
 */
const chartOption = computed(() => {
  const data = props.data || []
  const config = props.config

  // 获取字段配置
  const xField = config.xField || extractFields(data).xField
  const yFields = config.yFields || extractFields(data).yFields

  // 基础配置
  const baseOption = generateBaseOption()

  // 根据图表类型生成配置
  if (props.type === 'pie') {
    return {
      ...baseOption,
      xAxis: undefined,
      yAxis: undefined,
      series: generatePieSeries(data, xField, yFields),
      ...props.overrides
    }
  }

  // 柱状图/折线图/面积图
  const xAxisData = data.map((item) => item[xField])

  return {
    ...baseOption,
    xAxis: {
      type: 'category',
      data: xAxisData,
      boundaryGap: props.type === 'bar',
      ...config.axis
    },
    yAxis: {
      type: 'value',
      ...config.axis
    },
    series: generateCartesianSeries(data, xField, yFields),
    ...props.overrides
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
.dashboard-chart {
  width: 100%;
  height: 100%;
  min-height: 200px;
  position: relative;
}

.chart {
  width: 100%;
  height: 100%;
}

.chart-loading {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #a1a1aa;
  gap: 8px;
}

.chart-loading .el-icon {
  font-size: 24px;
}

.chart-loading .el-icon.is-loading {
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
