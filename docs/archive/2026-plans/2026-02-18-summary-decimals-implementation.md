# 汇总小数位数功能实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为报表汇总功能添加智能小数位数处理：求和自动检测最大小数位，平均值支持自定义小数位

**Architecture:**
- 后端：在 ReportField 实体和 DTO 中添加 SummaryDecimals 字段
- 前端：ReportDesigner 添加"小数位"列，ReportQuery 修改汇总计算逻辑

**Tech Stack:** ASP.NET Core 8.0, Vue 3, Element Plus, EF Core

---

## Task 1: 后端 - 添加 SummaryDecimals 属性到实体

**Files:**
- Modify: `backend/src/DataForgeStudio.Domain/Entities/Report.cs`

**Step 1: 添加 SummaryDecimals 属性**

在 `ReportField` 类中，`SummaryType` 属性后面添加：

```csharp
/// <summary>
/// 汇总类型：none（无）, sum（求和）, avg（平均值）
/// </summary>
[MaxLength(10)]
public string? SummaryType { get; set; } = "none";

/// <summary>
/// 汇总值小数位数，null 表示自动检测
/// </summary>
public int? SummaryDecimals { get; set; }

public bool IsFilterable { get; set; } = false;
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build --no-restore`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Domain/Entities/Report.cs
git commit -m "feat: add SummaryDecimals property to ReportField entity"
```

---

## Task 2: 后端 - 添加 SummaryDecimals 到 DTO

**Files:**
- Modify: `backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs`

**Step 1: 在 ReportFieldDto 中添加 SummaryDecimals**

找到 `ReportFieldDto` 类，在 `SummaryType` 后添加：

```csharp
public class ReportFieldDto
{
    public string FieldName { get; set; }
    public string DisplayName { get; set; }
    public string DataType { get; set; }
    public int Width { get; set; }
    public string Align { get; set; }
    public bool IsVisible { get; set; }
    public bool IsSortable { get; set; }
    /// <summary>
    /// 汇总类型：none, sum, avg
    /// </summary>
    public string? SummaryType { get; set; }
    /// <summary>
    /// 汇总值小数位数，null 表示自动检测
    /// </summary>
    public int? SummaryDecimals { get; set; }
}
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build --no-restore`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs
git commit -m "feat: add SummaryDecimals to ReportFieldDto"
```

---

## Task 3: 后端 - 更新 ReportService 映射

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs`

**Step 1: 更新 GetReportByIdAsync 映射**

找到 `GetReportByIdAsync` 方法中的 `Columns` 映射，添加 `SummaryDecimals`：

```csharp
Columns = report.Fields.OrderBy(f => f.SortOrder).Select(f => new ReportFieldDto
{
    FieldName = f.FieldName,
    DisplayName = f.DisplayName,
    DataType = f.DataType,
    Width = f.Width,
    Align = f.Align,
    IsVisible = f.IsVisible,
    IsSortable = f.IsSortable,
    SummaryType = f.SummaryType ?? "none",
    SummaryDecimals = f.SummaryDecimals
}).ToList(),
```

**Step 2: 更新 CreateReportAsync 字段保存**

找到 `CreateReportAsync` 方法中添加字段的代码，添加 `SummaryDecimals`：

```csharp
_context.ReportFields.Add(new ReportField
{
    ReportId = report.ReportId,
    FieldName = column.FieldName,
    DisplayName = column.DisplayName,
    DataType = column.DataType,
    Width = column.Width,
    Align = column.Align,
    IsVisible = column.IsVisible,
    IsSortable = column.IsSortable,
    SummaryType = column.SummaryType ?? "none",
    SummaryDecimals = column.SummaryDecimals,
    SortOrder = sortOrder++
});
```

**Step 3: 更新 UpdateReportAsync 字段保存**

找到 `UpdateReportAsync` 方法中添加新字段的代码，添加 `SummaryDecimals`：

```csharp
_context.ReportFields.Add(new ReportField
{
    ReportId = report.ReportId,
    FieldName = column.FieldName,
    DisplayName = column.DisplayName,
    DataType = column.DataType,
    Width = column.Width,
    Align = column.Align,
    IsVisible = column.IsVisible,
    IsSortable = column.IsSortable,
    SummaryType = column.SummaryType ?? "none",
    SummaryDecimals = column.SummaryDecimals,
    SortOrder = sortOrder++
});
```

**Step 4: 验证编译**

Run: `cd backend && dotnet build --no-restore`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/ReportService.cs
git commit -m "feat: map SummaryDecimals in ReportService"
```

---

## Task 4: 后端 - 创建数据库迁移

**Files:**
- Create: Migration file (auto-generated)

**Step 1: 创建迁移**

Run: `cd backend/src/DataForgeStudio.Data && dotnet ef migrations add AddSummaryDecimalsToReportField --startup-project ../DataForgeStudio.Api`

Expected: "Done. To undo this action, use 'ef migrations remove'"

**Step 2: 应用迁移**

