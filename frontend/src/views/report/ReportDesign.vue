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
            style="height: 300px;"
            placeholder="请输入SQL查询语句，使用 @参数名 格式定义参数，如: WHERE CreateTime >= @StartTime AND CreateTime <= @EndTime"
          />
          <div style="margin-top: 10px;">
            <el-button type="primary" @click="handleParseSql">
              <el-icon><Search /></el-icon>
              解析SQL
            </el-button>
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
        <el-card class="design-card" style="margin-top: 20px;">
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>参数配置</span>
              <el-button type="primary" link size="small" @click="handleAddParameter">
                <el-icon><Plus /></el-icon>
                添加参数
              </el-button>
            </div>
          </template>
          <el-table :data="form.parameters" border>
            <el-table-column prop="name" label="参数名" width="120" />
            <el-table-column prop="label" label="显示名称">
              <template #default="{ row }">
                <el-input v-model="row.label" size="small" />
              </template>
            </el-table-column>
            <el-table-column prop="dataType" label="数据类型" width="120">
              <template #default="{ row }">
                <el-select v-model="row.dataType" size="small">
                  <el-option label="字符串" value="String" />
                  <el-option label="数字" value="Number" />
                  <el-option label="日期" value="DateTime" />
                  <el-option label="下拉选择" value="Select" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column prop="defaultValue" label="默认值">
              <template #default="{ row }">
                <el-input v-model="row.defaultValue" size="small" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="80">
              <template #default="{ $index }">
                <el-button type="danger" link size="small" @click="handleRemoveParameter($index)">
                  删除
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
                    <el-select v-model="form.chartConfig.yField" multiple>
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
import { ref, reactive, onMounted } from 'vue'
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
  enableChart: false,
  chartConfig: {
    chartType: 'bar',
    xField: '',
    yField: [],
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
          yField: [],
          title: ''
        }
      }
    }
  } catch (error) {
    console.error('加载报表失败:', error)
  }
}

const handleDataSourceChange = () => {
  // 数据源切换时清除SQL和字段
  form.sqlQuery = ''
  form.columns = []
  form.parameters = []
}

const handleParseSql = () => {
  // 解析SQL中的参数 @参数名
  const regex = /@(\w+)/g
  const matches = []
  let match

  while ((match = regex.exec(form.sqlQuery)) !== null) {
    const paramName = match[1]
    if (!form.parameters.find(p => p.name === paramName)) {
      form.parameters.push({
        name: paramName,
        label: paramName,
        dataType: 'String',
        defaultValue: ''
      })
    }
  }

  ElMessage.success('解析成功')
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
      // 可以在这里显示预览结果
    }
  } catch (error) {
    console.error('测试查询失败:', error)
  }
}

const handleAddParameter = () => {
  form.parameters.push({
    name: '',
    label: '',
    dataType: 'String',
    defaultValue: ''
  })
}

const handleRemoveParameter = (index) => {
  form.parameters.splice(index, 1)
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
  form.columns.splice(index, 1)
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

    const res = await reportApi.testQuery({
      dataSourceId: form.dataSourceId,
      sql: form.sqlQuery,
      parameters: Object.keys(parameters).length > 0 ? parameters : null
    })

    if (res.success && res.data.length > 0) {
      // 从第一行数据获取字段信息
      const firstRow = res.data[0]
      const detectedFields = []

      for (const [fieldName, value] of Object.entries(firstRow)) {
        // 检测数据类型
        let dataType = 'String'
        if (typeof value === 'number') {
          dataType = Number.isInteger(value) ? 'Number' : 'Number'
        } else if (value instanceof Date) {
          dataType = 'DateTime'
        } else if (typeof value === 'boolean') {
          dataType = 'Boolean'
        }

        detectedFields.push({
          fieldName: fieldName,
          displayName: fieldName, // 默认使用字段名作为显示名称
          dataType: dataType,
          width: 120,
          align: dataType === 'Number' ? 'right' : 'left',
          isVisible: true,
          isSortable: true
        })
      }

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

const handleSave = async () => {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  saving.value = true
  try {
    if (form.reportId) {
      await reportApi.updateReport(form.reportId, form)
    } else {
      await reportApi.createReport(form)
    }
    ElMessage.success('保存成功')
    router.push('/report/list')
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
}

.design-card {
  height: fit-content;
}

.action-buttons {
  margin-top: 20px;
  text-align: right;
}
</style>
