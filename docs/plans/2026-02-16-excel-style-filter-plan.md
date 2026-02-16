# Excel-Style Column Filter Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace inline column filter inputs with Excel-style filter popups triggered by filter icons in column headers.

**Architecture:** Use Element Plus `el-popover` component with click trigger to display sort options and filter inputs. One popover open at a time, click-outside to close. State managed via reactive `filterPopoverVisible` object.

**Tech Stack:** Vue 3 Composition API, Element Plus (el-popover, el-button, el-input, el-input-number, el-date-picker, el-select)

---

## Task 1: Add State Variables and Helper Functions

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (script section)

**Step 1: Add filterPopoverVisible reactive state**

Add after line 338 (`const pageSize = ref(20)`):

```javascript
// Popover visibility state per column
const filterPopoverVisible = reactive({})
```

**Step 2: Add hasColumnFilter helper function**

Add after the `displayColumns` computed property (around line 410):

```javascript
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
```

**Step 3: Verify build**

```bash
cd frontend && npm run build
```
Expected: Build succeeded

**Step 4: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: add popover state and hasColumnFilter helper for Excel-style filters"
```

---

## Task 2: Add Popover Handler Methods

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (script section)

**Step 1: Add handlePopoverToggle method**

Add after `handleColumnSort` function (around line 511):

```javascript
// Handle popover toggle (close others when opening new)
const handlePopoverToggle = (fieldName, visible) => {
  if (visible) {
    // Close all other popovers
    Object.keys(filterPopoverVisible).forEach(key => {
      filterPopoverVisible[key] = false
    })
  }
  filterPopoverVisible[fieldName] = visible
}
```

**Step 2: Add handlePopoverSort method**

Add after `handlePopoverToggle`:

```javascript
// Handle sort from popover
const handlePopoverSort = (fieldName, order) => {
  if (order === null) {
    sortField.value = null
    sortOrder.value = null
  } else {
    sortField.value = fieldName
    sortOrder.value = order
  }
  filterPopoverVisible[fieldName] = false
}
```

**Step 3: Add handleClearColumnFilter method**

Add after `handlePopoverSort`:

```javascript
// Clear filter for specific column
const handleClearColumnFilter = (fieldName) => {
  delete columnFilters[fieldName]
  delete columnFilters[fieldName + '_min']
  delete columnFilters[fieldName + '_max']
  currentPage.value = 1
  filterPopoverVisible[fieldName] = false
}
```

**Step 4: Add handleApplyColumnFilter method**

Add after `handleClearColumnFilter`:

```javascript
// Apply filter (close popover)
const handleApplyColumnFilter = (fieldName) => {
  currentPage.value = 1
  filterPopoverVisible[fieldName] = false
}
```

**Step 5: Verify build**

```bash
cd frontend && npm run build
```
Expected: Build succeeded

**Step 6: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: add popover handler methods for Excel-style column filters"
```

---

## Task 3: Replace Column Header Template

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (template section)

**Step 1: Replace the el-table-column template #header**

Find the current `<template #header>` block (around lines 223-282) and replace it entirely with:

```vue
<template #header>
  <div class="column-header-compact">
    <span class="column-name" @click="handleColumnSort(col.fieldName)">
      {{ col.displayName }}
      <span v-if="sortField === col.fieldName" class="sort-indicator">
        {{ sortOrder === 'asc' ? '▲' : '▼' }}
      </span>
    </span>
    <el-popover
      :visible="filterPopoverVisible[col.fieldName]"
      placement="bottom-start"
      :width="280"
      trigger="click"
      @update:visible="(v) => handlePopoverToggle(col.fieldName, v)"
    >
      <template #reference>
        <el-icon
          class="filter-icon"
          :class="{ active: hasColumnFilter(col.fieldName) }"
          @click.stop
        >
          <Filter />
        </el-icon>
      </template>

      <!-- Popover content -->
      <div class="filter-popover-content">
        <!-- Sort section -->
        <div class="filter-section">
          <div class="filter-section-title">排序</div>
          <div class="sort-buttons">
            <el-button
              size="small"
              :type="sortField === col.fieldName && sortOrder === 'asc' ? 'primary' : 'default'"
              @click="handlePopoverSort(col.fieldName, 'asc')"
            >
              ↑ 升序
            </el-button>
            <el-button
              size="small"
              :type="sortField === col.fieldName && sortOrder === 'desc' ? 'primary' : 'default'"
              @click="handlePopoverSort(col.fieldName, 'desc')"
            >
              ↓ 降序
            </el-button>
            <el-button
              v-if="sortField === col.fieldName"
              size="small"
              @click="handlePopoverSort(col.fieldName, null)"
            >
              ✕
            </el-button>
          </div>
        </div>

        <!-- Filter section - varies by data type -->
        <div class="filter-section">
          <div class="filter-section-title">筛选</div>
          <div class="filter-input-wrapper">
            <!-- String -->
            <el-input
              v-if="col.dataType === 'String' || !col.dataType"
              v-model="columnFilters[col.fieldName]"
              placeholder="输入筛选文本..."
              size="small"
              clearable
            />
            <!-- Number range -->
            <div v-else-if="col.dataType === 'Number'" class="range-filter">
              <el-input-number
                v-model="columnFilters[col.fieldName + '_min']"
                placeholder="最小"
                size="small"
                :controls="false"
              />
              <span class="range-separator">~</span>
              <el-input-number
                v-model="columnFilters[col.fieldName + '_max']"
                placeholder="最大"
                size="small"
                :controls="false"
              />
            </div>
            <!-- DateTime range -->
            <el-date-picker
              v-else-if="col.dataType === 'DateTime'"
              v-model="columnFilters[col.fieldName]"
              type="daterange"
              size="small"
              range-separator="-"
              start-placeholder="开始"
              end-placeholder="结束"
              value-format="YYYY-MM-DD"
            />
            <!-- Boolean select -->
            <el-select
              v-else-if="col.dataType === 'Boolean'"
              v-model="columnFilters[col.fieldName]"
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
          <el-button size="small" @click="handleClearColumnFilter(col.fieldName)">
            清除
          </el-button>
          <el-button type="primary" size="small" @click="handleApplyColumnFilter(col.fieldName)">
            应用
          </el-button>
        </div>
      </div>
    </el-popover>
  </div>
</template>
```