Run: `cd backend/src/DataForgeStudio.Data && dotnet ef database update --startup-project ../DataForgeStudio.Api`

Expected: "Done."

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Data/Migrations/
git commit -m "feat: add database migration for SummaryDecimals"
```

---

## Task 5: 前端 - ReportDesigner 添加"小数位"列

**Files:**
- Modify: `frontend/src/views/report/ReportDesigner.vue`

**Step 1: 在 fieldColumns 中添加"小数位"列**

在 `summaryType` 列定义后面添加新列：

```javascript
  {
    key: 'summaryType',
    title: '汇总',
    width: 100,
    cellRenderer: ({ rowData }) => h(ElSelect, {
      modelValue: rowData.summaryType || 'none',
      'onUpdate:modelValue': (val) => { rowData.summaryType = val },
      size: 'small',
      disabled: rowData.dataType !== 'Number',
      options: rowData.dataType === 'Number'
        ? [
            { label: '无', value: 'none' },
            { label: '求和', value: 'sum' },
            { label: '平均值', value: 'avg' }
          ]
        : [
            { label: '无', value: 'none' }
          ]
    })
  },
  {
    key: 'summaryDecimals',
    title: '小数位',
    width: 80,
    cellRenderer: ({ rowData }) => h(ElInputNumber, {
      modelValue: rowData.summaryDecimals,
      'onUpdate:modelValue': (val) => { rowData.summaryDecimals = val },
      size: 'small',
      min: 0,
      max: 10,
      placeholder: '自动',
      controls: false,
      disabled: rowData.dataType !== 'Number'
    })
  },
```

**Step 2: 在 handleAddField 中添加默认值**

找到 `handleAddField` 函数，添加 `summaryDecimals: null`：

```javascript
const handleAddField = () => {
  form.columns.push({
    fieldName: '',
    displayName: '',
    dataType: 'String',
    width: 120,
    align: 'left',
    isVisible: true,
    isSortable: false,
    summaryType: 'none',
    summaryDecimals: null
  })
}
```

**Step 3: 在 handleAutoDetectFields 中添加字段**

找到 `handleAutoDetectFields` 函数中的 `detectedFields` 定义，添加 `summaryDecimals: null`：

```javascript
const detectedFields = res.data.map(field => ({
  fieldName: field.fieldName,
  displayName: field.fieldName,
  dataType: field.systemDataType,
  width: 120,
  align: field.systemDataType === 'Number' ? 'right' : 'left',
  isVisible: true,
  isSortable: false,
  summaryType: 'none',
  summaryDecimals: null
}))
```

**Step 4: 添加 ElInputNumber 导入**

在文件顶部的导入语句中添加 `ElInputNumber`：

```javascript
import { ElInput, ElSelect, ElInputNumber, ElSwitch, ElButton } from 'element-plus'
```

**Step 5: 验证前端编译**

Run: 检查 Vite HMR 输出或刷新页面
Expected: 无编译错误

**Step 6: Commit**

```bash
git add frontend/src/views/report/ReportDesigner.vue
git commit -m "feat: add summary decimals column to ReportDesigner"
```

---

## Task 6: 前端 - ReportQuery 实现智能小数位逻辑

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: 添加获取最大小数位数的辅助函数**

在 `getSummaryRow` 函数前面添加：

```javascript
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
```

**Step 2: 修改 getSummaryRow 函数**

替换原有的 `getSummaryRow` 函数：

```javascript
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
```

**Step 3: 验证前端编译**

Run: 检查 Vite HMR 输出
Expected: hmr update /src/views/report/ReportQuery.vue

**Step 4: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: implement smart decimal places for summary row"
```

---

## Task 7: 集成测试

**Step 1: 重启后端**

如果后端正在运行，先停止再启动：

```bash
# 停止当前运行的后端（如果有）
# 然后启动
cd "H:\开发项目\DataForgeStudio_V4"
dotnet run --project backend/src/DataForgeStudio.Api
```

**Step 2: 测试场景**

1. 打开报表设计器，创建/编辑一个报表
2. 添加数字类型字段，设置汇总为"求和"，小数位留空
3. 添加数字类型字段，设置汇总为"平均值"，小数位设置为 3
4. 保存报表
5. 在报表查询中执行查询
6. 验证：
   - 求和列的小数位数与数据中最大小数位数一致
   - 平均值列的小数位数为 3

**Step 3: 最终 Commit（如果需要修复）**

如果有任何修复：

```bash
git add -A
git commit -m "fix: summary decimals integration fixes"
```

---

## 完成检查清单

- [ ] 后端实体添加 SummaryDecimals 属性
- [ ] 后端 DTO 添加 SummaryDecimals 字段
- [ ] 后端服务映射 SummaryDecimals
- [ ] 数据库迁移已创建并应用
- [ ] 前端设计器添加"小数位"列
- [ ] 前端查询实现智能小数位逻辑
- [ ] 求和自动检测最大小数位
- [ ] 平均值支持自定义小数位
- [ ] 集成测试通过
