<template>
  <div class="dashboard-view" :class="{ 'fullscreen': isFullscreen }">
    <!-- 悬浮工具栏 -->
    <transition name="fade">
      <div v-show="showToolbar" class="floating-toolbar">
        <div class="toolbar-left">
          <span class="dashboard-title">{{ dashboardInfo.name || '大屏展示' }}</span>
          <span v-if="lastRefreshTime" class="refresh-time">
            最后更新: {{ lastRefreshTime }}
          </span>
        </div>
        <div class="toolbar-right">
          <el-button
            v-if="!isFullscreen"
            type="primary"
            circle
            @click="enterFullscreen"
            title="全屏"
          >
            <el-icon><FullScreen /></el-icon>
          </el-button>
          <el-button
            v-else
            type="primary"
            circle
            @click="exitFullscreen"
            title="退出全屏"
          >
            <el-icon><Close /></el-icon>
          </el-button>
          <el-button
            type="primary"
            circle
            @click="handleRefresh"
            :loading="refreshing"
            title="刷新数据"
          >
            <el-icon><Refresh /></el-icon>
          </el-button>
        </div>
      </div>
    </transition>

    <!-- 加载状态 -->
    <div v-if="loading" class="loading-container">
      <el-icon class="loading-icon" :size="48"><Loading /></el-icon>
      <span>加载中...</span>
    </div>

    <!-- 错误状态 -->
    <div v-else-if="error" class="error-container">
      <el-icon :size="64" color="#f56c6c"><WarningFilled /></el-icon>
      <p>{{ error }}</p>
      <el-button type="primary" @click="loadDashboard">重新加载</el-button>
    </div>

    <!-- 大屏画布 -->
    <div
      v-else
      class="dashboard-canvas"
      :style="canvasStyle"
      ref="canvasRef"
    >
      <!-- 使用 CSS Grid 渲染组件 -->
      <div
        v-for="widget in widgets"
        :key="widget.widgetId"
        class="widget-container"
        :style="getWidgetStyle(widget)"
      >
        <!-- 表格组件 -->
        <template v-if="widget.widgetType === 'table'">
          <div class="widget-header" v-if="widget.title">
            {{ widget.title }}
          </div>
          <div class="widget-body" :class="`table-style-${widget.styleConfig?.tableStyle || 'default'}`">
            <el-table
              :data="getWidgetData(widget.widgetId)"
              :style="{ width: '100%', height: widget.title ? 'calc(100% - 32px)' : '100%' }"
              size="small"
              border
              :header-cell-style="getTableHeaderStyle(widget)"
              :cell-style="getCellStyle(widget)"
            >
              <el-table-column
                v-for="(col, index) in getTableColumns(widget)"
                :key="index"
                :prop="col.field"
                :label="col.label"
                :min-width="col.width || 100"
              />
            </el-table>
          </div>
        </template>

        <!-- 数字卡片组件 -->
        <template v-else-if="widget.widgetType === 'card-number'">
          <div class="card-number-widget">
            <div class="card-value" :style="{ color: widget.config?.color || '#00d9ff' }">
              {{ formatCardValue(widget) }}
            </div>
            <div class="card-label">{{ widget.title || '数据' }}</div>
            <div v-if="widget.config?.trend" class="card-trend" :class="widget.config.trend > 0 ? 'up' : 'down'">
              <el-icon v-if="widget.config.trend > 0"><CaretTop /></el-icon>
              <el-icon v-else><CaretBottom /></el-icon>
              {{ Math.abs(widget.config.trend) }}%
            </div>
          </div>
        </template>

        <!-- 进度条组件 -->
        <template v-else-if="widget.widgetType === 'progress-bar'">
          <div class="progress-widget">
            <div class="widget-header" v-if="widget.title">{{ widget.title }}</div>
            <div class="progress-content">
              <el-progress
                :percentage="getProgressValue(widget)"
                :stroke-width="widget.config?.strokeWidth || 20"
                :color="widget.config?.color || '#409eff'"
                :text-inside="true"
              />
            </div>
          </div>
        </template>

        <!-- 状态灯组件 -->
        <template v-else-if="widget.widgetType === 'status-light'">
          <div class="status-widget">
            <div
              class="status-light"
              :style="{
                backgroundColor: getStatusColor(widget),
                boxShadow: `0 0 20px ${getStatusColor(widget)}`
              }"
            >
              <span v-if="widget.config?.showValue" class="status-value">
                {{ getStatusValue(widget) }}
              </span>
            </div>
            <div class="status-label">{{ widget.title || '状态' }}</div>
          </div>
        </template>

        <!-- 柱状图组件 -->
        <template v-else-if="widget.widgetType === 'chart-bar'">
          <div class="chart-widget">
            <div class="widget-header" v-if="widget.title">{{ widget.title }}</div>
            <div class="chart-container" :ref="el => setChartRef(widget.widgetId, el)"></div>
          </div>
        </template>

        <!-- 折线图组件 -->
        <template v-else-if="widget.widgetType === 'chart-line'">
          <div class="chart-widget">
            <div class="widget-header" v-if="widget.title">{{ widget.title }}</div>
            <div class="chart-container" :ref="el => setChartRef(widget.widgetId, el)"></div>
          </div>
        </template>

        <!-- 饼图组件 -->
        <template v-else-if="widget.widgetType === 'chart-pie'">
          <div class="chart-widget">
            <div class="widget-header" v-if="widget.title">{{ widget.title }}</div>
            <div class="chart-container" :ref="el => setChartRef(widget.widgetId, el)"></div>
          </div>
        </template>

        <!-- 仪表盘组件 -->
        <template v-else-if="widget.widgetType === 'gauge'">
          <div class="chart-widget">
            <div class="widget-header" v-if="widget.title">{{ widget.title }}</div>
            <div class="chart-container" :ref="el => setChartRef(widget.widgetId, el)"></div>
          </div>
        </template>

        <!-- 未知组件类型 -->
        <template v-else>
          <div class="unknown-widget">
            <el-icon :size="32"><QuestionFilled /></el-icon>
            <span>未知组件类型: {{ widget.widgetType }}</span>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup>
