/**
 * usePerformanceMonitor - 性能监控 Hook
 * 实时监测 FPS 和内存使用情况，为降级策略提供数据支持
 * @module display/composables/usePerformanceMonitor
 */

import { ref, onMounted, onUnmounted, computed, type Ref, type ComputedRef } from 'vue'

/**
 * 性能统计数据
 */
export interface PerformanceStats {
  /** 当前帧率 */
  fps: number
  /** 内存使用量（MB），仅在支持的浏览器中可用 */
  memoryUsage: number | null
  /** 是否应减少动画 */
  shouldReduceMotion: boolean
  /** 是否处于低性能模式 */
  isLowPerformance: boolean
}

/**
 * 性能监控配置选项
 */
export interface PerformanceMonitorOptions {
  /** 触发降级的 FPS 阈值，默认 30 */
  lowFpsThreshold?: number
  /** 采样间隔（毫秒），默认 1000 */
  sampleInterval?: number
  /** 低性能状态的持续帧数阈值，默认 3 */
  lowFpsFrameCount?: number
}

/**
 * 扩展的 Memory 接口（Chrome 特有）
 */
interface PerformanceMemory {
  usedJSHeapSize: number
  totalJSHeapSize: number
  jsHeapSizeLimit: number
}

/**
 * 性能监控 Hook
 * 实时监测 FPS 和内存使用情况
 *
 * @param options - 配置选项
 * @returns 性能状态和统计数据
 *
 * @example
 * ```typescript
 * const { fps, memoryUsage, isLowPerformance, stats } = usePerformanceMonitor({
 *   lowFpsThreshold: 30,
 *   sampleInterval: 1000
 * })
 *
 * // 响应式地使用
 * watch(isLowPerformance, (low) => {
 *   if (low) {
 *     console.warn('性能下降，启用降级策略')
 *   }
 * })
 * ```
 */
export function usePerformanceMonitor(
  options: PerformanceMonitorOptions = {}
): {
  /** 当前 FPS */
  fps: Ref<number>
  /** 内存使用量（MB） */
  memoryUsage: Ref<number | null>
  /** 是否应减少动画 */
  shouldReduceMotion: Ref<boolean>
  /** 是否处于低性能模式 */
  isLowPerformance: Ref<boolean>
  /** 性能统计对象（计算属性） */
  stats: ComputedRef<PerformanceStats>
  /** 手动停止监控 */
  stop: () => void
  /** 手动开始监控 */
  start: () => void
  /** 是否正在监控 */
  isMonitoring: Ref<boolean>
} {
  const {
    lowFpsThreshold = 30,
    sampleInterval = 1000,
    lowFpsFrameCount = 3
  } = options

  // 响应式状态
  const fps = ref(60)
  const memoryUsage = ref<number | null>(null)
  const shouldReduceMotion = ref(false)
  const isLowPerformance = ref(false)
  const isMonitoring = ref(false)

  // 内部状态
  let frameCount = 0
  let lastTime = performance.now()
  let animationId: number | null = null
  let sampleTimerId: ReturnType<typeof setInterval> | null = null
  let lowFpsConsecutiveCount = 0

  /**
   * 帧计数函数
   */
  function countFrame(): void {
    if (!isMonitoring.value) return
    frameCount++
    animationId = requestAnimationFrame(countFrame)
  }

  /**
   * 采样函数 - 计算当前 FPS 和内存使用
   */
  function sample(): void {
    const now = performance.now()
    const elapsed = now - lastTime

    if (elapsed >= sampleInterval) {
      // 计算 FPS
      const currentFps = Math.round((frameCount * 1000) / elapsed)
      fps.value = currentFps
      frameCount = 0
      lastTime = now

      // 检测低性能状态（需要连续多帧低于阈值）
      if (currentFps < lowFpsThreshold) {
        lowFpsConsecutiveCount++
        if (lowFpsConsecutiveCount >= lowFpsFrameCount) {
          isLowPerformance.value = true
          shouldReduceMotion.value = true
        }
      } else {
        lowFpsConsecutiveCount = 0
        // 性能恢复时退出低性能模式
        if (currentFps >= lowFpsThreshold + 10) {
          isLowPerformance.value = false
          shouldReduceMotion.value = false
        }
      }

      // 获取内存使用（Chrome 特有 API）
      const perfMemory = (performance as PerformanceMemory & { memory?: PerformanceMemory }).memory
      if (perfMemory) {
        memoryUsage.value = Math.round(perfMemory.usedJSHeapSize / (1024 * 1024) * 100) / 100
      }
    }
  }

  /**
   * 开始监控
   */
  function start(): void {
    if (isMonitoring.value) return

    isMonitoring.value = true
    frameCount = 0
    lastTime = performance.now()
    lowFpsConsecutiveCount = 0

    // 启动帧计数
    animationId = requestAnimationFrame(countFrame)

    // 启动定时采样
    sampleTimerId = setInterval(sample, sampleInterval)
  }

  /**
   * 停止监控
   */
  function stop(): void {
    isMonitoring.value = false

    if (animationId !== null) {
      cancelAnimationFrame(animationId)
      animationId = null
    }

    if (sampleTimerId !== null) {
      clearInterval(sampleTimerId)
      sampleTimerId = null
    }
  }

  // 计算属性：性能统计
  const stats = computed<PerformanceStats>(() => ({
    fps: fps.value,
    memoryUsage: memoryUsage.value,
    shouldReduceMotion: shouldReduceMotion.value,
    isLowPerformance: isLowPerformance.value
  }))

  // 组件挂载时自动开始监控
  onMounted(() => {
    start()
  })

  // 组件卸载时自动清理
  onUnmounted(() => {
    stop()
  })

  return {
    fps,
    memoryUsage,
    shouldReduceMotion,
    isLowPerformance,
    stats,
    stop,
    start,
    isMonitoring
  }
}

