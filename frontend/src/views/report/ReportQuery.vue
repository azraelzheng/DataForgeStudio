<template>
  <div class="report-query">
    <!-- 可折叠侧边栏 -->
    <div class="sidebar" :class="{ collapsed: sidebarCollapsed }">
      <!-- 搜索框 -->
      <div class="sidebar-search">
        <el-input
          v-if="!sidebarCollapsed"
          v-model="searchKeyword"
          placeholder="搜索报表..."
          clearable
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-input>
        <el-tooltip v-else content="搜索报表" placement="right">
          <el-icon class="sidebar-icon-only"><Search /></el-icon>
        </el-tooltip>
      </div>

      <!-- 报表列表 -->
      <div class="sidebar-list">
        <div v-if="filteredReports.length > 0" class="report-list">
          <div
            v-for="report in filteredReports"
            :key="report.reportId"
            :class="['report-item', { active: selectedReportId === report.reportId }]"
            @click="selectReport(report)"
          >
            <el-tooltip :content="sidebarCollapsed ? report.reportName : ''" placement="right">
              <div class="report-item-content">
                <el-icon class="report-item-icon"><Document /></el-icon>
                <div v-if="!sidebarCollapsed" class="report-item-info">
                  <div class="report-item-name">{{ report.reportName }}</div>
                  <div class="report-item-meta">
                    <el-tag size="small" type="info">{{ report.reportCategory || '未分类' }}</el-tag>
                    <span class="view-count">{{ report.viewCount || 0 }} 次查看</span>
                  </div>
                </div>
              </div>
            </el-tooltip>
          </div>
        </div>
        <el-empty v-else description="暂无报表数据" :image-size="60" style="margin-top: 20px;">
          <el-button type="primary" size="small" @click="router.push('/report/design')">创建报表</el-button>
        </el-empty>
      </div>

      <!-- 收起/展开按钮 -->
      <div class="sidebar-toggle" @click="toggleSidebar">
        <el-icon>
          <ArrowLeft v-if="!sidebarCollapsed" />
          <ArrowRight v-else />
        </el-icon>
      </div>
    </div>

    <!-- 主内容区域 -->
    <div class="main-content">
      <!-- 未选择报表 -->
      <div v-if="!selectedReport" class="empty-state">
        <el-empty description="请选择左侧报表进行查询">
          <template #image>
            <el-icon :size="80" color="#c0c4cc"><Document /></el-icon>
          </template>
        </el-empty>
      </div>

      <!-- 已选择报表 -->
      <template v-else>
        <!-- 报表标题 -->
        <div class="report-header">
          <h2>{{ selectedReport.reportName }}</h2>
          <el-tag v-if="selectedReport.reportCategory" type="info">
            {{ selectedReport.reportCategory }}
          </el-tag>
        </div>

        <!-- 查询条件 (可折叠) -->
        <el-collapse v-if="queryConditions.length > 0" v-model="conditionsActive" class="conditions-collapse">
          <el-collapse-item name="conditions">
            <template #title>
              <span class="collapse-title">
                <el-icon><Filter /></el-icon>
                查询条件
              </span>
            </template>
            <div class="conditions-content">
              <el-form :inline="true" :model="conditionForm" label-width="100px">
                <el-row :gutter="20">
                  <el-col :span="12" v-for="qc in queryConditions" :key="qc.fieldName + qc.operator">
                    <el-form-item :label="qc.displayName">
                      <!-- 不需要输入值的操作符 -->
                      <template v-if="['null', 'notnull', 'true', 'false'].includes(qc.operator)">
                        <span class="operator-label">{{ getOperatorLabel(qc.operator) }}</span>
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
                        <div class="number-range-input">
                          <el-input-number
                            v-model="conditionForm[getFieldKey(qc) + '_start']"
                            placeholder="最小值"
                            :controls-position="'right'"
                            class="flex-1"
                          />
                          <span class="range-separator-text">~</span>
                          <el-input-number
                            v-model="conditionForm[getFieldKey(qc) + '_end']"
                            placeholder="最大值"
                            :controls-position="'right'"
                            class="flex-1"
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
              <div class="conditions-actions">
                <el-button type="primary" @click="handleQuery" :loading="querying">
                  <el-icon><Search /></el-icon>
                  查询
                </el-button>
                <el-button @click="resetConditions">重置条件</el-button>
              </div>
            </div>
          </el-collapse-item>
        </el-collapse>

        <!-- 无查询条件时的操作按钮 -->
        <div v-if="queryConditions.length === 0" class="action-bar">
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
        <div v-if="reportData && reportData.length > 0" class="results-container">
          <!-- 表格工具栏 -->
          <div class="table-toolbar">
            <span class="result-count">共 {{ filteredTableData.length }} 条记录<template v-if="filteredTableData.length !== reportData.length"> (筛选自 {{ reportData.length }} 条)</template></span>
            <div class="toolbar-actions">
              <el-button v-if="queryConditions.length > 0" type="success" size="small" @click="handleExportExcel" :loading="exporting">
                <el-icon><Download /></el-icon>
                导出 Excel
              </el-button>
            </div>
          </div>

          <!-- 表格 -->
          <div class="table-wrapper" ref="tableWrapperRef">
            <el-table
              :data="paginatedData"
              border
              stripe
              size="small"
              style="width: 100%;"
              :max-height="tableMaxHeight"
              show-summary
              :summary-method="getSummaryRow"
              :row-class-name="tableRowClassName"
            >
              <!-- 序号列 -->
              <el-table-column
                type="index"
                label="序号"
                width="60"
                align="center"
                :index="indexMethod"
              />
              <el-table-column
                v-for="col in displayColumns"
                :key="col.fieldName"
                :prop="col.fieldName"
                :min-width="col.width"
                :align="col.align || 'left'"
                show-overflow-tooltip
              >
                <template #header>
                  <div class="column-header-compact">
                    <span class="column-name" @click="handleColumnSort(col.fieldName)">
                      {{ col.displayName }}
                      <span v-if="sortField === col.fieldName" class="sort-indicator">
                        {{ sortOrder === 'asc' ? '▲' : '▼' }}
                      </span>
                    </span>
                    <!-- 使用简单的点击事件替代每列独立的 popover -->
                    <el-icon
                      class="filter-icon"
                      :class="{ active: hasColumnFilter(col.fieldName) || activeFilterColumn === col.fieldName }"
                      @click.stop="openFilterPopover($event, col)"
                    >
                      <Filter />
                    </el-icon>
                  </div>
                </template>
              </el-table-column>
            </el-table>

            <!-- 共享的筛选弹出框 - 只渲染一个 -->
            <el-popover
              v-if="activeFilterColumn"
              :visible="!!activeFilterColumn"
              :virtual-ref="filterTriggerRef"
              virtual-triggering
              placement="bottom-start"
              :width="280"
              @update:visible="closeFilterPopover"
            >
              <div class="filter-popover-content" v-if="activeColumn">
                <!-- Sort section -->
                <div class="filter-section">
                  <div class="filter-section-title">排序</div>
                  <div class="sort-buttons">
                    <el-button
                      size="small"
                      :type="sortField === activeColumn.fieldName && sortOrder === 'asc' ? 'primary' : 'default'"
                      @click="handlePopoverSort(activeColumn.fieldName, 'asc')"
                    >
                      升序
                    </el-button>
                    <el-button
                      size="small"
                      :type="sortField === activeColumn.fieldName && sortOrder === 'desc' ? 'primary' : 'default'"
                      @click="handlePopoverSort(activeColumn.fieldName, 'desc')"
                    >
                      降序
                    </el-button>
                    <el-button
                      v-if="sortField === activeColumn.fieldName"
                      size="small"
                      @click="handlePopoverSort(activeColumn.fieldName, null)"
                    >
                      取消
                    </el-button>
                  </div>
                </div>

                <!-- Filter section - varies by data type -->
                <div class="filter-section">
                  <div class="filter-section-title">筛选</div>
                  <div class="filter-input-wrapper">
                    <!-- String -->
                    <el-input
                      v-if="activeColumn.dataType === 'String' || !activeColumn.dataType"
                      v-model="columnFilters[activeColumn.fieldName]"
                      placeholder="输入筛选文本..."
                      size="small"
                      clearable
                    />
                    <!-- Number range -->
                    <div v-else-if="activeColumn.dataType === 'Number'" class="range-filter">
                      <el-input-number
                        v-model="columnFilters[activeColumn.fieldName + '_min']"
                        placeholder="最小"
                        size="small"
                        :controls="false"
                      />
                      <span class="range-separator">~</span>
                      <el-input-number
                        v-model="columnFilters[activeColumn.fieldName + '_max']"
                        placeholder="最大"
                        size="small"
                        :controls="false"
                      />
                    </div>
                    <!-- DateTime range -->
                    <el-date-picker
                      v-else-if="activeColumn.dataType === 'DateTime'"
                      v-model="columnFilters[activeColumn.fieldName]"
                      type="daterange"
                      size="small"
                      range-separator="-"
                      start-placeholder="开始"
                      end-placeholder="结束"
                      value-format="YYYY-MM-DD"
                    />
                    <!-- Boolean select -->
                    <el-select
                      v-else-if="activeColumn.dataType === 'Boolean'"
                      v-model="columnFilters[activeColumn.fieldName]"
                      placeholder="全部"
                      size="small"
                      clearable
                    >
                      <el-option label="是" :value="true" />
                      <el-option label="否" :value="false" />
                    </el-select>
                  </div>
                </div>

                <!-- Action buttons -->
                <div class="filter-actions">
                  <el-button size="small" @click="handleClearColumnFilter(activeColumn.fieldName)">
                    清除
                  </el-button>
                  <el-button type="primary" size="small" @click="closeFilterPopover(false)">
                    应用
                  </el-button>
                </div>
              </div>
            </el-popover>
          </div>

          <!-- 分页 -->
          <div class="pagination-wrapper">
            <el-pagination
              v-model:current-page="currentPage"
              v-model:page-size="pageSize"
              :page-sizes="[20, 50, 100, 200]"
              :total="filteredTableData.length"
              layout="total, sizes, prev, pager, next, jumper"
            />
          </div>
        </div>

        <!-- 无数据提示 -->
        <div v-else-if="hasQueried" class="no-data">
          <el-empty description="查询结果为空" />
        </div>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Search, Document, Download, ArrowLeft, ArrowRight, Filter } from '@element-plus/icons-vue'
