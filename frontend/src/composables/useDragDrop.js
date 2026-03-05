import { ref } from 'vue'

/**
 * useDragDrop composable
 * 封装拖拽相关操作的组合式函数
 *
 * 功能包括:
 * - isDragging: 当前是否正在拖拽
 * - dragType: 当前拖拽的类型
 * - dragData: 当前拖拽携带的数据
 * - handleDragStart: 开始拖拽处理
 * - handleDragEnd: 结束拖拽处理
 * - handleDrop: 放置处理（支持缩放比例）
 *
 * @returns {Object} 拖拽操作方法和状态
 */
export function useDragDrop() {
  // 当前是否正在拖拽
  const isDragging = ref(false)

  // 当前拖拽的类型（如 'widget', 'report' 等）
  const dragType = ref(null)

  // 当前拖拽携带的数据
  const dragData = ref(null)

  /**
   * 开始拖拽处理
   * @param {string} type - 拖拽类型（如 'widget-chart', 'widget-text', 'report' 等）
   * @param {Object} data - 拖拽携带的数据（可选）
   */
  const handleDragStart = (type, data = null) => {
    isDragging.value = true
    dragType.value = type
    dragData.value = data
  }

  /**
   * 结束拖拽处理
   * 重置所有拖拽状态
   */
  const handleDragEnd = () => {
    isDragging.value = false
    dragType.value = null
    dragData.value = null
  }

  /**
   * 处理放置事件
   * 计算相对于画布的坐标位置，支持缩放比例
   *
   * @param {DragEvent} event - 原生拖拽事件
   * @param {DOMRect} canvasRect - 画布的边界矩形（通过 canvas.getBoundingClientRect() 获取）
   * @param {number} scale - 当前画布的缩放比例（默认 1）
   * @returns {Object|null} 返回包含 type、x、y、data 的对象，如果无效则返回 null
   *
   * @example
   * // 在组件中使用
   * const canvasRef = ref(null)
   * const { isDragging, handleDragStart, handleDragEnd, handleDrop } = useDragDrop()
   * const store = useDashboardStore()
   *
   * const onDrop = (event) => {
   *   const rect = canvasRef.value.getBoundingClientRect()
   *   const result = handleDrop(event, rect, store.scale)
   *   if (result) {
   *     // 创建新组件
   *     store.addNewWidget({
   *       widgetType: result.type,
   *       positionX: result.x,
   *       positionY: result.y,
   *       ...result.data
   *     })
   *   }
   * }
   */
  const handleDrop = (event, canvasRect, scale = 1) => {
    // 如果没有拖拽类型，说明不是有效的拖拽
    if (!dragType.value) {
      return null
    }

    // 阻止默认行为
    event.preventDefault()

    // 计算相对于画布的坐标（考虑缩放比例）
    const x = Math.round((event.clientX - canvasRect.left) / scale)
    const y = Math.round((event.clientY - canvasRect.top) / scale)

    // 确保坐标不为负数
    const clampedX = Math.max(0, x)
    const clampedY = Math.max(0, y)

    // 返回放置信息
    const result = {
      type: dragType.value,
      x: clampedX,
      y: clampedY,
      data: dragData.value
    }

    // 重置拖拽状态
    handleDragEnd()

    return result
  }

  /**
   * 从 DataTransfer 获取拖拽数据（用于外部拖拽源）
   * @param {DragEvent} event - 原生拖拽事件
   * @param {DOMRect} canvasRect - 画布的边界矩形
   * @param {number} scale - 当前画布的缩放比例
   * @returns {Object|null} 返回包含 x、y 和从 DataTransfer 解析的数据
   */
  const handleDropFromTransfer = (event, canvasRect, scale = 1) => {
    event.preventDefault()

    // 尝试从 DataTransfer 获取数据
    const rawData = event.dataTransfer?.getData('application/json')
    let parsedData = null

    if (rawData) {
      try {
        parsedData = JSON.parse(rawData)
      } catch {
        // 解析失败，忽略
      }
    }

    // 计算相对于画布的坐标（考虑缩放比例）
    const x = Math.round((event.clientX - canvasRect.left) / scale)
    const y = Math.round((event.clientY - canvasRect.top) / scale)

    // 确保坐标不为负数
    const clampedX = Math.max(0, x)
    const clampedY = Math.max(0, y)

    return {
      x: clampedX,
      y: clampedY,
      data: parsedData,
      rawData
    }
  }

  /**
   * 设置拖拽数据到 DataTransfer（用于拖拽源）
   * @param {DragEvent} event - 原生拖拽事件
   * @param {string} type - 拖拽类型
   * @param {Object} data - 要携带的数据
   */
  const setDragData = (event, type, data = null) => {
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'copy'
      event.dataTransfer.setData('application/json', JSON.stringify({ type, data }))
      event.dataTransfer.setData('text/plain', type)
    }

    // 同时设置内部状态
    handleDragStart(type, data)
  }

  return {
    // 状态
    isDragging,
    dragType,
    dragData,

    // 方法
    handleDragStart,
    handleDragEnd,
    handleDrop,
    handleDropFromTransfer,
    setDragData
  }
}

export default useDragDrop
