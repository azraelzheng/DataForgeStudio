using DeployManager.Models;

namespace DeployManager.Services;

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// 加载配置（从 config.json 和 appsettings.json 合并读取）
    /// </summary>
    DeployConfig Load();

    /// <summary>
    /// 保存配置（config.json 保存元信息，appsettings.json 保存实际配置）
    /// </summary>
    void Save(DeployConfig config);

    /// <summary>
    /// 配置文件路径（本地缓存文件）
    /// </summary>
    string ConfigPath { get; }

    /// <summary>
    /// 安装路径
    /// </summary>
    string InstallPath { get; }

    /// <summary>
    /// 获取 appsettings.json 的完整路径
    /// </summary>
    string GetAppSettingsPath();

    /// <summary>
    /// 更新后端端口到 appsettings.json
    /// </summary>
    /// <param name="port">端口号</param>
    void UpdateBackendPort(int port);

    /// <summary>
    /// 更新数据库连接字符串到 appsettings.json
    /// </summary>
    /// <param name="config">数据库配置</param>
    void UpdateDatabaseConfig(DatabaseConfig config);

    /// <summary>
    /// 从 appsettings.json 读取后端端口
    /// </summary>
    /// <returns>端口号，如果未配置则返回 5000</returns>
    int GetBackendPort();

    /// <summary>
    /// 从 appsettings.json 读取数据库配置
    /// </summary>
    /// <returns>数据库配置对象</returns>
    DatabaseConfig GetDatabaseConfig();
}
