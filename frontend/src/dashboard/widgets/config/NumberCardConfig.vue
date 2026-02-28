<!--
  NumberCardConfig - 数字卡片配置面板
  用于配置数字卡片组件的各种选项
-->
<template>
  <div class="number-card-config">
    <el-form ref="formRef" :model="formData" :rules="rules" label-width="100px" label-position="left">
      <!-- 基本配置 -->
      <div class="config-section">
        <div class="section-title">基本配置</div>

        <el-form-item label="卡片标题" prop="title">
          <el-input v-model="formData.title" placeholder="请输入卡片标题" clearable />
        </el-form-item>

        <el-form-item label="副标题" prop="subtitle">
          <el-input v-model="formData.subtitle" placeholder="请输入副标题" clearable />
        </el-form-item>

        <el-form-item label="颜色方案" prop="colorScheme">
          <el-select v-model="formData.colorScheme" placeholder="选择颜色方案">
            <el-option label="主色" value="primary" />
            <el-option label="成功" value="success" />
            <el-option label="警告" value="warning" />
            <el-option label="危险" value="danger" />
            <el-option label="信息" value="info" />
            <el-option label="自定义" value="custom" />
          </el-select>
        </el-form-item>

        <el-form-item v-if="formData.colorScheme === 'custom'" label="自定义颜色" prop="customColor">
          <el-color-picker v-model="formData.customColor" show-alpha />
        </el-form-item>
      </div>

      <!-- 数值配置 -->
      <div class="config-section">
        <div class="section-title">数值配置</div>

        <el-form-item label="前缀" prop="prefix">
          <el-input v-model="formData.prefix" placeholder="如: ¥、$" clearable />
        </el-form-item>

        <el-form-item label="后缀" prop="suffix">
          <el-input v-model="formData.suffix" placeholder="如: %、件" clearable />
        </el-form-item>

        <el-form-item label="小数位数" prop="decimals">
          <el-input-number
            v-model="formData.decimals"
            :min="0"
            :max="6"
            :step="1"
          />
        </el-form-item>
      </div>

      <!-- 趋势配置 -->
      <div class="config-section">
        <div class="section-title">趋势配置</div>

        <el-form-item label="显示趋势">
          <el-switch v-model="formData.showTrend" />
        </el-form-item>

        <template v-if="formData.showTrend">
          <el-form-item label="趋势字段" prop="trendField">
            <el-select v-model="formData.trendField" placeholder="选择趋势字段" clearable>
              <el-option
                v-for="field in availableFields"
                :key="field.name"
                :label="field.label || field.name"
                :value="field.name"
              />
            </el-select>
          </el-form-item>

          <el-form-item label="趋势标签" prop="trendLabel">
            <el-input v-model="formData.trendLabel" placeholder="如: 环比、同比" clearable />
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

        <el-form-item label="数值字段" prop="field">
          <el-select v-model="formData.field" placeholder="选择数值字段" clearable>
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
import type { FormInstance, FormRules } from 'element-plus'
import type { ColorScheme } from '../NumberCardWidget.vue'

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
 * 配置数据
 */
export interface NumberCardConfigData {
  title: string
  subtitle: string
  colorScheme: ColorScheme
  customColor: string
  prefix: string
  suffix: string
  decimals: number
  showTrend: boolean
  trendField: string
  trendLabel: string
  dataSourceType: 'report' | 'sql'
  reportId?: string
  sql?: string
  field: string
  refreshInterval: number
}

// Props
const props = defineProps({
  /** 配置数据 */
  config: {
    type: Object as PropType<Partial<NumberCardConfigData>>,
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
  }
})

// Emits
const emit = defineEmits<{
  /** 配置变更时触发 */
  'update:config': [config: Partial<NumberCardConfigData>]
  /** 应用配置时触发 */
  apply: [config: NumberCardConfigData]
  /** 重置时触发 */
  reset: []
}>()

// 表单引用
const formRef = ref<FormInstance>()

// 表单数据
const formData = ref<NumberCardConfigData>({
  title: '',
  subtitle: '',
  colorScheme: 'primary',
  customColor: '#409EFF',
  prefix: '',
  suffix: '',
  decimals: 0,
  showTrend: false,
  trendField: 'trend',
  trendLabel: '环比',
  dataSourceType: 'report',
  reportId: '',
  sql: '',
  field: 'value',
  refreshInterval: 0
})

// 表单验证规则
const rules: FormRules<NumberCardConfigData> = {
  title: [
    { required: true, message: '请输入卡片标题', trigger: 'blur' }
  ],
  field: [
    { required: true, message: '请选择数值字段', trigger: 'change' }
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

// 初始化配置
function initConfig(): void {
  if (props.config) {
    Object.assign(formData.value, props.config)
  }
}

// 应用配置
async function handleApply(): Promise<void> {
  try {
    await formRef.value?.validate()
    emit('apply', { ...formData.value })
  } catch (error) {
    console.error('[NumberCardConfig] 配置验证失败:', error)
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
.number-card-config {
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
</style>
