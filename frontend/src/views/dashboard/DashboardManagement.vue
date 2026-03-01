<template>
  <div class="dashboard-management">
    <div class="page-header">
      <h1>看板管理</h1>
      <el-button type="primary" :icon="Plus" @click="showCreateDialog">
        新建看板
      </el-button>
    </div>

    <!-- 看板列表 -->
    <el-card>
      <el-table
        v-loading="isLoading"
        :data="dashboards"
        stripe
        style="width: 100%"
      >
        <el-table-column prop="name" label="看板名称" width="200" />
        <el-table-column prop="description" label="描述" />
        <el-table-column prop="category" label="分类" width="120" />
        <el-table-column label="组件数量" width="100" align="center">
          <template #default="{ row }">
            {{ row.widgets?.length || 0 }}
          </template>
        </el-table-column>
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isPublished ? 'success' : 'info'" size="small">
              {{ row.isPublished ? '已发布' : '草稿' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdTime" label="创建时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createdTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button
              type="primary"
              size="small"
              link
              :icon="Edit"
              @click="editDashboard(row)"
            >
              编辑
            </el-button>
            <el-button
              type="danger"
              size="small"
              link
              :icon="Delete"
              @click="deleteDashboard(row.id)"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 创建/编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑看板' : '新建看板'"
      width="600px"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="看板名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入看板名称" />
        </el-form-item>

        <el-form-item label="描述">
          <el-input
            v-model="formData.description"
            type="textarea"
            :rows="2"
            placeholder="请输入描述"
          />
        </el-form-item>

        <el-form-item label="分类">
          <el-input v-model="formData.category" placeholder="请输入分类" />
        </el-form-item>

        <el-form-item label="刷新间隔">
          <el-input-number
            v-model="formData.refreshInterval"
            :min="10"
            :max="3600"
            :step="10"
          />
          <span class="unit">秒</span>
        </el-form-item>

        <el-form-item label="发布状态">
          <el-switch v-model="formData.isPublished" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="isSubmitting" @click="submitForm">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Edit, Delete } from '@element-plus/icons-vue'

// 状态
const dashboards = ref([])
const isLoading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const isSubmitting = ref(false)
const editingId = ref<string | null>(null)

// 表单
const formRef = ref<FormInstance | null>(null)
const formData = ref({
  name: '',
  description: '',
  category: '',
  refreshInterval: 60,
  isPublished: false
})

const formRules: FormRules = {
  name: [
    { required: true, message: '请输入看板名称', trigger: 'blur' }
  ]
}

/**
 * 加载看板列表
 */
async function loadDashboards() {
  isLoading.value = true

  try {
    const response = await fetch('/api/dashboard', {
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`)
    }

    const data = await response.json()

    if (data.success) {
      dashboards.value = data.data || []
    } else {
      ElMessage.error(data.message || '加载看板列表失败')
    }
  } catch (error) {
    console.error('[DashboardManagement] 加载看板列表失败:', error)
    ElMessage.error('加载看板列表失败')
  } finally {
    isLoading.value = false
  }
}

/**
 * 显示创建对话框
 */
function showCreateDialog() {
  isEdit.value = false
  editingId.value = null
  formData.value = {
    name: '',
    description: '',
    category: '',
    refreshInterval: 60,
    isPublished: false
  }
  dialogVisible.value = true
}

/**
 * 编辑看板
 */
function editDashboard(dashboard) {
  isEdit.value = true
  editingId.value = dashboard.id
  formData.value = {
    name: dashboard.name,
    description: dashboard.description,
    category: dashboard.category,
    refreshInterval: dashboard.refreshInterval,
    isPublished: dashboard.isPublished
  }
  dialogVisible.value = true
}

/**
 * 提交表单
 */
async function submitForm() {
  if (!formRef.value) {
    return
  }

  await formRef.value.validate(async (valid) => {
    if (!valid) {
      return
    }

    isSubmitting.value = true

    try {
      const url = isEdit.value
        ? `/api/dashboard/${editingId.value}`
        : '/api/dashboard'

      const method = isEdit.value ? 'PUT' : 'POST'

      const response = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(formData.value)
      })

      const data = await response.json()

      if (data.success) {
        ElMessage.success(isEdit.value ? '更新成功' : '创建成功')
        dialogVisible.value = false
        await loadDashboards()
      } else {
        ElMessage.error(data.message || '操作失败')
      }
    } catch (error) {
      console.error('[DashboardManagement] 提交表单失败:', error)
      ElMessage.error('操作失败')
    } finally {
      isSubmitting.value = false
    }
  })
}

/**
 * 删除看板
 */
async function deleteDashboard(id) {
  try {
    await ElMessageBox.confirm('确定要删除该看板吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    const response = await fetch(`/api/dashboard/${id}`, {
      method: 'DELETE'
    })

    const data = await response.json()

    if (data.success) {
      ElMessage.success('删除成功')
      await loadDashboards()
    } else {
      ElMessage.error(data.message || '删除失败')
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('[DashboardManagement] 删除看板失败:', error)
      ElMessage.error('删除失败')
    }
  }
}

/**
 * 格式化日期
 */
function formatDate(date) {
  const d = typeof date === 'string' ? new Date(date) : date
  return d.toLocaleString('zh-CN')
}

// 组件挂载
onMounted(() => {
  loadDashboards()
})
</script>

<style scoped>
.dashboard-management {
  padding: 2rem;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.page-header h1 {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 600;
}

.unit {
  margin-left: 0.5rem;
  color: var(--el-text-color-secondary);
}
</style>
