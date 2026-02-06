<template>
  <div class="report-query">
    <el-row :gutter="20" style="height: 100%;">
      <!-- 左侧：报表列表 -->
      <el-col :span="8" style="height: 100%;">
        <el-card style="height: 100%; overflow-y: auto;">
          <template #header>
            <span>报表列表</span>
          </template>

          <!-- 搜索 -->
          <el-input
            v-model="searchKeyword"
            placeholder="搜索报表..."
            clearable
            style="margin-bottom: 15px;"
          >
            <template #prefix>
              <el-icon><Search /></el-icon>
            </template>
          </el-input>

          <!-- 报表列表 -->
          <div class="report-list">
            <div
              v-for="report in filteredReports"
              :key="report.reportId"
              :class="['report-item', { active: selectedReportId === report.reportId }]"
              @click="selectReport(report)"
            >
              <div class="report-icon">
                <el-icon><Document /></el-icon>
              </div>
              <div class="report-info">
                <div class="report-name">{{ report.reportName }}</div>
                <div class="report-meta">
                  <el-tag size="small" type="info">{{ report.reportCategory || '未分类' }}</el-tag>
                  <span class="view-count">{{ report.viewCount || 0 }} 次查看</span>
                </div>
              </div>
              <el-icon class="report-arrow">
                <ArrowRight />
              </el-icon>
            </div>
          </div>
        </el-card>
      </el-col>

      <!-- 右侧：查询区域 -->
      <el-col :span="16" style="height: 100%;">
        <el-card style="height: 100%; overflow-y: auto;">
          <!-- 未选择报表 -->
          <div v-if="!selectedReport" class="empty-state">
            <el-empty description="请选择左侧报表进行查询">
              <el-icon size="60" color="#909399"><Document /></el-icon>
            </el-empty>
          </div>

          <!-- 已选择报表 -->
          <div v-else class="query-area">
            <!-- 报表标题 -->
            <div class="report-header">
              <h2>{{ selectedReport.reportName }}</h2>
              <el-tag v-if="selectedReport.reportCategory" type="info">{{ selectedReport.reportCategory }}</el-tag>
            </div>

            <!-- 查询条件 -->
            <div v-if="queryConditions.length > 0" class="conditions-section">
              <div class="section-header">
                <span>查询条件</span>
                <el-button link size="small" @click="resetConditions">重置条件</el-button>
              </div>
              <el-form :inline="true" :model="queryForm" label-width="100px">
                <el-row :gutter="20">
                  <el-col :span="12" v-for="qc in queryConditions" :key="qc.fieldName">
                    <el-form-item :label="qc.displayName">
                      <!-- 根据数据类型显示不同输入控件 -->
                      <el-input
                        v-if="qc.dataType === 'String' && qc.operator !== 'null' && qc.operator !== 'notnull'"
                        v-model="queryForm[qc.fieldName]"
                        :placeholder="getOperatorLabel(qc.operator)"
                        clearable
                      />
                      <el-input-number
                        v-else-if="qc.dataType === 'Number' && qc.operator !== 'null' && qc.operator !== 'notnull'"
                        v-model="queryForm[qc.fieldName]"
                        :placeholder="getOperatorLabel(qc.operator)"
                        :controls-position="'right'"
                        style="width: 100%;"
                      />
                      <el-date-picker
                        v-else-if="qc.dataType === 'DateTime' && qc.operator !== 'null' && qc.operator !== 'notnull'"
                        v-model="queryForm[qc.fieldName]"
                        type="date"
                        :placeholder="getOperatorLabel(qc.operator)"
                        value-format="YYYY-MM-DD"
                        style="width: 100%;"
                      />
                      <el-select
                        v-else-if="qc.dataType === 'Boolean'"
                        v-model="queryForm[qc.fieldName]"
                        placeholder="请选择"
                        clearable
                        style="width: 100%;"
                      >
                        <el-option label="是" :value="true" />
                        <el-option label="否" :value="false" />
                      </el-select>
                      <span v-else class="condition-note">{{ getOperatorLabel(qc.operator) }}</span>
                    </el-form-item>
                  </el-col>
                </el-row>
              </el-form>
            </div>

            <!-- 操作按钮 -->
            <div class="action-buttons">
              <el-button type="primary" @click="handleQuery" :loading="querying">
                <el-icon><Search /></el-icon>
                查询
              </el-button>
              <el-button type="success" @click="handleExportExcel" :loading="exporting" :disabled="!reportData || reportData.length === 0">
                <el-icon><Download /></el-icon>
                导出 Excel
              </el-button>
            </div>

            <!-- 查询结果 -->
            <div v-if="reportData && reportData.length > 0" class="results-section">
              <div class="section-header">
                <span>查询结果 (共 {{ reportData.length }} 条记录)</span>
              </div>

              <!-- 表格视图 -->
              <el-table :data="reportData" border max-height="400">
                <el-table-column
                  v-for="col in displayColumns"
                  :key="col.fieldName"
                  :prop="col.fieldName"
                  :label="col.displayName"
                  :width="col.width"
                  :align="col.align || 'left'"
                />
              </el-table>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { reportApi } from '../../api/request'

