<template>
  <div class="dashboard-table" ref="tableContainerRef">
    <!-- 表格标题 -->
    <div v-if="title" class="table-title">
      {{ title }}
    </div>

    <!-- 表格容器 -->
    <div
      class="table-wrapper"
      :class="{
        'is-scrolling': isScrollMode,
        'is-paused': isPaused,
        'is-virtual': useVirtualScroll
      }"
      @mouseenter="handleMouseEnter"
      @mouseleave="handleMouseLeave"
    >
      <!-- 滚动模式 -->
      <div
        v-if="isScrollMode && processedData.length > 0"
        class="scroll-container"
        ref="scrollContainerRef"
        :style="scrollContainerStyle"
      >
        <div
          class="scroll-content"
          :style="{ transform: `translateY(-${scrollOffset}px)` }"
        >
          <!-- 表头 -->
          <div class="scroll-header">
            <div
              v-for="col in normalizedColumns"
              :key="col.prop"
              class="scroll-cell header-cell"
              :style="getCellStyle(col, null)"
            >
              {{ col.label }}
            </div>
          </div>
          <!-- 数据行 -->
          <div
            v-for="(row, rowIndex) in duplicatedData"
            :key="`row-${rowIndex}`"
            class="scroll-row"
            :class="{ 'is-clone': rowIndex >= processedData.length }"
          >
            <div
              v-for="col in normalizedColumns"
              :key="col.prop"
              class="scroll-cell"
              :style="getCellStyle(col, row)"
            >
              {{ formatCellValue(row[col.prop], col) }}
            </div>
          </div>
        </div>
      </div>

      <!-- 虚拟滚动模式（大数据量） -->
      <template v-else-if="useVirtualScroll">
        <el-table-v2
          ref="virtualTableRef"
          :columns="virtualColumns"
          :data="processedData"
          :width="tableWidth"
          :height="virtualTableHeight"
          :row-height="virtualRowHeight"
          :header-height="virtualHeaderHeight"
          :row-class="virtualRowClass"
          :header-class="virtualHeaderClass"
          fixed
          class="virtual-table"
        >
          <!-- 空数据状态 -->
          <template #empty>
            <div class="empty-data">
              <el-icon :size="32"><Document /></el-icon>
              <span>暂无数据</span>
            </div>
          </template>
        </el-table-v2>
      </template>

      <!-- 分页模式（普通表格） -->
      <template v-else>
        <el-table
          ref="tableRef"
          :data="currentPageData"
          :border="config.bordered !== false"
          :stripe="config.striped !== false"
          :height="tableHeight"
          :header-cell-style="headerCellStyle"
          :cell-style="cellStyleHandler"
          size="small"
          class="data-table"
        >
          <el-table-column
            v-for="col in normalizedColumns"
            :key="col.prop"
            :prop="col.prop"
            :label="col.label"
            :width="col.width"
            :min-width="col.minWidth || 80"
            :align="col.align || 'center'"
            :sortable="col.sortable"
            :formatter="col.formatter"
          >
            <template #default="{ row }">
              <span :style="getCellStyle(col, row)">
                {{ formatCellValue(row[col.prop], col) }}
              </span>
            </template>
          </el-table-column>

          <!-- 空数据状态 -->
          <template #empty>
            <div class="empty-data">
              <el-icon :size="32"><Document /></el-icon>
              <span>暂无数据</span>
            </div>
          </template>
        </el-table>

        <!-- 分页指示器 -->
        <div v-if="showPagination && totalPages > 1" class="pagination-indicator">
          <span class="page-dot" :class="{ active: currentPage === 1 }"></span>
          <span
            v-for="page in totalPages - 2"
            :key="page"
            class="page-dot"
            :class="{ active: currentPage === page + 1 }"
          ></span>
          <span class="page-dot" :class="{ active: currentPage === totalPages }"></span>
          <span class="page-info">{{ currentPage }} / {{ totalPages }}</span>
        </div>
      </template>
    </div>

    <!-- 暂停指示器 -->
    <div v-if="isPaused && (isScrollMode || isAutoPageMode)" class="pause-indicator">
      <el-icon><VideoPause /></el-icon>
    </div>

    <!-- 数据量指示器（虚拟模式） -->
    <div v-if="useVirtualScroll && showDataCount" class="data-count-indicator">
      共 {{ processedData.length }} 条数据
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { Document, VideoPause } from '@element-plus/icons-vue'
import type { TableColumnConfig, StyleRule, ComparisonOperator } from './types'