/**
 * 全局性能监控实例（单例模式）
 * 用于跨组件共享性能状态
 */
let globalMonitorInstance: ReturnType<typeof usePerformanceMonitor> | null = null

/**
 * 获取全局性能监控实例
 * 适用于需要在多个组件间共享性能状态的场景
 *
 * @param options - 配置选项（仅首次调用时生效）
 * @returns 全局性能监控实例
 *
 * @example
 * ```typescript
 * // 在 App.vue 中初始化
 * const globalMonitor = getGlobalPerformanceMonitor()
 *
 * // 在其他组件中使用
 * const { isLowPerformance } = getGlobalPerformanceMonitor()
 * ```
 */
export function getGlobalPerformanceMonitor(
  options: PerformanceMonitorOptions = {}
): ReturnType<typeof usePerformanceMonitor> {
  if (!globalMonitorInstance) {
    // 创建全局实例（不使用 onMounted，手动控制生命周期）
    const {
      lowFpsThreshold = 30,
      sampleInterval = 1000,
      lowFpsFrameCount = 3
    } = options

    const fps = ref(60)
    const memoryUsage = ref<number | null>(null)
    const shouldReduceMotion = ref(false)
    const isLowPerformance = ref(false)
    const isMonitoring = ref(false)

    let frameCount = 0
    let lastTime = performance.now()
    let animationId: number | null = null
    let sampleTimerId: ReturnType<typeof setInterval> | null = null
    let lowFpsConsecutiveCount = 0

    function countFrame(): void {
      if (!isMonitoring.value) return
      frameCount++
      animationId = requestAnimationFrame(countFrame)
    }

    function sample(): void {
      const now = performance.now()
      const elapsed = now - lastTime

      if (elapsed >= sampleInterval) {
        const currentFps = Math.round((frameCount * 1000) / elapsed)
        fps.value = currentFps
        frameCount = 0
        lastTime = now

        if (currentFps < lowFpsThreshold) {
          lowFpsConsecutiveCount++
          if (lowFpsConsecutiveCount >= lowFpsFrameCount) {
            isLowPerformance.value = true
            shouldReduceMotion.value = true
          }
        } else {
          lowFpsConsecutiveCount = 0
          if (currentFps >= lowFpsThreshold + 10) {
            isLowPerformance.value = false
            shouldReduceMotion.value = false
          }
        }

        const perfMemory = (performance as PerformanceMemory & { memory?: PerformanceMemory }).memory
        if (perfMemory) {
          memoryUsage.value = Math.round(perfMemory.usedJSHeapSize / (1024 * 1024) * 100) / 100
        }
      }
    }

    function start(): void {
      if (isMonitoring.value) return
      isMonitoring.value = true
      frameCount = 0
      lastTime = performance.now()
      lowFpsConsecutiveCount = 0
      animationId = requestAnimationFrame(countFrame)
      sampleTimerId = setInterval(sample, sampleInterval)
    }

    function stop(): void {
      isMonitoring.value = false
      if (animationId !== null) {
        cancelAnimationFrame(animationId)
        animationId = null
      }
      if (sampleTimerId !== null) {
        clearInterval(sampleTimerId)
        sampleTimerId = null
      }
    }

    const stats = computed<PerformanceStats>(() => ({
      fps: fps.value,
      memoryUsage: memoryUsage.value,
      shouldReduceMotion: shouldReduceMotion.value,
      isLowPerformance: isLowPerformance.value
    }))

    globalMonitorInstance = {
      fps,
      memoryUsage,
      shouldReduceMotion,
      isLowPerformance,
      stats,
      start,
      stop,
      isMonitoring
    }

    // 自动启动全局监控
    start()
  }

  return globalMonitorInstance
}

/**
 * 销毁全局性能监控实例
 * 在应用卸载时调用
 */
export function destroyGlobalPerformanceMonitor(): void {
  if (globalMonitorInstance) {
    globalMonitorInstance.stop()
    globalMonitorInstance = null
  }
}
