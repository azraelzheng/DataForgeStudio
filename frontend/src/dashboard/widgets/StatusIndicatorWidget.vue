<!--
  StatusIndicatorWidget - 状态指示灯组件
  用于显示设备、流程等状态，支持闪烁动画
-->
<template>
  <div class="status-indicator-widget">
    <!-- 标题 -->
    <div v-if="title" class="status-header">
      <h3 class="status-title">{{ title }}</h3>
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

    <!-- 状态指示灯容器 -->
    <div class="status-content" :class="`layout-${layout}`">
      <!-- 网格布局 -->
      <template v-if="layout === 'grid'">
        <div
          v-for="(item, index) in displayItems"
          :key="index"
          class="status-item"
          :class="{ 'is-blinking': item.blink }"
          @click="handleItemClick(item)"
        >
          <div
            class="status-indicator"
            :style="{
              backgroundColor: item.color,
              boxShadow: `0 0 ${item.glowSize || 12}px ${item.color}80`
            }"
          >
            <el-icon v-if="item.icon" :size="iconSize">
              <component :is="item.icon" />
            </el-icon>
          </div>
          <div class="status-info">
            <span class="status-label">{{ item.label }}</span>
            <span v-if="showValue && item.value !== undefined" class="status-value">
              {{ item.value }}
            </span>
          </div>
        </div>
      </template>

      <!-- 列表布局 -->
      <template v-else-if="layout === 'list'">
        <div
          v-for="(item, index) in displayItems"
          :key="index"
          class="status-list-item"
          :class="{ 'is-blinking': item.blink }"
          @click="handleItemClick(item)"
        >
          <div class="status-list-indicator">
            <div
              class="status-dot"
              :style="{
                backgroundColor: item.color,
                boxShadow: `0 0 ${item.glowSize || 8}px ${item.color}80`
              }"
            />
          </div>
          <div class="status-list-content">
            <span class="status-list-label">{{ item.label }}</span>
            <span v-if="showValue && item.value !== undefined" class="status-list-value">
              {{ item.value }}
            </span>
          </div>
          <el-icon v-if="item.icon" :size="18" :color="item.color">
            <component :is="item.icon" />
          </el-icon>
        </div>
      </template>

      <!-- 卡片布局 -->
      <template v-else-if="layout === 'card'">
        <div
          v-for="(item, index) in displayItems"
          :key="index"
          class="status-card"
          :class="{ 'is-blinking': item.blink }"
          :style="{ borderColor: item.color }"
          @click="handleItemClick(item)"
        >
          <div class="status-card-header" :style="{ backgroundColor: `${item.color}10` }">
            <div
              class="status-card-dot"
              :style="{
                backgroundColor: item.color,
                boxShadow: `0 0 ${item.glowSize || 8}px ${item.color}80`
              }"
            />
            <span class="status-card-label">{{ item.label }}</span>
          </div>
          <div class="status-card-body">
            <div v-if="showValue && item.value !== undefined" class="status-card-value">
              {{ item.value }}
            </div>
            <div v-if="item.description" class="status-card-desc">
              {{ item.description }}
            </div>
          </div>
        </div>
      </template>

      <!-- 圆形仪表布局 -->
      <template v-else-if="layout === 'gauge'">
        <div
          v-for="(item, index) in displayItems"
          :key="index"
          class="status-gauge-item"
          :class="{ 'is-blinking': item.blink }"
          @click="handleItemClick(item)"
        >
          <div
            class="status-gauge-indicator"
            :style="{
              borderColor: item.color,
              backgroundColor: `${item.color}20`,
              boxShadow: `inset 0 0 ${item.glowSize || 20}px ${item.color}40`
            }"
          >
            <span class="status-gauge-label" :style="{ color: item.color }">
              {{ item.label }}
            </span>
          </div>
          <div v-if="showValue && item.value !== undefined" class="status-gauge-value">
            {{ item.value }}
          </div>
        </div>
      </template>
    </div>

    <!-- 图例 -->
    <div v-if="showLegend && legendItems.length > 0" class="status-legend">
      <div
        v-for="(legend, index) in legendItems"
        :key="index"
        class="legend-item"
      >
        <div
          class="legend-dot"
          :style="{ backgroundColor: legend.color }"
        />
        <span class="legend-label">{{ legend.label }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, markRaw, type PropType } from 'vue'
import { Refresh } from '@element-plus/icons-vue'
import {
  SuccessFilled,
  WarningFilled,
  CircleCloseFilled,
  InfoFilled,
  Loading
} from '@element-plus/icons-vue'
import type { DataSourceConfig } from '../types/dashboard'
import { dataBinder } from '../core/DataBinder'

/**
 * 状态映射项
 */
