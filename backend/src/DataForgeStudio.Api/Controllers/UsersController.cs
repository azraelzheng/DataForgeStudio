using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 用户管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponse<PagedResponse<UserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? username = null,
        [FromQuery] bool? isActive = null)
    {
        var request = new PagedRequest { PageIndex = page, PageSize = pageSize };
        return await _userService.GetUsersAsync(request, username, isActive);
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<UserDto>> GetUser(int id)
    {
        return await _userService.GetUserByIdAsync(id);
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return ApiResponse<UserDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _userService.CreateUserAsync(request, userId);
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse> UpdateUser(int id, [FromBody] CreateUserRequest request)
    {
        return await _userService.UpdateUserAsync(id, request);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse> DeleteUser(int id)
    {
        return await _userService.DeleteUserAsync(id);
    }

    /// <summary>
    /// 重置密码
    /// </summary>
    [HttpPost("{id}/reset-password")]
    public async Task<ApiResponse> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        return await _userService.ResetPasswordAsync(id, request);
    }

    /// <summary>
    /// 分配角色
    /// </summary>
    [HttpPost("{id}/roles")]
    public async Task<ApiResponse> AssignRoles(int id, [FromBody] AssignRolesRequest request)
    {
        return await _userService.AssignRolesAsync(id, request);
    }
}
