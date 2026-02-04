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
    /// 导出报表
    /// </summary>
    Task<ApiResponse<byte[]>> ExportReportAsync(int reportId, ExecuteReportRequest request);
}
