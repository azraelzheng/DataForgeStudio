using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 许可证服务接口
/// </summary>
public interface ILicenseService
{
    /// <summary>
    /// 获取许可证信息
    /// </summary>
    Task<ApiResponse<LicenseInfoDto>> GetLicenseAsync();

    /// <summary>
    /// 激活许可证
    /// </summary>
    Task<ApiResponse<LicenseInfoDto>> ActivateLicenseAsync(ActivateLicenseRequest request, string? ipAddress);

    /// <summary>
    /// 验证许可证
    /// </summary>
    /// <param name="forceRefresh">是否强制刷新缓存</param>
    Task<ApiResponse<LicenseValidationResponse>> ValidateLicenseAsync(bool forceRefresh = false);

    /// <summary>
    /// 生成试用许可证
    /// </summary>
    Task<ApiResponse<LicenseInfoDto>> GenerateTrialLicenseAsync();

    /// <summary>
    /// 获取许可证使用统计（用户数、报表数、数据源数）
    /// </summary>
    Task<ApiResponse<LicenseUsageStatsDto>> GetUsageStatsAsync();

    /// <summary>
    /// 检查是否可以创建新的用户
    /// </summary>
    /// <returns>如果可以创建返回成功响应，否则返回失败响应并包含限制信息</returns>
    Task<ApiResponse> CheckUserLimitAsync();

    /// <summary>
    /// 检查是否可以创建新的报表
    /// </summary>
    /// <returns>如果可以创建返回成功响应，否则返回失败响应并包含限制信息</returns>
    Task<ApiResponse> CheckReportLimitAsync();

    /// <summary>
    /// 检查是否可以创建新的数据源
    /// </summary>
    /// <returns>如果可以创建返回成功响应，否则返回失败响应并包含限制信息</returns>
    Task<ApiResponse> CheckDataSourceLimitAsync();

    /// <summary>
    /// 检查是否可以创建新的大屏
    /// </summary>
    /// <returns>如果可以创建返回成功响应，否则返回失败响应并包含限制信息</returns>
    Task<ApiResponse> CheckDashboardLimitAsync();
}
