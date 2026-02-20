using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeployManager.Models;

namespace DeployManager.Services;

/// <summary>
/// 配置服务实现
///
/// 配置管理分为两部分：
/// 1. config.json - 本地缓存，保存安装路径、前端模式等元信息
/// 2. appsettings.json - 实际项目配置，保存端口、数据库连接等
///
/// Load() 从两个文件合并读取
/// Save() 同时更新两个文件
/// </summary>
public class ConfigService : IConfigService
{
    private readonly string _configPath;
    private readonly string _installPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 初始化配置服务
    /// </summary>
    /// <param name="configPath">配置文件路径（可选，默认为应用程序目录下的 config.json）</param>
    /// <param name="installPath">安装路径（可选，默认从注册表或默认路径读取）</param>
    public ConfigService(string? configPath = null, string? installPath = null)
    {
        _configPath = configPath ?? GetDefaultConfigPath();
        _installPath = installPath ?? GetInstallPath();
        Debug.WriteLine($"[ConfigService] 配置文件路径: {_configPath}");
        Debug.WriteLine($"[ConfigService] 安装路径: {_installPath}");
    }

    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    public string ConfigPath => _configPath;

    /// <summary>
    /// 获取安装路径
    /// </summary>
    public string InstallPath => _installPath;

    /// <summary>
    /// 获取默认配置文件路径
    /// </summary>
    private static string GetDefaultConfigPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, "config.json");
    }

    /// <summary>
    /// 获取安装路径
    /// 自动检测：开发环境（查找.sln） -> 生产环境（查找api目录） -> 注册表 -> config.json -> 默认路径
    /// </summary>
    private string GetInstallPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        Debug.WriteLine($"[ConfigService] 程序目录: {appDir}");

        // 1. 尝试自动检测开发环境（向上查找 DataForgeStudio.sln）
        var slnPath = FindFileUpwards(appDir, "DataForgeStudio.sln");
        if (slnPath != null)
        {
            var slnDir = Path.GetDirectoryName(slnPath);
            if (slnDir != null)
            {
                // 开发环境：sln 目录下的 backend\src\DataForgeStudio.Api
                var devApiPath = Path.Combine(slnDir, "backend", "src", "DataForgeStudio.Api");
                var devAppSettings = Path.Combine(devApiPath, "appsettings.json");
                if (File.Exists(devAppSettings))
                {
                    Debug.WriteLine($"[ConfigService] 检测到开发环境，API路径: {devApiPath}");
                    return devApiPath;
                }
            }
        }

        // 2. 尝试检测生产环境（当前目录或上级目录有 api/appsettings.json）
        var prodApiPath = Path.Combine(appDir, "api", "appsettings.json");
        if (File.Exists(prodApiPath))
        {
            Debug.WriteLine($"[ConfigService] 检测到生产环境（当前目录）: {appDir}");
            return appDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        var parentDir = Directory.GetParent(appDir);
        if (parentDir != null)
        {
            prodApiPath = Path.Combine(parentDir.FullName, "api", "appsettings.json");
            if (File.Exists(prodApiPath))
            {
                Debug.WriteLine($"[ConfigService] 检测到生产环境（上级目录）: {parentDir.FullName}");
                return parentDir.FullName;
            }
        }

        // 3. 尝试从注册表读取
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DataForgeStudio");
            var regPath = key?.GetValue("InstallPath") as string;
            if (!string.IsNullOrEmpty(regPath) && Directory.Exists(regPath))
            {
                var regAppSettings = Path.Combine(regPath, "api", "appsettings.json");
                if (File.Exists(regAppSettings))
                {
                    Debug.WriteLine($"[ConfigService] 从注册表读取安装路径: {regPath}");
                    return regPath;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 读取注册表失败: {ex.Message}");
        }

        // 4. 尝试从 config.json 读取
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<DeployConfig>(json, JsonOptions);
                if (!string.IsNullOrEmpty(config?.InstallPath) && Directory.Exists(config.InstallPath))
                {
                    var configAppSettings = Path.Combine(config.InstallPath, "api", "appsettings.json");
                    if (File.Exists(configAppSettings))
                    {
                        Debug.WriteLine($"[ConfigService] 从 config.json 读取安装路径: {config.InstallPath}");
                        return config.InstallPath;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 读取 config.json 失败: {ex.Message}");
        }

        // 5. 使用默认路径
        const string defaultPath = @"C:\Program Files\DataForgeStudio";
        Debug.WriteLine($"[ConfigService] 使用默认安装路径: {defaultPath}");
        return defaultPath;
    }

    /// <summary>
    /// 向上查找文件
    /// </summary>
    /// <param name="startDir">起始目录</param>
    /// <param name="fileName">文件名</param>
    /// <param name="maxLevels">最大向上查找层数</param>
    /// <returns>文件完整路径，未找到返回 null</returns>
    private static string? FindFileUpwards(string startDir, string fileName, int maxLevels = 10)
    {
        var currentDir = startDir;
        for (int i = 0; i < maxLevels; i++)
        {
            var filePath = Path.Combine(currentDir, fileName);
            if (File.Exists(filePath))
            {
                return filePath;
            }

            var parent = Directory.GetParent(currentDir);
            if (parent == null)
            {
                break;
            }
            currentDir = parent.FullName;
        }
        return null;
    }

    /// <summary>
    /// 获取 appsettings.json 的完整路径
    /// </summary>
    public string GetAppSettingsPath()
    {
        // 检查安装路径是否已经包含 appsettings.json（开发环境）
        var directPath = Path.Combine(_installPath, "appsettings.json");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        // 否则查找 api 子目录（生产环境）
        return Path.Combine(_installPath, "api", "appsettings.json");
    }

    /// <summary>
    /// 加载配置文件
    /// 从 appsettings.json 读取端口和数据库配置，从 config.json 读取元信息
    /// </summary>
    /// <returns>部署配置对象</returns>
    public DeployConfig Load()
    {
        var config = new DeployConfig();

        try
        {
            // 从 appsettings.json 读取后端端口和数据库配置
            LoadFromAppSettings(config);

            // 从 config.json 读取元信息（安装路径、前端模式等）
            LoadFromLocalConfig(config);

            Debug.WriteLine($"[ConfigService] 成功加载配置");
            return config;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 加载配置文件失败: {ex.Message}");
            // 返回默认配置而不是抛出异常
            return config;
        }
    }

    /// <summary>
    /// 从 appsettings.json 读取配置
    /// </summary>
    private void LoadFromAppSettings(DeployConfig config)
    {
        var appSettingsPath = GetAppSettingsPath();
        if (!File.Exists(appSettingsPath))
        {
            Debug.WriteLine($"[ConfigService] appsettings.json 不存在: {appSettingsPath}");
            return;
        }

        try
        {
            var json = File.ReadAllText(appSettingsPath);
            var rootNode = JsonNode.Parse(json);
            if (rootNode == null) return;

            // 读取后端端口（从 Kestrel 配置）
            // Kestrel 配置格式: "Kestrel": { "Endpoints": { "Http": { "Url": "http://0.0.0.0:5000" } } }
            var kestrelNode = rootNode["Kestrel"];
            if (kestrelNode != null)
            {
                var endpointsNode = kestrelNode["Endpoints"];
                if (endpointsNode != null)
                {
                    var httpNode = endpointsNode["Http"];
                    if (httpNode != null)
                    {
                        var url = httpNode["Url"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(url))
                        {
                            // 从 URL 解析端口 (格式: http://0.0.0.0:5000)
                            var port = ExtractPortFromUrl(url);
                            if (port > 0)
                            {
                                config.Backend.Port = port;
                                Debug.WriteLine($"[ConfigService] 从 appsettings.json 读取后端端口: {port}");
                            }
                        }
                    }
                }
            }

            // 读取数据库连接字符串
            var connectionStringsNode = rootNode["ConnectionStrings"];
            if (connectionStringsNode != null)
            {
                var defaultConnection = connectionStringsNode["DefaultConnection"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(defaultConnection))
                {
                    var dbConfig = ParseConnectionString(defaultConnection);
                    config.Database = dbConfig;
                    Debug.WriteLine($"[ConfigService] 从 appsettings.json 读取数据库配置: {dbConfig.Server}, {dbConfig.Database}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 读取 appsettings.json 失败: {ex.Message}");
        }
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
    /// 从本地 config.json 读取元信息
    /// </summary>
    private void LoadFromLocalConfig(DeployConfig config)
    {
        if (!File.Exists(_configPath))
        {
            Debug.WriteLine($"[ConfigService] config.json 不存在: {_configPath}");
            return;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var localConfig = JsonSerializer.Deserialize<DeployConfig>(json, JsonOptions);
            if (localConfig == null) return;

            // 只读取元信息
            config.InstallPath = localConfig.InstallPath;
            config.Frontend.Mode = localConfig.Frontend.Mode;
            config.Frontend.IisSiteName = localConfig.Frontend.IisSiteName;
            config.Frontend.NginxPath = localConfig.Frontend.NginxPath;
            config.Frontend.Port = localConfig.Frontend.Port;
            config.Backend.ServiceName = localConfig.Backend.ServiceName;

            Debug.WriteLine($"[ConfigService] 从 config.json 读取元信息: 前端模式={config.Frontend.Mode}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 读取 config.json 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存配置到文件
    /// 同时更新 config.json（元信息）和 appsettings.json（实际配置）
    /// </summary>
    /// <param name="config">部署配置对象</param>
    /// <exception cref="ArgumentNullException">配置对象为空</exception>
    /// <exception cref="InvalidOperationException">保存失败</exception>
    public void Save(DeployConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            // 保存到 appsettings.json
            UpdateAppSettings(config);

            // 保存到 config.json（元信息）
            SaveLocalConfig(config);

            Debug.WriteLine($"[ConfigService] 配置已保存");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 保存配置文件失败: {ex.Message}");
            throw new InvalidOperationException($"保存配置文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 更新 appsettings.json
    /// </summary>
    private void UpdateAppSettings(DeployConfig config)
    {
        var appSettingsPath = GetAppSettingsPath();
        if (!File.Exists(appSettingsPath))
        {
            Debug.WriteLine($"[ConfigService] appsettings.json 不存在，跳过更新: {appSettingsPath}");
            return;
        }

        try
        {
            var json = File.ReadAllText(appSettingsPath);
            var rootNode = JsonNode.Parse(json);
            if (rootNode == null) return;

            // 更新后端端口（Kestrel 配置）
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

            httpNode["Url"] = $"http://0.0.0.0:{config.Backend.Port}";
            Debug.WriteLine($"[ConfigService] 更新后端端口: {config.Backend.Port}");

            // 更新数据库连接字符串
            var connectionStringsNode = rootNode["ConnectionStrings"];
            if (connectionStringsNode == null)
            {
                connectionStringsNode = new JsonObject();
                rootNode["ConnectionStrings"] = connectionStringsNode;
            }

            var connectionString = config.Database.GetConnectionString();
            connectionStringsNode["DefaultConnection"] = connectionString;

            // 同时更新 MasterConnection
            var masterConnectionString = BuildMasterConnectionString(config.Database);
            connectionStringsNode["MasterConnection"] = masterConnectionString;

            Debug.WriteLine($"[ConfigService] 更新数据库连接字符串");

            // 保存修改后的 JSON
            var serializerOptions = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = rootNode.ToJsonString(serializerOptions);
            File.WriteAllText(appSettingsPath, updatedJson, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 更新 appsettings.json 失败: {ex.Message}");
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

    /// <summary>
    /// 保存本地 config.json（仅元信息）
    /// </summary>
    private void SaveLocalConfig(DeployConfig config)
    {
        // 确保目录存在
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Debug.WriteLine($"[ConfigService] 创建配置目录: {directory}");
        }

        // 序列化并保存（使用 snake_case 格式）
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);

        Debug.WriteLine($"[ConfigService] 本地配置已保存: {_configPath}");
    }

    /// <summary>
    /// 更新后端端口到 appsettings.json
    /// </summary>
    /// <param name="port">端口号</param>
    public void UpdateBackendPort(int port)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "端口号必须在 1-65535 之间");
        }

        var appSettingsPath = GetAppSettingsPath();
        if (!File.Exists(appSettingsPath))
        {
            throw new FileNotFoundException($"appsettings.json 不存在: {appSettingsPath}");
        }

        try
        {
            var json = File.ReadAllText(appSettingsPath);
            var rootNode = JsonNode.Parse(json);
            if (rootNode == null) return;

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

            // 保存
            var serializerOptions = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = rootNode.ToJsonString(serializerOptions);
            File.WriteAllText(appSettingsPath, updatedJson, Encoding.UTF8);

            Debug.WriteLine($"[ConfigService] 后端端口已更新: {port}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 更新后端端口失败: {ex.Message}");
            throw new InvalidOperationException($"更新后端端口失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 更新数据库连接字符串到 appsettings.json
    /// </summary>
    /// <param name="config">数据库配置</param>
    public void UpdateDatabaseConfig(DatabaseConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var appSettingsPath = GetAppSettingsPath();
        if (!File.Exists(appSettingsPath))
        {
            throw new FileNotFoundException($"appsettings.json 不存在: {appSettingsPath}");
        }

        try
        {
            var json = File.ReadAllText(appSettingsPath);
            var rootNode = JsonNode.Parse(json);
            if (rootNode == null) return;

            // 更新连接字符串
            var connectionStringsNode = rootNode["ConnectionStrings"];
            if (connectionStringsNode == null)
            {
                connectionStringsNode = new JsonObject();
                rootNode["ConnectionStrings"] = connectionStringsNode;
            }

            var connectionString = config.GetConnectionString();
            connectionStringsNode["DefaultConnection"] = connectionString;

            // 同时更新 MasterConnection
            var masterConnectionString = BuildMasterConnectionString(config);
            connectionStringsNode["MasterConnection"] = masterConnectionString;

            // 保存
            var serializerOptions = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = rootNode.ToJsonString(serializerOptions);
            File.WriteAllText(appSettingsPath, updatedJson, Encoding.UTF8);

            Debug.WriteLine($"[ConfigService] 数据库配置已更新");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 更新数据库配置失败: {ex.Message}");
            throw new InvalidOperationException($"更新数据库配置失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从 appsettings.json 读取后端端口
    /// </summary>
    /// <returns>端口号，如果未配置则返回 5000</returns>
    public int GetBackendPort()
    {
        var appSettingsPath = GetAppSettingsPath();
        if (!File.Exists(appSettingsPath))
        {
            return 5000;
        }

        try
        {
            var json = File.ReadAllText(appSettingsPath);
            var rootNode = JsonNode.Parse(json);
            if (rootNode == null) return 5000;

            var kestrelNode = rootNode["Kestrel"];
            if (kestrelNode == null) return 5000;

            var endpointsNode = kestrelNode["Endpoints"];
            if (endpointsNode == null) return 5000;

            var httpNode = endpointsNode["Http"];
            if (httpNode == null) return 5000;

            var url = httpNode["Url"]?.GetValue<string>();
            if (string.IsNullOrEmpty(url)) return 5000;

            return ExtractPortFromUrl(url);
        }
        catch
        {
            return 5000;
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
    /// 检查配置文件是否存在
    /// </summary>
    /// <returns>如果配置文件存在返回 true，否则返回 false</returns>
    public bool ConfigExists()
    {
        return File.Exists(_configPath);
    }

    /// <summary>
    /// 删除配置文件
    /// </summary>
    public void DeleteConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                File.Delete(_configPath);
                Debug.WriteLine($"[ConfigService] 配置文件已删除: {_configPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 删除配置文件失败: {ex.Message}");
            throw new InvalidOperationException($"删除配置文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 备份配置文件
    /// </summary>
    /// <param name="backupPath">备份文件路径（可选，默认在原路径添加 .bak 后缀）</param>
    public void BackupConfig(string? backupPath = null)
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                Debug.WriteLine("[ConfigService] 配置文件不存在，无需备份");
                return;
            }

            backupPath ??= $"{_configPath}.bak";
            File.Copy(_configPath, backupPath, overwrite: true);

            Debug.WriteLine($"[ConfigService] 配置文件已备份: {backupPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 备份配置文件失败: {ex.Message}");
            throw new InvalidOperationException($"备份配置文件失败: {ex.Message}", ex);
        }
    }
}
