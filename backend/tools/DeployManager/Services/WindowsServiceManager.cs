using System.Diagnostics;
using System.ServiceProcess;
using DeployManager.Models;

namespace DeployManager.Services;

/// <summary>
/// Windows 服务管理器实现
/// 使用 System.ServiceProcess.ServiceController 管理 Windows 服务
/// </summary>
public class WindowsServiceManager : IWindowsServiceManager
{
    private readonly string _serviceName;
    private ServiceController? _controller;
    private bool _disposed = false;

    /// <summary>
    /// 初始化 Windows 服务管理器（从配置服务读取服务名）
    /// </summary>
    /// <param name="configService">配置服务</param>
    public WindowsServiceManager(IConfigService configService)
    {
        if (configService == null)
            throw new ArgumentNullException(nameof(configService));

        var config = configService.Load();
        _serviceName = config.Backend.ServiceName ?? "DataForgeStudio API";
    }

    /// <summary>
    /// 获取或创建 ServiceController 实例
    /// </summary>
    private ServiceController GetController()
    {
        return _controller ??= new ServiceController(_serviceName);
    }

    /// <summary>
    /// 获取服务当前状态
    /// </summary>
    /// <returns>服务状态枚举值</returns>
    public ServiceStatus GetStatus()
    {
        try
        {
            if (!IsServiceInstalled())
            {
                Debug.WriteLine($"[WindowsServiceManager] 服务 '{_serviceName}' 未安装");
                return ServiceStatus.Unknown;
            }

            var controller = GetController();
            controller.Refresh();

            var status = controller.Status switch
            {
                ServiceControllerStatus.Running => ServiceStatus.Running,
                ServiceControllerStatus.Stopped => ServiceStatus.Stopped,
                ServiceControllerStatus.StartPending => ServiceStatus.Running,
                ServiceControllerStatus.StopPending => ServiceStatus.Stopped,
                ServiceControllerStatus.ContinuePending => ServiceStatus.Running,
                ServiceControllerStatus.PausePending => ServiceStatus.Stopped,
                ServiceControllerStatus.Paused => ServiceStatus.Stopped,
                _ => ServiceStatus.Unknown
            };

            Debug.WriteLine($"[WindowsServiceManager] 服务 '{_serviceName}' 状态: {status}");
            return status;
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"[WindowsServiceManager] 获取服务状态失败: {ex.Message}");
            return ServiceStatus.Unknown;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowsServiceManager] 获取服务状态时发生异常: {ex.Message}");
            return ServiceStatus.Unknown;
        }
    }

    /// <summary>
    /// 启动服务
    /// </summary>
    /// <exception cref="InvalidOperationException">服务未安装或无法启动</exception>
    public async Task StartAsync()
    {
        try
        {
            if (!IsServiceInstalled())
            {
                throw new InvalidOperationException($"服务 '{_serviceName}' 未安装");
            }

            var controller = GetController();
            controller.Refresh();

            if (controller.Status == ServiceControllerStatus.Running)
            {
                Debug.WriteLine($"[WindowsServiceManager] 服务 '{_serviceName}' 已在运行中");
                return;
            }

            Debug.WriteLine($"[WindowsServiceManager] 正在启动服务 '{_serviceName}'...");
            controller.Start();

            // 等待服务启动完成，超时时间 30 秒
            await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));

            Debug.WriteLine($"[WindowsServiceManager] 服务 '{_serviceName}' 启动成功");
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"[WindowsServiceManager] 启动服务失败: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowsServiceManager] 启动服务时发生异常: {ex.Message}");
            throw new InvalidOperationException($"启动服务失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 停止服务
    /// </summary>
    /// <exception cref="InvalidOperationException">服务未安装或无法停止</exception>
    public async Task StopAsync()
    {
        try
        {
            if (!IsServiceInstalled())
            {
                throw new InvalidOperationException($"服务 '{_serviceName}' 未安装");
            }

            var controller = GetController();
            controller.Refresh();

            if (controller.Status == ServiceControllerStatus.Stopped)
            {
                Debug.WriteLine($"[WindowsServiceManager] 服务 '{_serviceName}' 已停止");
                return;
            }

            // 检查服务是否可以停止
            if (!controller.CanStop)
            {
                throw new InvalidOperationException($"服务 '{_serviceName}' 无法停止");
            }

            Debug.WriteLine($"[WindowsServiceManager] 正在停止服务 '{_serviceName}'...");
            controller.Stop();

            // 等待服务停止完成，超时时间 30 秒
            await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));

            Debug.WriteLine($"[WindowsServiceManager] 服务 '{_serviceName}' 停止成功");
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"[WindowsServiceManager] 停止服务失败: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowsServiceManager] 停止服务时发生异常: {ex.Message}");
            throw new InvalidOperationException($"停止服务失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 重启服务
    /// </summary>
    /// <exception cref="InvalidOperationException">服务未安装或无法重启</exception>
    public async Task RestartAsync()
    {
        try
        {
            Debug.WriteLine($"[WindowsServiceManager] 正在重启服务 '{_serviceName}'...");

            // 先停止服务
            await StopAsync();

            // 等待一小段时间确保服务完全停止
            await Task.Delay(1000);

            // 再启动服务
            await StartAsync();

            Debug.WriteLine($"[WindowsServiceManager] 服务 '{_serviceName}' 重启成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowsServiceManager] 重启服务失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 检查服务是否已安装
    /// </summary>
    /// <returns>如果服务已安装返回 true，否则返回 false</returns>
    public bool IsServiceInstalled()
    {
        try
        {
            // 获取所有服务并检查目标服务是否存在
            var services = ServiceController.GetServices();
            var exists = services.Any(s => s.ServiceName.Equals(_serviceName, StringComparison.OrdinalIgnoreCase));

            Debug.WriteLine($"[WindowsServiceManager] 检查服务 '{_serviceName}' 是否安装: {exists}");
            return exists;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowsServiceManager] 检查服务安装状态时发生异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源的核心方法
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
                _controller?.Dispose();
                _controller = null;
            }
            _disposed = true;
        }
    }
}
