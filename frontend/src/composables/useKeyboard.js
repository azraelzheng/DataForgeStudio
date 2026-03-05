/**
 * 键盘快捷键管理 Composable
 * 用于大屏设计器的快捷键操作
 */
import { onMounted, onUnmounted } from 'vue'
import { useDashboardStore } from '@/stores/dashboard'

/**
 * 检查当前焦点是否在输入元素上
 * @returns {boolean}
 */
function isInputFocused() {
  const activeElement = document.activeElement
  if (!activeElement) return false

  const tagName = activeElement.tagName
  return tagName === 'INPUT' ||
         tagName === 'TEXTAREA' ||
         activeElement.isContentEditable
}

/**
 * 键盘快捷键 Composable
 * 提供设计器快捷键功能：
 * - Delete/Backspace: 删除选中组件
 * - Ctrl+C: 复制组件
 * - Ctrl+V: 粘贴组件
 * - Ctrl+Z: 撤销
 * - Ctrl+Shift+Z / Ctrl+Y: 重做
 * - Escape: 取消选择
 */
export function useKeyboard() {
  const store = useDashboardStore()

  /**
   * 键盘按下事件处理
   * @param {KeyboardEvent} e
   */
  const handleKeyDown = (e) => {
    // 忽略输入框内的快捷键
    if (isInputFocused()) return

    const key = e.key.toLowerCase()

    // Delete/Backspace - 删除选中组件
    if (e.key === 'Delete' || e.key === 'Backspace') {
      if (store.selectedWidgetId) {
        e.preventDefault()
        store.deleteExistingWidget(store.selectedWidgetId)
      }
      return
    }

    // Escape - 取消选择
    if (e.key === 'Escape') {
      store.selectWidget(null)
      return
    }

    // Ctrl/Cmd 组合键
    if (e.ctrlKey || e.metaKey) {
      switch (key) {
        case 'c':
          // Ctrl+C - 复制
          if (store.selectedWidgetId) {
            e.preventDefault()
            store.copyWidget()
          }
          break

        case 'v':
          // Ctrl+V - 粘贴
          if (store.clipboard) {
            e.preventDefault()
            store.pasteWidget()
          }
          break

        case 'z':
          // Ctrl+Z - 撤销
          // Ctrl+Shift+Z - 重做
          e.preventDefault()
          if (e.shiftKey) {
            store.redo()
          } else {
            store.undo()
          }
          break

        case 'y':
          // Ctrl+Y - 重做
          e.preventDefault()
          store.redo()
          break

        case 'a':
          // Ctrl+A - 全选（可选功能，暂不实现）
          // e.preventDefault()
          break

        case 's':
          // Ctrl+S - 保存
          e.preventDefault()
          if (store.isDirty) {
            store.saveDashboard()
          }
          break
      }
    }

    // 方向键 - 移动选中组件（可选功能）
    if (store.selectedWidgetId) {
      const moveStep = e.shiftKey ? 10 : 1 // Shift 加速移动

      switch (e.key) {
        case 'ArrowUp':
          e.preventDefault()
          moveSelectedWidget(0, -moveStep)
          break
        case 'ArrowDown':
          e.preventDefault()
          moveSelectedWidget(0, moveStep)
          break
        case 'ArrowLeft':
          e.preventDefault()
          moveSelectedWidget(-moveStep, 0)
          break
        case 'ArrowRight':
          e.preventDefault()
          moveSelectedWidget(moveStep, 0)
          break
      }
    }
  }

  /**
   * 移动选中组件
   * @param {number} deltaX X 轴偏移
   * @param {number} deltaY Y 轴偏移
   */
  const moveSelectedWidget = (deltaX, deltaY) => {
    const widget = store.selectedWidget
    if (!widget) return

    // 使用本地更新实现流畅移动
    store.updateLocalWidget(widget.widgetId || widget.id, {
      positionX: Math.max(0, (widget.positionX || 0) + deltaX),
      positionY: Math.max(0, (widget.positionY || 0) + deltaY)
    })
  }

  // 挂载事件监听
  onMounted(() => {
    window.addEventListener('keydown', handleKeyDown)
  })

  // 卸载事件监听
  onUnmounted(() => {
    window.removeEventListener('keydown', handleKeyDown)
  })

  return {
    // 暴露方法供外部调用（可选）
    handleKeyDown
  }
}

/**
 * 快捷键帮助信息
 */
export const KEYBOARD_SHORTCUTS = [
  { key: 'Delete / Backspace', description: '删除选中组件' },
  { key: 'Ctrl + C', description: '复制组件' },
  { key: 'Ctrl + V', description: '粘贴组件' },
  { key: 'Ctrl + Z', description: '撤销' },
  { key: 'Ctrl + Shift + Z', description: '重做' },
  { key: 'Ctrl + Y', description: '重做' },
  { key: 'Ctrl + S', description: '保存' },
  { key: 'Escape', description: '取消选择' },
  { key: '方向键', description: '微调组件位置' },
  { key: 'Shift + 方向键', description: '快速移动组件' }
]
