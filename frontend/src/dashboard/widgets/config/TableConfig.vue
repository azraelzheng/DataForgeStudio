<!--
  TableConfig - 数据表格配置面板
  用于配置数据表格组件的各种选项
-->
<template>
  <div class="table-config-panel">
    <el-form ref="formRef" :model="formData" :rules="rules" label-width="100px" label-position="left">
      <!-- 基本配置 -->
      <div class="config-section">
        <div class="section-title">基本配置</div>

        <el-form-item label="表格标题" prop="title">
          <el-input v-model="formData.title" placeholder="请输入表格标题" clearable />
        </el-form-item>

        <el-form-item label="表格尺寸" prop="size">
          <el-radio-group v-model="formData.size">
            <el-radio-button value="large">大</el-radio-button>
            <el-radio-button value="default">中</el-radio-button>
            <el-radio-button value="small">小</el-radio-button>
          </el-radio-group>
        </el-form-item>
      </div>

      <!-- 列配置 -->
      <div class="config-section">
        <div class="section-title">
          <span>列配置</span>
          <el-button :icon="Plus" size="small" text @click="addColumn">添加列</el-button>
        </div>

        <div class="columns-list">
          <div
            v-for="(column, index) in formData.columns"
            :key="index"
            class="column-item"
          >
            <div class="column-header">
              <span>列 {{ index + 1 }}</span>
              <el-button :icon="Delete" size="small" text @click="removeColumn(index)" />
            </div>

            <el-form-item label="属性名" required>
              <el-input v-model="column.prop" placeholder="如: name" />
            </el-form-item>

            <el-form-item label="列标题" required>
              <el-input v-model="column.label" placeholder="如: 名称" />
            </el-form-item>

            <el-form-item label="列宽">
              <el-input v-model="column.width" placeholder="如: 120 或 auto">
                <template #append>px</template>
              </el-input>
            </el-form-item>

            <el-form-item label="对齐方式">
              <el-select v-model="column.align" placeholder="选择对齐方式">
                <el-option label="左对齐" value="left" />
                <el-option label="居中" value="center" />
                <el-option label="右对齐" value="right" />
              </el-select>
            </el-form-item>

            <el-form-item label="格式化">
              <el-select v-model="column.formatter" placeholder="选择格式化类型" clearable>
                <el-option label="文本" value="text" />
                <el-option label="数字" value="number" />
                <el-option label="货币" value="currency" />
                <el-option label="百分比" value="percent" />
                <el-option label="日期" value="date" />
                <el-option label="日期时间" value="datetime" />
              </el-select>
            </el-form-item>

            <el-form-item label="固定列">
              <el-select v-model="column.fixed" placeholder="选择固定位置" clearable>
                <el-option label="左侧" value="left" />
                <el-option label="右侧" value="right" />
              </el-select>
            </el-form-item>
          </div>
        </div>
      </div>

      <!-- 显示配置 -->
      <div class="config-section">
        <div class="section-title">显示配置</div>

        <el-form-item label="斑马纹">
          <el-switch v-model="formData.striped" />
        </el-form-item>

        <el-form-item label="边框">
          <el-switch v-model="formData.border" />
        </el-form-item>

        <el-form-item label="显示序号列">
          <el-switch v-model="formData.showIndex" />
        </el-form-item>

        <el-form-item label="可选择">
          <el-switch v-model="formData.selectable" />
        </el-form-item>

        <el-form-item label="表格高度" prop="tableHeight">
          <el-input v-model="formData.tableHeight" placeholder="如: 400 或 auto">
            <template #append>px</template>
          </el-input>
        </el-form-item>
      </div>

      <!-- 分页配置 -->
      <div class="config-section">
        <div class="section-title">分页配置</div>

        <el-form-item label="显示分页">
          <el-switch v-model="formData.showPagination" />
        </el-form-item>

        <template v-if="formData.showPagination">
          <el-form-item label="每页数量" prop="pageSize">
            <el-input-number
              v-model="formData.pageSize"
              :min="5"
              :max="200"
              :step="5"
            />
          </el-form-item>

          <el-form-item label="分页尺寸" prop="pageSizes">
            <el-select
              v-model="formData.pageSizes"
              multiple
              placeholder="选择分页尺寸选项"
            >
              <el-option label="10" :value="10" />
              <el-option label="20" :value="20" />
              <el-option label="50" :value="50" />
              <el-option label="100" :value="100" />
            </el-select>
          </el-form-item>
        </template>
      </div>

      <!-- 汇总配置 -->
      <div class="config-section">
        <div class="section-title">汇总配置</div>

        <el-form-item label="显示汇总">
          <el-switch v-model="formData.showSummary" />
        </el-form-item>

        <template v-if="formData.showSummary">
          <el-form-item label="汇总方式">
            <el-select v-model="formData.summaryType" placeholder="选择汇总方式">
              <el-option label="求和" value="sum" />
              <el-option label="平均" value="avg" />
              <el-option label="计数" value="count" />
              <el-option label="最大值" value="max" />
              <el-option label="最小值" value="min" />
            </el-select>
          </el-form-item>

          <el-form-item label="汇总列">
            <el-select v-model="formData.summaryColumns" multiple placeholder="选择需要汇总的列">
              <el-option
                v-for="col in formData.columns"
                :key="col.prop"
                :label="col.label"
                :value="col.prop"
              />
            </el-select>
          </el-form-item>
        </template>
      </div>

      <!-- 数据配置 -->
      <div class="config-section">
        <div class="section-title">数据配置</div>

        <el-form-item label="数据源类型" prop="dataSourceType">
          <el-radio-group v-model="formData.dataSourceType">
            <el-radio value="report">报表</el-radio>
            <el-radio value="sql">SQL</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item v-if="formData.dataSourceType === 'report'" label="选择报表" prop="reportId">
          <el-select v-model="formData.reportId" placeholder="选择报表" filterable clearable>
            <el-option
              v-for="report in reportList"
              :key="report.id"
              :label="report.name"
              :value="report.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item v-if="formData.dataSourceType === 'sql'" label="SQL 语句" prop="sql">
          <el-input
            v-model="formData.sql"
            type="textarea"
            :rows="3"
            placeholder="请输入 SQL 语句"
          />
        </el-form-item>

        <el-form-item label="刷新间隔" prop="refreshInterval">
          <el-input-number
            v-model="formData.refreshInterval"
            :min="0"
            :max="3600"
            :step="10"
          />
          <span class="unit-text">秒</span>
        </el-form-item>
      </div>
    </el-form>

    <!-- 操作按钮 -->
    <div class="config-actions">
      <el-button @click="handleReset">重置</el-button>
      <el-button type="primary" @click="handleApply">应用配置</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, type PropType } from 'vue'
