using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 用户服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 获取用户分页列表
    /// </summary>
    Task<ApiResponse<PagedResponse<UserDto>>> GetUsersAsync(PagedRequest request, string? username = null, bool? isActive = null);

    /// <summary>
    /// 获取用户详情
    /// </summary>
    Task<ApiResponse<UserDto>> GetUserByIdAsync(int userId);

    /// <summary>
    /// 创建用户
    /// </summary>
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request, int createdBy);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task<ApiResponse> UpdateUserAsync(int userId, CreateUserRequest request);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task<ApiResponse> DeleteUserAsync(int userId);

    /// <summary>
    /// 重置密码
    /// </summary>
    Task<ApiResponse> ResetPasswordAsync(int userId, ResetPasswordRequest request);

    /// <summary>
    /// 分配角色
    /// </summary>
    Task<ApiResponse> AssignRolesAsync(int userId, AssignRolesRequest request);
}
