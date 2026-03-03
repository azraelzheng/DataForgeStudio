import request from './request'

/**
 * Dashboard 大屏 API
 * 提供大屏的 CRUD、组件管理、一键转换和数据获取功能
 */

// ==================== 大屏 CRUD ====================

/**
 * 获取大屏列表（分页）
 * @param {Object} params - 查询参数
 * @param {number} params.page - 页码
 * @param {number} params.pageSize - 每页数量
 * @param {string} params.keyword - 搜索关键词
 * @param {boolean} params.isActive - 是否启用
 * @returns {Promise}
 */
export function getDashboards(params) {
  return request.get('/dashboards', { params })
}

/**
 * 获取大屏详情
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function getDashboard(id) {
  return request.get(`/dashboards/${id}`)
}

/**
 * 创建大屏
 * @param {Object} data - 大屏数据
 * @param {string} data.name - 大屏名称
 * @param {string} data.description - 描述
 * @param {number} data.width - 画布宽度
 * @param {number} data.height - 画布高度
 * @param {string} data.backgroundColor - 背景色
 * @param {string} data.backgroundImage - 背景图片URL
 * @param {boolean} data.isPublic - 是否公开
 * @param {Object} data.settings - 其他设置
 * @returns {Promise}
 */
export function createDashboard(data) {
  return request.post('/dashboards', data)
}

/**
 * 更新大屏
 * @param {number|string} id - 大屏ID
 * @param {Object} data - 大屏数据
 * @returns {Promise}
 */
export function updateDashboard(id, data) {
  return request.put(`/dashboards/${id}`, data)
}

/**
 * 删除大屏
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function deleteDashboard(id) {
  return request.delete(`/dashboards/${id}`)
}

/**
 * 切换大屏公开状态
 * 注意：此功能通过更新大屏实现，后端未提供单独的 toggle 端点
 * @param {number|string} id - 大屏ID
 * @param {Object} data - 更新数据
 * @returns {Promise}
 */
export function toggleDashboard(id, data) {
  return request.put(`/dashboards/${id}`, data)
}

/**
 * 复制大屏
 * 注意：此功能需要手动实现，后端未提供 copy 端点
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export async function copyDashboard(id) {
  // 获取原大屏详情
  const detail = await request.get(`/dashboards/${id}`)
  if (!detail.success) return detail

  // 创建新大屏
  const { dashboardId, widgets, createdTime, updatedTime, ...rest } = detail.data
  const newDashboard = {
    ...rest,
    name: `${rest.name} (副本)`
  }
  return request.post('/dashboards', newDashboard)
}

// ==================== 组件管理 ====================

/**
 * 添加组件到大屏
 * @param {number|string} dashboardId - 大屏ID
 * @param {Object} data - 组件数据
 * @param {string} data.widgetType - 组件类型 (chart/table/text/image/gauge/map等)
 * @param {string} data.title - 组件标题
 * @param {number} data.x - X坐标
 * @param {number} data.y - Y坐标
 * @param {number} data.width - 组件宽度
 * @param {number} data.height - 组件高度
 * @param {Object} data.config - 组件配置
 * @param {Object} data.dataSource - 数据源配置
 * @returns {Promise}
 */
export function addWidget(dashboardId, data) {
  return request.post(`/dashboards/${dashboardId}/widgets`, data)
}

/**
 * 更新大屏组件
 * @param {number|string} dashboardId - 大屏ID
 * @param {number|string} widgetId - 组件ID
 * @param {Object} data - 组件数据
 * @returns {Promise}
 */
export function updateWidget(dashboardId, widgetId, data) {
  return request.put(`/dashboards/${dashboardId}/widgets/${widgetId}`, data)
}

/**
 * 删除大屏组件
 * @param {number|string} dashboardId - 大屏ID
 * @param {number|string} widgetId - 组件ID
 * @returns {Promise}
 */
export function deleteWidget(dashboardId, widgetId) {
  return request.delete(`/dashboards/${dashboardId}/widgets/${widgetId}`)
}

/**
 * 批量更新组件位置
 * @param {number|string} dashboardId - 大屏ID
 * @param {Array} positions - 位置数组 [{widgetId, x, y, width, height}, ...]
 * @returns {Promise}
 */
export function updateWidgetPositions(dashboardId, positions) {
  return request.put(`/dashboards/${dashboardId}/widgets/positions`, { positions })
}

/**
 * 获取大屏组件列表
 * @param {number|string} dashboardId - 大屏ID
 * @returns {Promise}
 */
export function getWidgets(dashboardId) {
  return request.get(`/dashboards/${dashboardId}/widgets`)
}

// ==================== 一键转换 ====================

/**
 * 从报表转换为大屏
 * @param {number|string} reportId - 报表ID
 * @param {string} dashboardName - 大屏名称
 * @param {Object} options - 转换选项
 * @param {number} options.width - 画布宽度 (默认1920)
 * @param {number} options.height - 画布高度 (默认1080)
 * @param {string} options.theme - 主题 (dark/light)
 * @returns {Promise}
 */
export function convertFromReport(reportId, dashboardName, options = {}) {
  return request.post('/dashboards/convert', {
    reportId,
    dashboardName,
    ...options
  })
}

// ==================== 数据获取 ====================

/**
 * 获取大屏数据（所有组件的数据）
 * @param {number|string} id - 大屏ID
 * @param {Object} params - 查询参数（可选的筛选条件）
 * @returns {Promise}
 */
export function getDashboardData(id, params) {
  return request.get(`/dashboards/${id}/data`, { params })
}

