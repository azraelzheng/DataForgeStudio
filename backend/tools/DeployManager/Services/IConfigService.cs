using DeployManager.Models;

namespace DeployManager.Services;

/// <summary>
/// 配置服务接口
///
/// 配置存储位置：
/// - appsettings.json: 后端端口、数据库连接字符串
/// - nginx.conf: 前端端口（nginx 模式）
/// - IIS: 前端端口（iis 模式）
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// 服务名称常量
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// 安装路径
    /// </summary>
    string InstallPath { get; }

    // ========== 读取方法 ==========

    /// <summary>
    /// 获取 appsettings.json 的完整路径
    /// </summary>
    string GetAppSettingsPath();

    /// <summary>
    /// 从 appsettings.json 读取后端端口
    /// </summary>
    /// <returns>端口号，如果未配置则返回 5000</returns>
    int GetBackendPort();

    /// <summary>
    /// 从 nginx.conf 读取前端端口
    /// </summary>
    /// <returns>端口号，如果未配置则返回 80</returns>
    int GetFrontendPort();

    /// <summary>
    /// 获取前端模式 (nginx/iis)
    /// </summary>
    string GetFrontendMode();

    /// <summary>
    /// 从 appsettings.json 读取数据库配置
    /// </summary>
    /// <returns>数据库配置对象</returns>
    DatabaseConfig GetDatabaseConfig();

    /// <summary>
    /// 获取 Nginx 路径
    /// </summary>
    string GetNginxPath();

    /// <summary>
    /// 获取 WebSite 路径
    /// </summary>
    string GetWebSitePath();

    // ========== 保存方法 ==========

    /// <summary>
    /// 保存后端端口到 appsettings.json
    /// </summary>
    /// <param name="port">端口号</param>
    void SaveBackendPort(int port);

    /// <summary>
    /// 保存前端端口到 nginx.conf
    /// </summary>
    /// <param name="port">端口号</param>
    void SaveFrontendPort(int port);

    /// <summary>
    /// 保存数据库连接字符串到 appsettings.json
    /// </summary>
    /// <param name="config">数据库配置</param>
    void SaveDatabaseConfig(DatabaseConfig config);
}
