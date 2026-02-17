# 报表查询表格 UI 优化实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 优化报表查询表格的 UI 显示，提升数据展示清晰度和视觉效果

**Architecture:** 通过 CSS 样式优化和 Vue 模板调整实现表格美化

**Tech Stack:** Vue 3, Element Plus, CSS

---

## Task 1: 添加表格整体样式优化

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (CSS section)

**Step 1: 添加表格样式变量和基础样式**

在 `<style scoped>` 中添加表格样式：

```css
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
```

**Step 2: 验证前端编译**

检查 Vite HMR 输出，确保无编译错误。

**Step 3: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "style: add table base styles for UI optimization"
```

---

## Task 2: 添加行样式和悬停效果

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (CSS section)

**Step 1: 添加行悬停和斑马纹样式**

在表格样式后面添加：

```css
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
```

**Step 2: 验证前端编译**

检查 Vite HMR 输出。

**Step 3: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "style: add row hover and stripe styles"
```

---

## Task 3: 添加合计行样式

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (CSS section)

**Step 1: 添加合计行样式**

在表格样式后面添加：

```css
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
```

**Step 2: 验证前端编译**

检查 Vite HMR 输出。

**Step 3: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "style: add summary row styles"
```

---

## Task 4: 添加单元格 Tooltip 支持

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (template and script)

**Step 1: 添加 el-table-column 的默认插槽**

修改 `el-table-column` 添加 tooltip：

```html
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
```

**Step 2: 验证前端编译**

检查 Vite HMR 输出。

**Step 3: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: add cell tooltip for overflow content"
```

---

## Task 5: 测试与验证

**Step 1: 测试不同数据类型**

- 日期类型是否正确显示
- 长文本是否截断并显示 tooltip
- 数字是否正常显示

**Step 2: 测试视觉效果**

- 行悬停效果是否明显
- 斑马纹颜色是否柔和
- 合计行是否突出显示

**Step 3: 测试空间利用**

- 表格是否紧凑但不拥挤
- 字体大小是否清晰可读

**Step 4: 最终 Commit（如有修复）**

```bash
git add -A
git commit -m "fix: table UI optimization adjustments"
```

---

## 完成检查清单

- [ ] 表格整体样式（字体大小、内边距）
- [ ] 表头样式（背景色、字体加粗）
- [ ] 行悬停效果
- [ ] 斑马纹颜色
- [ ] 合计行样式
- [ ] 单元格截断 + tooltip
- [ ] 测试通过
