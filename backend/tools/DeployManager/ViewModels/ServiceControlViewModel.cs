using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployManager.Models;
using DeployManager.Services;

namespace DeployManager.ViewModels;

/// <summary>
/// 服务控制视图模型
/// 管理两个服务：
/// - AppService: 后端 API 服务（DFAppService）
/// - WebService: 前端服务（DFWebService，支持 IIS 或 Nginx）
/// </summary>
public partial class ServiceControlViewModel : ObservableObject, IDisposable
{
    private readonly IWindowsServiceManager _appServiceManager;
    private readonly IWebServiceManager _webServiceManager;
    private readonly IConfigService _configService;
    private System.Timers.Timer? _refreshTimer;
    private DateTime? _appProcessStartTime;
    private Process? _appServiceProcess;
    private bool _disposed = false;

    #region AppService 属性（后端服务）

    /// <summary>
    /// AppService 是否正在运行
    /// </summary>
    [ObservableProperty]
    private bool _isAppRunning;

    /// <summary>
    /// AppService 状态文本
    /// </summary>
    [ObservableProperty]
    private string _appStatusText = "未运行";

    /// <summary>
    /// AppService 启动时间
    /// </summary>
    [ObservableProperty]
    private string _appStartTimeText = "-";

    /// <summary>
    /// AppService 内存使用量
    /// </summary>
    [ObservableProperty]
    private string _appMemoryUsage = "-";

    #endregion

    #region WebService 属性（前端服务）

    /// <summary>
    /// WebService 是否正在运行
    /// </summary>
    [ObservableProperty]
    private bool _isWebRunning;

    /// <summary>
    /// WebService 状态文本
    /// </summary>
    [ObservableProperty]
    private string _webStatusText = "未运行";

    /// <summary>
    /// WebService 描述文本（IIS/Nginx）
    /// </summary>
    [ObservableProperty]
    private string _webServiceType = "IIS";

    /// <summary>
    /// 是否是 IIS 模式
    /// </summary>
    [ObservableProperty]
    private bool _isIisMode = true;

    /// <summary>
    /// 前端服务提示信息
    /// </summary>
    public string WebServiceHint => IsIisMode
        ? "IIS 模式：站点由 IIS 服务管理，通常无需手动启停"
        : "Nginx 模式：独立进程运行，可手动启停服务";

    #endregion

    #region 通用属性

    /// <summary>
    /// 是否开机自启
    /// </summary>
    [ObservableProperty]
    private bool _autoStart;

