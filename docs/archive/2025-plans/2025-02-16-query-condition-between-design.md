# 查询条件"两者之间"功能设计

## 日期
2026-02-16

## 需求背景

用户希望在报表设计器的查询条件配置中添加"两者之间"（between）比较方式，用于：
- Number 类型：如金额在 100 到 1000 之间
- DateTime 类型：如日期在 2024-01-01 到 2024-12-31 之间

报表查询页面的 UI 需要根据这个操作符显示两个输入框。

## 当前实现

### ReportDesigner.vue
- `operatorOptions` 定义了各类型的操作符
- Number 和 DateTime 有 eq/ne/gt/lt/ge/null/notnull 等操作符
- 查询条件配置表格中只有一个默认值输入框

### ReportQuery.vue
- 根据操作符类型决定是否显示输入框
- `null/notnull/true/false` 不显示输入框
- 其他情况显示单个输入框

## 设计方案

### 1. 前端 - ReportDesigner.vue

**添加操作符:**
```javascript
Number: [
  // 现有操作符...
  { label: '两者之间', value: 'between' },
],
DateTime: [
  // 现有操作符...
  { label: '两者之间', value: 'between' },
]
```

**默认值处理:**
- between 操作符的默认值用逗号分隔，如 "100,1000" 或 "2024-01-01,2024-12-31"
- 或者保持为空，让用户在查询时输入

### 2. 前端 - ReportQuery.vue

**UI 变化:**
- 当 `operator === 'between'` 时，显示两个输入框
- Number 类型: 两个 `el-input-number`（起始值 ~ 结束值）
- DateTime 类型: 使用 `el-date-picker` 的 `type="daterange"`（日期范围选择器）

**表单绑定:**
- 单值操作符: `conditionForm[fieldName_operator]`
- between 操作符:
  - Number: `conditionForm[fieldName_between_start]` 和 `conditionForm[fieldName_between_end]`
  - DateTime: `conditionForm[fieldName_between]`（daterange 返回数组）

**构建查询参数:**
```javascript
// between 操作符传递数组
if (qc.operator === 'between') {
  const key = getFieldKey(qc)
  const value = conditionForm[key]
  if (value && value.length === 2) {
    params[key] = value  // [startValue, endValue]
  }
}
```

### 3. 后端 - ReportService.cs

**BuildQueryWithConditions 方法修改:**

```csharp
"between" => $"{fieldName} BETWEEN {paramName}_start AND {paramName}_end"
```

**参数处理:**
- 检测参数值为数组或逗号分隔的字符串
- 拆分为 start 和 end 两个参数

```csharp
case "between":
    if (value is JsonElement jsonEl && jsonEl.ValueKind == JsonValueKind.Array)
    {
        var arr = jsonEl.EnumerateArray().ToArray();
        if (arr.Length == 2)
        {
            var startParam = $"@{fieldName}_start";
            var endParam = $"@{fieldName}_end";
            whereClauses.Add($"{fieldName} BETWEEN {startParam} AND {endParam}");
            parameters[startParam] = ConvertJsonElement(arr[0]);
            parameters[endParam] = ConvertJsonElement(arr[1]);
        }
    }
    break;
```

### 4. 数据流程

```
[设计器] 选择"两者之间"
  -> 保存: { fieldName: 'amount', operator: 'between', displayName: '金额' }

[查询页面] 渲染
  -> DateTime: el-date-picker type="daterange"
  -> Number: 两个 el-input-number

[用户输入]
  -> DateTime: ['2024-01-01', '2024-12-31']
  -> Number: [100, 1000]

[查询参数]
  -> { 'createDate_between': ['2024-01-01', '2024-12-31'] }

[后端 SQL]
  -> WHERE createDate BETWEEN @createDate_start AND @createDate_end
```

## 实现计划

### Task 1: ReportDesigner.vue 添加 between 操作符
- 在 Number 和 DateTime 的 operatorOptions 中添加 `{ label: '两者之间', value: 'between' }`

### Task 2: ReportQuery.vue 修改 UI
- 添加 `isBetweenOperator` 辅助函数
- DateTime between: 使用 `el-date-picker type="daterange"`
- Number between: 显示两个 `el-input-number`

### Task 3: ReportQuery.vue 修改参数构建
- 修改 `buildQueryParams` 函数处理 between 操作符
- 传递数组格式的参数值

### Task 4: 后端 ReportService 处理 between
- 修改 `BuildQueryWithConditions` 方法
- 添加 between 操作符的 SQL 生成逻辑

### Task 5: 测试验证
- 测试 Number 类型的 between 查询
- 测试 DateTime 类型的 between 查询
- 验证 SQL 正确生成

## UI 预览

**Number 类型两者之间:**
```
┌─────────────────────────────────────┐
│ 金额: [___100___] ~ [___1000___]    │
└─────────────────────────────────────┘
```

**DateTime 类型两者之间:**
```
┌─────────────────────────────────────┐
│ 日期: [2024-01-01] 至 [2024-12-31]  │
│       (daterange 选择器)             │
└─────────────────────────────────────┘
```
