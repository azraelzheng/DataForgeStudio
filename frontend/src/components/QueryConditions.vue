<template>
  <div class="query-conditions">
    <div class="conditions-header">
      <span>查询条件配置</span>
      <div>
        <el-button type="primary" link size="small" @click="handleAutoGenerate">
          <el-icon><MagicStick /></el-icon>
          自动生成
        </el-button>
        <el-button type="primary" link size="small" @click="showFieldSelector">
          <el-icon><Plus /></el-icon>
          添加条件
        </el-button>
      </div>
    </div>

    <el-table :data="modelValue" border max-height="300">
      <el-table-column label="字段" width="150">
        <template #default="{ row }">
          <span>{{ row.displayName }}</span>
        </template>
      </el-table-column>
      <el-table-column label="系统类型" width="100">
        <template #default="{ row }">
          <el-tag size="small">{{ getDataTypeLabel(row.dataType) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="条件关系" width="120">
        <template #default="{ row }">
          <el-select v-model="row.operator" size="small">
            <el-option
              v-for="op in getOperators(row.dataType)"
              :key="op.value"
              :label="op.label"
              :value="op.value"
            />
          </el-select>
        </template>
      </el-table-column>
      <el-table-column label="默认值">
        <template #default="{ row }">
          <template v-if="row.operator !== 'null' && row.operator !== 'notnull'">
            <el-input
              v-if="row.dataType === 'String'"
              v-model="row.defaultValue"
              size="small"
              placeholder="默认值"
            />
            <el-input-number
              v-else-if="row.dataType === 'Number'"
              v-model="row.defaultValue"
              size="small"
              :controls-position="'right'"
            />
            <el-select
              v-else-if="row.dataType === 'DateTime'"
              v-model="row.defaultValue"
              size="small"
            >
              <el-option label="今天" value="today" />
              <el-option label="本周" value="thisWeek" />
              <el-option label="本月" value="thisMonth" />
              <el-option label="最近7天" value="last7Days" />
              <el-option label="最近30天" value="last30Days" />
            </el-select>
            <el-select
              v-else-if="row.dataType === 'Boolean'"
              v-model="row.defaultValue"
              size="small"
            >
              <el-option label="是" :value="true" />
              <el-option label="否" :value="false" />
            </el-select>
          </template>
          <span v-else style="color: #909399;">无需默认值</span>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="80" align="center">
        <template #default="{ $index }">
          <el-button type="danger" link size="small" @click="removeCondition($index)">
            <el-icon><Delete /></el-icon>
          </el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 字段选择器对话框 -->
    <el-dialog v-model="fieldSelectorVisible" title="选择查询字段" width="500px">
      <el-checkbox-group v-model="selectedFieldNames">
        <el-checkbox
          v-for="field in availableFields"
          :key="field.fieldName"
          :label="field.fieldName"
        >
          {{ field.displayName }} ({{ getDataTypeLabel(field.dataType) }})
        </el-checkbox>
      </el-checkbox-group>
      <template #footer>
        <el-button @click="fieldSelectorVisible = false">取消</el-button>
        <el-button type="primary" @click="addSelectedFields">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'

const props = defineProps({
  fields: {
    type: Array,
    default: () => []
  },
  modelValue: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['update:modelValue'])

const fieldSelectorVisible = ref(false)
const selectedFieldNames = ref([])

const availableFields = computed(() => {
  const selectedNames = props.modelValue.map(c => c.fieldName)
  return props.fields.filter(f => !selectedNames.includes(f.fieldName))
})

const operatorMap = {
  String: [
    { label: '等于', value: 'eq' },
    { label: '不等于', value: 'ne' },
    { label: '包含', value: 'like' },
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
    { label: '不为空', value: 'notnull' }
  ],
  DateTime: [
    { label: '等于', value: 'eq' },
    { label: '大于', value: 'gt' },
    { label: '小于', value: 'lt' },
    { label: '大于等于', value: 'ge' },
    { label: '小于等于', value: 'le' },
    { label: '为空', value: 'null' },
    { label: '不为空', value: 'notnull' }
  ],
  Boolean: [
    { label: '等于', value: 'eq' }
  ]
}

const getDataTypeLabel = (type) => {
  const map = {
    'String': '字符串',
    'Number': '数值',
    'DateTime': '日期',
    'Boolean': '布尔'
  }
  return map[type] || type
}

const getOperators = (dataType) => {
  return operatorMap[dataType] || operatorMap.String
}

const showFieldSelector = () => {
  selectedFieldNames.value = []
  fieldSelectorVisible.value = true
}

const addSelectedFields = () => {
  selectedFieldNames.value.forEach(fieldName => {
    const field = props.fields.find(f => f.fieldName === fieldName)
    if (field && !props.modelValue.find(c => c.fieldName === fieldName)) {
      props.modelValue.push({
        fieldName: field.fieldName,
        displayName: field.displayName,
        dataType: field.dataType,
        operator: getOperators(field.dataType)[0].value,
        defaultValue: ''
      })
    }
  })
  fieldSelectorVisible.value = false
}

const removeCondition = (index) => {
  props.modelValue.splice(index, 1)
}

const handleAutoGenerate = () => {
  if (!props.fields || props.fields.length === 0) {
    ElMessage.warning('请先配置字段信息')
    return
  }

  const commonKeywords = ['名称', '姓名', '代码', '编号', '日期', '时间', '状态', '金额', '数量']
  const newConditions = []

  props.fields.forEach(field => {
    const displayName = field.displayName || field.fieldName
    const isCommonField = commonKeywords.some(kw => displayName.includes(kw))

    if (isCommonField && !props.modelValue.find(c => c.fieldName === field.fieldName)) {
      let defaultOperator = 'eq'
      if (field.dataType === 'String') {
        defaultOperator = 'like'
      } else if (field.dataType === 'DateTime') {
        defaultOperator = 'ge'
      } else if (field.dataType === 'Number') {
        defaultOperator = 'ge'
      }

      newConditions.push({
        fieldName: field.fieldName,
        displayName: displayName,
        dataType: field.dataType,
        operator: defaultOperator,
        defaultValue: field.dataType === 'DateTime' ? 'thisMonth' : ''
      })
    }
  })

  newConditions.forEach(c => props.modelValue.push(c))
  ElMessage.success(`已自动生成 ${newConditions.length} 个查询条件`)
}
</script>

<style scoped>
.query-conditions {
  margin-bottom: 20px;
}

.conditions-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
  font-weight: 500;
}
</style>
