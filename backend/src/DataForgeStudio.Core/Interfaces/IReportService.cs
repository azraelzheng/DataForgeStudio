using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 报表服务接口
/// </summary>
public interface IReportService
{
    /// <summary>
    /// 获取报表分页列表
    /// </summary>
    Task<ApiResponse<PagedResponse<ReportDto>>> GetReportsAsync(PagedRequest request, string? reportName = null, string? category = null);

    /// <summary>
    /// 获取报表详情
    /// </summary>
    Task<ApiResponse<ReportDetailDto>> GetReportByIdAsync(int reportId);

    /// <summary>
    /// 创建报表
    /// </summary>
    Task<ApiResponse<ReportDto>> CreateReportAsync(CreateReportRequest request, int createdBy);

    /// <summary>
    /// 更新报表
    /// </summary>
    Task<ApiResponse> UpdateReportAsync(int reportId, CreateReportRequest request);

    /// <summary>
    /// 删除报表
    /// </summary>
    Task<ApiResponse> DeleteReportAsync(int reportId);

    /// <summary>
    /// 执行报表查询
    /// </summary>
    Task<ApiResponse<List<Dictionary<string, object>>>> ExecuteReportAsync(int reportId, ExecuteReportRequest request);

    /// <summary>
    /// 测试SQL查询（用于报表设计器）
    /// </summary>
    Task<ApiResponse<List<Dictionary<string, object>>>> TestQueryAsync(int dataSourceId, string sql, Dictionary<string, object>? parameters);

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

    /// <summary>
    /// 导出报表
    /// </summary>
    Task<ApiResponse<byte[]>> ExportReportAsync(int reportId, ExecuteReportRequest request);

    /// <summary>
    /// 获取报表统计信息
    /// </summary>
    Task<ApiResponse<object>> GetReportStatisticsAsync(int reportId);

    /// <summary>
    /// 复制报表
    /// </summary>
    Task<ApiResponse<ReportDetailDto>> CopyReportAsync(int reportId, int? userId);

    /// <summary>
    /// 导出所有报表配置
    /// </summary>
    Task<ApiResponse<string>> ExportAllReportConfigsAsync();

    /// <summary>
    /// 切换报表启用状态
    /// </summary>
    Task<ApiResponse> ToggleReportAsync(int reportId);
}
