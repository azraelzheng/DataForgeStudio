using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 数据源管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataSourcesController : ControllerBase
{
    private readonly IDataSourceService _dataSourceService;
    private readonly ILogger<DataSourcesController> _logger;

    public DataSourcesController(IDataSourceService dataSourceService, ILogger<DataSourcesController> logger)
    {
        _dataSourceService = dataSourceService;
        _logger = logger;
    }

    /// <summary>
    /// 获取数据源列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponse<PagedResponse<DataSourceDto>>> GetDataSources(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? dataSourceName = null,
        [FromQuery] string? dbType = null)
    {
        var request = new PagedRequest { PageIndex = page, PageSize = pageSize };
        return await _dataSourceService.GetDataSourcesAsync(request, dataSourceName, dbType, includeInactive: true);
    }

    /// <summary>
    /// 获取启用的数据源列表（用于报表设计器）
    /// </summary>
    [HttpGet("active")]
    public async Task<ApiResponse<List<DataSourceDto>>> GetActiveDataSources()
    {
        var result = await _dataSourceService.GetDataSourcesAsync(new PagedRequest { PageIndex = 1, PageSize = 1000 }, includeInactive: false);
        if (!result.Success)
        {
            return ApiResponse<List<DataSourceDto>>.Fail(result.Message, result.ErrorCode);
        }
        return ApiResponse<List<DataSourceDto>>.Ok(result.Data.Items);
    }

    /// <summary>
    /// 获取数据源详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<DataSourceDto>> GetDataSource(int id)
    {
        return await _dataSourceService.GetDataSourceByIdAsync(id);
    }

    /// <summary>
    /// 创建数据源
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<DataSourceDto>> CreateDataSource([FromBody] CreateDataSourceRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return ApiResponse<DataSourceDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _dataSourceService.CreateDataSourceAsync(request, userId);
    }

    /// <summary>
    /// 更新数据源
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse> UpdateDataSource(int id, [FromBody] CreateDataSourceRequest request)
    {
        return await _dataSourceService.UpdateDataSourceAsync(id, request);
    }

    /// <summary>
    /// 删除数据源
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse> DeleteDataSource(int id)
    {
        return await _dataSourceService.DeleteDataSourceAsync(id);
    }

    /// <summary>
    /// 测试连接
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<ApiResponse> TestConnection(int id)
    {
        return await _dataSourceService.TestConnectionAsync(id);
    }

    /// <summary>
    /// 测试连接（创建前）
    /// </summary>
    [HttpPost("test")]
    public async Task<ApiResponse> TestConnection([FromBody] CreateDataSourceRequest request)
    {
        return await _dataSourceService.TestConnectionAsync(request);
    }

    /// <summary>
    /// 获取数据库列表
    /// </summary>
    [HttpPost("databases")]
    public async Task<ApiResponse<List<string>>> GetDatabases([FromBody] CreateDataSourceRequest request)
    {
        return await _dataSourceService.GetDatabasesAsync(request);
    }

    /// <summary>
    /// 停用/启用数据源
    /// </summary>
    [HttpPost("{id}/toggle-active")]
    public async Task<ApiResponse> ToggleActive(int id)
    {
        return await _dataSourceService.ToggleActiveAsync(id);
    }
}
