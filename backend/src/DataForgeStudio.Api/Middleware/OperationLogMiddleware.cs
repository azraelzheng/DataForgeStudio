using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Data.Data;

namespace DataForgeStudio.Api.Middleware;

/// <summary>
/// 操作日志记录中间件
/// </summary>
public class OperationLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OperationLogMiddleware> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    // 不需要记录日志的路径（精确匹配）
    private static readonly string[] ExcludePaths = new[]
    {
        "/api/auth/login",
        "/health",
        "/api"
    };

    // 只记录日志的 HTTP 方法
    private static readonly string[] LogMethods = new[] { "POST", "PUT", "DELETE" };

    public OperationLogMiddleware(
        RequestDelegate next,
        ILogger<OperationLogMiddleware> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        // 调试日志：记录所有请求
        _logger.LogInformation("请求: {Method} {Path}", method, path);

        // 跳过不需要记录的路径（精确匹配）
        if (ExcludePaths.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation("跳过记录日志（精确匹配）: {Path}", path);
            await _next(context);
            return;
        }

        // 跳过日志管理相关的操作（不记录日志管理的操作）
        if (path.StartsWith("/api/system/logs", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("跳过记录日志（日志管理操作）: {Path}", path);
            await _next(context);
            return;
        }

        // 只记录 POST、PUT、DELETE 请求
        if (!LogMethods.Contains(method))
        {
            _logger.LogInformation("跳过非写入操作: {Method} {Path}", method, path);
            await _next(context);
            return;
        }

        _logger.LogInformation("准备记录操作日志: {Method} {Path}", method, path);
        var stopwatch = Stopwatch.StartNew();

        // 启用请求体缓冲，以便读取请求内容
        context.Request.EnableBuffering();

        // 读取原始请求体用于提取资源名称
        string? requestBody = null;
        string? resourceName = null;
        string? resourceId = null;

        // 对于 DELETE 请求，在删除前查询数据库获取资源名称
        if (method == "DELETE")
        {
            resourceId = ExtractResourceIdFromPath(path);
            if (!string.IsNullOrEmpty(resourceId) && int.TryParse(resourceId, out int id))
            {
                resourceName = await GetResourceNameBeforeDeletion(path, id);
            }
        }
        // 对于 POST 和 PUT 请求，从请求体中提取资源名称
        else if ((method == "POST" || method == "PUT") && context.Request.ContentLength > 0 && context.Request.ContentLength < 100000)
        {
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: System.Text.Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // 从请求体中提取资源名称
            resourceName = ExtractResourceName(requestBody, path);
        }

        // 先提取所有需要的信息（在请求完成前）
        var username = context.User?.Identity?.Name ?? "Anonymous";
        var userIdClaim = context.User?.FindFirst("UserId")?.Value;
        var userId = int.TryParse(userIdClaim, out var uid) ? uid : (int?)null;
        var module = GetModuleFromPath(path);
        var action = GetActionFromMethod(method, path);
        var ip = GetClientIpAddress(context);
        var requestMethod = method;
        var requestUrl = path;

        try
        {
            await _next(context);
            stopwatch.Stop();

            _logger.LogInformation("请求完成: {Method} {Path} - Status: {StatusCode}", method, path, context.Response.StatusCode);

            // 生成描述
            var description = GetDescription(action, module, path, resourceName ?? resourceId);

            // 记录成功的请求
            if (context.Response.StatusCode < 400)
            {
                // 异步记录日志（但不使用 Task.Run，避免 HttpContext 释放问题）
                try
                {
                    _logger.LogInformation("开始记录操作日志...");

                    using var scope = _serviceScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataForgeStudioDbContext>();

                    var log = new Domain.Entities.OperationLog
                    {
                        Username = username,
                        UserId = userId,
                        Action = action,
                        Module = module,
                        Description = description,
                        IpAddress = ip,
                        RequestUrl = requestUrl,
                        RequestMethod = requestMethod,
                        Duration = (int)stopwatch.ElapsedMilliseconds,
                        IsSuccess = context.Response.StatusCode < 400,
                        CreatedTime = DateTime.Now  // 使用本地时间
                    };

                    dbContext.OperationLogs.Add(log);
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation("操作日志已记录: {Username} - {Action} - {Module}", username, action, module);
                }
                catch (Exception ex)
                {
                    // 记录日志失败不应该影响业务流程
                    _logger.LogError(ex, "记录操作日志失败");
                }
            }
            else
            {
                _logger.LogWarning("请求失败，不记录日志: {StatusCode}", context.Response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "请求处理异常: {Path}", path);
            throw;
        }
    }

    private string GetModuleFromPath(string path)
    {
        if (path.Contains("/users", StringComparison.OrdinalIgnoreCase)) return "User";
        if (path.Contains("/roles", StringComparison.OrdinalIgnoreCase)) return "Role";
        if (path.Contains("/datasources", StringComparison.OrdinalIgnoreCase)) return "DataSource";
        if (path.Contains("/reports", StringComparison.OrdinalIgnoreCase)) return "Report";
        if (path.Contains("/license", StringComparison.OrdinalIgnoreCase)) return "License";
        if (path.Contains("/system", StringComparison.OrdinalIgnoreCase)) return "System";
        return "Other";
    }

    private string GetActionFromMethod(string method, string path)
    {
        return method switch
        {
            "POST" when path.EndsWith("/toggle-active") => "Toggle",
            "POST" when path.Contains("/databases") => "GetDatabases",
            "POST" when path.Contains("/test") => "TestConnection",
            "POST" => "Create",
            "PUT" => "Update",
            "DELETE" => "Delete",
            "PATCH" => "Modify",
            _ => "Unknown"
        };
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ip))
        {
            return ip.Split(',')[0].Trim();
        }

        ip = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ip))
        {
            return ip;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string GetDescription(string action, string module, string path, string? resourceName = null)
    {
        // 根据模块和操作生成友好的中文描述
        var moduleText = GetModuleText(module);
        var actionText = GetActionText(action);

        return action switch
        {
            "Toggle" => !string.IsNullOrEmpty(resourceName) ? $"{moduleText}切换状态: {resourceName}" : $"{moduleText}切换状态",
            "TestConnection" => !string.IsNullOrEmpty(resourceName) ? $"测试数据源连接: {resourceName}" : "测试数据源连接",
            "GetDatabases" => "获取数据库列表",
            _ => string.IsNullOrEmpty(resourceName) ? $"{moduleText}{actionText}" : $"{moduleText}{actionText}: {resourceName}"
        };
    }

    private string? ExtractResourceName(string requestBody, string path)
    {
        if (string.IsNullOrWhiteSpace(requestBody))
            return null;

        try
        {
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(requestBody);
            var root = jsonDoc.RootElement;

            // 根据路径提取不同的字段名称
            if (path.Contains("/users", StringComparison.OrdinalIgnoreCase))
            {
                return root.TryGetProperty("username", out var username) ? username.GetString() : null;
            }
            else if (path.Contains("/roles", StringComparison.OrdinalIgnoreCase))
            {
                return root.TryGetProperty("roleName", out var roleName) ? roleName.GetString() : null;
            }
            else if (path.Contains("/datasources", StringComparison.OrdinalIgnoreCase))
            {
                return root.TryGetProperty("name", out var name) ? name.GetString() : null;
            }
            else if (path.Contains("/reports", StringComparison.OrdinalIgnoreCase))
            {
                return root.TryGetProperty("reportName", out var reportName) ? reportName.GetString() : null;
            }
            else if (path.Contains("/license", StringComparison.OrdinalIgnoreCase))
            {
                return "许可证文件";
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private string? ExtractResourceIdFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 2)
        {
            var lastSegment = segments[^1];
            // 检查是否是数字ID
            if (int.TryParse(lastSegment, out _))
            {
                return lastSegment;
            }
        }
        return null;
    }

    private string GetModuleText(string module)
    {
        return module switch
        {
            "User" => "用户",
            "Role" => "角色",
            "DataSource" => "数据源",
            "Report" => "报表",
            "License" => "许可证",
            "System" => "系统",
            _ => ""
        };
    }

    private string GetActionText(string action)
    {
        return action switch
        {
            "Create" => "创建",
            "Update" => "更新",
            "Delete" => "删除",
            "Modify" => "修改",
            _ => ""
        };
    }

    /// <summary>
    /// 在删除操作前查询资源名称
    /// </summary>
    private async Task<string?> GetResourceNameBeforeDeletion(string path, int resourceId)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataForgeStudioDbContext>();

            if (path.Contains("/users", StringComparison.OrdinalIgnoreCase))
            {
                var user = await dbContext.Users.FindAsync(resourceId);
                return user?.Username;
            }
            else if (path.Contains("/roles", StringComparison.OrdinalIgnoreCase))
            {
                var role = await dbContext.Roles.FindAsync(resourceId);
                return role?.RoleName;
            }
            else if (path.Contains("/datasources", StringComparison.OrdinalIgnoreCase))
            {
                var dataSource = await dbContext.DataSources.FindAsync(resourceId);
                return dataSource?.DataSourceName;
            }
            else if (path.Contains("/reports", StringComparison.OrdinalIgnoreCase))
            {
                var report = await dbContext.Reports.FindAsync(resourceId);
                return report?.ReportName;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
