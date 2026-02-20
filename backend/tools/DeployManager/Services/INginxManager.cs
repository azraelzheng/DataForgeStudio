namespace DeployManager.Services;

/// <summary>
/// Nginx 管理接口
/// </summary>
public interface INginxManager
{
    /// <summary>
    /// 检查 Nginx 是否已安装
    /// </summary>
    bool IsNginxInstalled();

    /// <summary>
    /// 启动 Nginx
    /// </summary>
    Task StartAsync(string configPath);

    /// <summary>
    /// 停止 Nginx
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 更新配置文件
    /// </summary>
    void UpdateConfig(string configPath, int port, string backendUrl);
}
