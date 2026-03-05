import { computed } from 'vue'
import { useDashboardStore } from '@/stores/dashboard'

/**
 * useSelection composable
 * 封装组件选择相关操作的组合式函数
 *
 * 功能包括:
 * - selectedWidget: 当前选中的组件（计算属性）
 * - selectedWidgetId: 当前选中组件的 ID（计算属性）
 * - selectWidget: 选择指定组件
 * - clearSelection: 清除选择
 * - isSelected: 检查指定组件是否被选中
 *
 * @returns {Object} 选择操作方法和状态
 */
export function useSelection() {
  const store = useDashboardStore()

  /**
   * 当前选中的组件
   * 如果没有选中任何组件，返回 null
   */
  const selectedWidget = computed(() => store.selectedWidget)

  /**
   * 当前选中组件的 ID
   * 如果没有选中任何组件，返回 null
   */
  const selectedWidgetId = computed(() => store.selectedWidgetId)

  /**
   * 选择指定组件
   * @param {number|string|null} id - 组件 ID，传入 null 取消选择
   */
  const selectWidget = (id) => {
    store.selectWidget(id)
  }

  /**
   * 清除当前选择
   * 等同于 selectWidget(null)
   */
  const clearSelection = () => {
    store.selectWidget(null)
  }

  /**
   * 检查指定组件是否被选中
   * @param {number|string} id - 要检查的组件 ID
   * @returns {boolean} 是否被选中
   */
  const isSelected = (id) => {
    return store.selectedWidgetId === id
  }

  return {
    selectedWidget,
    selectedWidgetId,
    selectWidget,
    clearSelection,
    isSelected
  }
}

export default useSelection