// ============================================================================
// 类型定义
// ============================================================================

/** 表格数据项 */
interface TableDataItem {
  [key: string]: string | number | boolean | null | undefined
}

/** 列配置 (内部使用) */
interface ColumnConfig {
  prop: string
  label: string
  width?: number | string
  minWidth?: number
  align?: 'left' | 'center' | 'right'
  sortable?: boolean
  formatter?: (row: TableDataItem, column: ColumnConfig, cellValue: unknown, index: number) => string
  styleRules?: StyleRule[]
}

/** 溢出处理模式 */
type OverflowMode = 'auto-page' | 'scroll' | 'none'

/** 表格配置 */
interface TableConfig {
  /** 是否显示边框 */
  bordered?: boolean
  /** 是否显示条纹 */
  striped?: boolean
  /** 溢出处理模式 */
  overflowMode?: OverflowMode
  /** 自动翻页间隔 (秒) */
  pageInterval?: number
  /** 每页显示条数 */
  pageSize?: number
  /** 滚动速度 (像素/秒) */
  scrollSpeed?: number
  /** 是否显示分页指示器 */
  showPagination?: boolean
  /** 是否固定表头 */
  fixedHeader?: boolean
  /** 行高 */
  rowHeight?: number
}

/** 条件样式规则 (简化版) */
interface ConditionalRule {
  field: string
  operator: ComparisonOperator | 'eq' | 'gt' | 'lt' | 'gte' | 'lte' | 'neq' | 'contains'
  value: string
  backgroundColor?: string
  textColor?: string
}

// ============================================================================
// Props 定义
// ============================================================================

const props = withDefaults(
  defineProps<{
    /** 表格标题 */
    title?: string
    /** 列配置 */
    columns?: TableColumnConfig[]
    /** 表格数据 */
    data?: TableDataItem[]
    /** 表格配置 */
    config?: TableConfig
    /** 条件样式规则 */
    rules?: ConditionalRule[]
    /** 主题 */
    theme?: 'dark' | 'light'
    /** 是否暂停动画 */
    paused?: boolean
  }>(),
  {
    title: '',
    columns: () => [],
    data: () => [],
    config: () => ({}),
    rules: () => [],
    theme: 'dark',
    paused: false
  }
)

// ============================================================================
// 响应式状态
// ============================================================================

const tableContainerRef = ref<HTMLElement | null>(null)
const scrollContainerRef = ref<HTMLElement | null>(null)
const tableRef = ref<any>(null)

const currentPage = ref(1)
const scrollOffset = ref(0)
const isPaused = ref(false)
const tableHeight = ref<number | undefined>(undefined)

// 动画控制
let autoPageTimer: ReturnType<typeof setInterval> | null = null
let scrollAnimationId: number | null = null
let lastScrollTime = 0

// ============================================================================
// 计算属性
// ============================================================================

/** 合并默认配置 */
const mergedConfig = computed<TableConfig>(() => ({
  bordered: true,
  striped: false,
  overflowMode: 'auto-page',
  pageInterval: 5,
  pageSize: 10,
  scrollSpeed: 30,
  showPagination: true,
  fixedHeader: true,
  rowHeight: 40,
  ...props.config
}))

/** 获取 config 的引用 */
const config = mergedConfig

/** 标准化列配置 */
const normalizedColumns = computed<ColumnConfig[]>(() => {
  return props.columns.map((col) => ({
    prop: col.field || col.key,
    label: col.title || col.key,
    width: col.width,
    minWidth: 80,
    align: col.align || 'center',
    sortable: col.sortable,
    styleRules: col.styleRules
  }))
})

/** 处理后的数据 */
const processedData = computed(() => {
  if (!props.data || props.data.length === 0) return []
  return props.data
})

/** 是否为滚动模式 */
const isScrollMode = computed(() => {
  return config.value.overflowMode === 'scroll'
})

/** 是否为自动翻页模式 */
const isAutoPageMode = computed(() => {
  return config.value.overflowMode === 'auto-page'
})

/** 是否显示分页 */
const showPagination = computed(() => {
  return config.value.showPagination !== false && !isScrollMode.value
})

/** 每页大小 */
const pageSize = computed(() => {
  return config.value.pageSize || 10
})

/** 总页数 */
const totalPages = computed(() => {
  return Math.ceil(processedData.value.length / pageSize.value)
})

/** 当前页数据 */
const currentPageData = computed(() => {
  if (isScrollMode.value) return processedData.value
  const start = (currentPage.value - 1) * pageSize.value
  const end = start + pageSize.value
  return processedData.value.slice(start, end)
})

