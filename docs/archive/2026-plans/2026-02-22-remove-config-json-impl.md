# 移除 config.json 实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 移除冗余的 config.json 文件，让 DeployManager 直接从源配置文件按需读取配置。

**Architecture:** 重构 ConfigService 为纯读取服务，从 appsettings.json 和 nginx.conf 解析配置，serviceName 硬编码，前端模式通过运行时检测确定。

**Tech Stack:** C# .NET 8.0, System.Text.Json, 正则表达式解析

---

## Task 1: 添加配置解析辅助方法

**Files:**
- Modify: `backend/tools/DeployManager/Services/ConfigService.cs`

**Step 1: 添加硬编码常量和必要的 using**

在 ConfigService 类顶部添加：

```csharp
using System.Text.Json;
using System.Text.RegularExpressions;

public class ConfigService : IConfigService
{
    // 硬编码常量
    public const string ServiceName = "DFAppService";
    private const int DefaultBackendPort = 5000;
    private const int DefaultFrontendPort = 80;
```

**Step 2: 添加解析后端端口方法**

```csharp
/// <summary>
/// 从 appsettings.json 解析后端端口
/// </summary>
private int ParseBackendPort(string appSettingsPath)
{
    try
    {
        var json = File.ReadAllText(appSettingsPath);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("Kestrel", out var kestrel) &&
            kestrel.TryGetProperty("Endpoints", out var endpoints) &&
            endpoints.TryGetProperty("Http", out var http) &&
            http.TryGetProperty("Url", out var url))
        {
            var urlStr = url.GetString();
            var portMatch = Regex.Match(urlStr ?? "", @":(\d+)");
            if (portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out var port))
                return port;
        }
    }
    catch (Exception ex)
    {
        FileLogger.Warning($"解析后端端口失败: {ex.Message}");
    }
    return DefaultBackendPort;
}
```

**Step 3: 添加解析前端端口方法**

```csharp
/// <summary>
/// 从 nginx.conf 解析前端端口
/// </summary>
private int ParseFrontendPort(string nginxConfPath)
{
    try
    {
        var content = File.ReadAllText(nginxConfPath);
        var match = Regex.Match(content, @"listen\s+(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var port))
            return port;
    }
    catch (Exception ex)
    {
        FileLogger.Warning($"解析前端端口失败: {ex.Message}");
    }
    return DefaultFrontendPort;
}
```

**Step 4: 添加解析数据库配置方法**

```csharp
/// <summary>
/// 从 appsettings.json 的连接字符串解析数据库配置
/// </summary>
private DatabaseConfig ParseDatabaseConfig(string appSettingsPath)
{
    var config = new DatabaseConfig();

    try
    {
        var json = File.ReadAllText(appSettingsPath);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings) &&
            connStrings.TryGetProperty("DefaultConnection", out var connStr))
        {
            var connectionString = connStr.GetString();
            if (!string.IsNullOrEmpty(connectionString))
            {
                // 解析 Server=tcp:localhost,1433
                var serverMatch = Regex.Match(connectionString, @"Server=tcp:([^,;]+)(?:,(\d+))?");
                if (serverMatch.Success)
                {
                    config.Server = serverMatch.Groups[1].Value;
                    config.Port = serverMatch.Groups[2].Success && int.TryParse(serverMatch.Groups[2].Value, out var port)
                        ? port : 1433;
                }

                // 解析 Database=DataForgeStudio
                var dbMatch = Regex.Match(connectionString, @"Database=([^;]+)");
                if (dbMatch.Success)
                    config.Database = dbMatch.Groups[1].Value;

                // 解析 User ID=sa
                var userMatch = Regex.Match(connectionString, @"User ID=([^;]+)");
                if (userMatch.Success)
                {
                    config.Username = userMatch.Groups[1].Value;
                    config.UseWindowsAuth = false;

                    var pwdMatch = Regex.Match(connectionString, @"Password=([^;]+)");
                    if (pwdMatch.Success)
                        config.Password = pwdMatch.Groups[1].Value;
                }
                else
                {
                    config.UseWindowsAuth = true;
                }
            }
        }
    }
    catch (Exception ex)
    {
        FileLogger.Warning($"解析数据库配置失败: {ex.Message}");
    }

    return config;
}
```

**Step 5: 验证编译**

Run: `dotnet build backend/tools/DeployManager/DeployManager.csproj -c Release`
Expected: 编译成功，无错误

**Step 6: Commit**

