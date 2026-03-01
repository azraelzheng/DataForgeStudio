<!--
  ChartWidget - ECharts 图表组件
  支持多种图表类型：bar, line, pie, doughnut, gauge, radar
-->
<template>
  <div class="chart-widget" :class="{ 'is-loading': loading, 'is-empty': isEmpty }">
    <!-- 标题栏 -->
    <div v-if="title" class="chart-header">
      <h3 class="chart-title">{{ title }}</h3>
      <div class="chart-actions">
        <el-button
          v-if="showRefreshButton"
          :icon="Refresh"
          circle
          size="small"
          :loading="loading"
          @click="handleRefresh"
        />
        <el-dropdown v-if="showMoreButton" trigger="click">
          <el-button :icon="More" circle size="small" />
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item :icon="Download" @click="handleDownload">下载图片</el-dropdown-item>
              <el-dropdown-item :icon="FullScreen" @click="handleFullscreen">全屏</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </div>

    <!-- 图表容器 -->
    <div
      ref="chartContainer"
      class="chart-container"
      :style="{ height: containerHeight }"
    >
      <!-- 加载中 -->
      <div v-if="loading" class="chart-loading">
        <el-icon class="is-loading" :size="32">
          <Loading />
        </el-icon>
        <p>加载中...</p>
      </div>

      <!-- 无数据 -->
      <div v-else-if="isEmpty" class="chart-empty">
        <el-empty description="暂无数据" :image-size="80" />
      </div>

      <!-- 图表 -->
      <div
        v-show="!loading && !isEmpty"
        ref="chartRef"
        class="chart-content"
      />

      <!-- 错误提示 -->
      <el-alert
        v-if="error"
        type="error"
        :title="error.message"
        :closable="false"
        show-icon
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, toRef, type PropType } from 'vue'
import { Refresh, More, Download, FullScreen, Loading } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useECharts, ChartConfigBuilder, ColorSchemes } from '../composables/useECharts'
import type { DataSourceConfig } from '../types/dashboard'
import { dataBinder } from '../core/DataBinder'

/**
 * 图表类型
 */
export type ChartType = 'bar' | 'line' | 'pie' | 'doughnut' | 'gauge' | 'radar'

/**
 * 颜色方案
 */
export type ColorScheme = 'default' | 'business' | 'warm' | 'cool' | 'monochrome' | 'custom'

/**
 * Props 接口
 */
interface ChartWidgetProps {
  /** 组件 ID */
  widgetId: string
  /** 图表类型 */
  chartType: ChartType
  /** 标题 */
  title?: string
  /** 数据源配置 */
  dataSource?: DataSourceConfig
  /** X 轴字段 */
  xField?: string
  /** Y 轴字段列表 */
  yFields?: string[]
  /** 颜色方案 */
  colorScheme?: ColorScheme
  /** 自定义颜色 */
  customColors?: string[]
  /** 是否显示图例 */
  showLegend?: boolean
  /** 是否显示提示框 */
  showTooltip?: boolean
  /** 是否启用动画 */
  animation?: boolean
  /** 容器高度 */
  height?: string
  /** 是否显示刷新按钮 */
  showRefreshButton?: boolean
  /** 是否显示更多按钮 */
  showMoreButton?: boolean
  /** 数据刷新间隔（秒） */
  refreshInterval?: number
}

// Props 定义
const props = withDefaults(defineProps<ChartWidgetProps>(), {
  title: '',
  chartType: 'bar',
  xField: '',
  yFields: () => [],
  colorScheme: 'default',
  customColors: () => [],
  showLegend: true,
  showTooltip: true,
  animation: true,
  height: '300px',
  showRefreshButton: true,
  showMoreButton: true,
  refreshInterval: 0
})

// Emits 定义
const emit = defineEmits<{
  /** 数据刷新时触发 */
  refresh: []
  /** 图表点击时触发 */
  click: [params: unknown]
  /** 数据加载完成时触发 */
  loaded: [data: unknown]
}>()

// 使用 ECharts composable
const { chartRef, loading, setOption, resize, getDataURL } = useECharts({
  autoResize: true,
  resizeDelay: 200
})

// 状态
const chartContainer = ref<HTMLElement>()
const error = ref<Error | null>(null)
const chartData = ref<unknown[]>([])
const bindingId = ref(`${props.widgetId}-chart`)

// 计算属性
const isEmpty = computed(() => {
  return !loading.value && chartData.value.length === 0
})

const containerHeight = computed(() => {
  return props.height || '300px'
})

const currentColors = computed(() => {
  return props.colorScheme === 'custom'
    ? props.customColors
    : ColorSchemes[props.colorScheme] || ColorSchemes.default
})

/**
 * 获取图表配置
 */
