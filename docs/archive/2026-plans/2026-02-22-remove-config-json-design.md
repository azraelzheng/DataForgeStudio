# 设计：移除 config.json，按需读取配置

## 背景

当前 `config.json` 是安装时生成的中间配置文件，用于 DeployManager 读取系统配置。但这些信息都可以从实际的配置文件（appsettings.json、nginx.conf）或运行时环境中获取，维护一个冗余的配置文件增加了复杂性和同步问题。

## 目标

- 移除 `config.json` 文件
- DeployManager 直接从源配置文件按需读取
- 简化代码，减少配置同步问题

## 配置来源映射

| 配置项 | 原来源 | 新来源 |
|--------|--------|--------|
| `installPath` | config.json | 运行时检测（向上查找 appsettings.json） |
| `backend.port` | config.json | `Server/appsettings.json` → Kestrel.Endpoints.Http.Url |
| `backend.serviceName` | config.json | 硬编码常量 `"DFAppService"` |
| `frontend.mode` | config.json | 运行时检测（检查 Nginx/IIS 安装状态） |
| `frontend.port` | config.json | `WebServer/conf/nginx.conf` → listen 指令 |
| `frontend.nginxPath` | config.json | installPath + `\WebServer` |
| `database.*` | config.json | `Server/appsettings.json` → ConnectionStrings |

## 实现设计

### 1. ConfigService 重构

将 ConfigService 从"加载/保存 config.json"模式改为"按需读取源文件"模式。

**新增方法：**

```csharp
public class ConfigService : IConfigService
{
    public const string ServiceName = "DFAppService";

    private readonly string _installPath;
    private readonly IIisManager _iisManager;
    private readonly INginxManager _nginxManager;

    // 按需读取方法
    public int GetBackendPort();
    public int GetFrontendPort();
    public string GetFrontendMode();
    public DatabaseConfig GetDatabaseConfig();
    public string InstallPath => _installPath;

    // 保存方法（写入源文件）
    public void SaveBackendPort(int port);
    public void SaveFrontendPort(int port);
    public void SaveDatabaseConfig(DatabaseConfig config);
}
```

**删除方法：**

- `Load()` - 不再需要加载整个配置
- `Save(DeployConfig)` - 不再需要保存到 config.json

### 2. 解析方法实现

**2.1 解析后端端口**

```csharp
private int ParseKestrelPort(string appSettingsPath)
{
    var json = File.ReadAllText(appSettingsPath);
    var doc = JsonDocument.Parse(json);

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
    return 5000;
}
```

**2.2 解析前端端口**

```csharp
private int ParseNginxListenPort(string nginxConfPath)
{
    var content = File.ReadAllText(nginxConfPath);
    var match = Regex.Match(content, @"listen\s+(\d+)");
    if (match.Success && int.TryParse(match.Groups[1].Value, out var port))
        return port;
    return 80;
}
```

**2.3 检测前端模式**

```csharp
private string DetectFrontendMode()
{
    if (_nginxManager.IsNginxInstalled())
        return "nginx";
    if (_iisManager.IsIisInstalled())
        return "iis";
    return "nginx";
}
```

### 3. 配置保存策略

**3.1 保存后端端口**

更新 `Server/appsettings.json`：
- `Kestrel.Endpoints.Http.Url`
- `Cors.AllowedOrigins` 中的端口引用

**3.2 保存前端端口**

更新 `WebServer/conf/nginx.conf`：
- `listen` 指令
- `proxy_pass` 中的后端 URL（如后端端口变化）

**3.3 保存数据库配置**

更新 `Server/appsettings.json`：
- `ConnectionStrings.DefaultConnection`
- `ConnectionStrings.MasterConnection`

### 4. 文件修改清单

| 文件 | 修改内容 |
|------|----------|
| `ConfigService.cs` | 重构为按需读取模式，删除 Load/Save 方法 |
| `DeployConfig.cs` | 简化或删除（视 ViewModel 使用情况） |
| `Configurator/Program.cs` | 删除 `GenerateDeployConfig()` 方法 |
| `setup.iss` | 移除 `{app}\config` 的清理逻辑 |

### 5. ViewModel 调用方式变更

**之前：**
```csharp
var config = _configService.Load();
var port = config.Backend.Port;
config.Backend.Port = newPort;
_configService.Save(config);
```

**之后：**
```csharp
var port = _configService.GetBackendPort();
_configService.SaveBackendPort(newPort);
```

## 优点

1. **消除配置冗余** - 单一数据源，无同步问题
2. **简化安装流程** - 不需要生成中间配置文件
3. **减少文件数量** - 根目录更整洁
4. **实时性** - 配置始终反映实际文件状态

## 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| 解析失败 | 提供合理的默认值，并记录警告日志 |
| 配置文件格式变化 | 使用容错解析，关键属性缺失时使用默认值 |
| 多处调用性能 | 可在 ConfigService 内部缓存常用配置（可选优化） |

## 实施步骤

1. 重构 ConfigService，添加按需读取方法
2. 更新所有 ViewModel 使用新的读取方式
3. 实现 Save 方法写入源文件
4. 删除 DeployConfig.cs（如果不再需要）
5. 删除 Configurator 中的 GenerateDeployConfig()
6. 更新 setup.iss 移除 config 相关清理
7. 测试完整流程
