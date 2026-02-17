<template>
  <div class="report-design">
    <el-row :gutter="20">
      <!-- 左侧：基本信息和数据源 -->
      <el-col :span="8">
        <el-card class="design-card">
          <template #header>
            <span>基本信息</span>
          </template>
          <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
            <el-form-item label="报表名称" prop="reportName">
              <el-input v-model="form.reportName" placeholder="请输入报表名称" />
            </el-form-item>
            <el-form-item label="分类" prop="reportCategory">
              <el-select v-model="form.reportCategory" placeholder="请选择分类">
                <el-option label="销售" value="销售" />
                <el-option label="库存" value="库存" />
                <el-option label="财务" value="财务" />
                <el-option label="其他" value="其他" />
              </el-select>
            </el-form-item>
            <el-form-item label="描述">
              <el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入描述" />
            </el-form-item>
            <el-form-item label="数据源" prop="dataSourceId">
              <el-select v-model="form.dataSourceId" placeholder="请选择数据源" @change="handleDataSourceChange">
                <el-option
                  v-for="ds in dataSources"
                  :key="ds.dataSourceId"
                  :label="ds.dataSourceName"
                  :value="ds.dataSourceId"
                />
              </el-select>
            </el-form-item>
          </el-form>
        </el-card>

        <!-- SQL编辑器 -->
        <el-card class="design-card" style="margin-top: 20px;">
          <template #header>
            <span>SQL查询</span>
          </template>
          <SqlEditor
            ref="sqlEditorRef"
            v-model="form.sqlQuery"
            :data-source-id="form.dataSourceId"
            style="height: 300px;"
          />
          <div style="margin-top: 10px;">
            <el-button @click="handleTestQuery">
              <el-icon><Connection /></el-icon>
              测试查询
            </el-button>
            <el-button @click="formatSQL">
              <el-icon><MagicStick /></el-icon>
              格式化SQL
            </el-button>
          </div>
        </el-card>
      </el-col>

      <!-- 右侧：字段配置和预览 -->
      <el-col :span="16">
        <!-- 字段配置 -->
        <el-card class="design-card">
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>字段配置 ({{ form.columns.length }} 个字段)</span>
              <div>
                <el-button type="primary" @click="handleAddField">
                  <el-icon><Plus /></el-icon>
                  添加字段
                </el-button>
                <el-button type="danger" @click="handleClearFields" :disabled="form.columns.length === 0">
                  <el-icon><Delete /></el-icon>
                  清空字段
                </el-button>
                <el-button @click="handleAutoDetectFields">
                  <el-icon><MagicStick /></el-icon>
                  自动识别
                </el-button>
              </div>
            </div>
          </template>
          <!-- 使用虚拟滚动表格提升性能 -->
          <div class="virtual-table-container">
            <el-auto-resizer>
              <template #default="{ height, width }">
                <el-table-v2
                  :columns="fieldColumns"
                  :data="form.columns"
                  :width="width"
                  :height="Math.max(300, Math.min(500, form.columns.length * 45))"
                  :row-height="45"
                  :header-height="40"
                  fixed
                />
              </template>
            </el-auto-resizer>
          </div>
        </el-card>

        <!-- 查询条件配置 -->
        <el-card class="design-card" style="margin-top: 20px;">
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>查询条件配置</span>
              <el-button type="primary" link size="small" @click="handleAddQueryCondition" :disabled="form.columns.length === 0">
                <el-icon><Plus /></el-icon>
                添加条件
              </el-button>
            </div>
          </template>
          <div v-if="form.queryConditions.length > 0">
            <el-table :data="form.queryConditions" border size="small">
              <el-table-column label="字段" width="150">
                <template #default="{ row }">
                  <el-select
                    v-model="row.fieldName"
                    size="small"
                    placeholder="选择或输入关键字"
                    filterable
                    clearable
                    @change="onConditionFieldChange(row)"
                  >
                    <el-option
                      v-for="col in form.columns"
                      :key="col.fieldName"
                      :label="col.displayName"
                      :value="col.fieldName"
                    />
                  </el-select>
                </template>
              </el-table-column>
              <el-table-column label="显示名" width="120">
                <template #default="{ row }">
                  <el-input v-model="row.displayName" size="small" placeholder="显示名称" />
                </template>
              </el-table-column>
              <el-table-column label="比较方式" width="120">
                <template #default="{ row }">
                  <el-select v-model="row.operator" size="small" placeholder="选择">
                    <el-option
                      v-for="op in getOperatorsForField(row.fieldName)"
                      :key="op.value"
                      :label="op.label"
                      :value="op.value"
                    />
                  </el-select>
                </template>
              </el-table-column>
              <el-table-column label="默认值">
                <template #default="{ row }">
                  <el-input
                    v-if="!['null', 'notnull'].includes(row.operator)"
                    v-model="row.defaultValue"
                    size="small"
                    placeholder="默认值"
                  />
                  <span v-else style="color: #909399; font-size: 12px;">-</span>
                </template>
              </el-table-column>
              <el-table-column label="" width="50">
                <template #default="{ $index }">
                  <el-button type="danger" link size="small" @click="handleRemoveQueryCondition($index)">
                    <el-icon><Delete /></el-icon>
                  </el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>
          <el-empty v-else description="配置字段后可添加查询条件" :image-size="60" />
        </el-card>

        <!-- 图表配置 -->
        <el-card class="design-card" style="margin-top: 20px;">
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>图表配置</span>
              <el-switch v-model="form.enableChart" active-text="启用图表" />
            </div>
          </template>
          <div v-if="form.enableChart">
            <el-form :model="form.chartConfig" label-width="100px">
              <el-row :gutter="20">
                <el-col :span="12">
                  <el-form-item label="图表类型">
                    <el-select v-model="form.chartConfig.chartType">
                      <el-option label="柱状图" value="bar" />
                      <el-option label="折线图" value="line" />
                      <el-option label="饼图" value="pie" />
                      <el-option label="环形图" value="doughnut" />
                    </el-select>
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="X轴字段">
                    <el-select v-model="form.chartConfig.xField">
                      <el-option
                        v-for="col in form.columns"
                        :key="col.fieldName"
                        :label="col.displayName"
                        :value="col.fieldName"
                      />
                    </el-select>
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="Y轴字段">
                    <el-select v-model="form.chartConfig.yFields" multiple>
                      <el-option
                        v-for="col in form.columns.filter(c => c.dataType === 'Number')"
                        :key="col.fieldName"
                        :label="col.displayName"
                        :value="col.fieldName"
                      />
                    </el-select>
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item label="图表标题">
                    <el-input v-model="form.chartConfig.title" />
                  </el-form-item>
                </el-col>
              </el-row>
            </el-form>
          </div>
          <el-empty v-else description="启用图表后可进行配置" />
        </el-card>

        <!-- 操作按钮 -->
        <div class="action-buttons">
          <el-button @click="router.back()">取消</el-button>
          <el-button type="primary" @click="handleSave" :loading="saving">
            <el-icon><Check /></el-icon>
            保存报表
          </el-button>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed, h } from 'vue'
