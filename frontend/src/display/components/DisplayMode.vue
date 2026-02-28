<template>
  <div ref="containerRef" class="display-mode" :class="{ fullscreen: isFullscreen }">
    <!-- 配置面板（非全屏时显示） -->
    <div v-if="!isFullscreen" class="display-config">
      <el-card>
        <template #header>
          <div class="card-header">
            <span>车间大屏配置</span>
            <el-button
              type="primary"
              :icon="isFullscreen ? ExitIcon : FullScreen"
              :disabled="!canStart"
              @click="toggleDisplayMode"
            >
              {{ isFullscreen ? '退出大屏' : '进入大屏' }}
            </el-button>
          </div>
        </template>

        <el-form :model="config" label-width="120px">
          <!-- 选择看板 -->
          <el-form-item label="选择看板">
            <el-select
              v-model="config.dashboardIds"
              multiple
              placeholder="请选择要展示的看板"
              style="width: 100%"
            >
              <el-option
                v-for="dashboard in availableDashboards"
                :key="dashboard.id"
                :label="dashboard.name"
                :value="dashboard.id"
              />
            </el-select>
            <div class="form-tip">可多选，将按顺序轮播展示</div>
          </el-form-item>

          <!-- 轮播间隔 -->
          <el-form-item label="轮播间隔">
            <el-input-number
              v-model="config.interval"
              :min="5"
              :max="300"
              :step="5"
            />
            <span class="unit">秒</span>
          </el-form-item>

          <!-- 数据刷新 -->
          <el-form-item label="数据刷新">
            <el-input-number
              v-model="config.autoRefresh"
              :min="10"
              :max="600"
              :step="10"
            />
            <span class="unit">秒</span>
          </el-form-item>

          <!-- 转场效果 -->
          <el-form-item label="转场效果">
            <el-radio-group v-model="config.transition">
              <el-radio-button label="fade">淡入淡出</el-radio-button>
              <el-radio-button label="slide">滑动</el-radio-button>
              <el-radio-button label="none">无</el-radio-button>
            </el-radio-group>
          </el-form-item>

          <!-- 显示选项 -->
          <el-form-item label="显示选项">
            <el-checkbox v-model="config.showClock">显示时钟</el-checkbox>
            <el-checkbox v-model="config.showDashboardName">显示看板名称</el-checkbox>
            <el-checkbox v-model="config.loop">循环播放</el-checkbox>
            <el-checkbox v-model="config.pauseOnHover">悬停暂停</el-checkbox>
          </el-form-item>

          <!-- 保存配置 -->
          <el-form-item>
            <el-button type="primary" :icon="Check" @click="saveConfig">
              保存配置
            </el-button>
            <el-button :icon="RefreshLeft" @click="resetConfig">
              重置
            </el-button>
          </el-form-item>
        </el-form>
      </el-card>
    </div>

    <!-- 轮播播放器（全屏时显示） -->
    <CarouselPlayer
      v-if="isFullscreen && config.dashboardIds.length > 0"
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

    <!-- 退出按钮（全屏时鼠标移动显示） -->
    <transition name="fade">
      <div v-if="isFullscreen && showExitButton" class="exit-button">
        <el-button type="danger" :icon="ExitIcon" @click="exitDisplayMode">
          退出大屏 (ESC)
        </el-button>
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  FullScreen,
  ExitIcon as Exit,
  Check,
  RefreshLeft
} from '@element-plus/icons-vue'
import { useFullscreen } from '../composables/useFullscreen'
import { checkDisplayLicense } from '../utils/licenseCheck'
import CarouselPlayer from './CarouselPlayer.vue'
import type { DisplayConfigCreateRequest, TransitionType } from '../types/display'

// 别名图标以避免冲突
const ExitIcon = Exit

/**
 * 配置接口
 */
interface DisplayConfig {
  dashboardIds: string[]
  interval: number
  autoRefresh: number
  transition: TransitionType
  showClock: boolean
  showDashboardName: boolean
  loop: boolean
  pauseOnHover: boolean
}

const DEFAULT_CONFIG: DisplayConfig = {
  dashboardIds: [],
  interval: 30,
  autoRefresh: 60,
  transition: 'fade',
  showClock: true,
  showDashboardName: true,
  loop: true,
  pauseOnHover: true
}

// 状态
const config = ref<DisplayConfig>({ ...DEFAULT_CONFIG })
const availableDashboards = ref<Array<{ id: string; name: string }>>([])
const showExitButton = ref(false)
const exitButtonTimer = ref<ReturnType<typeof setTimeout> | null>(null)

// 组件引用
const containerRef = ref<HTMLElement | null>(null)
const carouselRef = ref<InstanceType<typeof CarouselPlayer> | null>(null)

// 使用全屏 composable
const { isFullscreen, enterFullscreen, exitFullscreen } = useFullscreen({
  onExit: () => {
    // 用户按 ESC 退出全屏时的处理
    showExitButton.value = false
  }
})

