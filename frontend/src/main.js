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
    if (errors.length > 20) {
      errors.shift()
    }
    localStorage.setItem('app_errors', JSON.stringify(errors))
  } catch {
    // 存储错误失败时忽略
  }
}

// 全局未捕获的 Promise 错误处理
window.addEventListener('unhandledrejection', (event) => {
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
  } catch {
    // 存储错误失败时忽略
  }

  event.preventDefault()
})

// 全局未捕获的错误处理
window.addEventListener('error', (event) => {
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
  } catch {
    // 存储错误失败时忽略
  }
})

app.mount('#app')
