<!--
  DataTableWidget - 数据表格组件
  基于 Element Plus Table，支持分页、汇总
-->
<template>
  <div class="data-table-widget">
    <!-- 表格头部 -->
    <div v-if="title" class="table-header">
      <h3 class="table-title">{{ title }}</h3>
      <div class="table-actions">
        <el-button
          v-if="showRefreshButton"
          :icon="Refresh"
          circle
          size="small"
          :loading="loading"
          @click="handleRefresh"
        />
        <el-button
          v-if="showExportButton"
          :icon="Download"
          circle
          size="small"
          @click="handleExport"
        />
      </div>
    </div>

    <!-- 表格容器 -->
    <div class="table-container">
      <el-table
        :data="displayData"
        :stripe="striped"
        :border="border"
        :size="size"
        :height="tableHeight"
        :max-height="maxHeight"
        :show-summary="showSummary"
        :summary-method="getSummaryMethod"
        :empty-text="emptyText"
        v-bind="$attrs"
        @selection-change="handleSelectionChange"
        @sort-change="handleSortChange"
        @filter-change="handleFilterChange"
      >
        <!-- 选择列 -->
        <el-table-column
          v-if="selectable"
          type="selection"
          width="55"
          fixed
        />

        <!-- 序号列 -->
        <el-table-column
          v-if="showIndex"
          type="index"
          label="序号"
          width="60"
          fixed
        />

        <!-- 动态列 -->
        <template v-for="column in columns" :key="column.prop">
          <el-table-column
            :prop="column.prop"
            :label="column.label"
            :width="column.width"
            :min-width="column.minWidth"
            :fixed="column.fixed"
            :align="column.align || 'left'"
            :sortable="column.sortable ? 'custom' : false"
            :filters="column.filters"
            :filter-multiple="column.filterMultiple !== false"
            :filtered-value="column.filteredValue"
          >
            <template #default="scope">
              <slot
                :name="`column-${column.prop}`"
                :row="scope.row"
                :column="column"
                :$index="scope.$index"
              >
                <span>{{ formatCellData(scope.row[column.prop], column) }}</span>
              </slot>
            </template>

            <!-- 列头插槽 -->
            <template v-if="$slots[`header-${column.prop}`]" #header="scope">
              <slot
                :name="`header-${column.prop}`"
                :column="scope.column"
                :$index="scope.$index"
              />
            </template>
          </el-table-column>
        </template>

        <!-- 操作列 -->
        <el-table-column
          v-if="$slots.actions"
          label="操作"
          :width="actionColumnWidth"
          :fixed="actionColumnFixed"
          align="center"
        >
          <template #default="scope">
            <slot name="actions" :row="scope.row" :$index="scope.$index" />
          </template>
        </el-table-column>
      </el-table>
    </div>

    <!-- 分页 -->
    <div v-if="showPagination && pageSize > 0" class="table-pagination">
      <el-pagination
        v-model:current-page="currentPage"
        v-model:page-size="pageSize"
        :page-sizes="pageSizes"
        :total="total"
        :layout="paginationLayout"
        :background="backgroundPagination"
        @size-change="handleSizeChange"
        @current-change="handleCurrentChange"
      />
    </div>

    <!-- 统计信息 -->
    <div v-if="showStatistics" class="table-statistics">
      <span>共 {{ total }} 条数据</span>
      <span v-if="selectionCount > 0">，已选择 {{ selectionCount }} 条</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, type PropType } from 'vue'
import { Refresh, Download } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import type { DataSourceConfig } from '../types/dashboard'
import { dataBinder } from '../core/DataBinder'

/**
 * 表格列配置
 */
export interface TableColumnConfig {
  /** 属性名 */
  prop: string
  /** 列标题 */
  label: string
  /** 列宽 */
  width?: number | string
  /** 最小列宽 */
  minWidth?: number | string
  /** 固定列 */
  fixed?: boolean | 'left' | 'right'
  /** 对齐方式 */
  align?: 'left' | 'center' | 'right'
  /** 是否可排序 */
  sortable?: boolean
  /** 过滤选项 */
  filters?: Array<{ text: string; value: string | number }>
  /** 是否多选过滤 */
  filterMultiple?: boolean
  /** 默认过滤值 */
  filteredValue?: Array<string | number>
  /** 格式化类型 */
  formatter?: 'text' | 'number' | 'currency' | 'percent' | 'date' | 'datetime' | 'custom'
  /** 自定义格式化函数 */
  formatFn?: (value: unknown) => string
}