import { reportApi } from '../../api/request'

const router = useRouter()

// 侧边栏状态
const sidebarCollapsed = ref(false)

const searchKeyword = ref('')
const reports = ref([])
const selectedReportId = ref(null)
const selectedReport = ref(null)
const queryConditions = ref([])
const conditionForm = reactive({})
const reportData = ref([])
const querying = ref(false)
const exporting = ref(false)
const conditionsActive = ref(['conditions'])  // 默认展开条件面板
const hasQueried = ref(false)  // 是否已执行过查询
const tableMaxHeight = ref(null)  // 表格最大高度，null 表示自适应内容

// 动态计算表格高度
const updateTableHeight = () => {
  nextTick(() => {
    if (tableWrapperRef.value) {
      const wrapperHeight = tableWrapperRef.value.clientHeight
      if (wrapperHeight > 0) {
        // 表格元素高度常量（size="small"）
        const rowHeight = 32      // 数据行高度
        const headerHeight = 36   // 表头高度
        const summaryHeight = 32  // 合计行高度

        // 计算当前页的实际数据行数
        const realDataRows = filteredTableData.value?.length || 0
        const currentPageRows = Math.min(pageSize.value, Math.max(0, realDataRows - (currentPage.value - 1) * pageSize.value))

        if (currentPageRows > 0) {
          // 有数据时，高度 = 表头 + 数据行 + 合计行
          // paginatedData 会补充空行到 pageSize，所以用 pageSize 计算高度
          const calculatedHeight = headerHeight + (pageSize.value * rowHeight) + summaryHeight
          tableMaxHeight.value = calculatedHeight
        } else {
          // 无数据时，使用较小高度
          tableMaxHeight.value = headerHeight + summaryHeight + 50
        }
      }
    }
  })
}