import { Plus, Delete } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import type { TableColumnConfig, SummaryType } from '../DataTableWidget.vue'

/**
 * 报表信息
 */
interface ReportInfo {
  id: string
  name: string
}

/**
 * 配置数据
 */
export interface TableConfigData {
  title: string
  size: 'large' | 'default' | 'small'
  columns: TableColumnConfig[]
  striped: boolean
  border: boolean
  showIndex: boolean
  selectable: boolean
  tableHeight: string
  showPagination: boolean
  pageSize: number
  pageSizes: number[]
  showSummary: boolean
  summaryType: SummaryType
  summaryColumns: string[]
  dataSourceType: 'report' | 'sql'
  reportId?: string
  sql?: string
  refreshInterval: number
}

// Props
const props = defineProps({
  /** 配置数据 */
  config: {
    type: Object as PropType<Partial<TableConfigData>>,
    default: () => ({})
  },
  /** 报表列表 */
  reportList: {
    type: Array as PropType<ReportInfo[]>,
    default: () => []
  }
})

// Emits
const emit = defineEmits<{
  /** 配置变更时触发 */
  'update:config': [config: Partial<TableConfigData>]
  /** 应用配置时触发 */
  apply: [config: TableConfigData]
  /** 重置时触发 */
  reset: []
}>()

// 表单引用
const formRef = ref<FormInstance>()

