# 报表管理系统重新设计

> **设计日期**: 2026-02-06
> **设计目标**: 重构报表管理系统，分离报表查询和报表设计功能，增强字段类型自动匹配和查询条件配置

---

## 1. 设计概述

### 1.1 核心问题

当前报表管理系统存在以下问题：
- 报表查询和设计功能混在一起，职责不清
- 普通用户也能看到编辑/删除按钮，容易误操作
- 字段类型需要手动设置，没有自动映射
- 查询条件配置不灵活，无法设计复杂的查询界面

### 1.2 设计目标

1. **职责分离**: 报表查询只负责执行，报表设计负责管理
2. **权限控制**: 细粒度权限，不同角色不同功能
3. **自动化**: 字段类型自动映射，减少手动配置
4. **灵活查询**: 可配置的查询条件，支持多种操作符

### 1.3 技术栈

- 前端: Vue 3 + Element Plus + ECharts
- 后端: ASP.NET Core 8.0 + EF Core
- 数据库: SQL Server 2005+

---

## 2. 整体架构设计

### 2.1 页面结构

```
/report          → ReportQuery.vue       (报表查询页)
/report/design   → ReportDesignList.vue  (报表设计列表页)
/report/designer → ReportDesigner.vue    (报表设计器页)
```

### 2.2 菜单配置（一级菜单）

```
报表查询  → /report       (权限: report:query)
报表设计  → /report/design (权限: report:design)
```

### 2.3 核心设计原则

- **职责分离**: 查询页面只负责执行查询，设计页面负责报表生命周期管理
- **权限隔离**: 通过路由守卫控制不同角色的访问权限
- **数据流向**: 设计器保存报表 → 后端存储 → 查询页面读取配置 → 动态生成查询界面

---

## 3. 数据模型设计

### 3.1 Report 实体扩展

```csharp
// 图表配置
public bool EnableChart { get; set; } = false;
public string? ChartConfig { get; set; }  // JSON: {chartType, xField, yFields, title}

// 查询条件配置
public string? QueryConditions { get; set; }  // JSON: [{fieldName, displayName, dataType, operator, defaultValue}]

// 统计信息（部分已有）
public int ViewCount { get; set; } = 0;
public DateTime? LastViewTime { get; set; }

// 状态控制（已有）
public bool IsEnabled { get; set; } = true;
```

### 3.2 新增 DTO

```csharp
// 查询条件配置 DTO
public class QueryConditionDto
{
    public string FieldName { get; set; }
    public string DisplayName { get; set; }
    public string DataType { get; set; }  // String/Number/DateTime/Boolean
    public string Operator { get; set; }  // eq/ne/gt/le/null/like 等
    public string? DefaultValue { get; set; }
}

// 图表配置 DTO
public class ChartConfigDto
{
    public string ChartType { get; set; }  // bar/line/pie/doughnut
    public string XField { get; set; }
    public List<string> YFields { get; set; }
    public string Title { get; set; }
}

// 表结构查询响应扩展
public class TableColumnDto
{
    // ... 现有字段
    public string SystemDataType { get; set; }  // 新增：自动映射的系统类型
}
```

---

## 4. 系统字段类型体系

### 4.1 四种基础类型

- **String**: 字符串 - 文本输入框
- **Number**: 数值 - 数字输入框
- **DateTime**: 日期 - 日期选择器
- **Boolean**: 布尔 - 下拉选择（是/否）

### 4.2 数据库类型映射

| 数据库类型 | 系统类型 |
|-----------|---------|
| varchar, nvarchar, char, text | String |
| int, decimal, numeric, float, money | Number |
| datetime, date, smalldatetime | DateTime |
| bit | Boolean |

---

## 5. 查询条件配置

### 5.1 操作符映射（按字段类型）

```javascript
const operatorMap = {
  String: ['=', '<>', '包含', '为空', '不为空'],
  Number: ['=', '<>', '>', '<', '>=', '<=', '为空', '不为空'],
  DateTime: ['=', '>', '<', '>=', '<=', '为空', '不为空'],
  Boolean: ['=']
}
```

### 5.2 逻辑关系

- 固定 **AND** 关系
- 所有查询条件使用 AND 连接
- 满足 80% 的实际查询需求

### 5.3 默认值支持

- 字符串: 留空
- 数值: 留空或指定默认值
- 日期: 快捷选项（今天、本周、本月、最近7天、最近30天、本季度、本年度）

