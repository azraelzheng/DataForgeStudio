# 生产环境部署问题修复计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复生产环境部署后发现的3个问题：前端启动慢、按钮无法点击、数据库连接失败

**Architecture:**
- 问题2：WPF UI 属性必须在 UI 线程更新，当前 `RefreshWebServiceStatus()` 在后台线程执行导致状态不更新
- 问题3：数据库连接字符串或认证配置问题，需要增强错误诊断
- 问题1：Nginx 启动优化，减少冷启动延迟

**Tech Stack:** ASP.NET Core 8.0, WPF, Nginx, SQL Server

---

## 问题根因分析

### 问题1：前端服务启动时间比开发版本长

| 假设 | 可能性 | 证据 |
|------|--------|------|
| Nginx 冷启动延迟 | 高 | 生产环境首次启动需要加载完整配置 |
| 前端资源未优化 | 中 | Vite 构建配置可能需要调整 |
| 代理连接建立延迟 | 中 | Nginx 到后端的首次连接需要时间 |

### 问题2：Nginx模式下启动后停止/重启按钮仍不能点击

| 假设 | 可能性 | 证据 |
|------|--------|------|
| **UI 属性跨线程更新失败** | **高** | `RefreshStatusAsync()` 使用 `Task.Run()` 在后台线程修改 `IsWebRunning` 等属性，WPF 要求 UI 属性在 UI 线程更新 |
| `IsWebOperating` 未重置 | 低 | `finally` 块应该保证执行 |
| Command 状态未刷新 | 低 | 已添加 `CommandManager.InvalidateRequerySuggested()` |

**关键代码位置：** `ServiceControlViewModel.cs:185-202`
```csharp
private async Task RefreshStatusAsync()
{
    await Task.Run(() =>
    {
        RefreshAppServiceStatus();   // 后台线程修改 UI 属性！
        RefreshWebServiceStatus();   // 后台线程修改 UI 属性！
    });
}
```

### 问题3：登录失败 - 数据库重试次数超限

| 假设 | 可能性 | 证据 |
|------|--------|------|
| **Windows 认证权限不足** | **高** | IIS/服务以 NETWORK SERVICE 或 ApplicationPoolIdentity 运行，可能无权访问 SQL Server |
| 连接字符串格式错误 | 中 | Configurator 生成的格式可能与运行时不匹配 |
| SQL Server 服务未运行 | 低 | 安装时会验证连接 |
| 防火墙阻止 | 低 | 本地连接通常不受影响 |

**关键代码位置：** `Configurator/Program.cs:465-485`

---

## 修复任务

### Task 1: 修复 UI 属性跨线程更新问题（问题2 - 高优先级）

**Files:**
- Modify: `backend/tools/DeployManager/ViewModels/ServiceControlViewModel.cs:185-251`

**Step 1: 理解问题**

当前代码在后台线程修改 UI 绑定属性，导致 WPF 绑定不更新：
```csharp
// 问题代码
private async Task RefreshStatusAsync()
{
    await Task.Run(() =>
    {
        RefreshAppServiceStatus();  // 后台线程
        RefreshWebServiceStatus();  // 后台线程
    });
}
```

**Step 2: 修改 RefreshStatusAsync 方法**

将属性更新操作调度到 UI 线程：

