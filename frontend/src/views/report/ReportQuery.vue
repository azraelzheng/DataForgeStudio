<template>
  <div class="report-query">
    <!-- 可折叠侧边栏 -->
    <div class="sidebar">
      <!-- 临时占位，下个任务会完善 -->
      <div style="padding: 16px;">侧边栏占位</div>
    </div>

    <!-- 主内容区域 -->
    <div class="main-content">
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
              <el-form :inline="true" :model="conditionForm" label-width="100px">
                <el-row :gutter="20">
                  <el-col :span="12" v-for="qc in queryConditions" :key="qc.fieldName + qc.operator">
                    <el-form-item :label="qc.displayName">
                      <!-- 不需要输入值的操作符 -->
                      <template v-if="['null', 'notnull', 'true', 'false'].includes(qc.operator)">
                        <span style="color: #909399; font-size: 14px;">{{ getOperatorLabel(qc.operator) }}</span>
                      </template>

                      <!-- DateTime between: 日期范围选择器 -->
                      <template v-else-if="qc.operator === 'between' && qc.dataType === 'DateTime'">
                        <el-date-picker
                          v-model="conditionForm[getFieldKey(qc)]"
                          type="daterange"
                          range-separator="至"
                          start-placeholder="开始日期"
                          end-placeholder="结束日期"
                          value-format="YYYY-MM-DD"
                          style="width: 100%;"
                        />
                      </template>

                      <!-- Number between: 两个数字输入框 -->
                      <template v-else-if="qc.operator === 'between' && qc.dataType === 'Number'">
                        <div style="display: flex; align-items: center; gap: 8px;">
                          <el-input-number
                            v-model="conditionForm[getFieldKey(qc) + '_start']"
                            placeholder="最小值"
                            :controls-position="'right'"
                            style="flex: 1;"
                          />
                          <span style="color: #909399;">~</span>
                          <el-input-number
                            v-model="conditionForm[getFieldKey(qc) + '_end']"
                            placeholder="最大值"
                            :controls-position="'right'"
                            style="flex: 1;"
                          />
                        </div>
                      </template>

                      <!-- String 类型 -->
                      <template v-else-if="qc.dataType === 'String'">
                        <el-input
                          v-model="conditionForm[getFieldKey(qc)]"
                          :placeholder="getOperatorPlaceholder(qc.operator)"
                          clearable
                        />
                      </template>

                      <!-- Number 类型 -->
                      <template v-else-if="qc.dataType === 'Number'">
                        <el-input-number
                          v-model="conditionForm[getFieldKey(qc)]"
                          :placeholder="getOperatorPlaceholder(qc.operator)"
                          :controls-position="'right'"
                          style="width: 100%;"
                        />
                      </template>

                      <!-- DateTime 类型 -->
                      <template v-else-if="qc.dataType === 'DateTime'">
                        <el-date-picker
                          v-model="conditionForm[getFieldKey(qc)]"
                          type="date"
                          :placeholder="getOperatorPlaceholder(qc.operator)"
                          value-format="YYYY-MM-DD"
                          style="width: 100%;"
                        />
                      </template>

                      <!-- Boolean 类型 -->
                      <template v-else-if="qc.dataType === 'Boolean'">
                        <el-select
                          v-model="conditionForm[getFieldKey(qc)]"
                          placeholder="请选择"
                          clearable
                          style="width: 100%;"
                        >
                          <el-option label="是" :value="true" />
                          <el-option label="否" :value="false" />
                        </el-select>
                      </template>
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
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { reportApi } from '../../api/request'

const router = useRouter()

const searchKeyword = ref('')
const reports = ref([])
const selectedReportId = ref(null)
const selectedReport = ref(null)
const queryConditions = ref([])
const conditionForm = reactive({})
const reportData = ref([])
const querying = ref(false)
const exporting = ref(false)

// 操作符标签映射
const operatorLabels = {
  'eq': '等于',
  'ne': '不等于',
  'gt': '大于',
  'lt': '小于',
  'ge': '大于等于',
  'le': '小于等于',
  'like': '包含',
  'start': '开头是',
  'end': '结尾是',
  'null': '为空',
  'notnull': '不为空',
  'true': '为真',
  'false': '为假',
  'between': '两者之间'
}

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
      // 调试：打印查询条件数据
      console.log('查询条件数据:', JSON.stringify(queryConditions.value, null, 2))
      resetConditions()
      reportData.value = []
    }
  } catch (error) {
    console.error('加载报表详情失败:', error)
  }
}

