# 汇总小数位数功能设计

**日期**: 2026-02-18
**状态**: 已批准

---

## 需求

1. **求和自动检测小数位**：汇总行的小数位数应与数据中最大小数位数一致
   - 例如：20.358(3位) + 31(0位) + 23.23(2位) + 12.1(1位) → 合计显示3位小数

2. **平均值自定义小数位**：在报表设计器中，当字段设置为"平均值"时，可指定小数位数

---

## 设计方案

### 1. 数据结构变更

在字段配置中添加 `summaryDecimals` 字段：

```javascript
{
  fieldName: 'amount',
  displayName: '金额',
  dataType: 'Number',
  summaryType: 'avg',      // 'none' | 'sum' | 'avg'
  summaryDecimals: null    // 小数位数，null 表示自动检测
}
```

### 2. 报表设计器 UI 变更

在虚拟滚动表格中，在"汇总"列后面添加"小数位"列：

| 字段名 | 显示名称 | 数据类型 | ... | 汇总 | 小数位 |
|--------|----------|----------|-----|------|--------|
| amount | 金额 | Number | ... | 平均值 | 2 |
| qty | 数量 | Number | ... | 求和 | (空) |

**"小数位"列逻辑：**
- 使用 `el-input-number`，min=0, max=10
- 仅当 `dataType === 'Number'` 时可编辑
- 值为空或 null 时表示"自动检测"

### 3. 报表查询汇总逻辑

**求和 (sum)：**
- 如果设置了 `summaryDecimals` → 使用该值
- 如果未设置 → 自动检测数据中的最大小数位数

**平均值 (avg)：**
- 如果设置了 `summaryDecimals` → 使用该值
- 如果未设置 → 默认 2 位小数

### 4. 自动检测最大小数位数的算法

```javascript
function getMaxDecimals(values) {
  let maxDecimals = 0
  for (const val of values) {
    const str = String(val)
    const dotIndex = str.indexOf('.')
    if (dotIndex !== -1) {
      const decimals = str.length - dotIndex - 1
      maxDecimals = Math.max(maxDecimals, decimals)
    }
  }
  return maxDecimals
}
```

### 5. 后端变更

**实体 (`Report.cs`)：**
```csharp
/// <summary>
/// 汇总值小数位数，null 表示自动检测
/// </summary>
public int? SummaryDecimals { get; set; }
```

**DTO (`ApiResponse.cs`)：**
```csharp
public int? SummaryDecimals { get; set; }
```

**服务 (`ReportService.cs`)：**
- 加载和保存时映射 `SummaryDecimals` 字段

**数据库迁移：**
```sql
ALTER TABLE [ReportFields] ADD [SummaryDecimals] int NULL;
```

---

## 影响范围

| 文件 | 改动 |
|------|------|
| `ReportDesigner.vue` | 添加"小数位"列 |
| `ReportQuery.vue` | 修改汇总计算逻辑 |
| `Report.cs` | 添加 `SummaryDecimals` 属性 |
| `ApiResponse.cs` | 添加 DTO 字段 |
| `ReportService.cs` | 映射新字段 |
| 数据库 | 添加 `SummaryDecimals` 列 |
