<template>
  <div class="dashboard-list">
    <el-card class="flex-card">
      <template #header>
        <div class="card-header">
          <span>大屏管理</span>
          <div>
            <el-button type="success" @click="handleConvertFromReport">
              <el-icon><Switch /></el-icon>
              一键转换
            </el-button>
            <el-button type="primary" @click="handleCreate">
              <el-icon><Plus /></el-icon>
              新建大屏
            </el-button>
          </div>
        </div>
      </template>

      <!-- 搜索表单 -->
      <div class="search-grid">
        <div class="search-item">
          <label class="search-label">大屏名称</label>
          <el-input v-model="searchForm.keyword" placeholder="请输入大屏名称" clearable @keyup.enter="handleSearch" />
        </div>
        <div class="search-actions">
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>
            查询
          </el-button>
          <el-button @click="handleReset">重置</el-button>
        </div>
      </div>

      <!-- 大屏表格 -->
      <div class="table-wrapper" ref="tableWrapper">
        <template v-if="tableData && tableData.length > 0">
          <el-table :data="tableData" v-loading="loading" border stripe :height="tableHeight">
            <el-table-column prop="name" label="大屏名称" width="200" show-overflow-tooltip />
            <el-table-column prop="description" label="描述" width="200" show-overflow-tooltip>
              <template #default="{ row }">
                {{ row.description || '-' }}
              </template>
            </el-table-column>
            <el-table-column label="主题" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="getThemeTagType(row.settings?.theme)" size="small">
                  {{ getThemeLabel(row.settings?.theme) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="组件数" width="80" align="center">
              <template #default="{ row }">
                {{ row.widgetCount || 0 }}
              </template>
            </el-table-column>
            <el-table-column label="是否公开" width="90" align="center">
              <template #default="{ row }">
                <el-tag :type="row.isPublic ? 'success' : 'info'" size="small">
                  {{ row.isPublic ? '公开' : '私有' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="createdTime" label="创建时间" width="180">
              <template #default="{ row }">
                {{ formatDate(row.createdTime) }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="300" fixed="right">
              <template #default="{ row }">
                <el-button type="primary" link size="small" @click="handleEdit(row)">
                  <el-icon><Edit /></el-icon>
                  编辑
                </el-button>
                <el-button type="info" link size="small" @click="handleView(row)">
                  <el-icon><View /></el-icon>
                  查看
                </el-button>
                <el-button type="success" link size="small" @click="handleCopy(row)">
                  <el-icon><DocumentCopy /></el-icon>
                  复制
                </el-button>
                <el-button
                  :type="row.isPublic ? 'warning' : 'success'"
                  link
                  size="small"
                  @click="handleTogglePublic(row)"
                >
                  <el-icon><Unlock v-if="!row.isPublic" /><Lock v-else /></el-icon>
                  {{ row.isPublic ? '设为私有' : '设为公开' }}
                </el-button>
                <el-button type="danger" link size="small" @click="handleDelete(row)">
                  <el-icon><Delete /></el-icon>
                  删除
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </template>
        <el-empty v-else-if="!loading" description="暂无大屏数据，点击右上角创建大屏">
          <el-button type="primary" @click="handleCreate">创建大屏</el-button>
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

    <!-- 一键转换对话框 -->
    <el-dialog v-model="convertDialogVisible" title="从报表转换为大屏" width="500px">
      <el-form :model="convertForm" label-width="100px" :rules="convertRules" ref="convertFormRef">
        <el-form-item label="选择报表" prop="reportId">
          <el-select v-model="convertForm.reportId" placeholder="请选择要转换的报表" style="width: 100%" filterable>
            <el-option
              v-for="report in reportList"
              :key="report.reportId"
              :label="report.reportName"
              :value="report.reportId"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="大屏名称" prop="dashboardName">
          <el-input v-model="convertForm.dashboardName" placeholder="请输入大屏名称" />
        </el-form-item>
        <el-form-item label="画布尺寸">
          <el-col :span="11">
            <el-input-number v-model="convertForm.width" :min="800" :max="3840" placeholder="宽度" style="width: 100%" />
          </el-col>
          <el-col :span="2" style="text-align: center">x</el-col>
          <el-col :span="11">
            <el-input-number v-model="convertForm.height" :min="600" :max="2160" placeholder="高度" style="width: 100%" />
          </el-col>
        </el-form-item>
        <el-form-item label="主题风格">
          <el-radio-group v-model="convertForm.theme">
            <el-radio value="dark">深色主题</el-radio>
            <el-radio value="light">浅色主题</el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="convertDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleConfirmConvert" :loading="convertLoading">
          确认转换
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onActivated, onUnmounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getDashboards, deleteDashboard, toggleDashboard, copyDashboard, convertFromReport } from '../../api/dashboard'
import { reportApi } from '../../api/request'

const router = useRouter()
const loading = ref(false)
const tableData = ref([])
const tableWrapper = ref(null)
const tableHeight = ref(null)

// 一键转换相关
const convertDialogVisible = ref(false)
const convertLoading = ref(false)
const reportList = ref([])
const convertFormRef = ref(null)

const searchForm = reactive({
  keyword: ''
})

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

const convertForm = reactive({
  reportId: null,
  dashboardName: '',
  width: 1920,
  height: 1080,
  theme: 'dark'
})

const convertRules = {
  reportId: [{ required: true, message: '请选择要转换的报表', trigger: 'change' }],
  dashboardName: [{ required: true, message: '请输入大屏名称', trigger: 'blur' }]
}

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

// 加载大屏列表数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await getDashboards({
      page: pagination.page,
      pageSize: pagination.pageSize,
      keyword: searchForm.keyword
    })
    if (res.success) {
      const data = res.data
      tableData.value = data.Items || data.items || []
      pagination.total = data.TotalCount || data.total || 0
    }
  } catch {
    // 加载失败
  } finally {
    loading.value = false
  }
}

