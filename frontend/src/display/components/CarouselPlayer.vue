<template>
  <div
    class="carousel-player"
    :class="{ 'is-paused': isPaused, 'is-transitioning': isTransitioning }"
    @mouseenter="handleMouseEnter"
    @mouseleave="handleMouseLeave"
  >
    <!-- 转场遮罩 -->
    <transition :name="currentTransition">
      <div v-if="!isTransitioning" :key="currentDashboardId" class="carousel-slide">
        <!-- 看板内容 -->
        <DisplayDashboard
          :dashboard-id="currentDashboardId || ''"
          :show-header="showDashboardName"
          display-mode="fullscreen"
          :auto-refresh="autoRefresh"
        />
      </div>
    </transition>

    <!-- 覆盖层 -->
    <div class="carousel-overlay">
      <!-- 左上角：时钟 -->
      <div v-if="showClock" class="overlay-left">
        <ClockWidget :compact="false" />
      </div>

      <!-- 右上角：系统状态 -->
      <div class="overlay-right">
        <SystemStatusWidget ref="statusWidget" />
      </div>

      <!-- 底部：看板名称和页码 -->
      <div v-if="showDashboardName || dashboardIds.length > 1" class="overlay-bottom">
        <!-- 看板名称 -->
        <div v-if="showDashboardName" class="dashboard-name">
          {{ currentDashboardName }}
        </div>

        <!-- 页码指示器 -->
        <div v-if="dashboardIds.length > 1" class="page-indicator">
          <span class="current">{{ currentIndex + 1 }}</span>
          <span class="separator">/</span>
          <span class="total">{{ dashboardIds.length }}</span>
        </div>

        <!-- 进度条 -->
        <div v-if="dashboardIds.length > 1 && interval > 0" class="progress-bar">
          <div
            class="progress-fill"
            :style="{ animationDuration: `${interval}s` }"
          />
        </div>
      </div>
    </div>

    <!-- 手动控制按钮（鼠标悬停时显示） -->
    <transition name="fade">
      <div v-show="isPaused" class="carousel-controls">
        <el-button
          circle
          :size="controlSize"
          :icon="ArrowLeft"
          @click="prev"
        />
        <el-button
          v-if="isPaused"
          circle
          :size="controlSize"
          :icon="VideoPlay"
          @click="resume"
        />
        <el-button
          v-else
          circle
          :size="controlSize"
          :icon="VideoPause"
          @click="pause"
        />
        <el-button
          circle
          :size="controlSize"
          :icon="ArrowRight"
          @click="next"
        />
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { ArrowLeft, ArrowRight, VideoPlay, VideoPause } from '@element-plus/icons-vue'
import { useCarousel } from '../composables/useCarousel'
import { useAutoRefresh } from '../composables/useAutoRefresh'
import ClockWidget from './ClockWidget.vue'
import SystemStatusWidget from './SystemStatusWidget.vue'
import DisplayDashboard from './DisplayDashboard.vue'
import type { TransitionType } from '../types/display'

interface Props {
  /** 看板 ID 列表 */
  dashboardIds: string[]
  /** 轮播间隔（秒） */
  interval: number
  /** 转场效果 */
  transition: TransitionType
  /** 数据刷新间隔（秒） */
  autoRefresh: number
  /** 是否显示时钟 */
  showClock: boolean
  /** 是否显示看板名称 */
  showName: boolean
  /** 是否循环 */
  loop?: boolean
  /** 是否悬停暂停 */
  pauseOnHover?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  loop: true,
  pauseOnHover: true
})

// 看板名称映射
const dashboardNames = ref<Record<string, string>>({})

// 使用轮播 composable
const {
  currentIndex,
  isTransitioning,
  isPaused,
  currentDashboardId,
  start,
  stop,
  next,
  prev,
  goTo,
  pause,
  resume
} = useCarousel(
  computed(() => props.dashboardIds),
  computed(() => props.interval),
  {
    loop: props.loop,
    pauseOnHover: props.pauseOnHover
  }
)

// 当前转场效果
const currentTransition = computed(() => {
  return props.transition === 'none' ? '' : props.transition
})

// 当前看板名称
const currentDashboardName = computed(() => {
  return currentDashboardId.value ? dashboardNames.value[currentDashboardId.value] || '' : ''
})

// 控制按钮大小（根据分辨率）
const controlSize = computed(() => {
  const width = window.innerWidth
  if (width >= 3840) return 'large'
  if (width >= 2560) return 'default'
  return 'small'
})

