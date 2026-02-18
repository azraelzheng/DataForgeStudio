using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Domain.Interfaces;
using DataForgeStudio.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace DataForgeStudio.Data.Repositories;

/// <summary>
/// 用户仓储实现
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(DataForgeStudioDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<List<User>> GetRegularUsersAsync()
    {
        return await _dbSet
            .Where(u => u.IsSystem == false)
            .ToListAsync();
    }

    public async Task<(List<User> Items, int TotalCount)> GetPagedRegularUsersAsync(
        string? keyword,
        int pageIndex,
        int pageSize)
    {
        var query = _dbSet
            .Where(u => u.IsSystem == false)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u =>
                u.Username.Contains(keyword) ||
                (u.RealName != null && u.RealName.Contains(keyword)) ||
                (u.Email != null && u.Email.Contains(keyword)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<User?> GetUserWithRolesAsync(int userId)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User?> GetUserWithPermissionsAsync(int userId)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<bool> IsUsernameExistsAsync(string username, int? excludeUserId = null)
    {
        return await _dbSet
            .Where(u => u.Username == username)
            .Where(u => !excludeUserId.HasValue || u.UserId != excludeUserId.Value)
            .AnyAsync();
    }

    public async Task UpdateLoginInfoAsync(int userId, string ipAddress)
    {
        var user = await _dbSet.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginTime = DateTime.UtcNow;
            user.LastLoginIP = ipAddress;
            user.PasswordFailCount = 0;
            await _context.SaveChangesAsync();
        }
    }
}
