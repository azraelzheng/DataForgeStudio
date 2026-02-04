using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 角色管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// 获取角色列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponse<PagedResponse<RoleDto>>> GetRoles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? roleName = null)
    {
        var request = new PagedRequest { PageIndex = page, PageSize = pageSize };
        return await _roleService.GetRolesAsync(request, roleName);
    }

    /// <summary>
    /// 获取所有角色
    /// </summary>
    [HttpGet("all")]
    public async Task<ApiResponse<List<RoleDto>>> GetAllRoles()
    {
        return await _roleService.GetAllRolesAsync();
    }

    /// <summary>
    /// 获取角色详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<RoleDto>> GetRole(int id)
    {
        return await _roleService.GetRoleByIdAsync(id);
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<RoleDto>> CreateRole([FromBody] CreateRoleRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return ApiResponse<RoleDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _roleService.CreateRoleAsync(request, userId);
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse> UpdateRole(int id, [FromBody] CreateRoleRequest request)
    {
        return await _roleService.UpdateRoleAsync(id, request);
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse> DeleteRole(int id)
    {
        return await _roleService.DeleteRoleAsync(id);
    }

    /// <summary>
    /// 分配权限
    /// </summary>
    [HttpPost("{id}/permissions")]
    public async Task<ApiResponse> AssignPermissions(int id, [FromBody] AssignPermissionsRequest request)
    {
        return await _roleService.AssignPermissionsAsync(id, request);
    }
}