// 获取主题标签类型
const getThemeTagType = (theme) => {
  if (theme === 'dark') return 'dark'
  if (theme === 'light') return ''
  return 'info'
}

// 获取主题显示文字
const getThemeLabel = (theme) => {
  if (theme === 'dark') return '深色'
  if (theme === 'light') return '浅色'
  return '默认'
}

// 格式化日期
const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN')
}

// 搜索
const handleSearch = () => {
  pagination.page = 1
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.keyword = ''
  handleSearch()
}

// 新建大屏
const handleCreate = () => {
  router.push('/dashboard/designer')
}

// 编辑大屏
const handleEdit = (row) => {
  router.push(`/dashboard/designer/${row.dashboardId || row.id}`)
}

// 查看大屏
const handleView = (row) => {
  router.push(`/dashboard/view/${row.dashboardId || row.id}`)
}

// 复制大屏
const handleCopy = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要复制大屏"${row.name}"吗？`, '确认复制', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'info'
    })

    const res = await copyDashboard(row.dashboardId || row.id)
    if (res.success) {
      ElMessage.success('复制成功')
      loadData()
    } else {
      ElMessage.error(res.message || '复制失败')
    }
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('复制失败')
    }
  }
}

// 切换公开状态
const handleTogglePublic = async (row) => {
  try {
    const action = row.isPublic ? '设为私有' : '设为公开'
    await ElMessageBox.confirm(`确定要将大屏"${row.name}"${action}吗？`, '确认操作', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'info'
    })

    const res = await toggleDashboard(row.dashboardId || row.id)
    if (res.success) {
      ElMessage.success(res.message || `${action}成功`)
      loadData()
    } else {
      ElMessage.error(res.message || '操作失败')
    }
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('操作失败')
    }
  }
}

// 删除大屏
const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要删除大屏"${row.name}"吗？此操作不可恢复！`, '确认删除', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    const res = await deleteDashboard(row.dashboardId || row.id)
    if (res.success) {
      ElMessage.success('删除成功')
      loadData()
    } else {
      ElMessage.error(res.message || '删除失败')
    }
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 打开一键转换对话框
const handleConvertFromReport = async () => {
  // 加载报表列表
  try {
    const res = await reportApi.getReports({ pageSize: 1000, includeDisabled: false })
    if (res.success) {
      const data = res.data
      reportList.value = data.Items || data.items || []
      // 重置表单
      convertForm.reportId = null
      convertForm.dashboardName = ''
      convertForm.width = 1920
      convertForm.height = 1080
      convertForm.theme = 'dark'
      convertDialogVisible.value = true
    }
  } catch {
    ElMessage.error('加载报表列表失败')
  }
}

// 确认转换
const handleConfirmConvert = async () => {
  if (!convertFormRef.value) return

  try {
    await convertFormRef.value.validate()
  } catch {
    return
  }

  convertLoading.value = true
  try {
    const res = await convertFromReport(convertForm.reportId, convertForm.dashboardName, {
      width: convertForm.width,
      height: convertForm.height,
      theme: convertForm.theme
    })
    if (res.success) {
      ElMessage.success('转换成功')
      convertDialogVisible.value = false
      // 跳转到编辑页面
      const dashboardId = res.data?.dashboardId || res.data?.id
      if (dashboardId) {
        router.push(`/dashboard/designer/${dashboardId}`)
      } else {
        loadData()
      }
    } else {
      ElMessage.error(res.message || '转换失败')
    }
  } catch {
    ElMessage.error('转换失败')
  } finally {
    convertLoading.value = false
  }
}
</script>

<style scoped>
.dashboard-list {
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
</style>
