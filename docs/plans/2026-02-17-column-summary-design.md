# 字段汇总功能设计

**日期**: 2026-02-17
**状态**: 已批准

---

## 需求

在报表设计器的字段配置中添加汇总功能，允许用户为数字类型字段配置求和或平均值，在报表查询时显示汇总行。

## 设计方案

### 1. 数据结构变更

将现有的 `isSortable: boolean` 改为 `summaryType: string`：

```javascript
// 字段配置
{
  fieldName: 'amount',
  displayName: '金额',
  dataType: 'Number',
  summaryType: 'sum',  // 'none' | 'sum' | 'avg'
  // ... 其他字段
}
```

### 2. 字段配置 UI (ReportDesigner)

在虚拟滚动表格中，将「排序」列改为「汇总」列：

| 选项 | 值 | 可选条件 |
|------|-----|---------|
| 无 | `none` | 所有类型 |
| 求和 | `sum` | 仅 Number |
| 平均值 | `avg` | 仅 Number |

**实现方式：** 使用 `el-select`，根据 `dataType` 动态过滤选项。

### 3. 报表查询显示 (ReportQuery)

在表格底部添加汇总行：

- 遍历 `displayColumns`，找出 `summaryType !== 'none'` 的列
- 计算对应列的汇总值
- 使用 `el-table` 的 `show-summary` 和 `summary-method` 属性显示汇总

**汇总行示例：**
```
| 商品名称 | 销量(sum) | 金额(avg) |
|---------|-----------|-----------|
| 商品A   | 10        | 100       |
| 商品B   | 20        | 200       |
|---------|-----------|-----------|
| 合计    | 30        | 150       |
```

### 4. 兼容性处理

- 旧数据中 `isSortable` 字段将被忽略
- 默认 `summaryType` 为 `'none'`
- 后端无需改动（字段配置以 JSON 存储）

## 影响范围

| 文件 | 改动 |
|------|------|
| `ReportDesigner.vue` | 字段配置列定义 |
| `ReportQuery.vue` | 表格汇总行计算和显示 |
| 数据库 | 无需改动（JSON 字段） |
| 后端 | 无需改动 |
