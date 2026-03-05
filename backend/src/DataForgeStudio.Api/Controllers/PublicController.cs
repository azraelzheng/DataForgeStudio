using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 公开访问控制器（无需认证）
/// 用于访问公开的大屏展示页面
/// </summary>
[ApiController]
[Route("public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<PublicController> _logger;

    public PublicController(
        IDashboardService dashboardService,
        ILogger<PublicController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// 获取公开大屏配置和数据
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <returns>大屏配置和组件数据（仅限公开大屏）</returns>
    [HttpGet("d/{id}")]
    [ProducesResponseType(typeof(ApiResponse<PublicDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<PublicDashboardDto>> GetPublicDashboard(int id)
    {
        _logger.LogInformation("公开访问大屏: DashboardId={DashboardId}", id);

        // 获取公开大屏详情（服务层已验证 IsPublic）
        var detailResult = await _dashboardService.GetPublicDashboardAsync(id);
        if (!detailResult.Success || detailResult.Data == null)
        {
            return ApiResponse<PublicDashboardDto>.Fail(
                detailResult.Message,
                detailResult.ErrorCode);
        }

        // 获取大屏数据（服务层已验证 IsPublic）
        var dataResult = await _dashboardService.GetPublicDashboardDataAsync(id);
        if (!dataResult.Success || dataResult.Data == null)
        {
            return ApiResponse<PublicDashboardDto>.Fail(
                dataResult.Message,
                dataResult.ErrorCode);
        }

        // 组装公开大屏响应
        var publicDashboard = new PublicDashboardDto
        {
            Name = detailResult.Data.Name,
            Description = detailResult.Data.Description,
            Theme = detailResult.Data.Theme,
            ThemeConfig = detailResult.Data.ThemeConfig,
            RefreshInterval = detailResult.Data.RefreshInterval,
            LayoutConfig = detailResult.Data.LayoutConfig,
            Widgets = detailResult.Data.Widgets.Select(w => new PublicWidgetDto
            {
                WidgetId = w.WidgetId,
                WidgetType = w.WidgetType,
                Title = w.Title,
                PositionX = w.PositionX,
                PositionY = w.PositionY,
                Width = w.Width,
                Height = w.Height,
                StyleConfig = w.StyleConfig,
                Rules = w.Rules,
                Data = dataResult.Data.WidgetData.TryGetValue(w.WidgetId, out var widgetData)
                    ? widgetData
                    : null
            }).ToList()
        };

        return ApiResponse<PublicDashboardDto>.Ok(publicDashboard);
    }

    /// <summary>
    /// 仅获取公开大屏数据（用于刷新）
    /// </summary>
    /// <param name="id">大屏ID</param>
    /// <returns>大屏数据（仅限公开大屏）</returns>
    [HttpGet("d/{id}/data")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<int, WidgetDataResult>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<Dictionary<int, WidgetDataResult>>> GetPublicDashboardData(int id)
    {
        _logger.LogInformation("刷新公开大屏数据: DashboardId={DashboardId}", id);

        // 获取公开大屏数据（服务层已验证 IsPublic）
        var dataResult = await _dashboardService.GetPublicDashboardDataAsync(id);
        if (!dataResult.Success || dataResult.Data == null)
        {
            return ApiResponse<Dictionary<int, WidgetDataResult>>.Fail(
                dataResult.Message,
                dataResult.ErrorCode);
        }

        return ApiResponse<Dictionary<int, WidgetDataResult>>.Ok(dataResult.Data.WidgetData);
    }

    /// <summary>
    /// 根据公开URL获取大屏配置和数据（无需登录，GUID格式URL）
    /// </summary>
    /// <param name="publicUrl">公开URL标识（GUID）</param>
    /// <returns>大屏配置和组件数据</returns>
    [HttpGet("url/{publicUrl}")]
    [ProducesResponseType(typeof(ApiResponse<PublicDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<PublicDashboardDto>> GetPublicDashboardByUrl(string publicUrl)
    {
        _logger.LogInformation("公开访问大屏（URL方式）: PublicUrl={PublicUrl}", publicUrl);

        // 获取公开大屏详情
        var detailResult = await _dashboardService.GetPublicDashboardByUrlAsync(publicUrl);
        if (!detailResult.Success || detailResult.Data == null)
        {
            return ApiResponse<PublicDashboardDto>.Fail(
                detailResult.Message,
                detailResult.ErrorCode);
        }

        // 获取大屏数据
        var dataResult = await _dashboardService.GetPublicDashboardDataByUrlAsync(publicUrl);
        if (!dataResult.Success || dataResult.Data == null)
        {
            return ApiResponse<PublicDashboardDto>.Fail(
                dataResult.Message,
                dataResult.ErrorCode);
        }

        // 组装公开大屏响应
        var publicDashboard = new PublicDashboardDto
        {
            Name = detailResult.Data.Name,
            Description = detailResult.Data.Description,
            Theme = detailResult.Data.Theme,
            ThemeConfig = detailResult.Data.ThemeConfig,
            RefreshInterval = detailResult.Data.RefreshInterval,
            LayoutConfig = detailResult.Data.LayoutConfig,
            Widgets = detailResult.Data.Widgets.Select(w => new PublicWidgetDto
            {
                WidgetId = w.WidgetId,
                WidgetType = w.WidgetType,
                Title = w.Title,
                PositionX = w.PositionX,
                PositionY = w.PositionY,
                Width = w.Width,
                Height = w.Height,
                StyleConfig = w.StyleConfig,
                Rules = w.Rules,
                Data = dataResult.Data.WidgetData.TryGetValue(w.WidgetId, out var widgetData)
                    ? widgetData
                    : null
            }).ToList()
        };

        return ApiResponse<PublicDashboardDto>.Ok(publicDashboard);
    }

    /// <summary>
    /// 根据公开URL仅获取大屏数据（用于刷新，GUID格式URL）
    /// </summary>
    /// <param name="publicUrl">公开URL标识（GUID）</param>
    /// <returns>大屏数据</returns>
    [HttpGet("url/{publicUrl}/data")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<int, WidgetDataResult>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<Dictionary<int, WidgetDataResult>>> GetPublicDashboardDataByUrl(string publicUrl)
    {
        _logger.LogInformation("刷新公开大屏数据（URL方式）: PublicUrl={PublicUrl}", publicUrl);

        // 获取公开大屏数据
        var dataResult = await _dashboardService.GetPublicDashboardDataByUrlAsync(publicUrl);
        if (!dataResult.Success || dataResult.Data == null)
        {
            return ApiResponse<Dictionary<int, WidgetDataResult>>.Fail(
                dataResult.Message,
                dataResult.ErrorCode);
        }

        return ApiResponse<Dictionary<int, WidgetDataResult>>.Ok(dataResult.Data.WidgetData);
    }
}
