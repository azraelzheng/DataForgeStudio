<template>
  <div class="display-dashboard" :class="containerClass">
    <!-- 加载状态 -->
    <div v-if="isLoading" class="dashboard-loading">
      <el-icon class="is-loading" :size="48">
        <Loading />
      </el-icon>
      <p>加载看板数据...</p>
    </div>

    <!-- 错误状态 -->
    <div v-else-if="error" class="dashboard-error">
      <el-icon :size="48" color="var(--el-color-danger)">
        <CircleClose />
      </el-icon>
      <p>{{ error }}</p>
      <el-button type="primary" @click="loadDashboard">重试</el-button>
    </div>

    <!-- 看板内容 -->
    <div v-else-if="dashboard" class="dashboard-content">
      <!-- 看板头部 -->
      <header v-if="showHeader" class="dashboard-header">
        <h1 class="dashboard-title">{{ dashboard.name }}</h1>
        <p v-if="dashboard.description" class="dashboard-description">
          {{ dashboard.description }}
        </p>
      </header>

      <!-- 看板网格 -->
      <div class="dashboard-grid" :style="gridStyle">
        <div
          v-for="widget in widgets"
          :key="widget.id"
          class="widget-container"
          :style="widgetStyle(widget)"
        >
          <component
            :is="widgetComponent(widget.type)"
            v-bind="widget.config"
            :data-binding="widget.dataBinding"
            :is-readonly="true"
          />
        </div>
      </div>
    </div>

    <!-- 空状态 -->
    <div v-else class="dashboard-empty">
      <el-empty description="看板不存在或已被删除" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { Loading, CircleClose } from '@element-plus/icons-vue'
import { ComponentRegistry } from '@/dashboard/core/ComponentRegistry'
import type { Dashboard, WidgetInstance } from '@/dashboard/types/dashboard'
import type { DisplayMode as DisplayModeType } from '@/dashboard/types/dashboard'

interface Props {
  /** 看板 ID */
  dashboardId: string
  /** 是否显示头部 */
  showHeader?: boolean
  /** 显示模式 */
  displayMode?: DisplayModeType
  /** 自动刷新间隔（秒） */
  autoRefresh?: number
}

const props = withDefaults(defineProps<Props>(), {
  showHeader: true,
  displayMode: 'fullscreen',
  autoRefresh: 0
})

// 状态
const dashboard = ref<Dashboard | null>(null)
const widgets = ref<WidgetInstance[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)

/**
 * 容器类名
 */
const containerClass = computed(() => ({
  'is-loading': isLoading.value,
  'has-error': !!error.value,
  [`mode-${props.displayMode}`]: true
}))

/**
 * 网格样式
 */
const gridStyle = computed(() => {
  if (!dashboard.value) {
    return {}
  }

  const { columns, gap } = dashboard.value.layout
  return {
    display: 'grid',
    gridTemplateColumns: `repeat(${columns}, 1fr)`,
    gap: `${gap}px`
  }
})

/**
 * 组件样式
 */
function widgetStyle(widget: WidgetInstance) {
  if (!dashboard.value) {
    return {}
  }

  const { rowHeight } = dashboard.value.layout
  const { x, y, width, height } = widget.position

  return {
    gridColumn: `${x + 1} / span ${width}`,
    gridRow: `${y + 1} / span ${height}`,
    minHeight: `${height * rowHeight}px`
  }
}

/**
 * 获取组件
 */
function widgetComponent(type: string) {
  return ComponentRegistry.get(type)?.component
}

/**
 * 加载看板数据
 */
async function loadDashboard(): Promise<void> {
  if (!props.dashboardId) {
    error.value = '看板 ID 不能为空'
    return
  }

  isLoading.value = true
  error.value = null

  try {
    const response = await fetch(`/api/dashboard/${props.dashboardId}`, {
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`)
    }

    const apiResponse = await response.json()

    if (!apiResponse.success) {
      throw new Error(apiResponse.message || '加载看板失败')
    }

    dashboard.value = apiResponse.data
    widgets.value = apiResponse.data.widgets || []
  } catch (err) {
    console.error('[DisplayDashboard] 加载看板失败:', err)
    error.value = err instanceof Error ? err.message : '加载看板失败'
  } finally {
    isLoading.value = false
  }
}

/**
 * 刷新看板数据
 */
async function refreshDashboard(): Promise<void> {
  await loadDashboard()
}

// 监听 dashboardId 变化
watch(() => props.dashboardId, () => {
  loadDashboard()
})

// 组件挂载时加载数据
onMounted(() => {
  loadDashboard()
})

// 暴露刷新方法
defineExpose({
  refresh: refreshDashboard
})
</script>

<style scoped>
.display-dashboard {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.dashboard-loading,
.dashboard-error,
.dashboard-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  padding: 2rem;
  color: var(--el-text-color-secondary);
}

.dashboard-loading p,
.dashboard-error p {
  margin-top: 1rem;
  font-size: 1rem;
}

.dashboard-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.dashboard-header {
  padding: 1.5rem 2rem;
  border-bottom: 1px solid var(--el-border-color);
  background: var(--el-bg-color);
}

.dashboard-title {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.dashboard-description {
  margin: 0.5rem 0 0;
  font-size: 0.875rem;
  color: var(--el-text-color-secondary);
}

.dashboard-grid {
  flex: 1;
  padding: 1.5rem;
  overflow-y: auto;
}

.widget-container {
  background: var(--el-bg-color);
  border-radius: 8px;
  overflow: hidden;
}

/* 全屏模式样式 */
.mode-fullscreen .dashboard-header {
  padding: 2rem 3rem;
}

.mode-fullscreen .dashboard-title {
  font-size: 2rem;
}

.mode-fullscreen .dashboard-grid {
  padding: 2rem 3rem;
}

/* 响应式适配 */
@media (min-width: 2560px) {
  .mode-fullscreen .dashboard-title {
    font-size: 2.5rem;
  }
}

@media (min-width: 3840px) {
  .mode-fullscreen .dashboard-header {
    padding: 3rem 4rem;
  }

  .mode-fullscreen .dashboard-title {
    font-size: 3rem;
  }

  .mode-fullscreen .dashboard-description {
    font-size: 1.125rem;
  }

  .mode-fullscreen .dashboard-grid {
    padding: 3rem 4rem;
  }
}
</style>
