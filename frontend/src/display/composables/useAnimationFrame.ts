/**
 * useAnimationFrame - requestAnimationFrame 封装
 * 提供基于浏览器刷新率的高性能动画调度
 * @module display/composables/useAnimationFrame
 */

import { ref, onUnmounted, type Ref } from 'vue'

/**
 * requestAnimationFrame 基础 Hook
 * 提供动画帧级别的回调调度
 *
 * @example
 * ```typescript
 * const { start, stop, isRunning } = useAnimationFrame()
 *
 * start((deltaTime) => {
 *   // deltaTime: 距离上一帧的时间（毫秒）
 *   updateAnimation(deltaTime)
 * })
 *
 * // 停止动画
 * stop()
 * ```
 */
export function useAnimationFrame() {
  const frameId = ref<number | null>(null)
  const isRunning = ref(false)

  /**
   * 启动动画帧循环
   * @param callback - 每帧回调函数，接收 deltaTime 参数（毫秒）
   */
  function start(callback: (deltaTime: number) => void): void {
    if (isRunning.value) return

    isRunning.value = true
    let lastTime = performance.now()

    function tick(currentTime: number): void {
      if (!isRunning.value) return

      const deltaTime = currentTime - lastTime
      lastTime = currentTime

      callback(deltaTime)
      frameId.value = requestAnimationFrame(tick)
    }

    frameId.value = requestAnimationFrame(tick)
  }

  /**
   * 停止动画帧循环
   */
  function stop(): void {
    isRunning.value = false
    if (frameId.value !== null) {
      cancelAnimationFrame(frameId.value)
      frameId.value = null
    }
  }

  // 组件卸载时自动清理
  onUnmounted(() => {
    stop()
  })

  return {
    start,
    stop,
    isRunning,
    frameId
  }
}

/**
 * 基于 requestAnimationFrame 的定时刷新 Hook
 * 替代 setInterval，与浏览器刷新率同步，避免视觉撕裂
 *
 * @param callback - 定时回调函数
 * @param interval - 刷新间隔（秒）
 *
 * @example
 * ```typescript
 * const interval = ref(60) // 60秒刷新一次
 * const { start, stop, isRunning } = useRAFTimer(refreshData, interval)
 *
 * // 开始定时刷新
 * start()
 *
 * // 停止刷新
 * stop()
 *
 * // 动态修改间隔
 * interval.value = 30 // 改为30秒刷新
 * ```
 */
export function useRAFTimer(
  callback: () => void,
  interval: Ref<number>
): {
  start: () => void
  stop: () => void
  isRunning: Ref<boolean>
} {
  const { start: startRAF, stop: stopRAF, isRunning } = useAnimationFrame()
  let elapsed = 0

  /**
   * 启动定时器
   */
  function start(): void {
    stopRAF()

    if (interval.value <= 0) {
      console.warn('[useRAFTimer] 刷新间隔必须大于 0')
      return
    }

    elapsed = 0
    startRAF((deltaTime: number) => {
      elapsed += deltaTime
      if (elapsed >= interval.value * 1000) {
        callback()
        elapsed = 0
      }
    })
  }

  /**
   * 停止定时器
   */
  function stop(): void {
    stopRAF()
    elapsed = 0
  }

  return { start, stop, isRunning }
}

/**
 * 基于 requestAnimationFrame 的独立定时器控制器
 * 用于非 Vue 组件环境（如 DataBinder 类）
 */
export class RAFTimerController {
  private frameId: number | null = null
  private isRunning = false
  private elapsed = 0
  private interval: number // 毫秒
  private callback: () => void

  constructor(callback: () => void, intervalSeconds: number) {
    this.callback = callback
    this.interval = intervalSeconds * 1000
  }

  /**
   * 启动定时器
   */
  start(): void {
    if (this.isRunning) return
    if (this.interval <= 0) return

    this.isRunning = true
    this.elapsed = 0
    let lastTime = performance.now()

    const tick = (currentTime: number): void => {
      if (!this.isRunning) return

      const deltaTime = currentTime - lastTime
      lastTime = currentTime

      this.elapsed += deltaTime
      if (this.elapsed >= this.interval) {
        this.callback()
        this.elapsed = 0
      }

      this.frameId = requestAnimationFrame(tick)
    }

    this.frameId = requestAnimationFrame(tick)
  }

  /**
   * 停止定时器
   */
  stop(): void {
    this.isRunning = false
    this.elapsed = 0
    if (this.frameId !== null) {
      cancelAnimationFrame(this.frameId)
      this.frameId = null
    }
  }

  /**
   * 检查是否正在运行
   */
  get running(): boolean {
    return this.isRunning
  }

  /**
   * 更新刷新间隔
   */
  setInterval(intervalSeconds: number): void {
    this.interval = intervalSeconds * 1000
  }
}