```bash
git add backend/tools/DeployManager/Services/ConfigService.cs
git commit -m "feat(deploy-manager): add config parsing helper methods

- Add ParseBackendPort from appsettings.json
- Add ParseFrontendPort from nginx.conf
- Add ParseDatabaseConfig from connection string

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 2: 添加公开的配置读取方法

**Files:**
- Modify: `backend/tools/DeployManager/Services/ConfigService.cs`

**Step 1: 添加公开读取方法**

```csharp
/// <summary>
/// 获取后端端口
/// </summary>
public int GetBackendPort()
{
    var appSettingsPath = Path.Combine(_installPath, "Server", "appsettings.json");
    return ParseBackendPort(appSettingsPath);
}

/// <summary>
/// 获取前端端口
/// </summary>
public int GetFrontendPort()
{
    var nginxConfPath = Path.Combine(_installPath, "WebServer", "conf", "nginx.conf");
    return ParseFrontendPort(nginxConfPath);
}

/// <summary>
/// 获取前端模式 (nginx/iis)
/// </summary>
public string GetFrontendMode()
{
    // 优先使用 Nginx（安装包自带）
    if (_nginxManager.IsNginxInstalled())
        return "nginx";
    if (_iisManager.IsIisInstalled())
        return "iis";
    return "nginx";
}

/// <summary>
/// 获取数据库配置
/// </summary>
public DatabaseConfig GetDatabaseConfig()
{
    var appSettingsPath = Path.Combine(_installPath, "Server", "appsettings.json");
    return ParseDatabaseConfig(appSettingsPath);
}

/// <summary>
/// 获取 Nginx 路径
/// </summary>
public string GetNginxPath() => Path.Combine(_installPath, "WebServer");

/// <summary>
/// 获取 WebSite 路径
/// </summary>
public string GetWebSitePath() => Path.Combine(_installPath, "WebSite");
```

**Step 2: 验证编译**

Run: `dotnet build backend/tools/DeployManager/DeployManager.csproj -c Release`
Expected: 编译成功

**Step 3: Commit**

```bash
git add backend/tools/DeployManager/Services/ConfigService.cs
git commit -m "feat(deploy-manager): add public config getter methods

- GetBackendPort, GetFrontendPort, GetFrontendMode
- GetDatabaseConfig, GetNginxPath, GetWebSitePath

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 3: 添加配置保存方法

**Files:**
- Modify: `backend/tools/DeployManager/Services/ConfigService.cs`

**Step 1: 添加保存后端端口方法**

```csharp
/// <summary>
/// 保存后端端口（更新 appsettings.json）
/// </summary>
public void SaveBackendPort(int port)
{
    var appSettingsPath = Path.Combine(_installPath, "Server", "appsettings.json");

    try
    {
        var json = File.ReadAllText(appSettingsPath);
        using var doc = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name == "Kestrel")
            {
                writer.WritePropertyName("Kestrel");
                writer.WriteStartObject();
                writer.WritePropertyName("Endpoints");
                writer.WriteStartObject();
                writer.WritePropertyName("Http");
                writer.WriteStartObject();
                writer.WriteString("Url", $"http://*:{port}");
                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            else if (prop.Name == "Cors")
            {
                // 更新 CORS 中的端口引用
                writer.WritePropertyName("Cors");
                writer.WriteStartObject();
                writer.WritePropertyName("AllowedOrigins");
                writer.WriteStartArray();
                writer.WriteStringValue("http://localhost");
                writer.WriteStringValue($"http://localhost:{GetFrontendPort()}");
                writer.WriteStringValue("http://*");
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            else
            {
                prop.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        var newJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        File.WriteAllText(appSettingsPath, newJson);

        FileLogger.Info($"后端端口已更新为 {port}");
    }
    catch (Exception ex)
    {
        FileLogger.Error($"保存后端端口失败: {ex.Message}");
        throw;
    }
}
```

**Step 2: 添加保存前端端口方法**

```csharp
/// <summary>
/// 保存前端端口（更新 nginx.conf）
/// </summary>
public void SaveFrontendPort(int port)
{
    var nginxConfPath = Path.Combine(_installPath, "WebServer", "conf", "nginx.conf");

    try
    {
        var content = File.ReadAllText(nginxConfPath);
        var backendPort = GetBackendPort();

        // 替换 listen 端口
        content = Regex.Replace(content, @"listen\s+\d+", $"listen       {port}");

        // 替换 proxy_pass 中的后端端口
        content = Regex.Replace(
            content,
            @"proxy_pass\s+http://127\.0\.0\.1:\d+",
            $"proxy_pass         http://127.0.0.1:{backendPort}");

        File.WriteAllText(nginxConfPath, content);
        FileLogger.Info($"前端端口已更新为 {port}");
    }
    catch (Exception ex)
    {
        FileLogger.Error($"保存前端端口失败: {ex.Message}");
        throw;
    }
}
```

