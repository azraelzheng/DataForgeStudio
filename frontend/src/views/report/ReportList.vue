<template>
  <div class="report-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>报表查询</span>
          <el-button type="primary" @click="router.push('/report/design')">
            <el-icon><Plus /></el-icon>
            新建报表
          </el-button>
        </div>
      </template>

      <!-- 搜索表单 -->
      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="报表名称">
          <el-input v-model="searchForm.reportName" placeholder="请输入报表名称" clearable />
        </el-form-item>
        <el-form-item label="分类">
          <el-select v-model="searchForm.category" placeholder="请选择分类" clearable>
            <el-option label="全部" value="" />
            <el-option label="销售" value="销售" />
            <el-option label="库存" value="库存" />
            <el-option label="财务" value="财务" />
            <el-option label="其他" value="其他" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>
            查询
          </el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <!-- 报表表格 -->
      <el-table :data="tableData" v-loading="loading" border stripe>
        <el-table-column prop="reportName" label="报表名称" min-width="200" />
        <el-table-column prop="reportCategory" label="分类" width="120" />
        <el-table-column prop="description" label="描述" minwidth="200" show-overflow-tooltip />
        <el-table-column prop="createdBy" label="创建人" width="120" />
        <el-table-column prop="createdTime" label="创建时间" width="180" />
        <el-table-column prop="viewCount" label="查看次数" width="100" align="center" />
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleView(row)">
              <el-icon><View /></el-icon>
              查看
            </el-button>
            <el-button type="primary" link size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button type="primary" link size="small" @click="handleExport(row)">
              <el-icon><Download /></el-icon>
              导出
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

    <!-- 报表查看对话框 -->
    <el-dialog v-model="viewDialogVisible" title="报表查看" width="90%" top="5vh">
      <div v-if="currentReport">
        <div class="report-query-form">
          <el-form :inline="true" :model="queryForm">
            <el-form-item
              v-for="param in currentReport.parameters"
              :key="param.name"
              :label="param.label"
            >
              <el-date-picker
                v-if="param.dataType === 'DateTime'"
                v-model="queryForm[param.name]"
                type="daterange"
                range-separator="-"
                start-placeholder="开始日期"
                end-placeholder="结束日期"
              />
              <el-select v-else-if="param.dataType === 'Select'" v-model="queryForm[param.name]">
                <el-option
                  v-for="opt in param.options"
                  :key="opt.value"
                  :label="opt.label"
                  :value="opt.value"
                />
              </el-select>
              <el-input v-else v-model="queryForm[param.name]" :placeholder="`请输入${param.label}`" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleQuery">
                <el-icon><Search /></el-icon>
                查询
              </el-button>
              <el-button @click="handleExportExcel">
                <el-icon><Download /></el-icon>
                导出Excel
              </el-button>
            </el-form-item>
          </el-form>
        </div>

        <el-tabs v-model="activeTab">
          <el-tab-pane label="表格" name="table">
            <el-table :data="reportData" border max-height="500" v-loading="queryLoading">
              <el-table-column
                v-for="col in currentReport.columns"
                :key="col.fieldName"
                :prop="col.fieldName"
                :label="col.displayName"
                :width="col.width"
              />
            </el-table>
          </el-tab-pane>
          <el-tab-pane label="图表" name="chart">
            <div ref="chartRef" style="width: 100%; height: 400px;"></div>
          </el-tab-pane>
        </el-tabs>
      </div>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { reportApi } from '../../api/request'
import * as echarts from 'echarts'

const router = useRouter()

const loading = ref(false)
const queryLoading = ref(false)
const tableData = ref([])
const reportData = ref([])
const viewDialogVisible = ref(false)
const currentReport = ref(null)
const activeTab = ref('table')
const chartRef = ref(null)
let chartInstance = null

const searchForm = reactive({
  reportName: '',
  category: ''
})

const queryForm = reactive({})

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
    const res = await reportApi.getReports({
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
  searchForm.reportName = ''
  searchForm.category = ''
  handleSearch()
}

const handleView = async (row) => {
  try {
    const res = await reportApi.getReport(row.reportId)
    if (res.success) {
      currentReport.value = res.data
      viewDialogVisible.value = true
      // 重置查询参数
      Object.keys(queryForm).forEach(key => delete queryForm[key])
    }
  } catch (error) {
    console.error('加载报表失败:', error)
  }
}

const handleEdit = (row) => {
  router.push(`/report/design?id=${row.reportId}`)
}

const handleQuery = async () => {
  if (!currentReport.value) return

  queryLoading.value = true
  try {
    const res = await reportApi.executeReport(currentReport.value.reportId, queryForm)
    if (res.success) {
      reportData.value = res.data
    }
  } catch (error) {
    console.error('查询失败:', error)
  } finally {
    queryLoading.value = false
  }
}

const handleExport = async (row) => {
  try {
    const res = await reportApi.exportReport(row.reportId, {})
    // 处理文件下载
    const blob = new Blob([res], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${row.reportName}.xlsx`
    a.click()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    console.error('导出失败:', error)
  }
}

const handleExportExcel = () => {
  if (currentReport.value) {
    handleExport(currentReport.value)
  }
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要删除报表"${row.reportName}"吗？`, '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    const res = await reportApi.deleteReport(row.reportId)
    if (res.success) {
      ElMessage.success('删除成功')
      loadData()
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}
</script>

<style scoped>
.report-list {
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

.report-query-form {
  margin-bottom: 20px;
  padding: 15px;
  background-color: #f5f7fa;
  border-radius: 4px;
}
</style>
