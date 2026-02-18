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
    private readonly ILicenseService _licenseService;
    private readonly ILogger<DataSourcesController> _logger;

    public DataSourcesController(IDataSourceService dataSourceService, ILicenseService licenseService, ILogger<DataSourcesController> logger)
    {
        _dataSourceService = dataSourceService;
        _licenseService = licenseService;
        _logger = logger;
    }

    /// <summary>
    /// 验证许可证是否有效
    /// </summary>
    private async Task<ApiResponse?> ValidateLicenseAsync()
    {
        var validation = await _licenseService.ValidateLicenseAsync();
        if (!validation.Success || validation.Data == null || !validation.Data.Valid)
        {
            return ApiResponse.Fail(
                validation.Data?.Message ?? "许可证无效或已过期，请续费后继续使用",
                "LICENSE_INVALID");
        }
        return null;
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
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<PagedResponse<DataSourceDto>>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        var request = new PagedRequest { PageIndex = page, PageSize = pageSize };
        return await _dataSourceService.GetDataSourcesAsync(request, dataSourceName, dbType, includeInactive: true);
    }

    /// <summary>
    /// 获取启用的数据源列表（用于报表设计器）
    /// </summary>
    [HttpGet("active")]
    public async Task<ApiResponse<List<DataSourceDto>>> GetActiveDataSources()
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<List<DataSourceDto>>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

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
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<DataSourceDto>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        return await _dataSourceService.GetDataSourceByIdAsync(id);
    }

    /// <summary>
    /// 创建数据源
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<DataSourceDto>> CreateDataSource([FromBody] CreateDataSourceRequest request)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<DataSourceDto>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

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
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return licenseError;
        }

        return await _dataSourceService.UpdateDataSourceAsync(id, request);
    }

    /// <summary>
    /// 删除数据源
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse> DeleteDataSource(int id)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return licenseError;
        }

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

    /// <summary>
    /// 获取数据源的表结构（用于SQL编辑器自动补全）
    /// </summary>
    [HttpGet("{id}/tables")]
    public async Task<ApiResponse<List<TableInfoDto>>> GetTableStructure(int id)
    {
        return await _dataSourceService.GetTableStructureAsync(id);
    }
}
