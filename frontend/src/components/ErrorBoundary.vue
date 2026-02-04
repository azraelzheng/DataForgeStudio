<template>
  <div class="error-boundary">
    <slot v-if="!hasError"></slot>
    <div v-else class="error-container">
      <el-result icon="error" title="页面出错了" :sub-title="errorMessage">
        <template #extra>
          <el-button type="primary" @click="goHome">返回首页</el-button>
          <el-button @click="reload">刷新页面</el-button>
        </template>
      </el-result>
      <el-collapse v-if=" showErrorDetails" class="error-details">
        <el-collapse-item title="错误详情" name="details">
          <pre class="error-stack">{{ errorStack }}</pre>
        </el-collapse-item>
      </el-collapse>
    </div>
  </div>
</template>

<script setup>
import { ref, onErrorCaptured } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'

const router = useRouter()
const hasError = ref(false)
const errorMessage = ref('')
const errorStack = ref('')
const showErrorDetails = ref(import.meta.env.DEV) // 只在开发环境显示错误详情

onErrorCaptured((error, instance, info) => {
  console.error('ErrorBoundary caught an error:', error, info)

  hasError.value = true
  errorMessage.value = error.message || '未知错误'
  errorStack.value = error.stack || info

  // 显示错误提示
  ElMessage.error({
    message: '页面发生错误，请刷新或返回首页',
    duration: 5000
  })

  // 上报错误到服务器（可选）
  reportError(error, info)

  // 返回 false 阻止错误继续传播
  return false
})

const goHome = () => {
  hasError.value = false
  router.push('/home')
}

const reload = () => {
  hasError.value = false
  window.location.reload()
}

const reportError = async (error, info) => {
  try {
    // 这里可以添加错误上报逻辑
    // 例如发送到服务器或第三方错误追踪服务
    const errorData = {
      message: error.message,
      stack: error.stack,
      componentInfo: info,
      url: window.location.href,
      userAgent: navigator.userAgent,
      timestamp: new Date().toISOString()
    }

    // 存储到 localStorage 用于调试
    const errors = JSON.parse(localStorage.getItem('app_errors') || '[]')
    errors.push(errorData)
    // 只保留最近 10 条错误
    if (errors.length > 10) {
      errors.shift()
    }
    localStorage.setItem('app_errors', JSON.stringify(errors))
  } catch (e) {
    console.error('Failed to report error:', e)
  }
}
</script>

<style scoped>
.error-boundary {
  min-height: 100%;
}

.error-container {
  padding: 40px 20px;
}

.error-details {
  max-width: 800px;
  margin: 20px auto;
}

.error-stack {
  background-color: #f5f5f5;
  padding: 15px;
  border-radius: 4px;
  overflow-x: auto;
  font-size: 12px;
  line-height: 1.5;
  color: #666;
  max-height: 300px;
  overflow-y: auto;
}
</style>
