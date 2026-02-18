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
  hasError.value = true
  errorMessage.value = error.message || '未知错误'
  errorStack.value = error.stack || info

  ElMessage.error({
    message: '页面发生错误，请刷新或返回首页',
    duration: 5000
  })

  reportError(error, info)

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
    const errorData = {
      message: error.message,
      stack: error.stack,
      componentInfo: info,
      url: window.location.href,
      userAgent: navigator.userAgent,
      timestamp: new Date().toISOString()
    }

    const errors = JSON.parse(localStorage.getItem('app_errors') || '[]')
    errors.push(errorData)
    if (errors.length > 10) {
      errors.shift()
    }
    localStorage.setItem('app_errors', JSON.stringify(errors))
  } catch {
    // 忽略存储错误
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