// 表格筛选和排序状态
const columnFilters = reactive({})
const sortField = ref(null)
const sortOrder = ref(null) // 'asc' | 'desc'
const currentPage = ref(1)
const pageSize = ref(20)

// 序号列计算方法
const indexMethod = (index) => {
  return (currentPage.value - 1) * pageSize.value + index + 1
}

// 表格行样式类名
const tableRowClassName = ({ row }) => {
  if (row._isEmpty) {
    return 'empty-row'
  }
  return ''
}

// 共享 Popover 状态（替代每列独立的 popover）
const tableWrapperRef = ref(null)
const activeFilterColumn = ref(null)  // 当前激活筛选的列名
const filterTriggerRef = ref(null)     // 触发 popover 的虚拟引用
const activeColumn = computed(() => {  // 当前激活的列对象
  if (!activeFilterColumn.value) return null
  return displayColumns.value.find(c => c.fieldName === activeFilterColumn.value)
})

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
  // 从 localStorage 恢复侧边栏状态
  const savedState = localStorage.getItem('reportQuerySidebarCollapsed')
  if (savedState !== null) {
    sidebarCollapsed.value = savedState === 'true'
  }
  await loadReports()
  // 监听窗口大小变化
  window.addEventListener('resize', updateTableHeight)
})

