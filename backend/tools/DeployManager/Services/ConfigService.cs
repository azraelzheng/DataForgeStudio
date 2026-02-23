using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DeployManager.Models;
using DataForgeStudio.Shared.Utils;

namespace DeployManager.Services;

/// <summary>
/// 配置服务实现
///
/// 配置存储位置：
/// - appsettings.json: 后端端口、数据库连接字符串
/// - nginx.conf: 前端端口（nginx 模式）
/// - IIS: 前端端口（iis 模式）
///
/// 不再使用 config.json 作为配置存储，所有配置直接从实际配置文件读取。
/// </summary>
public class ConfigService : IConfigService
{
    private readonly string _installPath;
    private readonly IIisManager _iisManager;
    private readonly INginxManager _nginxManager;

    /// <summary>
    /// 服务名称常量
    /// </summary>
    public const string ServiceName = "DFAppService";

    /// <summary>
    /// 默认后端端口
    /// </summary>
    private const int DefaultBackendPort = 5000;

    /// <summary>
    /// 默认前端端口
    /// </summary>
    private const int DefaultFrontendPort = 80;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 初始化配置服务
    /// </summary>
    /// <param name="iisManager">IIS 管理器</param>
    /// <param name="nginxManager">Nginx 管理器</param>
    /// <param name="configPath">配置文件路径（已弃用，保留参数是为了向后兼容）</param>
    /// <param name="installPath">安装路径（可选，默认从注册表或默认路径读取）</param>
    public ConfigService(IIisManager iisManager, INginxManager nginxManager, string? configPath = null, string? installPath = null)
    {
        _iisManager = iisManager ?? throw new ArgumentNullException(nameof(iisManager));
        _nginxManager = nginxManager ?? throw new ArgumentNullException(nameof(nginxManager));
        _installPath = installPath ?? GetInstallPath();

        FileLogger.Info($"=== ConfigService 初始化 ===");
        FileLogger.Info($"程序目录: {AppDomain.CurrentDomain.BaseDirectory}");
        FileLogger.Info($"安装路径: {_installPath}");

        Debug.WriteLine($"[ConfigService] 安装路径: {_installPath}");
    }

    /// <summary>
    /// 服务名称（接口实现）
    /// </summary>
    string IConfigService.ServiceName => ServiceName;

    /// <summary>
    /// 获取安装路径
    /// </summary>
    public string InstallPath => _installPath;

    /// <summary>
    /// 获取安装路径
    /// 自动检测：开发环境（查找.sln） -> 生产环境（查找api目录） -> 注册表 -> 默认路径
    /// </summary>
    private string GetInstallPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        Debug.WriteLine($"[ConfigService] === 开始路径检测 ===");
        Debug.WriteLine($"[ConfigService] 程序目录: {appDir}");

        // 1. 尝试自动检测开发环境（向上查找 DataForgeStudio.sln）
        Debug.WriteLine($"[ConfigService] 步骤1: 检测开发环境...");
        var slnPath = FindFileUpwards(appDir, "DataForgeStudio.sln");
        Debug.WriteLine($"[ConfigService] 查找 sln 文件结果: {slnPath ?? "未找到"}");
        if (slnPath != null)
        {
            var slnDir = Path.GetDirectoryName(slnPath);
            if (slnDir != null)
            {
                // 情况A: sln 在 backend 目录下（直接检查 src\DataForgeStudio.Api）
                var devApiPathA = Path.Combine(slnDir, "src", "DataForgeStudio.Api");
                var devAppSettingsA = Path.Combine(devApiPathA, "appsettings.json");
                Debug.WriteLine($"[ConfigService] 检查开发环境路径A (sln在backend目录): {devApiPathA}");
                Debug.WriteLine($"[ConfigService] 检查 appsettings.json: {devAppSettingsA}, 存在: {File.Exists(devAppSettingsA)}");
                if (File.Exists(devAppSettingsA))
                {
                    Debug.WriteLine($"[ConfigService] 成功! 检测到开发环境（sln在backend目录），API路径: {devApiPathA}");
                    return devApiPathA;
                }

                // 情况B: sln 在项目根目录下（检查 backend\src\DataForgeStudio.Api）
                var devApiPathB = Path.Combine(slnDir, "backend", "src", "DataForgeStudio.Api");
                var devAppSettingsB = Path.Combine(devApiPathB, "appsettings.json");
                Debug.WriteLine($"[ConfigService] 检查开发环境路径B (sln在根目录): {devApiPathB}");
                Debug.WriteLine($"[ConfigService] 检查 appsettings.json: {devAppSettingsB}, 存在: {File.Exists(devAppSettingsB)}");
                if (File.Exists(devAppSettingsB))
                {
                    Debug.WriteLine($"[ConfigService] 成功! 检测到开发环境（sln在根目录），API路径: {devApiPathB}");
                    return devApiPathB;
                }
            }
        }

