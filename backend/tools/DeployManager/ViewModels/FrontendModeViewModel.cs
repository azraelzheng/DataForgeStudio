using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployManager.Services;
using System.IO;

namespace DeployManager.ViewModels;

/// <summary>
/// 前端模式配置视图模型
/// </summary>
public partial class FrontendModeViewModel : ObservableObject
{
    private readonly IIisManager _iisManager;
    private readonly INginxManager _nginxManager;
    private readonly IConfigService _configService;
    private readonly IWebServiceManager _webServiceManager;

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
    /// <param name="webServiceManager">Web 服务管理器</param>
    public FrontendModeViewModel(
        IIisManager iisManager,
        INginxManager nginxManager,
        IConfigService configService,
        IWebServiceManager webServiceManager)
    {
        _iisManager = iisManager;
        _nginxManager = nginxManager;
        _configService = configService;
        _webServiceManager = webServiceManager;

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
        FileLogger.Info("=== FrontendModeViewModel.CheckInstallationStatus() 开始 ===");

        try
        {
            IisInstalled = _iisManager.IsIisInstalled();
            FileLogger.Info($"IIS 安装状态: {IisInstalled}");

            NginxInstalled = _nginxManager.IsNginxInstalled();
            FileLogger.Info($"Nginx 安装状态: {NginxInstalled}");
        }
        catch (Exception ex)
        {
            FileLogger.Error("检查安装状态失败", ex);
        }

        FileLogger.Info($"最终状态: IisInstalled={IisInstalled}, NginxInstalled={NginxInstalled}");
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public void LoadConfig()
    {
        try
        {
            // 使用 getter 方法获取前端模式
            var configMode = _configService.GetFrontendMode().ToLowerInvariant();

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
            // 停止当前服务
            if (IsIisMode && IisInstalled)
            {
                try
                {
                    // IIS 站点名称硬编码为 "DataForgeStudio"
                    const string iisSiteName = "DataForgeStudio";
                    if (_iisManager.IsSiteExists(iisSiteName))
                    {
                        _iisManager.StopSite(iisSiteName);
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
                    // 使用 WebServiceManager 停止 Nginx Windows 服务
                    await _webServiceManager.StopAsync();
                }
                catch (Exception ex)
                {
                    // 忽略停止失败，继续切换
                    System.Diagnostics.Debug.WriteLine($"[FrontendModeViewModel] 停止 Nginx 服务失败: {ex.Message}");
                }
            }

            // 启动新服务
            if (targetMode == "iis")
            {
                // IIS 模式：确保 Web 服务已停止，然后启动 IIS 站点
                try
                {
                    await _webServiceManager.StopAsync();
                }
                catch
                {
                    // 忽略停止失败
                }

                const string siteName = "DataForgeStudio";
                var port = _configService.GetFrontendPort();
                var physicalPath = _configService.GetWebSitePath();

                _iisManager.ConfigureSite(siteName, port, physicalPath);
                _iisManager.StartSite(siteName);
            }
            else
            {
                // Nginx 模式：停止 IIS 站点，然后启动 Web 服务
                const string iisSiteName = "DataForgeStudio";
                if (_iisManager.IsSiteExists(iisSiteName))
                {
                    _iisManager.StopSite(iisSiteName);
                }

                var nginxConfigPath = System.IO.Path.Combine(_configService.InstallPath, "WebServer", "conf", "nginx.conf");
                var backendPort = _configService.GetBackendPort();
                var frontendPort = _configService.GetFrontendPort();
                var backendUrl = $"http://localhost:{backendPort}";

                _nginxManager.UpdateConfig(nginxConfigPath, frontendPort, backendUrl);

                // 使用 WebServiceManager 启动 Nginx Windows 服务
                await _webServiceManager.StartAsync();
            }

            IsIisMode = targetMode == "iis";

            // 保存用户选择的模式
            _configService.SaveFrontendMode(targetMode);

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
