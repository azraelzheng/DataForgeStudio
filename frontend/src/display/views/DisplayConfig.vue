<template>
  <div class="display-config-page">
    <div class="page-header">
      <h1>车间大屏管理</h1>
      <el-button type="primary" :icon="Plus" @click="showCreateDialog">
        新建大屏配置
      </el-button>
    </div>

    <!-- 配置列表 -->
    <el-card>
      <el-table
        v-loading="isLoading"
        :data="configs"
        stripe
        style="width: 100%"
      >
        <el-table-column prop="name" label="配置名称" width="200" />
        <el-table-column prop="description" label="描述" />
        <el-table-column label="看板数量" width="100" align="center">
          <template #default="{ row }">
            {{ row.dashboardIds?.length || 0 }}
          </template>
        </el-table-column>
        <el-table-column label="轮播间隔" width="100" align="center">
          <template #default="{ row }">
            {{ row.interval }}秒
          </template>
        </el-table-column>
        <el-table-column label="刷新间隔" width="100" align="center">
          <template #default="{ row }">
            {{ row.autoRefresh }}秒
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
              :icon="VideoPlay"
              @click="startDisplay(row.id)"
            >
              播放
            </el-button>
            <el-button
              type="primary"
              size="small"
              link
              :icon="Edit"
              @click="editConfig(row)"
            >
              编辑
            </el-button>
            <el-button
              type="danger"
              size="small"
              link
              :icon="Delete"
              @click="deleteConfig(row.id)"
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
      :title="isEdit ? '编辑大屏配置' : '新建大屏配置'"
      width="600px"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="120px"
      >
        <el-form-item label="配置名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入配置名称" />
        </el-form-item>

        <el-form-item label="描述">
          <el-input
            v-model="formData.description"
            type="textarea"
            :rows="2"
            placeholder="请输入描述"
          />
        </el-form-item>

        <el-form-item label="选择看板" prop="dashboardIds">
          <el-select
            v-model="formData.dashboardIds"
            multiple
            placeholder="请选择要展示的看板"
            style="width: 100%"
          >
            <el-option
              v-for="dashboard in availableDashboards"
              :key="dashboard.id"
              :label="dashboard.name"
              :value="dashboard.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="轮播间隔">
          <el-input-number
            v-model="formData.interval"
            :min="5"
            :max="300"
            :step="5"
          />
          <span class="unit">秒</span>
        </el-form-item>

        <el-form-item label="数据刷新">
          <el-input-number
            v-model="formData.autoRefresh"
            :min="10"
            :max="600"
            :step="10"
          />
          <span class="unit">秒</span>
        </el-form-item>

        <el-form-item label="转场效果">
          <el-radio-group v-model="formData.transition">
            <el-radio-button label="fade">淡入淡出</el-radio-button>
            <el-radio-button label="slide">滑动</el-radio-button>
            <el-radio-button label="none">无</el-radio-button>
          </el-radio-group>
        </el-form-item>

        <el-form-item label="显示选项">
          <el-checkbox v-model="formData.showClock">显示时钟</el-checkbox>
          <el-checkbox v-model="formData.showDashboardName">显示看板名称</el-checkbox>
        </el-form-item>

        <el-form-item label="播放选项">
          <el-checkbox v-model="formData.loop">循环播放</el-checkbox>
          <el-checkbox v-model="formData.pauseOnHover">悬停暂停</el-checkbox>
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
import { ref, onMounted, type Ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import {
  Plus,
  VideoPlay,
  Edit,
  Delete
} from '@element-plus/icons-vue'
import type { DisplayConfig, DisplayConfigCreateRequest, DisplayConfigUpdateRequest } from '../types/display'

const router = useRouter()

// 状态
const configs = ref<DisplayConfig[]>([])
const availableDashboards = ref<Array<{ id: string; name: string }>>([])
const isLoading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const isSubmitting = ref(false)
const editingId = ref<string | null>(null)

// 表单
const formRef = ref<FormInstance | null>(null)
const formData = ref<DisplayConfigCreateRequest>({
  name: '',
  description: '',
  dashboardIds: [],
  interval: 30,
  autoRefresh: 60,
  transition: 'fade',
  showClock: true,
  showDashboardName: true,
  loop: true,
  pauseOnHover: true
})

const formRules: FormRules = {
  name: [
    { required: true, message: '请输入配置名称', trigger: 'blur' }
  ],
  dashboardIds: [
    { required: true, message: '请至少选择一个看板', trigger: 'change' }
  ]
}

/**
 * 加载配置列表
 */
async function loadConfigs(): Promise<void> {
  isLoading.value = true

  try {
    const response = await fetch('/api/display', {
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`)
    }

    const data = await response.json()

    if (data.success) {
      configs.value = data.data.items || data.data || []
    } else {
      ElMessage.error(data.message || '加载配置列表失败')
    }
  } catch (error) {
    console.error('[DisplayConfig] 加载配置列表失败:', error)
    ElMessage.error('加载配置列表失败')
  } finally {
    isLoading.value = false
  }
}

/**
 * 加载可用看板列表
 */
async function loadDashboards(): Promise<void> {
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
      availableDashboards.value = data.data.items || data.data || []
    }
  } catch (error) {
    console.error('[DisplayConfig] 加载看板列表失败:', error)
  }
}

/**
 * 显示创建对话框
 */
function showCreateDialog(): void {
  isEdit.value = false
  editingId.value = null
  formData.value = {
    name: '',
    description: '',
    dashboardIds: [],
    interval: 30,
    autoRefresh: 60,
    transition: 'fade',
    showClock: true,
    showDashboardName: true,
    loop: true,
    pauseOnHover: true
  }
  dialogVisible.value = true
}

/**
 * 编辑配置
 */
function editConfig(config: DisplayConfig): void {
  isEdit.value = true
  editingId.value = config.id
  formData.value = {
    name: config.name,
    description: config.description,
    dashboardIds: [...config.dashboardIds],
    interval: config.interval,
    autoRefresh: config.autoRefresh,
    transition: config.transition,
    showClock: config.showClock,
    showDashboardName: config.showDashboardName,
    loop: config.loop,
    pauseOnHover: config.pauseOnHover
  }
  dialogVisible.value = true
}

/**
 * 提交表单
 */
async function submitForm(): Promise<void> {
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
        ? `/api/display/${editingId.value}`
        : '/api/display'

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
        await loadConfigs()
      } else {
        ElMessage.error(data.message || '操作失败')
      }
    } catch (error) {
      console.error('[DisplayConfig] 提交表单失败:', error)
      ElMessage.error('操作失败')
    } finally {
      isSubmitting.value = false
    }
  })
}

/**
 * 删除配置
 */
async function deleteConfig(id: string): Promise<void> {
  try {
    await ElMessageBox.confirm('确定要删除该配置吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    const response = await fetch(`/api/display/${id}`, {
      method: 'DELETE'
    })

    const data = await response.json()

    if (data.success) {
      ElMessage.success('删除成功')
      await loadConfigs()
    } else {
      ElMessage.error(data.message || '删除失败')
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('[DisplayConfig] 删除配置失败:', error)
      ElMessage.error('删除失败')
    }
  }
}

/**
 * 开始播放大屏
 */
function startDisplay(id: string): void {
  router.push({
    name: 'FullscreenView',
    query: { configId: id }
  })
}

/**
 * 格式化日期
 */
function formatDate(date: Date | string): string {
  const d = typeof date === 'string' ? new Date(date) : date
  return d.toLocaleString('zh-CN')
}

// 组件挂载
onMounted(() => {
  loadConfigs()
  loadDashboards()
})
</script>

<style scoped>
.display-config-page {
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
