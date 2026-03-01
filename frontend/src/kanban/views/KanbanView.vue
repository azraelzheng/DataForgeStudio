<template>
  <div class="kanban-view-page">
    <KanbanBoard
      v-if="config"
      :dashboard-id="dashboardId"
      :config="config"
      :compact="compact"
      :readonly="readonly"
      :show-priority="showPriority"
      @load="handleLoad"
      @refresh="handleRefresh"
      @change="handleChange"
    />
    <div v-else class="loading-container">
      <el-icon class="is-loading"><Loading /></el-icon>
      <span>加载中...</span>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * KanbanView - 看板视图页面
 * 看板组件的入口页面
 */

import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Loading } from '@element-plus/icons-vue'
import KanbanBoard from '../components/KanbanBoard.vue'
import type { KanbanConfig } from '../types/kanban'

const route = useRoute()

// 状态
const dashboardId = computed(() => route.params.id as string || 'default')
const config = ref<KanbanConfig | null>(null)
const compact = ref(false)
const readonly = ref(false)
const showPriority = ref(true)

// ============================================
// 生命周期
// ============================================

onMounted(async () => {
  await loadConfig()
})

// ============================================
// 方法
// ============================================

/** 加载看板配置 */
async function loadConfig(): Promise<void> {
  try {
    const response = await fetch(`/api/kanban/${dashboardId.value}/config`)
    const result = await response.json()

    if (result.success) {
      config.value = {
        id: result.data.id.toString(),
        name: result.data.name,
        description: result.data.description,
        columns: result.data.columns || getDefaultColumns(),
        enableSwimLanes: result.data.enableSwimLanes || false,
        swimLaneBy: result.data.swimLaneBy
      }
    } else {
      // 使用默认配置
      config.value = {
        id: dashboardId.value,
        name: '默认看板',
        columns: getDefaultColumns()
      }
    }
  } catch (error) {
    console.error('加载看板配置失败:', error)
    // 使用默认配置
    config.value = {
      id: dashboardId.value,
      name: '默认看板',
      columns: getDefaultColumns()
    }
  }
}

/** 获取默认列配置 */
function getDefaultColumns() {
  return [
    { id: 'todo', title: '待处理', color: '#909399', order: 0 },
    { id: 'in-progress', title: '进行中', color: '#409eff', order: 1 },
    { id: 'review', title: '审核中', color: '#e6a23c', order: 2 },
    { id: 'done', title: '已完成', color: '#67c23a', order: 3 }
  ]
}

/** 处理加载事件 */
function handleLoad(dashboardId: string): void {
  console.log('看板加载完成:', dashboardId)
}

/** 处理刷新事件 */
function handleRefresh(dashboardId: string): void {
  console.log('看板刷新:', dashboardId)
}

/** 处理变更事件 */
function handleChange(): void {
  // 卡片变更时的处理
  console.log('看板数据已变更')
}
</script>

<style scoped>
.kanban-view-page {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  gap: 16px;
  color: var(--el-text-color-secondary);
}

.is-loading {
  font-size: 32px;
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
