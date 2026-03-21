<template>
  <div class="daping-public-container">
    <PreviewIndex v-if="projectData" :project-data="projectData" />
    <div v-else-if="loading" class="loading-state">
      <el-icon class="is-loading" :size="40"><Loading /></el-icon>
      <p>加载中...</p>
    </div>
    <div v-else-if="error" class="error-state">
      <el-icon :size="40"><WarningFilled /></el-icon>
      <p>{{ error }}</p>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { Loading, WarningFilled } from '@element-plus/icons-vue'
import PreviewIndex from '@/daping/views/preview/index.vue'
import request from '@/api/request'

const route = useRoute()
const projectData = ref(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  const publicUrl = route.params.publicUrl
  if (!publicUrl) {
    error.value = '无效的访问链接'
    loading.value = false
    return
  }

  try {
    const res = await request.get(`/api/public/daping/${publicUrl}`)
    if (res.success && res.data) {
      // 解析项目配置
      projectData.value = JSON.parse(res.data.content || '{}')
    } else {
      error.value = res.message || '大屏不存在或未发布'
    }
  } catch (e) {
    error.value = '加载失败，请稍后重试'
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.daping-public-container {
  width: 100vw;
  height: 100vh;
  overflow: hidden;
}

.loading-state,
.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100vh;
  background: #1a1a1a;
  color: #fff;
}

.error-state {
  color: #f56c6c;
}
</style>
