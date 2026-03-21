import axios from 'axios'

// 从 DataForge 主应用获取 userStore
const getUserStore = () => {
  try {
    // 动态导入避免循环依赖
    return require('@/stores/user').useUserStore()
  } catch {
    return null
  }
}

// 从 DataForge 主应用获取 router
const getRouter = () => {
  try {
    return require('@/router').default || require('@/router').router
  } catch {
    return null
  }
}

const service = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api',
  timeout: 30000
})

// 请求拦截器：添加 DataForge JWT Token
service.interceptors.request.use(
  (config) => {
    const userStore = getUserStore()
    if (userStore && userStore.token) {
      config.headers.Authorization = `Bearer ${userStore.token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// 响应拦截器：处理 401 跳转登录页
service.interceptors.response.use(
  (response) => {
    return response
  },
  (error) => {
    if (error.response?.status === 401) {
      // 清除用户信息
      const userStore = getUserStore()
      if (userStore) {
        userStore.logout()
      }
      // 跳转到 DataForge 登录页
      const router = getRouter()
      if (router) {
        router.push('/login')
      } else {
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  }
)

export default service
