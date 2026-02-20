using DeployManager.Models;

namespace DeployManager.Services;

/// <summary>
/// Windows 服务管理接口
/// </summary>
public interface IWindowsServiceManager
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
    /// 检查服务是否已安装
    /// </summary>
    bool IsServiceInstalled();
}
