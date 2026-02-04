using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// 用户登录
    /// </summary>
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string ipAddress);

    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    string GenerateJwtToken(int userId, string username);

    /// <summary>
    /// 验证 Token
    /// </summary>
    bool ValidateToken(string token);

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<UserInfoDto?> GetCurrentUserAsync(int userId);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task<ApiResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);

    /// <summary>
    /// 检查用户是否有权限
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permissionCode);
}
