using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using DataForgeStudio.Core.Configuration;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Core.Validators;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Domain.Interfaces;
using DataForgeStudio.Shared.DTO;
using DataForgeStudio.Shared.Exceptions;
using DataForgeStudio.Shared.Utils;
using DataForgeStudio.Shared.Constants;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly SecurityOptions.JwtOptions _jwtOptions;

    public AuthenticationService(
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;

        // 从配置读取 JWT 设置（尝试新的 Security:Jwt 路径，然后回退到旧的 Jwt 路径）
        var secret = configuration["Security:Jwt:Secret"] ?? configuration["Jwt:Secret"];
        var issuer = configuration["Security:Jwt:Issuer"] ?? configuration["Jwt:Issuer"] ?? "DataForgeStudio";
        var audience = configuration["Security:Jwt:Audience"] ?? configuration["Jwt:Audience"] ?? "DataForgeStudio.Client";
        var expiration = configuration["Security:Jwt:ExpirationMinutes"] ?? configuration["Jwt:ExpirationMinutes"] ?? "1440";

        _jwtOptions = new SecurityOptions.JwtOptions
        {
            Secret = secret ?? throw new InvalidOperationException("JWT Secret 未配置"),
            Issuer = issuer,
            Audience = audience,
            ExpirationMinutes = int.Parse(expiration)
        };
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string ipAddress)
    {
        try
        {
            // 查找用户
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                throw new LoginFailedException("用户名或密码错误");
            }

            // 检查用户状态
            if (!user.IsActive)
            {
                throw new LoginFailedException("用户已被禁用");
            }

            if (user.IsLocked)
            {
                throw new LoginFailedException("账户已被锁定，请联系管理员");
            }

            // 验证密码
            if (!EncryptionHelper.VerifyPassword(request.Password, user.PasswordHash))
            {
                // 增加失败次数
                user.PasswordFailCount++;
                if (user.PasswordFailCount >= SystemConstants.MAX_LOGIN_FAILURE_COUNT)
                {
                    user.IsLocked = true;
                }

                await _userRepository.UpdateAsync(user);
                throw new LoginFailedException("用户名或密码错误");
            }

            // 检查是否需要强制修改密码
            if (user.MustChangePassword)
            {
                // 重置失败次数
                user.PasswordFailCount = 0;
                await _userRepository.UpdateAsync(user);

                return new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = "首次登录必须修改密码",
                    Data = new LoginResponse
                    {
                        Success = false,
                        RequiresPasswordChange = true,
                        UserId = user.UserId,
                        Token = string.Empty,
                        TokenType = "Bearer",
                        ExpiresIn = 0
                    },
                    ErrorCode = null,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
            }

            // 登录成功，重置失败次数
            user.PasswordFailCount = 0;
            await _userRepository.UpdateAsync(user);

            // 获取用户角色和权限
            var userWithRoles = await _userRepository.GetUserWithPermissionsAsync(user.UserId);

            var roles = userWithRoles?.UserRoles
                .Select(ur => ur.Role.RoleName)
                .ToList() ?? new List<string>();

            var permissions = userWithRoles?.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions
                    .Select(rp => rp.Permission.PermissionCode))
                .Distinct()
                .ToList() ?? new List<string>();

            // 生成 Token
            var token = GenerateJwtToken(user.UserId, user.Username);

            // 构建响应
            var response = new LoginResponse
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresIn = _jwtOptions.ExpirationMinutes * 60,
                UserInfo = new UserInfoDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    RealName = user.RealName,
                    Email = user.Email,
                    Roles = roles,
                    Permissions = permissions
                }
            };

            return ApiResponse<LoginResponse>.Ok(response, "登录成功");
        }
        catch (LoginFailedException ex)
        {
            return ApiResponse<LoginResponse>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return ApiResponse<LoginResponse>.Fail("登录失败: " + ex.Message);
        }
    }

    public string GenerateJwtToken(int userId, string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(SystemConstants.USER_ID_CLAIM, userId.ToString()),
            new Claim(SystemConstants.USERNAME_CLAIM, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidAudience = _jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserInfoDto?> GetCurrentUserAsync(int userId)
    {
        var user = await _userRepository.GetUserWithPermissionsAsync(userId);
        if (user == null)
        {
            return null;
        }

        return new UserInfoDto
        {
            UserId = user.UserId,
            Username = user.Username,
            RealName = user.RealName,
            Email = user.Email,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
            Permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.PermissionCode)
                .Distinct()
                .ToList()
        };
    }

    public async Task<ApiResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        try
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                return ApiResponse.Fail("两次输入的密码不一致");
            }

            // 验证新密码强度
            var passwordValidation = PasswordValidator.ValidatePassword(request.NewPassword);
            if (!passwordValidation.IsValid)
            {
                return ApiResponse.Fail(string.Join("; ", passwordValidation.Errors), "WEAK_PASSWORD");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("用户不存在");
            }

            // 验证旧密码
            if (!EncryptionHelper.VerifyPassword(request.OldPassword, user.PasswordHash))
            {
                return ApiResponse.Fail("旧密码错误");
            }

            // 更新密码
            user.PasswordHash = EncryptionHelper.HashPassword(request.NewPassword);
            user.MustChangePassword = false;

            await _userRepository.UpdateAsync(user);

            return ApiResponse.Ok("密码修改成功");
        }
        catch (NotFoundException ex)
        {
            return ApiResponse.Fail(ex.Message, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            return ApiResponse.Fail("密码修改失败: " + ex.Message);
        }
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        var user = await _userRepository.GetUserWithPermissionsAsync(userId);
        if (user == null)
        {
            return false;
        }

        // root 用户拥有所有权限
        if (user.IsSystem)
        {
            return true;
        }

        return user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Any(rp => rp.Permission.PermissionCode == permissionCode);
    }

    public async Task<ApiResponse<bool>> ForcePasswordChangeAsync(ForcePasswordChangeRequest request)
    {
        try
        {
            // 验证新密码
            if (request.NewPassword != request.ConfirmPassword)
            {
                return ApiResponse<bool>.Fail("两次输入的密码不一致");
            }

            // 验证新密码强度
            var passwordValidation = PasswordValidator.ValidatePassword(request.NewPassword);
            if (!passwordValidation.IsValid)
            {
                return ApiResponse<bool>.Fail(string.Join("; ", passwordValidation.Errors), "WEAK_PASSWORD");
            }

            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("用户不存在", "NOT_FOUND");
            }

            // 验证临时密码
            if (!EncryptionHelper.VerifyPassword(request.TemporaryPassword, user.PasswordHash))
            {
                return ApiResponse<bool>.Fail("临时密码错误");
            }

            // 更新密码
            user.PasswordHash = EncryptionHelper.HashPassword(request.NewPassword);
            user.MustChangePassword = false;
            user.PasswordFailCount = 0;

            await _userRepository.UpdateAsync(user);

            return ApiResponse<bool>.Ok(true, "密码修改成功，请重新登录");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.Fail("密码修改失败: " + ex.Message);
        }
    }
}