### 5.4 QueryConditions 组件

**设计模式界面：**
```
查询条件配置            [自动生成] [添加条件]
─────────────────────────────────────────
□ 客户名称  [包含 v]  默认值: [_______] [删除]
□ 订单日期  [≥ v]    默认值: [本月 ▼] [删除]
□ 金额      [> v]    默认值: [_______] [删除]
□ 状态      [= v]    默认值: [有效 ▼] [删除]
```

---

## 6. 页面设计

### 6.1 报表设计列表 (ReportDesignList.vue)

```
┌──────────────────────────────────────────────────────────────┐
│ 报表设计管理                          [+ 新建报表]  [导出配置] │
├──────────────────────────────────────────────────────────────┤
│ 搜索: [报表名称____] [分类▼] [状态▼] [查询] [重置]             │
├──────────────────────────────────────────────────────────────┤
│ 报表名称    │ 分类 │ 数据源 │ 状态 │ 查看次数 │ 最后查看 │ 操作 │
├──────────────────────────────────────────────────────────────┤
│ 销售报表    │ 销售 │ SQLServer│ 启用│  156    │ 2小时前  │...│
│ 库存预警    │ 库存 │ SQLServer│ 停用│   89    │ 昨天    │...│
│ 财务汇总    │ 财务 │ SQLServer│ 启用│  234    │ 今天    │...│
└──────────────────────────────────────────────────────────────┘
```

**操作按钮：**
- 编辑 - 跳转到报表设计器
- 预览 - 弹窗查看报表效果（只读模式）
- 复制 - 创建报表副本
- 停用/启用 - 切换状态
- 删除 - 删除报表（二次确认）

### 6.2 报表查询 (ReportQuery.vue) - 左右分栏布局

```
┌───────────────────────────────────────────────────────────────────┐
│ 报表查询                                                            │
├──────────────────┬────────────────────────────────────────────────┤
│ 报表列表 (30%)    │ 查询区域 (70%)                                 │
│                  │                                                │
│ 🔍 [搜索_______] │ 请选择左侧报表进行查询                          │
│                  │                                                │
│ 📄 销售报表       │ （或选中报表后显示：）                          │
│   销售 │ 156次   │ ┌──────────────────────────────────────┐      │
│   [点击查看→]   │ │ 销售报表                              │      │
│                  │ ├──────────────────────────────────────┤      │
│ 📄 库存预警       │ │ 查询条件              [重置]           │      │
│   库存 │ 89次    │ │ 客户名称: [___________]               │      │
│   [点击查看→]   │ │ 订单日期: [本月 ▼] ~ [今天 ▼]         │      │
│                  │ │ 金额: [> ___]  状态: [有效 ▼]         │      │
│ 📄 财务汇总       │ │ [查询] [导出Excel] [导出PDF]          │      │
│   财务 │ 234次   │ ├──────────────────────────────────────┤      │
│   [点击查看→]   │ │ 查询结果 (共 1,234 条)                  │      │
│                  │ │ [表格视图] [图表视图]                 │      │
│                  │ │ ...表格/图表内容...                   │      │
│                  │ └──────────────────────────────────────┘      │
└──────────────────┴────────────────────────────────────────────────┘
```

**核心变更：**
- 移除"新建报表"按钮
- 移除"编辑"、"删除"操作按钮
- 保留"查询"和"导出"功能
- 查询条件根据报表配置动态生成

### 6.3 报表设计器 (ReportDesigner.vue) - 功能增强

**新增功能：**
1. 测试查询时自动映射字段类型
2. "重新识别字段"按钮
3. 查询条件配置卡片
4. 字段配置显示系统类型标签

---

## 7. 权限控制

### 7.1 权限代码

| 权限代码 | 权限名称 | 描述 |
|---------|---------|------|
| report:query | 访问报表查询 | 访问报表查询页面 |
| report:execute | 执行报表查询 | 执行报表查询并查看结果 |
| report:design | 访问报表设计 | 访问报表设计管理页面 |
| report:create | 创建报表 | 创建新报表 |
| report:edit | 编辑报表 | 编辑报表配置 |
| report:delete | 删除报表 | 删除报表 |
| report:toggle | 停用启用报表 | 停用或启用报表 |

### 7.2 角色权限分配