/** 滚动模式 - 复制数据以实现无缝滚动 */
const duplicatedData = computed(() => {
  if (!isScrollMode.value) return processedData.value
  // 复制一份数据用于无缝滚动
  return [...processedData.value, ...processedData.value]
})

/** 滚动容器样式 */
const scrollContainerStyle = computed(() => {
  const rowHeight = config.value.rowHeight || 40
  const headerHeight = 36
  const visibleRows = Math.ceil((tableHeight.value || 200) / rowHeight)
  const maxHeight = visibleRows * rowHeight + headerHeight

  return {
    maxHeight: `${maxHeight}px`,
    overflow: 'hidden'
  }
})

// ============================================================================
// 样式计算
// ============================================================================

/**
 * 比较值
 */
const compareValue = (
  actualValue: unknown,
  operator: ComparisonOperator | string,
  targetValue: string
): boolean => {
  const actual = String(actualValue ?? '')
  const target = targetValue

  switch (operator) {
    case 'eq':
    case '==':
      return actual === target
    case 'neq':
    case '!=':
      return actual !== target
    case 'gt':
    case '>':
      return parseFloat(actual) > parseFloat(target)
    case 'gte':
    case '>=':
      return parseFloat(actual) >= parseFloat(target)
    case 'lt':
    case '<':
      return parseFloat(actual) < parseFloat(target)
    case 'lte':
    case '<=':
      return parseFloat(actual) <= parseFloat(target)
    case 'contains':
      return actual.includes(target)
    case 'startsWith':
      return actual.startsWith(target)
    case 'endsWith':
      return actual.endsWith(target)
    default:
      return false
  }
}

/**
 * 获取匹配的样式规则
 */
const getMatchingRule = (row: TableDataItem | null, rules: ConditionalRule[]) => {
  if (!row) return null

  for (const rule of rules) {
    const fieldValue = row[rule.field]
    if (compareValue(fieldValue, rule.operator, rule.value)) {
      return rule
    }
  }
  return null
}

/**
 * 获取单元格样式
 */
const getCellStyle = (column: ColumnConfig, row: TableDataItem | null) => {
  const styles: Record<string, string> = {}

  // 检查列级别的样式规则
  if (column.styleRules && column.styleRules.length > 0) {
    const matchingRule = getMatchingRule(
      row,
      column.styleRules.map((r) => ({
        field: r.field,
        operator: r.operator,
        value: r.value,
        backgroundColor: r.actionType === 'setBgColor' ? r.actionValue : undefined,
        textColor: r.actionType === 'setColor' ? r.actionValue : undefined
      }))
    )
    if (matchingRule) {
      if (matchingRule.backgroundColor) {
        styles.backgroundColor = matchingRule.backgroundColor
      }
      if (matchingRule.textColor) {
        styles.color = matchingRule.textColor
      }
    }
  }

  // 检查全局样式规则
  if (props.rules && props.rules.length > 0) {
    const matchingRule = getMatchingRule(row, props.rules)
    if (matchingRule) {
      if (matchingRule.backgroundColor) {
        styles.backgroundColor = matchingRule.backgroundColor
      }
      if (matchingRule.textColor) {
        styles.color = matchingRule.textColor
      }
    }
  }

  return styles
}

/**
 * 格式化单元格值
 */
const formatCellValue = (value: unknown, column: ColumnConfig): string => {
  if (value === null || value === undefined) return ''
  if (typeof value === 'number') {
    return value.toLocaleString()
  }
  return String(value)
}

/**
 * 表头单元格样式
 */
const headerCellStyle = () => {
  const isDark = props.theme === 'dark'
  return {
    backgroundColor: isDark ? 'rgba(30, 41, 59, 0.8)' : '#f5f7fa',
    color: isDark ? '#e2e8f0' : '#606266',
    fontWeight: 600,
    borderBottom: isDark ? '1px solid #334155' : '1px solid #ebeef5'
  }
}

/**
 * Element Plus 单元格样式处理器
 */
const cellStyleHandler = ({ row, column }: { row: TableDataItem; column: any }) => {
  const col = normalizedColumns.value.find((c) => c.prop === column.property)
  if (!col) return {}

  return getCellStyle(col, row)
}

// ============================================================================
// 动画控制
// ============================================================================

/**
 * 启动自动翻页
 */
