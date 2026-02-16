# Report Query Interface Redesign Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Redesign the report query interface with collapsible sidebar, column header filters, click-to-sort, and modern visual styling.

**Architecture:** Restructure ReportQuery.vue with a collapsible left sidebar (280px → 48px), enhanced El-Table with header filter inputs, and computed properties for client-side filtering/sorting. Use CSS variables for theming and transitions for animations.

**Tech Stack:** Vue 3 Composition API, Element Plus, CSS3 (flexbox, transitions, CSS variables)

---

## Task 1: Create CSS Variables and Base Layout Structure

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: Add CSS variables to style section**

Add CSS variables at the top of the `<style scoped>` section:

```css
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
}
```

**Step 2: Update main container layout**

Replace the outer container structure with a flexbox layout:

```vue
<template>
  <div class="report-query">
    <!-- 可折叠侧边栏 -->
    <div class="sidebar" :class="{ collapsed: sidebarCollapsed }">
      <!-- 侧边栏内容 -->
    </div>

    <!-- 主内容区域 -->
    <div class="main-content">
      <!-- 查询内容 -->
    </div>
  </div>
</template>
```

**Step 3: Add base CSS for layout**

```css
.report-query {
  display: flex;
  height: 100%;
  background-color: var(--bg-page);
}

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

.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 16px;
  overflow: hidden;
  min-width: 0;
}
```

**Step 4: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: add CSS variables and base layout structure for report query redesign"
```

---

## Task 2: Implement Collapsible Sidebar

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: Add sidebar state and localStorage persistence**

Add to `<script setup>`:

```javascript
// 侧边栏状态
const sidebarCollapsed = ref(false)

// 从 localStorage 恢复侧边栏状态
onMounted(async () => {
  const savedState = localStorage.getItem('reportQuerySidebarCollapsed')
  if (savedState !== null) {
    sidebarCollapsed.value = savedState === 'true'
  }
  await loadReports()
})

// 切换侧边栏
const toggleSidebar = () => {
  sidebarCollapsed.value = !sidebarCollapsed.value
  localStorage.setItem('reportQuerySidebarCollapsed', sidebarCollapsed.value)
}
```

**Step 2: Create sidebar template structure**

Replace the left column with the new sidebar structure:

```vue
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
    <div
      v-for="report in filteredReports"
      :key="report.reportId"
      :class="['sidebar-item', { active: selectedReportId === report.reportId }]"
      @click="selectReport(report)"
    >
      <el-tooltip :content="sidebarCollapsed ? report.reportName : ''" placement="right">
        <div class="sidebar-item-content">
          <el-icon class="sidebar-item-icon"><Document /></el-icon>
          <div v-if="!sidebarCollapsed" class="sidebar-item-info">
            <div class="sidebar-item-name">{{ report.reportName }}</div>
            <div class="sidebar-item-meta">
              <el-tag size="small" type="info">{{ report.reportCategory || '未分类' }}</el-tag>
            </div>
          </div>
        </div>
      </el-tooltip>
    </div>
  </div>

  <!-- 收起/展开按钮 -->
  <div class="sidebar-toggle" @click="toggleSidebar">
    <el-icon>
      <ArrowLeft v-if="!sidebarCollapsed" />
      <ArrowRight v-else />
    </el-icon>
  </div>
</div>
```

**Step 3: Add sidebar CSS styles**

```css
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

.sidebar-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.sidebar-item {
  padding: 12px;
  border-radius: 8px;
  cursor: pointer;
  margin-bottom: 4px;
  transition: all 200ms ease;
  border-left: 3px solid transparent;
}

.sidebar-item:hover {
  background-color: var(--bg-hover);
}

.sidebar-item.active {
  background-color: var(--primary-light);
  border-left-color: var(--primary-color);
}

.sidebar.collapsed .sidebar-item {
  padding: 12px 8px;
  display: flex;
  justify-content: center;
}

.sidebar-item-content {
  display: flex;
  align-items: center;
  gap: 12px;
}

.sidebar-item-icon {
  font-size: 20px;
  color: var(--primary-color);
  flex-shrink: 0;
}

.sidebar-item-info {
  flex: 1;
  min-width: 0;
  overflow: hidden;
}

