# Excel-Style Column Filter Design

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:writing-plans to create implementation plan from this design.

**Date:** 2026-02-16
**Component:** `frontend/src/views/report/ReportQuery.vue`

## Overview

Replace the current inline filter inputs (displayed below column names) with an Excel-style filter popup triggered by a filter icon in the column header.

## Current State

```
┌─────────────────────┐
│ Column Name    ▲    │  ← Click to sort
├─────────────────────┤
│ [ Filter Input... ] │  ← Always visible below
└─────────────────────┘
```

**Issues:**
- Filter inputs take up vertical space in header
- Cluttered appearance when many columns
- Not the familiar Excel-style interaction users expect

## Target State

```
┌─────────────────────┐
│ Column Name    ▼ 🔍 │  ← Sort icon + Filter icon
└─────────────────────┘
         │
         ▼ Click filter icon
┌─────────────────────────┐
│ 排序                    │
│ [↑ 升序]  [↓ 降序]  [✕] │
├─────────────────────────┤
│ 筛选                    │
│ [ Filter Input... ]     │
├─────────────────────────┤
│         [清除] [应用]   │
└─────────────────────────┘
```

## Design Decisions

### 1. Interaction Model
- **Click to toggle**: Click filter icon to show/hide popup
- **Click outside to close**: Clicking outside popover dismisses it
- **One popover at a time**: Opening a new popover closes any open one

### 2. Popover Content
- **Sort section**: Sort buttons (升序/降序) + clear sort button
- **Filter section**: Context-appropriate filter input based on data type
- **Action buttons**: 清除 (Clear) and 应用 (Apply)

### 3. Positioning
- **Placement**: `bottom-start` (below header, left-aligned with column)
- **Width**: 280px fixed width for consistency

### 4. Component Choice
- **Element**: `el-popover` component
- **Trigger**: `click` (not hover)
- **Reasoning**: Built-in click-outside handling, consistent Element Plus styling

## Implementation Details

### New State Variables

```javascript
// Track popover visibility per column
const filterPopoverVisible = reactive({})

// Helper to check if column has active filter
const hasColumnFilter = (fieldName) => {
  // Check if any filter value exists for this field
  const keys = [fieldName, fieldName + '_min', fieldName + '_max']
  return keys.some(key => {
    const val = columnFilters[key]
    return val !== null && val !== undefined && val !== ''
  })
}
```

### New Template Structure

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

### New Methods

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

// Clear filter for specific column
const handleClearColumnFilter = (fieldName) => {
  delete columnFilters[fieldName]
  delete columnFilters[fieldName + '_min']
  delete columnFilters[fieldName + '_max']
  currentPage.value = 1
  filterPopoverVisible[fieldName] = false
}

// Apply filter (close popover)
const handleApplyColumnFilter = (fieldName) => {
  currentPage.value = 1
  filterPopoverVisible[fieldName] = false
}

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

### CSS Changes

**Remove old styles:**
- `.column-header` (flex-direction: column)
- `.column-filter` and related styles
- `.column-filter :deep()` styles

**Add new styles:**

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

## Files to Modify

| File | Changes |
|------|---------|
| `frontend/src/views/report/ReportQuery.vue` | Replace inline filters with popover-based filters |

## Success Criteria

- [ ] Filter icon appears on right side of each column header
- [ ] Filter icon shows active state when column has filter
- [ ] Clicking filter icon opens popover with sort and filter options
- [ ] Popover contains sort buttons and appropriate filter input for data type
- [ ] Clicking outside popover closes it
- [ ] Only one popover open at a time
- [ ] Apply button closes popover and applies filter
- [ ] Clear button removes filter and closes popover
- [ ] Sort buttons apply sort and close popover
- [ ] Table header height is reduced (no inline filter inputs)
