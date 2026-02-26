# 报表设计器字段加载优化设计

## 日期
2025-02-16

## 问题背景

报表设计器中存在两个问题：
1. **字段加载缓慢**: 当字段配置中的字段比较多时，loading 字段很慢
2. **类型匹配不准确**: 日期类型的字段在数据库中是日期类型，但匹配后在字段配置中显示为字符串

## 问题分析

### 问题1: 字段加载缓慢

**当前流程**:
1. 前端调用 `handleAutoDetectFields` 方法
2. 调用 `testQuery` API 执行完整 SQL 查询
3. 后端返回最多 100 行数据
4. 前端从第一行数据推断字段类型

**性能瓶颈**:
- 需要执行完整查询并返回大量数据
- 数据传输和 JSON 序列化/反序列化开销大
- 实际上只需要字段元数据，不需要数据行

### 问题2: 类型匹配不准确

**根本原因**:
1. 后端 `ExecuteQueryAsync` 方法使用 `reader.GetValue(i)` 获取值，日期类型返回 .NET DateTime 对象
2. JSON 序列化时，DateTime 被序列化为 ISO 字符串格式（如 `"2024-01-01T00:00:00"`）
3. 前端收到的是字符串，`value instanceof Date` 返回 false
4. 导致日期字段被识别为 String 类型

**前端类型推断代码** (ReportDesigner.vue:585-594):
```javascript
let dataType = 'String'
if (typeof value === 'number') {
  dataType = Number.isInteger(value) ? 'Number' : 'Number'
} else if (value instanceof Date) {  // 永远不会为 true
  dataType = 'DateTime'
} else if (typeof value === 'boolean') {
  dataType = 'Boolean'
}
```

## 解决方案

### 核心思路

新增 `GetQuerySchema` API，让前端只获取字段结构信息（名称、类型），而非完整数据行。后端直接返回映射后的系统类型。

### 设计详情

#### 1. 后端新增接口

**IDatabaseService 新增方法**:
```csharp
/// <summary>
/// 获取查询的字段结构信息（不返回数据行）
/// </summary>
Task<ApiResponse<List<FieldSchemaDto>>> GetQuerySchemaAsync(
    DataSource dataSource,
    string sql,
    Dictionary<string, object>? parameters);
```

**新增 DTO**:
```csharp
public class FieldSchemaDto
{
    public string FieldName { get; set; }       // 字段名
    public string SqlDataType { get; set; }     // SQL 数据类型
    public string SystemDataType { get; set; }  // 系统类型 (String/Number/DateTime/Boolean)
}
```

**实现策略**:
- SQL Server: 使用 `SET FMTONLY ON; <query>; SET FMTONLY OFF;` 只获取元数据
- 其他数据库: 使用 `WHERE 1=0` 条件执行零行查询
- 利用现有的 `MapSystemDataType` 方法映射类型

**ReportService 新增方法**:
```csharp
Task<ApiResponse<List<FieldSchemaDto>>> GetQuerySchemaAsync(
    int dataSourceId,
    string sql,
    Dictionary<string, object>? parameters);
```

**Controller 新增端点**:
```
POST /api/reports/query-schema
Request: { dataSourceId, sql, parameters }
Response: { success, data: [{ fieldName, sqlDataType, systemDataType }] }
```

#### 2. 前端修改

**request.js 新增 API 方法**:
```javascript
getQuerySchema: (data) => request.post('/api/reports/query-schema', data)
```

**ReportDesigner.vue 修改 handleAutoDetectFields**:
```javascript
const handleAutoDetectFields = async () => {
  if (!form.dataSourceId) {
    ElMessage.warning('请先选择数据源')
    return
  }
  if (!form.sqlQuery) {
    ElMessage.warning('请先输入SQL查询语句')
    return
  }

  try {
    const parameters = {}
    form.parameters.forEach(p => {
      if (p.defaultValue) {
        parameters[p.name] = p.defaultValue
      }
    })

    // 调用新的 getQuerySchema API
    const res = await reportApi.getQuerySchema({
      dataSourceId: form.dataSourceId,
      sql: form.sqlQuery,
      parameters: Object.keys(parameters).length > 0 ? parameters : null
    })

    if (res.success && res.data.length > 0) {
      const detectedFields = res.data.map(field => ({
        fieldName: field.fieldName,
        displayName: field.fieldName,
        dataType: field.systemDataType,  // 直接使用后端返回的类型
        width: 120,
        align: field.systemDataType === 'Number' ? 'right' : 'left',
        isVisible: true,
        isSortable: true
      }))

      form.columns = detectedFields
      ElMessage.success(`自动识别成功，检测到 ${detectedFields.length} 个字段`)
    } else {
      ElMessage.warning('查询结果为空，无法自动识别字段')
    }
  } catch (error) {
    console.error('自动识别字段失败:', error)
    ElMessage.error('自动识别字段失败: ' + (error.message || '未知错误'))
  }
}
```

#### 3. 数据流程

```
[前端] 点击"自动识别"
  -> [API] POST /api/reports/query-schema
    -> [ReportService] GetQuerySchemaAsync
      -> [DatabaseService] GetQuerySchemaAsync
        -> SQL Server: SET FMTONLY ON; SELECT ...
        -> 返回 DataReader 元数据（不执行完整查询）
      -> 映射类型并返回 [{fieldName, sqlDataType, systemDataType}]
    -> [前端] 直接使用返回的类型，无需推断
```

## 实现计划

### 第一步: 后端实现
1. 创建 `FieldSchemaDto` 类
2. 在 `IDatabaseService` 接口添加 `GetQuerySchemaAsync` 方法
3. 在 `DatabaseService` 实现方法
4. 在 `IReportService` 接口添加 `GetQuerySchemaAsync` 方法
5. 在 `ReportService` 实现方法
6. 在 `ReportsController` 添加 `QuerySchema` 端点

### 第二步: 前端实现
1. 在 `request.js` 添加 `getQuerySchema` API 方法
2. 修改 `ReportDesigner.vue` 的 `handleAutoDetectFields` 方法

### 第三步: 测试验证
1. 测试各种数据类型的正确识别
2. 测试带参数的查询
3. 验证性能提升

## 预期效果

| 指标 | 优化前 | 优化后 |
|------|--------|--------|
| 响应数据量 | 最多 100 行完整数据 | 仅字段元数据（约 1KB） |
| 日期类型识别 | 被识别为字符串 | 正确识别为 DateTime |
| 查询执行时间 | 完整查询 + 数据传输 | 仅元数据查询（毫秒级） |
| 网络传输 | 大 | 极小 |

## 兼容性说明

- 此修改为增强功能，不影响现有 API
- `testQuery` API 保持不变，仍可用于测试查询结果
- 用户可以继续手动修改自动识别的字段类型
