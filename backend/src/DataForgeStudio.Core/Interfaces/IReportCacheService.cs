namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 报表缓存服务接口
/// </summary>
public interface IReportCacheService
{
    /// <summary>
    /// 获取缓存值
    /// </summary>
    Task<T?> GetAsync<T>(string key, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置缓存值
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除缓存值
    /// </summary>
    Task RemoveAsync(string key, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// 按前缀移除缓存值
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, System.Threading.CancellationToken cancellationToken = default);
}
