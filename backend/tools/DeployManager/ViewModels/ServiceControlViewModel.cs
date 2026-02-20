using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployManager.Models;
using DeployManager.Services;

namespace DeployManager.ViewModels;

/// <summary>
/// 服务控制视图模型
/// </summary>
public partial class ServiceControlViewModel : ObservableObject, IDisposable
{
    private readonly IWindowsServiceManager _serviceManager;
    private readonly IConfigService _configService;
    private System.Timers.Timer? _refreshTimer;
    private DateTime? _processStartTime;
    private Process? _serviceProcess;
    private bool _disposed = false;

    /// <summary>
    /// 服务是否正在运行
    /// </summary>
    [ObservableProperty]
    private bool _isRunning;

    /// <summary>
    /// 状态文本
    /// </summary>
    [ObservableProperty]
    private string _statusText = "未运行";

    /// <summary>
    /// 启动时间
    /// </summary>
    [ObservableProperty]
    private string _startTimeText = "-";

    /// <summary>
    /// 内存使用量
    /// </summary>
    [ObservableProperty]
    private string _memoryUsage = "-";

    /// <summary>
    /// 是否开机自启
    /// </summary>
    [ObservableProperty]
    private bool _autoStart;

    /// <summary>
    /// 操作是否正在进行中
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartCommand))]
    private bool _isOperating;

    /// <summary>
    /// 初始化服务控制视图模型
    /// </summary>
    /// <param name="serviceManager">Windows 服务管理器</param>
    /// <param name="configService">配置服务</param>
    public ServiceControlViewModel(IWindowsServiceManager serviceManager, IConfigService configService)
    {
        _serviceManager = serviceManager;
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
        // TODO: 从配置中加载自启动设置
        AutoStart = false;
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
                var status = _serviceManager.GetStatus();
                IsRunning = status == ServiceStatus.Running;
                StatusText = status switch
                {
                    ServiceStatus.Running => "运行中",
                    ServiceStatus.Stopped => "已停止",
                    _ => "未知"
                };

                // 更新进程信息
                UpdateProcessInfo();
            });
        }
        catch (Exception)
        {
            IsRunning = false;
            StatusText = "无法获取状态";
        }
    }

    /// <summary>
    /// 更新进程信息
    /// </summary>
    private void UpdateProcessInfo()
    {
        try
        {
            var config = _configService.Load();
            var processName = "DataForgeStudio.Api";

            var processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                _serviceProcess = processes[0];
                _processStartTime = _serviceProcess.StartTime;
                StartTimeText = _processStartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

                var memoryMB = _serviceProcess.WorkingSet64 / 1024.0 / 1024.0;
                MemoryUsage = $"{memoryMB:F2} MB";
            }
            else
            {
                _serviceProcess = null;
                _processStartTime = null;
                StartTimeText = "-";
                MemoryUsage = "-";
            }
        }
        catch (Exception)
        {
            StartTimeText = "-";
            MemoryUsage = "-";
        }
    }

    /// <summary>
    /// 启动服务命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task StartAsync()
    {
        if (IsOperating) return;

        IsOperating = true;
        StatusText = "正在启动...";

        try
        {
            await _serviceManager.StartAsync();
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"启动失败: {ex.Message}";
        }
        finally
        {
            IsOperating = false;
        }
    }

    /// <summary>
    /// 停止服务命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task StopAsync()
    {
        if (IsOperating) return;

        IsOperating = true;
        StatusText = "正在停止...";

        try
        {
            await _serviceManager.StopAsync();
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"停止失败: {ex.Message}";
        }
        finally
        {
            IsOperating = false;
        }
    }

    /// <summary>
    /// 重启服务命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task RestartAsync()
    {
        if (IsOperating) return;

        IsOperating = true;
        StatusText = "正在重启...";

        try
        {
            await _serviceManager.RestartAsync();
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"重启失败: {ex.Message}";
        }
        finally
        {
            IsOperating = false;
        }
    }

    /// <summary>
    /// 是否可以执行操作
    /// </summary>
    private bool CanOperate() => !IsOperating;

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
            var config = _configService.Load();
            // TODO: 在 DeployConfig 中添加 AutoStart 属性后保存
            _configService.Save(config);
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
                _serviceProcess?.Dispose();
                _serviceProcess = null;
                // 释放服务管理器资源
                _serviceManager?.Dispose();
            }
            _disposed = true;
        }
    }
}
