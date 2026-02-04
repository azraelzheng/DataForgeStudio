import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '../api/request'
import { ElMessage } from 'element-plus'

export const useUserStore = defineStore('user', () => {
  // 状态 - 从 localStorage 读取 token
  const getTokenFromStorage = () => localStorage.getItem('token') || ''
  const token = ref(getTokenFromStorage())
  const userInfo = ref(null)
  const permissions = ref([])

  // 计算属性 - 同时检查 store 和 localStorage
  const isLoggedIn = computed(() => {
    return !!token.value && !!localStorage.getItem('token')
  })
  const username = computed(() => userInfo.value?.username || '')
  const realName = computed(() => userInfo.value?.realName || '')

  // 方法
  const setToken = (newToken) => {
    token.value = newToken
    if (newToken) {
      localStorage.setItem('token', newToken)
    } else {
      localStorage.removeItem('token')
    }
  }

  // 检查 token 有效性（用于页面加载或从其他标签页切换回来时）
  const checkAuth = async () => {
    const storedToken = localStorage.getItem('token')
    if (!storedToken) {
      // localStorage 中没有 token，清除 store 状态
      token.value = ''
      userInfo.value = null
      permissions.value = []
      return false
    }

    // 如果 store 中的 token 与 localStorage 不同步，更新它
    if (token.value !== storedToken) {
      token.value = storedToken
    }

    // 如果有 token 但没有用户信息，尝试获取
    if (!userInfo.value) {
      try {
        await getCurrentUser()
        return true
      } catch {
        return false
      }
    }

    return true
  }

  const setUserInfo = (info) => {
    userInfo.value = info
    if (info) {
      permissions.value = info.permissions || []
    }
  }

  const hasPermission = (permissionCode) => {
    // Root 用户拥有所有权限
    if (permissions.value.includes('*')) {
      return true
    }
    return permissions.value.includes(permissionCode)
  }

  const hasAnyPermission = (permissionCodes) => {
    if (permissions.value.includes('*')) {
      return true
    }
    return permissionCodes.some(code => permissions.value.includes(code))
  }

  const login = async (credentials) => {
    try {
      const res = await authApi.login(credentials)
      console.log('Login response:', res) // Debug log
      if (res.success) {
        // 后端返回的是 PascalCase: Token, UserInfo
        const token = res.data.Token || res.data.token
        const userInfo = res.data.UserInfo || res.data.userInfo
        setToken(token)
        setUserInfo(userInfo)
        ElMessage.success('登录成功')
        return true
      }
      ElMessage.error(res.message || '登录失败')
      return false
    } catch (error) {
      console.error('Login error:', error)
      ElMessage.error('登录失败')
      return false
    }
  }

  const logout = () => {
    // 使用 setToken 确保一致性
    setToken('')
    userInfo.value = null
    permissions.value = []
    console.log('User logged out, token cleared')
  }

  const getCurrentUser = async () => {
    try {
      const res = await authApi.getCurrentUser()
      if (res.success) {
        setUserInfo(res.data)
        return res.data
      }
      // API 返回失败，清除登录状态并抛出错误
      logout()
      throw new Error('获取用户信息失败')
    } catch (error) {
      // Token 可能过期，清除登录状态并重新抛出错误
      logout()
      throw error
    }
  }

  return {
    // 状态
    token,
    userInfo,
    permissions,
    // 计算属性
    isLoggedIn,
    username,
    realName,
    // 方法
    setToken,
    setUserInfo,
    hasPermission,
    hasAnyPermission,
    login,
    logout,
    getCurrentUser,
    checkAuth
  }
})