/**
 * 公开大屏展示页面
 * - 无需登录即可访问
 * - 通过 publicId 访问大屏
 * - 自动进入全屏模式
 * - 根据 RefreshInterval 配置自动刷新数据
 * - 使用 requestAnimationFrame 替代 setInterval，实现与浏览器刷新率同步
 */
import { ref, reactive, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getPublicDashboardByUrl, getPublicDashboardDataByUrl } from '../../api/dashboard'
import { useRAFTimer } from '../../display/composables/useAnimationFrame'

const route = useRoute()

// 状态
const loading = ref(true)
const refreshing = ref(false)
const error = ref(null)
const isFullscreen = ref(false)
const showToolbar = ref(true)
const lastRefreshTime = ref(null)
const canvasRef = ref(null)
const canvasScale = ref(1)

// 大屏信息
const dashboardInfo = reactive({
  id: null,
  publicUrl: null,
  name: '',
  width: 1920,
  height: 1080,
  backgroundColor: '#0d1b2a',
  backgroundImage: '',
  settings: {
    theme: 'dark',
    refreshInterval: 0
  }
})

// 组件列表
const widgets = ref([])

// 组件数据
const widgetDataMap = ref({})

// 图表实例
const chartInstances = ref({})
const chartRefs = ref({})

// 自动刷新 - 使用 rAF 定时器
const refreshInterval = ref(0)
const { start: startRefreshTimer, stop: stopRefreshTimer } = useRAFTimer(
  async () => {
    await loadDashboardData()
    await initAllCharts()
    updateLastRefreshTime()
  },
  refreshInterval
)
let toolbarHideTimer = null

// 计算缩放比例（根据屏幕大小自动缩放)
const calculateScale = () => {
  // 获取视口尺寸（减去工具栏高度)
  const viewportWidth = window.innerWidth
  const viewportHeight = window.innerHeight - 60  // 预留工具栏空间

  // 计算缩放比例，保持宽高比
  const scaleX = viewportWidth / dashboardInfo.width
  const scaleY = viewportHeight / dashboardInfo.height

  // 取较小的比例，确保画布完整显示
  canvasScale.value = Math.min(scaleX, scaleY, 1)  // 最大不超过1，避免放大模糊
}

// 计算画布样式(支持自动缩放)
const canvasStyle = computed(() => {
  const scale = canvasScale.value
  return {
    width: `${dashboardInfo.width}px`,
    height: `${dashboardInfo.height}px`,
    backgroundColor: dashboardInfo.backgroundColor,
    backgroundImage: dashboardInfo.backgroundImage ? `url(${dashboardInfo.backgroundImage})` : 'none',
    backgroundSize: 'cover',
    backgroundPosition: 'center',
    transform: `translate(-50%, -50%) scale(${scale})`,
    transformOrigin: 'center center'
  }
})

// 表格表头样式
const tableHeaderStyle = computed(() => ({
  backgroundColor: dashboardInfo.settings?.theme === 'dark' ? '#1a1a2e' : '#f5f7fa',
  color: dashboardInfo.settings?.theme === 'dark' ? '#fff' : '#303133',
    fontWeight: 'bold'
}))