        // 2. 尝试检测生产环境（当前目录或上级目录有 Server/appsettings.json）
        Debug.WriteLine($"[ConfigService] 步骤2: 检测生产环境...");
        var prodApiPath = Path.Combine(appDir, "Server", "appsettings.json");
        Debug.WriteLine($"[ConfigService] 检查生产环境路径1: {prodApiPath}, 存在: {File.Exists(prodApiPath)}");
        if (File.Exists(prodApiPath))
        {
            Debug.WriteLine($"[ConfigService] 成功! 检测到生产环境（当前目录）: {appDir}");
            return appDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        var parentDir = Directory.GetParent(appDir);
        Debug.WriteLine($"[ConfigService] 父目录: {parentDir?.FullName ?? "无"}");
        if (parentDir != null)
        {
            prodApiPath = Path.Combine(parentDir.FullName, "Server", "appsettings.json");
            Debug.WriteLine($"[ConfigService] 检查生产环境路径2: {prodApiPath}, 存在: {File.Exists(prodApiPath)}");
            if (File.Exists(prodApiPath))
            {
                Debug.WriteLine($"[ConfigService] 成功! 检测到生产环境（上级目录）: {parentDir.FullName}");
                return parentDir.FullName;
            }
        }

        // 3. 尝试从注册表读取
        Debug.WriteLine($"[ConfigService] 步骤3: 检测注册表...");
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DataForgeStudio");
            var regPath = key?.GetValue("InstallPath") as string;
            Debug.WriteLine($"[ConfigService] 注册表路径: {regPath ?? "未找到"}");
            if (!string.IsNullOrEmpty(regPath) && Directory.Exists(regPath))
            {
                var regAppSettings = Path.Combine(regPath, "Server", "appsettings.json");
                Debug.WriteLine($"[ConfigService] 检查注册表路径的 appsettings.json: {regAppSettings}, 存在: {File.Exists(regAppSettings)}");
                if (File.Exists(regAppSettings))
                {
                    Debug.WriteLine($"[ConfigService] 成功! 从注册表读取安装路径: {regPath}");
                    return regPath;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 读取注册表失败: {ex.Message}");
        }

        // 4. 使用默认路径
        const string defaultPath = @"C:\Program Files\DataForgeStudio";
        Debug.WriteLine($"[ConfigService] 步骤4: 使用默认安装路径: {defaultPath}");
        Debug.WriteLine($"[ConfigService] === 路径检测结束 ===");
        return defaultPath;
    }

    /// <summary>
    /// 向上查找文件
    /// </summary>
    /// <param name="startDir">起始目录</param>
    /// <param name="fileName">文件名</param>
    /// <param name="maxLevels">最大向上查找层数</param>
    /// <returns>文件完整路径，未找到返回 null</returns>
    private static string? FindFileUpwards(string startDir, string fileName, int maxLevels = 15)
    {
        var currentDir = startDir;
        Debug.WriteLine($"[ConfigService] FindFileUpwards: 开始查找 '{fileName}'，起始目录: {startDir}，最大层数: {maxLevels}");

        for (int i = 0; i < maxLevels; i++)
        {
            var filePath = Path.Combine(currentDir, fileName);
            Debug.WriteLine($"[ConfigService] FindFileUpwards: 第 {i + 1} 层，检查路径: {filePath}");
            if (File.Exists(filePath))
            {
                Debug.WriteLine($"[ConfigService] FindFileUpwards: 找到文件! 路径: {filePath}");
                return filePath;
            }

            var parent = Directory.GetParent(currentDir);
            if (parent == null)
            {
                Debug.WriteLine($"[ConfigService] FindFileUpwards: 已到达根目录，未找到文件");
                break;
            }
            currentDir = parent.FullName;
        }
        Debug.WriteLine($"[ConfigService] FindFileUpwards: 遍历 {maxLevels} 层后未找到文件");
        return null;
    }

    /// <summary>
    /// 获取 appsettings.json 的完整路径
    /// </summary>
    public string GetAppSettingsPath()
    {
        Debug.WriteLine($"[ConfigService] === GetAppSettingsPath 开始 ===");
        Debug.WriteLine($"[ConfigService] 安装路径: {_installPath}");

        // 检查安装路径是否已经包含 appsettings.json（开发环境）
        var directPath = Path.Combine(_installPath, "appsettings.json");
        Debug.WriteLine($"[ConfigService] 检查直接路径: {directPath}, 存在: {File.Exists(directPath)}");
        if (File.Exists(directPath))
        {
            Debug.WriteLine($"[ConfigService] 使用开发环境路径: {directPath}");
            return directPath;
        }

        // 检查 Server 子目录（生产环境）
        var apiPath = Path.Combine(_installPath, "Server", "appsettings.json");
        Debug.WriteLine($"[ConfigService] 检查生产环境路径: {apiPath}, 存在: {File.Exists(apiPath)}");
        if (File.Exists(apiPath))
        {
            Debug.WriteLine($"[ConfigService] 使用生产环境路径: {apiPath}");
            return apiPath;
        }

        // 额外检查：当前工作目录
        var cwd = Directory.GetCurrentDirectory();
        var cwdPath = Path.Combine(cwd, "appsettings.json");
        Debug.WriteLine($"[ConfigService] 检查当前工作目录: {cwdPath}, 存在: {File.Exists(cwdPath)}");
        if (File.Exists(cwdPath))
        {
            Debug.WriteLine($"[ConfigService] 使用当前工作目录路径: {cwdPath}");
            return cwdPath;
        }

        // 额外检查：程序目录
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var appDirPath = Path.Combine(appDir, "appsettings.json");
        Debug.WriteLine($"[ConfigService] 检查程序目录: {appDirPath}, 存在: {File.Exists(appDirPath)}");
        if (File.Exists(appDirPath))
        {
            Debug.WriteLine($"[ConfigService] 使用程序目录路径: {appDirPath}");
            return appDirPath;
        }

        // 额外检查：程序目录的 Server 子目录
        var appDirApiPath = Path.Combine(appDir, "Server", "appsettings.json");
        Debug.WriteLine($"[ConfigService] 检查程序目录 api 子目录: {appDirApiPath}, 存在: {File.Exists(appDirApiPath)}");
        if (File.Exists(appDirApiPath))
        {
            Debug.WriteLine($"[ConfigService] 使用程序目录 api 子目录路径: {appDirApiPath}");
            return appDirApiPath;
        }

        // 默认返回生产环境路径（即使文件不存在，用于后续创建或错误提示）
        Debug.WriteLine($"[ConfigService] 未找到 appsettings.json，返回默认路径: {apiPath}");
        Debug.WriteLine($"[ConfigService] === GetAppSettingsPath 结束 ===");
        return apiPath;
    }

    /// <summary>
    /// 从 URL 字符串中提取端口号
    /// </summary>
    private static int ExtractPortFromUrl(string url)
    {
        try
        {
            // 格式: http://0.0.0.0:5000 或 http://localhost:5000
            var uri = new Uri(url);
            return uri.Port > 0 ? uri.Port : 5000;
        }
        catch
        {
            // 尝试正则匹配端口
            var match = System.Text.RegularExpressions.Regex.Match(url, @":(\d+)(?:/|$)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var port))
            {
                return port;
            }
            return 5000;
        }
    }

    /// <summary>
    /// 解析数据库连接字符串
    /// </summary>
    private static DatabaseConfig ParseConnectionString(string connectionString)
    {
        var config = new DatabaseConfig();

        try
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);

            // 解析服务器和端口
            var dataSource = builder.DataSource;
            if (!string.IsNullOrEmpty(dataSource))
            {
                // 格式可能是: server, port 或 tcp:server,port
                dataSource = dataSource.Replace("tcp:", "");
                var parts = dataSource.Split(',');
                if (parts.Length >= 2)
                {
                    config.Server = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out var port))
                    {
                        config.Port = port;
                    }
                }
                else
                {
                    config.Server = dataSource.Trim();
                    config.Port = 1433; // SQL Server 默认端口
                }
            }

