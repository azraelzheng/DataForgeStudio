<template>
  <div class="system-status-widget">
    <div class="status-item">
      <el-icon class="status-icon" :class="{ online: systemStatus.api }">
        <component :is="systemStatus.api ? 'CircleCheck' : 'CircleClose'" />
      </el-icon>
      <span class="status-label">API</span>
    </div>
    <div class="status-item">
      <el-icon class="status-icon" :class="{ online: systemStatus.database }">
        <component :is="systemStatus.database ? 'CircleCheck' : 'CircleClose'" />
      </el-icon>
      <span class="status-label">数据库</span>
    </div>
    <div v-if="lastUpdate" class="last-update">
      最后更新: {{ formatTime(lastUpdate) }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { CircleCheck, CircleClose } from '@element-plus/icons-vue'

/**
 * 系统状态
 */
interface SystemStatus {
  api: boolean
  database: boolean
}

const systemStatus = ref<SystemStatus>({
  api: false,
  database: false
})

const lastUpdate = ref<Date | null>(null)

let checkTimer: ReturnType<typeof setInterval> | null = null

/**
 * 检查系统状态
 */
async function checkStatus(): Promise<void> {
  try {
    // 检查 API 状态
    const apiResponse = await fetch('/health', {
      method: 'GET',
      cache: 'no-cache'
    })
    systemStatus.value.api = apiResponse.ok

    // 检查数据库状态（通过 API 响应）
    if (apiResponse.ok) {
      const data = await apiResponse.json()
      systemStatus.value.database = data.database?.status === 'healthy'
    } else {
      systemStatus.value.database = false
    }

    lastUpdate.value = new Date()
  } catch (error) {
    console.error('[SystemStatusWidget] 健康检查失败:', error)
    systemStatus.value.api = false
    systemStatus.value.database = false
    lastUpdate.value = new Date()
  }
}

/**
 * 格式化时间
 */
function formatTime(date: Date): string {
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  const seconds = String(date.getSeconds()).padStart(2, '0')
  return `${hours}:${minutes}:${seconds}`
}

onMounted(() => {
  checkStatus()
  // 每 30 秒检查一次
  checkTimer = setInterval(checkStatus, 30000)
})

onUnmounted(() => {
  if (checkTimer) {
    clearInterval(checkTimer)
  }
})

// 暴露刷新方法
defineExpose({
  refresh: checkStatus
})
</script>

<style scoped>
.system-status-widget {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.5rem 1rem;
  background: var(--el-fill-color-light);
  border-radius: 4px;
  font-size: 0.875rem;
}

.status-item {
  display: flex;
  align-items: center;
  gap: 0.375rem;
}

.status-icon {
  font-size: 1rem;
  color: var(--el-color-danger);
  transition: color 0.3s;
}

.status-icon.online {
  color: var(--el-color-success);
}

.status-label {
  font-weight: 500;
}

.last-update {
  margin-left: auto;
  opacity: 0.6;
  font-size: 0.75rem;
}

/* 响应式适配 */
@media (min-width: 3840px) {
  .system-status-widget {
    font-size: 1rem;
    padding: 0.75rem 1.5rem;
  }

  .status-icon {
    font-size: 1.25rem;
  }
}
</style>
