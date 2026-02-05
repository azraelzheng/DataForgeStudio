using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace DataForgeStudio.Api.Middleware;

/// <summary>
/// 简单的速率限制中间件
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly IConfiguration _configuration;

    // IP地址 -> (请求次数, 最后请求时间)
    private readonly ConcurrentDictionary<string, (int count, DateTime lastRequest)> _requestCounts = new();
    private readonly ConcurrentDictionary<string, DateTime> _blockedUntil = new();

    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIp(context);
        var path = context.Request.Path.Value ?? "";

        // 检查是否被阻止
        if (_blockedUntil.TryGetValue(clientIp, out var blockedUntil))
        {
            if (DateTime.UtcNow < blockedUntil)
            {
                _logger.LogWarning($"Rate limit blocked for {clientIp} on {path}");
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.Headers.Add("Retry-After", ((int)(blockedUntil - DateTime.UtcNow).TotalSeconds).ToString());
                await context.Response.WriteAsync("Too many requests. Please try again later.");
                return;
            }
            else
            {
                // 解除阻止
                _blockedUntil.TryRemove(clientIp, out _);
            }
        }

        // 获取速率限制配置
        var limit = GetRateLimit(path);
        var period = GetPeriod(path);

        // 检查速率限制
        var key = $"{clientIp}:{path}";
        if (_requestCounts.TryGetValue(key, out var requestInfo))
        {
            var (count, lastRequest) = requestInfo;
            var timeSinceLastRequest = DateTime.UtcNow - lastRequest;

            // 如果超过时间窗口，重置计数
            if (timeSinceLastRequest > period)
            {
                _requestCounts[key] = (1, DateTime.UtcNow);
            }
            else
            {
                // 在时间窗口内，检查是否超过限制
                if (count >= limit)
                {
                    // 阻止请求
                    var blockDuration = TimeSpan.FromMinutes(15);
                    _blockedUntil[clientIp] = DateTime.UtcNow.Add(blockDuration);
                    _logger.LogWarning($"Rate limit exceeded for {clientIp} on {path}. Blocked for {blockDuration.TotalMinutes} minutes.");

                    context.Response.StatusCode = 429;
                    context.Response.Headers.Add("Retry-After", "900"); // 15 minutes
                    await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                    return;
                }
                else
                {
                    // 增加计数
                    _requestCounts[key] = (count + 1, lastRequest);
                }
            }
        }
        else
        {
            _requestCounts[key] = (1, DateTime.UtcNow);
        }

        await _next(context);
    }

    private string GetClientIp(HttpContext context)
    {
        // 尝试从 X-Forwarded-For 获取真实 IP
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        // 尝试从 X-Real-IP 获取
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            return realIp.ToString();
        }

        // 使用连接的远程 IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private int GetRateLimit(string path)
    {
        // 根据路径返回限制
        if (path.Contains("/api/auth/login") || path.Contains("/api/auth/force-password-change"))
            return 5; // 5次/15分钟
        if (path.Contains("/api/reports/test-query"))
            return 10; // 10次/5分钟
        return 100; // 通用限制
    }

    private TimeSpan GetPeriod(string path)
    {
        if (path.Contains("/api/reports/test-query"))
            return TimeSpan.FromMinutes(5);
        if (path.Contains("/api/auth/login") || path.Contains("/api/auth/force-password-change"))
            return TimeSpan.FromMinutes(15);
        return TimeSpan.FromMinutes(1); // 通用：每分钟
    }
}
