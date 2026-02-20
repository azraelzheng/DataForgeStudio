using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using DeployManager.Models;
using Microsoft.Web.Administration;

namespace DeployManager.Services;

/// <summary>
/// 前端服务管理器实现
/// 支持 IIS 和 Nginx 两种模式
/// </summary>
public class WebServiceManager : IWebServiceManager
{
    private readonly IConfigService _configService;
    private readonly string _mode;
    private readonly string _iisSiteName;
    private readonly string _nginxPath;
    private bool _disposed = false;

    /// <summary>
    /// 获取服务类型（IIS 或 Nginx）
    /// </summary>
    public string ServiceType => _mode.Equals("nginx", StringComparison.OrdinalIgnoreCase) ? "Nginx" : "IIS";

    /// <summary>
    /// 初始化前端服务管理器
    /// </summary>
    /// <param name="configService">配置服务</param>
    public WebServiceManager(IConfigService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));

        var config = _configService.Load();
        _mode = config.Frontend.Mode ?? "iis";
        _iisSiteName = config.Frontend.IisSiteName ?? "DataForgeStudio";
        _nginxPath = config.Frontend.NginxPath ?? "";

        Debug.WriteLine($"[WebServiceManager] 初始化，模式: {_mode}, IIS站点: {_iisSiteName}, Nginx路径: {_nginxPath}");
    }

    /// <summary>
    /// 获取服务当前状态
    /// </summary>
    /// <returns>服务状态枚举值</returns>
    public ServiceStatus GetStatus()
    {
        try
        {
            if (_mode.Equals("nginx", StringComparison.OrdinalIgnoreCase))
            {
                return GetNginxStatus();
            }
            else
            {
                return GetIisStatus();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 获取服务状态失败: {ex.Message}");
            return ServiceStatus.Unknown;
        }
    }

    /// <summary>
    /// 获取 IIS 站点状态
    /// </summary>
    private ServiceStatus GetIisStatus()
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites.FirstOrDefault(s =>
                s.Name.Equals(_iisSiteName, StringComparison.OrdinalIgnoreCase));

            if (site == null)
            {
                Debug.WriteLine($"[WebServiceManager] IIS 站点 '{_iisSiteName}' 不存在");
                return ServiceStatus.Unknown;
            }

            var state = site.State;
            Debug.WriteLine($"[WebServiceManager] IIS 站点 '{_iisSiteName}' 状态: {state}");

            return state switch
            {
                ObjectState.Started => ServiceStatus.Running,
                ObjectState.Stopped => ServiceStatus.Stopped,
                _ => ServiceStatus.Unknown
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 获取 IIS 状态失败: {ex.Message}");
            return ServiceStatus.Unknown;
        }
    }

    /// <summary>
    /// 获取 Nginx 进程状态
    /// </summary>
    private ServiceStatus GetNginxStatus()
    {
        try
        {
            // 检查 nginx 进程是否存在
            var processes = Process.GetProcessesByName("nginx");
            var isRunning = processes.Length > 0;

            // 释放进程资源
            foreach (var process in processes)
            {
                process.Dispose();
            }

            Debug.WriteLine($"[WebServiceManager] Nginx 进程数: {processes.Length}, 运行中: {isRunning}");
            return isRunning ? ServiceStatus.Running : ServiceStatus.Stopped;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 获取 Nginx 状态失败: {ex.Message}");
            return ServiceStatus.Unknown;
        }
    }

    /// <summary>
    /// 启动服务
    /// </summary>
    public async Task StartAsync()
    {
        if (_mode.Equals("nginx", StringComparison.OrdinalIgnoreCase))
        {
            await StartNginxAsync();
        }
        else
        {
            await StartIisAsync();
        }
    }

    /// <summary>
    /// 启动 IIS 站点
    /// </summary>
    private async Task StartIisAsync()
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites.FirstOrDefault(s =>
                s.Name.Equals(_iisSiteName, StringComparison.OrdinalIgnoreCase));

            if (site == null)
            {
                throw new InvalidOperationException($"IIS 站点 '{_iisSiteName}' 不存在");
            }

            if (site.State == ObjectState.Started)
            {
                Debug.WriteLine($"[WebServiceManager] IIS 站点 '{_iisSiteName}' 已在运行中");
                return;
            }

            Debug.WriteLine($"[WebServiceManager] 正在启动 IIS 站点 '{_iisSiteName}'...");
            site.Start();
            serverManager.CommitChanges();

            await Task.CompletedTask;
            Debug.WriteLine($"[WebServiceManager] IIS 站点 '{_iisSiteName}' 启动成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 启动 IIS 站点失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 启动 Nginx
    /// </summary>
    private async Task StartNginxAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_nginxPath) || !Directory.Exists(_nginxPath))
            {
                throw new InvalidOperationException("Nginx 路径未配置或不存在");
            }

            var nginxExe = Path.Combine(_nginxPath, "nginx.exe");
            if (!File.Exists(nginxExe))
            {
                throw new InvalidOperationException($"Nginx 可执行文件不存在: {nginxExe}");
            }

            Debug.WriteLine($"[WebServiceManager] 正在启动 Nginx...");

            var startInfo = new ProcessStartInfo
            {
                FileName = nginxExe,
                WorkingDirectory = _nginxPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            process?.WaitForExit(5000);

            await Task.Delay(1000); // 等待进程启动
            Debug.WriteLine($"[WebServiceManager] Nginx 启动命令已执行");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 启动 Nginx 失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 停止服务
    /// </summary>
    public async Task StopAsync()
    {
        if (_mode.Equals("nginx", StringComparison.OrdinalIgnoreCase))
        {
            await StopNginxAsync();
        }
        else
        {
            await StopIisAsync();
        }
    }

    /// <summary>
    /// 停止 IIS 站点
    /// </summary>
    private async Task StopIisAsync()
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites.FirstOrDefault(s =>
                s.Name.Equals(_iisSiteName, StringComparison.OrdinalIgnoreCase));

            if (site == null)
            {
                throw new InvalidOperationException($"IIS 站点 '{_iisSiteName}' 不存在");
            }

            if (site.State == ObjectState.Stopped)
            {
                Debug.WriteLine($"[WebServiceManager] IIS 站点 '{_iisSiteName}' 已停止");
                return;
            }

            Debug.WriteLine($"[WebServiceManager] 正在停止 IIS 站点 '{_iisSiteName}'...");
            site.Stop();
            serverManager.CommitChanges();

            await Task.CompletedTask;
            Debug.WriteLine($"[WebServiceManager] IIS 站点 '{_iisSiteName}' 停止成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 停止 IIS 站点失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 停止 Nginx
    /// </summary>
    private async Task StopNginxAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_nginxPath) || !Directory.Exists(_nginxPath))
            {
                throw new InvalidOperationException("Nginx 路径未配置或不存在");
            }

            var nginxExe = Path.Combine(_nginxPath, "nginx.exe");
            if (!File.Exists(nginxExe))
            {
                throw new InvalidOperationException($"Nginx 可执行文件不存在: {nginxExe}");
            }

            Debug.WriteLine($"[WebServiceManager] 正在停止 Nginx...");

            // 使用 nginx -s quit 优雅停止
            var startInfo = new ProcessStartInfo
            {
                FileName = nginxExe,
                Arguments = "-s quit",
                WorkingDirectory = _nginxPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            process?.WaitForExit(5000);

            await Task.Delay(1000); // 等待进程停止

            // 如果还有进程在运行，强制终止
            var remainingProcesses = Process.GetProcessesByName("nginx");
            if (remainingProcesses.Length > 0)
            {
                Debug.WriteLine($"[WebServiceManager] 还有 {remainingProcesses.Length} 个 Nginx 进程，强制终止...");
                foreach (var p in remainingProcesses)
                {
                    try
                    {
                        p.Kill();
                    }
                    catch { }
                    p.Dispose();
                }
            }

            Debug.WriteLine($"[WebServiceManager] Nginx 停止成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 停止 Nginx 失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 重启服务
    /// </summary>
    public async Task RestartAsync()
    {
        if (_mode.Equals("nginx", StringComparison.OrdinalIgnoreCase))
        {
            await RestartNginxAsync();
        }
        else
        {
            await RestartIisAsync();
        }
    }

    /// <summary>
    /// 重启 IIS 站点
    /// </summary>
    private async Task RestartIisAsync()
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites.FirstOrDefault(s =>
                s.Name.Equals(_iisSiteName, StringComparison.OrdinalIgnoreCase));

            if (site == null)
            {
                throw new InvalidOperationException($"IIS 站点 '{_iisSiteName}' 不存在");
            }

            Debug.WriteLine($"[WebServiceManager] 正在重启 IIS 站点 '{_iisSiteName}'...");

            if (site.State != ObjectState.Stopped)
            {
                site.Stop();
            }

            await Task.Delay(1000);
            site.Start();
            serverManager.CommitChanges();

            Debug.WriteLine($"[WebServiceManager] IIS 站点 '{_iisSiteName}' 重启成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 重启 IIS 站点失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 重启 Nginx
    /// </summary>
    private async Task RestartNginxAsync()
    {
        try
        {
            Debug.WriteLine($"[WebServiceManager] 正在重启 Nginx...");
            await StopNginxAsync();
            await Task.Delay(1000);
            await StartNginxAsync();
            Debug.WriteLine($"[WebServiceManager] Nginx 重启成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 重启 Nginx 失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 检查服务是否已配置
    /// </summary>
    public bool IsServiceConfigured()
    {
        try
        {
            if (_mode.Equals("nginx", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrEmpty(_nginxPath) && Directory.Exists(_nginxPath);
            }
            else
            {
                using var serverManager = new ServerManager();
                var site = serverManager.Sites.FirstOrDefault(s =>
                    s.Name.Equals(_iisSiteName, StringComparison.OrdinalIgnoreCase));
                return site != null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 检查服务配置失败: {ex.Message}");
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
            }
            _disposed = true;
        }
    }
}
