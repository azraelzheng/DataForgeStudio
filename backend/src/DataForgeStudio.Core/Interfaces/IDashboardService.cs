using DataForgeStudio.Domain.DTOs;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 看板服务接口
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// 获取看板列表
    /// </summary>
    Task<ApiResponse<List<DashboardDto>>> GetDashboardsAsync();

    /// <summary>
    /// 获取看板详情
    /// </summary>
    Task<ApiResponse<DashboardDto>> GetDashboardByIdAsync(string id);

    /// <summary>
    /// 创建看板
    /// </summary>
    Task<ApiResponse<DashboardDto>> CreateDashboardAsync(DashboardCreateDto dto, int userId);

    /// <summary>
    /// 更新看板
    /// </summary>
    Task<ApiResponse<DashboardDto>> UpdateDashboardAsync(string id, DashboardUpdateDto dto, int userId);

    /// <summary>
    /// 删除看板
    /// </summary>
    Task<ApiResponse<bool>> DeleteDashboardAsync(string id);

    /// <summary>
    /// 添加组件到看板
    /// </summary>
    Task<ApiResponse<DashboardWidgetDto>> AddWidgetAsync(string dashboardId, DashboardWidgetDto dto, int userId);

    /// <summary>
    /// 更新看板组件
    /// </summary>
    Task<ApiResponse<DashboardWidgetDto>> UpdateWidgetAsync(string dashboardId, string widgetId, DashboardWidgetDto dto, int userId);

    /// <summary>
    /// 删除看板组件
    /// </summary>
    Task<ApiResponse<bool>> DeleteWidgetAsync(string dashboardId, string widgetId);
}

/// <summary>
/// 车间大屏服务接口
/// </summary>
public interface IDisplayService
{
    /// <summary>
    /// 获取大屏配置列表
    /// </summary>
    Task<ApiResponse<List<DisplayConfigDto>>> GetDisplayConfigsAsync();

    /// <summary>
    /// 获取大屏配置详情
    /// </summary>
    Task<ApiResponse<DisplayConfigDto>> GetDisplayConfigByIdAsync(string id);

    /// <summary>
    /// 创建大屏配置
    /// </summary>
    Task<ApiResponse<DisplayConfigDto>> CreateDisplayConfigAsync(DisplayConfigCreateDto dto, int userId);

    /// <summary>
    /// 更新大屏配置
    /// </summary>
    Task<ApiResponse<DisplayConfigDto>> UpdateDisplayConfigAsync(string id, DisplayConfigUpdateDto dto, int userId);

    /// <summary>
    /// 删除大屏配置
    /// </summary>
    Task<ApiResponse<bool>> DeleteDisplayConfigAsync(string id);

    /// <summary>
    /// 获取大屏聚合数据
    /// </summary>
    Task<ApiResponse<DisplayDataResponseDto>> GetDisplayDataAsync(string id);
}
