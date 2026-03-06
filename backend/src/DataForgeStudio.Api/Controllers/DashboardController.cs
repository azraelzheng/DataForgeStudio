using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 大屏管理控制器
///
/// 权限说明：
/// - dashboard:view  - 查看大屏列表和详情
/// - dashboard:edit  - 创建和编辑大屏/组件
/// - dashboard:delete - 删除大屏/组件
///
/// 前端路由已配置权限检查，后端通过 [Authorize] 确保用户已认证
/// </summary>
[ApiController]
[Route("api/dashboards")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILicenseService licenseService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
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
    /// 获取当前用户ID
    /// </summary>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    #region 大屏 CRUD

    /// <summary>
    /// 获取大屏列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="name">大屏名称过滤（可选）</param>
    /// <returns>大屏分页列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DashboardDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<PagedResponse<DashboardDto>>> GetDashboards(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? name = null)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<PagedResponse<DashboardDto>>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        var request = new PagedRequest { PageIndex = page, PageSize = pageSize };
        return await _dashboardService.GetDashboardsAsync(request, name);
    }

    /// <summary>
    /// 获取大屏详情
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <returns>大屏详情（包含组件列表）</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DashboardDetailDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<DashboardDetailDto>> GetDashboard(int id)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<DashboardDetailDto>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        return await _dashboardService.GetDashboardByIdAsync(id);
    }

    /// <summary>
    /// 创建大屏
    /// </summary>
    /// <param name="request">创建大屏请求</param>
    /// <returns>创建的大屏信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<DashboardDto>> CreateDashboard([FromBody] CreateDashboardRequest request)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<DashboardDto>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return ApiResponse<DashboardDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _dashboardService.CreateDashboardAsync(request, userId.Value);
    }

    /// <summary>
    /// 更新大屏
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <param name="request">更新大屏请求</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ApiResponse> UpdateDashboard(int id, [FromBody] CreateDashboardRequest request)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return licenseError;
        }

        return await _dashboardService.UpdateDashboardAsync(id, request);
    }

    /// <summary>
    /// 删除大屏
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ApiResponse> DeleteDashboard(int id)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return licenseError;
        }

        return await _dashboardService.DeleteDashboardAsync(id);
    }

    #endregion

    #region 发布管理

    /// <summary>
    /// 发布大屏
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <returns>更新后的大屏信息</returns>
    [HttpPost("{id}/publish")]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<DashboardDto>> PublishDashboard(int id)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<DashboardDto>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return ApiResponse<DashboardDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _dashboardService.PublishDashboardAsync(id, userId.Value);
    }

    /// <summary>
    /// 取消发布大屏
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <returns>更新后的大屏信息</returns>
    [HttpDelete("{id}/publish")]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<DashboardDto>> UnpublishDashboard(int id)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<DashboardDto>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return ApiResponse<DashboardDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _dashboardService.UnpublishDashboardAsync(id, userId.Value);
    }

    #endregion

    #region 访问设置

    /// <summary>
    /// 更新大屏访问设置
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <param name="request">访问设置请求</param>
    /// <returns>访问设置信息</returns>
    [HttpPut("{id}/access")]
    [ProducesResponseType(typeof(ApiResponse<DashboardAccessDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<DashboardAccessDto>> UpdateDashboardAccess(
        int id, [FromBody] UpdateDashboardAccessRequest request)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<DashboardAccessDto>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return ApiResponse<DashboardAccessDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _dashboardService.UpdateDashboardAccessAsync(id, request, userId.Value);
    }

    /// <summary>
    /// 获取大屏访问设置
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <returns>访问设置信息</returns>
    [HttpGet("{id}/access")]
    [ProducesResponseType(typeof(ApiResponse<DashboardAccessDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<DashboardAccessDto>> GetDashboardAccess(int id)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<DashboardAccessDto>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        return await _dashboardService.GetDashboardAccessAsync(id);
    }

    #endregion

    #region 组件管理

    /// <summary>
    /// 添加组件
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <param name="request">创建组件请求</param>
    /// <returns>创建的组件信息</returns>
    [HttpPost("{id}/widgets")]
    [ProducesResponseType(typeof(ApiResponse<DashboardWidgetDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<DashboardWidgetDto>> AddWidget(int id, [FromBody] CreateWidgetRequest request)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<DashboardWidgetDto>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        // 确保 DashboardId 一致
        request.DashboardId = id;
        return await _dashboardService.AddWidgetAsync(id, request);
    }

    /// <summary>
    /// 更新组件
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <param name="widgetId">组件ID</param>
    /// <param name="request">更新组件请求</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/widgets/{widgetId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ApiResponse> UpdateWidget(int id, int widgetId, [FromBody] CreateWidgetRequest request)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return licenseError;
        }

        // 确保 DashboardId 一致
        request.DashboardId = id;
        return await _dashboardService.UpdateWidgetAsync(id, widgetId, request);
    }

    /// <summary>
    /// 删除组件
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <param name="widgetId">组件ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}/widgets/{widgetId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ApiResponse> DeleteWidget(int id, int widgetId)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return licenseError;
        }

        return await _dashboardService.DeleteWidgetAsync(id, widgetId);
    }

    /// <summary>
    /// 批量更新组件位置
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <param name="request">组件位置列表</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/widgets/positions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ApiResponse> UpdateWidgetPositions(int id, [FromBody] BatchWidgetPositionRequest request)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return licenseError;
        }

        return await _dashboardService.UpdateWidgetPositionsAsync(id, request.Widgets);
    }

    #endregion

    #region 一键转换

    /// <summary>
    /// 从报表一键转换为大屏
    /// </summary>
    /// <param name="request">转换请求</param>
    /// <returns>创建的大屏详情</returns>
    [HttpPost("convert")]
    [ProducesResponseType(typeof(ApiResponse<DashboardDetailDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<DashboardDetailDto>> ConvertFromReport([FromBody] ConvertReportToDashboardRequest request)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<DashboardDetailDto>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return ApiResponse<DashboardDetailDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _dashboardService.ConvertFromReportAsync(
            request.ReportId,
            request.DashboardName,
            userId.Value);
    }

    #endregion

    #region 数据获取

    /// <summary>
    /// 获取大屏数据（包含所有组件的数据）
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <returns>大屏数据（包含组件数据字典）</returns>
    [HttpGet("{id}/data")]
    [ProducesResponseType(typeof(ApiResponse<DashboardDataDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<DashboardDataDto>> GetDashboardData(int id)
    {
        // 验证许可证
        var licenseError = await ValidateLicenseAsync();
        if (licenseError != null)
        {
            return ApiResponse<DashboardDataDto>.Fail(licenseError.Message, licenseError.ErrorCode);
        }

        return await _dashboardService.GetDashboardDataAsync(id);
    }

    #endregion
}
