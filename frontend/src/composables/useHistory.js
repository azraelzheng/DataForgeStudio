import { computed } from 'vue'
import { useDashboardStore } from '@/stores/dashboard'

/**
 * useHistory composable
 * 封装撤销/重做操作的组合式函数
 *
 * 功能包括:
 * - pushHistory: 保存当前状态到历史记录
 * - undo: 撤销到上一个状态
 * - redo: 重做到下一个状态
 * - clearHistory: 清除所有历史记录
 * - canUndo: 是否可以撤销（计算属性）
 * - canRedo: 是否可以重做（计算属性）
 *
 * @returns {Object} 历史操作方法和状态
 */
export function useHistory() {
  const store = useDashboardStore()

  /**
   * 保存当前状态到历史记录
   * 通常在组件数据变更后调用，以便支持撤销
   */
  const pushHistory = () => {
    store.saveHistory()
  }

  /**
   * 撤销操作
   * 恢复到上一个历史状态
   */
  const undo = () => {
    store.undo()
  }

  /**
   * 重做操作
   * 如果之前执行了撤销，可以重做到下一个状态
   */
  const redo = () => {
    store.redo()
  }

  /**
   * 清除所有历史记录
   * 重置撤销/重做栈
   */
  const clearHistory = () => {
    store.history = []
    store.historyIndex = -1
  }

  /**
   * 是否可以撤销
   * 当历史索引大于 0 时可以撤销
   */
  const canUndo = computed(() => store.canUndo)

  /**
   * 是否可以重做
   * 当历史索引小于历史记录长度 - 1 时可以重做
   */
  const canRedo = computed(() => store.canRedo)

  return {
    pushHistory,
    undo,
    redo,
    clearHistory,
    canUndo,
    canRedo
  }
}

export default useHistory
