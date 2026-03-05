import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  getDashboard,
  getDashboardData,
  getWidgets,
  addWidget,
  updateWidget,
  deleteWidget,
  updateWidgetPositions,
  updateDashboard,
  refreshDashboardData
} from '@/api/dashboard'
import { ElMessage } from 'element-plus'

/**
 * Dashboard Pinia Store
 * 大屏管理状态存储
 *
 * 功能包括:
 * - 大屏 CRUD 操作
 * - 组件管理
 * - 选择状态管理
 * - 撤销/重做历史
 * - 剪贴板操作
 * - 画布视图控制
 */

export const useDashboardStore = defineStore('dashboard', () => {
  // ==================== 状态 ====================

  // 当前编辑的大屏
  const currentDashboard = ref(null)

  // 大屏数据（组件执行后的数据）
  const dashboardData = ref(null)

  // 组件列表
  const widgets = ref([])

  // 选中的组件 ID
  const selectedWidgetId = ref(null)

  // 历史记录（撤销/重做）
  const history = ref([])
  const historyIndex = ref(-1)

  // 是否已修改
  const isDirty = ref(false)

  // 画布缩放比例
  const scale = ref(1)

  // 是否显示网格
  const showGrid = ref(true)

  // 剪贴板
  const clipboard = ref(null)

  // 加载状态
  const loading = ref(false)
  const dataLoading = ref(false)
  const error = ref(null)

  // ==================== 计算属性 ====================

  // 大屏基本信息
  const dashboardId = computed(() => currentDashboard.value?.dashboardId || currentDashboard.value?.id || null)
  const dashboardName = computed(() => currentDashboard.value?.name || '')
  const canvasWidth = computed(() => {
    if (currentDashboard.value?.themeConfig) {
      try {
        const config = typeof currentDashboard.value.themeConfig === 'string'
          ? JSON.parse(currentDashboard.value.themeConfig)
          : currentDashboard.value.themeConfig
        return config.width || 1920
      } catch { return 1920 }
    }
    return currentDashboard.value?.width || 1920
  })
  const canvasHeight = computed(() => {
    if (currentDashboard.value?.themeConfig) {
      try {
        const config = typeof currentDashboard.value.themeConfig === 'string'
          ? JSON.parse(currentDashboard.value.themeConfig)
          : currentDashboard.value.themeConfig
        return config.height || 1080
      } catch { return 1080 }
    }
    return currentDashboard.value?.height || 1080
  })
  const backgroundColor = computed(() => {
    if (currentDashboard.value?.themeConfig) {
      try {
        const config = typeof currentDashboard.value.themeConfig === 'string'
          ? JSON.parse(currentDashboard.value.themeConfig)
          : currentDashboard.value.themeConfig
        return config.backgroundColor || '#0d1b2a'
      } catch { return '#0d1b2a' }
    }
    return currentDashboard.value?.backgroundColor || '#0d1b2a'
  })
  const backgroundImage = computed(() => {
    if (currentDashboard.value?.themeConfig) {
      try {
        const config = typeof currentDashboard.value.themeConfig === 'string'
          ? JSON.parse(currentDashboard.value.themeConfig)
          : currentDashboard.value.themeConfig
        return config.backgroundImage || ''
      } catch { return '' }
    }
    return currentDashboard.value?.backgroundImage || ''
  })
  const isPublic = computed(() => currentDashboard.value?.isPublic || false)
  const hasData = computed(() => dashboardData.value !== null)

  // 选中的组件
  const selectedWidget = computed(() => {
    if (!selectedWidgetId.value) return null
    return widgets.value.find(w => (w.widgetId || w.id) === selectedWidgetId.value) || null
  })

  // 撤销/重做状态
  const canUndo = computed(() => historyIndex.value > 0)
  const canRedo = computed(() => historyIndex.value < history.value.length - 1)

  // ==================== 大屏管理 ====================

  /**
   * 加载大屏配置
   * @param {number|string} id - 大屏ID
   * @returns {Promise<Object|null>}
   */
  async function loadDashboard(id) {
    loading.value = true
    error.value = null
    try {
      const res = await getDashboard(id)
      if (res.success) {
        currentDashboard.value = res.data

        // 解析组件列表
        const rawWidgets = res.data.widgets || res.data.Widgets || []
        widgets.value = rawWidgets.map((w, index) => {
          // 解析配置
          let config = {}
          let styleConfig = {}
          try {
            config = JSON.parse(w.dataConfig || w.DataConfig || '{}')
          } catch { config = {} }
          try {
            styleConfig = JSON.parse(w.styleConfig || w.StyleConfig || '{}')
          } catch { styleConfig = {} }

          return {
            widgetId: w.widgetId || w.WidgetId || w.id || `widget-${index}`,
            widgetType: w.widgetType || w.WidgetType,
            title: w.title || w.Title,
            reportId: w.reportId || w.ReportId,
            positionX: w.positionX ?? w.PositionX ?? 0,
            positionY: w.positionY ?? w.PositionY ?? 0,
            width: w.width ?? w.Width ?? 3,
            height: w.height ?? w.Height ?? 3,
            dataConfig: w.dataConfig || w.DataConfig,
            styleConfig: w.styleConfig || w.StyleConfig,
            config,
            conditionStyles: styleConfig.conditionStyles || []
          }
        })

        // 初始化历史记录
        saveHistory()

        return res.data
      }
      error.value = res.message || '加载大屏失败'
      ElMessage.error(error.value)
      return null
    } catch (err) {
      error.value = err.message || '加载大屏失败'
      ElMessage.error(error.value)
      return null
    } finally {
      loading.value = false
    }
  }

  /**
   * 保存大屏
   * @returns {Promise<boolean>}
   */
  async function saveDashboard() {
    if (!currentDashboard.value) return false

    loading.value = true
    try {
      const id = dashboardId.value

      // 构建保存数据
      const data = {
        name: currentDashboard.value.name,
        description: currentDashboard.value.description,
        layoutConfig: currentDashboard.value.layoutConfig,
        themeConfig: currentDashboard.value.themeConfig
      }

      const res = await updateDashboard(id, data)
      if (res.success) {
        isDirty.value = false
        ElMessage.success('保存成功')
        return true
      }
      ElMessage.error(res.message || '保存失败')
      return false
    } catch (err) {
      ElMessage.error(err.message || '保存失败')
      return false
    } finally {
      loading.value = false
    }
  }

  /**
   * 设置修改状态
   * @param {boolean} dirty - 是否已修改
   */
  function setDirty(dirty) {
    isDirty.value = dirty
  }

  // ==================== 组件管理 ====================

  /**
   * 添加组件
   * @param {Object} widget - 组件数据
   */
  async function addNewWidget(widget) {
    if (!currentDashboard.value) return null

    try {
      const id = dashboardId.value
      const res = await addWidget(id, widget)
      if (res.success) {
        // 添加到本地列表
        const newWidget = {
          ...widget,
          widgetId: res.data.widgetId || res.data.WidgetId || res.data.id
        }
        widgets.value.push(newWidget)
        saveHistory()
        ElMessage.success('添加组件成功')
        return newWidget
      }
      ElMessage.error(res.message || '添加组件失败')
      return null
    } catch (err) {
      ElMessage.error(err.message || '添加组件失败')
      return null
    }
  }

  /**
   * 更新组件
   * @param {number|string} id - 组件ID
   * @param {Object} updates - 更新数据
   */
  async function updateExistingWidget(id, updates) {
    if (!currentDashboard.value) return false

    try {
      const dashId = dashboardId.value
      const res = await updateWidget(dashId, id, updates)
      if (res.success) {
        // 更新本地数据
        const index = widgets.value.findIndex(w => (w.widgetId || w.id) === id)
        if (index !== -1) {
          widgets.value[index] = { ...widgets.value[index], ...updates }
        }
        saveHistory()
        return true
      }
      ElMessage.error(res.message || '更新组件失败')
      return false
    } catch (err) {
      ElMessage.error(err.message || '更新组件失败')
      return false
    }
  }

  /**
   * 删除组件
   * @param {number|string} id - 组件ID
   */
  async function deleteExistingWidget(id) {
    if (!currentDashboard.value) return false

    try {
      const dashId = dashboardId.value
      const res = await deleteWidget(dashId, id)
      if (res.success) {
        // 从本地列表移除
        widgets.value = widgets.value.filter(w => (w.widgetId || w.id) !== id)

        // 如果删除的是选中组件，清除选择
        if (selectedWidgetId.value === id) {
          selectedWidgetId.value = null
        }

        saveHistory()
        ElMessage.success('删除组件成功')
        return true
      }
      ElMessage.error(res.message || '删除组件失败')
      return false
    } catch (err) {
      ElMessage.error(err.message || '删除组件失败')
      return false
    }
  }

  /**
   * 选择组件
   * @param {number|string|null} id - 组件ID，null 表示取消选择
   */
  function selectWidget(id) {
    selectedWidgetId.value = id
  }

  /**
   * 批量更新组件位置
   * @param {Array<{widgetId: number, x: number, y: number, w: number, h: number}>} positions - 位置数组
   */
  async function updatePositions(positions) {
    if (!currentDashboard.value) return false

    try {
      const dashId = dashboardId.value
      const res = await updateWidgetPositions(dashId, positions)
      if (res.success) {
        // 更新本地组件位置
        positions.forEach(pos => {
          const widget = widgets.value.find(w => (w.widgetId || w.id) === pos.widgetId)
          if (widget) {
            widget.positionX = pos.x
            widget.positionY = pos.y
            widget.width = pos.w
            widget.height = pos.h
          }
        })
        saveHistory()
        return true
      }
      return false
    } catch (err) {
      return false
    }
  }

  // ==================== 历史管理（撤销/重做） ====================

  /**
   * 保存当前状态到历史
   */
  function saveHistory() {
    if (!widgets.value.length && historyIndex.value === -1) {
      // 初始化时保存空状态
      history.value = [[]]
      historyIndex.value = 0
      return
    }

    // 截断后面的历史
    history.value = history.value.slice(0, historyIndex.value + 1)

    // 添加当前快照
    const snapshot = JSON.parse(JSON.stringify(widgets.value))
    history.value.push(snapshot)

    // 限制历史记录数量（最多 50 条）
    if (history.value.length > 50) {
      history.value.shift()
    }

    historyIndex.value = history.value.length - 1
    isDirty.value = true
  }

  /**
   * 撤销
   */
  function undo() {
    if (!canUndo.value) return

    historyIndex.value--
    const snapshot = history.value[historyIndex.value]
    widgets.value = JSON.parse(JSON.stringify(snapshot))
  }

  /**
   * 重做
   */
  function redo() {
    if (!canRedo.value) return

    historyIndex.value++
    const snapshot = history.value[historyIndex.value]
    widgets.value = JSON.parse(JSON.stringify(snapshot))
  }

  // ==================== 剪贴板操作 ====================

  /**
   * 复制选中组件到剪贴板
   */
  function copyWidget() {
    if (!selectedWidget.value) return

    clipboard.value = JSON.parse(JSON.stringify(selectedWidget.value))
    ElMessage.success('已复制组件')
  }

  /**
   * 粘贴剪贴板内容
   */
  async function pasteWidget() {
    if (!clipboard.value || !currentDashboard.value) return

    // 创建新组件（偏移位置）
    const newWidget = {
      ...JSON.parse(JSON.stringify(clipboard.value)),
      widgetId: null, // 新组件没有 ID，需要后端生成
      positionX: (clipboard.value.positionX || 0) + 20,
      positionY: (clipboard.value.positionY || 0) + 20
    }

    // 调用 API 添加组件
    const result = await addNewWidget(newWidget)
    if (result) {
      // 选中新组件
      selectWidget(result.widgetId || result.id)
    }
  }

  // ==================== 视图控制 ====================

  /**
   * 设置缩放比例
   * @param {number} newScale - 缩放值 (0.25 ~ 2)
   */
  function setScale(newScale) {
    scale.value = Math.max(0.25, Math.min(2, newScale))
  }

  /**
   * 切换网格显示
   */
  function toggleGrid() {
    showGrid.value = !showGrid.value
  }

  // ==================== 数据加载 ====================

  /**
   * 加载大屏数据
   * @param {number|string} id - 大屏ID
   */
  async function loadDashboardData(id) {
    dataLoading.value = true
    try {
      const res = await getDashboardData(id)
      if (res.success) {
        dashboardData.value = res.data
        return res.data
      }
      return null
    } catch (err) {
      return null
    } finally {
      dataLoading.value = false
    }
  }

  /**
   * 加载组件列表
   * @param {number|string} id - 大屏ID
   */
  async function loadWidgets(id) {
    try {
      const res = await getWidgets(id)
      if (res.success) {
        widgets.value = res.data || []
        return res.data
      }
      return null
    } catch (err) {
      return null
    }
  }

  /**
   * 刷新大屏数据
   * @param {number|string} id - 大屏ID
   */
  async function refreshData(id) {
    dataLoading.value = true
    try {
      const res = await refreshDashboardData(id)
      if (res.success) {
        dashboardData.value = res.data
        ElMessage.success('数据刷新成功')
        return res.data
      }
      ElMessage.error(res.message || '刷新数据失败')
      return null
    } catch (err) {
      ElMessage.error(err.message || '刷新数据失败')
      return null
    } finally {
      dataLoading.value = false
    }
  }

  // ==================== 工具方法 ====================

  /**
   * 清除当前大屏
   */
  function clearDashboard() {
    currentDashboard.value = null
    dashboardData.value = null
    widgets.value = []
    selectedWidgetId.value = null
    history.value = []
    historyIndex.value = -1
    isDirty.value = false
    scale.value = 1
    showGrid.value = true
    clipboard.value = null
    error.value = null
  }

  /**
   * 重置所有状态
   */
  function reset() {
    clearDashboard()
  }

  /**
   * 更新本地大屏配置（不调用 API）
   * @param {Object} updates - 更新数据
   */
  function updateLocalDashboard(updates) {
    if (currentDashboard.value) {
      currentDashboard.value = { ...currentDashboard.value, ...updates }
      isDirty.value = true
    }
  }

  /**
   * 更新本地组件数据（不调用 API）
   * @param {number|string} widgetId - 组件ID
   * @param {Object} updates - 更新数据
   */
  function updateLocalWidget(widgetId, updates) {
    const index = widgets.value.findIndex(w => (w.widgetId || w.id) === widgetId)
    if (index !== -1) {
      widgets.value[index] = { ...widgets.value[index], ...updates }
      isDirty.value = true
    }
  }

  /**
   * 获取组件数据
   * @param {number|string} widgetId - 组件ID
   */
  function getWidgetDataById(widgetId) {
    if (!dashboardData.value?.widgets) return null
    return dashboardData.value.widgets.find(w => w.widgetId === widgetId) || null
  }

  // ==================== 导出 ====================

  return {
    // 状态
    currentDashboard,
    dashboardData,
    widgets,
    selectedWidgetId,
    history,
    historyIndex,
    isDirty,
    scale,
    showGrid,
    clipboard,
    loading,
    dataLoading,
    error,

    // 计算属性
    dashboardId,
    dashboardName,
    canvasWidth,
    canvasHeight,
    backgroundColor,
    backgroundImage,
    isPublic,
    hasData,
    selectedWidget,
    canUndo,
    canRedo,

    // 大屏管理
    loadDashboard,
    saveDashboard,
    setDirty,

    // 组件管理
    addNewWidget,
    updateExistingWidget,
    deleteExistingWidget,
    selectWidget,
    updatePositions,

    // 历史管理
    saveHistory,
    undo,
    redo,

    // 剪贴板
    copyWidget,
    pasteWidget,

    // 视图控制
    setScale,
    toggleGrid,

    // 数据加载
    loadDashboardData,
    loadWidgets,
    refreshData,

    // 工具方法
    clearDashboard,
    reset,
    updateLocalDashboard,
    updateLocalWidget,
    getWidgetDataById
  }
})