import { ElInput, ElSelect, ElInputNumber, ElSwitch, ElButton } from 'element-plus'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { reportApi, dataSourceApi } from '../../api/request'
import SqlEditor from '../../components/SqlEditor.vue'

const router = useRouter()
const route = useRoute()

const formRef = ref()
const saving = ref(false)
const dataSources = ref([])
const availableFields = ref([])  // SQL 解析的字段列表，用于字段名下拉筛选

const form = reactive({
  reportId: null,
  reportName: '',
  reportCategory: '',
  description: '',
  dataSourceId: null,
  sqlQuery: '',
  columns: [],
  parameters: [],  // 添加 parameters 字段
  queryConditions: [],
  enableChart: false,
  chartConfig: {
    chartType: 'bar',
    xField: '',
    yFields: [],
    title: ''
  }
})

const rules = {
  reportName: [{ required: true, message: '请输入报表名称', trigger: 'blur' }],
  reportCategory: [{ required: true, message: '请选择分类', trigger: 'change' }],
  dataSourceId: [{ required: true, message: '请选择数据源', trigger: 'change' }],
  sqlQuery: [{ required: true, message: '请输入SQL查询语句', trigger: 'blur' }]
}

// 虚拟滚动表格列定义
const fieldColumns = computed(() => [
  {
    key: 'fieldName',
    title: '字段名',
    width: 150,
    cellRenderer: ({ rowData }) => h(ElSelect, {
      modelValue: rowData.fieldName,
      'onUpdate:modelValue': (val) => {
        rowData.fieldName = val
        // 自动填充显示名和数据类型
        if (val) {
          const field = availableFields.value.find(f => f.fieldName === val)
          if (field) {
            rowData.displayName = field.displayName
            rowData.dataType = field.dataType
            rowData.align = field.dataType === 'Number' ? 'right' : 'left'
          }
        }
      },
      size: 'small',
      filterable: true,
      clearable: true,
      placeholder: '输入关键字筛选',
      options: availableFields.value.map(f => ({
        label: f.displayName,
        value: f.fieldName
      }))
    })
  },
  {
    key: 'displayName',
    title: '显示名称',
    width: 150,
    cellRenderer: ({ rowData }) => h(ElInput, {
      modelValue: rowData.displayName,
      'onUpdate:modelValue': (val) => { rowData.displayName = val },
      size: 'small'
    })
  },
  {
    key: 'dataType',
    title: '数据类型',
    width: 120,
    cellRenderer: ({ rowData }) => h(ElSelect, {
      modelValue: rowData.dataType,
      'onUpdate:modelValue': (val) => { rowData.dataType = val },
      size: 'small',
      options: [
        { label: '字符串', value: 'String' },
        { label: '数字', value: 'Number' },
        { label: '日期', value: 'DateTime' },
        { label: '布尔', value: 'Boolean' }
      ]
    })
  },
  {
    key: 'width',
    title: '宽度',
    width: 100,
    cellRenderer: ({ rowData }) => h(ElInputNumber, {
      modelValue: rowData.width,
      'onUpdate:modelValue': (val) => { rowData.width = val },
      min: 50,
      max: 500,
      step: 10,
      size: 'small',
      controls: false
    })
  },
  {
    key: 'align',
    title: '对齐',
    width: 100,
    cellRenderer: ({ rowData }) => h(ElSelect, {
      modelValue: rowData.align,
      'onUpdate:modelValue': (val) => { rowData.align = val },
      size: 'small',
      options: [
        { label: '左对齐', value: 'left' },
        { label: '居中', value: 'center' },
        { label: '右对齐', value: 'right' }
      ]
    })
  },
  {
    key: 'isVisible',
    title: '可见',
    width: 80,
    align: 'center',
    cellRenderer: ({ rowData }) => h(ElSwitch, {
      modelValue: rowData.isVisible,
      'onUpdate:modelValue': (val) => { rowData.isVisible = val }
    })
  },
  {
    key: 'isSortable',
    title: '排序',
    width: 80,
    align: 'center',
    cellRenderer: ({ rowData }) => h(ElSwitch, {
      modelValue: rowData.isSortable,
      'onUpdate:modelValue': (val) => { rowData.isSortable = val }
    })
  },
  {
    key: 'actions',
    title: '操作',
    width: 80,
    cellRenderer: ({ rowIndex }) => h(ElButton, {
      type: 'danger',
      link: true,
      size: 'small',
      onClick: () => handleRemoveField(rowIndex)
    }, () => '删除')
  }
])

