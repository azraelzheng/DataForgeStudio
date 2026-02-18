using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Core.Validators;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;
using DataForgeStudio.Shared.Utils;
using DataForgeStudio.Shared.Exceptions;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 用户服务实现
/// </summary>
public class UserService : IUserService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(DataForgeStudioDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResponse<UserDto>>> GetUsersAsync(PagedRequest request, string? username = null, bool? isActive = null)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsSystem);

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(u => u.Username.Contains(username));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                RealName = u.RealName,
                Email = u.Email,
                Phone = u.Phone,
                IsActive = u.IsActive,
                LastLoginTime = u.LastLoginTime,
                CreatedTime = u.CreatedTime,
                Roles = u.UserRoles.Select(ur => new RoleDto
                {
                    RoleId = ur.Role.RoleId,
                    RoleName = ur.Role.RoleName,
                    Description = ur.Role.Description,
                    IsSystem = ur.Role.IsSystem
                }).ToList()
            })
            .ToListAsync();

        // 检查每个用户是否有操作日志记录
        var userIds = users.Select(u => u.UserId).ToList();
        var usersWithLogs = await _context.OperationLogs
            .Where(log => log.UserId.HasValue && userIds.Contains(log.UserId.Value))
            .Select(log => log.UserId.Value)
            .Distinct()
            .ToListAsync();

        foreach (var user in users)
        {
            user.HasOperationLogs = usersWithLogs.Contains(user.UserId);
        }

        var pagedResponse = new PagedResponse<UserDto>(users, totalCount, request.PageIndex, request.PageSize);
        return ApiResponse<PagedResponse<UserDto>>.Ok(pagedResponse);
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.UserId == userId && !u.IsSystem)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                RealName = u.RealName,
                Email = u.Email,
                Phone = u.Phone,
                IsActive = u.IsActive,
                LastLoginTime = u.LastLoginTime,
                CreatedTime = u.CreatedTime,
                Roles = u.UserRoles.Select(ur => new RoleDto
                {
                    RoleId = ur.Role.RoleId,
                    RoleName = ur.Role.RoleName,
                    Description = ur.Role.Description,
                    IsSystem = ur.Role.IsSystem
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return ApiResponse<UserDto>.Fail("用户不存在", "NOT_FOUND");
        }

        return ApiResponse<UserDto>.Ok(user);
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request, int createdBy)
    {
        // 验证密码强度
        var passwordValidation = PasswordValidator.ValidatePassword(request.Password);
        if (!passwordValidation.IsValid)
        {
            return ApiResponse<UserDto>.Fail(string.Join("; ", passwordValidation.Errors), "WEAK_PASSWORD");
        }

        // 检查用户名是否存在
        var exists = await _context.Users.AnyAsync(u => u.Username == request.Username);
        if (exists)
        {
            return ApiResponse<UserDto>.Fail("用户名已存在", "DUPLICATE_USERNAME");
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = EncryptionHelper.HashPassword(request.Password ?? throw new ArgumentException("密码不能为空")),
            RealName = request.RealName,
            Email = request.Email,
            Phone = request.Phone,
            IsActive = request.IsActive,
            CreatedBy = createdBy,
            CreatedTime = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userDto = new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            RealName = user.RealName,
            Email = user.Email,
            Phone = user.Phone,
            IsActive = user.IsActive,
            CreatedTime = user.CreatedTime,
            Roles = new List<RoleDto>()
        };

        return ApiResponse<UserDto>.Ok(userDto, "用户创建成功");
    }

    public async Task<ApiResponse> UpdateUserAsync(int userId, CreateUserRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return ApiResponse.Fail("用户不存在", "NOT_FOUND");
        }

        // 禁止修改 root 用户
        if (user.IsSystem)
        {
            return ApiResponse.Fail("root 用户是系统管理员，不能被修改", "FORBIDDEN");
        }

        user.RealName = request.RealName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.IsActive = request.IsActive;
        user.UpdatedTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("用户更新成功");
    }

    public async Task<ApiResponse> DeleteUserAsync(int userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return ApiResponse.Fail("用户不存在", "NOT_FOUND");
        }

        // 禁止删除 root 用户
        if (user.IsSystem)
        {
            return ApiResponse.Fail("root 用户是系统管理员，不能被删除", "FORBIDDEN");
        }

        // 检查是否有操作日志记录
        var hasOperationLogs = await _context.OperationLogs
            .AnyAsync(log => log.UserId == userId);

        if (hasOperationLogs)
        {
            return ApiResponse.Fail("该用户有操作记录，不能删除，只能停用", "FORBIDDEN");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return ApiResponse.Ok("用户删除成功");
    }

    public async Task<ApiResponse> ResetPasswordAsync(int userId, ResetPasswordRequest request)
    {
        // 验证新密码强度
        var passwordValidation = PasswordValidator.ValidatePassword(request.NewPassword);
        if (!passwordValidation.IsValid)
        {
            return ApiResponse.Fail(string.Join("; ", passwordValidation.Errors), "WEAK_PASSWORD");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return ApiResponse.Fail("用户不存在", "NOT_FOUND");
        }

        // 禁止重置 root 用户密码
        if (user.IsSystem)
        {
            return ApiResponse.Fail("root 用户密码不能通过此方式重置", "FORBIDDEN");
        }

        user.PasswordHash = EncryptionHelper.HashPassword(request.NewPassword);
        user.MustChangePassword = true;
        user.UpdatedTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("密码重置成功");
    }

    public async Task<ApiResponse> AssignRolesAsync(int userId, AssignRolesRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return ApiResponse.Fail("用户不存在", "NOT_FOUND");
        }

        // root 用户无需分配角色，已拥有所有权限
        if (user.IsSystem)
        {
            return ApiResponse.Fail("root 用户拥有所有权限，无需分配角色", "FORBIDDEN");
        }

        // 删除现有角色
        _context.UserRoles.RemoveRange(user.UserRoles);

        // 添加新角色
        foreach (var roleId in request.RoleIds)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role != null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId
                });
            }
        }

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("角色分配成功");
    }
}
