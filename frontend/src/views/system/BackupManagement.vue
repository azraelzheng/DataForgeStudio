<template>
  <div class="backup-management">
    <!-- 顶部工具栏：创建备份 + 快捷操作 -->
    <div class="toolbar">
      <div class="toolbar-left">
        <el-input
          v-model="backupForm.description"
          placeholder="备份备注（可选）"
          style="width: 200px;"
          clearable
        />
        <DirectorySelector
          v-model="backupForm.backupPath"
          placeholder="选择备份路径"
          style="width: 280px;"
        />
        <el-button type="primary" @click="handleCreateBackup" :loading="creating">
          <el-icon><Plus /></el-icon>
          创建备份
        </el-button>
      </div>
      <div class="toolbar-right">
        <el-button @click="loadData; loadSchedules()">
          <el-icon><Refresh /></el-icon>
          刷新全部
        </el-button>
      </div>
    </div>

    <!-- 主内容区：左右布局 -->
    <div class="main-content">
      <!-- 左侧：备份计划 -->
      <div class="schedule-panel">
        <el-card class="panel-card">
          <template #header>
            <div class="card-header">
              <span>备份计划</span>
              <el-button type="primary" size="small" @click="handleAddSchedule">
                <el-icon><Plus /></el-icon>
                新增
              </el-button>
            </div>
          </template>

          <div class="table-wrapper" ref="scheduleTableWrapper">
            <el-table
              :data="schedules"
              v-loading="schedulesLoading"
              border
              stripe
              :height="scheduleTableHeight"
              size="small"
            >
              <el-table-column prop="scheduleName" label="计划名称" min-width="100" />
              <el-table-column prop="scheduleType" label="类型" width="70">
                <template #default="{ row }">
                  <el-tag :type="row.scheduleType === 'Recurring' ? 'primary' : 'warning'" size="small">
                    {{ row.scheduleType === 'Recurring' ? '重复' : '单次' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="执行时间" min-width="120">
                <template #default="{ row }">
                  <span v-if="row.scheduleType === 'Recurring'" class="schedule-time">
                    {{ formatRecurringDays(row.recurringDays) }} {{ row.scheduledTime }}
                  </span>
                  <span v-else class="schedule-time">
                    {{ formatOnceDate(row.onceDate) }}
                  </span>
                </template>
              </el-table-column>
              <el-table-column prop="nextRunTime" label="下次执行" width="130">
                <template #default="{ row }">
                  {{ formatDateTime(row.nextRunTime) }}
                </template>
              </el-table-column>
              <el-table-column prop="isEnabled" label="状态" width="60" align="center">
                <template #default="{ row }">
                  <el-switch v-model="row.isEnabled" size="small" @change="handleToggleSchedule(row)" />
                </template>
              </el-table-column>
              <el-table-column label="操作" width="80" fixed="right">
                <template #default="{ row }">
                  <el-button type="primary" link size="small" @click="handleEditSchedule(row)">编辑</el-button>
                  <el-button type="danger" link size="small" @click="handleDeleteSchedule(row)">删除</el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-card>
      </div>

      <!-- 右侧：备份列表 -->
      <div class="backup-panel">
        <el-card class="panel-card">
          <template #header>
            <div class="card-header">
              <span>备份列表</span>
              <div class="header-actions">
                <el-input
                  v-model="searchForm.backupName"
                  placeholder="搜索备份名称"
                  clearable
                  style="width: 180px;"
                  @keyup.enter="handleSearch"
                />
                <el-button type="primary" size="small" @click="handleSearch">
                  <el-icon><Search /></el-icon>
                </el-button>
              </div>
            </div>
          </template>

          <div class="table-wrapper" ref="backupTableWrapper">
            <el-table
              :data="tableData"
              v-loading="loading"
              border
              stripe
              :height="backupTableHeight"
            >
              <el-table-column prop="backupName" label="备份名称" min-width="160" />
              <el-table-column prop="fileName" label="文件名" min-width="180" show-overflow-tooltip />
              <el-table-column prop="fileSize" label="大小" width="90">
                <template #default="{ row }">
                  {{ formatFileSize(row.fileSize) }}
                </template>
              </el-table-column>
              <el-table-column prop="description" label="备注" min-width="150" show-overflow-tooltip />
              <el-table-column prop="createdBy" label="创建人" width="90" />
              <el-table-column prop="createdTime" label="创建时间" width="150" />
              <el-table-column label="操作" width="160" fixed="right">
                <template #default="{ row }">
                  <el-button type="warning" link size="small" @click="handleRestore(row)" :loading="row.restoring">
                    恢复
                  </el-button>
                  <el-button type="primary" link size="small" @click="handleDownload(row)">
                    下载
                  </el-button>
                  <el-button type="danger" link size="small" @click="handleDelete(row)">
                    删除
                  </el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>

          <!-- 分页 -->
          <el-pagination
            v-model:current-page="pagination.page"
            v-model:page-size="pagination.pageSize"
            :page-sizes="[10, 20, 50, 100]"
            :total="pagination.total"
            layout="total, sizes, prev, pager, next"
            @size-change="loadData"
            @current-change="loadData"
            class="pagination"
          />
        </el-card>
      </div>
    </div>

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
        <el-form-item label="备份路径" required>
          <DirectorySelector
            v-model="scheduleForm.backupPath"
            placeholder="选择备份路径"
          />
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
import { ref, reactive, onMounted, onUnmounted, nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { QuestionFilled } from '@element-plus/icons-vue'
import { systemApi } from '../../api/request'
import DirectorySelector from '../../components/DirectorySelector.vue'

// 动态表格高度相关
const scheduleTableWrapper = ref(null)
const scheduleTableHeight = ref(null)
const backupTableWrapper = ref(null)
const backupTableHeight = ref(null)

const loading = ref(false)
const creating = ref(false)

const tableData = ref([])

const backupForm = reactive({
  description: '',
  backupPath: ''
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
  retentionCount: 10,
  backupPath: ''
})

// 更新表格高度
const updateTableHeights = () => {
  nextTick(() => {
    if (scheduleTableWrapper.value) {
      scheduleTableHeight.value = scheduleTableWrapper.value.clientHeight
    }
    if (backupTableWrapper.value) {
      backupTableHeight.value = backupTableWrapper.value.clientHeight
    }
  })
}

onMounted(() => {
  loadData()
  loadSchedules()
  nextTick(updateTableHeights)
  window.addEventListener('resize', updateTableHeights)
})

onUnmounted(() => {
  window.removeEventListener('resize', updateTableHeights)
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
      // 去重：确保不会有重复的备份记录
      const items = data.Items || data.items || []
      const uniqueItems = items.filter((item, index, self) =>
        index === self.findIndex(t => t.backupId === item.backupId)
      )
      tableData.value = uniqueItems
      pagination.total = data.TotalCount || data.total || 0
    }
  } catch {
    // 加载失败
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
  } catch {
    // 加载失败
  } finally {
    schedulesLoading.value = false
  }
}

const handleSearch = () => {
  pagination.page = 1
  loadData()
}

const handleCreateBackup = async () => {
  // 验证备份路径
  if (!backupForm.backupPath || !backupForm.backupPath.trim()) {
    ElMessage.warning('请输入备份路径，确保 SQL Server 服务有写入权限')
    return
  }

  creating.value = true
  try {
    const backupName = `backup_${new Date().toISOString().replace(/[-:T]/g, '').slice(0, 15)}`
    const res = await systemApi.createBackup({
      backupName,
      description: backupForm.description,
      backupPath: backupForm.backupPath.trim()
    })
    if (res.success) {
      ElMessage.success('备份创建成功')
      backupForm.description = ''
      backupForm.backupPath = ''
      loadData()
    }
  } catch {
    // 创建失败
  } finally {
    creating.value = false
  }
}

const handleRestore = async (row) => {
  try {
    await ElMessageBox.confirm(
      `确定要恢复备份"${row.backupName}"吗？\n\n警告：此操作将覆盖当前数据库！`,
      '恢复备份',
      {
        confirmButtonText: '确定恢复',
        cancelButtonText: '取消',
        type: 'warning'
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
      // 恢复失败
    }
  } finally {
    row.restoring = false
  }
}

const handleDownload = async (row) => {
  ElMessage.info('下载功能待实现')
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
      // 删除失败
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
    retentionCount: 10,
    backupPath: ''
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
    retentionCount: row.retentionCount,
    backupPath: row.backupPath || ''
  })
  scheduleDialogVisible.value = true
}

const handleSaveSchedule = async () => {
  if (!scheduleForm.scheduleName) {
    ElMessage.warning('请输入计划名称')
    return
  }

  if (!scheduleForm.backupPath || !scheduleForm.backupPath.trim()) {
    ElMessage.warning('请输入备份路径，确保 SQL Server 服务有写入权限')
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
  } catch {
    // 保存失败
  } finally {
    savingSchedule.value = false
  }
}

const handleToggleSchedule = async (row) => {
  try {
    await systemApi.toggleBackupSchedule(row.scheduleId)
    ElMessage.success(row.isEnabled ? '已启用' : '已禁用')
    loadSchedules()
  } catch {
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
      // 删除失败
    }
  }
}

