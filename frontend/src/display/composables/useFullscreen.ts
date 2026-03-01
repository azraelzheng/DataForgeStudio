/**
 * useFullscreen - 全屏 API Composable
 * 提供全屏模式的进入、退出和状态管理
 * @module display/composables/useFullscreen
 */

import { ref, onMounted, onUnmounted, type Ref } from 'vue'
import type { FullscreenState, FullscreenOptions } from '../types/display'

/**
 * 全屏 API 兼容性处理
 */
const fullscreenApi = {
  // 获取全屏元素
  getElement: (): Element | null => {
    return document.fullscreenElement ||
      (document as any).webkitFullscreenElement ||
      (document as any).mozFullScreenElement ||
      (document as any).msFullscreenElement ||
      null
  },

  // 请求全屏
  request: async (element: HTMLElement): Promise<void> => {
    if (element.requestFullscreen) {
      await element.requestFullscreen()
    } else if ((element as any).webkitRequestFullscreen) {
      await (element as any).webkitRequestFullscreen()
    } else if ((element as any).mozRequestFullScreen) {
      await (element as any).mozRequestFullScreen()
    } else if ((element as any).msRequestFullscreen) {
      await (element as any).msRequestFullscreen()
    } else {
      throw new Error('当前浏览器不支持全屏 API')
    }
  },

  // 退出全屏
  exit: async (): Promise<void> => {
    if (document.exitFullscreen) {
      await document.exitFullscreen()
    } else if ((document as any).webkitExitFullscreen) {
      await (document as any).webkitExitFullscreen()
    } else if ((document as any).mozCancelFullScreen) {
      await (document as any).mozCancelFullScreen()
    } else if ((document as any).msExitFullscreen) {
      await (document as any).msExitFullscreen()
    }
  },

  // 全屏事件名称
  eventName: (): string => {
    if ('onfullscreenchange' in document) {
      return 'fullscreenchange'
    } else if ('onwebkitfullscreenchange' in document) {
      return 'webkitfullscreenchange'
    } else if ('onmozfullscreenchange' in document) {
      return 'mozfullscreenchange'
    } else if ('onmsfullscreenchange' in document) {
      return 'msfullscreenchange'
    }
    return 'fullscreenchange'
  }
}

/**
 * 全屏 API Composable
 *
 * @example
 * ```typescript
 * const { isFullscreen, enterFullscreen, exitFullscreen, toggleFullscreen } = useFullscreen({
 *   onEnter: () => console.log('进入全屏'),
 *   onExit: () => console.log('退出全屏')
 * })
 *
 * // 进入全屏
 * await enterFullscreen()
 *
 * // 退出全屏
 * await exitFullscreen()
 *
 * // 切换全屏
 * toggleFullscreen()
 * ```
 */
export function useFullscreen(options?: FullscreenOptions) {
  const isFullscreen = ref(false)
  const element = ref<HTMLElement | null>(null)

  let cleanupEventListener: (() => void) | null = null

  /**
   * 处理全屏状态变化
   */
  const handleFullscreenChange = () => {
    const fullscreenElement = fullscreenApi.getElement()
    isFullscreen.value = fullscreenElement !== null

    if (!isFullscreen.value && options?.onExit) {
      options.onExit()
    } else if (isFullscreen.value && options?.onEnter) {
      options.onEnter()
    }
  }

  /**
   * 进入全屏模式
   *
   * @param targetElement - 目标元素，默认为当前组件元素
   */
  async function enterFullscreen(targetElement?: HTMLElement): Promise<void> {
    const el = targetElement || element.value
    if (!el) {
      console.warn('[useFullscreen] 没有找到目标元素')
      return
    }

    try {
      await fullscreenApi.request(el)
      isFullscreen.value = true
    } catch (error) {
      console.error('[useFullscreen] 进入全屏失败:', error)
      throw error
    }
  }

  /**
   * 退出全屏模式
   */
  async function exitFullscreen(): Promise<void> {
    if (!isFullscreen.value) {
      return
    }

    try {
      await fullscreenApi.exit()
      isFullscreen.value = false
    } catch (error) {
      console.error('[useFullscreen] 退出全屏失败:', error)
      throw error
    }
  }

  /**
   * 切换全屏模式
   *
   * @param targetElement - 目标元素，用于进入全屏时使用
   */
  function toggleFullscreen(targetElement?: HTMLElement): void {
    if (isFullscreen.value) {
      exitFullscreen()
    } else {
      enterFullscreen(targetElement)
    }
  }

  /**
   * 设置目标元素
   *
   * @param el - 目标元素
   */
  function setElement(el: HTMLElement | null): void {
    element.value = el
  }

  /**
   * 检查浏览器是否支持全屏 API
   */
  function isSupported(): boolean {
    return !!(
      document.fullscreenEnabled ||
      (document as any).webkitFullscreenEnabled ||
      (document as any).mozFullScreenEnabled ||
      (document as any).msFullscreenEnabled
    )
  }

  // 监听全屏状态变化
  onMounted(() => {
    const eventName = fullscreenApi.eventName()
    document.addEventListener(eventName, handleFullscreenChange)
    cleanupEventListener = () => {
      document.removeEventListener(eventName, handleFullscreenChange)
    }

    // 初始化状态
    handleFullscreenChange()
  })

  // 清理事件监听
  onUnmounted(() => {
    if (cleanupEventListener) {
      cleanupEventListener()
    }
  })

  return {
    isFullscreen,
    element: element as Ref<HTMLElement | null>,
    enterFullscreen,
    exitFullscreen,
    toggleFullscreen,
    setElement,
    isSupported
  }
}

/**
 * 全屏状态 Hook（简化版）
 * 只提供状态监听，不提供操作方法
 */
export function useFullscreenState() {
  const isFullscreen = ref(false)

  onMounted(() => {
    const checkFullscreen = () => {
      isFullscreen.value = fullscreenApi.getElement() !== null
    }

    const eventName = fullscreenApi.eventName()
    document.addEventListener(eventName, checkFullscreen)
    checkFullscreen()

    onUnmounted(() => {
      document.removeEventListener(eventName, checkFullscreen)
    })
  })

  return { isFullscreen }
}