export interface StatusMappingItem {
  /** 显示标签 */
  label: string
  /** 颜色 */
  color: string
  /** 图标名称或组件 */
  icon?: string | unknown
  /** 是否闪烁 */
  blink?: boolean
  /** 描述 */
  description?: string
}

/**
 * 状态映射配置
 */
export type StatusMapping = Record<string, StatusMappingItem>

/**
 * 布局类型
 */
export type StatusLayout = 'grid' | 'list' | 'card' | 'gauge'

/**
 * 显示项
 */
interface DisplayItem {
  label: string
  value?: string | number
  color: string
  icon?: unknown
  blink?: boolean
  description?: string
  glowSize?: number
}

/**
 * Props 接口
 */
interface StatusIndicatorProps {
  /** 组件 ID */
  widgetId: string
  /** 标题 */
  title?: string
  /** 数据源配置 */
  dataSource?: DataSourceConfig
  /** 状态字段 */
  statusField?: string
  /** 状态映射 */
  statusMapping: StatusMapping
  /** 额外显示字段 */
  valueField?: string
  /** 布局类型 */
  layout?: StatusLayout
  /** 图标大小 */
  iconSize?: number
  /** 是否显示数值 */
  showValue?: boolean
  /** 是否显示图例 */
  showLegend?: boolean
  /** 警告时是否闪烁 */
  blinkOnWarning?: boolean
  /** 危险时是否闪烁 */
  blinkOnDanger?: boolean
  /** 直接传值数据 */
  data?: Array<Record<string, unknown>>
  /** 是否显示刷新按钮 */
  showRefreshButton?: boolean
  /** 数据刷新间隔（秒） */
  refreshInterval?: number
}

// Props 定义
const props = withDefaults(defineProps<StatusIndicatorProps>(), {
  title: '',
  statusField: 'status',
  valueField: 'value',
  layout: 'grid',
  iconSize: 20,
  showValue: true,
  showLegend: false,
  blinkOnWarning: true,
  blinkOnDanger: true,
  data: () => [],
  showRefreshButton: false,
  refreshInterval: 0
})

// Emits 定义
const emit = defineEmits<{
  /** 数据刷新时触发 */
  refresh: []
  /** 状态项点击时触发 */
  'item-click': [item: DisplayItem]
}>()

// 状态
const loading = ref(false)
const itemsData = ref<Array<Record<string, unknown>>>([])
const bindingId = ref(`${props.widgetId}-status`)

// 图标映射
const iconMap: Record<string, unknown> = {
  success: markRaw(SuccessFilled),
  warning: markRaw(WarningFilled),
  danger: markRaw(CircleCloseFilled),
  info: markRaw(InfoFilled),
  loading: markRaw(Loading)
}

// 计算属性
const displayItems = computed((): DisplayItem[] => {
  if (!itemsData.value || itemsData.value.length === 0) {
    // 如果没有数据，显示所有状态映射项
    return Object.entries(props.statusMapping).map(([key, config]) => {
      const icon = typeof config.icon === 'string' ? iconMap[config.icon] : config.icon
      return {
        label: config.label,
        color: config.color,
        icon,
        blink: config.blink || false,
        description: config.description
      }
    })
  }

  return itemsData.value.map(item => {
    const status = String(item[props.statusField] ?? '')
    const config = props.statusMapping[status] || {
      label: status,
      color: '#909399',
      icon: 'info'
    }

    const icon = typeof config.icon === 'string' ? iconMap[config.icon] : config.icon
    const value = props.valueField ? item[props.valueField] : undefined

    // 判断是否需要闪烁
    let blink = config.blink || false
    if (props.blinkOnWarning && config.color.includes('E6A23C')) {
      blink = true
    }
    if (props.blinkOnDanger && config.color.includes('F56C6C')) {
      blink = true
    }

    return {
      label: config.label,
      value: value as string | number,
      color: config.color,
      icon,
      blink,
      description: config.description,
      glowSize: status === 'danger' ? 16 : 12
    }
  })
})

const legendItems = computed(() => {
  return Object.values(props.statusMapping).map(config => ({
    label: config.label,
    color: config.color
  }))
})

/**
 * 处理状态项点击
 */
function handleItemClick(item: DisplayItem): void {
  emit('item-click', item)
}

/**
 * 加载数据
 */