            config.Database = builder.InitialCatalog;
            config.UseWindowsAuth = builder.IntegratedSecurity;

            if (!builder.IntegratedSecurity)
            {
                config.Username = builder.UserID ?? "sa";
                config.Password = builder.Password ?? "";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 解析连接字符串失败: {ex.Message}");
        }

        return config;
    }

    /// <summary>
    /// 更新 appsettings.json 中的指定节点
    /// </summary>
    private void UpdateAppSettings(Action<JsonObject> updateAction)
    {
        var appSettingsPath = GetAppSettingsPath();
        Debug.WriteLine($"[ConfigService] === UpdateAppSettings 开始 ===");
        Debug.WriteLine($"[ConfigService] 目标文件: {appSettingsPath}");

        if (!File.Exists(appSettingsPath))
        {
            Debug.WriteLine($"[ConfigService] appsettings.json 不存在，跳过更新: {appSettingsPath}");
            throw new FileNotFoundException($"appsettings.json 不存在: {appSettingsPath}");
        }

        try
        {
            Debug.WriteLine($"[ConfigService] 读取文件内容...");
            var json = File.ReadAllText(appSettingsPath);
            Debug.WriteLine($"[ConfigService] 文件内容长度: {json.Length} 字符");

            var rootNode = JsonNode.Parse(json);
            if (rootNode == null)
            {
                Debug.WriteLine($"[ConfigService] 警告: 无法解析 JSON");
                throw new InvalidOperationException("无法解析 appsettings.json");
            }

            // 执行更新操作
            updateAction((JsonObject)rootNode);

            // 保存修改后的 JSON
            Debug.WriteLine($"[ConfigService] 保存修改后的 JSON...");
            var serializerOptions = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = rootNode.ToJsonString(serializerOptions);
            File.WriteAllText(appSettingsPath, updatedJson, Encoding.UTF8);

            Debug.WriteLine($"[ConfigService] === UpdateAppSettings 完成 ===");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 更新 appsettings.json 失败: {ex.Message}");
            Debug.WriteLine($"[ConfigService] 堆栈跟踪: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 构建 master 数据库连接字符串
    /// </summary>
    private static string BuildMasterConnectionString(DatabaseConfig config)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = config.Port == 1433 ? $"tcp:{config.Server}" : $"tcp:{config.Server},{config.Port}",
            InitialCatalog = "master",
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };

        if (config.UseWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = config.Username;
            builder.Password = config.Password;
        }

        return builder.ConnectionString;
    }

    // ========== 接口实现 ==========

    /// <summary>
    /// 从 appsettings.json 读取后端端口
    /// </summary>
    /// <returns>端口号，如果未配置则返回 5000</returns>
    public int GetBackendPort()
    {
        var appSettingsPath = GetAppSettingsPath();
        if (!File.Exists(appSettingsPath))
        {
            return DefaultBackendPort;
        }

        try
        {
            var json = File.ReadAllText(appSettingsPath);
            var rootNode = JsonNode.Parse(json);
            if (rootNode == null) return DefaultBackendPort;

            var kestrelNode = rootNode["Kestrel"];
            if (kestrelNode == null) return DefaultBackendPort;

            var endpointsNode = kestrelNode["Endpoints"];
            if (endpointsNode == null) return DefaultBackendPort;

            var httpNode = endpointsNode["Http"];
            if (httpNode == null) return DefaultBackendPort;

            var url = httpNode["Url"]?.GetValue<string>();
            if (string.IsNullOrEmpty(url)) return DefaultBackendPort;

            return ExtractPortFromUrl(url);
        }
        catch
        {
            return DefaultBackendPort;
        }
    }

    /// <summary>
    /// 从 appsettings.json 读取数据库配置
    /// </summary>
    /// <returns>数据库配置对象</returns>
    public DatabaseConfig GetDatabaseConfig()
    {
        var appSettingsPath = GetAppSettingsPath();
        if (!File.Exists(appSettingsPath))
        {
            return new DatabaseConfig();
        }

        try
        {
            var json = File.ReadAllText(appSettingsPath);
            var rootNode = JsonNode.Parse(json);
            if (rootNode == null) return new DatabaseConfig();

            var connectionStringsNode = rootNode["ConnectionStrings"];
            if (connectionStringsNode == null) return new DatabaseConfig();

            var defaultConnection = connectionStringsNode["DefaultConnection"]?.GetValue<string>();
            if (string.IsNullOrEmpty(defaultConnection)) return new DatabaseConfig();

            return ParseConnectionString(defaultConnection);
        }
        catch
        {
            return new DatabaseConfig();
        }
    }

    /// <summary>
    /// 从 nginx.conf 解析前端端口
    /// </summary>
    private int ParseFrontendPort(string nginxConfPath)
    {
        try
        {
            if (!File.Exists(nginxConfPath))
            {
                return DefaultFrontendPort;
            }
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
    /// 优先级：用户保存的设置 > 自动检测
    /// </summary>
    public string GetFrontendMode()
    {
        // 1. 首先检查用户保存的设置
        var savedMode = LoadSavedFrontendMode();
        if (!string.IsNullOrEmpty(savedMode))
        {
            // 验证保存的模式是否仍然可用
            if (savedMode == "iis" && _iisManager.IsIisInstalled())
                return "iis";
            if (savedMode == "nginx" && _nginxManager.IsNginxInstalled())
                return "nginx";
        }

        // 2. 自动检测（优先使用 Nginx，因为安装包自带）
        if (_nginxManager.IsNginxInstalled())
            return "nginx";
        if (_iisManager.IsIisInstalled())
            return "iis";
        return "nginx";
    }

    /// <summary>
    /// 保存前端模式
    /// </summary>
    /// <param name="mode">前端模式 ("nginx" 或 "iis")</param>
    public void SaveFrontendMode(string mode)
    {
        if (mode != "nginx" && mode != "iis")
        {
            throw new ArgumentException("前端模式必须是 'nginx' 或 'iis'", nameof(mode));
        }

        try
        {
            var modeFilePath = GetFrontendModeFilePath();
            File.WriteAllText(modeFilePath, mode, Encoding.UTF8);
            FileLogger.Info($"前端模式已保存: {mode}");
            Debug.WriteLine($"[ConfigService] 前端模式已保存: {mode} -> {modeFilePath}");
        }
        catch (Exception ex)
        {
            FileLogger.Error($"保存前端模式失败: {ex.Message}");
            Debug.WriteLine($"[ConfigService] 保存前端模式失败: {ex.Message}");
            // 不抛出异常，保存失败不应该阻止模式切换
        }
    }

    /// <summary>
    /// 加载保存的前端模式
    /// </summary>
    /// <returns>保存的模式，如果没有保存则返回 null</returns>
    private string? LoadSavedFrontendMode()
    {
        try
        {
            var modeFilePath = GetFrontendModeFilePath();
            if (File.Exists(modeFilePath))
            {
                var mode = File.ReadAllText(modeFilePath, Encoding.UTF8).Trim().ToLowerInvariant();
                if (mode == "nginx" || mode == "iis")
                {
                    Debug.WriteLine($"[ConfigService] 从文件加载前端模式: {mode}");
                    return mode;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 读取前端模式失败: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// 获取前端模式配置文件路径
    /// </summary>
    private string GetFrontendModeFilePath()
    {
        return Path.Combine(_installPath, ".frontend_mode");
    }

    /// <summary>
    /// 获取 Nginx 路径
    /// </summary>
    public string GetNginxPath() => Path.Combine(_installPath, "WebServer");

    /// <summary>
    /// 获取 WebSite 路径
    /// </summary>
    public string GetWebSitePath() => Path.Combine(_installPath, "WebSite");

    /// <summary>
    /// 保存后端端口（更新 appsettings.json）
    /// </summary>
    public void SaveBackendPort(int port)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "端口号必须在 1-65535 之间");
        }

        UpdateAppSettings(rootNode =>
        {
            // 更新 Kestrel 配置
            var kestrelNode = rootNode["Kestrel"];
            if (kestrelNode == null)
            {
                kestrelNode = new JsonObject();
                rootNode["Kestrel"] = kestrelNode;
            }

            var endpointsNode = kestrelNode["Endpoints"];
            if (endpointsNode == null)
            {
                endpointsNode = new JsonObject();
                kestrelNode["Endpoints"] = endpointsNode;
            }

            var httpNode = endpointsNode["Http"];
            if (httpNode == null)
            {
                httpNode = new JsonObject();
                endpointsNode["Http"] = httpNode;
            }

            httpNode["Url"] = $"http://0.0.0.0:{port}";
        });

        Debug.WriteLine($"[ConfigService] 后端端口已更新: {port}");
    }

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

            // 使用 UTF-8 without BOM 编码，避免 Nginx 解析错误
            File.WriteAllText(nginxConfPath, content, new System.Text.UTF8Encoding(false));
            FileLogger.Info($"前端端口已更新为 {port}");
        }
        catch (Exception ex)
        {
            FileLogger.Error($"保存前端端口失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 保存数据库配置（更新 appsettings.json 连接字符串）
    /// 密码会被加密存储（使用 EncryptedPassword= 格式）
    /// </summary>
    public void SaveDatabaseConfig(DatabaseConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        UpdateAppSettings(rootNode =>
        {
            // 更新连接字符串
            var connectionStringsNode = rootNode["ConnectionStrings"];
            if (connectionStringsNode == null)
            {
                connectionStringsNode = new JsonObject();
                rootNode["ConnectionStrings"] = connectionStringsNode;
            }

            // 构建带加密密码的连接字符串
            var connectionString = BuildEncryptedConnectionString(config);
            connectionStringsNode["DefaultConnection"] = connectionString;

            // 同时更新 MasterConnection
            var masterConnectionString = BuildEncryptedMasterConnectionString(config);
            connectionStringsNode["MasterConnection"] = masterConnectionString;
        });

        Debug.WriteLine($"[ConfigService] 数据库配置已更新（密码已加密）");
    }

    /// <summary>
    /// 获取加密密钥
    /// </summary>
    private static (string key, string iv) GetEncryptionKeys()
    {
        // 优先从环境变量读取
        var key = Environment.GetEnvironmentVariable("DFS_ENCRYPTION_AESKEY");
        var iv = Environment.GetEnvironmentVariable("DFS_ENCRYPTION_AESIV");

        // 如果环境变量未设置，使用默认值（生产环境应该通过环境变量设置）
        if (string.IsNullOrEmpty(key))
        {
            key = "DataForgeStudioV4AESKey32Bytes!!";
            FileLogger.Warning("使用默认加密密钥（生产环境应设置 DFS_ENCRYPTION_AESKEY 环境变量）");
        }
        if (string.IsNullOrEmpty(iv))
        {
            iv = "DataForgeIV16Byte!";
            FileLogger.Warning("使用默认加密IV（生产环境应设置 DFS_ENCRYPTION_AESIV 环境变量）");
        }

        return (key, iv);
    }

    /// <summary>
    /// 构建带加密密码的连接字符串
    /// </summary>
    private string BuildEncryptedConnectionString(DatabaseConfig config)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = config.Port == 1433 ? $"tcp:{config.Server}" : $"tcp:{config.Server},{config.Port}",
            InitialCatalog = config.Database,
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };

        if (config.UseWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = config.Username;
            // 加密密码
            var (key, iv) = GetEncryptionKeys();
            var encryptedPassword = DataForgeStudio.Shared.Utils.EncryptionHelper.AesEncrypt(config.Password, key, iv);
            // 使用 EncryptedPassword 替代 Password
            // 注意：SqlConnectionStringBuilder 不支持 EncryptedPassword，所以手动构建
            return $"Data Source={builder.DataSource};Initial Catalog={builder.InitialCatalog};User ID={config.Username};EncryptedPassword={encryptedPassword};Connect Timeout={builder.ConnectTimeout};Trust Server Certificate=True";
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// 构建 master 数据库连接字符串（带加密密码）
    /// </summary>
    private string BuildEncryptedMasterConnectionString(DatabaseConfig config)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = config.Port == 1433 ? $"tcp:{config.Server}" : $"tcp:{config.Server},{config.Port}",
            InitialCatalog = "master",
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };

        if (config.UseWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = config.Username;
            var (key, iv) = GetEncryptionKeys();
            var encryptedPassword = DataForgeStudio.Shared.Utils.EncryptionHelper.AesEncrypt(config.Password, key, iv);
            return $"Data Source={builder.DataSource};Initial Catalog={builder.InitialCatalog};User ID={config.Username};EncryptedPassword={encryptedPassword};Connect Timeout={builder.ConnectTimeout};Trust Server Certificate=True";
        }

        return builder.ConnectionString;
    }
}
