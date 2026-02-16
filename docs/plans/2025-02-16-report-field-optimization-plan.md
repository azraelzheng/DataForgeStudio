# 报表设计器字段加载优化 实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 优化报表设计器的字段自动识别功能，提升加载速度并修复日期类型识别问题。

**Architecture:** 新增后端 GetQuerySchema API，只返回字段元数据（名称、类型）而不返回数据行。后端使用 SQL Server 的 SET FMTONLY ON 或其他数据库的 WHERE 1=0 方式获取字段结构，并直接返回映射后的系统类型。

**Tech Stack:** ASP.NET Core 8.0, Vue 3, Element Plus, SQL Server

---

## Task 1: 创建 FieldSchemaDto 类

**Files:**
- Create: `backend/src/DataForgeStudio.Shared/DTO/FieldSchemaDto.cs`

**Step 1: 创建 FieldSchemaDto 类**

```csharp
namespace DataForgeStudio.Shared.DTO;

/// <summary>
/// 字段结构信息 DTO
/// </summary>
public class FieldSchemaDto
{
    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// SQL 数据类型（如 datetime, nvarchar, int）
    /// </summary>
    public string SqlDataType { get; set; } = string.Empty;

    /// <summary>
    /// 系统数据类型（String, Number, DateTime, Boolean）
    /// </summary>
    public string SystemDataType { get; set; } = string.Empty;
}
```

**Step 2: 验证文件创建**

检查文件是否正确创建在 `backend/src/DataForgeStudio.Shared/DTO/` 目录下。

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Shared/DTO/FieldSchemaDto.cs
git commit -m "feat: add FieldSchemaDto for query schema metadata"
```

---

## Task 2: 扩展 IDatabaseService 接口

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Interfaces/IDatabaseService.cs`

**Step 1: 添加 GetQuerySchemaAsync 方法签名**

在 `IDatabaseService.cs` 接口中添加新方法：

```csharp
/// <summary>
/// 获取查询的字段结构信息（不返回数据行）
/// </summary>
/// <param name="dataSource">数据源</param>
/// <param name="sql">SQL 查询语句</param>
/// <param name="parameters">查询参数</param>
/// <returns>字段结构列表</returns>
Task<ApiResponse<List<FieldSchemaDto>>> GetQuerySchemaAsync(
    DataSource dataSource,
    string sql,
    Dictionary<string, object>? parameters);
```

确保在文件顶部添加 using 语句：
```csharp
using DataForgeStudio.Shared.DTO;
```

**Step 2: 验证编译**

Run: `dotnet build backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj`
Expected: Build succeeded (会有未实现的错误，稍后实现)

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Interfaces/IDatabaseService.cs
git commit -m "feat: add GetQuerySchemaAsync method to IDatabaseService interface"
```

---

## Task 3: 在 DatabaseService 实现 GetQuerySchemaAsync

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/DatabaseService.cs`

**Step 1: 实现 GetQuerySchemaAsync 方法**

在 `DatabaseService.cs` 中添加以下方法：

```csharp
/// <summary>
/// 获取查询的字段结构信息（不返回数据行）
/// </summary>
public async Task<ApiResponse<List<FieldSchemaDto>>> GetQuerySchemaAsync(
    DataSource dataSource,
    string sql,
    Dictionary<string, object>? parameters)
{
    try
    {
        _logger.LogInformation($"获取查询结构: {dataSource.DbType}");

        using var connection = CreateConnection(dataSource);
        await connection.OpenAsync();

        // 根据数据库类型调整 SQL 以只获取元数据
        var schemaSql = dataSource.DbType switch
        {
            "SqlServer" => $"SET FMTONLY ON; {sql}; SET FMTONLY OFF;",
            "MySql" => sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase)
                ? sql.Replace(" WHERE ", " WHERE 1=0 AND ", StringComparison.OrdinalIgnoreCase)
                : sql + " WHERE 1=0",
            "PostgreSQL" => sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase)
                ? sql.Replace(" WHERE ", " WHERE 1=0 AND ", StringComparison.OrdinalIgnoreCase)
                : sql + " WHERE 1=0",
            "Oracle" => sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase)
                ? sql.Replace(" WHERE ", " WHERE 1=0 AND ", StringComparison.OrdinalIgnoreCase)
                : sql + " WHERE 1=0",
            "SQLite" => sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase)
                ? sql.Replace(" WHERE ", " WHERE 1=0 AND ", StringComparison.OrdinalIgnoreCase)
                : sql + " WHERE 1=0",
            _ => sql + " WHERE 1=0"
        };

        using var command = CreateCommand(schemaSql, connection, parameters);
        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly);

        var fields = new List<FieldSchemaDto>();

        // 获取列结构
        var schemaTable = reader.GetColumnSchema();
        foreach (var column in schemaTable)
        {
            var sqlDataType = column.DataTypeName ?? "unknown";
            var systemDataType = MapSystemDataType(sqlDataType);

            fields.Add(new FieldSchemaDto
            {
                FieldName = column.ColumnName,
                SqlDataType = sqlDataType,
                SystemDataType = systemDataType
            });
        }

        _logger.LogInformation($"获取查询结构成功: 共 {fields.Count} 个字段");
        return ApiResponse<List<FieldSchemaDto>>.Ok(fields);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"获取查询结构失败: {ex.Message}");
        return ApiResponse<List<FieldSchemaDto>>.Fail($"获取查询结构失败: {ex.Message}", "SCHEMA_ERROR");
    }
}
```

