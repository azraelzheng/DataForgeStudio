<template>
  <div class="backup-management">
    <el-row :gutter="20">
      <!-- 创建备份 -->
      <el-col :span="24">
        <el-card>
          <template #header>
            <span>创建备份</span>
          </template>
          <el-form :model="backupForm" ref="backupFormRef" :inline="true">
            <el-form-item label="备注">
              <el-input v-model="backupForm.description" placeholder="请输入备注（可选）" style="width: 300px;" />
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

      <!-- 备份计划 -->
      <el-col :span="24" style="margin-top: 20px;">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>备份计划</span>
              <el-button type="primary" @click="handleAddSchedule">
                <el-icon><Plus /></el-icon>
                新增计划
              </el-button>
            </div>
          </template>

          <el-table :data="schedules" v-loading="schedulesLoading" border stripe>
            <el-table-column prop="scheduleName" label="计划名称" width="150" />
            <el-table-column prop="scheduleType" label="类型" width="100">
              <template #default="{ row }">
                <el-tag :type="row.scheduleType === 'Recurring' ? 'primary' : 'warning'">
                  {{ row.scheduleType === 'Recurring' ? '重复' : '单次' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="执行时间" min-width="200">
              <template #default="{ row }">
                <span v-if="row.scheduleType === 'Recurring'">
                  {{ formatRecurringDays(row.recurringDays) }} {{ row.scheduledTime }}
                </span>
                <span v-else>
                  {{ formatOnceDate(row.onceDate) }}
                </span>
              </template>
            </el-table-column>
            <el-table-column prop="retentionCount" label="保留数量" width="100" />
            <el-table-column prop="nextRunTime" label="下次执行" width="180">
              <template #default="{ row }">
                {{ formatDateTime(row.nextRunTime) }}
              </template>
            </el-table-column>
            <el-table-column prop="isEnabled" label="状态" width="80">
              <template #default="{ row }">
                <el-switch v-model="row.isEnabled" @change="handleToggleSchedule(row)" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="120" fixed="right">
              <template #default="{ row }">
                <el-button type="primary" link size="small" @click="handleEditSchedule(row)">编辑</el-button>
                <el-button type="danger" link size="small" @click="handleDeleteSchedule(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
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

    <!-- 计划编辑对话框 -->
    <el-dialog v-model="scheduleDialogVisible" :title="editingSchedule ? '编辑计划' : '新增计划'" width="500px">
      <el-form :model="scheduleForm" label-width="100px">
        <el-form-item label="计划名称" required>
          <el-input v-model="scheduleForm.scheduleName" placeholder="请输入计划名称" />
        </el-form-item>
        <el-form-item label="计划类型" required>
          <el-radio-group v-model="scheduleForm.scheduleType">
            <el-radio value="Recurring">重复计划</el-radio>
            <el-radio value="Once">单次计划</el-radio>
          </el-radio-group>
        </el-form-item>

        <!-- 重复计划设置 -->
        <template v-if="scheduleForm.scheduleType === 'Recurring'">
          <el-form-item label="执行日期" required>
            <el-checkbox-group v-model="scheduleForm.recurringDays">
              <el-checkbox :value="0">周日</el-checkbox>
              <el-checkbox :value="1">周一</el-checkbox>
              <el-checkbox :value="2">周二</el-checkbox>
              <el-checkbox :value="3">周三</el-checkbox>
              <el-checkbox :value="4">周四</el-checkbox>
              <el-checkbox :value="5">周五</el-checkbox>
              <el-checkbox :value="6">周六</el-checkbox>
            </el-checkbox-group>
          </el-form-item>
          <el-form-item label="执行时间" required>
            <el-time-select
              v-model="scheduleForm.scheduledTime"
              start="00:00"
              end="23:59"
              step="00:30"
              placeholder="选择时间"
            />
          </el-form-item>
        </template>

        <!-- 单次计划设置 -->
        <template v-if="scheduleForm.scheduleType === 'Once'">
          <el-form-item label="执行时间" required>
            <el-date-picker
              v-model="scheduleForm.onceDate"
              type="datetime"
              placeholder="选择日期时间"
              value-format="YYYY-MM-DDTHH:mm:ss"
            />
          </el-form-item>
        </template>

        <el-form-item label="保留数量">
          <el-input-number v-model="scheduleForm.retentionCount" :min="1" :max="100" />
          <span style="margin-left: 10px; color: #909399;">个备份</span>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="scheduleDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSaveSchedule" :loading="savingSchedule">保存</el-button>
      </template>
    </el-dialog>
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
  description: ''
})

