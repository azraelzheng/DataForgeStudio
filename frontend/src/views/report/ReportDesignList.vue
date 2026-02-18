<template>
  <div class="report-design-list">
    <el-card class="flex-card">
      <template #header>
        <div class="card-header">
          <span>报表设计管理</span>
          <div>
            <el-button type="primary" @click="handleExportConfig">
              <el-icon><Download /></el-icon>
              导出配置
            </el-button>
            <el-button type="primary" @click="handleCreate">
              <el-icon><Plus /></el-icon>
              新建报表
            </el-button>
          </div>
        </div>
      </template>

      <!-- 搜索表单 -->
      <div class="search-grid">
        <div class="search-item">
          <label class="search-label">报表名称</label>
          <el-input v-model="searchForm.reportName" placeholder="请输入报表名称" clearable />
        </div>
        <div class="search-item">
          <label class="search-label">分类</label>
          <el-select v-model="searchForm.category" placeholder="请选择分类" clearable>
            <el-option label="全部" value="" />
            <el-option label="销售" value="销售" />
            <el-option label="库存" value="库存" />
            <el-option label="财务" value="财务" />
            <el-option label="其他" value="其他" />
          </el-select>
        </div>
        <div class="search-item">
          <label class="search-label">状态</label>
          <el-select v-model="searchForm.isEnabled" placeholder="请选择状态" clearable>
            <el-option label="全部" value="" />
            <el-option label="启用" :value="true" />
            <el-option label="停用" :value="false" />
          </el-select>
        </div>
        <div class="search-actions">
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>
            查询
          </el-button>
          <el-button @click="handleReset">重置</el-button>
        </div>
      </div>

      <!-- 报表表格 -->
      <div class="table-wrapper" ref="tableWrapper">
        <template v-if="tableData && tableData.length > 0">
          <el-table :data="tableData" v-loading="loading" border stripe :height="tableHeight">
            <el-table-column prop="reportName" label="报表名称" width="200" />
            <el-table-column prop="reportCategory" label="分类" width="100" />
            <el-table-column prop="dataSourceName" label="数据源" width="150" />
            <el-table-column label="状态" width="80" align="center">
              <template #default="{ row }">
                <el-tag :type="row.isEnabled ? 'success' : 'danger'" size="small">
                  {{ row.isEnabled ? '启用' : '停用' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="viewCount" label="查看次数" width="100" align="center" />
            <el-table-column prop="lastViewTime" label="最后查看" width="180">
              <template #default="{ row }">
                {{ row.lastViewTime ? formatDate(row.lastViewTime) : '-' }}
              </template>
            </el-table-column>
            <el-table-column prop="createdTime" label="创建时间" width="180" />
            <el-table-column label="操作" width="320" fixed="right">
              <template #default="{ row }">
                <el-button type="primary" link size="small" @click="handleEdit(row)">
                  <el-icon><Edit /></el-icon>
                  编辑
                </el-button>
                <el-button type="info" link size="small" @click="handlePreview(row)">
                  <el-icon><View /></el-icon>
                  预览
                </el-button>
                <el-button type="success" link size="small" @click="handleCopy(row)">
                  <el-icon><DocumentCopy /></el-icon>
                  复制
                </el-button>
                <el-button
                  :type="row.isEnabled ? 'warning' : 'success'"
                  link
                  size="small"
                  @click="handleToggleStatus(row)"
                >
                  <el-icon><Switch /></el-icon>
                  {{ row.isEnabled ? '停用' : '启用' }}
                </el-button>
                <el-button type="danger" link size="small" @click="handleDelete(row)">
                  <el-icon><Delete /></el-icon>
                  删除
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </template>
        <el-empty v-else-if="!loading" description="暂无报表数据，点击右上角创建报表">
          <el-button type="primary" @click="handleCreate">创建报表</el-button>
        </el-empty>
      </div>

      <!-- 分页 -->
      <el-pagination
        v-model:current-page="pagination.page"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[10, 20, 50, 100]"
        :total="pagination.total"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="loadData"
        @current-change="loadData"
      />
    </el-card>

    <!-- 预览对话框 -->
    <el-dialog v-model="previewVisible" title="报表预览" width="90%" top="5vh">
      <div v-if="previewReport">
        <div class="report-info">
          <el-descriptions :column="2" border>
            <el-descriptions-item label="报表名称">{{ previewReport.reportName }}</el-descriptions-item>
            <el-descriptions-item label="分类">{{ previewReport.reportCategory }}</el-descriptions-item>
            <el-descriptions-item label="数据源">{{ previewReport.dataSourceName }}</el-descriptions-item>
            <el-descriptions-item label="描述">{{ previewReport.description || '-' }}</el-descriptions-item>
          </el-descriptions>
        </div>
        <el-divider>字段配置</el-divider>
        <el-table :data="previewReport.columns || []" border>
          <el-table-column prop="fieldName" label="字段名" width="150" />
          <el-table-column prop="displayName" label="显示名称" width="150" />
          <el-table-column prop="dataType" label="数据类型" width="100" />
          <el-table-column prop="width" label="宽度" width="80" />
        </el-table>
      </div>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onActivated, onUnmounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { reportApi } from '../../api/request'

const router = useRouter()
const loading = ref(false)
const tableData = ref([])
const previewVisible = ref(false)
const previewReport = ref(null)
const tableWrapper = ref(null)
const tableHeight = ref(null)

const searchForm = reactive({
  reportName: '',
  category: '',
  isEnabled: ''
})

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

onMounted(() => {
  loadData()
  // 初始化表格高度
  nextTick(() => {
    updateTableHeight()
  })
  // 监听窗口大小变化
  window.addEventListener('resize', updateTableHeight)
})

// 当从 keep-alive 缓存中重新激活时，刷新数据
onActivated(() => {
  loadData()
  // 重新激活时更新表格高度
  nextTick(() => {
    updateTableHeight()
  })
})

onUnmounted(() => {
  // 清理 resize 监听
  window.removeEventListener('resize', updateTableHeight)
})

// 更新表格高度
const updateTableHeight = () => {
  if (tableWrapper.value) {
    // 使用 wrapper 的实际高度作为表格高度
    tableHeight.value = tableWrapper.value.clientHeight
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await reportApi.getReports({
      page: pagination.page,
      pageSize: pagination.pageSize,
      includeDisabled: true,
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
  searchForm.reportName = ''
  searchForm.category = ''
  searchForm.isEnabled = ''
  handleSearch()
}

const handleCreate = () => {
  router.push('/report/designer')
}

const handleEdit = (row) => {
  router.push(`/report/designer?id=${row.reportId}`)
}

const handlePreview = async (row) => {
  try {
    const res = await reportApi.getReport(row.reportId)
    if (res.success) {
      previewReport.value = res.data
      previewVisible.value = true
    }
  } catch (error) {
    console.error('加载报表失败:', error)
  }
}

const handleCopy = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要复制报表"${row.reportName}"吗？`, '确认复制', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'info'
    })

    const res = await reportApi.copyReport(row.reportId)
    if (res.success) {
      ElMessage.success('复制成功，请在编辑页面完善副本报表')
      loadData()
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('复制失败:', error)
    }
  }
}

const handleToggleStatus = async (row) => {
  try {
    const res = await reportApi.toggleReport(row.reportId)
    if (res.success) {
      ElMessage.success(res.message || (row.isEnabled ? '已停用' : '已启用'))
      loadData()
    } else {
      ElMessage.error(res.message || '操作失败')
    }
  } catch (error) {
    console.error('更新状态失败:', error)
    ElMessage.error('更新状态失败')
  }
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要删除报表"${row.reportName}"吗？此操作不可恢复！`, '确认删除', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await reportApi.deleteReport(row.reportId)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}

const handleExportConfig = async () => {
  try {
    const res = await reportApi.exportAllConfigs()
    if (res.success) {
      const blob = new Blob([JSON.stringify(res.data, null, 2)], { type: 'application/json' })
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `report-configs-${Date.now()}.json`
      a.click()
      window.URL.revokeObjectURL(url)
      ElMessage.success('配置导出成功')
    }
  } catch (error) {
    console.error('导出配置失败:', error)
  }
}

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN')
}
</script>

<style scoped>
.report-design-list {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.flex-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.flex-card :deep(.el-card__header) {
  flex-shrink: 0;
}

.flex-card :deep(.el-card__body) {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 12px 16px;
  margin-bottom: 16px;
  align-items: end;
  flex-shrink: 0;
}

.search-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.search-label {
  font-size: 14px;
  color: #606266;
  font-weight: 500;
}

.search-item :deep(.el-input),
.search-item :deep(.el-select) {
  width: 100%;
}

.search-actions {
  display: flex;
  gap: 8px;
  align-items: flex-end;
  height: 32px;
}

.table-wrapper {
  flex: 1;
  overflow: hidden;
  min-height: 100px;
}

.el-pagination {
  flex-shrink: 0;
  margin-top: 16px;
  justify-content: flex-end;
}

.report-info {
  margin-bottom: 20px;
}
</style>