/**
 * 获取单个组件的数据
 * @param {number|string} dashboardId - 大屏ID
 * @param {number|string} widgetId - 组件ID
 * @param {Object} params - 查询参数
 * @returns {Promise}
 */
export function getWidgetData(dashboardId, widgetId, params) {
  return request.get(`/dashboards/${dashboardId}/widgets/${widgetId}/data`, { params })
}

/**
 * 刷新大屏数据
 * 注意：后端未提供 refresh 端点，使用 getDashboardData 获取最新数据
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function refreshDashboardData(id) {
  return request.get(`/dashboards/${id}/data`)
}

// ==================== 公开访问 ====================

/**
 * 获取公开大屏详情（无需登录）
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function getPublicDashboard(id) {
  return request.get(`/public/d/${id}`)
}

/**
 * 获取公开大屏数据（无需登录）
 * @param {number|string} id - 大屏ID
 * @param {Object} params - 查询参数
 * @returns {Promise}
 */
export function getPublicDashboardData(id, params) {
  return request.get(`/public/d/${id}/data`, { params })
}

// ==================== 模板管理 ====================

/**
 * 获取大屏模板列表
 * @param {Object} params - 查询参数
 * @returns {Promise}
 */
export function getDashboardTemplates(params) {
  return request.get('/dashboards/templates', { params })
}

/**
 * 从模板创建大屏
 * @param {number|string} templateId - 模板ID
 * @param {Object} data - 大屏基本信息
 * @returns {Promise}
 */
export function createFromTemplate(templateId, data) {
  return request.post(`/dashboards/templates/${templateId}/create`, data)
}

/**
 * 将大屏保存为模板
 * @param {number|string} dashboardId - 大屏ID
 * @param {Object} data - 模板信息
 * @returns {Promise}
 */
export function saveAsTemplate(dashboardId, data) {
  return request.post(`/dashboards/${dashboardId}/save-as-template`, data)
}

// ==================== 导出功能 ====================

/**
 * 导出大屏配置
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function exportDashboard(id) {
  return request.get(`/dashboards/${id}/export`, {
    responseType: 'blob'
  })
}

/**
 * 导入大屏配置
 * @param {FormData} formData - 包含配置文件的表单数据
 * @returns {Promise}
 */
export function importDashboard(formData) {
  return request.post('/dashboards/import', formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  })
}

/**
 * 导出大屏为图片
 * @param {number|string} id - 大屏ID
 * @param {Object} options - 导出选项
 * @returns {Promise}
 */
export function exportAsImage(id, options = {}) {
  return request.post(`/dashboards/${id}/export-image`, options, {
    responseType: 'blob'
  })
}

/**
 * 导出大屏为PDF
 * @param {number|string} id - 大屏ID
 * @param {Object} options - 导出选项
 * @returns {Promise}
 */
export function exportAsPdf(id, options = {}) {
  return request.post(`/dashboards/${id}/export-pdf`, options, {
    responseType: 'blob'
  })
}

// ==================== 分享管理 ====================

/**
 * 获取大屏分享设置
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function getShareSettings(id) {
  return request.get(`/dashboards/${id}/share`)
}

/**
 * 更新大屏分享设置
 * @param {number|string} id - 大屏ID
 * @param {Object} data - 分享设置
 * @param {boolean} data.isPublic - 是否公开
 * @param {string} data.accessPassword - 访问密码（可选）
 * @param {string} data.expireTime - 过期时间（可选）
 * @returns {Promise}
 */
export function updateShareSettings(id, data) {
  return request.put(`/dashboards/${id}/share`, data)
}

/**
 * 生成分享链接
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function generateShareLink(id) {
  return request.post(`/dashboards/${id}/share/generate-link`)
}

/**
 * 取消分享
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function revokeShare(id) {
  return request.delete(`/dashboards/${id}/share`)
}

/**
 * 验证大屏访问密码
 * @param {string} publicId - 公开访问ID
 * @param {string} password - 访问密码
 * @returns {Promise}
 */
export function verifyAccessPassword(publicId, password) {
  return request.post(`/dashboards/public/${publicId}/verify`, { password })
}

// ==================== 预览和统计 ====================

/**
 * 获取大屏预览数据
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function getPreviewData(id) {
  return request.get(`/dashboards/${id}/preview`)
}

/**
 * 记录大屏访问
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function recordView(id) {
  return request.post(`/dashboards/${id}/view`)
}

/**
 * 获取大屏统计数据
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function getDashboardStatistics(id) {
  return request.get(`/dashboards/${id}/statistics`)
}

// 导出为命名对象（与项目风格一致）
export const dashboardApi = {
  // 大屏 CRUD
  getDashboards,
  getDashboard,
  createDashboard,
  updateDashboard,
  deleteDashboard,
  toggleDashboard,
  copyDashboard,

  // 组件管理
  addWidget,
  updateWidget,
  deleteWidget,
  updateWidgetPositions,
  getWidgets,

  // 一键转换
  convertFromReport,

  // 数据获取
  getDashboardData,
  getWidgetData,
  refreshDashboardData,

  // 公开访问
  getPublicDashboard,
  getPublicDashboardData,

  // 模板管理
  getDashboardTemplates,
  createFromTemplate,
  saveAsTemplate,

  // 导出功能
  exportDashboard,
  importDashboard,
  exportAsImage,
  exportAsPdf,

  // 分享管理
  getShareSettings,
  updateShareSettings,
  generateShareLink,
  revokeShare,
  verifyAccessPassword,

  // 预览和统计
  getPreviewData,
  recordView,
  getDashboardStatistics
}

export default dashboardApi
