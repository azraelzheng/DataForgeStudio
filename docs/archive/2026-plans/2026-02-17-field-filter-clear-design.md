# 报表设计器字段过滤和清空功能设计

**日期**: 2026-02-17
**状态**: 已批准

---

## 背景

报表设计器中，当 SQL 语句返回大量字段（如 140 个）时，用户手动添加字段时需要记住或复制字段名，效率较低。

## 需求

1. **字段名关键字过滤**: "添加字段"后，字段名输入框支持关键字过滤，从 SQL 解析结果中快速选择
2. **清空所有字段**: 添加一个按钮一键清空所有已配置的字段

## 设计方案

### 1. 数据结构

新增 `availableFields` 响应式变量存储 SQL 解析的字段列表：

```javascript
const availableFields = ref([])
```

### 2. 字段名输入改造

将"字段名"列从 `el-input` 改为 `el-select`，启用 `filterable` 属性：

```html
<el-select
  v-model="row.fieldName"
  size="small"
  filterable
  clearable
  placeholder="输入关键字筛选"
  @change="onFieldNameChange(row)"
>
  <el-option
    v-for="field in availableFields"
    :key="field.fieldName"
    :label="field.displayName"
    :value="field.fieldName"
  />
</el-select>
```

选择字段后自动填充 `displayName` 和 `dataType`。

### 3. 清空字段按钮

在"添加字段"按钮旁添加"清空字段"按钮：

```html
<el-button type="danger" @click="handleClearFields" :disabled="form.columns.length === 0">
  <el-icon><Delete /></el-icon>
  清空字段
</el-button>
```

处理函数：

```javascript
const handleClearFields = () => {
  form.columns = []
  form.queryConditions = []  // 同时清空相关查询条件
}
```

### 4. 用户流程

1. 用户编写 SQL 并点击"自动识别"
2. 字段加载到 `form.columns`（表格）和 `availableFields`（下拉选项）
3. 用户点击"添加字段"手动添加时：
   - 字段名显示可过滤的下拉框
   - 输入关键字即时过滤
   - 选择后自动填充显示名和数据类型
4. "清空字段"按钮一键移除所有字段

## 性能考虑

- API `/api/reports/query-schema` 只返回元数据（字段名、类型），不返回实际数据行
- 140 个字段测试：API 响应 ~43ms，性能优秀
- `el-select` 的 `filterable` 属性提供即时客户端过滤

## 影响范围

- 文件: `frontend/src/views/report/ReportDesigner.vue`
- 无后端改动
- 无数据库改动
