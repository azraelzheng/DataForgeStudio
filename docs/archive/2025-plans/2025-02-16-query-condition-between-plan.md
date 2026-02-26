# 查询条件"两者之间"功能 实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在报表设计器的查询条件配置中添加"两者之间"(between)比较方式，支持 Number 和 DateTime 类型，并在报表查询页面显示两个输入框。

**Architecture:** 前端在 ReportDesigner.vue 添加 between 操作符选项，在 ReportQuery.vue 根据 between 操作符显示双输入框（DateTime 使用 daterange，Number 使用两个 input-number）。后端 ReportService 修改 BuildQueryWithConditions 方法处理 between 操作符生成 SQL BETWEEN 语句。

**Tech Stack:** Vue 3, Element Plus, ASP.NET Core 8.0

---

## Task 1: ReportDesigner.vue 添加 between 操作符

**Files:**
- Modify: `frontend/src/views/report/ReportDesigner.vue`

**Step 1: 读取文件找到 operatorOptions 定义**

读取文件，找到 `operatorOptions` 对象定义的位置（约 367-403 行）。

**Step 2: 在 Number 和 DateTime 类型中添加 between 操作符**

在 `Number` 数组末尾（`'notnull'` 之后）添加：
```javascript
{ label: '两者之间', value: 'between' }
```

在 `DateTime` 数组末尾（`'notnull'` 之后）添加：
```javascript
{ label: '两者之间', value: 'between' }
```

修改后的代码：
```javascript
const operatorOptions = {
  String: [
    { label: '等于', value: 'eq' },
    { label: '不等于', value: 'ne' },
    { label: '包含', value: 'like' },
    { label: '开头是', value: 'start' },
    { label: '结尾是', value: 'end' },
    { label: '为空', value: 'null' },
    { label: '不为空', value: 'notnull' }
  ],
  Number: [
    { label: '等于', value: 'eq' },
    { label: '不等于', value: 'ne' },
    { label: '大于', value: 'gt' },
    { label: '小于', value: 'lt' },
    { label: '大于等于', value: 'ge' },
    { label: '小于等于', value: 'le' },
    { label: '为空', value: 'null' },
    { label: '不为空', value: 'notnull' },
    { label: '两者之间', value: 'between' }
  ],
  DateTime: [
    { label: '等于', value: 'eq' },
    { label: '不等于', value: 'ne' },
    { label: '之后', value: 'gt' },
    { label: '之前', value: 'lt' },
    { label: '不晚于', value: 'le' },
    { label: '不早于', value: 'ge' },
    { label: '为空', value: 'null' },
    { label: '不为空', value: 'notnull' },
    { label: '两者之间', value: 'between' }
  ],
  Boolean: [
    { label: '等于', value: 'eq' },
    { label: '为真', value: 'true' },
    { label: '为假', value: 'false' }
  ]
}
```

**Step 3: Commit**

```bash
git add frontend/src/views/report/ReportDesigner.vue
git commit -m "feat: add 'between' operator for Number and DateTime types"
```

---

## Task 2: ReportQuery.vue 添加 between 操作符标签

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: 读取文件找到 operatorLabels 定义**

读取文件，找到 `operatorLabels` 对象定义的位置（约 179-193 行）。

**Step 2: 添加 between 标签**

在 `operatorLabels` 对象中添加：
```javascript
'between': '两者之间'
```

修改后的代码：
```javascript
const operatorLabels = {
  'eq': '等于',
  'ne': '不等于',
  'gt': '大于',
  'lt': '小于',
  'ge': '大于等于',
  'le': '小于等于',
  'like': '包含',
  'start': '开头是',
  'end': '结尾是',
  'null': '为空',
  'notnull': '不为空',
  'true': '为真',
  'false': '为假',
  'between': '两者之间'
}
```

