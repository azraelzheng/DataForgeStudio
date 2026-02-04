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
}