```csharp
/// <summary>
/// 刷新服务状态
/// </summary>
private async Task RefreshStatusAsync()
{
    try
    {
        // 在后台线程获取状态
        var (appStatus, webStatus) = await Task.Run(() =>
        {
            var appStatus = GetAppServiceStatus();
            var webStatus = GetWebServiceStatus();
            return (appStatus, webStatus);
        });

        // 在 UI 线程更新属性
        UpdateAppServiceStatus(appStatus);
        UpdateWebServiceStatus(webStatus);
    }
    catch (Exception)
    {
        // 忽略刷新错误
    }
}

/// <summary>
/// 获取后端服务状态（可在后台线程调用）
/// </summary>
private (ServiceStatus status, ProcessInfo? processInfo) GetAppServiceStatus()
{
    try
    {
        var status = _appServiceManager.GetStatus();
        ProcessInfo? processInfo = null;

        if (status == ServiceStatus.Running)
        {
            var processes = Process.GetProcessesByName("DataForgeStudio.Api");
            if (processes.Length > 0)
            {
                try
                {
                    processInfo = new ProcessInfo
                    {
                        StartTime = processes[0].StartTime,
                        MemoryMB = processes[0].WorkingSet64 / 1024.0 / 1024.0
                    };
                }
                catch { }
                foreach (var p in processes) p.Dispose();
            }
        }

        return (status, processInfo);
    }
    catch
    {
        return (ServiceStatus.Unknown, null);
    }
}

/// <summary>
/// 获取前端服务状态（可在后台线程调用）
/// </summary>
private ServiceStatus GetWebServiceStatus()
{
    try
    {
        return _webServiceManager.GetStatus();
    }
    catch
    {
        return ServiceStatus.Unknown;
    }
}

/// <summary>
/// 更新后端服务状态（必须在 UI 线程调用）
/// </summary>
private void UpdateAppServiceStatus((ServiceStatus status, ProcessInfo? processInfo) data)
{
    IsAppRunning = data.status == ServiceStatus.Running;
    AppStatusText = data.status switch
    {
        ServiceStatus.Running => "运行中",
        ServiceStatus.Stopped => "已停止",
        _ => "未知"
    };

    if (data.processInfo != null)
    {
        AppStartTimeText = data.processInfo.Value.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
        AppMemoryUsage = $"{data.processInfo.Value.MemoryMB:F2} MB";
    }
    else
    {
        AppStartTimeText = "-";
        AppMemoryUsage = "-";
    }
}

/// <summary>
/// 更新前端服务状态（必须在 UI 线程调用）
/// </summary>
private void UpdateWebServiceStatus(ServiceStatus status)
{
    IsWebRunning = status == ServiceStatus.Running;
    WebStatusText = status switch
    {
        ServiceStatus.Running => "运行中",
        ServiceStatus.Stopped => "已停止",
        _ => "未知"
    };
}

/// <summary>
/// 进程信息结构
/// </summary>
private readonly record struct ProcessInfo(DateTime StartTime, double MemoryMB);
```

**Step 3: 删除旧的 RefreshAppServiceStatus 和 RefreshWebServiceStatus 方法**

这些方法的内容已经合并到新方法中。

**Step 4: 构建并验证**

```bash
cd backend/tools/DeployManager && dotnet build --no-restore
```

Expected: 构建成功，无错误

**Step 5: 提交**

```bash
git add backend/tools/DeployManager/ViewModels/ServiceControlViewModel.cs
git commit -m "fix: resolve UI thread issue in service status refresh

- Fix cross-thread UI property update issue
- Status retrieval runs on background thread
- Property updates dispatched to UI thread
- Resolves issue where stop/restart buttons remain disabled after service start"
```

---

### Task 2: 增强数据库连接错误诊断（问题3 - 高优先级）

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Program.cs:118-138`
- Modify: `backend/src/DataForgeStudio.Api/Services/DbConnectionStringProvider.cs:40-91`

**Step 1: 在 Program.cs 添加数据库连接测试和详细错误信息**

```csharp
// 配置数据库 - 支持加密的连接字符串
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var masterConnectionString = builder.Configuration.GetConnectionString("MasterConnection");

// 检查是否需要解密连接字符串
connectionString = ConnectionStringHelper.DecryptIfNeeded(connectionString, builder.Configuration);
masterConnectionString = ConnectionStringHelper.DecryptIfNeeded(masterConnectionString, builder.Configuration);

// 验证连接字符串并测试连接
Console.WriteLine("=== 数据库连接配置 ===");
Console.WriteLine($"连接字符串（脱敏）: {SanitizeConnectionString(connectionString)}");