**Step 3: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: add 'between' operator label"
```

---

## Task 3: ReportQuery.vue 修改 UI 显示双输入框

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: 读取文件找到查询条件渲染部分**

读取文件，找到模板中渲染查询条件的部分（约 76-120 行）。

**Step 2: 修改模板，添加 between 操作符的 UI 处理**

将现有的条件渲染部分替换为：

```vue
<el-form :inline="true" :model="conditionForm" label-width="100px">
  <el-row :gutter="20">
    <el-col :span="12" v-for="qc in queryConditions" :key="qc.fieldName + qc.operator">
      <el-form-item :label="qc.displayName">
        <!-- 不需要输入值的操作符 -->
        <template v-if="['null', 'notnull', 'true', 'false'].includes(qc.operator)">
          <span style="color: #909399; font-size: 14px;">{{ getOperatorLabel(qc.operator) }}</span>
        </template>

        <!-- DateTime between: 日期范围选择器 -->
        <template v-else-if="qc.operator === 'between' && qc.dataType === 'DateTime'">
          <el-date-picker
            v-model="conditionForm[getFieldKey(qc)]"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
            style="width: 100%;"
          />
        </template>

        <!-- Number between: 两个数字输入框 -->
        <template v-else-if="qc.operator === 'between' && qc.dataType === 'Number'">
          <div style="display: flex; align-items: center; gap: 8px;">
            <el-input-number
              v-model="conditionForm[getFieldKey(qc) + '_start']"
              placeholder="最小值"
              :controls-position="'right'"
              style="flex: 1;"
            />
            <span style="color: #909399;">~</span>
            <el-input-number
              v-model="conditionForm[getFieldKey(qc) + '_end']"
              placeholder="最大值"
              :controls-position="'right'"
              style="flex: 1;"
            />
          </div>
        </template>

        <!-- String 类型 -->
        <template v-else-if="qc.dataType === 'String'">
          <el-input
            v-model="conditionForm[getFieldKey(qc)]"
            :placeholder="getOperatorPlaceholder(qc.operator)"
            clearable
          />
        </template>

        <!-- Number 类型 -->
        <template v-else-if="qc.dataType === 'Number'">
          <el-input-number
            v-model="conditionForm[getFieldKey(qc)]"
            :placeholder="getOperatorPlaceholder(qc.operator)"
            :controls-position="'right'"
            style="width: 100%;"
          />
        </template>

        <!-- DateTime 类型 -->
        <template v-else-if="qc.dataType === 'DateTime'">
          <el-date-picker
            v-model="conditionForm[getFieldKey(qc)]"
            type="date"
            :placeholder="getOperatorPlaceholder(qc.operator)"
            value-format="YYYY-MM-DD"
            style="width: 100%;"
          />
        </template>

        <!-- Boolean 类型 -->
        <template v-else-if="qc.dataType === 'Boolean'">
          <el-select
            v-model="conditionForm[getFieldKey(qc)]"
            placeholder="请选择"
            clearable
            style="width: 100%;"
          >
            <el-option label="是" :value="true" />
            <el-option label="否" :value="false" />
          </el-select>
        </template>
      </el-form-item>
    </el-col>
  </el-row>
</el-form>
```

**Step 3: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: add dual input UI for 'between' operator"
```

---

## Task 4: ReportQuery.vue 修改参数构建逻辑

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: 读取文件找到 buildQueryParams 函数**

读取文件，找到 `buildQueryParams` 函数定义的位置（约 293-307 行）。

**Step 2: 修改 buildQueryParams 函数处理 between 操作符**

将 `buildQueryParams` 函数替换为：

```javascript
const buildQueryParams = () => {
  const params = {}
  queryConditions.value.forEach(qc => {
    const key = getFieldKey(qc)

    // 对于不需要值的操作符，直接传递操作符标记
    if (['null', 'notnull', 'true', 'false'].includes(qc.operator)) {
      params[key] = qc.operator
      return
    }

    // between 操作符特殊处理
    if (qc.operator === 'between') {
      if (qc.dataType === 'DateTime') {
        // DateTime: daterange 返回数组
        const value = conditionForm[key]
        if (value && Array.isArray(value) && value.length === 2) {
          params[key] = value
        }
      } else if (qc.dataType === 'Number') {
        // Number: 从两个输入框获取值
        const startValue = conditionForm[key + '_start']
        const endValue = conditionForm[key + '_end']
        if (startValue !== null && startValue !== undefined &&
            endValue !== null && endValue !== undefined) {
          params[key] = [startValue, endValue]
        }
      }
      return
    }

    // 其他操作符：单值处理
    const value = conditionForm[key]
    if (value !== '' && value !== null && value !== undefined) {
      params[key] = value
    }
  })
  return params
}
```

**Step 3: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: handle 'between' operator in query params builder"
```

---

## Task 5: 后端 ReportService 处理 between 操作符

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs`

**Step 1: 读取文件找到 BuildQueryWithConditions 方法**

读取文件，找到 `BuildQueryWithConditions` 方法中的 switch 表达式（约 461-477 行）。

