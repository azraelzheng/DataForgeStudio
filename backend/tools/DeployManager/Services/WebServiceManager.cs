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

        _mode = _configService.GetFrontendMode();
        _iisSiteName = "DataForgeStudio";  // IIS 站点名称硬编码
        _nginxPath = _configService.GetNginxPath();

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
                ObjectState.Starting => ServiceStatus.Running,  // Starting 状态也视为运行中
                ObjectState.Stopped => ServiceStatus.Stopped,
                ObjectState.Stopping => ServiceStatus.Stopped,  // Stopping 状态视为已停止
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

            // 等待站点完全启动
            int retryCount = 0;
            const int maxRetries = 10;
            while (retryCount < maxRetries)
            {
                // 重新获取站点状态（需要新的 ServerManager 实例）
                using var checkManager = new ServerManager();
                var checkSite = checkManager.Sites.FirstOrDefault(s =>
                    s.Name.Equals(_iisSiteName, StringComparison.OrdinalIgnoreCase));

                if (checkSite != null && checkSite.State == ObjectState.Started)
                {
                    Debug.WriteLine($"[WebServiceManager] IIS 站点 '{_iisSiteName}' 启动成功");
                    return;
                }

                retryCount++;
                await Task.Delay(500);
            }

            Debug.WriteLine($"[WebServiceManager] IIS 站点 '{_iisSiteName}' 启动命令已发送，等待状态同步");
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

            // 检查是否已有 Nginx 进程在运行
            if (IsNginxRunning())
            {
                Debug.WriteLine($"[WebServiceManager] Nginx 已在运行中");
                return;
            }

            // 检查配置文件是否存在
            var nginxConfPath = Path.Combine(_nginxPath, "conf", "nginx.conf");
            if (!File.Exists(nginxConfPath))
            {
                throw new InvalidOperationException($"Nginx 配置文件不存在: {nginxConfPath}");
            }

            Debug.WriteLine($"[WebServiceManager] 正在启动 Nginx...");

            var startInfo = new ProcessStartInfo
            {
                FileName = nginxExe,
                WorkingDirectory = _nginxPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("无法启动 Nginx 进程");
            }

            // 异步读取输出流，避免死锁
            var stderrTask = process.StandardError.ReadToEndAsync();

            // 等待进程完成（Nginx 主进程会快速退出，这是正常的）
            process.WaitForExit(5000);

            // 获取错误输出
            var errorOutput = await stderrTask;

            // 等待 Nginx 完全启动（master 和 worker 进程）
            await Task.Delay(500);  // 从 2000ms 减少到 500ms

            // 检查 Nginx 是否真正启动成功
            int retryCount = 0;
            const int maxRetries = 5;
            while (retryCount < maxRetries)
            {
                if (IsNginxRunning())
                {
                    Debug.WriteLine($"[WebServiceManager] Nginx 启动成功");
                    return;
                }
                retryCount++;
                await Task.Delay(300);  // 从 1000ms 减少到 300ms
            }

            // 如果启动失败，尝试获取更详细的错误信息
            if (!IsNginxRunning())
            {
                // 尝试读取 Nginx 错误日志
                var errorLogPath = Path.Combine(_nginxPath, "logs", "error.log");
                var errorLogContent = "";
                if (File.Exists(errorLogPath))
                {
                    try
                    {
                        // 读取最后几行错误日志
                        var lines = File.ReadAllLines(errorLogPath);
                        var lastLines = lines.TakeLast(5).ToArray();
                        errorLogContent = string.Join("\n", lastLines);
                    }
                    catch { }
                }

                var errorMessage = !string.IsNullOrEmpty(errorOutput)
                    ? $"Nginx 启动失败: {errorOutput}"
                    : !string.IsNullOrEmpty(errorLogContent)
                        ? $"Nginx 启动失败，错误日志:\n{errorLogContent}"
                        : "Nginx 启动失败，请检查配置文件";

                throw new InvalidOperationException(errorMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 启动 Nginx 失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 检查 Nginx 进程是否正在运行
    /// </summary>
    private static bool IsNginxRunning()
    {
        try
        {
            var processes = Process.GetProcessesByName("nginx");
            var isRunning = processes.Length > 0;

            // 释放进程资源
            foreach (var process in processes)
            {
                process.Dispose();
            }

            return isRunning;
        }
        catch
        {
            return false;
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

            // 等待站点完全停止
            int retryCount = 0;
            const int maxRetries = 10;
            while (retryCount < maxRetries)
            {
                // 重新获取站点状态（需要新的 ServerManager 实例）
                using var checkManager = new ServerManager();
                var checkSite = checkManager.Sites.FirstOrDefault(s =>
                    s.Name.Equals(_iisSiteName, StringComparison.OrdinalIgnoreCase));

                if (checkSite != null && checkSite.State == ObjectState.Stopped)
                {
                    Debug.WriteLine($"[WebServiceManager] IIS 站点 '{_iisSiteName}' 停止成功");
                    return;
                }

                retryCount++;
                await Task.Delay(500);
            }

            Debug.WriteLine($"[WebServiceManager] IIS 站点 '{_iisSiteName}' 停止命令已发送，等待状态同步");
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