onUnmounted(() => {
  window.removeEventListener('resize', updateTableHeight)
})

// 切换侧边栏
const toggleSidebar = () => {
  sidebarCollapsed.value = !sidebarCollapsed.value
  localStorage.setItem('reportQuerySidebarCollapsed', sidebarCollapsed.value)
}

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
  hasQueried.value = false
  try {
    const res = await reportApi.getReport(report.reportId)
    if (res.success) {
      selectedReport.value = res.data
      queryConditions.value = res.data.queryConditions || []
      resetConditions()
      reportData.value = []
    }
  } catch (error) {
    console.error('加载报表详情失败:', error)
  }
}

const displayColumns = computed(() => {
  const cols = selectedReport.value?.columns || selectedReport.value?.fields || []
  // 过滤掉不可见的列
  return cols.filter(col => col.isVisible !== false)
})

// Check if column has active filter
const hasColumnFilter = (fieldName) => {
  const val = columnFilters[fieldName]
  const minVal = columnFilters[fieldName + '_min']
  const maxVal = columnFilters[fieldName + '_max']

  if (val !== null && val !== undefined && val !== '') return true
  if (minVal !== null && minVal !== undefined) return true
  if (maxVal !== null && maxVal !== undefined) return true
  return false
}

// 筛选后的数据
const filteredTableData = computed(() => {
  if (!reportData.value || reportData.value.length === 0) return []

  let data = [...reportData.value]

  // 应用列筛选
  data = data.filter(row => {
    for (const [key, value] of Object.entries(columnFilters)) {
      if (value === null || value === undefined || value === '') continue

      // 范围筛选 (数字最小值)
      if (key.endsWith('_min')) {
        const field = key.replace('_min', '')
        const maxKey = field + '_max'
        const minVal = value
        const maxVal = columnFilters[maxKey]

        if (minVal !== null && minVal !== undefined && row[field] < minVal) return false
        if (maxVal !== null && maxVal !== undefined && row[field] > maxVal) return false
        continue
      }

      // 范围筛选 (数字最大值) - 已在上面处理
      if (key.endsWith('_max')) continue

      // 日期范围筛选
      if (Array.isArray(value) && value.length === 2) {
        const dateVal = row[key] ? new Date(row[key]) : null
        if (!dateVal) return false
        const start = new Date(value[0])
        const end = new Date(value[1])
        end.setHours(23, 59, 59, 999)
        if (dateVal < start || dateVal > end) return false
        continue
      }

      // 布尔值筛选
      if (typeof value === 'boolean') {
        if (row[key] !== value) return false
        continue
      }

      // 文本模糊匹配
      if (typeof value === 'string') {
        const cellValue = String(row[key] || '').toLowerCase()
        if (!cellValue.includes(value.toLowerCase())) return false
      }
    }
    return true
  })

  // 应用排序
  if (sortField.value && sortOrder.value) {
    data.sort((a, b) => {
      let aVal = a[sortField.value]
      let bVal = b[sortField.value]

      // 处理 null/undefined
      if (aVal == null) return 1
      if (bVal == null) return -1

      // 字符串比较
      if (typeof aVal === 'string' && typeof bVal === 'string') {
        aVal = aVal.toLowerCase()
        bVal = bVal.toLowerCase()
      }

      let result = 0
      if (aVal < bVal) result = -1
      if (aVal > bVal) result = 1

      return sortOrder.value === 'asc' ? result : -result
    })
  }

  return data
})

// 分页后的数据（补充空行填满表格）
const paginatedData = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  const end = start + pageSize.value
  const data = filteredTableData.value.slice(start, end)

  // 如果数据不足一页，补充空行
  if (data.length > 0 && data.length < pageSize.value) {
    const emptyRows = Array(pageSize.value - data.length).fill({ _isEmpty: true })
    return [...data, ...emptyRows]
  }

  return data
})

