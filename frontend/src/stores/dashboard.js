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
  refreshDashboardData
} from '@/api/dashboard'
import { ElMessage } from 'element-plus'

export const useDashboardStore = defineStore('dashboard', () => {
  // 状态
  const currentDashboard = ref(null)
  const dashboardData = ref(null)
  const widgets = ref([])
  const loading = ref(false)
  const dataLoading = ref(false)
  const error = ref(null)

  // 计算属性
  const dashboardId = computed(() => currentDashboard.value?.id || null)
  const dashboardName = computed(() => currentDashboard.value?.name || '')
  const canvasWidth = computed(() => currentDashboard.value?.width || 1920)
  const canvasHeight = computed(() => currentDashboard.value?.height || 1080)
  const backgroundColor = computed(() => currentDashboard.value?.backgroundColor || '#0d1b2a')
  const backgroundImage = computed(() => currentDashboard.value?.backgroundImage || '')
  const isPublic = computed(() => currentDashboard.value?.isPublic || false)
  const hasData = computed(() => dashboardData.value !== null)

  // 加载大屏配置
  async function loadDashboard(id) {
    loading.value = true
    error.value = null
    try {
      const res = await getDashboard(id)
      if (res.success) {
        currentDashboard.value = res.data
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

  // 加载大屏数据（所有组件的数据）
  async function loadDashboardData(id, params = {}) {
    dataLoading.value = true
    try {
      const res = await getDashboardData(id, params)
      if (res.success) {
        dashboardData.value = res.data
        return res.data
      }
      ElMessage.error(res.message || '加载大屏数据失败')
      return null
    } catch (err) {
      ElMessage.error(err.message || '加载大屏数据失败')
      return null
    } finally {
      dataLoading.value = false
    }
  }

  // 加载组件列表
  async function loadWidgets(dashboardId) {
    try {
      const res = await getWidgets(dashboardId)
      if (res.success) {
        widgets.value = res.data || []
        return res.data
      }
      ElMessage.error(res.message || '加载组件失败')
      return []
    } catch (err) {
      ElMessage.error(err.message || '加载组件失败')
      return []
    }
  }

  // 添加组件
  async function addNewWidget(dashboardId, widgetData) {
    try {
      const res = await addWidget(dashboardId, widgetData)
      if (res.success) {
        // 添加成功后刷新组件列表
        await loadWidgets(dashboardId)
        ElMessage.success('添加组件成功')
        return res.data
      }
      ElMessage.error(res.message || '添加组件失败')
      return null
    } catch (err) {
      ElMessage.error(err.message || '添加组件失败')
      return null
    }
  }

  // 更新组件
  async function updateExistingWidget(dashboardId, widgetId, widgetData) {
    try {
      const res = await updateWidget(dashboardId, widgetId, widgetData)
      if (res.success) {
        // 更新本地组件数据
        const index = widgets.value.findIndex(w => w.id === widgetId)
        if (index !== -1) {
          widgets.value[index] = { ...widgets.value[index], ...widgetData }
        }
        return res.data
      }
      ElMessage.error(res.message || '更新组件失败')
      return null
    } catch (err) {
      ElMessage.error(err.message || '更新组件失败')
      return null
    }
  }

  // 删除组件
  async function removeWidget(dashboardId, widgetId) {
    try {
      const res = await deleteWidget(dashboardId, widgetId)
      if (res.success) {
        // 从本地列表中移除
        widgets.value = widgets.value.filter(w => w.id !== widgetId)
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

  // 批量更新组件位置
  async function updatePositions(dashboardId, positions) {
    try {
      const res = await updateWidgetPositions(dashboardId, positions)
      if (res.success) {
        // 更新本地组件位置
        positions.forEach(pos => {
          const widget = widgets.value.find(w => w.id === pos.widgetId)
          if (widget) {
            widget.x = pos.x
            widget.y = pos.y
            widget.width = pos.width
            widget.height = pos.height
          }
        })
        return true
      }
      ElMessage.error(res.message || '更新位置失败')
      return false
    } catch (err) {
      ElMessage.error(err.message || '更新位置失败')
      return false
    }
  }

  // 刷新大屏数据
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

  // 清除当前大屏
  function clearDashboard() {
    currentDashboard.value = null
    dashboardData.value = null
    widgets.value = []
    error.value = null
  }

  // 更新本地大屏配置（不调用 API）
  function updateLocalDashboard(updates) {
    if (currentDashboard.value) {
      currentDashboard.value = { ...currentDashboard.value, ...updates }
    }
  }

  // 更新本地组件数据（不调用 API）
  function updateLocalWidget(widgetId, updates) {
    const index = widgets.value.findIndex(w => w.id === widgetId)
    if (index !== -1) {
      widgets.value[index] = { ...widgets.value[index], ...updates }
    }
  }

  // 获取组件数据
  function getWidgetDataById(widgetId) {
    if (!dashboardData.value?.widgets) return null
    return dashboardData.value.widgets.find(w => w.widgetId === widgetId) || null
  }

  return {
    // 状态
    currentDashboard,
    dashboardData,
    widgets,
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

    // 方法
    loadDashboard,
    loadDashboardData,
    loadWidgets,
    addNewWidget,
    updateExistingWidget,
    removeWidget,
    updatePositions,
    refreshData,
    clearDashboard,
    updateLocalDashboard,
    updateLocalWidget,
    getWidgetDataById
  }
})