try
{
    using var testConnection = new Microsoft.Data.SqlClient.SqlConnection(masterConnectionString);
    testConnection.Open();
    Console.WriteLine("✅ 数据库连接测试成功");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 数据库连接测试失败: {ex.Message}");
    Console.WriteLine($"   连接字符串详情: {connectionString}");
    Console.WriteLine($"   请检查:");
    Console.WriteLine($"   1. SQL Server 服务是否运行");
    Console.WriteLine($"   2. 服务器地址和端口是否正确");
    Console.WriteLine($"   3. 认证方式（Windows/SQL）是否正确");
    Console.WriteLine($"   4. 用户名密码是否正确（SQL 认证）");
    Console.WriteLine($"   5. Windows 账户是否有权限（Windows 认证）");
    // 不阻止启动，让应用继续运行以便诊断
}

// 脱敏连接字符串（隐藏密码）
static string SanitizeConnectionString(string? connectionString)
{
    if (string.IsNullOrEmpty(connectionString)) return "(空)";
    return System.Text.RegularExpressions.Regex.Replace(
        connectionString,
        @"(Password|Pwd|EncryptedPassword)\s*=\s*[^;]+",
        "$1=***",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}

builder.Services.AddDbContext<DataForgeStudioDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));
```

**Step 2: 在 DbConnectionStringProvider.cs 增强错误信息**

在 `DecryptIfNeeded` 方法的 catch 块中添加更详细的诊断：

```csharp
catch (Exception ex)
{
    Console.WriteLine($"⚠️ 解密连接字符串失败: {ex.Message}");
    Console.WriteLine($"   可能原因:");
    Console.WriteLine($"   1. 环境变量 DFS_ENCRYPTION_AESKEY 未设置");
    Console.WriteLine($"   2. 环境变量 DFS_ENCRYPTION_AESIV 未设置");
    Console.WriteLine($"   3. 密钥与加密时使用的密钥不匹配");
    throw new InvalidOperationException($"解密数据库连接字符串失败: {ex.Message}", ex);
}
```

**Step 3: 添加 using 语句**

确保 Program.cs 顶部有必要的 using：
```csharp
using Microsoft.Data.SqlClient;
```

**Step 4: 构建并验证**

```bash
cd backend/src/DataForgeStudio.Api && dotnet build --no-restore
```

**Step 5: 提交**

```bash
git add backend/src/DataForgeStudio.Api/Program.cs
git add backend/src/DataForgeStudio.Api/Services/DbConnectionStringProvider.cs
git commit -m "feat: add database connection diagnostics on startup