// 监听分页数据变化，更新表格高度
watch(paginatedData, () => {
  updateTableHeight()
})

// 获取数值数组中的最大小数位数
const getMaxDecimals = (values) => {
  let maxDecimals = 0
  for (const val of values) {
    if (val === null || val === undefined) continue
    const str = String(val)
    // 处理科学计数法
    if (str.includes('e') || str.includes('E')) {
      const num = Number(val)
      const fixed = num.toFixed(10)
      const dotIndex = fixed.indexOf('.')
      if (dotIndex !== -1) {
        const decimals = fixed.length - dotIndex - 1
        maxDecimals = Math.max(maxDecimals, decimals)
      }
    } else {
      const dotIndex = str.indexOf('.')
      if (dotIndex !== -1) {
        const decimals = str.length - dotIndex - 1
        maxDecimals = Math.max(maxDecimals, decimals)
      }
    }
  }
  return maxDecimals
}

// 汇总行计算方法
const getSummaryRow = (param) => {
  const { columns } = param
  const data = filteredTableData.value

  if (!data || data.length === 0) {
    return []
  }

  const sums = []
  columns.forEach((column, index) => {
    // 第一列显示"合计"
    if (index === 0) {
      sums[index] = '合计'
      return
    }

    const fieldName = column.property
    const col = displayColumns.value.find(c => c.fieldName === fieldName)

    // 检查该列是否配置了汇总
    if (!col || col.summaryType === 'none' || !col.summaryType) {
      sums[index] = ''
      return
    }

    // 计算汇总值
    const values = data.map(row => row[fieldName]).filter(v => v !== null && v !== undefined && !isNaN(v))

    if (col.summaryType === 'sum') {
      const sum = values.reduce((acc, val) => acc + Number(val), 0)
      // 确定小数位数：优先使用配置值，否则自动检测
      const decimals = col.summaryDecimals !== null && col.summaryDecimals !== undefined
        ? col.summaryDecimals
        : getMaxDecimals(values)
      sums[index] = sum.toFixed(decimals)
    } else if (col.summaryType === 'avg') {
      if (values.length > 0) {
        const avg = values.reduce((acc, val) => acc + Number(val), 0) / values.length
        // 确定小数位数：优先使用配置值，否则默认2位
        const decimals = col.summaryDecimals !== null && col.summaryDecimals !== undefined
          ? col.summaryDecimals
          : 2
        sums[index] = avg.toFixed(decimals)
      } else {
        sums[index] = ''
      }
    } else {
      sums[index] = ''
    }
  })

  return sums
}

// 点击列标题排序
const handleColumnSort = (field) => {
  if (sortField.value === field) {
    // 循环: asc -> desc -> 无排序
    if (sortOrder.value === 'asc') {
      sortOrder.value = 'desc'
    } else if (sortOrder.value === 'desc') {
      sortField.value = null
      sortOrder.value = null
    }
  } else {
    sortField.value = field
    sortOrder.value = 'asc'
  }
}

// 打开筛选弹出框（懒加载方式）
const openFilterPopover = (event, col) => {
  // 创建虚拟触发元素
  filterTriggerRef.value = {
    getBoundingClientRect: () => event.target.getBoundingClientRect()
  }
  activeFilterColumn.value = col.fieldName
}

// 关闭筛选弹出框
const closeFilterPopover = (visible) => {
  if (!visible) {
    activeFilterColumn.value = null
    filterTriggerRef.value = null
  }
}

// Handle sort from popover
const handlePopoverSort = (fieldName, order) => {
  if (order === null) {
    sortField.value = null
    sortOrder.value = null
  } else {
    sortField.value = fieldName
    sortOrder.value = order
  }
  closeFilterPopover(false)
}

// Clear filter for specific column
const handleClearColumnFilter = (fieldName) => {
  delete columnFilters[fieldName]
  delete columnFilters[fieldName + '_min']
  delete columnFilters[fieldName + '_max']
  currentPage.value = 1
  closeFilterPopover(false)
}

// Apply filter (close popover)
const handleApplyColumnFilter = (fieldName) => {
  currentPage.value = 1
  closeFilterPopover(false)
}

