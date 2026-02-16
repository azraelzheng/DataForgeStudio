import axios from 'axios'
import { ElMessage } from 'element-plus'

// 创建 axios 实例
const request = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 30000
})

// 请求拦截器 - 添加调试日志
request.interceptors.request.use(
  (config) => {
    // 从 localStorage 获取 token
    const token = localStorage.getItem('token')
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`
    }
    console.log('API Request:', config.method?.toUpperCase(), config.baseURL + config.url)
    return config
  },
  (error) => {
    console.error('Request Error:', error)
    return Promise.reject(error)
  }
)

// PascalCase 转 camelCase 的工具函数
function toCamelCase(obj) {
  if (obj === null || typeof obj !== 'object') {
    return obj
  }

  if (Array.isArray(obj)) {
    return obj.map(toCamelCase)
  }

  const result = {}
  for (const key in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, key)) {
      // 将 PascalCase 转换为 camelCase
      const camelKey = key.charAt(0).toLowerCase() + key.slice(1)
      result[camelKey] = toCamelCase(obj[key])
    }
  }
  return result
}

// 响应拦截器
request.interceptors.response.use(
  (response) => {
    // 如果是 blob 类型（文件下载），直接返回原始数据
    if (response.config.responseType === 'blob') {
      return response.data
    }

    const res = response.data

    console.log('API Response:', res) // Debug log

    // 检查业务状态码 - 支持 PascalCase (后端) 和 camelCase
    const isSuccess = res.Success === true || res.success === true
    const isFailure = res.Success === false || res.success === false

    if (isFailure) {
      const message = res.Message || res.message || '请求失败'
      ElMessage.error(message)
      return Promise.reject(new Error(message))
    }

    // 转换后端的 PascalCase 到前端的 camelCase
    const rawData = res.Data !== undefined ? res.Data : res.data
    const convertedData = toCamelCase(rawData)

    return {
      success: res.Success !== undefined ? res.Success : res.success,
      message: res.Message !== undefined ? res.Message : res.message,
      data: convertedData,
      errorCode: res.ErrorCode !== undefined ? res.ErrorCode : res.errorCode,
      timestamp: res.Timestamp !== undefined ? res.Timestamp : res.timestamp
    }
  },
  (error) => {
    console.error('Response Error:', error)

    // 处理 HTTP 错误状态码
    if (error.response) {
      const status = error.response.status

      switch (status) {
        case 401:
          ElMessage.error('未授权，请重新登录')
          localStorage.removeItem('token')
          window.location.href = '/login'
          break
        case 403:
          ElMessage.error('无权访问')
          break
        case 404:
          ElMessage.error('请求的资源不存在')
          break
        case 500:
          ElMessage.error('服务器内部错误')
          break
        default:
          ElMessage.error(error.response.data?.message || '网络错误')
      }
    } else if (error.request) {
      ElMessage.error('网络连接失败，请检查网络')
    } else {
      ElMessage.error('请求失败，请稍后重试')
    }

    return Promise.reject(error)
  }
)

export default request

// API 接口定义
export const authApi = {
  // 登录
  login: (data) => request.post('/auth/login', data),

  // 获取当前用户信息
  getCurrentUser: () => request.get('/auth/current-user'),

  // 修改密码
  changePassword: (data) => request.post('/auth/change-password', data),

  // 验证 Token
  validateToken: (data) => request.post('/auth/validate-token', data)
}

export const userApi = {
  // 获取用户列表（分页）
  getUsers: (params) => request.get('/users', { params }),

  // 创建用户
  createUser: (data) => request.post('/users', data),

  // 更新用户
  updateUser: (id, data) => request.put(`/users/${id}`, data),

  // 删除用户
  deleteUser: (id) => request.delete(`/users/${id}`),

  // 重置密码
  resetPassword: (id, data) => request.post(`/users/${id}/reset-password`, data),

  // 分配角色
  assignRoles: (id, data) => request.post(`/users/${id}/roles`, data)
}

export const roleApi = {
  // 获取角色列表
  getRoles: (params) => request.get('/roles', { params }),

  // 创建角色
  createRole: (data) => request.post('/roles', data),

  // 更新角色
  updateRole: (id, data) => request.put(`/roles/${id}`, data),

  // 删除角色
  deleteRole: (id) => request.delete(`/roles/${id}`),

  // 分配权限
  assignPermissions: (id, data) => request.post(`/roles/${id}/permissions`, data)
}

export const dataSourceApi = {
  // 获取数据源列表
  getDataSources: (params) => request.get('/datasources', { params }),

  // 创建数据源
  createDataSource: (data) => request.post('/datasources', data),

  // 更新数据源
  updateDataSource: (id, data) => request.put(`/datasources/${id}`, data),

  // 删除数据源
  deleteDataSource: (id) => request.delete(`/datasources/${id}`),

  // 测试连接（测试已存在的数据源）
  testConnection: (id) => request.post(`/datasources/${id}/test`),

  // 测试连接（创建前测试，传入完整数据源对象）
  testConnectionBeforeSave: (data) => request.post('/datasources/test', data),

  // 获取数据库列表
  getDatabases: (data) => request.post('/datasources/databases', data),

  // 停用/启用数据源
  toggleActive: (id) => request.post(`/datasources/${id}/toggle-active`),

  // 获取默认数据源
  getDefaultDataSource: () => request.get('/datasources/default'),

  // 获取数据源的表结构
  getTableStructure: (dataSourceId) => request.get(`/datasources/${dataSourceId}/tables`)
}

export const reportApi = {
  // 获取报表列表（分页）
  getReports: (params) => request.get('/reports', { params }),

  // 获取报表详情
  getReport: (id) => request.get(`/reports/${id}`),

  // 创建报表
  createReport: (data) => request.post('/reports', data),

  // 更新报表
  updateReport: (id, data) => request.put(`/reports/${id}`, data),

  // 删除报表
  deleteReport: (id) => request.delete(`/reports/${id}`),

  // 执行报表查询
  executeReport: (id, params) => request.post(`/reports/${id}/execute`, params),

  // 测试SQL查询（用于报表设计器）
  testQuery: (data) => request.post('/reports/test-query', data),

  // 导出报表
  exportReport: (id, params) => request.post(`/reports/${id}/export`, params, {
    responseType: 'blob'
  }),

  // 报表统计
  getReportStatistics: (id) => request.get(`/reports/${id}/statistics`),

  // 复制报表
  copyReport: (id) => request.post(`/reports/${id}/copy`),

  // 导出所有报表配置
  exportAllConfigs: () => request.get('/reports/export-config'),

  // 切换报表启用状态
  toggleReport: (id) => request.post(`/reports/${id}/toggle`),

  // 获取查询字段结构（用于自动识别字段）
  getQuerySchema: (data) => request.post('/reports/query-schema', data)
}

export const licenseApi = {
  // 获取许可证信息
  getLicense: () => request.get('/license'),

  // 激活许可证
  activateLicense: (data) => request.post('/license/activate', data)
}

export const systemApi = {
  // 获取系统配置
  getConfigs: (params) => request.get('/system/configs', { params }),

  // 更新系统配置
  updateConfig: (id, data) => request.put(`/system/configs/${id}`, data),

  // 获取操作日志（分页）
  getLogs: (params) => request.get('/system/logs', { params }),

  // 清空日志
  clearLogs: () => request.delete('/system/logs'),

  // 根据查询条件删除日志
  deleteLogsByQuery: (params) => request.delete('/system/logs/delete-by-query', { params }),

  // 根据ID列表删除日志
  deleteLogsByIds: (logIds) => request.delete('/system/logs/delete-by-ids', { data: logIds }),

  // 导出日志
  exportLogs: (params) => request.get('/system/logs/export', {
    params,
    responseType: 'blob'
  }),

  // 导出选中的日志
  exportSelectedLogs: (logIds) => request.post('/system/logs/export-selected', logIds, {
    responseType: 'blob'
  }),

  // 创建备份
  createBackup: (data) => request.post('/system/backup', data),

  // 获取备份列表
  getBackups: (params) => request.get('/system/backups', { params }),

  // 删除备份
  deleteBackup: (id) => request.delete(`/system/backups/${id}`),

  // 恢复备份
  restoreBackup: (id) => request.post(`/system/backups/${id}/restore`),

  // 获取机器码
  getMachineCode: () => request.get('/system/machine-code'),

  // 验证许可证
  validateLicense: (params) => request.post('/license/validate', null, { params }),

  // 激活许可证
  activateLicense: (data) => request.post('/license/activate', data),

  // 备份计划 API
  getBackupSchedules: () => request.get('/system/backup-schedules'),
  createBackupSchedule: (data) => request.post('/system/backup-schedules', data),
  updateBackupSchedule: (id, data) => request.put(`/system/backup-schedules/${id}`, data),
  deleteBackupSchedule: (id) => request.delete(`/system/backup-schedules/${id}`),
  toggleBackupSchedule: (id) => request.post(`/system/backup-schedules/${id}/toggle`)
}