确保在文件顶部添加 using 语句：
```csharp
using System.Data.Common;
using DataForgeStudio.Shared.DTO;
```

**Step 2: 验证编译**

Run: `dotnet build backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/DatabaseService.cs
git commit -m "feat: implement GetQuerySchemaAsync in DatabaseService"
```

---

## Task 4: 扩展 IReportService 接口

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Interfaces/IReportService.cs`

**Step 1: 添加 GetQuerySchemaAsync 方法签名**

在 `IReportService.cs` 接口中添加新方法：

```csharp
/// <summary>
/// 获取查询的字段结构信息
/// </summary>
/// <param name="dataSourceId">数据源ID</param>
/// <param name="sql">SQL 查询语句</param>
/// <param name="parameters">查询参数</param>
/// <returns>字段结构列表</returns>
Task<ApiResponse<List<FieldSchemaDto>>> GetQuerySchemaAsync(
    int dataSourceId,
    string sql,
    Dictionary<string, object>? parameters);
```

确保在文件顶部添加 using 语句：
```csharp
using DataForgeStudio.Shared.DTO;
```

**Step 2: 验证编译**

Run: `dotnet build backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj`
Expected: Build succeeded (会有未实现的错误，稍后实现)

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Interfaces/IReportService.cs
git commit -m "feat: add GetQuerySchemaAsync method to IReportService interface"
```

---

## Task 5: 在 ReportService 实现 GetQuerySchemaAsync

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs`

**Step 1: 实现 GetQuerySchemaAsync 方法**

在 `ReportService.cs` 中添加以下方法：

```csharp
/// <summary>
/// 获取查询的字段结构信息（用于报表设计器自动识别字段）
/// </summary>
public async Task<ApiResponse<List<FieldSchemaDto>>> GetQuerySchemaAsync(
    int dataSourceId,
    string sql,
    Dictionary<string, object>? parameters)
{
    // SQL 验证
    var validationResult = _sqlValidationService.ValidateQuery(sql);
    if (!validationResult.IsValid)
    {
        _logger.LogWarning("获取查询结构时 SQL 验证失败: {Message}, SQL: {Sql}",
            validationResult.ErrorMessage, sql);
        return ApiResponse<List<FieldSchemaDto>>.Fail(validationResult.ErrorMessage, "SQL_VALIDATION_FAILED");
    }

    var dataSource = await _context.DataSources.FindAsync(dataSourceId);
    if (dataSource == null)
    {
        return ApiResponse<List<FieldSchemaDto>>.Fail("数据源不存在", "NOT_FOUND");
    }

    if (!dataSource.IsActive)
    {
        return ApiResponse<List<FieldSchemaDto>>.Fail("数据源已被禁用", "DATASOURCE_DISABLED");
    }

    try
    {
        var result = await _databaseService.GetQuerySchemaAsync(dataSource, sql, parameters);
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"获取查询结构失败: DataSourceId={dataSourceId}, Error={ex.Message}");
        return ApiResponse<List<FieldSchemaDto>>.Fail($"获取查询结构失败: {ex.Message}", "SCHEMA_ERROR");
    }
}
```

**Step 2: 验证编译**

Run: `dotnet build backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/ReportService.cs
git commit -m "feat: implement GetQuerySchemaAsync in ReportService"
```

---

## Task 6: 在 ReportsController 添加 QuerySchema 端点

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Controllers/ReportsController.cs`

**Step 1: 添加 QuerySchema 端点**

在 `ReportsController.cs` 中添加以下方法：

```csharp
/// <summary>
/// 获取查询的字段结构信息（用于自动识别字段）
/// </summary>
/// <param name="request">查询请求</param>
/// <returns>字段结构列表</returns>
[HttpPost("query-schema")]
[ProducesResponseType(typeof(ApiResponse<List<FieldSchemaDto>>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetQuerySchema([FromBody] TestQueryRequest request)
{
    var result = await _reportService.GetQuerySchemaAsync(
        request.DataSourceId,
        request.Sql,
        request.Parameters);

    if (!result.Success)
    {
        return BadRequest(result);
    }

    return Ok(result);
}
```