// 重置列筛选
const resetColumnFilters = () => {
  Object.keys(columnFilters).forEach(key => delete columnFilters[key])
  sortField.value = null
  sortOrder.value = null
  currentPage.value = 1
}

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
  hasQueried.value = true
  querying.value = true
  try {
    const params = buildQueryParams()
    const res = await reportApi.executeReport(selectedReport.value.reportId, { parameters: params })
    if (res.success) {
      reportData.value = res.data
      // 重置筛选和分页
      resetColumnFilters()
      // 数据加载后更新表格高度
      updateTableHeight()
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
  gap: 16px;
  overflow: hidden;
  min-width: 0;
}

/* 侧边栏搜索框 */
.sidebar-search {
  padding: 12px;
  border-bottom: 1px solid var(--border-light);
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 56px;
}

.sidebar-icon-only {
  font-size: 20px;
  color: var(--primary-color);
  cursor: pointer;
}

/* 侧边栏列表区域 */
.sidebar-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.report-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.report-item {
  display: flex;
  align-items: center;
  padding: 12px;
  border-radius: 8px;
  cursor: pointer;
  margin-bottom: 4px;
  border-left: 3px solid transparent;
  /* transition is defined in animation section */
}

.report-item:hover {
  background-color: var(--bg-hover);
}

.report-item.active {
  background-color: var(--primary-light);
  border-left: 3px solid var(--primary-color);
}

.report-item-content {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
}

.report-item-icon {
  font-size: 20px;
  color: var(--primary-color);
  flex-shrink: 0;
}

.report-item-info {
  flex: 1;
  min-width: 0;
  overflow: hidden;
}

.report-item-name {
  font-weight: 500;
  margin-bottom: 4px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.report-item-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: #909399;
}

.view-count {
  font-size: 12px;
  color: #909399;
  margin-left: 8px;
}

/* 侧边栏收起状态下的报表项 */
.sidebar.collapsed .report-item {
  padding: 12px 8px;
  display: flex;
  justify-content: center;
}

.sidebar.collapsed .report-item-info {
  display: none;
}

/* 侧边栏收起/展开按钮 */
.sidebar-toggle {
  padding: 12px;
  border-top: 1px solid var(--border-light);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: #909399;
  transition: all 200ms ease;
}

.sidebar-toggle:hover {
  background-color: var(--bg-hover);
  color: var(--primary-color);
}

.empty-state {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  flex: 1;
}

/* 报表标题 */
.report-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-bottom: 12px;
  margin-bottom: 16px;
  border-bottom: 1px solid var(--border-light);
}

.report-header h2 {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: #303133;
}

/* 查询条件折叠 */
.conditions-collapse {
  border: none;
  background: var(--bg-card);
  border-radius: 8px;
  box-shadow: var(--shadow-card);
  margin-bottom: 16px;
}

.conditions-collapse :deep(.el-collapse-item__header) {
  border-bottom: none;
  height: 44px;
  font-size: 14px;
}

.conditions-collapse :deep(.el-collapse-item__wrap) {
  border-bottom: none;
}

.conditions-collapse :deep(.el-collapse-item__content) {
  padding: 0;
}

.collapse-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 500;
  color: #303133;
}

.conditions-content {
  padding: 16px;
}

.conditions-form {
  padding: 0 16px;
}

.conditions-actions {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid var(--border-light);
  display: flex;
  gap: 12px;
}

/* 查询条件表单样式 */
.operator-label {
  color: #909399;
  font-size: 14px;
}

.number-range-input {
  display: flex;
  align-items: center;
  gap: 8px;
}

.number-range-input .flex-1 {
  flex: 1;
}

.range-separator-text {
  color: #909399;
}

/* 操作按钮栏 */
.action-bar {
  display: flex;
  gap: 12px;
  margin-bottom: 16px;
}

/* 查询结果容器 */
.results-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: var(--bg-card);
  border-radius: 8px;
  box-shadow: var(--shadow-card);
  overflow: hidden;
  min-height: 0;  /* 关键：允许 flex 收缩 */
}

/* 表格工具栏 */
.table-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  border-bottom: 1px solid var(--border-light);
  background: #fafafa;
  flex-shrink: 0;
}

.result-count {
  font-size: 14px;
  color: #606266;
  font-weight: 500;
}

.toolbar-actions {
  display: flex;
  gap: 8px;
}

/* 表格包装器 */
.table-wrapper {
  flex: 1;
  min-height: 0;  /* 关键：允许 flex 收缩 */
  overflow: hidden;
  padding: 0;
}

/* ========== 表格样式优化 ========== */

