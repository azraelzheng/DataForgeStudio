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
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>SQL查询</span>
              <div>
                <el-button type="primary" link size="small" @click="handleParseSql">
                  <el-icon><Search /></el-icon>
                  解析参数
                </el-button>
              </div>
            </div>
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

        <!-- 参数配置 -->
        <el-card class="design-card" style="margin-top: 20px;" v-if="form.parameters.length > 0">
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>参数配置</span>
            </div>
          </template>
          <el-table :data="form.parameters" border size="small">
            <el-table-column prop="name" label="参数名" width="100" />
            <el-table-column prop="label" label="显示名称" width="120">
              <template #default="{ row }">
                <el-input v-model="row.label" size="small" />
              </template>
            </el-table-column>
            <el-table-column prop="dataType" label="类型" width="100">
              <template #default="{ row }">
                <el-select v-model="row.dataType" size="small">
                  <el-option label="字符串" value="String" />
                  <el-option label="数字" value="Number" />
                  <el-option label="日期" value="DateTime" />
                  <el-option label="下拉" value="Select" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column prop="defaultValue" label="默认值">
              <template #default="{ row }">
                <el-input v-model="row.defaultValue" size="small" :placeholder="row.dataType === 'Select' ? '选项用换行分隔' : ''" />
              </template>
            </el-table-column>
            <el-table-column label="" width="50">
              <template #default="{ $index }">
                <el-button type="danger" link size="small" @click="handleRemoveParameter($index)">
                  <el-icon><Delete /></el-icon>
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>

      <!-- 右侧：字段配置和预览 -->
      <el-col :span="16">
        <!-- 字段配置 -->
        <el-card class="design-card">
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>字段配置</span>
              <div>
                <el-button type="primary" @click="handleAddField">
                  <el-icon><Plus /></el-icon>
                  添加字段
                </el-button>
                <el-button @click="handleAutoDetectFields">
                  <el-icon><MagicStick /></el-icon>
                  自动识别
                </el-button>
              </div>
            </div>
          </template>
          <el-table :data="form.columns" border max-height="300">
            <el-table-column prop="fieldName" label="字段名" width="150">
              <template #default="{ row }">
                <el-input v-model="row.fieldName" size="small" />
              </template>
            </el-table-column>
            <el-table-column prop="displayName" label="显示名称" width="150">
              <template #default="{ row }">
                <el-input v-model="row.displayName" size="small" />
              </template>
            </el-table-column>
            <el-table-column prop="dataType" label="数据类型" width="120">
              <template #default="{ row }">
                <el-select v-model="row.dataType" size="small">
                  <el-option label="字符串" value="String" />
                  <el-option label="数字" value="Number" />
                  <el-option label="日期" value="DateTime" />
                  <el-option label="布尔" value="Boolean" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column prop="width" label="宽度" width="100">
              <template #default="{ row }">
                <el-input-number v-model="row.width" :min="50" :max="500" :step="10" size="small" />
              </template>
            </el-table-column>
            <el-table-column prop="align" label="对齐" width="100">
              <template #default="{ row }">
                <el-select v-model="row.align" size="small">
                  <el-option label="左对齐" value="left" />
                  <el-option label="居中" value="center" />
                  <el-option label="右对齐" value="right" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column prop="isVisible" label="可见" width="80" align="center">
              <template #default="{ row }">
                <el-switch v-model="row.isVisible" />
              </template>
            </el-table-column>
            <el-table-column prop="isSortable" label="排序" width="80" align="center">
              <template #default="{ row }">
                <el-switch v-model="row.isSortable" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="100">
              <template #default="{ $index }">
                <el-button type="danger" link size="small" @click="handleRemoveField($index)">
                  删除
                </el-button>
              </template>
            </el-table-column>
          </el-table>
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
import { ref, reactive, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { reportApi, dataSourceApi } from '../../api/request'
import SqlEditor from '../../components/SqlEditor.vue'

const router = useRouter()
const route = useRoute()

const formRef = ref()
const saving = ref(false)
const dataSources = ref([])

const form = reactive({
  reportId: null,
  reportName: '',
  reportCategory: '',
  description: '',
  dataSourceId: null,
  sqlQuery: '',
  parameters: [],
  columns: [],
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
      Object.assign(form, res.data)
      if (!form.chartConfig) {
        form.chartConfig = {
          chartType: 'bar',
          xField: '',
          yFields: [],
          title: ''
        }
      }
      if (!form.queryConditions) {
        form.queryConditions = []
      }
    }
  } catch (error) {
    console.error('加载报表失败:', error)
  }
}

const handleDataSourceChange = async () => {
  // 数据源切换时清除SQL和字段
  form.sqlQuery = ''
  form.columns = []
  form.parameters = []
  form.queryConditions = []

  // 预加载表结构
  if (form.dataSourceId && sqlEditorRef.value) {
    try {
      await sqlEditorRef.value.preloadTableStructure(form.dataSourceId)
    } catch (error) {
      console.warn('预加载表结构失败:', error)
    }
  }
}

const handleParseSql = () => {
  // 解析SQL中的参数 @参数名
  const regex = /@(\w+)/g
  let match
  let addedCount = 0

  while ((match = regex.exec(form.sqlQuery)) !== null) {
    const paramName = match[1]
    if (!form.parameters.find(p => p.name === paramName)) {
      form.parameters.push({
        name: paramName,
        label: paramName,
        dataType: 'String',
        defaultValue: ''
      })
      addedCount++
    }
  }

  if (addedCount > 0) {
    ElMessage.success(`解析成功，添加了 ${addedCount} 个参数`)
  } else {
    ElMessage.info('没有发现新的参数')
  }
}

const handleRemoveParameter = (index) => {
  form.parameters.splice(index, 1)
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
    // 准备参数
    const parameters = {}
    form.parameters.forEach(p => {
      if (p.defaultValue) {
        parameters[p.name] = p.defaultValue
      }
    })

    const res = await reportApi.testQuery({
      dataSourceId: form.dataSourceId,
      sql: form.sqlQuery,
      parameters: Object.keys(parameters).length > 0 ? parameters : null
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

const handleAutoDetectFields = async () => {
  if (!form.dataSourceId) {
    ElMessage.warning('请先选择数据源')
    return
  }
  if (!form.sqlQuery) {
    ElMessage.warning('请先输入SQL查询语句')
    return
  }

  try {
    // 准备参数（使用默认值）
    const parameters = {}
    form.parameters.forEach(p => {
      if (p.defaultValue) {
        parameters[p.name] = p.defaultValue
      }
    })

    // 调用新的 getQuerySchema API 只获取字段结构
    const res = await reportApi.getQuerySchema({
      dataSourceId: form.dataSourceId,
      sql: form.sqlQuery,
      parameters: Object.keys(parameters).length > 0 ? parameters : null
    })

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

  saving.value = true
  try {
    const saveData = {
      ...form,
      chartConfig: form.enableChart ? form.chartConfig : null,
      queryConditions: form.queryConditions.length > 0 ? form.queryConditions : null
    }

    if (form.reportId) {
      await reportApi.updateReport(form.reportId, saveData)
    } else {
      await reportApi.createReport(saveData)
    }
    ElMessage.success('保存成功')
    router.push('/report/design')
  } catch (error) {
    console.error('保存失败:', error)
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
</style>
