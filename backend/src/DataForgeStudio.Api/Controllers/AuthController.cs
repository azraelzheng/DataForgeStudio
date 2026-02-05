using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authenticationService,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求，包含用户名和密码</param>
    /// <returns>登录结果，包含 JWT Token 和用户信息</returns>
    /// <remarks>
    /// 示例请求:
    ///
    ///     POST /api/auth/login
    ///     {
    ///       "username": "admin",
    ///       "password": "admin123"
    ///     }
    ///
    /// 示例响应:
    ///
    ///     {
    ///       "success": true,
    ///       "message": "登录成功",
    ///       "data": {
    ///         "token": "eyJhbGciOiJIUzI1NiJ9...",
    ///         "tokenType": "Bearer",
    ///         "expiresIn": 3600,
    ///         "userInfo": { "userId": 1, "username": "admin" }
    ///       }
    ///     }
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ApiResponse<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        return await _authenticationService.LoginAsync(request, ipAddress);
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    /// <returns>当前用户的详细信息，包括角色和权限</returns>
    /// <response code="200">返回用户信息</response>
    /// <response code="401">未登录或 Token 无效</response>
    [HttpGet("current-user")]
    [Authorize]
    public async Task<ApiResponse<UserInfoDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return ApiResponse<UserInfoDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        var user = await _authenticationService.GetCurrentUserAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserInfoDto>.Fail("用户不存在", "NOT_FOUND");
        }

        return ApiResponse<UserInfoDto>.Ok(user);
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ApiResponse> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return ApiResponse.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _authenticationService.ChangePasswordAsync(userId, request);
    }

    /// <summary>
    /// 验证 Token
    /// </summary>
    [HttpPost("validate-token")]
    [AllowAnonymous]
    public ApiResponse ValidateToken([FromBody] TokenRequest request)
    {
        var isValid = _authenticationService.ValidateToken(request.Token);
        return isValid
            ? ApiResponse.Ok("Token 有效")
            : ApiResponse.Fail("Token 无效", "INVALID_TOKEN");
    }

    /// <summary>
    /// 强制修改密码（首次登录）
    /// </summary>
    [HttpPost("force-password-change")]
    [AllowAnonymous]
    public async Task<ApiResponse<bool>> ForcePasswordChange([FromBody] ForcePasswordChangeRequest request)
    {
        return await _authenticationService.ForcePasswordChangeAsync(request);
    }
}

/// <summary>
/// Token 请求
/// </summary>
public class TokenRequest
{
    public string Token { get; set; } = string.Empty;
}