// 4种表格样式预设（字体大小使用相对单位以支持缩放）
const tableStylePresets = {
  // 深蓝色系 - 流程类数据（工序进度等)
  'deep-blue': {
    headerBg: 'linear-gradient(135deg, #1a3a5c 0%, #0d2847 100%)',
    headerColor: '#ffffff',
    borderColor: '#22d3ee',
    borderWidth: '2px',
    cellBg: 'rgba(13, 40, 71, 0.8)',
    cellColor: '#cffafe',
    rowHoverBg: 'rgba(34, 211, 238, 0.15)',
    shadowColor: 'rgba(34, 211, 238, 0.3)',
    headerFontSize: '1rem',
    cellFontSize: '0.875rem'
  },
  // 深紫色系 - 结果类数据（质检、订单进度等)
  'deep-purple': {
    headerBg: 'linear-gradient(135deg, #6b21a8 0%, #4c1d95 100%)',
    headerColor: '#ffffff',
    borderColor: '#a855f7',
    borderWidth: '2px',
    cellBg: 'rgba(88, 28, 135, 0.8)',
    cellColor: '#f3e8ff',
    rowHoverBg: 'rgba(139, 92, 246, 0.15)',
    shadowColor: 'rgba(139, 92, 246, 0.3)',
    headerFontSize: '1rem',
    cellFontSize: '0.875rem'
  },
  // 青色系 - 特殊强调
  'cyan': {
    headerBg: 'linear-gradient(135deg, #0e7490 0%, #064e5e 100%)',
    headerColor: '#ffffff',
    borderColor: '#06b6d4',
    borderWidth: '2px',
    cellBg: 'rgba(6, 78, 94, 0.8)',
    cellColor: '#cffafe',
    rowHoverBg: 'rgba(6, 182, 212, 0.15)',
    shadowColor: 'rgba(6, 182, 212, 0.3)',
    headerFontSize: '1rem',
    cellFontSize: '0.875rem'
  },
  // 橙色系 - 警告/重点数据
  'orange': {
    headerBg: 'linear-gradient(135deg, #c2410c 0%, #7c2d12 100%)',
    headerColor: '#ffffff',
    borderColor: '#f97316',
    borderWidth: '2px',
    cellBg: 'rgba(124, 45, 18, 0.8)',
    cellColor: '#fed7aa',
    rowHoverBg: 'rgba(249, 115, 22, 0.15)',
    shadowColor: 'rgba(249, 115, 22, 0.3)',
    headerFontSize: '1rem',
    cellFontSize: '0.875rem'
  },
  // 默认样式
  'default': {
    headerBg: dashboardInfo.settings?.theme === 'dark' ? '#1a1a2e' : '#f5f7fa',
    headerColor: dashboardInfo.settings?.theme === 'dark' ? '#fff' : '#303133',
    borderColor: 'rgba(255, 255, 255, 0.1)',
    borderWidth: '1px',
    cellBg: 'transparent',
    cellColor: dashboardInfo.settings?.theme === 'dark' ? '#fff' : '#303133',
    rowHoverBg: 'rgba(255, 255, 255, 0.05)',
    shadowColor: 'transparent',
    headerFontSize: '0.875rem',
    cellFontSize: '0.875rem'
  }
}

// 获取表格容器样式（支持多种风格)
const getTableContainerStyle = (widget) => {
  const styleName = widget.styleConfig?.tableStyle || 'default'
  const preset = tableStylePresets[styleName] || tableStylePresets.default
  return {
    border: `${preset.borderWidth} solid ${preset.borderColor}`,
    borderRadius: '4px',
    overflow: 'hidden',
    boxShadow: `0 4px 20px ${preset.shadowColor}`,
    backgroundColor: preset.cellBg
  }
}

// 获取表格表头样式（支持多种风格)
const getTableHeaderStyle = (widget) => {
  const styleName = widget.styleConfig?.tableStyle || 'default'
  const preset = tableStylePresets[styleName] || tableStylePresets.default

  return {
    background: preset.headerBg,
    color: preset.headerColor,
    fontWeight: 'bold',
    fontSize: preset.headerFontSize,
    borderBottom: `${preset.borderWidth} solid ${preset.borderColor}`
  }
}

// 获取单元格样式（支持多种风格)
const getCellStyle = (widget) => {
  const styleName = widget.styleConfig?.tableStyle || 'default'
  const preset = tableStylePresets[styleName] || tableStylePresets.default

  return ({ row, column, rowIndex }) => {
    const baseStyle = {
      backgroundColor: preset.cellBg,
      color: preset.cellColor,
      fontSize: preset.cellFontSize
    }

    // 检查条件样式
    if (widget.conditionStyles && widget.conditionStyles.length > 0) {
      const value = row[column.property]
      for (const condition of widget.conditionStyles) {
        if (matchCondition(value, condition)) {
          return {
            ...baseStyle,
            backgroundColor: condition.backgroundColor || baseStyle.backgroundColor,
            color: condition.textColor || baseStyle.color,
            fontWeight: condition.textColor ? 'bold' : 'normal'
          }
        }
      }
    }

    // 斑马纹效果（可选)
    if (widget.styleConfig?.zebra && rowIndex % 2 === 1) {
      return {
        ...baseStyle,
        backgroundColor: `rgba(255, 255, 255, 0.03)`
      }
    }

    return baseStyle
  }
}

// 获取组件位置样式
const getWidgetStyle = (widget) => {
  // 使用绝对定位
  const gridWidth = dashboardInfo.width / 12
  const gridHeight = dashboardInfo.height / 12

  return {
    position: 'absolute',
    left: `${widget.x * gridWidth}px`,
    top: `${widget.y * gridHeight}px`,
    width: `${widget.width * gridWidth}px`,
    height: `${widget.height * gridHeight}px`,
    padding: '10px'
  }
}

// 获取组件数据
const getWidgetData = (widgetId) => {
  const data = widgetDataMap.value[widgetId]
  if (!data || !Array.isArray(data)) return []
  return data
}

