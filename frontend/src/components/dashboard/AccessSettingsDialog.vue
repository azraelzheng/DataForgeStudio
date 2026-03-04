<template>
  <el-dialog
    v-model="visible"
    title="访问设置"
    width="500px"
    :close-on-click-modal="false"
    @open="handleDialogOpen"
  >
    <el-form :model="form" label-width="100px">
      <!-- 公开访问开关 -->
      <el-form-item label="公开访问">
        <div class="access-switch">
          <el-switch v-model="form.isPublic" />
          <span class="hint">{{ form.isPublic ? '任何人都可以访问此大屏' : '只有授权用户可以访问' }}</span>
        </div>
      </el-form-item>

      <!-- 公开链接（isPublic=true时显示） -->
      <el-form-item v-if="form.isPublic && publicUrl" label="访问链接">
        <el-input :model-value="fullPublicUrl" readonly>
          <template #append>
            <el-button @click="handleCopyLink">
              <el-icon><DocumentCopy /></el-icon>
              复制
            </el-button>
          </template>
        </el-input>
      </el-form-item>

      <!-- 授权用户（isPublic=false时显示） -->
      <el-form-item v-if="!form.isPublic" label="授权用户">
        <el-select
          v-model="form.authorizedUserIds"
          multiple
          filterable
          placeholder="请选择授权用户"
          style="width: 100%"
          :loading="userLoading"
        >
          <el-option
            v-for="user in userList"
            :key="user.userId"
            :label="user.username"
            :value="user.userId"
          />
        </el-select>
        <div class="hint">未选择任何用户时，仅创建者可访问</div>
      </el-form-item>

      <!-- 状态显示 -->
      <el-form-item label="发布状态">
        <el-tag :type="statusTagType">{{ statusLabel }}</el-tag>
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="visible = false">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="loading">保存</el-button>
    </template>
  </el-dialog>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { DocumentCopy } from '@element-plus/icons-vue'
import { getDashboardAccess, updateDashboardAccess } from '../../api/dashboard'
import { userApi } from '../../api/request'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  dashboardId: { type: [Number, String], required: true }
})

const emit = defineEmits(['update:modelValue', 'success'])

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

// 表单数据
const form = reactive({
  isPublic: false,
  authorizedUserIds: []
})

// 公开访问URL
const publicUrl = ref('')

// 发布状态
const status = ref('draft')

// 用户列表
const userList = ref([])

// 加载状态
const loading = ref(false)
const userLoading = ref(false)

// 计算完整公开URL
const fullPublicUrl = computed(() => {
  if (!publicUrl.value) return ''
  return `${window.location.origin}/public/d/${publicUrl.value}`
})

// 状态标签类型
const statusTagType = computed(() => {
  if (status.value === 'draft') return 'info'
  if (form.isPublic) return 'success'
  return 'primary'
})

// 状态标签文字
const statusLabel = computed(() => {
  if (status.value === 'draft') return '草稿'
  if (form.isPublic) return '已发布 - 公开'
  return '已发布 - 授权访问'
})

// 加载用户列表
const loadUserList = async () => {
  userLoading.value = true
  try {
    // 获取所有用户（设置较大的pageSize以获取全部用户）
    const res = await userApi.getUsers({ page: 1, pageSize: 1000 })
    if (res.success && res.data) {
      // 过滤掉系统用户（root用户）
      userList.value = (res.data.items || res.data || []).filter(
        user => !user.isSystem && user.username !== 'root'
      )
    }
  } catch {
    console.error('获取用户列表失败')
  } finally {
    userLoading.value = false
  }
}

// 加载访问设置
const loadAccessSettings = async () => {
  if (!props.dashboardId) return

  loading.value = true
  try {
    const res = await getDashboardAccess(props.dashboardId)
    if (res.success && res.data) {
      form.isPublic = res.data.isPublic || false
      form.authorizedUserIds = res.data.authorizedUserIds || []
      publicUrl.value = res.data.publicUrl || ''
      status.value = res.data.status || 'draft'
    }
  } catch {
    ElMessage.error('获取访问设置失败')
  } finally {
    loading.value = false
  }
}

// 对话框打开时加载数据
const handleDialogOpen = () => {
  loadAccessSettings()
  loadUserList()
}

// 复制链接
const handleCopyLink = async () => {
  try {
    await navigator.clipboard.writeText(fullPublicUrl.value)
    ElMessage.success('链接已复制到剪贴板')
  } catch {
    // 降级方案：使用传统复制方式
    try {
      const textArea = document.createElement('textarea')
      textArea.value = fullPublicUrl.value
      textArea.style.position = 'fixed'
      textArea.style.left = '-9999px'
      document.body.appendChild(textArea)
      textArea.select()
      document.execCommand('copy')
      document.body.removeChild(textArea)
      ElMessage.success('链接已复制到剪贴板')
    } catch {
      ElMessage.error('复制失败，请手动复制')
    }
  }
}

// 提交保存
const handleSubmit = async () => {
  loading.value = true
  try {
    const data = {
      isPublic: form.isPublic,
      authorizedUserIds: form.isPublic ? [] : form.authorizedUserIds
    }

    const res = await updateDashboardAccess(props.dashboardId, data)
    if (res.success) {
      ElMessage.success('保存成功')
      visible.value = false
      // 更新本地状态
      if (res.data) {
        publicUrl.value = res.data.publicUrl || publicUrl.value
        status.value = res.data.status || status.value
      }
      emit('success', res.data)
    } else {
      ElMessage.error(res.message || '保存失败')
    }
  } catch {
    ElMessage.error('保存失败')
  } finally {
    loading.value = false
  }
}

// 监听dashboardId变化，重新加载数据
watch(() => props.dashboardId, (newId) => {
  if (newId && visible.value) {
    loadAccessSettings()
  }
})
</script>

<style scoped>
.access-switch {
  display: flex;
  align-items: center;
  gap: 12px;
}

.hint {
  color: #909399;
  font-size: 12px;
  margin-top: 4px;
}

.access-switch .hint {
  margin-top: 0;
}

:deep(.el-input-group__append) {
  padding: 0;
}

:deep(.el-input-group__append .el-button) {
  margin: 0;
  border: none;
  background: transparent;
}
</style>
