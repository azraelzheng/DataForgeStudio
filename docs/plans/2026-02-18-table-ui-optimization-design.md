# 报表查询表格 UI 优化设计

**日期**: 2026-02-18
**状态**: 已批准

---

## 需求

优化报表查询页面的查询结果表格 UI，目标是：
- 显示更多内容
- 确保清晰度
- 提升美观度

---

## 设计方案

### 1. 数据展示优化

| 类型 | 优化内容 |
|------|----------|
| **日期** | 统一格式 YYYY-MM-DD 或 YYYY-MM-DD HH:mm:ss |
| **长文本** | 超过宽度截断 + tooltip 显示完整内容 |
| **数字** | 保持原样，不添加千分符 |

### 2. 视觉效果优化

| 元素 | 优化内容 |
|------|----------|
| **行悬停** | 更明显的背景色变化（浅蓝色高亮） |
| **合计行** | 加粗字体，浅灰色背景区分 |
| **表头** | 浅灰色背景，固定不滚动 |
| **斑马纹** | 更柔和的交替颜色 |

### 3. 空间利用优化

| 元素 | 优化内容 |
|------|----------|
| **单元格内边距** | 8px 水平，6px 垂直 |
| **字体大小** | 13px |
| **行高** | 32px（紧凑模式） |

---

## CSS 样式变更

### 表格整体样式

```css
/* 表格整体样式优化 */
.el-table {
  font-size: 13px;
}

/* 表头样式 */
.el-table th.el-table__cell {
  background-color: #f5f7fa;
  color: #606266;
  font-weight: 600;
  padding: 8px 0;
}

/* 单元格内边距 */
.el-table td.el-table__cell {
  padding: 6px 8px;
}

/* 行悬停效果 */
.el-table tbody tr:hover > td {
  background-color: #ecf5ff !important;
}

/* 斑马纹颜色 */
.el-table--striped .el-table__body tr.el-table__row--striped td {
  background-color: #fafafa;
}

/* 合计行样式 */
.el-table__footer-wrapper .el-table__cell {
  background-color: #f5f7fa;
  font-weight: 600;
  color: #303133;
}
```

### 单元格内容截断

```css
/* 单元格内容截断 */
.el-table .cell {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
```

### 日期格式化

使用 el-table-column 的 formatter 属性：

```javascript
const formatDateValue = (row, column, cellValue) => {
  if (!cellValue) return ''
  // 检测日期格式
  if (column.property && column.property.toLowerCase().includes('date')) {
    // 如果包含时间，显示完整格式
    if (cellValue.includes(':')) {
      return cellValue.substring(0, 19) // YYYY-MM-DD HH:mm:ss
    }
    return cellValue.substring(0, 10) // YYYY-MM-DD
  }
  return cellValue
}
```

---

## 影响范围

| 文件 | 改动 |
|------|------|
| `ReportQuery.vue` | CSS 样式优化、添加 formatter |