**Step 3: 添加保存数据库配置方法**

```csharp
/// <summary>
/// 保存数据库配置（更新 appsettings.json 连接字符串）
/// </summary>
public void SaveDatabaseConfig(DatabaseConfig config)
{
    var appSettingsPath = Path.Combine(_installPath, "Server", "appsettings.json");

    try
    {
        var connectionString = config.GetConnectionString();
        var masterConnectionString = connectionString.Replace(config.Database, "master");

        var json = File.ReadAllText(appSettingsPath);
        using var doc = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name == "ConnectionStrings")
            {
                writer.WritePropertyName("ConnectionStrings");
                writer.WriteStartObject();
                writer.WriteString("DefaultConnection", connectionString);
                writer.WriteString("MasterConnection", masterConnectionString);
                writer.WriteEndObject();
            }
            else
            {
                prop.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        var newJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        File.WriteAllText(appSettingsPath, newJson);

        FileLogger.Info("数据库配置已更新");
    }
    catch (Exception ex)
    {
        FileLogger.Error($"保存数据库配置失败: {ex.Message}");
        throw;
    }
}
```

**Step 4: 验证编译**

Run: `dotnet build backend/tools/DeployManager/DeployManager.csproj -c Release`
Expected: 编译成功

**Step 5: Commit**

```bash
git add backend/tools/DeployManager/Services/ConfigService.cs
git commit -m "feat(deploy-manager): add config save methods

- SaveBackendPort updates appsettings.json
- SaveFrontendPort updates nginx.conf
- SaveDatabaseConfig updates connection strings

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 4: 更新 IConfigService 接口

**Files:**
- Modify: `backend/tools/DeployManager/Services/IConfigService.cs`

**Step 1: 更新接口定义**

```csharp
namespace DeployManager.Services;

public interface IConfigService
{
    /// <summary>
    /// 安装路径
    /// </summary>
    string InstallPath { get; }

    // 读取方法
    int GetBackendPort();
    int GetFrontendPort();
    string GetFrontendMode();
    DatabaseConfig GetDatabaseConfig();
    string GetNginxPath();
    string GetWebSitePath();

    // 保存方法
    void SaveBackendPort(int port);
    void SaveFrontendPort(int port);
    void SaveDatabaseConfig(DatabaseConfig config);
}
```

**Step 2: 验证编译**

Run: `dotnet build backend/tools/DeployManager/DeployManager.csproj -c Release`
Expected: 编译成功

**Step 3: Commit**

```bash
git add backend/tools/DeployManager/Services/IConfigService.cs
git commit -m "refactor(deploy-manager): update IConfigService interface

- Remove Load/Save(DeployConfig) methods
- Add granular getter/setter methods

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 5: 更新 ViewModel 使用新接口

**Files:**
- Modify: `backend/tools/DeployManager/ViewModels/PortConfigViewModel.cs`
- Modify: `backend/tools/DeployManager/ViewModels/FrontendModeViewModel.cs`
- Modify: `backend/tools/DeployManager/ViewModels/ServiceControlViewModel.cs`

**Step 1: 更新 PortConfigViewModel 读取配置**

将 `_configService.Load()` 调用替换为新的 getter 方法：

```csharp
// 旧代码
var config = _configService.Load();
BackendPort = config.Backend.Port;
FrontendPort = config.Frontend.Port;

// 新代码
BackendPort = _configService.GetBackendPort();
FrontendPort = _configService.GetFrontendPort();
```

**Step 2: 更新 PortConfigViewModel 保存配置**

```csharp
// 旧代码
var config = _configService.Load();
config.Backend.Port = BackendPort;
config.Frontend.Port = FrontendPort;
_configService.Save(config);

// 新代码
_configService.SaveBackendPort(BackendPort);
_configService.SaveFrontendPort(FrontendPort);
```

**Step 3: 更新 FrontendModeViewModel**

```csharp
// 旧代码
var config = _configService.Load();
var configMode = config.Frontend.Mode?.ToLowerInvariant() ?? "nginx";

// 新代码
var configMode = _configService.GetFrontendMode().ToLowerInvariant();
```