**Step 2: 在 switch 表达式中添加 between 处理**

在 switch 表达式中添加 `between` case（在 `"false"` 之后）：

```csharp
var clause = op.ToLower() switch
{
    "eq" => $"{fieldName} = {paramName}",
    "ne" => $"{fieldName} <> {paramName}",
    "gt" => $"{fieldName} > {paramName}",
    "lt" => $"{fieldName} < {paramName}",
    "ge" => $"{fieldName} >= {paramName}",
    "le" => $"{fieldName} <= {paramName}",
    "like" => $"{fieldName} LIKE '%' + {paramName} + '%'",
    "start" => $"{fieldName} LIKE {paramName} + '%'",
    "end" => $"{fieldName} LIKE '%' + {paramName}",
    "null" => $"{fieldName} IS NULL",
    "notnull" => $"{fieldName} IS NOT NULL",
    "true" => $"{fieldName} = 1",
    "false" => $"{fieldName} = 0",
    "between" => null,  // 特殊处理，在下面单独添加
    _ => null
};
```

**Step 3: 在 clause 生成后添加 between 的特殊处理逻辑**

在 `if (clause != null)` 块之后，添加 between 的处理：

```csharp
// between 操作符特殊处理
if (op.ToLower() == "between")
{
    var startParam = $"@p{paramIndex++}_start";
    var endParam = $"@p{paramIndex++}_end";

    // 解析 between 值（数组或逗号分隔字符串）
    var (startValue, endValue) = ParseBetweenValue(value);
    if (startValue != null && endValue != null)
    {
        whereClauses.Add($"{fieldName} BETWEEN {startParam} AND {endParam}");
        parameters[startParam] = startValue;
        parameters[endParam] = endValue;
    }
    continue;
}

if (clause != null)
{
    whereClauses.Add(clause);
    // 对于不需要值的操作符，不添加参数
    if (!new[] { "null", "notnull", "true", "false" }.Contains(op.ToLower()))
    {
        parameters[paramName] = value;
    }
}
```

**Step 4: 添加 ParseBetweenValue 辅助方法**

在类中添加辅助方法：

```csharp
/// <summary>
/// 解析 between 操作符的值
/// </summary>
private (object? start, object? end) ParseBetweenValue(object? value)
{
    if (value == null) return (null, null);

    // 处理 JsonElement（从前端传来的数组）
    if (value is JsonElement jsonEl)
    {
        if (jsonEl.ValueKind == JsonValueKind.Array)
        {
            var arr = jsonEl.EnumerateArray().ToArray();
            if (arr.Length == 2)
            {
                return (ConvertJsonElement(arr[0]), ConvertJsonElement(arr[1]));
            }
        }
        // 处理逗号分隔的字符串
        else if (jsonEl.ValueKind == JsonValueKind.String)
        {
            var str = jsonEl.GetString();
            if (!string.IsNullOrEmpty(str) && str.Contains(','))
            {
                var parts = str.Split(',');
                if (parts.Length == 2)
                {
                    return (parts[0].Trim(), parts[1].Trim());
                }
            }
        }
    }

    return (null, null);
}
```

**Step 5: 验证编译**

Run: `dotnet build backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj`
Expected: Build succeeded

**Step 6: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/ReportService.cs
git commit -m "feat: handle 'between' operator in SQL query builder"
```

---

## Task 6: 测试验证

**Step 1: 验证前端构建**

Run: `cd frontend && npm run build`
Expected: Build succeeded

**Step 2: 验证后端构建**

Run: `dotnet build backend/DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: 功能测试**

1. 启动后端和前端服务
2. 进入报表设计器，添加查询条件
3. 验证 Number 和 DateTime 类型显示"两者之间"选项
4. 选择"两者之间"，保存报表
5. 进入报表查询页面，验证显示双输入框
6. 输入值并查询，验证 SQL 正确执行

**Step 4: 最终 Commit**

```bash
git add -A
git commit -m "feat: complete 'between' operator for query conditions"
```

---

## 完成标准

- [ ] ReportDesigner.vue 中 Number 和 DateTime 类型显示"两者之间"选项
- [ ] ReportQuery.vue 中 between 操作符显示双输入框
- [ ] DateTime between 使用 daterange 选择器
- [ ] Number between 显示两个数字输入框
- [ ] 后端正确生成 BETWEEN SQL 语句
- [ ] 查询结果正确