/* 表格整体字体大小 */
:deep(.el-table) {
  font-size: 13px;
}

/* 表头样式 */
:deep(.el-table th.el-table__cell) {
  background-color: #f5f7fa !important;
  color: #606266;
  font-weight: 600;
  padding: 8px 0;
  border-bottom: 1px solid #e4e7ed;
}

/* 单元格内边距 */
:deep(.el-table td.el-table__cell) {
  padding: 6px 8px;
}

/* 单元格内容截断 */
:deep(.el-table .cell) {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 行悬停效果 */
:deep(.el-table tbody tr:hover > td.el-table__cell) {
  background-color: #ecf5ff !important;
}

/* 斑马纹颜色 - 更柔和 */
:deep(.el-table--striped .el-table__body tr.el-table__row--striped td.el-table__cell) {
  background-color: #fafafa;
}

/* 斑马纹悬停效果 */
:deep(.el-table--striped .el-table__body tr.el-table__row--striped:hover > td.el-table__cell) {
  background-color: #ecf5ff !important;
}

/* 合计行样式 */
:deep(.el-table__footer-wrapper .el-table__cell) {
  background-color: #f5f7fa !important;
  font-weight: 600;
  color: #303133;
  border-top: 2px solid #409eff;
}

:deep(.el-table__footer-wrapper .cell) {
  font-weight: 600;
}

/* 空行样式 */
:deep(.el-table .empty-row td) {
  background-color: #fff !important;
  color: transparent;
}

:deep(.el-table .empty-row:hover > td) {
  background-color: #fff !important;
}

/* 序号列样式 */
:deep(.el-table .el-table__cell:first-child) {
  text-align: center;
}

/* 列头样式 - Excel风格 */
.column-header-compact {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 4px 0;
  min-height: 24px;
}

.column-name {
  display: flex;
  align-items: center;
  gap: 4px;
  cursor: pointer;
  font-weight: 600;
  color: #303133;
  user-select: none;
  flex: 1;
}

.column-name:hover {
  color: var(--primary-color);
}

.sort-indicator {
  color: var(--primary-color);
  font-size: 12px;
}

.filter-icon {
  cursor: pointer;
  color: #909399;
  font-size: 14px;
  padding: 2px;
  border-radius: 4px;
  transition: all 0.2s ease;
}

.filter-icon:hover {
  color: var(--primary-color);
  background-color: var(--primary-light);
}

.filter-icon.active {
  color: var(--primary-color);
}

/* Popover 内容样式 */
.filter-popover-content {
  padding: 4px 0;
}

.filter-section {
  padding: 8px 12px;
  border-bottom: 1px solid #ebeef5;
}

.filter-section:last-of-type {
  border-bottom: none;
}

.filter-section-title {
  font-size: 12px;
  color: #909399;
  margin-bottom: 8px;
  font-weight: 500;
}

.sort-buttons {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.filter-input-wrapper {
  width: 100%;
}

.filter-input-wrapper .range-filter {
  display: flex;
  align-items: center;
  gap: 8px;
}

.filter-input-wrapper .range-filter :deep(.el-input-number) {
  flex: 1;
  min-width: 80px;
}

.range-separator {
  color: #909399;
  flex-shrink: 0;
}

.filter-input-wrapper :deep(.el-date-editor) {
  width: 100%;
}

.filter-input-wrapper :deep(.el-select) {
  width: 100%;
}

.filter-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding-top: 12px;
  border-top: 1px solid var(--border-light);
}

/* 分页 */
.pagination-wrapper {
  padding: 12px 0 0 0;
  display: flex;
  justify-content: flex-end;
  flex-shrink: 0;
  border-top: 1px solid var(--border-light);
  background: #fafafa;
}

/* 无数据 */
.no-data {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--bg-card);
  border-radius: 8px;
  box-shadow: var(--shadow-card);
  min-height: 300px;
}

/* 动画 */
.sidebar,
.report-item,
.action-bar button {
  transition: all var(--transition-speed) ease;
}

.report-item:hover {
  transform: translateX(2px);
}

/* 响应式 */
@media (max-width: 768px) {
  .sidebar {
    width: var(--sidebar-collapsed-width);
    min-width: var(--sidebar-collapsed-width);
  }

  .sidebar .report-item-info {
    display: none;
  }

  .main-content {
    padding: 12px;
  }

  .conditions-content {
    padding: 12px;
  }
}
</style>