**Step 2: Verify build**

```bash
cd frontend && npm run build
```
Expected: Build succeeded

**Step 3: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: replace inline column filters with Excel-style popover filters"
```

---

## Task 4: Update CSS Styles

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (style section)

**Step 1: Remove old column header and filter styles**

Find and remove these CSS blocks (around lines 952-1007):

```css
/* REMOVE THESE */
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
  user-select: none;
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

.range-separator {
  color: #909399;
  font-size: 12px;
}
```

**Step 2: Add new compact header and popover styles**

Add these new CSS rules where the old ones were removed:

```css
/* Compact column header */
.column-header-compact {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 4px 0;
}

.column-name {
  display: flex;
  align-items: center;
  gap: 4px;
  cursor: pointer;
  font-weight: 600;
  color: #303133;
  user-select: none;
}

.column-name:hover {
  color: var(--primary-color);
}

.sort-indicator {
  color: var(--primary-color);
  font-size: 12px;
}

.filter-icon {
  font-size: 14px;
  color: #909399;
  cursor: pointer;
  padding: 2px;
  border-radius: 4px;
  transition: all 200ms ease;
}

.filter-icon:hover {
  color: var(--primary-color);
  background: var(--primary-light);
}

.filter-icon.active {
  color: var(--primary-color);
  background: var(--primary-light);
}

/* Popover content */
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
}

.sort-buttons {
  display: flex;
  gap: 8px;
}

.filter-input-wrapper {
  margin-top: 4px;
}

.range-filter {
  display: flex;
  align-items: center;
  gap: 8px;
}

.range-filter :deep(.el-input-number) {
  width: 100px;
}

.range-separator {
  color: #909399;
  flex-shrink: 0;
}

.filter-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 8px 12px;
  background: #fafafa;
}
```

**Step 3: Verify build**

```bash
cd frontend && npm run build
```
Expected: Build succeeded

**Step 4: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: update CSS for compact column headers and filter popovers"
```

---

## Task 5: Final Testing and Polish

**Step 1: Verify build**

```bash
cd frontend && npm run build
```
Expected: Build succeeded without errors

**Step 2: Verify functionality checklist**

Run the application and verify:
1. Filter icon appears on right side of each column header
2. Filter icon shows active (blue) state when column has filter applied
3. Clicking filter icon opens popover with sort and filter options
4. Popover contains sort buttons (升序/降序) appropriate to column
5. Sort buttons highlight correctly when that sort is active
6. Clear sort button (✕) only appears when column is sorted
7. Filter input shows correct type for each data type (text, number range, date range, boolean select)
8. Clicking outside popover closes it
9. Only one popover can be open at a time
10. Apply button closes popover and applies filter
11. Clear button removes filter and closes popover
12. Table header height is reduced (no inline filter inputs visible)
13. Pagination resets to page 1 when filter is applied or cleared

**Step 3: Final commit (if any fixes needed)**

```bash
git add -A
git commit -m "feat: complete Excel-style column filter implementation"
```

---

## Completion Checklist

- [ ] Task 1: Add state variables and helper functions
- [ ] Task 2: Add popover handler methods
- [ ] Task 3: Replace column header template
- [ ] Task 4: Update CSS styles
- [ ] Task 5: Final testing and polish
