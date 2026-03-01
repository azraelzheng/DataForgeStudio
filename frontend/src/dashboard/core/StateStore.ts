/**
 * StateStore - 看板状态管理 (Pinia)
 * 负责管理看板的全局状态、组件状态和编辑状态
 * @module dashboard/core/StateStore
 */

import { defineStore } from 'pinia'
import { ref, computed, type Ref, type ComputedRef } from 'vue'
import type {
  Dashboard,
  DashboardConfig,
  WidgetInstance,
  DisplayMode,
  GridPosition,
  ApiResponse
} from '../types/dashboard'
import { layoutEngine } from './LayoutEngine'
import { dataBinder } from './DataBinder'

/**
 * 看板状态 Store
 *
 * @example
 * ```typescript
 * const store = useDashboardStore()
 *
 * // 加载看板
 * await store.loadDashboard('dashboard-123')
 *
 * // 添加组件
 * store.addWidget({
 *   id: 'widget-1',
 *   type: 'chart',
 *   position: { x: 0, y: 0, width: 4, height: 3 },
 *   config: {}
 * })
 *
 * // 选中组件
 * store.selectWidget('widget-1')
 *
 * // 保存看板
 * await store.saveDashboard()
 * ```
 */
export const useDashboardStore = defineStore('dashboard', () => {
  // ============================================
  // 状态
  // ============================================

  /** 当前看板 */
  const currentDashboard: Ref<Dashboard | null> = ref(null)

  /** 组件映射 (id -> widget) */
  const widgets: Ref<Map<string, WidgetInstance>> = ref(new Map())

  /** 选中的组件 ID */
  const selectedWidgetId: Ref<string | null> = ref(null)

  /** 是否编辑模式 */
  const isEditing: Ref<boolean> = ref(false)

  /** 显示模式 */
  const displayMode: Ref<DisplayMode> = ref('preview')

  /** 是否加载中 */
  const isLoading: Ref<boolean> = ref(false)

  /** 错误信息 */
  const error: Ref<string | null> = ref(null)

  /** 是否有未保存的更改 */
  const isDirty: Ref<boolean> = ref(false)

  /** 复制的组件（用于粘贴） */
  const clipboard: Ref<WidgetInstance | null> = ref(null)

  /** 历史记录（用于撤销/重做） */
  const history: Ref<WidgetInstance[][]> = ref([])

  /** 历史记录索引 */
  const historyIndex: Ref<number> = ref(-1)

  /** 最大历史记录数 */
  const maxHistorySize = 50

  // ============================================
  // 计算属性
  // ============================================

  /** 组件列表 */
  const widgetList: ComputedRef<WidgetInstance[]> = computed(() => {
    return Array.from(widgets.value.values())
  })

  /** 选中的组件 */
  const selectedWidget: ComputedRef<WidgetInstance | null> = computed(() => {
    if (!selectedWidgetId.value) return null
    return widgets.value.get(selectedWidgetId.value) || null
  })

  /** 组件数量 */
  const widgetCount: ComputedRef<number> = computed(() => {
    return widgets.value.size
  })

  /** 是否有选中的组件 */
  const hasSelection: ComputedRef<boolean> = computed(() => {
    return selectedWidgetId.value !== null
  })

  /** 总行数 */
  const totalRows: ComputedRef<number> = computed(() => {
    return layoutEngine.calculateTotalRows(widgetList.value)
  })

  /** 是否可以撤销 */
  const canUndo: ComputedRef<boolean> = computed(() => {
    return historyIndex.value > 0
  })

  /** 是否可以重做 */
  const canRedo: ComputedRef<boolean> = computed(() => {
    return historyIndex.value < history.value.length - 1
  })

  // ============================================
  // 看板操作
  // ============================================

  /**
   * 加载看板
   */
  async function loadDashboard(dashboardId: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      // TODO: 调用 API 获取看板数据
      const response = await fetch(`/api/dashboards/${dashboardId}`)
      const result: ApiResponse<Dashboard> = await response.json()

      if (result.success) {
        currentDashboard.value = result.data

        // 重建组件映射
        widgets.value.clear()
        for (const widget of result.data.widgets) {
          widgets.value.set(widget.id, widget)
        }

        // 重置编辑状态
        selectedWidgetId.value = null
        isDirty.value = false
        clearHistory()
        pushHistory()
      } else {
        error.value = result.message
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : '加载看板失败'
    } finally {
      isLoading.value = false
    }
  }

  /**
   * 创建新看板
   */
  async function createDashboard(config: Partial<DashboardConfig>): Promise<string | null> {
    isLoading.value = true
    error.value = null

    try {
      const response = await fetch('/api/dashboards', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      })
      const result: ApiResponse<Dashboard> = await response.json()

      if (result.success) {
        currentDashboard.value = result.data
        widgets.value.clear()
        isDirty.value = false
        clearHistory()
        pushHistory()
        return result.data.id
      } else {
        error.value = result.message
        return null
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : '创建看板失败'
      return null
    } finally {
      isLoading.value = false
    }
  }

  /**
   * 保存看板
   */
  async function saveDashboard(): Promise<boolean> {
    if (!currentDashboard.value) return false

    isLoading.value = true
    error.value = null

    try {
      const dashboard: Dashboard = {
        ...currentDashboard.value,
        widgets: widgetList.value
      }

      const response = await fetch(`/api/dashboards/${currentDashboard.value.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dashboard)
      })
      const result: ApiResponse<void> = await response.json()

      if (result.success) {
        isDirty.value = false
        return true
      } else {
        error.value = result.message
        return false
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : '保存看板失败'
      return false
    } finally {
      isLoading.value = false
    }
  }

  /**
   * 删除看板
   */
  async function deleteDashboard(dashboardId: string): Promise<boolean> {
    isLoading.value = true
    error.value = null

    try {
      const response = await fetch(`/api/dashboards/${dashboardId}`, {
        method: 'DELETE'
      })
      const result: ApiResponse<void> = await response.json()

      if (result.success) {
        if (currentDashboard.value?.id === dashboardId) {
          currentDashboard.value = null
          widgets.value.clear()
        }
        return true
      } else {
        error.value = result.message
        return false
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : '删除看板失败'
      return false
    } finally {
      isLoading.value = false
    }
  }

  // ============================================
  // 组件操作
  // ============================================

  /**
   * 添加组件
   */
  function addWidget(widget: WidgetInstance): void {
    widgets.value.set(widget.id, widget)
    isDirty.value = true
    pushHistory()
  }

  /**
   * 更新组件
   */
  function updateWidget(widgetId: string, updates: Partial<WidgetInstance>): void {
    const widget = widgets.value.get(widgetId)
    if (widget) {
      widgets.value.set(widgetId, { ...widget, ...updates })
      isDirty.value = true
      pushHistory()
    }
  }

  /**
   * 删除组件
   */
  function removeWidget(widgetId: string): void {
    widgets.value.delete(widgetId)

    if (selectedWidgetId.value === widgetId) {
      selectedWidgetId.value = null
    }

    // 解绑数据
    dataBinder.unbind(widgetId)

    isDirty.value = true
    pushHistory()
  }

  /**
   * 批量删除组件
   */
  function removeWidgets(widgetIds: string[]): void {
    widgetIds.forEach(id => {
      widgets.value.delete(id)
      dataBinder.unbind(id)
    })

    if (widgetIds.includes(selectedWidgetId.value || '')) {
      selectedWidgetId.value = null
    }

    isDirty.value = true
    pushHistory()
  }

  /**
   * 移动组件位置
   */
  function moveWidget(widgetId: string, position: GridPosition): void {
    const widget = widgets.value.get(widgetId)
    if (widget) {
      // 验证位置
      if (!layoutEngine.isValidPosition(position)) {
        console.warn('[DashboardStore] 无效的组件位置:', position)
        return
      }

      // 检查碰撞
      const collisions = layoutEngine.detectCollisions(
        widgetList.value.filter(w => w.id !== widgetId)
      )

      if (collisions.length > 0) {
        console.warn('[DashboardStore] 位置冲突:', collisions)
      }

      widgets.value.set(widgetId, { ...widget, position })
      isDirty.value = true
      pushHistory()
    }
  }

  /**
   * 调整组件大小
   */
  function resizeWidget(widgetId: string, size: { width: number; height: number }): void {
    const widget = widgets.value.get(widgetId)
    if (widget) {
      const newPosition = layoutEngine.resizeWidget(
        widget,
        size,
        widgetList.value
      )

      if (newPosition) {
        widgets.value.set(widgetId, { ...widget, position: newPosition })
        isDirty.value = true
        pushHistory()
      }
    }
  }

  /**
   * 选中组件
   */
  function selectWidget(widgetId: string | null): void {
    selectedWidgetId.value = widgetId
  }

  /**
   * 清除选择
   */
  function clearSelection(): void {
    selectedWidgetId.value = null
  }

  // ============================================
  // 模式切换
  // ============================================

  /**
   * 设置编辑模式
   */
  function setEditingMode(editing: boolean): void {
    isEditing.value = editing
    if (!editing) {
      clearSelection()
    }
  }

  /**
   * 设置显示模式
   */
  function setDisplayMode(mode: DisplayMode): void {
    displayMode.value = mode
    if (mode !== 'edit') {
      clearSelection()
    }
  }

  // ============================================
  // 数据刷新
  // ============================================

  /**
   * 刷新所有数据
   */
  async function refreshAllData(): Promise<void> {
    await dataBinder.refreshAll()
  }

  /**
   * 开始自动刷新
   */
  function startAutoRefresh(interval?: number): void {
    const refreshInterval = interval || currentDashboard.value?.refreshInterval || 60
    dataBinder.startAutoRefresh(refreshInterval)
  }

  /**
   * 停止自动刷新
   */
  function stopAutoRefresh(): void {
    dataBinder.stopAutoRefresh()
  }

  // ============================================
  // 剪贴板操作
  // ============================================

  /**
   * 复制组件
   */
  function copyWidget(): void {
    if (selectedWidget.value) {
      clipboard.value = JSON.parse(JSON.stringify(selectedWidget.value))
    }
  }

  /**
   * 粘贴组件
   */
  function pasteWidget(): void {
    if (!clipboard.value) return

    const newId = `widget-${Date.now()}`
    const newWidget: WidgetInstance = {
      ...JSON.parse(JSON.stringify(clipboard.value)),
      id: newId,
      position: {
        ...clipboard.value.position,
        x: clipboard.value.position.x + 1,
        y: clipboard.value.position.y + 1
      }
    }

    addWidget(newWidget)
    selectWidget(newId)
  }

  // ============================================
  // 撤销/重做
  // ============================================

  /**
   * 清除历史记录
   */
  function clearHistory(): void {
    history.value = []
    historyIndex.value = -1
  }

  /**
   * 推入历史记录
   */
  function pushHistory(): void {
    // 删除当前位置之后的历史
    history.value = history.value.slice(0, historyIndex.value + 1)

    // 添加当前状态
    const snapshot = widgetList.value.map(w => JSON.parse(JSON.stringify(w)))
    history.value.push(snapshot)

    // 限制历史记录大小
    if (history.value.length > maxHistorySize) {
      history.value.shift()
    }

    historyIndex.value = history.value.length - 1
  }

  /**
   * 撤销
   */
  function undo(): void {
    if (!canUndo.value) return

    historyIndex.value--
    restoreFromHistory()
  }

  /**
   * 重做
   */
  function redo(): void {
    if (!canRedo.value) return

    historyIndex.value++
    restoreFromHistory()
  }

  /**
   * 从历史记录恢复
   */
  function restoreFromHistory(): void {
    const snapshot = history.value[historyIndex.value]
    if (!snapshot) return

    widgets.value.clear()
    for (const widget of snapshot) {
      widgets.value.set(widget.id, widget)
    }

    isDirty.value = true
  }

  // ============================================
  // 工具方法
  // ============================================

  /**
   * 自动排列组件
   */
  function autoArrange(): void {
    const positions = layoutEngine.autoArrange(widgetList.value)

    positions.forEach((position, widgetId) => {
      const widget = widgets.value.get(widgetId)
      if (widget) {
        widgets.value.set(widgetId, { ...widget, position })
      }
    })

    isDirty.value = true
    pushHistory()
  }

  /**
   * 重置状态
   */
  function reset(): void {
    currentDashboard.value = null
    widgets.value.clear()
    selectedWidgetId.value = null
    isEditing.value = false
    displayMode.value = 'preview'
    isLoading.value = false
    error.value = null
    isDirty.value = false
    clipboard.value = null
    clearHistory()
    dataBinder.destroy()
  }

  return {
    // 状态
    currentDashboard,
    widgets,
    selectedWidgetId,
    isEditing,
    displayMode,
    isLoading,
    error,
    isDirty,
    clipboard,
    history,
    historyIndex,

    // 计算属性
    widgetList,
    selectedWidget,
    widgetCount,
    hasSelection,
    totalRows,
    canUndo,
    canRedo,

    // 看板操作
    loadDashboard,
    createDashboard,
    saveDashboard,
    deleteDashboard,

    // 组件操作
    addWidget,
    updateWidget,
    removeWidget,
    removeWidgets,
    moveWidget,
    resizeWidget,
    selectWidget,
    clearSelection,

    // 模式切换
    setEditingMode,
    setDisplayMode,

    // 数据刷新
    refreshAllData,
    startAutoRefresh,
    stopAutoRefresh,

    // 剪贴板
    copyWidget,
    pasteWidget,

    // 撤销/重做
    undo,
    redo,
    clearHistory,
    pushHistory,

    // 工具方法
    autoArrange,
    reset
  }
})

// 导出类型
export type DashboardStore = ReturnType<typeof useDashboardStore>