.sidebar-item-name {
  font-weight: 500;
  font-size: 14px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sidebar-item-meta {
  margin-top: 4px;
}

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
```

**Step 4: Verify build**

```bash
cd frontend && npm run build
```
Expected: Build succeeded

**Step 5: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: implement collapsible sidebar with localStorage persistence"
```

---

## Task 3: Redesign Main Content Area

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: Create main content template**

Replace the right column with the new main content structure:

```vue
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
        <!-- 条件表单内容保持不变 -->
        <el-form :inline="true" :model="conditionForm" label-width="100px" class="conditions-form">
          <!-- 保持原有的条件输入控件 -->
        </el-form>
      </el-collapse-item>
    </el-collapse>

    <!-- 操作按钮 -->
    <div class="action-bar">
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
        <span class="result-count">共 {{ filteredTableData.length }} 条记录</span>
        <el-button link @click="showColumnSettings = true">
          <el-icon><Setting /></el-icon>
          列设置
        </el-button>
      </div>

      <!-- 增强表格 -->
      <div class="table-wrapper">
        <el-table
          ref="tableRef"
          :data="paginatedData"
          border
          stripe
          height="100%"
          @sort-change="handleSortChange"
        >
          <!-- 动态列 -->
          <el-table-column
            v-for="col in visibleDisplayColumns"
            :key="col.fieldName"
            :prop="col.fieldName"
            :label="col.displayName"
            :width="col.width"
            :align="col.align || 'left'"
            :sortable="'custom'"
          >
            <template #header>
              <div class="column-header">
                <span class="column-title" @click="handleColumnSort(col.fieldName)">
                  {{ col.displayName }}
                  <el-icon v-if="sortField === col.fieldName" class="sort-icon">
                    <ArrowUp v-if="sortOrder === 'asc'" />
                    <ArrowDown v-else />
                  </el-icon>
                </span>
                <div class="column-filter">
                  <!-- 根据类型显示不同筛选控件 -->
                  <el-input
                    v-if="col.dataType === 'String'"
                    v-model="columnFilters[col.fieldName]"
                    placeholder="筛选..."
                    size="small"
                    clearable
                    @click.stop
                  />
                  <div v-else-if="col.dataType === 'Number'" class="range-filter" @click.stop>
                    <el-input-number
                      v-model="columnFilters[col.fieldName + '_min']"
                      placeholder="最小"
                      size="small"
                      :controls="false"
                    />
                    <span>-</span>
                    <el-input-number
                      v-model="columnFilters[col.fieldName + '_max']"
                      placeholder="最大"
                      size="small"
                      :controls="false"
                    />
                  </div>
                  <el-date-picker
                    v-else-if="col.dataType === 'DateTime'"
                    v-model="columnFilters[col.fieldName]"
                    type="daterange"
                    size="small"
                    range-separator="-"
                    start-placeholder="开始"
                    end-placeholder="结束"
                    value-format="YYYY-MM-DD"
                    @click.stop
                  />
                  <el-select
                    v-else-if="col.dataType === 'Boolean'"
                    v-model="columnFilters[col.fieldName]"
                    placeholder="全部"
                    size="small"
                    clearable
                    @click.stop
                  >
                    <el-option label="是" :value="true" />
                    <el-option label="否" :value="false" />
                  </el-select>
                </div>
              </div>
            </template>
          </el-table-column>
        </el-table>
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
```

**Step 2: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: redesign main content area with enhanced table structure"
```

---

## Task 4: Implement Column Filtering Logic

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: Add filtering state and computed properties**

Add to `<script setup>`:

```javascript
// 表格筛选和排序状态
const columnFilters = reactive({})
const sortField = ref(null)
const sortOrder = ref(null) // 'asc' | 'desc'
const currentPage = ref(1)
const pageSize = ref(20)
const hasQueried = ref(false)
const showColumnSettings = ref(false)
const conditionsActive = ref(['conditions'])

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

// 分页后的数据
const paginatedData = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  const end = start + pageSize.value
  return filteredTableData.value.slice(start, end)
})

// 可见列
const visibleDisplayColumns = computed(() => {
  return displayColumns.value.filter(col => {
    // 默认所有列可见，后续可扩展列设置功能
    return true
  })
})

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

// 表格排序变化事件
const handleSortChange = ({ prop, order }) => {
  // 使用自定义排序，忽略 el-table 的排序
}

