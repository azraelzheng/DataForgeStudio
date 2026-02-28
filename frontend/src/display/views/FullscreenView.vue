<template>
  <div
    ref="containerRef"
    class="fullscreen-view"
    :class="{ 'is-fullscreen': isFullscreen }"
  >
    <!-- 加载状态 -->
    <div v-if="isLoading" class="loading-state">
      <el-icon class="is-loading" :size="64">
        <Loading />
      </el-icon>
      <p>加载大屏配置...</p>
    </div>

    <!-- 错误状态 -->
    <div v-else-if="error" class="error-state">
      <el-icon :size="64" color="var(--el-color-danger)">
        <CircleClose />
      </el-icon>
      <p>{{ error }}</p>
      <el-button type="primary" @click="goBack">返回</el-button>
    </div>

    <!-- 全屏内容 -->
    <template v-else-if="config">
      <!-- 轮播播放器 -->
      <CarouselPlayer
        v-if="config.dashboardIds.length > 0"
        :dashboard-ids="config.dashboardIds"
        :interval="config.interval"
        :transition="config.transition"
        :auto-refresh="config.autoRefresh"
        :show-clock="config.showClock"
        :show-name="config.showDashboardName"
        :loop="config.loop"
        :pause-on-hover="config.pauseOnHover"
        ref="carouselRef"
      />

      <!-- 退出按钮（鼠标移动显示） -->
      <transition name="fade">
        <div v-if="showExitButton" class="exit-overlay">
          <div class="exit-bar">
            <div class="exit-info">
              <span class="config-name">{{ config.name }}</span>
              <span class="separator">|</span>
              <span class="dashboard-count">{{ config.dashboardIds.length }} 个看板</span>
            </div>
            <div class="exit-actions">
              <el-button type="danger" :icon="ExitIcon" @click="exitFullscreen">
                退出大屏 (ESC)
              </el-button>
            </div>
          </div>
        </div>
      </transition>
    </template>

    <!-- 空状态 -->
    <div v-else class="empty-state">
      <el-empty description="配置不存在或已被删除" />
      <el-button type="primary" @click="goBack">返回</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Loading, CircleClose } from '@element-plus/icons-vue'
import { useFullscreen } from '../composables/useFullscreen'
import { checkDisplayLicense } from '../utils/licenseCheck'
import CarouselPlayer from '../components/CarouselPlayer.vue'
import type { DisplayConfig } from '../types/display'

// 别名图标
const ExitIcon = CircleClose

const router = useRouter()
const route = useRoute()

// 状态
const isLoading = ref(true)
const error = ref<string | null>(null)
const config = ref<DisplayConfig | null>(null)
const showExitButton = ref(false)
const exitButtonTimer = ref<ReturnType<typeof setTimeout> | null>(null)

// 组件引用
const containerRef = ref<HTMLElement | null>(null)
const carouselRef = ref<InstanceType<typeof CarouselPlayer> | null>(null)

// 使用全屏 composable
const { isFullscreen, enterFullscreen, exitFullscreen } = useFullscreen({
  onExit: () => {
    // 退出全屏时返回
    goBack()
  }
})

/**
 * 加载配置
 */
async function loadConfig(configId: string): Promise<void> {
  isLoading.value = true
  error.value = null

  try {
    const response = await fetch(`/api/display/${configId}`, {
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`)
    }

    const data = await response.json()

    if (data.success) {
      config.value = data.data
    } else {
      error.value = data.message || '加载配置失败'
    }
  } catch (err) {
    console.error('[FullscreenView] 加载配置失败:', err)
    error.value = '加载配置失败'
  } finally {
    isLoading.value = false
  }
}

/**
 * 进入大屏模式
 */
async function enterFullscreenMode(): Promise<void> {
  // 检查许可证
  const isValid = await checkDisplayLicense()
  if (!isValid) {
    goBack()
    return
  }

  try {
    await enterFullscreen(containerRef.value || undefined)
  } catch (err) {
    console.error('[FullscreenView] 进入全屏失败:', err)
    ElMessage.error('进入全屏失败，请检查浏览器权限')
    goBack()
  }
}

/**
 * 退出全屏模式
 */
async function exitFullscreenMode(): Promise<void> {
  try {
    await exitFullscreen()
  } catch (err) {
    console.error('[FullscreenView] 退出全屏失败:', err)
  }
  goBack()
}

/**
 * 返回上一页
 */
function goBack(): void {
  router.push({ name: 'DisplayConfig' })
}

/**
 * 处理鼠标移动
 */
function handleMouseMove(): void {
  if (!isFullscreen.value) {
    return
  }

  showExitButton.value = true

  if (exitButtonTimer.value) {
    clearTimeout(exitButtonTimer.value)
  }

  exitButtonTimer.value = setTimeout(() => {
    showExitButton.value = false
  }, 3000)
}

/**
 * 处理 ESC 键
 */
function handleKeyDown(event: KeyboardEvent): void {
  if (event.key === 'Escape' && isFullscreen.value) {
    exitFullscreenMode()
  }
}

// 监听路由参数变化
watch(
  () => route.query.configId,
  async (newConfigId) => {
    if (newConfigId) {
      await loadConfig(newConfigId as string)
      if (config.value) {
        await enterFullscreenMode()
      }
    }
  },
  { immediate: true }
)

// 组件挂载
onMounted(() => {
  document.addEventListener('mousemove', handleMouseMove)
  document.addEventListener('keydown', handleKeyDown)
})

// 组件卸载
onUnmounted(() => {
  document.removeEventListener('mousemove', handleMouseMove)
  document.removeEventListener('keydown', handleKeyDown)

  if (exitButtonTimer.value) {
    clearTimeout(exitButtonTimer.value)
  }

  // 确保退出全屏
  if (isFullscreen.value) {
    exitFullscreen()
  }
})
</script>

<style scoped>
.fullscreen-view {
  width: 100%;
  height: 100vh;
  background: var(--el-bg-color-page);
  position: relative;
  overflow: hidden;
}

.fullscreen-view.is-fullscreen {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 9999;
}

/* 加载状态 */
.loading-state,
.error-state,
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  gap: 1rem;
  color: var(--el-text-color-secondary);
}

.loading-state p,
.error-state p {
  font-size: 1.125rem;
}

/* 退出覆盖层 */
.exit-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 10000;
  padding: 2rem;
  pointer-events: none;
}

.exit-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: rgba(0, 0, 0, 0.7);
  backdrop-filter: blur(10px);
  padding: 1rem 2rem;
  border-radius: 8px;
  pointer-events: auto;
}

.exit-info {
  display: flex;
  align-items: center;
  gap: 1rem;
  color: white;
}

.config-name {
  font-size: 1.125rem;
  font-weight: 600;
}

.separator {
  opacity: 0.5;
}

.dashboard-count {
  opacity: 0.8;
  font-size: 0.875rem;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* 响应式适配 */
@media (min-width: 2560px) {
  .exit-bar {
    padding: 1.25rem 2.5rem;
  }

  .config-name {
    font-size: 1.25rem;
  }

  .dashboard-count {
    font-size: 1rem;
  }
}

@media (min-width: 3840px) {
  .exit-overlay {
    padding: 3rem;
  }

  .exit-bar {
    padding: 1.5rem 3rem;
  }

  .config-name {
    font-size: 1.5rem;
  }

  .dashboard-count {
    font-size: 1.125rem;
  }

  .exit-bar .el-button {
    font-size: 1.125rem;
    padding: 0.75rem 1.5rem;
  }
}
</style>
