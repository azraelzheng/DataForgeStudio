<!--
  ProgressBarWidget - 进度条组件
  支持线性、圆形进度条，支持阈值警告
-->
<template>
  <div class="progress-bar-widget">
    <!-- 标题 -->
    <div v-if="title" class="progress-header">
      <h3 class="progress-title">{{ title }}</h3>
      <el-button
        v-if="showRefreshButton"
        :icon="Refresh"
        circle
        size="small"
        text
        :loading="loading"
        @click="handleRefresh"
      />
    </div>

    <!-- 进度条容器 -->
    <div class="progress-content" :class="`type-${type}`">
      <!-- 线性进度条 -->
      <template v-if="type === 'linear'">
        <div class="linear-progress-wrapper">
          <div class="progress-info">
            <span class="progress-label">{{ currentLabel }} / {{ totalLabel }}</span>
            <span v-if="showPercentage" class="progress-percent" :style="{ color: progressColor }">
              {{ percentage }}%
            </span>
          </div>
          <el-progress
            :percentage="percentage"
            :color="progressColor"
            :stroke-width="strokeWidth"
            :show-text="false"
            :striped="striped"
            :striped-flow="stripedFlow"
          />
          <div v-if="thresholds && showThresholdLabels" class="progress-thresholds">
            <span
              v-for="(label, index) in thresholdLabels"
              :key="index"
              class="threshold-label"
              :style="{ left: `${label.position}%` }"
              :class="`threshold-${label.level}`"
            >
              {{ label.value }}{{ unit }}
            </span>
          </div>
        </div>
      </template>

      <!-- 圆形进度条 -->
      <template v-else-if="type === 'circle'">
        <div class="circle-progress-wrapper">
          <el-progress
            type="circle"
            :percentage="percentage"
            :color="progressColor"
            :width="circleSize"
            :stroke-width="circleStrokeWidth"
          >
            <template #default="{ percentage }">
              <div class="circle-content">
                <span class="circle-value" :style="{ color: progressColor }">
                  {{ displayValue }}
                </span>
                <span v-if="showPercentage" class="circle-percent">
                  {{ percentage }}%
                </span>
              </div>
            </template>
          </el-progress>
          <div v-if="currentLabel || totalLabel" class="circle-labels">
            <div v-if="currentLabel" class="circle-label current">{{ currentLabel }}</div>
            <div v-if="totalLabel" class="circle-label total">/ {{ totalLabel }}</div>
          </div>
        </div>
      </template>

      <!-- 仪表盘进度条 -->
      <template v-else-if="type === 'dashboard'">
        <div class="dashboard-progress-wrapper">
          <el-progress
            type="dashboard"
            :percentage="percentage"
            :color="progressColor"
            :width="dashboardSize"
          >
            <template #default="{ percentage }">
              <div class="dashboard-content">
                <span class="dashboard-value" :style="{ color: progressColor, fontSize: `${dashboardSize / 5}px` }">
                  {{ displayValue }}
                </span>
                <span v-if="showPercentage" class="dashboard-percent">
                  {{ percentage }}%
                </span>
              </div>
            </template>
          </el-progress>
          <div v-if="currentLabel" class="dashboard-label">{{ currentLabel }}</div>
        </div>
      </template>

      <!-- 分段进度条 -->
      <template v-else-if="type === 'segmented'">
        <div class="segmented-progress-wrapper">
          <div class="progress-info">
            <span class="progress-label">{{ currentLabel }} / {{ totalLabel }}</span>
            <span v-if="showPercentage" class="progress-percent" :style="{ color: progressColor }">
              {{ percentage }}%
            </span>
          </div>
          <div class="segmented-bar">
            <div
              v-for="(seg, index) in segments"
              :key="index"
              class="segment"
              :class="{ active: index < activeSegments, 'is-warning': seg.level === 'warning', 'is-danger': seg.level === 'danger' }"
              :style="{ flex: seg.ratio, backgroundColor: index < activeSegments ? seg.color : '' }"
            />
          </div>
          <div v-if="thresholds && showThresholdLabels" class="segment-labels">
            <span
              v-for="(label, index) in thresholdLabels"
              :key="index"
              class="segment-label"
            >
              {{ label.value }}{{ unit }}
            </span>
          </div>
        </div>
      </template>
    </div>

    <!-- 状态指示 -->
    <div v-if="showStatus" class="progress-status" :class="`status-${statusLevel}`">
      <el-icon>
        <Warning v-if="statusLevel === 'warning'" />
        <CircleClose v-else-if="statusLevel === 'danger'" />
        <CircleCheck v-else />
      </el-icon>
      <span>{{ statusText }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, type PropType } from 'vue'