// 获取表格列配置
const getTableColumns = (widget) => {
  const data = getWidgetData(widget.widgetId)
  if (data.length === 0) {
    // 使用配置中的列信息
    if (widget.config?.columns) {
      return widget.config.columns.map(col => ({
        field: col.field || col.fieldName,
        label: col.label || col.displayName || col.fieldName,
        width: col.width || 100
      }))
    }
    return []
  }

  // 从数据中推断列
  const firstRow = data[0]
  return Object.keys(firstRow).map(key => ({
    field: key,
    label: key,
    width: 100
  }))
}

// 匹配条件
const matchCondition = (value, condition) => {
  if (value === undefined || value === null) return false

  const conditionValue = condition.value
  switch (condition.operator) {
    case 'eq':
      return value == conditionValue
    case 'gt':
      return Number(value) > Number(conditionValue)
    case 'lt':
      return Number(value) < Number(conditionValue)
    case 'contains':
      return String(value).includes(conditionValue)
    default:
      return false
  }
}

// 格式化数字卡片值
const formatCardValue = (widget) => {
  const data = getWidgetData(widget.widgetId)
  let value = widget.config?.value

  // 如果有数据源，从数据中获取值
  if (data.length > 0 && widget.config?.valueField) {
    const firstRow = data[0]
    value = firstRow[widget.config.valueField]
  }

  if (value === undefined || value === null) return '--'

  // 格式化数字
  const numValue = Number(value)
  if (!isNaN(numValue)) {
    // 添加千分位
    const formatted = numValue.toLocaleString()
    const prefix = widget.config?.prefix || ''
    const suffix = widget.config?.suffix || ''
    return `${prefix}${formatted}${suffix}`
  }

  return value
}

// 获取进度条值
const getProgressValue = (widget) => {
  const data = getWidgetData(widget.widgetId)
  let value = widget.config?.percentage || 0

  if (data.length > 0 && widget.config?.valueField) {
    const firstRow = data[0]
    value = Number(firstRow[widget.config.valueField]) || 0
  }

  return Math.min(100, Math.max(0, value))
}

// 获取状态颜色
const getStatusColor = (widget) => {
  const data = getWidgetData(widget.widgetId)

  // 检查条件样式
  if (widget.conditionStyles && widget.conditionStyles.length > 0 && data.length > 0) {
    const firstRow = data[0]
    const statusField = widget.config?.statusField
    if (statusField && firstRow[statusField] !== undefined) {
      const value = firstRow[statusField]
      for (const condition of widget.conditionStyles) {
        if (matchCondition(value, condition)) {
          return condition.backgroundColor || widget.config?.color || '#67c23a'
        }
      }
    }
  }

  return widget.config?.color || '#67c23a'
}

// 获取状态值
const getStatusValue = (widget) => {
  const data = getWidgetData(widget.widgetId)
  if (data.length > 0 && widget.config?.statusField) {
    return data[0][widget.config.statusField]
  }
  return null
}

// 设置图表引用
const setChartRef = (widgetId, el) => {
  if (el) {
    chartRefs.value[widgetId] = el
  }
}

// 初始化图表（简化版，使用 Canvas 绘制基本图表）
const initChart = (widget) => {
  const container = chartRefs.value[widget.widgetId]
  if (!container) return

  // 清除旧图表
  if (chartInstances.value[widget.widgetId]) {
    chartInstances.value[widget.widgetId] = null
  }

  const canvas = document.createElement('canvas')
  canvas.width = container.clientWidth
  canvas.height = container.clientHeight
  canvas.style.width = '100%'
  canvas.style.height = '100%'
  container.innerHTML = ''
  container.appendChild(canvas)

  const ctx = canvas.getContext('2d')
  const data = getWidgetData(widget.widgetId)

  // 根据图表类型绘制
  switch (widget.widgetType) {
    case 'chart-bar':
      drawBarChart(ctx, canvas, data, widget)
      break
    case 'chart-line':
      drawLineChart(ctx, canvas, data, widget)
      break
    case 'chart-pie':
      drawPieChart(ctx, canvas, data, widget)
      break
    case 'gauge':
      drawGaugeChart(ctx, canvas, data, widget)
      break
  }

  chartInstances.value[widget.widgetId] = { canvas, ctx }
}

// 绘制柱状图
const drawBarChart = (ctx, canvas, data, widget) => {
  const width = canvas.width
  const height = canvas.height
  const padding = 40
  const xField = widget.config?.xField
  const yField = widget.config?.yField

  if (!data.length || !xField || !yField) {
    drawNoData(ctx, canvas)
    return
  }

  const values = data.map(d => Number(d[yField]) || 0)
  const maxValue = Math.max(...values) * 1.2 || 100
  const barWidth = (width - padding * 2) / data.length - 10
  const isDark = dashboardInfo.settings?.theme === 'dark'

  ctx.clearRect(0, 0, width, height)

  // 绘制柱子
  data.forEach((item, index) => {
    const x = padding + index * (barWidth + 10)
    const barHeight = (values[index] / maxValue) * (height - padding * 2)
    const y = height - padding - barHeight

    // 渐变色
    const gradient = ctx.createLinearGradient(x, y, x, height - padding)
    gradient.addColorStop(0, widget.config?.color || '#409eff')
    gradient.addColorStop(1, widget.config?.colorEnd || '#1e88e5')

    ctx.fillStyle = gradient
    ctx.fillRect(x, y, barWidth, barHeight)

    // 标签
    ctx.fillStyle = isDark ? '#fff' : '#303133'
    ctx.font = '12px Arial'
    ctx.textAlign = 'center'
    ctx.fillText(item[xField] || '', x + barWidth / 2, height - 10)

    // 数值
    ctx.fillText(values[index].toString(), x + barWidth / 2, y - 5)
  })
}

