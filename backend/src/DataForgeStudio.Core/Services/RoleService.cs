using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;
using DataForgeStudio.Shared.Exceptions;
using System.Text.Json;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 角色服务实现
/// </summary>
public class RoleService : IRoleService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly ILogger<RoleService> _logger;

    public RoleService(DataForgeStudioDbContext context, ILogger<RoleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResponse<RoleDto>>> GetRolesAsync(PagedRequest request, string? roleName = null)
    {
        // 显示所有角色（包括系统预置角色），用户可以看到但不能编辑/删除系统角色
        var query = _context.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            query = query.Where(r => r.RoleName.Contains(roleName));
        }

        var totalCount = await query.CountAsync();

        var roleIds = await query
            .OrderByDescending(r => r.CreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => r.RoleId)
            .ToListAsync();

        // 加载完整的角色数据（包含权限）
        var roles = await _context.Roles
            .Include(r => r.UserRoles)
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Where(r => roleIds.Contains(r.RoleId))
            .OrderByDescending(r => r.CreatedTime)
            .ToListAsync();

        // 手动映射到 DTO
        var roleDtos = roles.Select(r => new RoleDto
        {
            RoleId = r.RoleId,
            RoleName = r.RoleName,
            Description = r.Description,
            IsSystem = r.IsSystem,
            CreatedTime = r.CreatedTime,
            UserCount = r.UserRoles.Count,
            Permissions = r.RolePermissions
                .Select(rp => rp.Permission.PermissionCode)
                .ToList()
        }).ToList();

        var pagedResponse = new PagedResponse<RoleDto>(roleDtos, totalCount, request.PageIndex, request.PageSize);
        return ApiResponse<PagedResponse<RoleDto>>.Ok(pagedResponse);
    }

    public async Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync()
    {
        // 显示所有角色（包括系统预置角色），用于用户分配角色时选择
        var roles = await _context.Roles
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.RoleName)
            .Select(r => new RoleDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                IsSystem = r.IsSystem,
                CreatedTime = r.CreatedTime,
                UserCount = 0
            })
            .ToListAsync();

        return ApiResponse<List<RoleDto>>.Ok(roles);
    }

    public async Task<ApiResponse<RoleDto>> GetRoleByIdAsync(int roleId)
    {
        var role = await _context.Roles
            .Where(r => r.RoleId == roleId)
            .Select(r => new RoleDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                IsSystem = r.IsSystem,
                CreatedTime = r.CreatedTime,
                UserCount = 0
            })
            .FirstOrDefaultAsync();

        if (role == null)
        {
            return ApiResponse<RoleDto>.Fail("角色不存在", "NOT_FOUND");
        }

        return ApiResponse<RoleDto>.Ok(role);
    }

    public async Task<ApiResponse<RoleDto>> CreateRoleAsync(CreateRoleRequest request, int createdBy)
    {
        var exists = await _context.Roles.AnyAsync(r => r.RoleName == request.RoleName);
        if (exists)
        {
            return ApiResponse<RoleDto>.Fail("角色名称已存在", "DUPLICATE_ROLENAME");
        }

        // 生成角色编码
        var code = $"ROLE_{DateTime.UtcNow:yyyyMMddHHmmss}";

        var role = new Role
        {
            RoleName = request.RoleName,
            RoleCode = code,
            Description = request.Description,
            IsSystem = false,
            CreatedBy = createdBy,
            CreatedTime = DateTime.UtcNow
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var roleDto = new RoleDto
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            Description = role.Description,
            IsSystem = role.IsSystem,
            CreatedTime = role.CreatedTime
        };

        return ApiResponse<RoleDto>.Ok(roleDto, "角色创建成功");
    }

    public async Task<ApiResponse> UpdateRoleAsync(int roleId, CreateRoleRequest request)
    {
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleId == roleId && !r.IsSystem);

        if (role == null)
        {
            return ApiResponse.Fail("角色不存在或系统角色不可修改", "NOT_FOUND");
        }

        role.RoleName = request.RoleName;
        role.Description = request.Description;
        role.UpdatedTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("角色更新成功");
    }

    public async Task<ApiResponse> DeleteRoleAsync(int roleId)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.RoleId == roleId && !r.IsSystem);

        if (role == null)
        {
            return ApiResponse.Fail("角色不存在或系统角色不可删除", "NOT_FOUND");
        }

        // 检查是否有用户使用此角色
        var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
        if (hasUsers)
        {
            return ApiResponse.Fail("该角色下还有用户，无法删除", "ROLE_IN_USE");
        }

        // 先删除角色权限关联
        if (role.RolePermissions.Any())
        {
            _context.RolePermissions.RemoveRange(role.RolePermissions);
        }

        // 删除角色
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"角色删除成功: RoleId={roleId}, RoleName={role.RoleName}");
        return ApiResponse.Ok("角色删除成功");
    }

    public async Task<ApiResponse> AssignPermissionsAsync(int roleId, AssignPermissionsRequest request)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.RoleId == roleId);

        if (role == null)
        {
            return ApiResponse.Fail("角色不存在", "NOT_FOUND");
        }

        // 删除现有的角色权限关联
        _context.RolePermissions.RemoveRange(role.RolePermissions);

        // 获取所有权限
        var allPermissions = await _context.Permissions.ToListAsync();

        // 添加新的角色权限关联
        foreach (var permissionCode in request.Permissions)
        {
            var permission = allPermissions.FirstOrDefault(p => p.PermissionCode == permissionCode);
            if (permission != null)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permission.PermissionId
                });
            }
        }

        role.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ApiResponse.Ok("权限分配成功");
    }
}
