<!--
  ChartConfigPanel - 图表配置面板
  用于配置图表组件的各种选项
-->
<template>
  <div class="chart-config-panel">
    <el-form ref="formRef" :model="formData" :rules="rules" label-width="100px" label-position="left">
      <!-- 基本配置 -->
      <div class="config-section">
        <div class="section-title">基本配置</div>

        <el-form-item label="图表标题" prop="title">
          <el-input v-model="formData.title" placeholder="请输入图表标题" clearable />
        </el-form-item>

        <el-form-item label="图表类型" prop="chartType">
          <el-select v-model="formData.chartType" placeholder="选择图表类型" @change="handleChartTypeChange">
            <el-option label="柱状图" value="bar" />
            <el-option label="折线图" value="line" />
            <el-option label="饼图" value="pie" />
            <el-option label="环形图" value="doughnut" />
            <el-option label="仪表盘" value="gauge" />
            <el-option label="雷达图" value="radar" />
          </el-select>
        </el-form-item>

        <el-form-item label="颜色方案" prop="colorScheme">
          <el-select v-model="formData.colorScheme" placeholder="选择颜色方案">
            <el-option label="默认" value="default" />
            <el-option label="商务" value="business" />
            <el-option label="暖色" value="warm" />
            <el-option label="冷色" value="cool" />
            <el-option label="单色" value="monochrome" />
            <el-option label="自定义" value="custom" />
          </el-select>
        </el-form-item>

        <el-form-item v-if="formData.colorScheme === 'custom'" label="自定义颜色" prop="customColors">
          <div class="color-picker-list">
            <div v-for="(color, index) in formData.customColors" :key="index" class="color-picker-item">
              <el-color-picker v-model="formData.customColors[index]" show-alpha />
              <el-button :icon="Delete" size="small" text @click="removeColor(index)" />
            </div>
            <el-button :icon="Plus" size="small" text @click="addColor">添加颜色</el-button>
          </div>
        </el-form-item>
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

        <el-form-item v-if="formData.dataSourceType === 'sql'" label="数据库" prop="connectionId">
          <el-select v-model="formData.connectionId" placeholder="选择数据库" clearable>
            <el-option
              v-for="conn in connectionList"
              :key="conn.id"
              :label="conn.name"
              :value="conn.id"
            />
          </el-select>
        </el-form-item>

        <!-- X 轴配置（非饼图类图表） -->
        <template v-if="!isPieType">
          <el-form-item label="X 轴字段" prop="xField">
            <el-select v-model="formData.xField" placeholder="选择 X 轴字段" clearable>
              <el-option
                v-for="field in availableFields"
                :key="field.name"
                :label="field.label || field.name"
                :value="field.name"
              />
            </el-select>
          </el-form-item>
        </template>

        <!-- Y 轴配置 -->
        <el-form-item label="Y 轴字段" prop="yFields">
          <el-select
            v-model="formData.yFields"
            placeholder="选择 Y 轴字段"
            multiple
            clearable
          >
            <el-option
              v-for="field in availableFields"
              :key="field.name"
              :label="field.label || field.name"
              :value="field.name"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="刷新间隔" prop="refreshInterval">
          <el-input-number
            v-model="formData.refreshInterval"
            :min="0"
            :max="3600"
            :step="10"
            placeholder="秒"
          />
          <span class="unit-text">秒（0 表示不自动刷新）</span>
        </el-form-item>
      </div>

      <!-- 显示配置 -->
      <div class="config-section">
        <div class="section-title">显示配置</div>

        <el-form-item label="显示图例">
          <el-switch v-model="formData.showLegend" />
        </el-form-item>

        <el-form-item label="显示提示框">
          <el-switch v-model="formData.showTooltip" />
        </el-form-item>

        <el-form-item label="启用动画">
          <el-switch v-model="formData.animation" />
        </el-form-item>

        <el-form-item label="容器高度" prop="height">
          <el-input v-model="formData.height" placeholder="如: 300px">
            <template #append>px</template>
          </el-input>
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
import { ref, computed, watch, type PropType } from 'vue'
import { Delete, Plus } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import type { ChartType } from '../ChartWidget.vue'