- Test database connection at startup with detailed error messages
- Sanitize connection string in logs (hide password)
- Provide troubleshooting checklist for connection failures
- Help diagnose Windows auth vs SQL auth issues"
```

---

### Task 3: 优化 Nginx 启动配置（问题1 - 中优先级）

**Files:**
- Modify: `backend/tools/DeployManager/Services/WebServiceManager.cs:201-306`
- Modify: `backend/tools/Configurator/Program.cs:487-526`

**Step 1: 优化 Nginx 配置生成**

在 `Configurator/Program.cs` 的 `GenerateNginxConfig` 方法中添加优化：

```csharp
static void GenerateNginxConfig(Configuration config)
{
    var nginxConfPath = Path.Combine(config.InstallPath, "WebServer", "conf", "nginx.conf");
    var nginxConf = $$"""
worker_processes  auto;
error_log  logs/error.log warn;
pid        logs/nginx.pid;

events {
    worker_connections  1024;
    use epoll;
    multi_accept on;
}

http {
    include       mime.types;
    default_type  application/octet-stream;

    # 性能优化
    sendfile        on;
    tcp_nopush      on;
    tcp_nodelay     on;
    keepalive_timeout  65;
    types_hash_max_size 2048;

    # 日志格式
    log_format  main  '$remote_addr - $remote_user [$time_local] "$request" '
                      '$status $body_bytes_sent "$http_referer" '
                      '"$http_user_agent" "$http_x_forwarded_for"';
    access_log  logs/access.log  main;

    # Gzip 压缩
    gzip  on;
    gzip_vary on;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types text/plain text/css text/xml application/json application/javascript application/rss+xml application/atom+xml image/svg+xml;

    server {
        listen       {{config.FrontendPort}};
        server_name  localhost;

        # 静态文件缓存
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
            root   ../WebSite;
            expires 1d;
            add_header Cache-Control "public, immutable";
        }

        location / {
            root   ../WebSite;
            index  index.html index.htm;
            try_files $uri $uri/ /index.html;
        }

        location /api/ {
            proxy_pass         http://127.0.0.1:{{config.BackendPort}}/api/;
            proxy_http_version 1.1;
            proxy_set_header   Upgrade $http_upgrade;
            proxy_set_header   Connection keep-alive;
            proxy_set_header   Host $host;
            proxy_cache_bypass $http_upgrade;
            proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header   X-Forwarded-Proto $scheme;

            # 代理超时设置
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
        }

        error_page   500 502 503 504  /50x.html;
        location = /50x.html { root   html; }
    }
}
""";
    File.WriteAllText(nginxConfPath, nginxConf);
}
```

**Step 2: 减少启动等待时间**

在 `WebServiceManager.cs` 的 `StartNginxAsync` 方法中优化等待逻辑：

将初始等待从 2000ms 减少到 500ms：
```csharp
// 等待 Nginx 完全启动（master 和 worker 进程）
await Task.Delay(500);  // 从 2000ms 减少到 500ms
```

将重试间隔从 1000ms 减少到 300ms：
```csharp
retryCount++;
await Task.Delay(300);  // 从 1000ms 减少到 300ms
```

**Step 3: 构建并验证**

```bash
cd backend/tools && dotnet build --no-restore
```

**Step 4: 提交**

```bash
git add backend/tools/DeployManager/Services/WebServiceManager.cs
git add backend/tools/Configurator/Program.cs
git commit -m "perf: optimize Nginx startup performance

- Enable gzip compression for static assets
- Add static file caching headers
- Reduce startup wait time from 2000ms to 500ms
- Reduce retry interval from 1000ms to 300ms
- Add proxy timeout settings"
```

---

### Task 4: 重新构建安装包并测试

**Step 1: 完整构建**

```bash
cd H:\NEW\DataForgeStudio
scripts/build-installer.bat
```

**Step 2: 验证构建**

确认输出：
```
Successful compile. Resulting Setup program filename is:
H:\NEW\DataForgeStudio\dist\DataForgeStudio-Setup.exe
```

**Step 3: 提交所有更改**

```bash
git add -A
git commit -m "build: generate new installer with fixes

- Fix UI thread issue in service status refresh
- Add database connection diagnostics
- Optimize Nginx startup performance"
```

---

## 测试验证清单

### 问题2 验证（按钮可点击）
- [ ] 安装新版本
- [ ] 打开系统管理工具
- [ ] 切换到 Nginx 模式
- [ ] 点击"启动"按钮
- [ ] 验证"停止"按钮立即可点击
- [ ] 验证"重启"按钮立即可点击
- [ ] 点击"停止"按钮，验证功能正常
- [ ] 再次启动，验证状态显示正确

### 问题3 验证（数据库连接）
- [ ] 安装时选择正确的数据库配置
- [ ] 检查控制台输出的连接测试结果
- [ ] 如果失败，根据提示信息诊断
- [ ] 启动后端服务，检查日志
- [ ] 尝试登录，验证功能正常

### 问题1 验证（启动时间）
- [ ] 记录开发环境启动时间
- [ ] 记录生产环境启动时间
- [ ] 比较差异，确认优化效果

---

## 风险评估

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| UI 线程修复可能影响定时器 | 中 | 充分测试各种场景 |
| Nginx 配置变更可能不兼容旧版本 | 低 | 配置向后兼容 |
| 诊断信息可能暴露敏感信息 | 中 | 使用脱敏函数隐藏密码 |

---

## 相关文档

- `docs/PROJECT_STATUS.md` - 项目状态
- `CLAUDE.md` - 项目指南
- `backend/tools/DeployManager/README.md` - 管理工具说明
