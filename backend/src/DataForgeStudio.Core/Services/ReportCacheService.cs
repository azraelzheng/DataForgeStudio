using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Core.Interfaces;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 报表缓存服务实现 - 使用 IMemoryCache
/// </summary>
public class ReportCacheService : IReportCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ReportCacheService> _logger;

    public ReportCacheService(IMemoryCache cache, ILogger<ReportCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("缓存命中: {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("缓存未命中: {Key}", key);
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(5),
            Size = 1 // 设置缓存项大小
        };

        _cache.Set(key, value, options);
        _logger.LogDebug("缓存已设置: {Key}, 过期时间: {Expiration}分钟",
            key,
            (absoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(5)).TotalMinutes);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        _logger.LogDebug("缓存已移除: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // MemoryCache 不支持按前缀删除，这里只记录日志
        // 实际使用中可以考虑维护一个键列表或使用 IDistributedCache
        _logger.LogDebug("请求清除前缀为 {Prefix} 的缓存（当前不支持）", prefix);
        return Task.CompletedTask;
    }
}