// 表单数据
const formData = ref<TableConfigData>({
  title: '',
  size: 'default',
  columns: [],
  striped: true,
  border: false,
  showIndex: false,
  selectable: false,
  tableHeight: '400',
  showPagination: true,
  pageSize: 20,
  pageSizes: [10, 20, 50, 100],
  showSummary: false,
  summaryType: 'sum',
  summaryColumns: [],
  dataSourceType: 'report',
  reportId: '',
  sql: '',
  refreshInterval: 0
})

// 表单验证规则
const rules: FormRules<TableConfigData> = {
  title: [
    { required: true, message: '请输入表格标题', trigger: 'blur' }
  ],
  columns: [
    {
      required: true,
      validator: (rule, value, callback) => {
        if (!value || value.length === 0) {
          callback(new Error('请至少添加一列'))
        } else {
          callback()
        }
      },
      trigger: 'change'
    }
  ],
  dataSourceType: [
    { required: true, message: '请选择数据源类型', trigger: 'change' }
  ],
  reportId: [
    {
      required: true,
      message: '请选择报表',
      trigger: 'change',
      validator: (rule, value, callback) => {
        if (formData.value.dataSourceType === 'report' && !value) {
          callback(new Error('请选择报表'))
        } else {
          callback()
        }
      }
    }
  ],
  sql: [
    {
      required: true,
      message: '请输入 SQL 语句',
      trigger: 'blur',
      validator: (rule, value, callback) => {
        if (formData.value.dataSourceType === 'sql' && !value) {
          callback(new Error('请输入 SQL 语句'))
        } else {
          callback()
        }
      }
    }
  ]
}

// 添加列
function addColumn(): void {
  formData.value.columns.push({
    prop: '',
    label: '',
    width: undefined,
    align: 'left',
    formatter: 'text'
  })
}

// 移除列
function removeColumn(index: number): void {
  if (formData.value.columns.length > 1) {
    formData.value.columns.splice(index, 1)
  }
}

// 初始化配置
function initConfig(): void {
  if (props.config) {
    Object.assign(formData.value, props.config)
    // 确保 columns 是数组
    if (!formData.value.columns || formData.value.columns.length === 0) {
      addColumn()
    }
  } else {
    addColumn()
  }
}

// 应用配置
async function handleApply(): Promise<void> {
  try {
    await formRef.value?.validate()
    emit('apply', { ...formData.value })
  } catch (error) {
    console.error('[TableConfig] 配置验证失败:', error)
  }
}

// 重置
function handleReset(): void {
  formRef.value?.resetFields()
  emit('reset')
}

// 监听配置变更
watch(formData, (newData) => {
  emit('update:config', { ...newData })
}, { deep: true })

// 监听 props.config 变化
watch(() => props.config, initConfig, { immediate: true, deep: true })

// 暴露方法
defineExpose({
  validate: () => formRef.value?.validate(),
  resetFields: () => formRef.value?.resetFields()
})
</script>

<style scoped lang="scss">
.table-config-panel {
  padding: 16px;
  max-height: 600px;
  overflow-y: auto;
}

.config-section {
  margin-bottom: 24px;

  &:last-of-type {
    margin-bottom: 16px;
  }
}

.section-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 14px;
  font-weight: 500;
  color: #303133;
  margin-bottom: 16px;
  padding-bottom: 8px;
  border-bottom: 1px solid #eee;
}

.columns-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.column-item {
  padding: 12px;
  border: 1px solid #eee;
  border-radius: 4px;
  background: #fafafa;

  .column-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 12px;
    font-size: 13px;
    font-weight: 500;
    color: #606266;
  }

  :deep(.el-form-item) {
    margin-bottom: 12px;

    &:last-child {
      margin-bottom: 0;
    }
  }
}

.unit-text {
  margin-left: 8px;
  font-size: 12px;
  color: #909399;
}

.config-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 16px;
  border-top: 1px solid #eee;
  position: sticky;
  bottom: 0;
  background: #fff;
}

:deep(.el-form-item) {
  margin-bottom: 16px;
}

:deep(.el-form-item__label) {
  font-size: 13px;
}

:deep(.el-select),
:deep(.el-input),
:deep(.el-input-number) {
  width: 100%;
}
</style>