import { Refresh, Warning, CircleClose, CircleCheck } from '@element-plus/icons-vue'
import type { DataSourceConfig } from '../types/dashboard'
import { dataBinder } from '../core/DataBinder'

/**
 * 进度条类型
 */
export type ProgressType = 'linear' | 'circle' | 'dashboard' | 'segmented'

/**
 * 颜色方案
 */
export type ProgressColorScheme = 'primary' | 'success' | 'warning' | 'danger' | 'gradient' | 'custom'

/**
 * 阈值配置
 */
export interface ThresholdConfig {
  /** 警告阈值（百分比） */
  warning: number
  /** 危险阈值（百分比） */
  danger: number
}

/**
 * Props 接口
 */
interface ProgressBarProps {
  /** 组件 ID */
  widgetId: string
  /** 标题 */
  title?: string
  /** 数据源配置 */
  dataSource?: DataSourceConfig
  /** 当前进度字段 */
  currentField?: string
  /** 总量字段 */
  totalField?: string
  /** 当前进度值（直接传值） */
  current?: number
  /** 总量值（直接传值） */
  total?: number
  /** 当前值标签 */
  currentLabel?: string
  /** 总量标签 */
  totalLabel?: string
  /** 单位 */
  unit?: string
  /** 进度条类型 */
  type?: ProgressType
  /** 颜色方案 */
  colorScheme?: ProgressColorScheme
  /** 自定义颜色 */
  customColor?: string
  /** 自定义渐变色 */
  customGradient?: [string, string]
  /** 是否显示百分比 */
  showPercentage?: boolean
  /** 是否显示状态 */
  showStatus?: boolean
  /** 是否显示阈值标签 */
  showThresholdLabels?: boolean
  /** 阈值配置 */
  thresholds?: ThresholdConfig
  /** 线性进度条高度 */
  strokeWidth?: number
  /** 是否条纹动画 */
  striped?: boolean
  /** 是否流动条纹 */
  stripedFlow?: boolean
  /** 圆形进度条尺寸 */
  circleSize?: number
  /** 圆形进度条描边宽度 */
  circleStrokeWidth?: number
  /** 仪表盘尺寸 */
  dashboardSize?: number
  /** 分段数 */
  segmentCount?: number
  /** 是否显示刷新按钮 */
  showRefreshButton?: boolean
  /** 数据刷新间隔（秒） */
  refreshInterval?: number
}

// Props 定义
const props = withDefaults(defineProps<ProgressBarProps>(), {
  title: '',
  currentField: 'current',
  totalField: 'total',
  current: 0,
  total: 100,
  currentLabel: '',
  totalLabel: '',
  unit: '',
  type: 'linear',
  colorScheme: 'primary',
  customColor: '#409EFF',
  customGradient: () => ['#409EFF', '#67C23A'],
  showPercentage: true,
  showStatus: true,
  showThresholdLabels: false,
  strokeWidth: 12,
  striped: false,
  stripedFlow: false,
  circleSize: 120,
  circleStrokeWidth: 6,
  dashboardSize: 120,
  segmentCount: 10,
  showRefreshButton: false,
  refreshInterval: 0
})

// Emits 定义
const emit = defineEmits<{
  /** 数据刷新时触发 */
  refresh: []
}>()

// 状态
const loading = ref(false)
const currentValue = ref(props.current)
const totalValue = ref(props.total)
const bindingId = ref(`${props.widgetId}-progress`)