// 绘制折线图
const drawLineChart = (ctx, canvas, data, widget) => {
  const width = canvas.width
  const height = canvas.height
  const padding = 40
  const xField = widget.config?.xField
  const yField = widget.config?.yField

  if (!data.length || !xField || !yField) {
    drawNoData(ctx, canvas)
    return
  }

  const values = data.map(d => Number(d[yField]) || 0)
  const maxValue = Math.max(...values) * 1.2 || 100
  const stepX = (width - padding * 2) / (data.length - 1 || 1)
  const isDark = dashboardInfo.settings?.theme === 'dark'

  ctx.clearRect(0, 0, width, height)

  // 绘制折线
  ctx.beginPath()
  ctx.strokeStyle = widget.config?.color || '#409eff'
  ctx.lineWidth = 2

  data.forEach((item, index) => {
    const x = padding + index * stepX
    const y = height - padding - (values[index] / maxValue) * (height - padding * 2)

    if (index === 0) {
      ctx.moveTo(x, y)
    } else {
      ctx.lineTo(x, y)
    }
  })

  ctx.stroke()

  // 绘制点
  data.forEach((item, index) => {
    const x = padding + index * stepX
    const y = height - padding - (values[index] / maxValue) * (height - padding * 2)

    ctx.beginPath()
    ctx.arc(x, y, 4, 0, Math.PI * 2)
    ctx.fillStyle = widget.config?.color || '#409eff'
    ctx.fill()

    // 标签
    ctx.fillStyle = isDark ? '#fff' : '#303133'
    ctx.font = '12px Arial'
    ctx.textAlign = 'center'
    ctx.fillText(item[xField] || '', x, height - 10)
    ctx.fillText(values[index].toString(), x, y - 10)
  })
}

// 绘制饼图
const drawPieChart = (ctx, canvas, data, widget) => {
  const width = canvas.width
  const height = canvas.height
  const centerX = width / 2
  const centerY = height / 2
  const radius = Math.min(width, height) / 2 - 40
  const xField = widget.config?.xField
  const yField = widget.config?.yField

  if (!data.length || !xField || !yField) {
    drawNoData(ctx, canvas)
    return
  }

  const values = data.map(d => Number(d[yField]) || 0)
  const total = values.reduce((a, b) => a + b, 0)
  const colors = ['#409eff', '#67c23a', '#e6a23c', '#f56c6c', '#909399', '#00d9ff']

  ctx.clearRect(0, 0, width, height)

  let startAngle = -Math.PI / 2

  data.forEach((item, index) => {
    const sliceAngle = (values[index] / total) * Math.PI * 2

    ctx.beginPath()
    ctx.moveTo(centerX, centerY)
    ctx.arc(centerX, centerY, radius, startAngle, startAngle + sliceAngle)
    ctx.closePath()

    ctx.fillStyle = colors[index % colors.length]
    ctx.fill()

    // 标签
    const labelAngle = startAngle + sliceAngle / 2
    const labelX = centerX + (radius + 20) * Math.cos(labelAngle)
    const labelY = centerY + (radius + 20) * Math.sin(labelAngle)

    ctx.fillStyle = '#fff'
    ctx.font = '12px Arial'
    ctx.textAlign = 'center'
    ctx.fillText(item[xField] || '', labelX, labelY)

    startAngle += sliceAngle
  })
}

// 绘制仪表盘
const drawGaugeChart = (ctx, canvas, data, widget) => {
  const width = canvas.width
  const height = canvas.height
  const centerX = width / 2
  const centerY = height / 2
  const radius = Math.min(width, height) / 2 - 40
  const maxValue = widget.config?.maxValue || 100

  let value = widget.config?.value || 0
  if (data.length > 0 && widget.config?.valueField) {
    value = Number(data[0][widget.config.valueField]) || 0
  }

  ctx.clearRect(0, 0, width, height)

  // 背景弧
  ctx.beginPath()
  ctx.arc(centerX, centerY, radius, Math.PI * 0.75, Math.PI * 2.25)
  ctx.strokeStyle = dashboardInfo.settings?.theme === 'dark' ? '#1a1a2e' : '#e4e7ed'
  ctx.lineWidth = 20
  ctx.lineCap = 'round'
  ctx.stroke()

  // 值弧
  const progress = value / maxValue
  const endAngle = Math.PI * 0.75 + progress * Math.PI * 1.5

  ctx.beginPath()
  ctx.arc(centerX, centerY, radius, Math.PI * 0.75, endAngle)

  // 渐变色
  const gradient = ctx.createLinearGradient(0, 0, width, 0)
  if (progress < 0.5) {
    gradient.addColorStop(0, '#67c23a')
    gradient.addColorStop(1, '#e6a23c')
  } else {
    gradient.addColorStop(0, '#e6a23c')
    gradient.addColorStop(1, '#f56c6c')
  }

  ctx.strokeStyle = gradient
  ctx.lineWidth = 20
  ctx.lineCap = 'round'
  ctx.stroke()

  // 中心数值
  ctx.fillStyle = '#fff'
  ctx.font = 'bold 32px Arial'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'middle'
  ctx.fillText(value.toString(), centerX, centerY)

  // 单位/标题
  ctx.font = '14px Arial'
  ctx.fillText(widget.title || '', centerX, centerY + 30)
}