function getChartOption(data: unknown[]): unknown {
  const builder = new ChartConfigBuilder()

  // 设置标题
  if (props.title) {
    builder.setTitle(props.title)
  }

  // 设置颜色
  builder.setColors(currentColors.value)

  // 根据图表类型构建配置
  switch (props.chartType) {
    case 'bar':
    case 'line':
      return buildCartesianChart(builder, data)
    case 'pie':
    case 'doughnut':
      return buildPieChart(builder, data)
    case 'gauge':
      return buildGaugeChart(builder, data)
    case 'radar':
      return buildRadarChart(builder, data)
    default:
      return buildCartesianChart(builder, data)
  }
}

/**
 * 构建笛卡尔坐标系图表（柱状图、折线图）
 */
function buildCartesianChart(builder: ChartConfigBuilder, data: unknown[]): unknown {
  if (!data || data.length === 0) {
    return {}
  }

  const firstItem = data[0] as Record<string, unknown>
  const xData = data.map(item => (item as Record<string, unknown>)[props.xField || ''] as string)
  const yFieldList = props.yFields && props.yFields.length > 0 ? props.yFields : Object.keys(firstItem).filter(k => k !== props.xField)

  // 设置提示框
  builder.setTooltip('axis')

  // 设置 X 轴
  builder.setXAxis(xData, props.xField)

  // 设置 Y 轴
  builder.setYAxis()

  // 设置网格
  builder.setGrid({ top: props.title ? '60px' : '10px', bottom: '60px' })

  // 设置图例
  if (yFieldList.length > 1 && props.showLegend) {
    builder.setLegend(yFieldList)
  }

  // 添加系列
  yFieldList.forEach((field, index) => {
    const seriesData = data.map(item => (item as Record<string, unknown>)[field])
    builder.addSeries(
      field,
      props.chartType,
      seriesData,
      currentColors.value[index % currentColors.value.length]
    )
  })

  return builder.build()
}

/**
 * 构建饼图/环形图
 */
function buildPieChart(builder: ChartConfigBuilder, data: unknown[]): unknown {
  if (!data || data.length === 0) {
    return {}
  }

  const firstItem = data[0] as Record<string, unknown>
  const nameField = props.xField || Object.keys(firstItem)[0]
  const valueField = props.yFields && props.yFields.length > 0 ? props.yFields[0] : Object.keys(firstItem)[1]

  // 设置提示框
  builder.setTooltip('item')

  // 设置图例
  if (props.showLegend) {
    builder.setLegend(data.map(item => (item as Record<string, unknown>)[nameField] as string))
  }

  // 准备饼图数据
  const pieData = data.map(item => ({
    name: (item as Record<string, unknown>)[nameField] as string,
    value: (item as Record<string, unknown>)[valueField]
  }))

  builder.addSeries('', props.chartType, pieData)

  return builder.build()
}

/**
 * 构建仪表盘图表
 */
function buildGaugeChart(builder: ChartConfigBuilder, data: unknown[]): unknown {
  if (!data || data.length === 0) {
    return {}
  }

  const valueField = props.yFields && props.yFields.length > 0 ? props.yFields[0] : 'value'
  const value = (data[0] as Record<string, unknown>)[valueField] as number

  return {
    series: [
      {
        type: 'gauge',
        startAngle: 180,
        endAngle: 0,
        min: 0,
        max: 100,
        splitNumber: 10,
        axisLine: {
          lineStyle: {
            width: 20,
            color: [
              [0.3, '#67E0E3'],
              [0.7, '#37A2DA'],
              [1, '#FD666D']
            ]
          }
        },
        pointer: {
          itemStyle: {
            color: 'auto'
          }
        },
        axisTick: {
          distance: -20,
          length: 5,
          lineStyle: {
            color: '#fff',
            width: 1
          }
        },
        splitLine: {
          distance: -20,
          length: 15,
          lineStyle: {
            color: '#fff',
            width: 2
          }
        },
        axisLabel: {
          color: 'auto',
          distance: 30,
          fontSize: 12
        },
        detail: {
          valueAnimation: true,
          formatter: '{value}%',
          color: 'auto',
          fontSize: 20
        },
        data: [
          {
            value: value as number,
            name: props.title || ''
          }
        ]
      }
    ]
  }
}

/**
 * 构建雷达图
 */
function buildRadarChart(builder: ChartConfigBuilder, data: unknown[]): unknown {
  if (!data || data.length === 0) {
    return {}
  }

  const firstItem = data[0] as Record<string, unknown>
  const indicators = Object.keys(firstItem).map(key => ({
    name: key,
    max: 100
  }))

  const seriesData = data.map(item => {
    return {
      value: Object.values(item as Record<string, unknown>),
      name: (item as Record<string, unknown>)[props.xField || 'name'] as string || 'Series'
    }
  })

  return {
    tooltip: {},
    legend: props.showLegend ? {
      data: seriesData.map(d => d.name)
    } : undefined,
    radar: {
      indicator: indicators
    },
    series: [
      {
        type: 'radar',
        data: seriesData
      }
    ]
  }
}

/**
 * 加载数据
 */