    /// <summary>
    /// AppService 操作是否正在进行中
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartAppCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopAppCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartAppCommand))]
    private bool _isAppOperating;

    /// <summary>
    /// WebService 操作是否正在进行中
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartWebCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopWebCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartWebCommand))]
    private bool _isWebOperating;

    #endregion

    /// <summary>
    /// 初始化服务控制视图模型
    /// </summary>
    /// <param name="appServiceManager">后端服务管理器</param>
    /// <param name="webServiceManager">前端服务管理器</param>
    /// <param name="configService">配置服务</param>
    public ServiceControlViewModel(
        IWindowsServiceManager appServiceManager,
        IWebServiceManager webServiceManager,
        IConfigService configService)
    {
        _appServiceManager = appServiceManager;
        _webServiceManager = webServiceManager;
        _configService = configService;

        // 初始化定时器，每 5 秒刷新状态
        _refreshTimer = new System.Timers.Timer(5000);
        _refreshTimer.Elapsed += async (s, e) => await RefreshStatusAsync();

        // 加载配置
        LoadConfig();
    }

    /// <summary>
    /// 启动定时刷新
    /// </summary>
    public void StartRefresh()
    {
        RefreshStatusAsync().ConfigureAwait(false);
        _refreshTimer?.Start();
    }

    /// <summary>
    /// 停止定时刷新
    /// </summary>
    public void StopRefresh()
    {
        if (_refreshTimer != null)
        {
            _refreshTimer.Stop();
            _refreshTimer.Elapsed -= async (s, e) => await RefreshStatusAsync();
            _refreshTimer.Dispose();
            _refreshTimer = null;
        }
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            var frontendMode = _configService.GetFrontendMode();
            IsIisMode = frontendMode.Equals("iis", StringComparison.OrdinalIgnoreCase);
            WebServiceType = IsIisMode ? "IIS" : "Nginx";
            AutoStart = false; // TODO: 从配置加载
        }
        catch (Exception)
        {
            IsIisMode = true;
            WebServiceType = "IIS";
            AutoStart = false;
        }
    }

    /// <summary>
    /// 刷新服务状态
    /// </summary>
    private async Task RefreshStatusAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                // 刷新 AppService 状态
                RefreshAppServiceStatus();

                // 刷新 WebService 状态
                RefreshWebServiceStatus();
            });
        }
        catch (Exception)
        {
            // 忽略刷新错误
        }
    }

    /// <summary>
    /// 刷新后端服务状态
    /// </summary>
    private void RefreshAppServiceStatus()
    {
        try
        {
            var status = _appServiceManager.GetStatus();
            IsAppRunning = status == ServiceStatus.Running;
            AppStatusText = status switch
            {
                ServiceStatus.Running => "运行中",
                ServiceStatus.Stopped => "已停止",
                _ => "未知"
            };

            // 更新进程信息
            UpdateAppProcessInfo();
        }
        catch (Exception)
        {
            IsAppRunning = false;
            AppStatusText = "无法获取状态";
        }
    }

    /// <summary>
    /// 刷新前端服务状态
    /// </summary>
    private void RefreshWebServiceStatus()
    {
        try
        {
            var status = _webServiceManager.GetStatus();
            IsWebRunning = status == ServiceStatus.Running;
            WebStatusText = status switch
            {
                ServiceStatus.Running => "运行中",
                ServiceStatus.Stopped => "已停止",
                _ => "未知"
            };
        }
        catch (Exception)
        {
            IsWebRunning = false;
            WebStatusText = "无法获取状态";
        }
    }

    /// <summary>
    /// 更新后端进程信息
    /// </summary>
    private void UpdateAppProcessInfo()
    {
        try
        {
            var processName = "DataForgeStudio.Api";

            var processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                _appServiceProcess = processes[0];
                _appProcessStartTime = _appServiceProcess.StartTime;
                AppStartTimeText = _appProcessStartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

                var memoryMB = _appServiceProcess.WorkingSet64 / 1024.0 / 1024.0;
                AppMemoryUsage = $"{memoryMB:F2} MB";
            }
            else
            {
                _appServiceProcess = null;
                _appProcessStartTime = null;
                AppStartTimeText = "-";
                AppMemoryUsage = "-";
            }
        }
        catch (Exception)
        {
            AppStartTimeText = "-";
            AppMemoryUsage = "-";
        }
    }

    #region AppService 命令

    /// <summary>
    /// 启动后端服务命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperateApp))]
    private async Task StartAppAsync()
    {
        if (IsAppOperating) return;

        IsAppOperating = true;
        AppStatusText = "正在启动...";

        try
        {
            await _appServiceManager.StartAsync();
            RefreshAppServiceStatus();
        }
        catch (Exception ex)
        {
            AppStatusText = $"启动失败: {ex.Message}";
        }
        finally
        {
            IsAppOperating = false;
        }
    }

    /// <summary>
    /// 停止后端服务命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperateApp))]
    private async Task StopAppAsync()
    {
        if (IsAppOperating) return;

        IsAppOperating = true;
        AppStatusText = "正在停止...";

        try
        {
            await _appServiceManager.StopAsync();
            RefreshAppServiceStatus();
        }
        catch (Exception ex)
        {
            AppStatusText = $"停止失败: {ex.Message}";
        }
        finally
        {
            IsAppOperating = false;
        }
    }

    /// <summary>
    /// 重启后端服务命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperateApp))]
    private async Task RestartAppAsync()
    {
        if (IsAppOperating) return;

        IsAppOperating = true;
        AppStatusText = "正在重启...";

        try
        {
            await _appServiceManager.RestartAsync();
            RefreshAppServiceStatus();
        }
        catch (Exception ex)
        {
            AppStatusText = $"重启失败: {ex.Message}";
        }
        finally
        {
            IsAppOperating = false;
        }
    }

    /// <summary>
    /// 是否可以执行 AppService 操作
    /// </summary>
    private bool CanOperateApp() => !IsAppOperating;

    #endregion

    #region WebService 命令

    /// <summary>
    /// 启动前端服务命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperateWeb))]
    private async Task StartWebAsync()
    {
        if (IsWebOperating) return;

        IsWebOperating = true;
        WebStatusText = "正在启动...";

        try
        {
            await _webServiceManager.StartAsync();
            RefreshWebServiceStatus();
        }
        catch (Exception ex)
        {
            WebStatusText = $"启动失败: {ex.Message}";
        }
        finally
        {
            IsWebOperating = false;
        }
    }

    /// <summary>
    /// 停止前端服务命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperateWeb))]
    private async Task StopWebAsync()
    {
        if (IsWebOperating) return;

        IsWebOperating = true;
        WebStatusText = "正在停止...";

        try
        {
            await _webServiceManager.StopAsync();
            RefreshWebServiceStatus();
        }
        catch (Exception ex)
        {
            WebStatusText = $"停止失败: {ex.Message}";
        }
        finally
        {
            IsWebOperating = false;
        }
    }

    /// <summary>
    /// 重启前端服务命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperateWeb))]
    private async Task RestartWebAsync()
    {
        if (IsWebOperating) return;

        IsWebOperating = true;
        WebStatusText = "正在重启...";

        try
        {
            await _webServiceManager.RestartAsync();
            RefreshWebServiceStatus();
        }
        catch (Exception ex)
        {
            WebStatusText = $"重启失败: {ex.Message}";
        }
        finally
        {
            IsWebOperating = false;
        }
    }

    /// <summary>
    /// 是否可以执行 WebService 操作
    /// </summary>
    private bool CanOperateWeb() => !IsWebOperating;

    #endregion

    /// <summary>
    /// AutoStart 属性变更时的处理
    /// </summary>
    partial void OnAutoStartChanged(bool value)
    {
        // TODO: 保存自启动设置到配置
        SaveAutoStartSetting(value);
    }

    /// <summary>
    /// 保存自启动设置
    /// </summary>
    private void SaveAutoStartSetting(bool autoStart)
    {
        try
        {
            // TODO: 在 IConfigService 中添加 SaveAutoStart 方法后保存
            // _configService.SaveAutoStart(autoStart);
        }
        catch (Exception)
        {
            // 忽略保存错误
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
                // 停止并释放 Timer
                StopRefresh();
                // 释放 Process 资源
                _appServiceProcess?.Dispose();
                _appServiceProcess = null;
                // 释放服务管理器资源
                _appServiceManager?.Dispose();
                _webServiceManager?.Dispose();
            }
            _disposed = true;
        }
    }
}
