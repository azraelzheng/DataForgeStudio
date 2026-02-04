using DataForgeStudio.Domain.Entities;

namespace DataForgeStudio.Domain.Interfaces;

/// <summary>
/// 用户仓储接口
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// 获取普通用户列表（不包括系统用户）
    /// </summary>
    Task<List<User>> GetRegularUsersAsync();

    /// <summary>
    /// 分页获取普通用户列表
    /// </summary>
    Task<(List<User> Items, int TotalCount)> GetPagedRegularUsersAsync(
        string? keyword,
        int pageIndex,
        int pageSize);

    /// <summary>
    /// 获取用户及其角色
    /// </summary>
    Task<User?> GetUserWithRolesAsync(int userId);

    /// <summary>
    /// 获取用户及其权限
    /// </summary>
    Task<User?> GetUserWithPermissionsAsync(int userId);

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    Task<bool> IsUsernameExistsAsync(string username, int? excludeUserId = null);

    /// <summary>
    /// 更新登录信息
    /// </summary>
    Task UpdateLoginInfoAsync(int userId, string ipAddress);
}