// 计算属性
const percentage = computed(() => {
  if (totalValue.value <= 0) return 0
  const pct = (currentValue.value / totalValue.value) * 100
  return Math.min(100, Math.max(0, pct))
})

const displayValue = computed(() => {
  return currentValue.value.toLocaleString()
})

const statusLevel = computed(() => {
  if (!props.thresholds) return 'success'
  if (percentage.value >= props.thresholds.danger) return 'danger'
  if (percentage.value >= props.thresholds.warning) return 'warning'
  return 'success'
})

const statusText = computed(() => {
  const texts = {
    success: '正常',
    warning: '警告',
    danger: '危险'
  }
  return texts[statusLevel.value as keyof typeof texts]
})

const progressColor = computed(() => {
  if (props.colorScheme === 'custom') {
    return props.customColor
  }

  if (props.colorScheme === 'gradient') {
    return props.customGradient
  }

  // 根据状态使用不同颜色
  if (statusLevel.value === 'danger') {
    return '#F56C6C'
  }
  if (statusLevel.value === 'warning') {
    return '#E6A23C'
  }

  const colors: Record<ProgressColorScheme, string> = {
    primary: '#409EFF',
    success: '#67C23A',
    warning: '#E6A23C',
    danger: '#F56C6C',
    gradient: props.customGradient as string,
    custom: props.customColor
  }

  return colors[props.colorScheme] || colors.primary
})

const thresholdLabels = computed(() => {
  if (!props.thresholds) return []

  const labels: Array<{ position: number; value: number; level: string }> = []

  if (props.thresholds.warning > 0) {
    labels.push({
      position: props.thresholds.warning,
      value: Math.round((props.thresholds.warning / 100) * totalValue.value),
      level: 'warning'
    })
  }

  if (props.thresholds.danger > 0) {
    labels.push({
      position: props.thresholds.danger,
      value: Math.round((props.thresholds.danger / 100) * totalValue.value),
      level: 'danger'
    })
  }

  return labels.sort((a, b) => a.position - b.position)
})

const activeSegments = computed(() => {
  return Math.ceil((percentage.value / 100) * props.segmentCount)
})

const segments = computed(() => {
  const segs: Array<{ ratio: number; color: string; level: string }> = []
  const segmentSize = 100 / props.segmentCount

  for (let i = 0; i < props.segmentCount; i++) {
    const pos = ((i + 1) / props.segmentCount) * 100
    let level = 'success'
    let color = '#67C23A'

    if (props.thresholds) {
      if (pos >= props.thresholds.danger) {
        level = 'danger'
        color = '#F56C6C'
      } else if (pos >= props.thresholds.warning) {
        level = 'warning'
        color = '#E6A23C'
      }
    }

    segs.push({ ratio: 1, color, level })
  }

  return segs
})

/**
 * 加载数据
 */
async function loadData(): Promise<void> {
  loading.value = true

  try {
    const data = dataBinder.getData(bindingId.value)

    if (data && typeof data === 'object') {
      const record = data as Record<string, unknown>
      currentValue.value = (record[props.currentField] as number) ?? props.current
      totalValue.value = (record[props.totalField] as number) ?? props.total
    }
  } catch (err) {
    console.error('[ProgressBarWidget] 加载数据失败:', err)
  } finally {
    loading.value = false
  }
}

/**
 * 刷新数据
 */
function handleRefresh(): void {
  emit('refresh')
  loadData()
}

/**
 * 初始化数据绑定
 */
function initDataBinding(): void {
  if (!props.dataSource) {
    return
  }

  const sourceId = `${props.widgetId}-source`
  dataBinder.registerSource(
    {
      id: sourceId,
      ...props.dataSource
    },
    async () => {
      return fetchProgressData()
    }
  )

  dataBinder.bind({
    widgetId: bindingId.value,
    sourceId,
    fieldMapping: {},
    refreshInterval: props.refreshInterval
  })

  loadData()
}

/**
 * 获取进度数据（模拟）
 */
async function fetchProgressData(): Promise<unknown> {
  // TODO: 替换为实际 API 调用
  return {
    [props.currentField]: 75,
    [props.totalField]: 100
  }
}