/**
 * 汇总类型
 */
export type SummaryType = 'sum' | 'avg' | 'count' | 'max' | 'min'

/**
 * Props 接口
 */
interface DataTableProps {
  /** 组件 ID */
  widgetId: string
  /** 标题 */
  title?: string
  /** 数据源配置 */
  dataSource?: DataSourceConfig
  /** 列配置 */
  columns: TableColumnConfig[]
  /** 数据 */
  data?: unknown[]
  /** 表格尺寸 */
  size?: 'large' | 'default' | 'small'
  /** 表格高度 */
  tableHeight?: string | number
  /** 最大高度 */
  maxHeight?: string | number
  /** 是否斑马纹 */
  striped?: boolean
  /** 是否边框 */
  border?: boolean
  /** 是否可选择 */
  selectable?: boolean
  /** 是否显示序号 */
  showIndex?: boolean
  /** 是否显示分页 */
  showPagination?: boolean
  /** 每页数量 */
  pageSize?: number
  /** 当前页码 */
  page?: number
  /** 分页尺寸选项 */
  pageSizes?: number[]
  /** 分页布局 */
  paginationLayout?: string
  /** 分页背景 */
  backgroundPagination?: boolean
  /** 是否显示汇总行 */
  showSummary?: boolean
  /** 汇总方法 */
  summaryType?: SummaryType
  /** 汇总列 */
  summaryColumns?: string[]
  /** 是否显示统计信息 */
  showStatistics?: boolean
  /** 空数据文本 */
  emptyText?: string
  /** 是否显示刷新按钮 */
  showRefreshButton?: boolean
  /** 是否显示导出按钮 */
  showExportButton?: boolean
  /** 操作列宽度 */
  actionColumnWidth?: number
  /** 操作列固定 */
  actionColumnFixed?: boolean | 'left' | 'right'
  /** 数据刷新间隔（秒） */
  refreshInterval?: number
}

// Props 定义
const props = withDefaults(defineProps<DataTableProps>(), {
  title: '',
  data: () => [],
  size: 'default',
  striped: true,
  border: false,
  selectable: false,
  showIndex: false,
  showPagination: true,
  pageSize: 20,
  page: 1,
  pageSizes: () => [10, 20, 50, 100],
  paginationLayout: 'total, sizes, prev, pager, next, jumper',
  backgroundPagination: true,
  showSummary: false,
  summaryType: 'sum',
  summaryColumns: () => [],
  showStatistics: false,
  emptyText: '暂无数据',
  showRefreshButton: true,
  showExportButton: false,
  actionColumnWidth: 120,
  actionColumnFixed: 'right',
  refreshInterval: 0
})

// Emits 定义
const emit = defineEmits<{
  /** 数据刷新时触发 */
  refresh: []
  /** 分页变化时触发 */
  'page-change': [page: number]
  /** 每页数量变化时触发 */
  'size-change': [size: number]
  /** 选择变化时触发 */
  'selection-change': [selection: unknown[]]
  /** 排序变化时触发 */
  'sort-change': [sort: { column: unknown; prop: string; order: string | null }]
  /** 过滤变化时触发 */
  'filter-change': [filters: Record<string, unknown[]>]
  /** 导出时触发 */
  export: [data: unknown[]]
}>()

// 状态
const loading = ref(false)
const tableData = ref<unknown[]>([])
const currentPage = ref(props.page)
const selection = ref<unknown[]>([])
const bindingId = ref(`${props.widgetId}-table`)

// 计算属性
const total = computed(() => tableData.value.length)

const displayData = computed(() => {
  if (!props.showPagination || props.pageSize <= 0) {
    return tableData.value
  }

  const start = (currentPage.value - 1) * props.pageSize
  const end = start + props.pageSize
  return tableData.value.slice(start, end)
})

