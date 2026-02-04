using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 报表管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// 获取报表列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponse<PagedResponse<ReportDto>>> GetReports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? reportName = null,
        [FromQuery] string? category = null)
    {
        var request = new PagedRequest { PageIndex = page, PageSize = pageSize };
        return await _reportService.GetReportsAsync(request, reportName, category);
    }

    /// <summary>
    /// 获取报表详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<ReportDetailDto>> GetReport(int id)
    {
        return await _reportService.GetReportByIdAsync(id);
    }

    /// <summary>
    /// 创建报表
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<ReportDto>> CreateReport([FromBody] CreateReportRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return ApiResponse<ReportDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _reportService.CreateReportAsync(request, userId);
    }

    /// <summary>
    /// 更新报表
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse> UpdateReport(int id, [FromBody] CreateReportRequest request)
    {
        return await _reportService.UpdateReportAsync(id, request);
    }

    /// <summary>
    /// 删除报表
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse> DeleteReport(int id)
    {
        return await _reportService.DeleteReportAsync(id);
    }

    /// <summary>
    /// 执行报表查询
    /// </summary>
    [HttpPost("{id}/execute")]
    public async Task<ApiResponse<List<Dictionary<string, object>>>> ExecuteReport(int id, [FromBody] ExecuteReportRequest request)
    {
        return await _reportService.ExecuteReportAsync(id, request);
    }

    /// <summary>
    /// 测试SQL查询（用于报表设计器）
    /// </summary>
    [HttpPost("test-query")]
    public async Task<ApiResponse<List<Dictionary<string, object>>>> TestQuery([FromBody] TestQueryRequest request)
    {
        return await _reportService.TestQueryAsync(request.DataSourceId, request.Sql, request.Parameters);
    }

    /// <summary>
    /// 导出报表
    /// </summary>
    [HttpPost("{id}/export")]
    public async Task<IActionResult> ExportReport(int id, [FromBody] ExecuteReportRequest request)
    {
        var result = await _reportService.ExportReportAsync(id, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return File(
            result.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"report_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx"
        );
    }
}