const searchKeyword = ref('')
const reports = ref([])
const selectedReportId = ref(null)
const selectedReport = ref(null)
const queryConditions = ref([])
const queryForm = reactive({})
const reportData = ref([])
const querying = ref(false)
const exporting = ref(false)

onMounted(async () => {
  await loadReports()
})

const loadReports = async () => {
  try {
    const res = await reportApi.getReports({ page: 1, pageSize: 1000 })
    if (res.success) {
      reports.value = (res.data.Items || res.data.items || []).filter(r => r.isEnabled)
    }
  } catch (error) {
    console.error('加载报表失败:', error)
  }
}

const filteredReports = computed(() => {
  if (!searchKeyword.value) return reports.value
  return reports.value.filter(r =>
    r.reportName.toLowerCase().includes(searchKeyword.value.toLowerCase())
  )
})

const selectReport = async (report) => {
  selectedReportId.value = report.reportId
  try {
    const res = await reportApi.getReport(report.reportId)
    if (res.success) {
      selectedReport.value = res.data
      queryConditions.value = res.data.queryConditions || []
      resetConditions()
      reportData.value = []
    }
  } catch (error) {
    console.error('加载报表详情失败:', error)
  }
}

const displayColumns = computed(() => {
  return selectedReport.value?.fields || []
})

const resetConditions = () => {
  Object.keys(queryForm).forEach(key => delete queryForm[key])
  queryConditions.value.forEach(qc => {
    if (qc.defaultValue) {
      queryForm[qc.fieldName] = qc.defaultValue
    }
  })
}

const getOperatorLabel = (operator) => {
  const map = {
    'eq': '等于',
    'ne': '不等于',
    'gt': '大于',
    'lt': '小于',
    'ge': '大于等于',
    'le': '小于等于',
    'like': '包含',
    'null': '为空',
    'notnull': '不为空'
  }
  return map[operator] || operator
}

const handleQuery = async () => {
  querying.value = true
  try {
    const params = buildQueryParams()
    const res = await reportApi.executeReport(selectedReport.value.reportId, params)
    if (res.success) {
      reportData.value = res.data
    }
  } catch (error) {
    console.error('查询失败:', error)
  } finally {
    querying.value = false
  }
}

const buildQueryParams = () => {
  const params = {}
  queryConditions.value.forEach(qc => {
    const value = queryForm[qc.fieldName]
    if (value === '' || value === null || value === undefined) {
      return
    }
    params[`${qc.fieldName}_${qc.operator}`] = value
  })
  return params
}

const handleExportExcel = async () => {
  exporting.value = true
  try {
    const params = buildQueryParams()
    const res = await reportApi.exportReport(selectedReport.value.reportId, params)
    const blob = new Blob([res], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${selectedReport.value.reportName}_${Date.now()}.xlsx`
    a.click()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    console.error('导出失败:', error)
  } finally {
    exporting.value = false
  }
}
</script>

<style scoped>
.report-query {
  height: 100%;
}

.report-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.report-item {
  display: flex;
  align-items: center;
  padding: 12px;
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
}

.report-item:hover {
  background-color: #f5f7fa;
  border-color: #409eff;
}

.report-item.active {
  background-color: #ecf5ff;
  border-color: #409eff;
}

.report-icon {
  font-size: 24px;
  color: #409eff;
  margin-right: 12px;
}

.report-info {
  flex: 1;
}

.report-name {
  font-weight: 500;
  margin-bottom: 4px;
}

.report-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: #909399;
}

.report-arrow {
  color: #c0c4cc;
}

.empty-state {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
}

.query-area {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.report-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-bottom: 16px;
  border-bottom: 1px solid #e4e7ed;
}

.report-header h2 {
  margin: 0;
  font-size: 20px;
}

.conditions-section,
.results-section {
  padding: 16px;
  background-color: #f5f7fa;
  border-radius: 6px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
  font-weight: 500;
}

.action-buttons {
  display: flex;
  gap: 12px;
}

.condition-note {
  color: #909399;
  font-size: 14px;
}
</style>