const displayColumns = computed(() => {
  return selectedReport.value?.columns || selectedReport.value?.fields || []
})

// 获取字段键名（用于表单绑定）
const getFieldKey = (qc) => {
  return `${qc.fieldName}_${qc.operator}`
}

// 获取操作符标签
const getOperatorLabel = (operator) => {
  return operatorLabels[operator] || operator
}

// 获取操作符占位符
const getOperatorPlaceholder = (operator) => {
  const labels = {
    'eq': '请输入等于的值',
    'ne': '请输入不等于的值',
    'gt': '请输入最小值（不含）',
    'lt': '请输入最大值（不含）',
    'ge': '请输入最小值（含）',
    'le': '请输入最大值（含）',
    'like': '请输入包含的关键字',
    'start': '请输入开头文字',
    'end': '请输入结尾文字'
  }
  return labels[operator] || '请输入值'
}

const resetConditions = () => {
  Object.keys(conditionForm).forEach(key => delete conditionForm[key])
  queryConditions.value.forEach(qc => {
    // 不需要输入值的操作符不需要默认值
    if (['null', 'notnull', 'true', 'false'].includes(qc.operator)) {
      return
    }
    if (qc.defaultValue) {
      conditionForm[getFieldKey(qc)] = qc.defaultValue
    }
  })
}

const handleQuery = async () => {
  querying.value = true
  try {
    const params = buildQueryParams()
    const res = await reportApi.executeReport(selectedReport.value.reportId, { parameters: params })
    if (res.success) {
      reportData.value = res.data
    } else {
      ElMessage.error(res.message || '查询失败')
    }
  } catch (error) {
    console.error('查询失败:', error)
    ElMessage.error('查询失败：网络错误')
  } finally {
    querying.value = false
  }
}

const buildQueryParams = () => {
  const params = {}
  queryConditions.value.forEach(qc => {
    const key = getFieldKey(qc)

    // 对于不需要值的操作符，直接传递操作符标记
    if (['null', 'notnull', 'true', 'false'].includes(qc.operator)) {
      params[key] = qc.operator
      return
    }

    // between 操作符特殊处理
    if (qc.operator === 'between') {
      if (qc.dataType === 'DateTime') {
        // DateTime: daterange 返回数组
        const value = conditionForm[key]
        if (value && Array.isArray(value) && value.length === 2) {
          params[key] = value
        }
      } else if (qc.dataType === 'Number') {
        // Number: 从两个输入框获取值
        const startValue = conditionForm[key + '_start']
        const endValue = conditionForm[key + '_end']
        if (startValue !== null && startValue !== undefined &&
            endValue !== null && endValue !== undefined) {
          params[key] = [startValue, endValue]
        }
      }
      return
    }

    // 其他操作符：单值处理
    const value = conditionForm[key]
    if (value !== '' && value !== null && value !== undefined) {
      params[key] = value
    }
  })
  return params
}

const handleExportExcel = async () => {
  exporting.value = true
  try {
    const params = buildQueryParams()
    const res = await reportApi.exportReport(selectedReport.value.reportId, { parameters: params })
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
    ElMessage.error('导出失败')
  } finally {
    exporting.value = false
  }
}
</script>

<style scoped>
/* CSS 变量定义 */
.report-query {
  height: 100%;
  --sidebar-width: 280px;
  --sidebar-collapsed-width: 48px;
  --primary-color: #409eff;
  --primary-light: #ecf5ff;
  --bg-page: #f5f7fa;
  --bg-card: #ffffff;
  --bg-hover: #f0f7ff;
  --border-light: #e4e7ed;
  --border-active: #409eff;
  --shadow-card: 0 2px 12px rgba(0, 0, 0, 0.08);
  --shadow-card-hover: 0 4px 16px rgba(0, 0, 0, 0.12);
  --transition-speed: 300ms;

  /* 布局 */
  display: flex;
  background-color: var(--bg-page);
}

/* 侧边栏样式 */
.sidebar {
  width: var(--sidebar-width);
  min-width: var(--sidebar-width);
  background: var(--bg-card);
  border-right: 1px solid var(--border-light);
  display: flex;
  flex-direction: column;
  transition: all var(--transition-speed) ease-in-out;
  overflow: hidden;
}

.sidebar.collapsed {
  width: var(--sidebar-collapsed-width);
  min-width: var(--sidebar-collapsed-width);
}

/* 主内容区域样式 */
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 16px;
  overflow: hidden;
  min-width: 0;
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
</style>