// SqlEditor 组件引用
const sqlEditorRef = ref(null)

// 根据字段类型获取可用的操作符
const operatorOptions = {
  String: [
    { label: '等于', value: 'eq' },
    { label: '不等于', value: 'ne' },
    { label: '包含', value: 'like' },
    { label: '开头是', value: 'start' },
    { label: '结尾是', value: 'end' },
    { label: '为空', value: 'null' },
    { label: '不为空', value: 'notnull' }
  ],
  Number: [
    { label: '等于', value: 'eq' },
    { label: '不等于', value: 'ne' },
    { label: '大于', value: 'gt' },
    { label: '小于', value: 'lt' },
    { label: '大于等于', value: 'ge' },
    { label: '小于等于', value: 'le' },
    { label: '为空', value: 'null' },
    { label: '不为空', value: 'notnull' },
    { label: '两者之间', value: 'between' }
  ],
  DateTime: [
    { label: '等于', value: 'eq' },
    { label: '不等于', value: 'ne' },
    { label: '之后', value: 'gt' },
    { label: '之前', value: 'lt' },
    { label: '不晚于', value: 'le' },
    { label: '不早于', value: 'ge' },
    { label: '为空', value: 'null' },
    { label: '不为空', value: 'notnull' },
    { label: '两者之间', value: 'between' }
  ],
  Boolean: [
    { label: '等于', value: 'eq' },
    { label: '为真', value: 'true' },
    { label: '为假', value: 'false' }
  ]
}