/**
 * 是否可以开始大屏模式
 */
const canStart = computed(() => {
  return config.value.dashboardIds.length > 0
})

/**
 * 进入大屏模式
 */
async function enterDisplayMode(): Promise<void> {
  // 检查许可证
  const isValid = await checkDisplayLicense()
  if (!isValid) {
    return
  }

  if (config.value.dashboardIds.length === 0) {
    ElMessage.warning('请至少选择一个看板')
    return
  }

  try {
    await enterFullscreen(containerRef.value || undefined)
    ElMessage.success('进入大屏模式，按 ESC 或移动鼠标可显示退出按钮')
  } catch (error) {
    console.error('[DisplayMode] 进入全屏失败:', error)
    ElMessage.error('进入全屏失败，请检查浏览器权限')
  }
}

/**
 * 退出大屏模式
 */
async function exitDisplayMode(): Promise<void> {
  try {
    await exitFullscreen()
  } catch (error) {
    console.error('[DisplayMode] 退出全屏失败:', error)
  }
}

/**
 * 切换大屏模式
 */
async function toggleDisplayMode(): Promise<void> {
  if (isFullscreen.value) {
    await exitDisplayMode()
  } else {
    await enterDisplayMode()
  }
}

/**
 * 处理鼠标移动
 */
function handleMouseMove(): void {
  if (!isFullscreen.value) {
    return
  }

  showExitButton.value = true

  // 清除之前的定时器
  if (exitButtonTimer.value) {
    clearTimeout(exitButtonTimer.value)
  }

  // 3秒后隐藏退出按钮
  exitButtonTimer.value = setTimeout(() => {
    showExitButton.value = false
  }, 3000)
}

/**
 * 处理 ESC 键
 */
function handleKeyDown(event: KeyboardEvent): void {
  if (event.key === 'Escape' && isFullscreen.value) {
    exitDisplayMode()
  }
}

/**
 * 保存配置
 */
function saveConfig(): void {
  try {
    localStorage.setItem('display-mode-config', JSON.stringify(config.value))
    ElMessage.success('配置已保存')
  } catch (error) {
    console.error('[DisplayMode] 保存配置失败:', error)
    ElMessage.error('保存配置失败')
  }
}

/**
 * 重置配置
 */
function resetConfig(): void {
  config.value = { ...DEFAULT_CONFIG }
  ElMessage.info('配置已重置')
}

/**
 * 加载可用看板列表
 */
async function loadDashboards(): Promise<void> {
  try {
    const response = await fetch('/api/dashboard', {
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`)
    }

    const data = await response.json()

    if (data.success) {
      availableDashboards.value = data.data.items || data.data || []
    }
  } catch (error) {
    console.error('[DisplayMode] 加载看板列表失败:', error)
    ElMessage.error('加载看板列表失败')
  }
}

/**
 * 加载保存的配置
 */
function loadSavedConfig(): void {
  try {
    const saved = localStorage.getItem('display-mode-config')
    if (saved) {
      const parsed = JSON.parse(saved)
      config.value = { ...DEFAULT_CONFIG, ...parsed }
    }
  } catch (error) {
    console.error('[DisplayMode] 加载保存的配置失败:', error)
  }
}

// 组件挂载
onMounted(() => {
  loadDashboards()
  loadSavedConfig()

  // 添加事件监听
  document.addEventListener('mousemove', handleMouseMove)
  document.addEventListener('keydown', handleKeyDown)
})

// 组件卸载
onUnmounted(() => {
  // 移除事件监听
  document.removeEventListener('mousemove', handleMouseMove)
  document.removeEventListener('keydown', handleKeyDown)

  // 清理定时器
  if (exitButtonTimer.value) {
    clearTimeout(exitButtonTimer.value)
  }
})
</script>

<style scoped>
.display-mode {
  width: 100%;
  height: 100%;
  position: relative;
}

.display-mode.fullscreen {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 9999;
  background: var(--el-bg-color-page);
}

/* 配置面板 */
.display-config {
  max-width: 800px;
  margin: 0 auto;
  padding: 2rem;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.form-tip {
  font-size: 0.75rem;
  color: var(--el-text-color-secondary);
  margin-top: 0.25rem;
}

.unit {
  margin-left: 0.5rem;
  color: var(--el-text-color-secondary);
}

/* 退出按钮 */
.exit-button {
  position: fixed;
  top: 2rem;
  right: 2rem;
  z-index: 10000;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* 全屏模式样式 */
.fullscreen .display-config {
  display: none;
}

/* 响应式适配 */
@media (min-width: 3840px) {
  .display-config {
    max-width: 1200px;
  }

  .exit-button {
    top: 3rem;
    right: 3rem;
  }

  .exit-button .el-button {
    font-size: 1.125rem;
    padding: 0.75rem 1.5rem;
  }
}
</style>