// 绘制无数据提示
const drawNoData = (ctx, canvas) => {
  ctx.clearRect(0, 0, canvas.width, canvas.height)
  ctx.fillStyle = '#909399'
  ctx.font = '14px Arial'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'middle'
  ctx.fillText('暂无数据', canvas.width / 2, canvas.height / 2)
}

// 初始化所有图表
const initAllCharts = async () => {
  await nextTick()
  for (const widget of widgets.value) {
    if (['chart-bar', 'chart-line', 'chart-pie', 'gauge'].includes(widget.widgetType)) {
      initChart(widget)
    }
  }
}

// 加载公开大屏配置
const loadDashboard = async () => {
  // 从路由参数获取 publicUrl（GUID格式）
  const publicUrl = route.params.publicUrl
  if (!publicUrl) {
    error.value = '大屏访问链接无效'
    loading.value = false
    return
  }

  loading.value = true
  error.value = null

  try {
    // 使用公开 URL API 加载大屏配置
    const res = await getPublicDashboardByUrl(publicUrl)

    // 处理错误响应
    if (!res.success) {
      // 根据错误信息判断具体原因
      const message = res.message || '加载大屏失败'
      if (message.includes('未公开') || message.includes('not public')) {
        error.value = '此大屏未公开'
      } else if (message.includes('不存在') || message.includes('not found')) {
        error.value = '大屏不存在'
      } else {
        error.value = message
      }
      return
    }

    const data = res.data
    dashboardInfo.id = data.dashboardId || data.id
    dashboardInfo.publicUrl = publicUrl
    dashboardInfo.name = data.name || ''
    dashboardInfo.width = data.width || 1920
    dashboardInfo.height = data.height || 1080
    dashboardInfo.backgroundColor = data.backgroundColor || '#0d1b2a'
    dashboardInfo.backgroundImage = data.backgroundImage || ''
    dashboardInfo.settings = {
      theme: data.settings?.theme || 'dark',
      refreshInterval: data.settings?.refreshInterval || data.refreshInterval || 0
    }

    // 加载组件
    if (data.widgets && data.widgets.length > 0) {
      widgets.value = data.widgets.map(w => ({
        ...w,
        widgetId: w.widgetId || w.id
      }))
    }

    // 加载数据
    await loadDashboardData()

    // 初始化图表
    await initAllCharts()

    // 设置自动刷新
    setupAutoRefresh()

    // 记录最后刷新时间
    updateLastRefreshTime()

    // 自动进入全屏模式
    await enterFullscreen()

  } catch (err) {
    // 处理网络错误或其他异常
    const message = err.response?.data?.message || err.message || '加载大屏失败'
    if (message.includes('未公开') || message.includes('not public')) {
      error.value = '此大屏未公开'
    } else if (message.includes('不存在') || message.includes('not found')) {
      error.value = '大屏不存在'
    } else if (err.response?.status === 404) {
      error.value = '大屏不存在'
    } else if (err.response?.status === 403) {
      error.value = '此大屏未公开'
    } else {
      error.value = message
    }
  } finally {
    loading.value = false
  }
}

// 加载大屏数据（使用公开 API）
const loadDashboardData = async () => {
  const publicUrl = route.params.publicUrl
  if (!publicUrl) return

  try {
    // 使用公开 URL API 获取数据
    const res = await getPublicDashboardDataByUrl(publicUrl)
    if (res.success && res.data) {
      // 数据格式可能是 { widgetId: data } 的映射
      if (typeof res.data === 'object') {
        widgetDataMap.value = res.data
      }
    }
  } catch (err) {
    console.error('加载大屏数据失败:', err)
  }
}

// 刷新数据
const handleRefresh = async () => {
  if (refreshing.value) return

  refreshing.value = true
  try {
    await loadDashboardData()
    await initAllCharts()
    updateLastRefreshTime()
    ElMessage.success('数据已刷新')
  } catch (err) {
    ElMessage.error('刷新失败: ' + (err.message || '未知错误'))
  } finally {
    refreshing.value = false
  }
}

// 更新最后刷新时间
const updateLastRefreshTime = () => {
  const now = new Date()
  lastRefreshTime.value = `${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}:${now.getSeconds().toString().padStart(2, '0')}`
}

// 设置自动刷新 - 使用 rAF 定时器替代 setInterval
const setupAutoRefresh = () => {
  // 停止旧定时器
  stopAutoRefresh()

  const interval = dashboardInfo.settings?.refreshInterval
  if (interval && interval > 0) {
    refreshInterval.value = interval
    startRefreshTimer()
  }
}

// 停止自动刷新
const stopAutoRefresh = () => {
  stopRefreshTimer()
}