async function loadData(): Promise<void> {
  loading.value = true
  error.value = null

  try {
    // 从 DataBinder 获取数据
    const data = dataBinder.getData(bindingId.value)

    if (data) {
      chartData.value = data as unknown[]
      updateChart()
      emit('loaded', data)
    }
  } catch (err) {
    error.value = err instanceof Error ? err : new Error('加载数据失败')
    console.error('[ChartWidget] 加载数据失败:', err)
  } finally {
    loading.value = false
  }
}

/**
 * 更新图表
 */
function updateChart(): void {
  if (chartData.value.length === 0) {
    return
  }

  const option = getChartOption(chartData.value)
  setOption(option, true)
}

/**
 * 刷新数据
 */
function handleRefresh(): void {
  emit('refresh')
  loadData()
}

/**
 * 下载图表
 */
function handleDownload(): void {
  try {
    const url = getDataURL()
    const link = document.createElement('a')
    link.download = `${props.title || 'chart'}.png`
    link.href = url
    link.click()
    ElMessage.success('图表已下载')
  } catch (err) {
    ElMessage.error('下载失败')
    console.error('[ChartWidget] 下载图表失败:', err)
  }
}

/**
 * 全屏显示
 */
function handleFullscreen(): void {
  if (chartContainer.value) {
    if (document.fullscreenElement) {
      document.exitFullscreen()
    } else {
      chartContainer.value.requestFullscreen()
    }
  }
}

/**
 * 初始化数据绑定
 */
function initDataBinding(): void {
  if (!props.dataSource) {
    return
  }

  // 注册数据源
  const sourceId = `${props.widgetId}-source`
  dataBinder.registerSource(
    {
      id: sourceId,
      ...props.dataSource
    },
    async () => {
      // TODO: 调用实际的数据获取 API
      // 这里返回模拟数据
      return fetchChartData()
    }
  )

  // 绑定组件
  dataBinder.bind({
    widgetId: bindingId.value,
    sourceId,
    fieldMapping: {},
    refreshInterval: props.refreshInterval
  })

  // 初次加载数据
  loadData()
}

/**
 * 获取图表数据（模拟）
 */
async function fetchChartData(): Promise<unknown> {
  // TODO: 替换为实际 API 调用
  // 根据不同的图表类型返回不同的模拟数据
  switch (props.chartType) {
    case 'bar':
    case 'line':
      return [
        { month: '一月', sales: 120, profit: 40 },
        { month: '二月', sales: 200, profit: 70 },
        { month: '三月', sales: 150, profit: 50 },
        { month: '四月', sales: 80, profit: 30 },
        { month: '五月', sales: 170, profit: 60 },
        { month: '六月', sales: 220, profit: 90 }
      ]
    case 'pie':
    case 'doughnut':
      return [
        { name: '直接访问', value: 335 },
        { name: '邮件营销', value: 310 },
        { name: '联盟广告', value: 234 },
        { name: '视频广告', value: 135 },
        { name: '搜索引擎', value: 1548 }
      ]
    case 'gauge':
      return [{ value: 75 }]
    case 'radar':
      return [
        { name: '预算分配', '销售': 4200, '管理': 10000, '信息技术': 28000, '客服': 28000, '研发': 52000, '市场': 21000 },
        { name: '实际开销', '销售': 5000, '管理': 14000, '信息技术': 28000, '客服': 26000, '研发': 52000, '市场': 21000 }
      ]
    default:
      return []
  }
}

// 监听数据变化
watch(() => props.dataSource, initDataBinding, { immediate: true })

// 监听配置变化
watch(
  [
    () => props.chartType,
    () => props.xField,
    () => props.yFields,
    () => props.colorScheme,
    () => props.customColors
  ],
  () => {
    updateChart()
  }
)

// 监听 DataBinder 数据更新
watch(
  () => dataBinder.getBindingState(bindingId.value)?.value.data,
  (newData) => {
    if (newData) {
      chartData.value = newData as unknown[]
      updateChart()
    }
  }
)

onMounted(() => {
  if (!props.dataSource) {
    // 如果没有数据源，使用模拟数据
    fetchChartData().then(data => {
      chartData.value = data as unknown[]
      updateChart()
    })
  }
})

onUnmounted(() => {
  dataBinder.unbind(bindingId.value)
})
</script>

<style scoped lang="scss">
.chart-widget {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  background: #fff;
  border-radius: 4px;
  overflow: hidden;

  &.is-loading,
  &.is-empty {
    .chart-content {
      display: none;
    }
  }
}

.chart-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 1px solid #eee;
  background: #fafafa;

  .chart-title {
    margin: 0;
    font-size: 14px;
    font-weight: 500;
    color: #333;
  }

  .chart-actions {
    display: flex;
    gap: 8px;
  }
}

.chart-container {
  flex: 1;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #fff;
}

.chart-content {
  width: 100%;
  height: 100%;
}

.chart-loading,
.chart-empty {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #999;

  .el-icon {
    color: #409eff;
  }

  p {
    margin-top: 12px;
    font-size: 14px;
  }
}

:deep(.el-alert) {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  max-width: 80%;
}

// 全屏样式
:fullscreen .chart-widget {
  width: 100vw;
  height: 100vh;
  border-radius: 0;
}
</style>