**Step 4: 更新 ServiceControlViewModel**

```csharp
// 旧代码
var config = _configService.Load();
WebServiceType = config.Frontend.Mode?.ToUpper() == "NGINX" ? "Nginx" : "IIS";

// 新代码
WebServiceType = _configService.GetFrontendMode().ToUpper() == "NGINX" ? "Nginx" : "IIS";
```

**Step 5: 验证编译**

Run: `dotnet build backend/tools/DeployManager/DeployManager.csproj -c Release`
Expected: 编译成功

**Step 6: Commit**

```bash
git add backend/tools/DeployManager/ViewModels/
git commit -m "refactor(deploy-manager): update ViewModels to use new config interface

- Replace Load/Save with granular getter/setter methods
- Remove DeployConfig dependency

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 6: 清理旧代码

**Files:**
- Modify: `backend/tools/DeployManager/Services/ConfigService.cs`
- Modify: `backend/tools/DeployManager/Models/DeployConfig.cs`
- Modify: `backend/tools/Configurator/Program.cs`

**Step 1: 从 ConfigService 移除旧方法**

删除以下方法：
- `GetDefaultConfigPath()`
- `Load()`
- `Save(DeployConfig config)`
- 所有与 config.json 相关的读写逻辑

**Step 2: 简化 DeployConfig.cs**

保留 DatabaseConfig 类（GetConnectionString 方法仍需要），删除其他类：

```csharp
namespace DeployManager.Models;

public class DatabaseConfig
{
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 1433;
    public string Database { get; set; } = "DataForgeStudio";
    public string Username { get; set; } = "sa";
    public string Password { get; set; } = "";
    public bool UseWindowsAuth { get; set; } = true;

    public string GetConnectionString()
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = Port == 1433 ? $"tcp:{Server}" : $"tcp:{Server},{Port}",
            InitialCatalog = Database,
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };
        if (UseWindowsAuth)
            builder.IntegratedSecurity = true;
        else
        {
            builder.UserID = Username;
            builder.Password = Password;
        }
        return builder.ConnectionString;
    }
}
```

**Step 3: 从 Configurator 删除 GenerateDeployConfig 方法**

删除 `Program.cs` 中的：
- `GenerateDeployConfig()` 方法（第 529-567 行）
- 对该方法的调用（第 406 行）

**Step 4: 验证编译**

Run: `dotnet build backend/tools/DeployManager/DeployManager.csproj -c Release && dotnet build backend/tools/Configurator/Configurator.csproj -c Release`
Expected: 编译成功

**Step 5: Commit**

```bash
git add backend/tools/DeployManager/Services/ConfigService.cs
git add backend/tools/DeployManager/Models/DeployConfig.cs
git add backend/tools/Configurator/Program.cs
git commit -m "refactor: remove config.json generation and legacy methods

- Remove Load/Save methods from ConfigService
- Simplify DeployConfig to only keep DatabaseConfig
- Remove GenerateDeployConfig from Configurator

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 7: 更新安装脚本

**Files:**
- Modify: `installer/setup.iss`

**Step 1: 移除 config 目录清理**

在 `[UninstallDelete]` 段中，移除这一行：

```
Type: filesandordirs; Name: "{app}\config"
```

**Step 2: Commit**

```bash
git add installer/setup.iss
git commit -m "chore(installer): remove config directory cleanup

config.json is no longer generated

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 8: 构建和测试

**Files:**
- N/A

**Step 1: 完整构建**

Run: `scripts/build-installer.bat`
Expected: 构建成功，生成安装包

**Step 2: 手动测试清单**

- [ ] 安装程序正常运行
- [ ] DeployManager 正确读取后端端口
- [ ] DeployManager 正确读取前端端口
- [ ] DeployManager 正确检测前端模式
- [ ] 修改端口后配置正确保存
- [ ] 服务启动/停止正常
- [ ] 卸载程序正常运行

**Step 3: Final Commit**

```bash
git add -A
git commit -m "feat: complete config.json removal

- DeployManager reads config directly from source files
- Simplified architecture with no redundant config
- Root directory is cleaner

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Summary

| Task | Description |
|------|-------------|
| 1 | 添加配置解析辅助方法 |
| 2 | 添加公开的配置读取方法 |
| 3 | 添加配置保存方法 |
| 4 | 更新 IConfigService 接口 |
| 5 | 更新 ViewModel 使用新接口 |
| 6 | 清理旧代码 |
| 7 | 更新安装脚本 |
| 8 | 构建和测试 |