/**
 * 字段信息
 */
interface FieldInfo {
  name: string
  label?: string
  type?: string
}

/**
 * 报表信息
 */
interface ReportInfo {
  id: string
  name: string
}

/**
 * 数据库连接信息
 */
interface ConnectionInfo {
  id: string
  name: string
}

/**
 * 配置数据
 */
export interface ChartConfigData {
  title: string
  chartType: ChartType
  colorScheme: string
  customColors: string[]
  dataSourceType: 'report' | 'sql'
  reportId?: string
  sql?: string
  connectionId?: string
  xField?: string
  yFields: string[]
  refreshInterval: number
  showLegend: boolean
  showTooltip: boolean
  animation: boolean
  height: string
}

// Props
const props = defineProps({
  /** 配置数据 */
  config: {
    type: Object as PropType<Partial<ChartConfigData>>,
    default: () => ({})
  },
  /** 可用字段列表 */
  availableFields: {
    type: Array as PropType<FieldInfo[]>,
    default: () => []
  },
  /** 报表列表 */
  reportList: {
    type: Array as PropType<ReportInfo[]>,
    default: () => []
  },
  /** 数据库连接列表 */
  connectionList: {
    type: Array as PropType<ConnectionInfo[]>,
    default: () => []
  }
})

// Emits
const emit = defineEmits<{
  /** 配置变更时触发 */
  'update:config': [config: Partial<ChartConfigData>]
  /** 应用配置时触发 */
  apply: [config: ChartConfigData]
  /** 重置时触发 */
  reset: []
}>()

// 表单引用
const formRef = ref<FormInstance>()

// 表单数据
const formData = ref<ChartConfigData>({
  title: '',
  chartType: 'bar',
  colorScheme: 'default',
  customColors: ['#409EFF', '#67C23A', '#E6A23C'],
  dataSourceType: 'report',
  reportId: '',
  sql: '',
  connectionId: '',
  xField: '',
  yFields: [],
  refreshInterval: 0,
  showLegend: true,
  showTooltip: true,
  animation: true,
  height: '300px'
})

// 表单验证规则
const rules: FormRules<ChartConfigData> = {
  chartType: [
    { required: true, message: '请选择图表类型', trigger: 'change' }
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

// 计算属性
const isPieType = computed(() => {
  return ['pie', 'doughnut', 'gauge'].includes(formData.value.chartType)
})

// 初始化配置
function initConfig(): void {
  if (props.config) {
    Object.assign(formData.value, props.config)
  }
}

// 图表类型变化
function handleChartTypeChange(): void {
  // 切换到饼图类型时清空 X 轴字段
  if (isPieType.value) {
    formData.value.xField = ''
  }
}

// 添加颜色
function addColor(): void {
  formData.value.customColors.push('#409EFF')
}

// 移除颜色
function removeColor(index: number): void {
  if (formData.value.customColors.length > 1) {
    formData.value.customColors.splice(index, 1)
  }
}

// 应用配置
async function handleApply(): Promise<void> {
  try {
    await formRef.value?.validate()
    emit('apply', { ...formData.value })
  } catch (error) {
    console.error('[ChartConfigPanel] 配置验证失败:', error)
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
.chart-config-panel {
  padding: 16px;
}

.config-section {
  margin-bottom: 24px;

  &:last-of-type {
    margin-bottom: 16px;
  }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: #303133;
  margin-bottom: 16px;
  padding-bottom: 8px;
  border-bottom: 1px solid #eee;
}

.color-picker-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.color-picker-item {
  display: flex;
  align-items: center;
  gap: 4px;
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
}

// 表单项样式覆盖
:deep(.el-form-item) {
  margin-bottom: 16px;
}

:deep(.el-form-item__label) {
  font-size: 13px;
}

:deep(.el-select),
:deep(.el-input) {
  width: 100%;
}

:deep(.el-color-picker) {
  vertical-align: middle;
}
</style>
