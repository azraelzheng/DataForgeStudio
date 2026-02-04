using System.Linq.Expressions;

namespace DataForgeStudio.Domain.Interfaces;

/// <summary>
/// 仓储接口基础
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// 根据 ID 获取实体
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    Task<List<T>> GetAllAsync();

    /// <summary>
    /// 根据条件查找实体
    /// </summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 根据条件查找实体列表
    /// </summary>
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 分页查询
    /// </summary>
    Task<(List<T> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>? orderBy,
        bool ascending = true,
        int pageIndex = 1,
        int pageSize = 20);

    /// <summary>
    /// 添加实体
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// 批量添加实体
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// 更新实体
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// 批量更新实体
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// 删除实体
    /// </summary>
    Task DeleteAsync(T entity);

    /// <summary>
    /// 批量删除实体
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 获取实体数量
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
}