确保在文件顶部添加 using 语句（如果尚未添加）：
```csharp
using DataForgeStudio.Shared.DTO;
```

**Step 2: 验证编译**

Run: `dotnet build backend/src/DataForgeStudio.Api/DataForgeStudio.Api.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Controllers/ReportsController.cs
git commit -m "feat: add query-schema endpoint to ReportsController"
```

---

## Task 7: 后端集成测试

**Files:**
- Test: `backend/src/DataForgeStudio.Api/`

**Step 1: 启动后端服务**

Run: `dotnet run --project backend/src/DataForgeStudio.Api`
Expected: 服务启动成功，监听 https://localhost:5000

**Step 2: 测试新 API**

使用 Swagger 或 curl 测试：
```
POST https://localhost:5000/api/reports/query-schema
Content-Type: application/json

{
  "dataSourceId": 1,
  "sql": "SELECT 1 as id, GETDATE() as created_date, 'test' as name"
}
```

Expected: 返回包含正确类型信息的字段列表
```json
{
  "success": true,
  "data": [
    { "fieldName": "id", "sqlDataType": "int", "systemDataType": "Number" },
    { "fieldName": "created_date", "sqlDataType": "datetime", "systemDataType": "DateTime" },
    { "fieldName": "name", "sqlDataType": "varchar", "systemDataType": "String" }
  ]
}
```

**Step 3: 验证日期类型正确映射**

确认 `systemDataType` 为 `DateTime` 而非 `String`。

---

## Task 8: 前端添加 getQuerySchema API 方法

**Files:**
- Modify: `frontend/src/api/request.js`

**Step 1: 添加 getQuerySchema 方法**

在 `request.js` 的 `reportApi` 对象中添加：

```javascript
// 获取查询字段结构（用于自动识别字段）
getQuerySchema: (data) => request.post('/api/reports/query-schema', data),
```

**Step 2: 验证语法**

检查文件语法是否正确。

**Step 3: Commit**

```bash
git add frontend/src/api/request.js
git commit -m "feat: add getQuerySchema API method to frontend"
```

---

## Task 9: 修改 ReportDesigner.vue 的 handleAutoDetectFields 方法

**Files:**
- Modify: `frontend/src/views/report/ReportDesigner.vue`

**Step 1: 替换 handleAutoDetectFields 方法**

将原有的 `handleAutoDetectFields` 方法替换为：

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
    // 准备参数（使用默认值）
    const parameters = {}
    form.parameters.forEach(p => {
      if (p.defaultValue) {
        parameters[p.name] = p.defaultValue
      }
    })

    // 调用新的 getQuerySchema API 只获取字段结构
    const res = await reportApi.getQuerySchema({
      dataSourceId: form.dataSourceId,
      sql: form.sqlQuery,
      parameters: Object.keys(parameters).length > 0 ? parameters : null
    })

    if (res.success && res.data.length > 0) {
      // 直接使用后端返回的字段元数据
      const detectedFields = res.data.map(field => ({
        fieldName: field.fieldName,
        displayName: field.fieldName,
        dataType: field.systemDataType,  // 直接使用后端映射的类型
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

**Step 2: 验证语法**

检查文件语法是否正确，确保 `reportApi` 已导入。

**Step 3: Commit**

```bash
git add frontend/src/views/report/ReportDesigner.vue
git commit -m "feat: optimize handleAutoDetectFields to use getQuerySchema API"
```

---

## Task 10: 前端集成测试

**Files:**
- Test: `frontend/src/`

**Step 1: 启动前端开发服务器**

Run: `cd frontend && npm run dev`
Expected: 服务启动成功

**Step 2: 测试自动识别功能**

1. 打开报表设计器
2. 选择数据源
3. 输入包含日期字段的 SQL 查询
4. 点击"自动识别"按钮
5. 验证字段配置中的日期字段显示为"日期"类型

**Step 3: 验证性能提升**

比较优化前后的加载速度，确认自动识别响应更快。

---

## Task 11: 最终验证和清理

**Step 1: 运行后端测试**

Run: `dotnet test backend/tests/DataForgeStudio.Tests`
Expected: All tests passed

**Step 2: 验证前后端集成**

完整测试报表设计器的自动识别功能，确保：
- 字段名正确
- 数据类型正确（特别是日期类型）
- 响应速度明显提升

**Step 3: 最终 Commit**

```bash
git add -A
git commit -m "feat: complete report field optimization - faster loading and accurate type detection"
```

---

## 完成标准

- [ ] 后端 `GetQuerySchema` API 正常工作
- [ ] 前端调用新 API 获取字段结构
- [ ] 日期类型正确识别为 `DateTime`
- [ ] 自动识别响应速度明显提升
- [ ] 所有测试通过