// 获取字段对应的操作符列表
const getOperatorsForField = (fieldName) => {
  const field = form.columns.find(c => c.fieldName === fieldName)
  if (!field) return operatorOptions.String
  return operatorOptions[field.dataType] || operatorOptions.String
}

// 格式化 SQL
const formatSQL = () => {
  if (sqlEditorRef.value) {
    sqlEditorRef.value.formatSQL()
    ElMessage.success('SQL 格式化成功')
  }
}

onMounted(async () => {
  // 加载数据源列表
  const res = await dataSourceApi.getDataSources()
  if (res.success) {
    dataSources.value = res.data.items || res.data
    // Debug logging
    console.log('=== Datasources loaded ===')
    dataSources.value.forEach(ds => {
      console.log(`  ID: ${ds.dataSourceId} (${typeof ds.dataSourceId}), Name: ${ds.dataSourceName}`)
    })
  }

  // 如果有ID，则加载报表
  const reportId = route.query.id
  if (reportId) {
    loadReport(reportId)
  }
})

const loadReport = async (id) => {
  try {
    const res = await reportApi.getReport(id)
    if (res.success) {
      const data = res.data

      // 先赋值基本字段（避免逐个触发响应式）
      form.reportId = data.reportId
      form.reportName = data.reportName || ''
      form.reportCategory = data.reportCategory || ''
      form.description = data.description || ''
      form.dataSourceId = data.dataSourceId
      form.sqlQuery = data.sqlStatement || data.sqlQuery || ''
      form.parameters = data.parameters || []
      form.enableChart = data.enableChart || false

      // 处理 chartConfig
      form.chartConfig = data.chartConfig || {
        chartType: 'bar',
        xField: '',
        yFields: [],
        title: ''
      }

      // 处理 queryConditions
      form.queryConditions = data.queryConditions || []

      // 最后赋值 columns（最大的数组，一次性赋值减少渲染次数）
      const columns = data.columns || data.fields || []
      form.columns = columns

      // 同时填充 availableFields 供字段名下拉筛选使用
      if (columns.length > 0) {
        availableFields.value = columns.map(f => ({
          fieldName: f.fieldName,
          displayName: f.displayName || f.fieldName,
          dataType: f.dataType
        }))
      }
    }
  } catch (error) {
    console.error('加载报表失败:', error)
  }
}

const handleDataSourceChange = async () => {
  // Debug logging
  console.log('=== DataSource changed ===')
  console.log('New form.dataSourceId:', form.dataSourceId, typeof form.dataSourceId)

  // 数据源切换时清除SQL和字段
  form.sqlQuery = ''
  form.columns = []
  form.queryConditions = []
  availableFields.value = []  // 清空可用字段列表

  // 注意：已移除预加载表结构功能，避免加载大量表数据影响性能
}

const handleTestQuery = async () => {
  if (!form.dataSourceId) {
    ElMessage.warning('请先选择数据源')
    return
  }
  if (!form.sqlQuery) {
    ElMessage.warning('请先输入SQL查询语句')
    return
  }

  try {
    const res = await reportApi.testQuery({
      dataSourceId: form.dataSourceId,
      sql: form.sqlQuery
    })

    if (res.success) {
      ElMessage.success(`查询成功，返回 ${res.data.length} 条记录`)
    }
  } catch (error) {
    console.error('测试查询失败:', error)
  }
}

const handleAddField = () => {
  form.columns.push({
    fieldName: '',
    displayName: '',
    dataType: 'String',
    width: 120,
    align: 'left',
    isVisible: true,
    isSortable: true
  })
}

const handleRemoveField = (index) => {
  const fieldName = form.columns[index].fieldName
  form.columns.splice(index, 1)
  // 同时移除相关的查询条件
  form.queryConditions = form.queryConditions.filter(qc => qc.fieldName !== fieldName)
}

// 清空所有字段
const handleClearFields = () => {
  form.columns = []
  form.queryConditions = []  // 同时清空相关查询条件
}

