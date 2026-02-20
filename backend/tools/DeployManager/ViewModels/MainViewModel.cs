using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployManager.Services;

namespace DeployManager.ViewModels;

/// <summary>
/// 主视图模型
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ServiceControlViewModel _serviceControlViewModel;
    private readonly DatabaseConfigViewModel _databaseConfigViewModel;
    private readonly PortConfigViewModel _portConfigViewModel;
    private readonly FrontendModeViewModel _frontendModeViewModel;
    private readonly PropertyChangedEventHandler _serviceControlHandler;
    private bool _disposed = false;

    /// <summary>
    /// 当前显示的视图
    /// </summary>
    [ObservableProperty]
    private object _currentView;

    /// <summary>
    /// 状态栏颜色
    /// </summary>
    [ObservableProperty]
    private string _statusColor = "#FF4CAF50"; // 绿色

    /// <summary>
    /// 状态栏文本
    /// </summary>
    [ObservableProperty]
    private string _statusText = "就绪";

    /// <summary>
    /// 端口信息
    /// </summary>
    [ObservableProperty]
    private string _portText = "后端: 5000 | 前端: 80";

    /// <summary>
    /// 模式信息
    /// </summary>
    [ObservableProperty]
    private string _modeText = "IIS 模式";

    /// <summary>
    /// 服务选项卡是否选中
    /// </summary>
    [ObservableProperty]
    private bool _isServiceTabSelected = true;

    /// <summary>
    /// 数据库选项卡是否选中
    /// </summary>
    [ObservableProperty]
    private bool _isDatabaseTabSelected;

    /// <summary>
    /// 端口选项卡是否选中
    /// </summary>
    [ObservableProperty]
    private bool _isPortTabSelected;

    /// <summary>
    /// 前端选项卡是否选中
    /// </summary>
    [ObservableProperty]
    private bool _isFrontendTabSelected;

    /// <summary>
    /// 获取服务控制视图模型
    /// </summary>
    public ServiceControlViewModel ServiceControl => _serviceControlViewModel;

    /// <summary>
    /// 获取数据库配置视图模型
    /// </summary>
    public DatabaseConfigViewModel DatabaseConfig => _databaseConfigViewModel;

    /// <summary>
    /// 获取端口配置视图模型
    /// </summary>
    public PortConfigViewModel PortConfig => _portConfigViewModel;

    /// <summary>
    /// 获取前端模式视图模型
    /// </summary>
    public FrontendModeViewModel FrontendMode => _frontendModeViewModel;

    /// <summary>
    /// 初始化主视图模型
    /// </summary>
    /// <param name="serviceControlViewModel">服务控制视图模型</param>
    /// <param name="databaseConfigViewModel">数据库配置视图模型</param>
    /// <param name="portConfigViewModel">端口配置视图模型</param>
    /// <param name="frontendModeViewModel">前端模式视图模型</param>
    public MainViewModel(
        ServiceControlViewModel serviceControlViewModel,
        DatabaseConfigViewModel databaseConfigViewModel,
        PortConfigViewModel portConfigViewModel,
        FrontendModeViewModel frontendModeViewModel)
    {
        _serviceControlViewModel = serviceControlViewModel;
        _databaseConfigViewModel = databaseConfigViewModel;
        _portConfigViewModel = portConfigViewModel;
        _frontendModeViewModel = frontendModeViewModel;

        // 默认显示服务控制视图
        _currentView = _serviceControlViewModel;

        // 启动服务状态刷新定时器
        _serviceControlViewModel.StartRefresh();

        // 存储处理程序引用以便后续取消订阅
        _serviceControlHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(ServiceControlViewModel.IsAppRunning))
            {
                UpdateStatusBar();
            }
        };
        _serviceControlViewModel.PropertyChanged += _serviceControlHandler;

        // 初始化状态栏
        UpdateStatusBar();
    }

    /// <summary>
    /// 刷新所有配置
    /// </summary>
    public void RefreshAll()
    {
        _databaseConfigViewModel.LoadConfig();
        _portConfigViewModel.LoadConfig();
        _frontendModeViewModel.LoadConfig();
        _frontendModeViewModel.CheckInstallationStatus();
        UpdateStatusBar();
    }

    /// <summary>
    /// 更新状态栏
    /// </summary>
    private void UpdateStatusBar()
    {
        // 更新状态颜色和文本（以后端服务状态为准）
        if (_serviceControlViewModel.IsAppRunning)
        {
            StatusColor = "#FF4CAF50"; // 绿色
            StatusText = "服务运行中";
        }
        else
        {
            StatusColor = "#FFFF9800"; // 橙色
            StatusText = "服务已停止";
        }

        // 更新端口信息
        PortText = $"后端: {_portConfigViewModel.BackendPort} | 前端: {_portConfigViewModel.FrontendPort}";

        // 更新模式信息
        ModeText = _frontendModeViewModel.IsIisMode ? "IIS 模式" : "Nginx 模式";
    }

    /// <summary>
    /// 选择服务选项卡命令
    /// </summary>
    [RelayCommand]
    private void SelectServiceTab()
    {
        CurrentView = _serviceControlViewModel;
        IsServiceTabSelected = true;
        IsDatabaseTabSelected = false;
        IsPortTabSelected = false;
        IsFrontendTabSelected = false;
    }

    /// <summary>
    /// 选择数据库选项卡命令
    /// </summary>
    [RelayCommand]
    private void SelectDatabaseTab()
    {
        CurrentView = _databaseConfigViewModel;
        IsServiceTabSelected = false;
        IsDatabaseTabSelected = true;
        IsPortTabSelected = false;
        IsFrontendTabSelected = false;
    }

    /// <summary>
    /// 选择端口选项卡命令
    /// </summary>
    [RelayCommand]
    private void SelectPortTab()
    {
        CurrentView = _portConfigViewModel;
        IsServiceTabSelected = false;
        IsDatabaseTabSelected = false;
        IsPortTabSelected = true;
        IsFrontendTabSelected = false;
    }

    /// <summary>
    /// 选择前端选项卡命令
    /// </summary>
    [RelayCommand]
    private void SelectFrontendTab()
    {
        CurrentView = _frontendModeViewModel;
        IsServiceTabSelected = false;
        IsDatabaseTabSelected = false;
        IsPortTabSelected = false;
        IsFrontendTabSelected = true;
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
                // 取消事件订阅
                if (_serviceControlHandler != null)
                {
                    _serviceControlViewModel.PropertyChanged -= _serviceControlHandler;
                }
                // 释放服务控制视图模型资源
                _serviceControlViewModel?.Dispose();
            }
            _disposed = true;
        }
    }
}
