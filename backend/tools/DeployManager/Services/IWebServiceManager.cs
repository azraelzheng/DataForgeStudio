using DeployManager.Models;

namespace DeployManager.Services;

/// <summary>
/// 前端服务管理接口（IIS 或 Nginx）
/// </summary>
public interface IWebServiceManager : IDisposable
{
    /// <summary>
    /// 获取服务状态
    /// </summary>
    ServiceStatus GetStatus();

    /// <summary>
    /// 启动服务
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// 停止服务
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 重启服务
    /// </summary>
    Task RestartAsync();

    /// <summary>
    /// 检查服务是否已安装/配置
    /// </summary>
    bool IsServiceConfigured();

    /// <summary>
    /// 获取服务类型（IIS 或 Nginx）
    /// </summary>
    string ServiceType { get; }
}