```
报表查询组 (ROLE_REPORT_VIEWER):
  - report:query
  - report:execute

报表设计组 (ROLE_REPORT_DESIGNER):
  - report:query
  - report:execute
  - report:design
  - report:create
  - report:edit
  - report:toggle

管理员 (ROLE_ADMIN):
  - 所有报表权限
```

---

## 8. API 接口设计

### 8.1 报表查询相关

```
GET /api/reports/enabled
  获取启用的报表列表（查询页面使用）

POST /api/reports/{id}/execute
  执行报表查询
  Body: { conditions: { fieldName_eq: "value", ... } }

GET /api/reports/{id}/export?format=excel|pdf
  导出报表
```

### 8.2 报表设计相关

```
GET /api/reports/all?includeDisabled=true
  获取所有报表（包括停用的）

POST /api/reports
  创建报表

PUT /api/reports/{id}
  更新报表

DELETE /api/reports/{id}
  删除报表

PATCH /api/reports/{id}/toggle
  切换启用状态

POST /api/reports/{id}/copy
  复制报表

GET /api/reports/{id}/statistics
  获取报表使用统计

GET /api/reports/{id}/export-config
  导出报表配置（JSON）
```

### 8.3 数据源相关扩展

```
GET /api/datasources/{id}/tables/{tableName}/structure
  获取表结构（带系统类型映射）
  Response: ApiResponse<List<TableColumnDto>> (包含 SystemDataType)
```

---

## 9. 字段类型自动映射

### 9.1 映射逻辑

```csharp
private string MapSystemDataType(string sqlDataType)
{
    var type = sqlDataType?.ToLower() ?? "";

    // 字符串类型
    if (type.Contains("char") || type.Contains("text") || type.Contains("nvarchar"))
        return "String";

    // 数值类型
    if (type.Contains("int") || type.Contains("decimal") ||
        type.Contains("numeric") || type.Contains("float") ||
        type.Contains("real") || type.Contains("money"))
        return "Number";

    // 日期类型
    if (type.Contains("date") || type.Contains("time"))
        return "DateTime";

    // 布尔类型
    if (type.Contains("bit"))
        return "Boolean";

    return "String"; // 默认字符串
}
```

### 9.2 触发时机

1. **测试查询时** - 自动执行映射，填充字段配置列表
2. **点击"重新识别字段"按钮** - 覆盖现有字段配置

### 9.3 自动填充的字段属性

```javascript
{
  fieldName: "CustomerName",    // 从数据库获取
  displayName: "CustomerName",  // 默认使用字段名，用户可修改
  dataType: "String",           // 自动映射
  width: 120,                   // 默认值
  align: "left",                // 根据类型推断（Number 右对齐）
  isVisible: true,
  isSortable: true
}
```

---

## 10. 实施计划

### 阶段一：基础架构重构（约4个任务）
1. 路由和菜单结构调整
2. ReportDesignList.vue 创建
3. ReportQuery.vue 重构
4. 数据库迁移（添加图表和查询条件字段）

### 阶段二：核心功能实现（约5个任务）
5. QueryConditions.vue 组件创建
6. DatabaseService 类型映射实现
7. ReportDesigner.vue 集成新功能
8. ReportService 查询条件持久化
9. ReportQuery.vue 动态查询条件集成

### 阶段三：增强功能（约3个任务）
10. 报表统计功能（查看次数、最后查看时间）
11. 报表复制功能
12. 报表配置导出功能

### 阶段四：权限和测试（约2个任务）
13. 权限定义更新和角色分配
14. 端到端测试和验证

**总计：14 个主要任务**

---

## 11. 设计决策总结

| 决策点 | 选择 |
|--------|------|
| 菜单结构 | 两个一级菜单（报表查询、报表设计） |
| 系统字段类型 | 4种基础类型（字符串、数值、日期、布尔） |
| 查询条件配置 | 设计时预定义 |
| 操作符支持 | 按字段类型分配固定操作符 |
| 条件逻辑关系 | 固定 AND 关系 |
| 查询条件默认值 | 支持默认值 |
| 报表设计列表操作 | 增强操作（复制、预览、统计、导出） |
| 图表配置 | 完善支持，添加数据库字段 |
| 报表停用行为 | 仅在查询列表中隐藏 |
| 字段类型映射 | 混合方案（自动映射 + 重新识别） |
| 权限控制 | 基于功能代码的细粒度权限 |
| 报表分类 | 保持硬编码 |
| 查询页面布局 | 左右分栏（列表+详情） |
