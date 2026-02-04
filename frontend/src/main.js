import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import zhCn from 'element-plus/dist/locale/zh-cn.mjs'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import 'element-plus/dist/index.css'

import App from './App.vue'
import router from './router'
import './assets/css/main.css'

const app = createApp(App)

// 注册所有 Element Plus 图标
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}

app.use(createPinia())
app.use(router)
app.use(ElementPlus, {
  locale: zhCn,
})

// 全局错误处理
app.config.errorHandler = (err, instance, info) => {
  console.error('全局错误:', err, info)

  // 存储错误信息到 localStorage 用于调试
  try {
    const errorData = {
      message: err.message,
      stack: err.stack,
      componentInfo: info,
      url: window.location.href,
      timestamp: new Date().toISOString()
    }

    const errors = JSON.parse(localStorage.getItem('app_errors') || '[]')
    errors.push(errorData)
    // 只保留最近 20 条错误
    if (errors.length > 20) {
      errors.shift()
    }
    localStorage.setItem('app_errors', JSON.stringify(errors))
  } catch (e) {
    console.error('Failed to store error:', e)
  }

  // 在开发环境下显示详细错误信息
  if (import.meta.env.DEV) {
    console.error('错误详情:', {
      error: err,
      instance,
      info
    })
  }
}

// 全局未捕获的 Promise 错误处理
window.addEventListener('unhandledrejection', (event) => {
  console.error('未处理的 Promise 错误:', event.reason)

  // 存储错误信息
  try {
    const errorData = {
      message: event.reason?.message || String(event.reason),
      stack: event.reason?.stack,
      type: 'unhandledrejection',
      url: window.location.href,
      timestamp: new Date().toISOString()
    }

    const errors = JSON.parse(localStorage.getItem('app_errors') || '[]')
    errors.push(errorData)
    if (errors.length > 20) {
      errors.shift()
    }
    localStorage.setItem('app_errors', JSON.stringify(errors))
  } catch (e) {
    console.error('Failed to store error:', e)
  }

  // 阻止默认的控制台错误输出
  event.preventDefault()
})

// 全局未捕获的错误处理
window.addEventListener('error', (event) => {
  console.error('未捕获的错误:', event.error)

  // 存储错误信息
  try {
    const errorData = {
      message: event.message,
      filename: event.filename,
      lineno: event.lineno,
      colno: event.colno,
      stack: event.error?.stack,
      type: 'error',
      url: window.location.href,
      timestamp: new Date().toISOString()
    }

    const errors = JSON.parse(localStorage.getItem('app_errors') || '[]')
    errors.push(errorData)
    if (errors.length > 20) {
      errors.shift()
    }
    localStorage.setItem('app_errors', JSON.stringify(errors))
  } catch (e) {
    console.error('Failed to store error:', e)
  }
})

app.mount('#app')
