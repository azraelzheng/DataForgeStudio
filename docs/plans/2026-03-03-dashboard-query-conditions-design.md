# 数据流优化：查询条件复用 设计文档

> **目标**: 在大屏组件中复用报表的查询条件 UI，允许为每个组件设置查询条件值

## 1. 概述

### 1.1 背景

当前大屏组件绑定报表后，无法设置查询条件，只能获取报表的默认数据。需要复用报表设计器中的查询条件 UI，让用户能够为大屏组件设置特定的查询条件。

### 1.2 成功标准

1. 绑定报表后，自动显示该报表的查询条件
2. 用户可以设置查询条件的值
3. 展示大屏时，使用存储的条件值获取数据

## 2. 现有实现分析

### 2.1 报表查询条件结构

报表查询条件存储在 `Report.QueryConditions` 字段（JSON 格式）：

```json
[
  {
    "fieldName": "order_date",
    "displayName": "订单日期",
    "dataType": "DateTime",
    "operator": "between",
    "defaultValue": null
  },
  {
    "fieldName": "department",
    "displayName": "车间",
    "dataType": "String",
    "operator": "eq",
    "defaultValue": "A1"
  }
]
```

### 2.2 现有 API

| API | 用途 |
|-----|------|
| `GET /api/reports/{id}` | 获取报表详情（包含 queryConditions） |
| `POST /api/reports/{id}/execute` | 执行报表，参数: `{ parameters: {...} }` |
| `GET /api/dashboards/{id}/data` | 获取大屏数据 |

### 2.3 前端组件

- `ReportQuery.vue` - 已实现查询条件 UI 渲染
- 支持的操作符: eq, between, like, null, notnull, true, false 等
- 支持的数据类型: String, Number, DateTime, Boolean

## 3. 设计方案

### 3.1 数据结构变更

**DashboardWidget.DataConfig 新增字段：**

```json
{
  "reportId": 123,
  "queryConditionValues": {
    "order_date_between": ["2024-01-01", "2024-01-31"],
    "department_eq": "A1",
    "status_eq": "进行中"
  }
}
```

**字段说明：**
- `queryConditionValues`: 查询条件值的键值对
- 键名格式: `{fieldName}_{operator}` (与 ReportQuery.vue 中的 getFieldKey 一致)

### 3.2 后端改动

**DashboardService.GetWidgetDataAsync 修改：**

```csharp
private async Task<WidgetDataResult> GetWidgetDataAsync(DashboardWidget widget)
{
    try
    {
        // 解析查询条件值
        var dataConfig = JsonSerializer.Deserialize<WidgetDataConfig>(widget.DataConfig ?? "{}");

        var executeRequest = new ExecuteReportRequest
        {
            Parameters = dataConfig?.QueryConditionValues ?? new Dictionary<string, object>()
        };

        var reportResult = await _reportService.ExecuteReportAsync(widget.ReportId, executeRequest);

        // ... 其余逻辑不变
    }
    catch (Exception ex)
    {
        // ... 错误处理
    }
}

// 辅助类
private class WidgetDataConfig
{
    public int? ReportId { get; set; }
    public Dictionary<string, object>? QueryConditionValues { get; set; }
}
```

### 3.3 前端改动

**DashboardDesigner.vue 修改：**

1. 选择报表后，获取报表的查询条件
2. 渲染查询条件输入控件（复用 ReportQuery.vue 的逻辑）
3. 保存时，将条件值存入组件配置

**示例代码结构：**

```vue
<template>
  <!-- 组件配置面板 -->
  <el-form-item label="绑定报表">
    <el-select v-model="selectedWidget.reportId" @change="handleReportChange">
      <el-option v-for="r in reportList" :key="r.reportId" :label="r.reportName" :value="r.reportId" />
    </el-select>
  </el-form-item>

  <!-- 查询条件 (绑定报表后显示) -->
  <template v-if="widgetQueryConditions.length > 0">
    <el-divider>查询条件</el-divider>
    <div v-for="qc in widgetQueryConditions" :key="qc.fieldName + qc.operator">
      <el-form-item :label="qc.displayName">
        <!-- 根据 dataType 和 operator 渲染不同输入控件 -->
        <QueryConditionInput
          v-model="selectedWidget.queryConditionValues[getFieldKey(qc)]"
          :condition="qc"
        />
      </el-form-item>
    </div>
  </template>
</template>
```

### 3.4 数据流

```
设计器:
1. 选择报表 → 获取报表详情 → 提取 queryConditions
2. 渲染条件输入控件 → 用户填写值 → 存入 widget.config.queryConditionValues
3. 保存 → 后端存储 DataConfig JSON

展示:
1. 加载大屏 → 读取组件配置
2. GetWidgetDataAsync → 解析 DataConfig.queryConditionValues
3. 调用 ExecuteReportAsync(reportId, conditionValues)
4. 返回数据 → 前端渲染
```

## 4. 实现范围

### 4.1 本次实现

| 任务 | 内容 |
|------|------|
| 后端 | DashboardService.GetWidgetDataAsync 传递查询条件 |
| 前端 | DashboardDesigner 显示查询条件输入控件 |
| 前端 | 创建可复用的 QueryConditionInput 组件 |

### 4.2 不实现

- 服务端缓存（后续迭代）
- 大屏全局参数（后续迭代）
- 参数联动（后续迭代）

## 5. 测试要点

1. **绑定报表后显示条件** - 确保查询条件正确渲染
2. **保存条件值** - 确保条件值正确存储到组件配置
3. **展示时使用条件** - 确保获取数据时传递了条件值
4. **各种条件类型** - 测试 String/Number/DateTime/Boolean 类型
5. **各种操作符** - 测试 eq/between/like/null 等操作符

---

**设计批准**: 2026-03-03
**设计者**: Claude Code