const selectionCount = computed(() => selection.value.length)

/**
 * 格式化单元格数据
 */
function formatCellData(value: unknown, column: TableColumnConfig): string {
  if (value === null || value === undefined) {
    return ''
  }

  // 使用自定义格式化函数
  if (column.formatFn) {
    return column.formatFn(value)
  }

  // 根据类型格式化
  switch (column.formatter) {
    case 'number':
      return formatNumber(value as number)
    case 'currency':
      return formatCurrency(value as number)
    case 'percent':
      return formatPercent(value as number)
    case 'date':
      return formatDate(value as string)
    case 'datetime':
      return formatDateTime(value as string)
    default:
      return String(value)
  }
}

/**
 * 格式化数字
 */
function formatNumber(value: number): string {
  if (typeof value !== 'number') return String(value)
  return value.toLocaleString('zh-CN')
}

/**
 * 格式化货币
 */
function formatCurrency(value: number): string {
  if (typeof value !== 'number') return String(value)
  return '¥' + value.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

/**
 * 格式化百分比
 */
function formatPercent(value: number): string {
  if (typeof value !== 'number') return String(value)
  return (value * 100).toFixed(2) + '%'
}

/**
 * 格式化日期
 */
function formatDate(value: string): string {
  const date = new Date(value)
  if (isNaN(date.getTime())) return String(value)
  return date.toLocaleDateString('zh-CN')
}

/**
 * 格式化日期时间
 */
function formatDateTime(value: string): string {
  const date = new Date(value)
  if (isNaN(date.getTime())) return String(value)
  return date.toLocaleString('zh-CN')
}

/**
 * 获取汇总方法
 */
function getSummaryMethod(param: {
  columns: unknown[]
  data: unknown[]
}): string[] {
  const { columns, data } = param
  const sums: string[] = []

  columns.forEach((column, index) => {
    if (index === 0) {
      sums[index] = '汇总'
      return
    }

    const col = props.columns[index - 1] as TableColumnConfig | undefined
    if (!col || !props.summaryColumns.includes(col.prop)) {
      sums[index] = ''
      return
    }

    const values = data.map((item) => {
      const val = (item as Record<string, unknown>)[col.prop]
      return typeof val === 'number' ? val : 0
    })

    if (!values.length || values.every(v => v === 0)) {
      sums[index] = ''
      return
    }

    switch (props.summaryType) {
      case 'sum':
        sums[index] = formatNumber(values.reduce((prev, curr) => prev + curr, 0))
        break
      case 'avg':
        const avg = values.reduce((prev, curr) => prev + curr, 0) / values.length
        sums[index] = formatNumber(avg)
        break
      case 'count':
        sums[index] = formatNumber(values.filter(v => v !== 0).length)
        break
      case 'max':
        sums[index] = formatNumber(Math.max(...values))
        break
      case 'min':
        sums[index] = formatNumber(Math.min(...values))
        break
      default:
        sums[index] = ''
    }
  })

  return sums
}

/**
 * 刷新数据
 */
function handleRefresh(): void {
  emit('refresh')
  loadData()
}

/**
 * 导出数据
 */
function handleExport(): void {
  emit('export', tableData.value)

  // 简单的 CSV 导出
  try {
    const headers = props.columns.map(col => col.label).join(',')
    const rows = tableData.value.map(row => {
      return props.columns.map(col => {
        const value = (row as Record<string, unknown>)[col.prop]
        return formatCellData(value, col)
      }).join(',')
    })

    const csv = [headers, ...rows].join('\n')
    const blob = new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `${props.title || 'table'}.csv`
    link.click()
    URL.revokeObjectURL(link.href)

    ElMessage.success('导出成功')
  } catch (err) {
    ElMessage.error('导出失败')
    console.error('[DataTableWidget] 导出失败:', err)
  }
}

/**
 * 分页变化
 */
function handleCurrentChange(page: number): void {
  currentPage.value = page
  emit('page-change', page)
}

/**
 * 每页数量变化
 */
function handleSizeChange(size: number): void {
  emit('size-change', size)
}

/**
 * 选择变化
 */
function handleSelectionChange(newSelection: unknown[]): void {
  selection.value = newSelection
  emit('selection-change', newSelection)
}

/**
 * 排序变化
 */
function handleSortChange(sort: { column: unknown; prop: string; order: string | null }): void {
  emit('sort-change', sort)
}

/**
 * 过滤变化
 */
function handleFilterChange(filters: Record<string, unknown[]>): void {
  emit('filter-change', filters)
}

/**
 * 加载数据
 */
async function loadData(): Promise<void> {
  loading.value = true

  try {
    const data = dataBinder.getData(bindingId.value)

    if (data && Array.isArray(data)) {
      tableData.value = data
    } else if (props.data && props.data.length > 0) {
      tableData.value = props.data
    }
  } catch (err) {
    console.error('[DataTableWidget] 加载数据失败:', err)
  } finally {
    loading.value = false
  }
}

/**
 * 初始化数据绑定
 */
function initDataBinding(): void {
  if (!props.dataSource) {
    return
  }

  const sourceId = `${props.widgetId}-source`
  dataBinder.registerSource(
    {
      id: sourceId,
      ...props.dataSource
    },
    async () => {
      return fetchTableData()
    }
  )

  dataBinder.bind({
    widgetId: bindingId.value,
    sourceId,
    fieldMapping: {},
    refreshInterval: props.refreshInterval
  })

  loadData()
}

/**
 * 获取表格数据（模拟）
 */
async function fetchTableData(): Promise<unknown> {
  // TODO: 替换为实际 API 调用
  return Array.from({ length: 50 }, (_, i) => {
    return {
      id: i + 1,
      name: `项目 ${i + 1}`,
      category: ['类别A', '类别B', '类别C'][i % 3],
      value: Math.floor(Math.random() * 10000),
      status: ['进行中', '已完成', '待开始'][i % 3],
      date: new Date(Date.now() - i * 86400000).toISOString()
    }
  })
}

// 监听数据源变化
watch(() => props.dataSource, initDataBinding, { immediate: true })

// 监听 DataBinder 数据更新
watch(
  () => dataBinder.getBindingState(bindingId.value)?.value.data,
  (newData) => {
    if (newData && Array.isArray(newData)) {
      tableData.value = newData
    }
  }
)

// 监听 props.data 变化
watch(() => props.data, (newData) => {
  if (newData && newData.length > 0) {
    tableData.value = newData
  }
}, { immediate: true })

onMounted(() => {
  if (!props.dataSource) {
    fetchTableData().then(data => {
      if (Array.isArray(data)) {
        tableData.value = data
      }
    })
  }
})

onUnmounted(() => {
  dataBinder.unbind(bindingId.value)
})

// 暴露方法给父组件
defineExpose({
  refresh: handleRefresh,
  export: handleExport,
  getSelection: () => selection.value,
  clearSelection: () => {
    selection.value = []
  }
})
</script>

<style scoped lang="scss">
.data-table-widget {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  background: #fff;
  border-radius: 4px;
  overflow: hidden;
}

.table-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 1px solid #eee;
  background: #fafafa;

  .table-title {
    margin: 0;
    font-size: 14px;
    font-weight: 500;
    color: #333;
  }

  .table-actions {
    display: flex;
    gap: 8px;
  }
}

.table-container {
  flex: 1;
  overflow: auto;
}

.table-pagination {
  padding: 12px 16px;
  border-top: 1px solid #eee;
  display: flex;
  justify-content: flex-end;
  background: #fafafa;
}

.table-statistics {
  padding: 8px 16px;
  font-size: 12px;
  color: #909399;
  background: #f5f7fa;
  border-top: 1px solid #eee;
}

// 表格样式覆盖
:deep(.el-table) {
  font-size: 13px;

  .el-table__header-wrapper {
    th {
      background: #f5f7fa;
      color: #333;
      font-weight: 500;
    }
  }

  .el-table__body-wrapper {
    .el-table__row {
      &:hover {
        background: #f5f7fa;
      }
    }
  }
}
</style>