const formatRecurringDays = (days) => {
  if (!days || days.length === 0) return '-'
  const dayNames = ['日', '一', '二', '三', '四', '五', '六']
  return days.map(d => dayNames[d]).join('')
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
  display: flex;
  flex-direction: column;
  gap: 12px;
}

/* 顶部工具栏 */
.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  background: #fff;
  border-radius: 4px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

.toolbar-left {
  display: flex;
  gap: 12px;
  align-items: center;
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

/* 主内容区：左右布局 */
.main-content {
  flex: 1;
  display: flex;
  gap: 12px;
  min-height: 0;
  overflow: hidden;
}

/* 左侧备份计划面板 */
.schedule-panel {
  width: 420px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
}

/* 右侧备份列表面板 */
.backup-panel {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

/* 卡片样式 */
.panel-card {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.panel-card :deep(.el-card__body) {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 12px;
  min-height: 0;
  overflow: hidden;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-actions {
  display: flex;
  gap: 8px;
}

/* 表格容器 */
.table-wrapper {
  flex: 1;
  min-height: 0;
  overflow: hidden;
}

/* 分页 */
.pagination {
  flex-shrink: 0;
  margin-top: 12px;
  justify-content: flex-end;
}

/* 计划时间文字 */
.schedule-time {
  font-size: 12px;
}

/* 响应式：窄屏时改为上下布局 */
@media (max-width: 1200px) {
  .main-content {
    flex-direction: column;
  }

  .schedule-panel {
    width: 100%;
    height: 280px;
    flex-shrink: 0;
  }

  .backup-panel {
    flex: 1;
  }
}

@media (max-width: 768px) {
  .toolbar {
    flex-direction: column;
    gap: 12px;
    align-items: stretch;
  }

  .toolbar-left,
  .toolbar-right {
    justify-content: center;
  }

  .schedule-panel {
    height: 250px;
  }
}
</style>
