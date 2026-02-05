<template>
  <div class="log-management">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>操作日志</span>
          <el-button type="danger" @click="handleClearLogs">
            <el-icon><Delete /></el-icon>
            清空日志
          </el-button>
        </div>
      </template>

      <!-- 搜索表单 -->
      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="操作人">
          <el-input v-model="searchForm.username" placeholder="请输入操作人" clearable />
        </el-form-item>
        <el-form-item label="操作类型">
          <el-select v-model="searchForm.action" placeholder="请选择操作类型" clearable>
            <el-option label="全部" value="" />
            <el-option label="创建" value="Create" />
            <el-option label="更新" value="Update" />
            <el-option label="删除" value="Delete" />
            <el-option label="切换状态" value="Toggle" />
            <el-option label="测试连接" value="TestConnection" />
            <el-option label="获取数据库列表" value="GetDatabases" />
            <el-option label="修改" value="Modify" />
          </el-select>
        </el-form-item>
        <el-form-item label="模块">
          <el-select v-model="searchForm.module" placeholder="请选择模块" clearable>
            <el-option label="全部" value="" />
            <el-option label="用户管理" value="User" />
            <el-option label="角色管理" value="Role" />
            <el-option label="数据源管理" value="DataSource" />
            <el-option label="报表管理" value="Report" />
            <el-option label="许可管理" value="License" />
            <el-option label="系统管理" value="System" />
            <el-option label="其他" value="Other" />
          </el-select>
        </el-form-item>
        <el-form-item label="时间范围">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>
            查询
          </el-button>
          <el-button @click="handleReset">重置</el-button>
          <el-button type="warning" @click="handleDeleteSelected">
            <el-icon><Delete /></el-icon>
            删除
          </el-button>
          <el-button type="success" @click="handleExport" :loading="exporting">
            <el-icon><Download /></el-icon>
            导出
          </el-button>
        </el-form-item>
      </el-form>

      <!-- 日志表格 -->
      <el-table
        :data="tableData"
        v-loading="loading"
        border
        stripe
        @selection-change="handleSelectionChange"
      >
        <el-table-column type="selection" width="55" />
        <el-table-column prop="username" label="操作人" width="120" />
        <el-table-column prop="action" label="操作类型" width="100">
          <template #default="{ row }">
            <el-tag :type="getActionType(row.action)" size="small">
              {{ getActionText(row.action) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="module" label="模块" width="100" />
        <el-table-column prop="description" label="操作描述" min-width="250" show-overflow-tooltip />
        <el-table-column prop="ip" label="IP地址" width="140" />
        <el-table-column prop="createdTime" label="操作时间" width="180" />
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleViewDetail(row)">
              <el-icon><View /></el-icon>
              详情
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

    <!-- 日志详情对话框 -->
    <el-dialog v-model="detailDialogVisible" title="日志详情" width="600px">
      <el-descriptions :column="1" border v-if="currentLog">
        <el-descriptions-item label="操作人">{{ currentLog.username }}</el-descriptions-item>
        <el-descriptions-item label="操作类型">
          <el-tag :type="getActionType(currentLog.action)" size="small">
            {{ getActionText(currentLog.action) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="模块">{{ currentLog.module }}</el-descriptions-item>
        <el-descriptions-item label="操作描述">{{ currentLog.description }}</el-descriptions-item>
        <el-descriptions-item label="IP地址">{{ currentLog.ip }}</el-descriptions-item>
        <el-descriptions-item label="浏览器">{{ currentLog.browser }}</el-descriptions-item>
        <el-descriptions-item label="操作系统">{{ currentLog.os }}</el-descriptions-item>
        <el-descriptions-item label="操作时间">{{ currentLog.createdTime }}</el-descriptions-item>
        <el-descriptions-item label="请求参数" v-if="currentLog.requestData">
          <pre style="max-height: 200px; overflow: auto;">{{ formatJson(currentLog.requestData) }}</pre>
        </el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { systemApi } from '../../api/request'

const loading = ref(false)
const exporting = ref(false)
const detailDialogVisible = ref(false)
const currentLog = ref(null)
const dateRange = ref([])
const selectedRows = ref([])

const tableData = ref([])

const searchForm = reactive({
  username: '',
  action: '',
  module: '',
  startTime: '',
  endTime: ''
})

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

// 检查是否有查询条件
onMounted(() => {
  loadData()
})

const loadData = async () => {
  loading.value = true
  try {
    // 处理日期范围
    const params = { ...searchForm }
    if (dateRange.value && dateRange.value.length === 2) {
      params.startTime = dateRange.value[0]
      params.endTime = dateRange.value[1]
    }

    const res = await systemApi.getLogs({
      page: pagination.page,
      pageSize: pagination.pageSize,
      ...params
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
  searchForm.username = ''
  searchForm.action = ''
  searchForm.module = ''
  dateRange.value = []
  handleSearch()
}

const handleViewDetail = (row) => {
  currentLog.value = row
  detailDialogVisible.value = true
}

const handleClearLogs = async () => {
  try {
    await ElMessageBox.confirm('确定要清空所有日志吗？此操作不可恢复！', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    const res = await systemApi.clearLogs()
    if (res.success) {
      ElMessage.success('清空成功')
      loadData()
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('清空失败:', error)
    }
  }
}

const handleDeleteSelected = async () => {
  try {
    if (selectedRows.value.length === 0) {
      ElMessage.warning('请先选择要删除的日志')
      return
    }

    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 条日志吗？此操作不可恢复！`, '确认删除', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    const logIds = selectedRows.value.map(row => row.logId)
    const res = await systemApi.deleteLogsByIds(logIds)
    if (res.success) {
      ElMessage.success(res.message || '删除成功')
      selectedRows.value = []
      loadData()
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}

const handleSelectionChange = (selection) => {
  selectedRows.value = selection
}

const handleExport = async () => {
  exporting.value = true
  try {
    const params = { ...searchForm }
    if (dateRange.value && dateRange.value.length === 2) {
      params.startTime = dateRange.value[0]
      params.endTime = dateRange.value[1]
    }

    const blob = await systemApi.exportLogs(params)

    // 创建下载链接
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `操作日志_${new Date().getTime()}.xlsx`
    link.click()
    window.URL.revokeObjectURL(url)

    ElMessage.success('导出成功')
  } catch (error) {
    console.error('导出失败:', error)
    ElMessage.error('导出失败')
  } finally {
    exporting.value = false
  }
}

const getActionType = (action) => {
  const map = {
    'Login': 'success',
    'Logout': 'info',
    'Create': 'success',
    'Update': 'warning',
    'Delete': 'danger',
    'Toggle': 'warning',
    'TestConnection': 'primary',
    'GetDatabases': 'info',
    'Modify': 'warning'
  }
  return map[action] || 'info'
}

const getActionText = (action) => {
  const map = {
    'Login': '登录',
    'Logout': '登出',
    'Create': '创建',
    'Update': '更新',
    'Delete': '删除',
    'Toggle': '切换状态',
    'TestConnection': '测试连接',
    'GetDatabases': '获取数据库列表',
    'Modify': '修改',
    'Unknown': '未知'
  }
  return map[action] || action || '未知'
}

const formatJson = (data) => {
  try {
    return JSON.stringify(JSON.parse(data), null, 2)
  } catch {
    return data
  }
}
</script>

<style scoped>
.log-management {
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

pre {
  background-color: #f5f7fa;
  padding: 10px;
  border-radius: 4px;
  font-size: 12px;
}
</style>