const startAutoPage = () => {
  if (!isAutoPageMode.value || autoPageTimer) return

  const interval = (config.value.pageInterval || 5) * 1000
  autoPageTimer = setInterval(() => {
    if (isPaused.value) return

    if (currentPage.value >= totalPages.value) {
      currentPage.value = 1
    } else {
      currentPage.value++
    }
  }, interval)
}

/**
 * 停止自动翻页
 */
const stopAutoPage = () => {
  if (autoPageTimer) {
    clearInterval(autoPageTimer)
    autoPageTimer = null
  }
}

/**
 * 滚动动画帧
 */
const scrollAnimationFrame = (timestamp: number) => {
  if (isPaused.value || !isScrollMode.value) {
    scrollAnimationId = requestAnimationFrame(scrollAnimationFrame)
    return
  }

  if (!lastScrollTime) {
    lastScrollTime = timestamp
  }

  const deltaTime = timestamp - lastScrollTime
  lastScrollTime = timestamp

  const speed = config.value.scrollSpeed || 30
  const pixelsPerFrame = (speed * deltaTime) / 1000

  scrollOffset.value += pixelsPerFrame

  // 计算滚动总高度
  const rowHeight = config.value.rowHeight || 40
  const totalHeight = processedData.value.length * rowHeight

  // 当滚动超过原始数据高度时，重置到顶部
  if (scrollOffset.value >= totalHeight) {
    scrollOffset.value = 0
  }

  scrollAnimationId = requestAnimationFrame(scrollAnimationFrame)
}

/**
 * 启动滚动动画
 */
const startScrollAnimation = () => {
  if (!isScrollMode.value || scrollAnimationId) return

  lastScrollTime = 0
  scrollAnimationId = requestAnimationFrame(scrollAnimationFrame)
}

/**
 * 停止滚动动画
 */
const stopScrollAnimation = () => {
  if (scrollAnimationId) {
    cancelAnimationFrame(scrollAnimationId)
    scrollAnimationId = null
  }
}

// ============================================================================
// 事件处理
// ============================================================================

/**
 * 鼠标进入 - 暂停动画
 */
const handleMouseEnter = () => {
  isPaused.value = true
}

/**
 * 鼠标离开 - 恢复动画
 */
const handleMouseLeave = () => {
  isPaused.value = props.paused
}

/**
 * 计算表格高度
 */
const calculateTableHeight = () => {
  if (!tableContainerRef.value) return

  const container = tableContainerRef.value
  const titleHeight = props.title ? 36 : 0
  const paginationHeight = showPagination.value && totalPages.value > 1 ? 32 : 0
  const availableHeight = container.clientHeight - titleHeight - paginationHeight - 16

  tableHeight.value = availableHeight > 0 ? availableHeight : undefined
}

// ============================================================================
// 生命周期
// ============================================================================

onMounted(() => {
  nextTick(() => {
    calculateTableHeight()
  })

  // 启动动画
  if (!props.paused) {
    if (isAutoPageMode.value) {
      startAutoPage()
    } else if (isScrollMode.value) {
      startScrollAnimation()
    }
  }

  // 监听窗口大小变化
  window.addEventListener('resize', calculateTableHeight)
})

onUnmounted(() => {
  stopAutoPage()
  stopScrollAnimation()
  window.removeEventListener('resize', calculateTableHeight)
})

// 监听配置变化
watch(
  () => config.value.overflowMode,
  (newMode) => {
    stopAutoPage()
    stopScrollAnimation()

    if (!isPaused.value) {
      if (newMode === 'auto-page') {
        startAutoPage()
      } else if (newMode === 'scroll') {
        startScrollAnimation()
      }
    }
  }
)

// 监听暂停状态
watch(
  () => props.paused,
  (newPaused) => {
    isPaused.value = newPaused
  },
  { immediate: true }
)

// 监听数据变化 - 重置页码
watch(
  () => props.data,
  () => {
    currentPage.value = 1
    scrollOffset.value = 0
  }
)

// 监听页数变化
watch(totalPages, (newTotal) => {
  if (currentPage.value > newTotal && newTotal > 0) {
    currentPage.value = newTotal
  }
})
</script>

<style scoped>
.dashboard-table {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  position: relative;
  overflow: hidden;
}

/* 标题 */
.table-title {
  padding: 8px 12px;
  font-size: 16px;
  font-weight: 600;
  color: #e2e8f0;
  background: linear-gradient(90deg, rgba(59, 130, 246, 0.2), transparent);
  border-bottom: 1px solid rgba(59, 130, 246, 0.3);
}

