using System.Diagnostics;
using System.IO;
using System.Text.Json;
using DeployManager.Models;

namespace DeployManager.Services;

/// <summary>
/// 配置服务实现
/// 使用 System.Text.Json 进行配置文件的读写
/// </summary>
public class ConfigService : IConfigService
{
    private readonly string _configPath;
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
    public ConfigService(string? configPath = null)
    {
        _configPath = configPath ?? GetDefaultConfigPath();
        Debug.WriteLine($"[ConfigService] 配置文件路径: {_configPath}");
    }

    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    public string ConfigPath => _configPath;

    /// <summary>
    /// 获取默认配置文件路径
    /// </summary>
    private static string GetDefaultConfigPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, "config.json");
    }

    /// <summary>
    /// 加载配置文件
    /// 如果文件不存在，返回默认配置
    /// </summary>
    /// <returns>部署配置对象</returns>
    public DeployConfig Load()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                Debug.WriteLine($"[ConfigService] 配置文件不存在，返回默认配置: {_configPath}");
                return new DeployConfig();
            }

            var json = File.ReadAllText(_configPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.WriteLine("[ConfigService] 配置文件为空，返回默认配置");
                return new DeployConfig();
            }

            var config = JsonSerializer.Deserialize<DeployConfig>(json, JsonOptions);

            if (config == null)
            {
                Debug.WriteLine("[ConfigService] 配置文件反序列化失败，返回默认配置");
                return new DeployConfig();
            }

            Debug.WriteLine($"[ConfigService] 成功加载配置文件: {_configPath}");
            return config;
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"[ConfigService] JSON 解析错误: {ex.Message}");
            throw new InvalidOperationException($"配置文件格式错误: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 加载配置文件失败: {ex.Message}");
            throw new InvalidOperationException($"加载配置文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 保存配置到文件
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
            // 确保目录存在
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.WriteLine($"[ConfigService] 创建配置目录: {directory}");
            }

            // 序列化并保存
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(_configPath, json);

            Debug.WriteLine($"[ConfigService] 配置已保存: {_configPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigService] 保存配置文件失败: {ex.Message}");
            throw new InvalidOperationException($"保存配置文件失败: {ex.Message}", ex);
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
