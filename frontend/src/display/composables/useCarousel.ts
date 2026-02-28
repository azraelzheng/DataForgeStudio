/**
 * useCarousel - 轮播逻辑 Composable
 * 提供看板轮播的控制逻辑
 * @module display/composables/useCarousel
 */

import { ref, watch, onUnmounted, type Ref, type ComputedRef } from 'vue'
import type { CarouselState, CarouselOptions } from '../types/display'

const DEFAULT_OPTIONS: Required<CarouselOptions> = {
  loop: true,
  pauseOnHover: true,
  transitionDuration: 500
}

/**
 * 轮播逻辑 Composable
 *
 * @param items - 轮播项目列表（看板 ID）
 * @param interval - 轮播间隔（秒）
 * @param options - 轮播选项
 *
 * @example
 * ```typescript
 * const items = ref(['dashboard-1', 'dashboard-2', 'dashboard-3'])
 * const interval = ref(30) // 30秒切换一次
 *
 * const {
 *   currentIndex,
 *   isTransitioning,
 *   isPaused,
 *   currentDashboardId,
 *   start,
 *   stop,
 *   next,
 *   prev,
 *   goTo,
 *   pause,
 *   resume
 * } = useCarousel(items, interval, {
 *   loop: true,
 *   pauseOnHover: true
 * })
 *
 * // 开始自动轮播
 * start()
 * ```
 */
export function useCarousel(
  items: Ref<string[]>,
  interval: Ref<number>,
  options?: CarouselOptions
) {
  const opts = { ...DEFAULT_OPTIONS, ...options }

  // 状态
  const currentIndex = ref(0)
  const isTransitioning = ref(false)
  const isPaused = ref(false)

  // 定时器
  let timer: ReturnType<typeof setInterval> | null = null

  /**
   * 当前看板 ID（计算属性）
   */
  const currentDashboardId = ref<string | null>(
    items.value.length > 0 ? items.value[0] : null
  )

  /**
   * 开始轮播
   */
  function start(): void {
    stop() // 先停止已有的定时器

    if (items.value.length <= 1 || interval.value <= 0) {
      return // 不需要轮播
    }

    timer = setInterval(() => {
      if (!isPaused.value) {
        next()
      }
    }, interval.value * 1000)
  }

  /**
   * 停止轮播
   */
  function stop(): void {
    if (timer) {
      clearInterval(timer)
      timer = null
    }
  }

  /**
   * 切换到下一个
   */
  function next(): void {
    if (isTransitioning.value) {
      return // 转场中不允许切换
    }

    if (items.value.length === 0) {
      return
    }

    if (currentIndex.value >= items.value.length - 1) {
      if (opts.loop) {
        goTo(0)
      } else {
        stop() // 不循环且到达末尾，停止轮播
      }
    } else {
      goTo(currentIndex.value + 1)
    }
  }

  /**
   * 切换到上一个
   */
  function prev(): void {
    if (isTransitioning.value) {
      return
    }

    if (items.value.length === 0) {
      return
    }

    if (currentIndex.value <= 0) {
      if (opts.loop) {
        goTo(items.value.length - 1)
      }
    } else {
      goTo(currentIndex.value - 1)
    }
  }

  /**
   * 跳转到指定索引
   *
   * @param index - 目标索引
   */
  function goTo(index: number): void {
    if (index < 0 || index >= items.value.length) {
      console.warn(`[useCarousel] 无效的索引: ${index}`)
      return
    }

    if (isTransitioning.value && index !== currentIndex.value) {
      return // 转场中不允许切换
    }

    isTransitioning.value = true

    setTimeout(() => {
      currentIndex.value = index
      currentDashboardId.value = items.value[index]

      // 转场完成后重置状态
      setTimeout(() => {
        isTransitioning.value = false
      }, opts.transitionDuration)
    }, 50) // 短暂延迟确保转场动画触发
  }

  /**
   * 暂停轮播
   */
  function pause(): void {
    isPaused.value = true
  }

  /**
   * 恢复轮播
   */
  function resume(): void {
    isPaused.value = false
  }

  /**
   * 重置轮播状态
   */
  function reset(): void {
    stop()
    currentIndex.value = 0
    isTransitioning.value = false
    isPaused.value = false
    currentDashboardId.value = items.value.length > 0 ? items.value[0] : null
  }

  // 监听 items 变化
  watch(items, (newItems) => {
    if (newItems.length === 0) {
      currentDashboardId.value = null
      stop()
    } else if (currentIndex.value >= newItems.length) {
      // 当前索引超出范围，重置到第一个
      goTo(0)
    } else {
      currentDashboardId.value = newItems[currentIndex.value]
    }
  })

  // 监听 interval 变化，重启定时器
  watch(interval, () => {
    if (timer) {
      start() // 重启定时器以应用新的间隔
    }
  })

  // 组件卸载时清理定时器
  onUnmounted(() => {
    stop()
  })

  // 导出状态
  const state: CarouselState = {
    get currentIndex() { return currentIndex.value },
    get isTransitioning() { return isTransitioning.value },
    get isPaused() { return isPaused.value },
    get loop() { return opts.loop }
  }

  return {
    // 状态
    currentIndex,
    isTransitioning,
    isPaused,
    currentDashboardId,
    state,

    // 方法
    start,
    stop,
    next,
    prev,
    goTo,
    pause,
    resume,
    reset
  }
}

/**
 * 简化版轮播 Hook（仅状态，无定时器）
 * 用于手动控制轮播
 */
export function useCarouselState(itemCount: Ref<number>) {
  const currentIndex = ref(0)
  const isTransitioning = ref(false)

  function next(): void {
    if (currentIndex.value >= itemCount.value - 1) {
      currentIndex.value = 0
    } else {
      currentIndex.value++
    }
  }

  function prev(): void {
    if (currentIndex.value <= 0) {
      currentIndex.value = Math.max(0, itemCount.value - 1)
    } else {
      currentIndex.value--
    }
  }

  function goTo(index: number): void {
    if (index >= 0 && index < itemCount.value) {
      currentIndex.value = index
    }
  }

  return {
    currentIndex,
    isTransitioning,
    next,
    prev,
    goTo
  }
}