// 系统状态组件引用
const statusWidget = ref<InstanceType<typeof SystemStatusWidget> | null>(null)

/**
 * 处理鼠标进入
 */
function handleMouseEnter(): void {
  if (props.pauseOnHover) {
    pause()
  }
}

/**
 * 处理鼠标离开
 */
function handleMouseLeave(): void {
  if (props.pauseOnHover) {
    resume()
  }
}

/**
 * 加载看板名称
 */
async function loadDashboardNames(): Promise<void> {
  for (const id of props.dashboardIds) {
    try {
      const response = await fetch(`/api/dashboard/${id}`)
      if (response.ok) {
        const data = await response.json()
        if (data.success) {
          dashboardNames.value[id] = data.data.name
        }
      }
    } catch (error) {
      console.error('[CarouselPlayer] 加载看板名称失败:', error)
    }
  }
}

// 监听看板 ID 列表变化
watch(
  () => props.dashboardIds,
  (newIds) => {
    if (newIds.length > 0) {
      loadDashboardNames()
      start()
    } else {
      stop()
    }
  },
  { immediate: true }
)

// 组件挂载时
onMounted(() => {
  if (props.dashboardIds.length > 0) {
    loadDashboardNames()
    start()
  }
})

// 组件卸载时
onUnmounted(() => {
  stop()
})

// 暴露方法
defineExpose({
  start,
  stop,
  next,
  prev,
  pause,
  resume,
  goTo,
  refreshStatus: () => statusWidget.value?.refresh()
})
</script>

<style scoped>
.carousel-player {
  position: relative;
  width: 100%;
  height: 100%;
  overflow: hidden;
  background: var(--el-bg-color-page);
}

.carousel-slide {
  width: 100%;
  height: 100%;
}

/* 转场动画 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.5s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.slide-enter-active,
.slide-leave-active {
  transition: transform 0.5s ease;
}

.slide-enter-from {
  transform: translateX(100%);
}

.slide-leave-to {
  transform: translateX(-100%);
}

/* 覆盖层 */
.carousel-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  pointer-events: none;
  z-index: 10;
}

.overlay-left {
  position: absolute;
  top: 2rem;
  left: 3rem;
}

.overlay-right {
  position: absolute;
  top: 2rem;
  right: 3rem;
}

.overlay-bottom {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  padding: 1.5rem 3rem;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 2rem;
  background: linear-gradient(to top, rgba(0, 0, 0, 0.5), transparent);
}

.dashboard-name {
  font-size: 1.25rem;
  font-weight: 600;
  color: white;
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
}

.page-indicator {
  display: flex;
  align-items: baseline;
  gap: 0.25rem;
  font-size: 1rem;
  color: white;
  font-weight: 500;
}

.page-indicator .current {
  font-size: 1.5rem;
  font-weight: 600;
}

.page-indicator .separator {
  opacity: 0.7;
}

.progress-bar {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: rgba(255, 255, 255, 0.2);
}

.progress-fill {
  height: 100%;
  background: var(--el-color-primary);
  animation-name: progress;
  animation-timing-function: linear;
  animation-iteration-count: infinite;
}

@keyframes progress {
  from {
    width: 0%;
  }
  to {
    width: 100%;
  }
}

/* 暂停时停止进度条动画 */
.is-paused .progress-fill {
  animation-play-state: paused;
}

/* 控制按钮 */
.carousel-controls {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  display: flex;
  gap: 1rem;
  pointer-events: auto;
  z-index: 20;
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
  .overlay-left,
  .overlay-right {
    top: 3rem;
  }

  .overlay-left {
    left: 4rem;
  }

  .overlay-right {
    right: 4rem;
  }

  .overlay-bottom {
    padding: 2rem 4rem;
  }

  .dashboard-name {
    font-size: 1.5rem;
  }

  .page-indicator {
    font-size: 1.125rem;
  }

  .page-indicator .current {
    font-size: 1.75rem;
  }
}

@media (min-width: 3840px) {
  .overlay-left,
  .overlay-right {
    top: 4rem;
  }

  .overlay-left {
    left: 6rem;
  }

  .overlay-right {
    right: 6rem;
  }

  .overlay-bottom {
    padding: 3rem 6rem;
    gap: 3rem;
  }

  .dashboard-name {
    font-size: 2rem;
  }

  .page-indicator {
    font-size: 1.25rem;
  }

  .page-indicator .current {
    font-size: 2rem;
  }

  .progress-bar {
    height: 4px;
  }
}
</style>
