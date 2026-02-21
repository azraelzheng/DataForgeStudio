using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployManager.Services;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace DeployManager.ViewModels;

/// <summary>
/// 端口配置视图模型
///
/// 保存端口时会：
/// 1. 检测端口是否被占用
/// 2. 保存后端端口到 appsettings.json
/// 3. 保存前端端口到 IIS（如果是 IIS 模式）
/// 4. 保存元信息到 config.json
/// </summary>
public partial class PortConfigViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly IIisManager? _iisManager;
    private readonly INginxManager? _nginxManager;

    /// <summary>
    /// 后端端口
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private int _backendPort = 5000;

    /// <summary>
    /// 前端端口
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private int _frontendPort = 80;

    /// <summary>
    /// 前端模式（iis 或 nginx）
    /// </summary>
    [ObservableProperty]
    private string _frontendMode = "iis";

    /// <summary>
    /// IIS 站点名称
    /// </summary>
    [ObservableProperty]
    private string _iisSiteName = "DataForgeStudio";

    /// <summary>
    /// 测试结果消息
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTestResult))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _testResult = "";

    /// <summary>
    /// 是否有测试结果
    /// </summary>
    public bool HasTestResult => !string.IsNullOrEmpty(TestResult);

    /// <summary>
    /// 测试是否成功
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _testSuccess;

    /// <summary>
    /// 是否正在测试
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestPortsCommand))]
    private bool _isTesting;

    /// <summary>
    /// 是否正在保存
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isSaving;

    /// <summary>
    /// 保存结果消息
    /// </summary>
    [ObservableProperty]
    private string _saveResult = "";

    /// <summary>
    /// 保存是否成功
    /// </summary>
    [ObservableProperty]
    private bool _saveSuccess;

    /// <summary>
    /// 是否显示保存结果
    /// </summary>
    [ObservableProperty]
    private bool _showSaveResult;

    /// <summary>
    /// 是否可以保存（测试成功后才能保存）
    /// </summary>
    public bool CanSave => !IsSaving && TestSuccess;

    /// <summary>
    /// 初始化端口配置视图模型
    /// </summary>
    /// <param name="configService">配置服务</param>
    /// <param name="iisManager">IIS 管理器（可选）</param>
    /// <param name="nginxManager">Nginx 管理器（可选）</param>
    public PortConfigViewModel(
        IConfigService configService,
        IIisManager? iisManager = null,
        INginxManager? nginxManager = null)
    {
        _configService = configService;
        _iisManager = iisManager;
        _nginxManager = nginxManager;

        // 加载配置
        LoadConfig();
    }

    /// <summary>
    /// 加载配置
    /// 从 appsettings.json 读取实际端口配置
    /// </summary>
    public void LoadConfig()
    {
        try
        {
            // 从 appsettings.json 读取后端端口
            BackendPort = _configService.GetBackendPort();

            // 从 config.json 读取前端配置
            var config = _configService.Load();
            FrontendPort = config.Frontend.Port;
            FrontendMode = config.Frontend.Mode;
            IisSiteName = config.Frontend.IisSiteName;

            ClearTestResult();
            ShowSaveResult = false;
        }
        catch (Exception ex)
        {
            TestResult = $"加载配置失败: {ex.Message}";
            TestSuccess = false;
        }
    }

    /// <summary>
    /// 清除测试结果
    /// </summary>
    private void ClearTestResult()
    {
        TestResult = "";
        TestSuccess = false;
        SaveCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 测试端口命令
    /// </summary>
    [RelayCommand]
    private void TestPorts()
    {
        if (IsTesting) return;

        IsTesting = true;
        var messages = new List<string>();

        try
        {
            // 验证端口范围
            if (BackendPort < 1 || BackendPort > 65535)
            {
                TestResult = "后端端口必须在 1-65535 范围内";
                TestSuccess = false;
                return;
            }

            if (FrontendPort < 1 || FrontendPort > 65535)
            {
                TestResult = "前端端口必须在 1-65535 范围内";
                TestSuccess = false;
                return;
            }

            // 检测后端端口
            var backendInUse = IsPortInUse(BackendPort);
            if (backendInUse)
            {
                messages.Add($"后端端口 {BackendPort} 已被占用");
            }
            else
            {
                messages.Add($"后端端口 {BackendPort} 可用");
            }

            // 检测前端端口
            var frontendInUse = IsPortInUse(FrontendPort);
            if (frontendInUse)
            {
                messages.Add($"前端端口 {FrontendPort} 已被占用");
            }
            else
            {
                messages.Add($"前端端口 {FrontendPort} 可用");
            }

            // 设置结果
            var hasConflict = backendInUse || frontendInUse;
            TestResult = string.Join("，", messages);
            TestSuccess = !hasConflict;

            SaveCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            TestResult = $"测试失败: {ex.Message}";
            TestSuccess = false;
        }
        finally
        {
            IsTesting = false;
        }
    }

    /// <summary>
    /// 检测端口是否被占用
    /// </summary>
    /// <param name="port">端口号</param>
    /// <returns>如果端口被占用返回 true，否则返回 false</returns>
    private static bool IsPortInUse(int port)
    {
        try
        {
            // 尝试监听端口
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return false; // 端口可用
        }
        catch (SocketException)
        {
            return true; // 端口被占用
        }
        catch
        {
            return true; // 其他错误也认为端口不可用
        }
    }

    /// <summary>
    /// 保存配置命令
    /// 同时更新 appsettings.json 和 IIS/Nginx 配置
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        if (IsSaving) return;

        // 再次检查测试是否成功
        if (!TestSuccess)
        {
            SaveResult = "请先测试端口可用后再保存配置";
            SaveSuccess = false;
            ShowSaveResult = true;
            return;
        }

        IsSaving = true;
        var messages = new List<string>();

        try
        {
            // 1. 更新后端端口到 appsettings.json
            try
            {
                _configService.UpdateBackendPort(BackendPort);
                messages.Add($"后端端口已更新为 {BackendPort}");
            }
            catch (Exception ex)
            {
                messages.Add($"后端端口更新失败: {ex.Message}");
            }

            // 2. 更新前端配置（IIS 或 Nginx）
            if (FrontendMode.Equals("iis", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    UpdateIisPort();
                    messages.Add($"IIS 站点端口已更新为 {FrontendPort}");
                }
                catch (Exception ex)
                {
                    messages.Add($"IIS 端口更新失败: {ex.Message}");
                }
            }
            else if (FrontendMode.Equals("nginx", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    UpdateNginxPort();
                    messages.Add($"Nginx 端口已更新为 {FrontendPort}");
                }
                catch (Exception ex)
                {
                    messages.Add($"Nginx 端口更新失败: {ex.Message}");
                }
            }

            // 3. 保存元信息到 config.json
            var config = _configService.Load();
            config.Backend.Port = BackendPort;
            config.Frontend.Port = FrontendPort;
            config.Frontend.Mode = FrontendMode;
            config.Frontend.IisSiteName = IisSiteName;
            _configService.Save(config);
            messages.Add("配置已保存");

            // 设置结果
            SaveResult = string.Join("\n", messages);
            SaveSuccess = messages.All(m => !m.Contains("失败"));
            ShowSaveResult = true;
        }
        catch (Exception ex)
        {
            SaveResult = $"保存失败: {ex.Message}";
            SaveSuccess = false;
            ShowSaveResult = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// 更新 IIS 站点端口
    /// </summary>
    private void UpdateIisPort()
    {
        if (_iisManager == null)
        {
            throw new InvalidOperationException("IIS 管理器未初始化");
        }

        // 获取前端物理路径
        var config = _configService.Load();
        var frontendPath = GetFrontendPath(config.InstallPath);

        // 配置 IIS 站点
        _iisManager.ConfigureSite(IisSiteName, FrontendPort, frontendPath);
    }

    /// <summary>
    /// 更新 Nginx 端口
    /// </summary>
    private void UpdateNginxPort()
    {
        if (_nginxManager == null)
        {
            throw new InvalidOperationException("Nginx 管理器未初始化");
        }

        var config = _configService.Load();
        var nginxPath = config.Frontend.NginxPath;

        if (string.IsNullOrEmpty(nginxPath))
        {
            throw new InvalidOperationException("Nginx 路径未配置");
        }

        // 构建配置文件路径（假设在 nginx 目录下的 conf/nginx.conf）
        var configPath = Path.Combine(nginxPath, "conf", "nginx.conf");

        // 后端 URL（使用更新后的后端端口）
        var backendUrl = $"http://localhost:{BackendPort}";

        _nginxManager.UpdateConfig(configPath, FrontendPort, backendUrl);
    }

    /// <summary>
    /// 获取前端部署路径
    /// </summary>
    private static string GetFrontendPath(string installPath)
    {
        return Path.Combine(installPath, "wwwroot");
    }

    /// <summary>
    /// BackendPort 属性变更时的处理
    /// </summary>
    partial void OnBackendPortChanged(int value) => ClearTestResult();

    /// <summary>
    /// FrontendPort 属性变更时的处理
    /// </summary>
    partial void OnFrontendPortChanged(int value) => ClearTestResult();
}
