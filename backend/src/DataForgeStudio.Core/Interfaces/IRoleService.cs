using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 角色服务接口
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// 获取角色分页列表
    /// </summary>
    Task<ApiResponse<PagedResponse<RoleDto>>> GetRolesAsync(PagedRequest request, string? roleName = null);

    /// <summary>
    /// 获取所有角色
    /// </summary>
    Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync();

    /// <summary>
    /// 获取角色详情
    /// </summary>
    Task<ApiResponse<RoleDto>> GetRoleByIdAsync(int roleId);

    /// <summary>
    /// 创建角色
    /// </summary>
    Task<ApiResponse<RoleDto>> CreateRoleAsync(CreateRoleRequest request, int createdBy);

    /// <summary>
    /// 更新角色
    /// </summary>
    Task<ApiResponse> UpdateRoleAsync(int roleId, CreateRoleRequest request);

    /// <summary>
    /// 删除角色
    /// </summary>
    Task<ApiResponse> DeleteRoleAsync(int roleId);

    /// <summary>
    /// 分配权限
    /// </summary>
    Task<ApiResponse> AssignPermissionsAsync(int roleId, AssignPermissionsRequest request);
}
