<!--
  NumberCardWidget - 数字卡片组件
  用于显示关键指标数值，支持趋势显示
-->
<template>
  <div class="number-card-widget" :class="[`color-scheme-${colorScheme}`, { 'is-loading': loading }]">
    <!-- 标题 -->
    <div v-if="title" class="card-header">
      <h3 class="card-title">{{ title }}</h3>
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

    <!-- 数值区域 -->
    <div class="card-content">
      <!-- 加载中 -->
      <div v-if="loading" class="card-loading">
        <el-icon class="is-loading" :size="24">
          <Loading />
        </el-icon>
      </div>

      <!-- 数值显示 -->
      <template v-else>
        <div class="card-value" :style="{ color: displayColor }">
          <span v-if="prefix" class="value-prefix">{{ prefix }}</span>
          <span class="value-number">{{ formattedValue }}</span>
          <span v-if="suffix" class="value-suffix">{{ suffix }}</span>
        </div>

        <!-- 趋势显示 -->
        <div v-if="showTrend && trendValue !== null" class="card-trend">
          <el-icon
            :size="14"
            :color="trendColor"
          >
            <ArrowUp v-if="trendValue > 0" />
            <ArrowDown v-else-if="trendValue < 0" />
            <Minus v-else />
          </el-icon>
          <span :style="{ color: trendColor }">{{ trendText }}</span>
        </div>

        <!-- 副标题/说明 -->
        <div v-if="subtitle" class="card-subtitle">
          {{ subtitle }}
        </div>
      </template>
    </div>

    <!-- 底部操作 -->
    <div v-if="$slots.extra" class="card-extra">
      <slot name="extra" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, type PropType } from 'vue'
import { Refresh, Loading, ArrowUp, ArrowDown, Minus } from '@element-plus/icons-vue'
import type { DataSourceConfig } from '../types/dashboard'
import { dataBinder } from '../core/DataBinder'

/**
 * 颜色方案
 */
export type ColorScheme = 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'custom'

/**
 * Props 接口
 */
interface NumberCardProps {
  /** 组件 ID */
  widgetId: string
  /** 标题 */
  title?: string
  /** 副标题 */
  subtitle?: string
  /** 数据源配置 */
  dataSource?: DataSourceConfig
  /** 数值字段 */
  field?: string
  /** 前缀 */
  prefix?: string
  /** 后缀 */
  suffix?: string
  /** 小数位数 */
  decimals?: number
  /** 是否显示趋势 */
  showTrend?: boolean
  /** 趋势对比字段 */
  trendField?: string
  /** 趋势标签 */
  trendLabel?: string
  /** 颜色方案 */
  colorScheme?: ColorScheme
  /** 自定义颜色 */
  customColor?: string
  /** 是否显示刷新按钮 */
  showRefreshButton?: boolean
  /** 数据刷新间隔（秒） */
  refreshInterval?: number
}

// Props 定义
const props = withDefaults(defineProps<NumberCardProps>(), {
  title: '',
  subtitle: '',
  field: 'value',
  prefix: '',
  suffix: '',
  decimals: 0,
  showTrend: false,
  trendField: 'trend',
  trendLabel: '环比',
  colorScheme: 'primary',
  customColor: '#409EFF',
  showRefreshButton: false,
  refreshInterval: 0
})

// Emits 定义
const emit = defineEmits<{
  /** 数据刷新时触发 */
  refresh: []
  /** 卡片点击时触发 */
  click: []
}>()

// 状态
const loading = ref(false)
const currentValue = ref<number | null>(null)
const currentTrend = ref<number | null>(null)
const bindingId = ref(`${props.widgetId}-numbercard`)

// 计算属性
const formattedValue = computed(() => {
  if (currentValue.value === null) return '--'
  return currentValue.value.toFixed(props.decimals)
})

const trendValue = computed(() => {
  return currentTrend.value
})

const trendText = computed(() => {
  if (trendValue.value === null) return ''
  const sign = trendValue.value > 0 ? '+' : ''
  return `${props.trendLabel} ${sign}${trendValue.value.toFixed(props.decimals)}${props.suffix}`
})

const trendColor = computed(() => {
  if (trendValue.value === null) return '#909399'
  if (trendValue.value > 0) return '#67C23A'
  if (trendValue.value < 0) return '#F56C6C'
  return '#909399'
})

const displayColor = computed(() => {
  if (props.colorScheme === 'custom') {
    return props.customColor
  }

  const colors: Record<ColorScheme, string> = {
    primary: '#409EFF',
    success: '#67C23A',
    warning: '#E6A23C',
    danger: '#F56C6C',
    info: '#909399',
    custom: props.customColor
  }

  return colors[props.colorScheme] || colors.primary
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
      currentValue.value = (record[props.field] as number) ?? null
      currentTrend.value = props.showTrend ? ((record[props.trendField] as number) ?? null) : null
    }
  } catch (err) {
    console.error('[NumberCardWidget] 加载数据失败:', err)
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
      return fetchCardData()
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
 * 获取卡片数据（模拟）
 */
async function fetchCardData(): Promise<unknown> {
  // TODO: 替换为实际 API 调用
  return {
    [props.field]: 12345.678,
    ...(props.showTrend && { [props.trendField]: 12.5 })
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
      currentValue.value = (record[props.field] as number) ?? null
      currentTrend.value = props.showTrend ? ((record[props.trendField] as number) ?? null) : null
    }
  }
)

onMounted(() => {
  if (!props.dataSource) {
    fetchCardData().then(data => {
      if (data && typeof data === 'object') {
        const record = data as Record<string, unknown>
        currentValue.value = (record[props.field] as number) ?? null
        currentTrend.value = props.showTrend ? ((record[props.trendField] as number) ?? null) : null
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
.number-card-widget {
  width: 100%;
  height: 100%;
  padding: 20px;
  background: #fff;
  border-radius: 4px;
  display: flex;
  flex-direction: column;
  transition: all 0.3s;

  &:hover {
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  }

  &.is-loading {
    .card-value {
      opacity: 0.5;
    }
  }
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;

  .card-title {
    margin: 0;
    font-size: 14px;
    font-weight: 500;
    color: #606266;
  }
}

.card-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  position: relative;
  min-height: 80px;
}

.card-loading {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  color: #409eff;
}

.card-value {
  display: flex;
  align-items: baseline;
  justify-content: flex-start;
  margin-bottom: 8px;

  .value-prefix {
    font-size: 16px;
    margin-right: 4px;
    opacity: 0.8;
  }

  .value-number {
    font-size: 32px;
    font-weight: 600;
    line-height: 1;
  }

  .value-suffix {
    font-size: 16px;
    margin-left: 4px;
    opacity: 0.8;
  }
}

.card-trend {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
  margin-top: 4px;
}

.card-subtitle {
  font-size: 12px;
  color: #909399;
  margin-top: 8px;
}

.card-extra {
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px solid #eee;
}

// 颜色方案
.color-scheme-primary {
  .card-value .value-number {
    color: #409eff;
  }
}

.color-scheme-success {
  .card-value .value-number {
    color: #67c23a;
  }
}

.color-scheme-warning {
  .card-value .value-number {
    color: #e6a23c;
  }
}

.color-scheme-danger {
  .card-value .value-number {
    color: #f56c6c;
  }
}

.color-scheme-info {
  .card-value .value-number {
    color: #909399;
  }
}
</style>
