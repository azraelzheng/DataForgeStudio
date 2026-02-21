using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployManager.Services;

namespace DeployManager.ViewModels;

/// <summary>
/// 前端模式配置视图模型
/// </summary>
public partial class FrontendModeViewModel : ObservableObject
{
    private readonly IIisManager _iisManager;
    private readonly INginxManager _nginxManager;
    private readonly IConfigService _configService;

    /// <summary>
    /// 是否使用 IIS 模式
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNginxMode))]
    private bool _isIisMode = true;

    /// <summary>
    /// 是否使用 Nginx 模式
    /// </summary>
    public bool IsNginxMode => !IsIisMode;

    /// <summary>
    /// IIS 是否已安装
    /// </summary>
    [ObservableProperty]
    private bool _iisInstalled;

    /// <summary>
    /// Nginx 是否已安装
    /// </summary>
    [ObservableProperty]
    private bool _nginxInstalled;

    /// <summary>
    /// 操作结果消息
    /// </summary>
    [ObservableProperty]
    private string _operationResult = "";

    /// <summary>
    /// 操作是否成功
    /// </summary>
    [ObservableProperty]
    private bool _operationSuccess;

    /// <summary>
    /// 是否正在切换模式
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SwitchModeCommand))]
    private bool _isSwitching;

    /// <summary>
    /// 初始化前端模式配置视图模型
    /// </summary>
    /// <param name="iisManager">IIS 管理器</param>
    /// <param name="nginxManager">Nginx 管理器</param>
    /// <param name="configService">配置服务</param>
    public FrontendModeViewModel(IIisManager iisManager, INginxManager nginxManager, IConfigService configService)
    {
        _iisManager = iisManager;
        _nginxManager = nginxManager;
        _configService = configService;

        // 检查安装状态
        CheckInstallationStatus();

        // 加载配置
        LoadConfig();
    }

    /// <summary>
    /// 检查安装状态
    /// </summary>
    public void CheckInstallationStatus()
    {
        IisInstalled = _iisManager.IsIisInstalled();
        NginxInstalled = _nginxManager.IsNginxInstalled();
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public void LoadConfig()
    {
        try
        {
            var config = _configService.Load();

            // 根据配置和安装状态设置模式
            var configMode = config.Frontend.Mode?.ToLowerInvariant() ?? "nginx";

            // 如果配置的是 IIS 模式但 IIS 未安装，自动切换到 Nginx
            if (configMode == "iis" && !IisInstalled && NginxInstalled)
            {
                IsIisMode = false;
                System.Diagnostics.Debug.WriteLine("[FrontendModeViewModel] IIS 未安装，使用 Nginx 模式");
            }
            // 如果配置的是 Nginx 模式但 Nginx 未安装，自动切换到 IIS
            else if (configMode == "nginx" && !NginxInstalled && IisInstalled)
            {
                IsIisMode = true;
                System.Diagnostics.Debug.WriteLine("[FrontendModeViewModel] Nginx 未安装，使用 IIS 模式");
            }
            // 如果都未安装，默认使用 Nginx（捆绑的）
            else if (!IisInstalled && !NginxInstalled)
            {
                IsIisMode = false;
                System.Diagnostics.Debug.WriteLine("[FrontendModeViewModel] 两者都未安装，默认使用 Nginx 模式");
            }
            else
            {
                IsIisMode = configMode == "iis";
            }

            OperationResult = "";
            OperationSuccess = false;
        }
        catch (Exception ex)
        {
            // 配置加载失败时，根据安装状态设置默认模式
            IsIisMode = IisInstalled && !NginxInstalled;
            OperationResult = $"加载配置失败: {ex.Message}";
            OperationSuccess = false;
        }
    }

    /// <summary>
    /// 切换模式命令
    /// </summary>
    /// <param name="mode">目标模式 ("iis" 或 "nginx")</param>
    [RelayCommand(CanExecute = nameof(CanSwitch))]
    private async Task SwitchModeAsync(string mode)
    {
        if (IsSwitching) return;

        var targetMode = mode.ToLowerInvariant();

        // 验证安装状态
        if (targetMode == "iis" && !IisInstalled)
        {
            OperationResult = "IIS 未安装，无法切换到 IIS 模式";
            OperationSuccess = false;
            return;
        }

        if (targetMode == "nginx" && !NginxInstalled)
        {
            OperationResult = "Nginx 未安装，无法切换到 Nginx 模式";
            OperationSuccess = false;
            return;
        }

        // 如果已经是目标模式，不需要切换
        if ((targetMode == "iis" && IsIisMode) || (targetMode == "nginx" && IsNginxMode))
        {
            OperationResult = $"已经是 {(IsIisMode ? "IIS" : "Nginx")} 模式";
            OperationSuccess = true;
            return;
        }

        IsSwitching = true;
        OperationResult = "正在切换模式...";

        try
        {
            // 停止当前服务（仅在已安装时）
            if (IsIisMode && IisInstalled)
            {
                try
                {
                    var config = _configService.Load();
                    if (_iisManager.IsSiteExists(config.Frontend.IisSiteName))
                    {
                        _iisManager.StopSite(config.Frontend.IisSiteName);
                    }
                }
                catch (Exception ex)
                {
                    // 忽略停止失败，继续切换
                    System.Diagnostics.Debug.WriteLine($"[FrontendModeViewModel] 停止 IIS 站点失败: {ex.Message}");
                }
            }
            else if (IsNginxMode)
            {
                try
                {
                    await _nginxManager.StopAsync();
                }
                catch (Exception ex)
                {
                    // 忽略停止失败，继续切换
                    System.Diagnostics.Debug.WriteLine($"[FrontendModeViewModel] 停止 Nginx 失败: {ex.Message}");
                }
            }

            // 更新配置
            var deployConfig = _configService.Load();
            deployConfig.Frontend.Mode = targetMode;
            _configService.Save(deployConfig);

            // 启动新服务
            if (targetMode == "iis")
            {
                var config = _configService.Load();
                var siteName = config.Frontend.IisSiteName;
                var port = config.Frontend.Port;
                var physicalPath = System.IO.Path.Combine(config.InstallPath, "wwwroot");

                _iisManager.ConfigureSite(siteName, port, physicalPath);
                _iisManager.StartSite(siteName);
            }
            else
            {
                var config = _configService.Load();
                var nginxConfigPath = System.IO.Path.Combine(config.InstallPath, "nginx", "conf", "nginx.conf");
                var backendUrl = $"http://localhost:{config.Backend.Port}";

                _nginxManager.UpdateConfig(nginxConfigPath, config.Frontend.Port, backendUrl);
                await _nginxManager.StartAsync(nginxConfigPath);
            }

            IsIisMode = targetMode == "iis";
            OperationResult = $"已成功切换到 {(IsIisMode ? "IIS" : "Nginx")} 模式";
            OperationSuccess = true;
        }
        catch (Exception ex)
        {
            OperationResult = $"切换失败: {ex.Message}";
            OperationSuccess = false;
        }
        finally
        {
            IsSwitching = false;
        }
    }

    /// <summary>
    /// 是否可以切换
    /// </summary>
    private bool CanSwitch() => !IsSwitching;
}