const handleAutoDetectFields = async () => {
  if (!form.dataSourceId) {
    ElMessage.warning('请先选择数据源')
    return
  }
  if (!form.sqlQuery) {
    ElMessage.warning('请先输入SQL查询语句')
    return
  }

  // Debug logging
  console.log('=== Auto-detect called ===')
  console.log('form.dataSourceId:', form.dataSourceId, typeof form.dataSourceId)
  console.log('form.sqlQuery:', form.sqlQuery)

  try {
    // 调用 getQuerySchema API 只获取字段结构
    const requestData = {
      dataSourceId: form.dataSourceId,
      sql: form.sqlQuery
    }
    console.log('API request data:', JSON.stringify(requestData))

    const res = await reportApi.getQuerySchema(requestData)

    console.log('API response success:', res.success)
    console.log('API response first 3 fields:', res.data?.slice(0, 3))

    if (res.success && res.data.length > 0) {
      // 直接使用后端返回的字段元数据
      const detectedFields = res.data.map(field => ({
        fieldName: field.fieldName,
        displayName: field.fieldName,
        dataType: field.systemDataType,  // 直接使用后端映射的类型
        width: 120,
        align: field.systemDataType === 'Number' ? 'right' : 'left',
        isVisible: true,
        isSortable: true
      }))

      form.columns = detectedFields
      // 同时更新 availableFields 供字段名下拉筛选使用
      availableFields.value = detectedFields.map(f => ({
        fieldName: f.fieldName,
        displayName: f.displayName,
        dataType: f.dataType
      }))
      ElMessage.success(`自动识别成功，检测到 ${detectedFields.length} 个字段`)
    } else {
      ElMessage.warning('查询结果为空，无法自动识别字段')
    }
  } catch (error) {
    console.error('自动识别字段失败:', error)
    ElMessage.error('自动识别字段失败: ' + (error.message || '未知错误'))
  }
}

// 添加查询条件
const handleAddQueryCondition = () => {
  if (form.columns.length === 0) {
    ElMessage.warning('请先配置字段')
    return
  }
  form.queryConditions.push({
    fieldName: '',
    displayName: '',
    dataType: 'String',
    operator: 'eq',
    defaultValue: ''
  })
}

// 删除查询条件
const handleRemoveQueryCondition = (index) => {
  form.queryConditions.splice(index, 1)
}

// 当选择字段时，自动填充显示名和数据类型
const onConditionFieldChange = (row) => {
  const field = form.columns.find(c => c.fieldName === row.fieldName)
  if (field) {
    row.displayName = field.displayName
    row.dataType = field.dataType
    // 重置操作符为默认值
    const operators = getOperatorsForField(row.fieldName)
    row.operator = operators[0]?.value || 'eq'
  }
}

const handleSave = async () => {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  // Debug logging
  console.log('=== Save report ===')
  console.log('form.dataSourceId:', form.dataSourceId)
  console.log('form.reportId:', form.reportId)
  console.log('form.columns count:', form.columns.length)

  saving.value = true
  try {
    const saveData = {
      ...form,
      parameters: [],  // 添加必需的 parameters 字段
      chartConfig: form.enableChart ? form.chartConfig : null,
      queryConditions: form.queryConditions.length > 0 ? form.queryConditions : null
    }
    console.log('Save data:', JSON.stringify(saveData, null, 2).substring(0, 500))

    if (form.reportId) {
      console.log('Updating existing report, ID:', form.reportId)
      await reportApi.updateReport(form.reportId, saveData)
    } else {
      console.log('Creating new report')
      await reportApi.createReport(saveData)
    }
    ElMessage.success('保存成功')
    router.push('/report/design')
  } catch (error) {
    console.error('保存失败:', error)
    console.error('Error details:', error.response?.data || error.message)
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.report-design {
  height: 100%;
  overflow-y: auto;
  padding-bottom: 20px;
}

.design-card {
  height: fit-content;
}

.action-buttons {
  margin-top: 20px;
  text-align: right;
}

.virtual-table-container {
  width: 100%;
  min-height: 300px;
}

/* 虚拟表格内组件样式调整 */
:deep(.el-table-v2__cell) {
  padding: 4px 8px;
}

:deep(.el-table-v2__cell .el-input__wrapper) {
  padding: 1px 8px;
}

:deep(.el-table-v2__cell .el-select) {
  width: 100%;
}
</style>