/* 表格容器 */
.table-wrapper {
  flex: 1;
  position: relative;
  overflow: hidden;
}

/* Element Plus 表格样式覆盖 */
.data-table {
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-header-bg-color: rgba(30, 41, 59, 0.6);
  --el-table-row-hover-bg-color: rgba(59, 130, 246, 0.1);
  --el-table-border-color: rgba(71, 85, 105, 0.5);
  --el-table-text-color: #cbd5e1;
  --el-table-header-text-color: #94a3b8;
}

.data-table :deep(.el-table__header-wrapper) {
  position: sticky;
  top: 0;
  z-index: 10;
}

.data-table :deep(.el-table__body-wrapper) {
  overflow-y: auto;
}

.data-table :deep(.el-table__cell) {
  padding: 8px 0;
  border-bottom: 1px solid rgba(71, 85, 105, 0.3);
}

.data-table :deep(.el-table__row) {
  transition: background-color 0.2s;
}

.data-table :deep(.el-table__row:hover > td) {
  background-color: rgba(59, 130, 246, 0.1) !important;
}

/* 空数据状态 */
.empty-data {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px;
  color: #64748b;
}

.empty-data .el-icon {
  margin-bottom: 8px;
  opacity: 0.5;
}

/* 分页指示器 */
.pagination-indicator {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 8px;
  gap: 6px;
}

.page-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background-color: rgba(148, 163, 184, 0.4);
  transition: all 0.3s;
}

.page-dot.active {
  background-color: #3b82f6;
  width: 18px;
  border-radius: 3px;
}

.page-info {
  margin-left: 12px;
  font-size: 12px;
  color: #94a3b8;
}

/* 滚动模式 */
.scroll-container {
  width: 100%;
  position: relative;
  will-change: transform;
}

.scroll-content {
  will-change: transform;
}

.scroll-header {
  display: flex;
  background: rgba(30, 41, 59, 0.8);
  position: sticky;
  top: 0;
  z-index: 10;
}

.scroll-row {
  display: flex;
  border-bottom: 1px solid rgba(71, 85, 105, 0.3);
  transition: background-color 0.2s;
}

.scroll-row:hover {
  background-color: rgba(59, 130, 246, 0.1);
}

.scroll-row.is-clone {
  opacity: 0;
}

.scroll-cell {
  flex: 1;
  padding: 10px 12px;
  min-width: 80px;
  color: #cbd5e1;
  font-size: 13px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  display: flex;
  align-items: center;
}

.scroll-cell.header-cell {
  color: #94a3b8;
  font-weight: 600;
  background: rgba(30, 41, 59, 0.8);
}

/* 暂停指示器 */
.pause-indicator {
  position: absolute;
  top: 50%;
  right: 12px;
  transform: translateY(-50%);
  background: rgba(0, 0, 0, 0.6);
  padding: 8px;
  border-radius: 50%;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0.7;
  transition: opacity 0.2s;
}

.pause-indicator:hover {
  opacity: 1;
}

/* 浅色主题 */
.dashboard-table[data-theme='light'] .table-title {
  color: #1f2937;
  background: linear-gradient(90deg, rgba(59, 130, 246, 0.1), transparent);
  border-bottom: 1px solid rgba(59, 130, 246, 0.2);
}

.dashboard-table[data-theme='light'] .data-table {
  --el-table-bg-color: #fff;
  --el-table-tr-bg-color: #fff;
  --el-table-header-bg-color: #f9fafb;
  --el-table-row-hover-bg-color: #f3f4f6;
  --el-table-border-color: #e5e7eb;
  --el-table-text-color: #374151;
  --el-table-header-text-color: #6b7280;
}

.dashboard-table[data-theme='light'] .scroll-header {
  background: #f9fafb;
}

.dashboard-table[data-theme='light'] .scroll-row {
  border-bottom-color: #e5e7eb;
}

.dashboard-table[data-theme='light'] .scroll-row:hover {
  background-color: #f3f4f6;
}

.dashboard-table[data-theme='light'] .scroll-cell {
  color: #374151;
}

.dashboard-table[data-theme='light'] .scroll-cell.header-cell {
  color: #6b7280;
  background: #f9fafb;
}

/* 暂停状态 */
.table-wrapper.is-paused {
  cursor: pointer;
}

/* 响应式 */
@media (max-width: 768px) {
  .scroll-cell {
    padding: 8px;
    font-size: 12px;
    min-width: 60px;
  }

  .table-title {
    font-size: 14px;
    padding: 6px 10px;
  }
}
</style>
