using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;
using System.Text.Json;
using System.Data;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 报表服务实现
/// </summary>
public class ReportService : IReportService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly IDataSourceService _dataSourceService;
    private readonly IDatabaseService _databaseService;
    private readonly ISqlValidationService _sqlValidationService;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        DataForgeStudioDbContext context,
        IDataSourceService dataSourceService,
        IDatabaseService databaseService,
        ISqlValidationService sqlValidationService,
        ILicenseService licenseService,
        ILogger<ReportService> logger)
    {
        _context = context;
        _dataSourceService = dataSourceService;
        _databaseService = databaseService;
        _sqlValidationService = sqlValidationService;
        _licenseService = licenseService;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResponse<ReportDto>>> GetReportsAsync(PagedRequest request, string? reportName = null, string? category = null, bool? isEnabled = null)
    {
        var query = _context.Reports.AsQueryable();

        if (!string.IsNullOrWhiteSpace(reportName))
        {
            query = query.Where(r => r.ReportName.Contains(reportName));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(r => r.ReportCategory == category);
        }

        // 添加启用状态过滤
        if (isEnabled.HasValue)
        {
            query = query.Where(r => r.IsEnabled == isEnabled.Value);
        }

        var totalCount = await query.CountAsync();

        var reports = await query
            .Include(r => r.DataSource)
            .OrderByDescending(r => r.CreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReportDto
            {
                ReportId = r.ReportId,
                ReportName = r.ReportName,
                ReportCategory = r.ReportCategory,
                DataSourceId = r.DataSourceId,
                DataSourceName = r.DataSource != null ? r.DataSource.DataSourceName : null,
                Description = r.Description,
                ViewCount = r.ViewCount,
                LastViewTime = r.LastViewTime,
                CreatedTime = r.CreatedTime,
                IsEnabled = r.IsEnabled
            })
            .ToListAsync();

        var pagedResponse = new PagedResponse<ReportDto>(reports, totalCount, request.PageIndex, request.PageSize);
        return ApiResponse<PagedResponse<ReportDto>>.Ok(pagedResponse);
    }

    public async Task<ApiResponse<ReportDetailDto>> GetReportByIdAsync(int reportId)
    {
        var report = await _context.Reports
            .Include(r => r.Fields)
            .Include(r => r.Parameters)
            .Where(r => r.ReportId == reportId)
            .FirstOrDefaultAsync();

        if (report == null)
        {
            return ApiResponse<ReportDetailDto>.Fail("报表不存在", "NOT_FOUND");
        }

        var reportDetail = new ReportDetailDto
        {
            ReportId = report.ReportId,
            ReportName = report.ReportName,
            ReportCategory = report.ReportCategory,
            DataSourceId = report.DataSourceId,
            Description = report.Description,
            ViewCount = report.ViewCount,
            LastViewTime = report.LastViewTime,
            CreatedTime = report.CreatedTime,
            SqlQuery = report.SqlStatement,
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
            Parameters = report.Parameters.OrderBy(p => p.SortOrder).Select(p => new ReportParameterDto
            {
                Name = p.ParameterName,
                Label = p.DisplayName,
                DataType = p.DataType,
                DefaultValue = p.DefaultValue
            }).ToList()
        };

        // 解析查询条件
        if (!string.IsNullOrEmpty(report.QueryConditions))
        {
            try
            {
                reportDetail.QueryConditions = JsonSerializer.Deserialize<List<QueryConditionDto>>(report.QueryConditions);
            }
            catch
            {
                reportDetail.QueryConditions = new List<QueryConditionDto>();
            }
        }
        else
        {
            reportDetail.QueryConditions = new List<QueryConditionDto>();
        }

        // 解析图表配置
        if (report.EnableChart && !string.IsNullOrEmpty(report.ChartConfig))
        {
            try
            {
                reportDetail.ChartConfig = JsonSerializer.Deserialize<ChartConfigDto>(report.ChartConfig);
            }
            catch
            {
                reportDetail.ChartConfig = null;
            }
        }

        return ApiResponse<ReportDetailDto>.Ok(reportDetail);
    }

    public async Task<ApiResponse<ReportDto>> CreateReportAsync(CreateReportRequest request, int createdBy)
    {
        // 检查许可证报表数量限制
        var limitCheck = await _licenseService.CheckReportLimitAsync();
        if (!limitCheck.Success)
        {
            return ApiResponse<ReportDto>.Fail(limitCheck.Message, limitCheck.ErrorCode);
        }

        // SQL 验证
        var validationResult = _sqlValidationService.ValidateQuery(request.SqlQuery);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("创建报表时 SQL 验证失败: {Message}, SQL: {Sql}",
                validationResult.ErrorMessage, request.SqlQuery);
            return ApiResponse<ReportDto>.Fail(validationResult.ErrorMessage, "SQL_VALIDATION_FAILED");
        }

        var code = $"RPT_{DateTime.UtcNow:yyyyMMddHHmmss}";

        var report = new Report
        {
            ReportName = request.ReportName,
            ReportCode = code,
            ReportCategory = request.ReportCategory,
            DataSourceId = request.DataSourceId,
            SqlStatement = request.SqlQuery,
            Description = request.Description,
            CreatedBy = createdBy,
            CreatedTime = DateTime.UtcNow
        };

        // 保存查询条件
        if (request.QueryConditions != null && request.QueryConditions.Count > 0)
        {
            report.QueryConditions = JsonSerializer.Serialize(request.QueryConditions);
        }
        else
        {
            report.QueryConditions = null;
        }

        // 保存图表配置
        if (request.EnableChart)
        {
            report.EnableChart = true;
            if (request.ChartConfig != null)
            {
                report.ChartConfig = JsonSerializer.Serialize(request.ChartConfig);
            }
        }
        else
        {
            report.EnableChart = false;
            report.ChartConfig = null;
        }

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        // 添加字段
        if (request.Columns != null && request.Columns.Count > 0)
        {
            var sortOrder = 0;
            foreach (var column in request.Columns)
            {
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
            }
        }

        // 添加参数
        if (request.Parameters != null && request.Parameters.Count > 0)
        {
            var sortOrder = 0;
            foreach (var parameter in request.Parameters)
            {
                _context.ReportParameters.Add(new ReportParameter
                {
                    ReportId = report.ReportId,
                    ParameterName = parameter.Name,
                    DisplayName = parameter.Label,
                    DataType = parameter.DataType,
                    InputType = "Text",
                    DefaultValue = parameter.DefaultValue,
                    SortOrder = sortOrder++
                });
            }
        }

        await _context.SaveChangesAsync();

        var reportDto = new ReportDto
        {
            ReportId = report.ReportId,
            ReportName = report.ReportName,
            ReportCategory = report.ReportCategory,
            DataSourceId = report.DataSourceId,
            Description = report.Description,
            CreatedTime = report.CreatedTime
        };

        return ApiResponse<ReportDto>.Ok(reportDto, "报表创建成功");
    }

    public async Task<ApiResponse> UpdateReportAsync(int reportId, CreateReportRequest request)
    {
        // SQL 验证
        var validationResult = _sqlValidationService.ValidateQuery(request.SqlQuery);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("更新报表时 SQL 验证失败: {Message}, SQL: {Sql}",
                validationResult.ErrorMessage, request.SqlQuery);
            return ApiResponse.Fail(validationResult.ErrorMessage, "SQL_VALIDATION_FAILED");
        }

        var report = await _context.Reports
            .Include(r => r.Fields)
            .Include(r => r.Parameters)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);

        if (report == null)
        {
            return ApiResponse.Fail("报表不存在", "NOT_FOUND");
        }

        report.ReportName = request.ReportName;
        report.ReportCategory = request.ReportCategory;
        report.DataSourceId = request.DataSourceId;
        report.SqlStatement = request.SqlQuery;
        report.Description = request.Description;
        report.UpdatedTime = DateTime.UtcNow;

        // 保存查询条件
        if (request.QueryConditions != null && request.QueryConditions.Count > 0)
        {
            report.QueryConditions = JsonSerializer.Serialize(request.QueryConditions);
        }
        else
        {
            report.QueryConditions = null;
        }

        // 保存图表配置
        if (request.EnableChart)
        {
            report.EnableChart = true;
            if (request.ChartConfig != null)
            {
                report.ChartConfig = JsonSerializer.Serialize(request.ChartConfig);
            }
        }
        else
        {
            report.EnableChart = false;
            report.ChartConfig = null;
        }

        // 删除旧字段和参数
        _context.ReportFields.RemoveRange(report.Fields);
        _context.ReportParameters.RemoveRange(report.Parameters);

        // 添加新字段
        if (request.Columns != null && request.Columns.Count > 0)
        {
            var sortOrder = 0;
            foreach (var column in request.Columns)
            {
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
            }
        }

        // 添加新参数
        if (request.Parameters != null && request.Parameters.Count > 0)
        {
            var sortOrder = 0;
            foreach (var parameter in request.Parameters)
            {
                _context.ReportParameters.Add(new ReportParameter
                {
                    ReportId = report.ReportId,
                    ParameterName = parameter.Name,
                    DisplayName = parameter.Label,
                    DataType = parameter.DataType,
                    InputType = "Text",
                    DefaultValue = parameter.DefaultValue,
                    SortOrder = sortOrder++
                });
            }
        }

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("报表更新成功");
    }

    public async Task<ApiResponse> DeleteReportAsync(int reportId)
    {
        var report = await _context.Reports.FindAsync(reportId);
        if (report == null)
        {
            return ApiResponse.Fail("报表不存在", "NOT_FOUND");
        }

        _context.Reports.Remove(report);
        await _context.SaveChangesAsync();
        return ApiResponse.Ok("报表删除成功");
    }

    public async Task<ApiResponse<List<Dictionary<string, object>>>> ExecuteReportAsync(int reportId, ExecuteReportRequest request)
    {
        var report = await _context.Reports
            .Include(r => r.DataSource)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);

        if (report == null)
        {
            return ApiResponse<List<Dictionary<string, object>>>.Fail("报表不存在", "NOT_FOUND");
        }

        if (!report.IsEnabled)
        {
            return ApiResponse<List<Dictionary<string, object>>>.Fail("报表已被禁用", "REPORT_DISABLED");
        }

        if (!report.DataSource.IsActive)
        {
            return ApiResponse<List<Dictionary<string, object>>>.Fail("数据源已被禁用", "DATASOURCE_DISABLED");
        }

        try
        {
            // 构建带查询条件的SQL
            var (modifiedSql, sqlParameters) = BuildQueryWithConditions(report.SqlStatement, request.Parameters);

            // 执行查询
            var result = await _databaseService.ExecuteQueryAsync(report.DataSource, modifiedSql, sqlParameters);

            if (!result.Success)
            {
                return ApiResponse<List<Dictionary<string, object>>>.Fail(result.Message, result.ErrorCode);
            }

            // 更新查看次数
            report.ViewCount++;
            report.LastViewTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"报表执行失败: ReportId={reportId}, Error={ex.Message}");
            return ApiResponse<List<Dictionary<string, object>>>.Fail($"报表执行失败: {ex.Message}", "EXECUTION_ERROR");
        }
    }

    /// <summary>
    /// 构建带查询条件的SQL语句
    /// </summary>
    private (string sql, Dictionary<string, object> parameters) BuildQueryWithConditions(
        string originalSql,
        Dictionary<string, object>? requestParams)
    {
        var parameters = new Dictionary<string, object>();
        var whereClauses = new List<string>();
        int paramIndex = 0;

        if (requestParams != null)
        {
            foreach (var param in requestParams)
            {
                // 解析字段名和操作符 (格式: fieldName_operator)
                var parts = param.Key.Split('_');
                if (parts.Length < 2)
                {
                    // 普通参数，直接添加
                    parameters[param.Key] = ConvertJsonElement(param.Value);
                    continue;
                }

                var fieldName = parts[0];
                var op = parts[1];
                var value = ConvertJsonElement(param.Value);

                // 跳过空值
                if (value == null || (value is string s && string.IsNullOrEmpty(s)))
                {
                    continue;
                }

                // between 操作符特殊处理（需要两个值）
                if (op.ToLower() == "between")
                {
                    var (startValue, endValue) = ParseBetweenValue(param.Value);
                    if (startValue != null && endValue != null)
                    {
                        var startParam = $"@p{paramIndex++}";
                        var endParam = $"@p{paramIndex++}";
                        whereClauses.Add($"{fieldName} BETWEEN {startParam} AND {endParam}");
                        parameters[startParam] = startValue;
                        parameters[endParam] = endValue;
                    }
                    continue;
                }

                var paramName = $"@p{paramIndex++}";
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
                    "between" => null, // between 已在上面特殊处理
                    _ => null
                };

                if (clause != null)
                {
                    whereClauses.Add(clause);
                    // 对于不需要值的操作符，不添加参数
                    if (!new[] { "null", "notnull", "true", "false" }.Contains(op.ToLower()))
                    {
                        parameters[paramName] = value;
                    }
                }
            }
        }

        // 如果有查询条件，构建新的SQL
        if (whereClauses.Count > 0)
        {
            var whereClause = string.Join(" AND ", whereClauses);

            // 检查原SQL是否已有WHERE子句
            var upperSql = originalSql.ToUpperInvariant();
            if (upperSql.Contains(" WHERE "))
            {
                // 已有WHERE，使用AND添加条件
                return ($"{originalSql} AND {whereClause}", parameters);
            }
            else
            {
                // 没有WHERE，添加WHERE子句
                return ($"{originalSql} WHERE {whereClause}", parameters);
            }
        }

        return (originalSql, parameters);
    }

    /// <summary>
    /// 测试 SQL 查询（用于报表设计器）
    /// </summary>
    public async Task<ApiResponse<List<Dictionary<string, object>>>> TestQueryAsync(
        int dataSourceId,
        string sql,
        Dictionary<string, object>? parameters)
    {
        // SQL 验证
        var validationResult = _sqlValidationService.ValidateQuery(sql);
        if (!validationResult.IsValid)
        {
            // 记录被阻止的查询
            _logger.LogWarning("SQL 查询被阻止: {Message}, SQL: {Sql}",
                validationResult.ErrorMessage, sql);

            return ApiResponse<List<Dictionary<string, object>>>.Fail(validationResult.ErrorMessage, "SQL_VALIDATION_FAILED");
        }

        var dataSource = await _context.DataSources.FindAsync(dataSourceId);
        if (dataSource == null)
        {
            return ApiResponse<List<Dictionary<string, object>>>.Fail("数据源不存在", "NOT_FOUND");
        }

        if (!dataSource.IsActive)
        {
            return ApiResponse<List<Dictionary<string, object>>>.Fail("数据源已被禁用", "DATASOURCE_DISABLED");
        }

        try
        {
            // 执行查询
            var result = await _databaseService.ExecuteQueryAsync(dataSource, sql, parameters);

            if (!result.Success)
            {
                return ApiResponse<List<Dictionary<string, object>>>.Fail(result.Message, result.ErrorCode);
            }

            // 限制返回行数（测试时只返回前 100 行）
            var limitedResult = result.Data?.Take(100).ToList() ?? new List<Dictionary<string, object>>();
            return ApiResponse<List<Dictionary<string, object>>>.Ok(limitedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"测试查询失败: DataSourceId={dataSourceId}, Error={ex.Message}");
            return ApiResponse<List<Dictionary<string, object>>>.Fail($"测试查询失败: {ex.Message}", "QUERY_ERROR");
        }
    }

    public async Task<ApiResponse<byte[]>> ExportReportAsync(int reportId, ExecuteReportRequest request)
    {
        var report = await _context.Reports
            .Include(r => r.DataSource)
            .Include(r => r.Fields)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);

        if (report == null)
        {
            return ApiResponse<byte[]>.Fail("报表不存在", "NOT_FOUND");
        }

        if (!report.IsEnabled)
        {
            return ApiResponse<byte[]>.Fail("报表已被禁用", "REPORT_DISABLED");
        }

        if (!report.DataSource.IsActive)
        {
            return ApiResponse<byte[]>.Fail("数据源已被禁用", "DATASOURCE_DISABLED");
        }

        // 获取导出格式
        ExportFormat format = ExportFormat.Excel;
        string? fileName = null;

        if (request is ExportReportRequest exportRequest)
        {
            format = exportRequest.Format;
            fileName = exportRequest.FileName;
        }

        try
        {
            // 构建带查询条件的SQL
            var (modifiedSql, sqlParameters) = BuildQueryWithConditions(report.SqlStatement, request.Parameters);

            // 执行查询
            var result = await _databaseService.ExecuteQueryDataTableAsync(report.DataSource, modifiedSql, sqlParameters);

            if (!result.Success || result.Data == null)
            {
                return ApiResponse<byte[]>.Fail(result.Message ?? "查询数据失败", result.ErrorCode ?? "QUERY_ERROR");
            }

            var dataTable = result.Data;

            // 根据格式导出
            byte[] exportData;
            switch (format)
            {
                case ExportFormat.Excel:
                    exportData = ExportToExcel(dataTable, report.ReportName, report.Fields.ToList());
                    break;
                case ExportFormat.Csv:
                    exportData = ExportToCsv(dataTable);
                    break;
                case ExportFormat.Pdf:
                    exportData = ExportToPdf(dataTable, report.ReportName, report.Fields.ToList());
                    break;
                default:
                    return ApiResponse<byte[]>.Fail("不支持的导出格式", "UNSUPPORTED_FORMAT");
            }

            // 更新查看次数
            report.ViewCount++;
            report.LastViewTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<byte[]>.Ok(exportData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"报表导出失败: ReportId={reportId}, Error={ex.Message}");
            return ApiResponse<byte[]>.Fail($"报表导出失败: {ex.Message}", "EXPORT_ERROR");
        }
    }

    /// <summary>
    /// 导出为 Excel 格式
    /// </summary>
    private byte[] ExportToExcel(DataTable dataTable, string reportName, List<ReportField> fields)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(reportName);

        // 设置列标题
        var col = 1;
        foreach (DataColumn column in dataTable.Columns)
        {
            var cell = worksheet.Cell(1, col);
            cell.Value = column.ColumnName;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;

            // 设置列宽
            var field = fields.FirstOrDefault(f => f.FieldName == column.ColumnName);
            if (field != null)
            {
                worksheet.Column(col).Width = field.Width / 10.0;
            }
            else
            {
                worksheet.Column(col).AdjustToContents();
            }

            col++;
        }

        // 填充数据
        var row = 2;
        foreach (DataRow dataRow in dataTable.Rows)
        {
            col = 1;
            foreach (DataColumn column in dataTable.Columns)
            {
                var value = dataRow[column];
                if (value == null || value == DBNull.Value)
                {
                    worksheet.Cell(row, col).Value = "";
                }
                else
                {
                    // 根据类型设置值
                    switch (value)
                    {
                        case int i:
                            worksheet.Cell(row, col).Value = i;
                            break;
                        case double d:
                            worksheet.Cell(row, col).Value = d;
                            break;
                        case decimal dm:
                            worksheet.Cell(row, col).Value = dm;
                            break;
                        case float f:
                            worksheet.Cell(row, col).Value = f;
                            break;
                        case long l:
                            worksheet.Cell(row, col).Value = l;
                            break;
                        case bool b:
                            worksheet.Cell(row, col).Value = b;
                            break;
                        case DateTime dt:
                            worksheet.Cell(row, col).Value = dt;
                            break;
                        default:
                            worksheet.Cell(row, col).Value = value.ToString() ?? "";
                            break;
                    }
                }
                col++;
            }
            row++;
        }

        // 添加边框
        var range = worksheet.Range(1, 1, row - 1, dataTable.Columns.Count);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        using var stream = new System.IO.MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// 导出为 CSV 格式
    /// </summary>
    private byte[] ExportToCsv(DataTable dataTable)
    {
        using var output = new System.IO.MemoryStream();
        using var writer = new System.IO.StreamWriter(output, System.Text.Encoding.UTF8);

        // 写入列标题
        for (var i = 0; i < dataTable.Columns.Count; i++)
        {
            writer.Write(EscapeCsvField(dataTable.Columns[i].ColumnName));
            if (i < dataTable.Columns.Count - 1)
            {
                writer.Write(",");
            }
        }
        writer.WriteLine();

        // 写入数据行
        foreach (DataRow row in dataTable.Rows)
        {
            for (var i = 0; i < dataTable.Columns.Count; i++)
            {
                writer.Write(EscapeCsvField(row[i]?.ToString() ?? ""));
                if (i < dataTable.Columns.Count - 1)
                {
                    writer.Write(",");
                }
            }
            writer.WriteLine();
        }

        writer.Flush();
        return output.ToArray();
    }

    /// <summary>
    /// 导出为 PDF 格式
    /// </summary>
    private byte[] ExportToPdf(DataTable dataTable, string reportName, List<ReportField> fields)
    {
        // 配置 QuestPDF 许可（社区版）
        QuestPDF.Settings.License = LicenseType.Community;

        // 准备数据
        var columnNames = new List<string>();
        var columnDisplayNames = new List<string>();
        var rows = new List<List<string>>();

        // 获取列信息
        foreach (DataColumn column in dataTable.Columns)
        {
            var field = fields.FirstOrDefault(f => f.FieldName == column.ColumnName);
            columnNames.Add(column.ColumnName);
            columnDisplayNames.Add(field?.DisplayName ?? column.ColumnName);
        }

        // 获取数据行
        foreach (DataRow dataRow in dataTable.Rows)
        {
            var row = new List<string>();
            foreach (var columnName in columnNames)
            {
                var value = dataRow[columnName];
                row.Add(value?.ToString() ?? "");
            }
            rows.Add(row);
        }

        // 定义样式
        var headerStyle = TextStyle.Default.FontSize(10).Bold();
        var dataStyle = TextStyle.Default.FontSize(9);
        var titleStyle = TextStyle.Default.FontSize(20).Bold();

        // 生成 PDF
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header().ShowOnce().Text(reportName).Style(titleStyle);

                page.Content().PaddingTop(1, Unit.Centimetre).Table(table =>
                {
                    // 定义表格列
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in columnNames)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    // 表头
                    table.Header(header =>
                    {
                        foreach (var displayName in columnDisplayNames)
                        {
                            header.Cell()
                                .Element(cell => cell
                                    .Padding(5)
                                    .Border(1)
                                    .Background("#E0E0E0"))
                                .Text(displayName).Style(headerStyle);
                        }
                    });

                    // 数据行
                    foreach (var row in rows)
                    {
                        var rowIndex = rows.IndexOf(row);
                        foreach (var cellValue in row)
                        {
                            table.Cell()
                                .Element(cell => cell
                                    .Padding(5)
                                    .Border(1)
                                    .Background(rowIndex % 2 == 0 ? "#FFFFFF" : "#F5F5F5"))
                                .Text(cellValue).Style(dataStyle);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("第 ");
                    x.CurrentPageNumber();
                    x.Span(" 页，共 ");
                    x.TotalPages();
                    x.Span(" 页");
                });
            });
        }).GeneratePdf();
    }

    /// <summary>
    /// 转义 CSV 字段
    /// </summary>
    private string EscapeCsvField(string field)
    {
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    /// <summary>
    /// 获取报表统计信息
    /// </summary>
    public async Task<ApiResponse<object>> GetReportStatisticsAsync(int reportId)
    {
        var report = await _context.Reports.FindAsync(reportId);
        if (report == null)
        {
            return ApiResponse<object>.Fail("报表不存在", "REPORT_NOT_FOUND");
        }

        var statistics = new
        {
            report.ReportId,
            report.ReportName,
            report.ViewCount,
            report.LastViewTime,
            report.CreatedTime,
            report.UpdatedTime
        };

        return ApiResponse<object>.Ok(statistics);
    }

    /// <summary>
    /// 复制报表
    /// </summary>
    public async Task<ApiResponse<ReportDetailDto>> CopyReportAsync(int reportId, int? userId)
    {
        var originalReport = await _context.Reports
            .Include(r => r.Fields)
            .Include(r => r.Parameters)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);

        if (originalReport == null)
        {
            return ApiResponse<ReportDetailDto>.Fail("报表不存在", "REPORT_NOT_FOUND");
        }

        // 创建新报表
        var newReport = new Report
        {
            ReportName = $"{originalReport.ReportName} (副本)",
            ReportCode = $"{originalReport.ReportCode}_COPY_{DateTime.UtcNow:yyyyMMddHHmmss}",
            ReportCategory = originalReport.ReportCategory,
            DataSourceId = originalReport.DataSourceId,
            SqlStatement = originalReport.SqlStatement,
            Description = originalReport.Description,
            IsPaged = originalReport.IsPaged,
            PageSize = originalReport.PageSize,
            CacheDuration = originalReport.CacheDuration,
            IsEnabled = false,
            IsSystem = false,
            ViewCount = 0,
            CreatedBy = userId,
            CreatedTime = DateTime.UtcNow,
            EnableChart = originalReport.EnableChart,
            ChartConfig = originalReport.ChartConfig,
            QueryConditions = originalReport.QueryConditions
        };

        _context.Reports.Add(newReport);
        await _context.SaveChangesAsync();

        // 复制字段配置
        foreach (var field in originalReport.Fields)
        {
            var newField = new ReportField
            {
                ReportId = newReport.ReportId,
                FieldName = field.FieldName,
                DisplayName = field.DisplayName,
                DataType = field.DataType,
                Width = field.Width,
                IsVisible = field.IsVisible,
                IsSortable = field.IsSortable,
                IsFilterable = field.IsFilterable,
                IsGroupable = field.IsGroupable,
                SortOrder = field.SortOrder,
                Align = field.Align,
                FormatString = field.FormatString,
                AggregateFunction = field.AggregateFunction,
                CssClass = field.CssClass,
                Remark = field.Remark,
                CreatedTime = DateTime.UtcNow
            };
            _context.ReportFields.Add(newField);
        }

        // 复制参数配置
        foreach (var param in originalReport.Parameters)
        {
            var newParam = new ReportParameter
            {
                ReportId = newReport.ReportId,
                ParameterName = param.ParameterName,
                DisplayName = param.DisplayName,
                DataType = param.DataType,
                InputType = param.InputType,
                DefaultValue = param.DefaultValue,
                IsRequired = param.IsRequired,
                SortOrder = param.SortOrder,
                Options = param.Options,
                QueryOptions = param.QueryOptions,
                Remark = param.Remark,
                CreatedTime = DateTime.UtcNow
            };
            _context.ReportParameters.Add(newParam);
        }

        await _context.SaveChangesAsync();

        return await GetReportByIdAsync(newReport.ReportId);
    }

    /// <summary>
    /// 切换报表启用状态
    /// </summary>
    public async Task<ApiResponse> ToggleReportAsync(int reportId)
    {
        var report = await _context.Reports.FindAsync(reportId);
        if (report == null)
        {
            return ApiResponse.Fail("报表不存在", "NOT_FOUND");
        }

        report.IsEnabled = !report.IsEnabled;
        report.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse.Ok(report.IsEnabled ? "已启用" : "已停用");
    }

    /// <summary>
    /// 导出所有报表配置
    /// </summary>
    public async Task<ApiResponse<string>> ExportAllReportConfigsAsync()
    {
        var reports = await _context.Reports
            .Include(r => r.Fields)
            .Include(r => r.Parameters)
            .Where(r => !r.IsSystem)
            .OrderBy(r => r.ReportCategory)
            .ThenBy(r => r.ReportName)
            .ToListAsync();

        var configs = reports.Select(r => new
        {
            r.ReportName,
            r.ReportCode,
            r.ReportCategory,
            r.DataSourceId,
            r.SqlStatement,
            r.Description,
            r.EnableChart,
            r.ChartConfig,
            r.QueryConditions,
            Fields = r.Fields.Select(f => new
            {
                f.FieldName,
                f.DisplayName,
                f.DataType,
                f.Width,
                f.IsVisible,
                f.IsSortable,
                f.Align
            }).ToList(),
            Parameters = r.Parameters.Select(p => new
            {
                p.ParameterName,
                p.DisplayName,
                p.DataType,
                p.InputType,
                p.DefaultValue,
                p.IsRequired
            }).ToList()
        }).ToList();

        var json = JsonSerializer.Serialize(configs, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        return ApiResponse<string>.Ok(json);
    }

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

    /// <summary>
    /// 解析 between 操作符的值
    /// 支持两种格式：数组 ["start", "end"] 或逗号分隔的字符串 "start,end"
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

    /// <summary>
    /// 将 JsonElement 转换为实际的 .NET 类型
    /// </summary>
    private object ConvertJsonElement(object value)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                JsonValueKind.Number => jsonElement.TryGetInt32(out int intVal) ? intVal : jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => DBNull.Value,
                _ => jsonElement.ToString()
            };
        }
        return value;
    }
}