// 监听数据源变化
watch(() => props.dataSource, initDataBinding, { immediate: true })

// 监听 DataBinder 数据更新
watch(
  () => dataBinder.getBindingState(bindingId.value)?.value.data,
  (newData) => {
    if (newData && typeof newData === 'object') {
      const record = newData as Record<string, unknown>
      currentValue.value = (record[props.currentField] as number) ?? props.current
      totalValue.value = (record[props.totalField] as number) ?? props.total
    }
  }
)

// 监听直接传值变化
watch([() => props.current, () => props.total], ([newCurrent, newTotal]) => {
  currentValue.value = newCurrent
  totalValue.value = newTotal
})

onMounted(() => {
  if (!props.dataSource) {
    fetchProgressData().then(data => {
      if (data && typeof data === 'object') {
        const record = data as Record<string, unknown>
        currentValue.value = (record[props.currentField] as number) ?? props.current
        totalValue.value = (record[props.totalField] as number) ?? props.total
      }
    })
  }
})

onUnmounted(() => {
  dataBinder.unbind(bindingId.value)
})

// 暴露方法给父组件
defineExpose({
  refresh: handleRefresh
})
</script>

<style scoped lang="scss">
.progress-bar-widget {
  width: 100%;
  height: 100%;
  padding: 16px;
  background: #fff;
  border-radius: 4px;
  display: flex;
  flex-direction: column;
}

.progress-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;

  .progress-title {
    margin: 0;
    font-size: 14px;
    font-weight: 500;
    color: #333;
  }
}

.progress-content {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

// 线性进度条
.linear-progress-wrapper {
  width: 100%;
}

.progress-info {
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
  font-size: 13px;

  .progress-label {
    color: #606266;
  }

  .progress-percent {
    font-weight: 600;
  }
}

.progress-thresholds {
  position: relative;
  height: 20px;
  margin-top: 4px;

  .threshold-label {
    position: absolute;
    transform: translateX(-50%);
    font-size: 11px;
    color: #909399;

    &.threshold-warning {
      color: #E6A23C;
    }

    &.threshold-danger {
      color: #F56C6C;
    }
  }
}

// 圆形进度条
.circle-progress-wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.circle-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;

  .circle-value {
    font-size: 28px;
    font-weight: 600;
  }

  .circle-percent {
    font-size: 12px;
    color: #909399;
  }
}

.circle-labels {
  display: flex;
  align-items: baseline;
  gap: 4px;
  font-size: 14px;

  .circle-label {
    &.total {
      color: #909399;
    }
  }
}

// 仪表盘进度条
.dashboard-progress-wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.dashboard-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;

  .dashboard-value {
    font-weight: 600;
  }

  .dashboard-percent {
    font-size: 12px;
    color: #909399;
  }
}

.dashboard-label {
  font-size: 14px;
  color: #606266;
}

// 分段进度条
.segmented-progress-wrapper {
  width: 100%;
}

.segmented-bar {
  display: flex;
  gap: 4px;
  height: 20px;

  .segment {
    flex: 1;
    border-radius: 2px;
    background: #f0f2f5;
    transition: all 0.3s;

    &.active {
      background: #409eff;
    }

    &.is-warning.active {
      background: #E6A23C;
    }

    &.is-danger.active {
      background: #F56C6C;
    }
  }
}

.segment-labels {
  display: flex;
  justify-content: space-between;
  margin-top: 8px;
  font-size: 11px;
  color: #909399;
}

// 状态指示
.progress-status {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 8px 16px;
  margin-top: 12px;
  border-radius: 4px;
  font-size: 13px;

  &.status-success {
    background: #f0f9ff;
    color: #67c23a;
  }

  &.status-warning {
    background: #fdf6ec;
    color: #e6a23c;
  }

  &.status-danger {
    background: #fef0f0;
    color: #f56c6c;
  }
}

// Element Plus 进度条样式覆盖
:deep(.el-progress) {
  .el-progress__text {
    font-size: 14px !important;
  }
}

:deep(.el-progress-bar__inner) {
  transition: all 0.3s;
}
</style>