// 重置筛选
const resetColumnFilters = () => {
  Object.keys(columnFilters).forEach(key => delete columnFilters[key])
  sortField.value = null
  sortOrder.value = null
  currentPage.value = 1
}
```

**Step 2: Update handleQuery to set hasQueried**

```javascript
const handleQuery = async () => {
  querying.value = true
  hasQueried.value = true
  try {
    const params = buildQueryParams()
    const res = await reportApi.executeReport(selectedReport.value.reportId, { parameters: params })
    if (res.success) {
      reportData.value = res.data
      // 重置筛选和分页
      resetColumnFilters()
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
```

**Step 3: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: implement column filtering and sorting logic"
```

---

## Task 5: Add Visual Styling and Animations

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: Add complete CSS styles**

```css
/* 主内容区域 */
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-width: 0;
  gap: 16px;
}

/* 空状态 */
.empty-state {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 报表标题 */
.report-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-bottom: 12px;
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
}

.conditions-collapse :deep(.el-collapse-item__header) {
  border-bottom: none;
  height: 44px;
}

.conditions-collapse :deep(.el-collapse-item__wrap) {
  border-bottom: none;
}

.conditions-collapse :deep(.el-collapse-item__content) {
  padding-bottom: 16px;
}

.collapse-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 500;
  color: #303133;
}

.conditions-form {
  padding: 0 16px;
}

/* 操作按钮栏 */
.action-bar {
  display: flex;
  gap: 12px;
  flex-shrink: 0;
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
  min-height: 300px;
}

/* 表格工具栏 */
.table-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  border-bottom: 1px solid var(--border-light);
  background: #fafafa;
}

.result-count {
  font-size: 14px;
  color: #606266;
  font-weight: 500;
}

/* 表格包装器 */
.table-wrapper {
  flex: 1;
  overflow: hidden;
}

/* 列头样式 */
.column-header {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 4px 0;
}

.column-title {
  display: flex;
  align-items: center;
  gap: 4px;
  cursor: pointer;
  font-weight: 600;
  color: #303133;
}

.column-title:hover {
  color: var(--primary-color);
}

.sort-icon {
  color: var(--primary-color);
  font-size: 12px;
}

.column-filter {
  width: 100%;
}

.column-filter :deep(.el-input__wrapper) {
  background: #f5f7fa;
}

.column-filter :deep(.el-input__wrapper:focus-within) {
  background: #fff;
}

.range-filter {
  display: flex;
  align-items: center;
  gap: 4px;
}

.range-filter :deep(.el-input-number) {
  width: 80px;
}

.range-filter span {
  color: #909399;
}

/* 分页 */
.pagination-wrapper {
  padding: 12px 16px;
  border-top: 1px solid var(--border-light);
  display: flex;
  justify-content: flex-end;
  background: #fafafa;
}

/* 无数据 */
.no-data {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 动画 */
.sidebar,
.sidebar-item,
.action-bar button {
  transition: all var(--transition-speed) ease;
}

.sidebar-item:hover {
  transform: translateX(2px);
}

/* 响应式 */
@media (max-width: 768px) {
  .sidebar {
    width: var(--sidebar-collapsed-width);
    min-width: var(--sidebar-collapsed-width);
  }
}
```

**Step 2: Add Filter icon import**

Make sure to import the Filter icon if not already:

```javascript
import { Search, Document, ArrowRight, ArrowLeft, Download, Filter, Setting, ArrowUp, ArrowDown } from '@element-plus/icons-vue'
```

**Step 3: Verify build**

```bash
cd frontend && npm run build
```
Expected: Build succeeded

**Step 4: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: add visual styling and animations for report query interface"
```

---

## Task 6: Final Testing and Polish

**Step 1: Test frontend build**

```bash
cd frontend && npm run build
```
Expected: Build succeeded without errors

**Step 2: Test functionality**

1. Start backend: `dotnet run --project backend/src/DataForgeStudio.Api`
2. Start frontend: `cd frontend && npm run dev`
3. Navigate to Report Query page
4. Test sidebar collapse/expand
5. Test column filtering on different data types
6. Test column sorting (click header)
7. Verify pagination works with filtered data
8. Check responsive behavior at different screen sizes

**Step 3: Final commit**

```bash
git add -A
git commit -m "feat: complete report query interface redesign"
```

---

## 完成标准

- [ ] 侧边栏可收起/展开，状态保存到 localStorage
- [ ] 表格占据主内容区域大部分空间
- [ ] 列头筛选支持文本、数字、日期、布尔类型
- [ ] 点击列标题可排序（升序→降序→无序）
- [ ] 分页与筛选结果联动
- [ ] 视觉效果美观，有过渡动画
- [ ] 响应式设计，小屏幕自动收起侧边栏
