using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 大屏服务接口
/// </summary>
public interface IDashboardService
{
    #region 大屏 CRUD

    /// <summary>
    /// 获取大屏分页列表
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <param name="name">大屏名称过滤（可选）</param>
    /// <returns>大屏分页列表</returns>
    Task<ApiResponse<PagedResponse<DashboardDto>>> GetDashboardsAsync(PagedRequest request, string? name = null);

    /// <summary>
    /// 获取大屏详情
    /// </summary>
    /// <param name="dashboardId">大屏ID</param>
    /// <returns>大屏详情（包含组件列表）</returns>
    Task<ApiResponse<DashboardDetailDto>> GetDashboardByIdAsync(int dashboardId);

    /// <summary>
    /// 创建大屏
    /// </summary>
    /// <param name="request">创建大屏请求</param>
    /// <param name="createdBy">创建人ID</param>
    /// <returns>创建的大屏信息</returns>
    Task<ApiResponse<DashboardDto>> CreateDashboardAsync(CreateDashboardRequest request, int createdBy);

    /// <summary>
    /// 更新大屏
    /// </summary>
    /// <param name="dashboardId">大屏ID</param>
    /// <param name="request">更新大屏请求</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> UpdateDashboardAsync(int dashboardId, CreateDashboardRequest request);

    /// <summary>
    /// 删除大屏
    /// </summary>
    /// <param name="dashboardId">大屏ID</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> DeleteDashboardAsync(int dashboardId);

    #endregion

    #region 组件管理

    /// <summary>
    /// 添加组件
    /// </summary>
    /// <param name="dashboardId">大屏ID</param>
    /// <param name="request">创建组件请求</param>
    /// <returns>创建的组件信息</returns>
    Task<ApiResponse<DashboardWidgetDto>> AddWidgetAsync(int dashboardId, CreateWidgetRequest request);

    /// <summary>
    /// 更新组件
    /// </summary>
    /// <param name="dashboardId">大屏ID</param>
    /// <param name="widgetId">组件ID</param>
    /// <param name="request">更新组件请求</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> UpdateWidgetAsync(int dashboardId, int widgetId, CreateWidgetRequest request);

    /// <summary>
    /// 删除组件
    /// </summary>
    /// <param name="dashboardId">大屏ID</param>
    /// <param name="widgetId">组件ID</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> DeleteWidgetAsync(int dashboardId, int widgetId);

    /// <summary>
    /// 批量更新组件位置
    /// </summary>
    /// <param name="dashboardId">大屏ID</param>
    /// <param name="positions">组件位置列表</param>
    /// <returns>操作结果</returns>
    Task<ApiResponse> UpdateWidgetPositionsAsync(int dashboardId, List<WidgetPositionRequest> positions);

    #endregion

    #region 一键转换

    /// <summary>
    /// 从报表一键转换为大屏
    /// </summary>
    /// <param name="reportId">源报表ID</param>
    /// <param name="dashboardName">大屏名称（可选，默认使用报表名称）</param>
    /// <param name="createdBy">创建人ID</param>
    /// <returns>创建的大屏详情</returns>
    Task<ApiResponse<DashboardDetailDto>> ConvertFromReportAsync(int reportId, string? dashboardName, int createdBy);

    #endregion

    #region 数据获取

    /// <summary>
    /// 获取大屏数据（包含所有组件的数据）
    /// </summary>
    /// <param name="dashboardId">大屏ID</param>
    /// <returns>大屏数据（包含组件数据字典）</returns>
    Task<ApiResponse<DashboardDataDto>> GetDashboardDataAsync(int dashboardId);

    #endregion

    #region 公开访问

    /// <summary>
    /// 获取公开大屏详情（无需登录）
    /// </summary>
    /// <param name="dashboardId">大屏ID</param>
    /// <returns>大屏详情（仅限公开大屏）</returns>
    Task<ApiResponse<DashboardDetailDto>> GetPublicDashboardAsync(int dashboardId);

    /// <summary>
    /// 获取公开大屏数据（无需登录）
    /// </summary>
    /// <param name="dashboardId">大屏ID</param>
    /// <returns>大屏数据（仅限公开大屏）</returns>
    Task<ApiResponse<DashboardDataDto>> GetPublicDashboardDataAsync(int dashboardId);

    #endregion
}