// 进入全屏
const enterFullscreen = async () => {
  try {
    const elem = document.documentElement
    if (elem.requestFullscreen) {
      await elem.requestFullscreen()
    } else if (elem.webkitRequestFullscreen) {
      await elem.webkitRequestFullscreen()
    } else if (elem.msRequestFullscreen) {
      await elem.msRequestFullscreen()
    }
    isFullscreen.value = true
  } catch (err) {
    // 某些浏览器可能阻止自动全屏，忽略错误
    console.log('进入全屏失败（可能是浏览器策略限制）:', err)
  }
}

// 退出全屏
const exitFullscreen = async () => {
  try {
    if (document.exitFullscreen) {
      await document.exitFullscreen()
    } else if (document.webkitExitFullscreen) {
      await document.webkitExitFullscreen()
    } else if (document.msExitFullscreen) {
      await document.msExitFullscreen()
    }
    isFullscreen.value = false
  } catch (err) {
    console.error('退出全屏失败:', err)
  }
}

// 监听全屏变化
const handleFullscreenChange = () => {
  isFullscreen.value = !!(
    document.fullscreenElement ||
    document.webkitFullscreenElement ||
    document.msFullscreenElement
  )
}

// 鼠标移动显示工具栏
const handleMouseMove = () => {
  showToolbar.value = true

  // 清除旧定时器
  if (toolbarHideTimer) {
    clearTimeout(toolbarHideTimer)
  }

  // 3秒后隐藏工具栏
  toolbarHideTimer = setTimeout(() => {
    if (isFullscreen.value) {
      showToolbar.value = false
    }
  }, 3000)
}

// 窗口大小变化时重新初始化图表和计算缩放
const handleResize = () => {
  calculateScale()
  initAllCharts()
}

// 生命周期
onMounted(async () => {
  await loadDashboard()

  // 监听全屏变化
  document.addEventListener('fullscreenchange', handleFullscreenChange)
  document.addEventListener('webkitfullscreenchange', handleFullscreenChange)
  document.addEventListener('msfullscreenchange', handleFullscreenChange)

  // 监听鼠标移动
  document.addEventListener('mousemove', handleMouseMove)

  // 监听窗口大小变化
  window.addEventListener('resize', handleResize)
})

onUnmounted(() => {
  // 清除定时器
  stopAutoRefresh()
  if (toolbarHideTimer) {
    clearTimeout(toolbarHideTimer)
  }

  // 移除事件监听
  document.removeEventListener('fullscreenchange', handleFullscreenChange)
  document.removeEventListener('webkitfullscreenchange', handleFullscreenChange)
  document.removeEventListener('msfullscreenchange', handleFullscreenChange)
  document.removeEventListener('mousemove', handleMouseMove)
  window.removeEventListener('resize', handleResize)

  // 清除图表实例
  chartInstances.value = {}
})
</script>

<style scoped>
.dashboard-view {
  width: 100vw;
  height: 100vh;
  overflow: hidden;
  position: relative;
  background-color: #0a0a14;
}

.dashboard-view.fullscreen {
  position: fixed;
  top: 0;
  left: 0;
  z-index: 9999;
}

/* 悬浮工具栏 */
.floating-toolbar {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 50px;
  background: linear-gradient(to bottom, rgba(0, 0, 0, 0.8), transparent);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 20px;
  z-index: 100;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 20px;
}

.dashboard-title {
  color: #fff;
  font-size: 18px;
  font-weight: 500;
}

.refresh-time {
  color: rgba(255, 255, 255, 0.6);
  font-size: 12px;
}

.toolbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

/* 加载和错误状态 */
.loading-container,
.error-container {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  color: #fff;
}

