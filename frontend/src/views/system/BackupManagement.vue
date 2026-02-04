<template>
  <div class="backup-management">
    <el-row :gutter="20">
      <!-- 创建备份 -->
      <el-col :span="24">
        <el-card>
          <template #header>
            <span>创建备份</span>
          </template>
          <el-form :model="backupForm" :rules="backupRules" ref="backupFormRef" :inline="true">
            <el-form-item label="备份名称" prop="backupName">
              <el-input v-model="backupForm.backupName" placeholder="请输入备份名称" style="width: 300px;" />
            </el-form-item>
            <el-form-item label="备注">
              <el-input v-model="backupForm.description" placeholder="请输入备注" style="width: 300px;" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleCreateBackup" :loading="creating">
                <el-icon><Plus /></el-icon>
                创建备份
              </el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-col>

      <!-- 备份列表 -->
      <el-col :span="24" style="margin-top: 20px;">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>备份列表</span>
              <el-button type="primary" link @click="loadData">
                <el-icon><Refresh /></el-icon>
                刷新
              </el-button>
            </div>
          </template>

          <!-- 搜索表单 -->
          <el-form :inline="true" :model="searchForm" class="search-form">
            <el-form-item label="备份名称">
              <el-input v-model="searchForm.backupName" placeholder="请输入备份名称" clearable />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleSearch">
                <el-icon><Search /></el-icon>
                查询
              </el-button>
              <el-button @click="handleReset">重置</el-button>
            </el-form-item>
          </el-form>

          <!-- 备份表格 -->
          <el-table :data="tableData" v-loading="loading" border stripe>
            <el-table-column prop="backupName" label="备份名称" width="200" />
            <el-table-column prop="fileName" label="文件名" min-width="200" />
            <el-table-column prop="fileSize" label="文件大小" width="120">
              <template #default="{ row }">
                {{ formatFileSize(row.fileSize) }}
              </template>
            </el-table-column>
            <el-table-column prop="description" label="备注" min-width="200" show-overflow-tooltip />
            <el-table-column prop="createdBy" label="创建人" width="120" />
            <el-table-column prop="createdTime" label="创建时间" width="180" />
            <el-table-column label="操作" width="180" fixed="right">
              <template #default="{ row }">
                <el-button type="warning" link size="small" @click="handleRestore(row)" :loading="row.restoring">
                  <el-icon><RefreshLeft /></el-icon>
                  恢复
                </el-button>
                <el-button type="primary" link size="small" @click="handleDownload(row)">
                  <el-icon><Download /></el-icon>
                  下载
                </el-button>
                <el-button type="danger" link size="small" @click="handleDelete(row)">
                  <el-icon><Delete /></el-icon>
                  删除
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <!-- 分页 -->
          <el-pagination
            v-model:current-page="pagination.page"
            v-model:page-size="pagination.pageSize"
            :page-sizes="[10, 20, 50, 100]"
            :total="pagination.total"
            layout="total, sizes, prev, pager, next, jumper"
            @size-change="loadData"
            @current-change="loadData"
            style="margin-top: 20px; justify-content: flex-end;"
          />
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { systemApi } from '../../api/request'

const backupFormRef = ref()
const loading = ref(false)
const creating = ref(false)

const tableData = ref([])

const backupForm = reactive({
  backupName: '',
  description: ''
})

const backupRules = {
  backupName: [{ required: true, message: '请输入备份名称', trigger: 'blur' }]
}

const searchForm = reactive({
  backupName: ''
})

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

onMounted(() => {
  loadData()
})

const loadData = async () => {
  loading.value = true
  try {
    const res = await systemApi.getBackups({
      page: pagination.page,
      pageSize: pagination.pageSize,
      ...searchForm
    })
    if (res.success) {
      const data = res.data
      tableData.value = data.Items || data.items || []
      pagination.total = data.TotalCount || data.total || 0
    }
  } catch (error) {
    console.error('加载数据失败:', error)
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  pagination.page = 1
  loadData()
}

const handleReset = () => {
  searchForm.backupName = ''
  handleSearch()
}

const handleCreateBackup = async () => {
  const valid = await backupFormRef.value.validate().catch(() => false)
  if (!valid) return

  creating.value = true
  try {
    // 生成默认备份名称
    if (!backupForm.backupName) {
      const now = new Date()
      backupForm.backupName = `backup_${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}_${String(now.getHours()).padStart(2, '0')}${String(now.getMinutes()).padStart(2, '0')}`
    }

    const res = await systemApi.createBackup(backupForm)
    if (res.success) {
      ElMessage.success('备份创建成功')
      backupForm.backupName = ''
      backupForm.description = ''
      loadData()
    }
  } catch (error) {
    console.error('创建备份失败:', error)
  } finally {
    creating.value = false
  }
}

const handleRestore = async (row) => {
  try {
    await ElMessageBox.confirm(
      `确定要恢复备份"${row.backupName}"吗？\n\n警告：此操作将覆盖当前数据库，请确保已做好数据备份！`,
      '恢复备份',
      {
        confirmButtonText: '确定恢复',
        cancelButtonText: '取消',
        type: 'warning',
        dangerouslyUseHTMLString: true
      }
    )

    row.restoring = true
    const res = await systemApi.restoreBackup(row.backupId)
    if (res.success) {
      ElMessage.success('备份恢复成功，系统将在3秒后刷新页面')
      setTimeout(() => {
        window.location.reload()
      }, 3000)
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('恢复备份失败:', error)
    }
  } finally {
    row.restoring = false
  }
}

const handleDownload = async (row) => {
  try {
    // TODO: 实现下载功能
    ElMessage.info('下载功能待实现')
  } catch (error) {
    console.error('下载失败:', error)
  }
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要删除备份"${row.backupName}"吗？`, '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await systemApi.deleteBackup(row.backupId)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}

const formatFileSize = (bytes) => {
  if (!bytes) return '-'
  const units = ['B', 'KB', 'MB', 'GB']
  let size = bytes
  let unitIndex = 0

  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024
    unitIndex++
  }

  return `${size.toFixed(2)} ${units[unitIndex]}`
}
</script>

<style scoped>
.backup-management {
  height: 100%;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
}
</style>