async function loadData(): Promise<void> {
  loading.value = true

  try {
    const data = dataBinder.getData(bindingId.value)

    if (data && Array.isArray(data)) {
      itemsData.value = data
    }
  } catch (err) {
    console.error('[StatusIndicatorWidget] 加载数据失败:', err)
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
      return fetchStatusData()
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
 * 获取状态数据（模拟）
 */
async function fetchStatusData(): Promise<unknown> {
  // TODO: 替换为实际 API 调用
  return [
    { status: 'normal', value: 5 },
    { status: 'warning', value: 2 },
    { status: 'danger', value: 1 },
    { status: 'offline', value: 1 }
  ]
}

// 监听数据源变化
watch(() => props.dataSource, initDataBinding, { immediate: true })

// 监听 DataBinder 数据更新
watch(
  () => dataBinder.getBindingState(bindingId.value)?.value.data,
  (newData) => {
    if (newData && Array.isArray(newData)) {
      itemsData.value = newData
    }
  }
)

// 监听直接传值
watch(() => props.data, (newData) => {
  if (newData && newData.length > 0) {
    itemsData.value = newData
  }
}, { immediate: true })

onMounted(() => {
  if (!props.dataSource) {
    fetchStatusData().then(data => {
      if (Array.isArray(data)) {
        itemsData.value = data
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
.status-indicator-widget {
  width: 100%;
  height: 100%;
  padding: 16px;
  background: #fff;
  border-radius: 4px;
  display: flex;
  flex-direction: column;
}

.status-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;

  .status-title {
    margin: 0;
    font-size: 14px;
    font-weight: 500;
    color: #333;
  }
}

.status-content {
  flex: 1;
  display: flex;
  flex-wrap: wrap;
  align-content: flex-start;
  gap: 12px;
}

// 网格布局
.layout-grid {
  .status-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    padding: 12px;
    border-radius: 8px;
    background: #f5f7fa;
    transition: all 0.3s;
    cursor: pointer;
    flex: 1 1 calc(33.333% - 8px);
    min-width: 80px;

    &:hover {
      background: #ecf5ff;
      transform: translateY(-2px);
    }

    &.is-blinking .status-indicator {
      animation: blink 1.5s ease-in-out infinite;
    }
  }

  .status-indicator {
    width: 48px;
    height: 48px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #fff;
    transition: all 0.3s;
  }

  .status-info {
    text-align: center;

    .status-label {
      display: block;
      font-size: 12px;
      color: #606266;
    }

    .status-value {
      display: block;
      font-size: 16px;
      font-weight: 600;
      color: #303133;
    }
  }
}

// 列表布局
.layout-list {
  flex-direction: column;
  gap: 8px;

  .status-list-item {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 10px 12px;
    border-radius: 6px;
    background: #f5f7fa;
    transition: all 0.3s;
    cursor: pointer;

    &:hover {
      background: #ecf5ff;
    }

    &.is-blinking .status-dot {
      animation: blink 1.5s ease-in-out infinite;
    }
  }

  .status-list-indicator {
    flex-shrink: 0;
  }

  .status-dot {
    width: 12px;
    height: 12px;
    border-radius: 50%;
  }

  .status-list-content {
    flex: 1;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .status-list-label {
    font-size: 13px;
    color: #606266;
  }

  .status-list-value {
    font-size: 14px;
    font-weight: 600;
    color: #303133;
  }
}

// 卡片布局
.layout-card {
  .status-card {
    flex: 1 1 calc(50% - 6px);
    min-width: 140px;
    border: 2px solid;
    border-radius: 8px;
    overflow: hidden;
    transition: all 0.3s;
    cursor: pointer;

    &:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }

    &.is-blinking .status-card-dot {
      animation: blink 1.5s ease-in-out infinite;
    }
  }

  .status-card-header {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 12px;
  }

  .status-card-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    flex-shrink: 0;
  }

  .status-card-label {
    font-size: 13px;
    font-weight: 500;
    color: #303133;
  }

  .status-card-body {
    padding: 12px;
  }

  .status-card-value {
    font-size: 24px;
    font-weight: 600;
    color: #303133;
  }

  .status-card-desc {
    font-size: 12px;
    color: #909399;
    margin-top: 4px;
  }
}

// 仪表布局
.layout-gauge {
  .status-gauge-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    flex: 1 1 calc(33.333% - 8px);
    min-width: 80px;

    &.is-blinking .status-gauge-indicator {
      animation: pulse 1.5s ease-in-out infinite;
    }
  }

  .status-gauge-indicator {
    width: 60px;
    height: 60px;
    border-radius: 50%;
    border: 4px solid;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .status-gauge-label {
    font-size: 11px;
    font-weight: 600;
  }

  .status-gauge-value {
    font-size: 14px;
    font-weight: 600;
    color: #303133;
  }
}

// 图例
.status-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  padding-top: 12px;
  border-top: 1px solid #eee;
  margin-top: 8px;

  .legend-item {
    display: flex;
    align-items: center;
    gap: 6px;
  }

  .legend-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
  }

  .legend-label {
    font-size: 12px;
    color: #606266;
  }
}

// 动画
@keyframes blink {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.3;
  }
}

@keyframes pulse {
  0%, 100% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.05);
  }
}
</style>