.loading-icon {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.error-container p {
  color: #f56c6c;
  font-size: 16px;
}

/* 画布 */
.dashboard-canvas {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  transform-origin: center center;
  overflow: hidden;
}

/* 组件容器 */
.widget-container {
  box-sizing: border-box;
}

.widget-header {
  color: #fff;
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 10px;
  padding-bottom: 8px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.widget-body {
  height: calc(100% - 32px);
  overflow: auto;
}

/* 数字卡片 */
.card-number-widget {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.card-value {
  font-size: 48px;
  font-weight: bold;
  text-shadow: 0 0 20px currentColor;
}

.card-label {
  color: rgba(255, 255, 255, 0.6);
  font-size: 14px;
  margin-top: 8px;
}

.card-trend {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 14px;
  margin-top: 8px;
}

.card-trend.up {
  color: #67c23a;
}

.card-trend.down {
  color: #f56c6c;
}

/* 进度条 */
.progress-widget {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.progress-content {
  flex: 1;
  display: flex;
  align-items: center;
}

/* 状态灯 */
.status-widget {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
}

.status-light {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  animation: pulse 2s ease-in-out infinite;
}

.status-value {
  color: #fff;
  font-size: 14px;
  font-weight: bold;
  text-shadow: 0 0 10px rgba(0, 0, 0, 0.5);
}

.status-label {
  color: #fff;
  font-size: 14px;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.7; }
}

/* 图表 */
.chart-widget {
  height: 100%;
  display: flex;
  flex-direction: column;
  background: rgba(26, 26, 46, 0.6);
  border-radius: 4px;
  border: 1px solid rgba(255, 255, 255, 0.1);
}

.chart-container {
  flex: 1;
  min-height: 0;
}

/* 未知组件 */
.unknown-widget {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: #909399;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 4px;
}

/* 淡入淡出动画 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* Element Plus 暗色主题适配 */
:deep(.el-table) {
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-header-bg-color: rgba(26, 26, 46, 0.8);
  --el-table-row-hover-bg-color: rgba(255, 255, 255, 0.05);
  --el-table-border-color: rgba(255, 255, 255, 0.1);
  --el-table-text-color: #fff;
  --el-table-header-text-color: #fff;
}

:deep(.el-progress__text) {
  color: #fff !important;
}

/* 表格样式 - 深蓝色系 */
.table-style-deep-blue :deep(.el-table) {
  --el-table-bg-color: rgba(13, 40, 71, 0.8);
  --el-table-tr-bg-color: transparent;
  --el-table-header-bg-color: linear-gradient(135deg, #1a3a5c 0%, #0d2847 100%);
  --el-table-row-hover-bg-color: rgba(34, 211, 238, 0.15);
  --el-table-border-color: #22d3ee;
  --el-table-text-color: #cffafe;
  --el-table-header-text-color: #ffffff;
  border: 2px solid #22d3ee;
  box-shadow: 0 4px 20px rgba(34, 211, 238, 0.3);
}

.table-style-deep-blue :deep(.el-table th.el-table__cell) {
  background: linear-gradient(135deg, #1a3a5c 0%, #0d2847 100%) !important;
  font-size: 16px;
  font-weight: bold;
  border-bottom: 2px solid #22d3ee !important;
}

.table-style-deep-blue :deep(.el-table td.el-table__cell) {
  background-color: rgba(13, 40, 71, 0.8);
  color: #cffafe;
  font-size: 14px;
}

/* 表格样式 - 深紫色系 */
.table-style-deep-purple :deep(.el-table) {
  --el-table-bg-color: rgba(88, 28, 135, 0.8);
  --el-table-tr-bg-color: transparent;
  --el-table-header-bg-color: linear-gradient(135deg, #6b21a8 0%, #4c1d95 100%);
  --el-table-row-hover-bg-color: rgba(139, 92, 246, 0.15);
  --el-table-border-color: #a855f7;
  --el-table-text-color: #f3e8ff;
  --el-table-header-text-color: #ffffff;
  border: 2px solid #a855f7;
  box-shadow: 0 4px 20px rgba(139, 92, 246, 0.3);
}

.table-style-deep-purple :deep(.el-table th.el-table__cell) {
  background: linear-gradient(135deg, #6b21a8 0%, #4c1d95 100%) !important;
  font-size: 16px;
  font-weight: bold;
  border-bottom: 2px solid #a855f7 !important;
}

.table-style-deep-purple :deep(.el-table td.el-table__cell) {
  background-color: rgba(88, 28, 135, 0.8);
  color: #f3e8ff;
  font-size: 14px;
}

/* 表格样式 - 青色系 */
.table-style-cyan :deep(.el-table) {
  --el-table-bg-color: rgba(6, 78, 94, 0.8);
  --el-table-tr-bg-color: transparent;
  --el-table-header-bg-color: linear-gradient(135deg, #0e7490 0%, #064e5e 100%);
  --el-table-row-hover-bg-color: rgba(6, 182, 212, 0.15);
  --el-table-border-color: #06b6d4;
  --el-table-text-color: #cffafe;
  --el-table-header-text-color: #ffffff;
  border: 2px solid #06b6d4;
  box-shadow: 0 4px 20px rgba(6, 182, 212, 0.3);
}

.table-style-cyan :deep(.el-table th.el-table__cell) {
  background: linear-gradient(135deg, #0e7490 0%, #064e5e 100%) !important;
  font-size: 16px;
  font-weight: bold;
  border-bottom: 2px solid #06b6d4 !important;
}

.table-style-cyan :deep(.el-table td.el-table__cell) {
  background-color: rgba(6, 78, 94, 0.8);
  color: #cffafe;
  font-size: 14px;
}

/* 表格样式 - 橙色系 */
.table-style-orange :deep(.el-table) {
  --el-table-bg-color: rgba(124, 45, 18, 0.8);
  --el-table-tr-bg-color: transparent;
  --el-table-header-bg-color: linear-gradient(135deg, #c2410c 0%, #7c2d12 100%);
  --el-table-row-hover-bg-color: rgba(249, 115, 22, 0.15);
  --el-table-border-color: #f97316;
  --el-table-text-color: #fed7aa;
  --el-table-header-text-color: #ffffff;
  border: 2px solid #f97316;
  box-shadow: 0 4px 20px rgba(249, 115, 22, 0.3);
}

.table-style-orange :deep(.el-table th.el-table__cell) {
  background: linear-gradient(135deg, #c2410c 0%, #7c2d12 100%) !important;
  font-size: 16px;
  font-weight: bold;
  border-bottom: 2px solid #f97316 !important;
}

.table-style-orange :deep(.el-table td.el-table__cell) {
  background-color: rgba(124, 45, 18, 0.8);
  color: #fed7aa;
  font-size: 14px;
}
</style>