const searchForm = reactive({
  backupName: ''
})

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

// 备份计划相关
const schedules = ref([])
const schedulesLoading = ref(false)
const scheduleDialogVisible = ref(false)
const editingSchedule = ref(null)
const savingSchedule = ref(false)

const scheduleForm = reactive({
  scheduleName: '',
  scheduleType: 'Recurring',
  recurringDays: [1, 2, 3, 4, 5],
  scheduledTime: '02:00',
  onceDate: null,
  retentionCount: 10
})

onMounted(() => {
  loadData()
  loadSchedules()
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

const loadSchedules = async () => {
  schedulesLoading.value = true
  try {
    const res = await systemApi.getBackupSchedules()
    if (res.success) {
      schedules.value = res.data || []
    }
  } catch (error) {
    console.error('加载备份计划失败:', error)
  } finally {
    schedulesLoading.value = false
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

// 备份计划相关方法
const handleAddSchedule = () => {
  editingSchedule.value = null
  Object.assign(scheduleForm, {
    scheduleName: '',
    scheduleType: 'Recurring',
    recurringDays: [1, 2, 3, 4, 5],
    scheduledTime: '02:00',
    onceDate: null,
    retentionCount: 10
  })
  scheduleDialogVisible.value = true
}

const handleEditSchedule = (row) => {
  editingSchedule.value = row
  Object.assign(scheduleForm, {
    scheduleName: row.scheduleName,
    scheduleType: row.scheduleType,
    recurringDays: row.recurringDays || [],
    scheduledTime: row.scheduledTime,
    onceDate: row.onceDate,
    retentionCount: row.retentionCount
  })
  scheduleDialogVisible.value = true
}

const handleSaveSchedule = async () => {
  if (!scheduleForm.scheduleName) {
    ElMessage.warning('请输入计划名称')
    return
  }

  savingSchedule.value = true
  try {
    if (editingSchedule.value) {
      await systemApi.updateBackupSchedule(editingSchedule.value.scheduleId, scheduleForm)
      ElMessage.success('更新成功')
    } else {
      await systemApi.createBackupSchedule(scheduleForm)
      ElMessage.success('创建成功')
    }
    scheduleDialogVisible.value = false
    loadSchedules()
  } catch (error) {
    console.error('保存失败:', error)
  } finally {
    savingSchedule.value = false
  }
}

const handleToggleSchedule = async (row) => {
  try {
    await systemApi.toggleBackupSchedule(row.scheduleId)
    ElMessage.success(row.isEnabled ? '已启用' : '已禁用')
    loadSchedules()
  } catch (error) {
    console.error('切换状态失败:', error)
    row.isEnabled = !row.isEnabled
  }
}

const handleDeleteSchedule = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要删除计划"${row.scheduleName}"吗？`, '提示', {
      type: 'warning'
    })
    await systemApi.deleteBackupSchedule(row.scheduleId)
    ElMessage.success('删除成功')
    loadSchedules()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}

const formatRecurringDays = (days) => {
  if (!days || days.length === 0) return '-'
  const dayNames = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']
  return days.map(d => dayNames[d]).join('、')
}

const formatOnceDate = (date) => {
  if (!date) return '-'
  return new Date(date).toLocaleString('zh-CN')
}

const formatDateTime = (date) => {
  if (!date) return '-'
  return new Date(date).toLocaleString('zh-CN')
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
