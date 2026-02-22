using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployManager.Models;
using DeployManager.Services;

namespace DeployManager.ViewModels;

/// <summary>
/// 数据库配置视图模型
///
/// 保存数据库配置时会：
/// 1. 保存数据库连接字符串到 appsettings.json
/// 2. 保存元信息到 config.json
/// </summary>
public partial class DatabaseConfigViewModel : ObservableObject
{
    private readonly IDatabaseConnectionService _databaseService;
    private readonly IConfigService _configService;

    /// <summary>
    /// 服务器地址
    /// </summary>
    [ObservableProperty]
    private string _server = "localhost";

    /// <summary>
    /// 端口号
    /// </summary>
    [ObservableProperty]
    private string _port = "1433";

    /// <summary>
    /// 数据库名称
    /// </summary>
    [ObservableProperty]
    private string _database = "DataForgeStudio";

    /// <summary>
    /// 用户名
    /// </summary>
    [ObservableProperty]
    private string _username = "sa";

    /// <summary>
    /// 密码
    /// </summary>
    [ObservableProperty]
    private string _password = "";

    /// <summary>
    /// 是否使用 Windows 身份验证
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSqlAuth))]
    private bool _useWindowsAuth = true;

    /// <summary>
    /// 是否使用 SQL 身份验证
    /// </summary>
    public bool IsSqlAuth => !UseWindowsAuth;

    /// <summary>
    /// 测试连接结果消息
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTestResult))]
    private string _testResult = "";

    /// <summary>
    /// 是否有测试结果
    /// </summary>
    public bool HasTestResult => !string.IsNullOrEmpty(TestResult);

    /// <summary>
    /// 测试连接是否成功
    /// </summary>
    [ObservableProperty]
    private bool _testSuccess;

    /// <summary>
    /// 是否正在测试连接
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private bool _isTesting;

    /// <summary>
    /// 是否正在保存
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isSaving;

    /// <summary>
    /// 初始化数据库配置视图模型
    /// </summary>
    /// <param name="databaseService">数据库连接服务</param>
    /// <param name="configService">配置服务</param>
    public DatabaseConfigViewModel(IDatabaseConnectionService databaseService, IConfigService configService)
    {
        _databaseService = databaseService;
        _configService = configService;

        // 加载配置
        LoadConfig();
    }

    /// <summary>
    /// 加载配置
    /// 从 appsettings.json 读取实际数据库配置
    /// </summary>
    public void LoadConfig()
    {
        try
        {
            // 从 appsettings.json 读取数据库配置
            var dbConfig = _configService.GetDatabaseConfig();

            Server = dbConfig.Server;
            Port = dbConfig.Port.ToString();
            Database = dbConfig.Database;
            Username = dbConfig.Username;
            Password = dbConfig.Password;
            UseWindowsAuth = dbConfig.UseWindowsAuth;

            TestResult = "";
            TestSuccess = false;
        }
        catch (Exception ex)
        {
            TestResult = $"加载配置失败: {ex.Message}";
            TestSuccess = false;
        }
    }

    /// <summary>
    /// 获取当前数据库配置
    /// </summary>
    private DatabaseConfig GetCurrentConfig()
    {
        return new DatabaseConfig
        {
            Server = Server,
            Port = int.TryParse(Port, out var port) ? port : 1433,
            Database = Database,
            Username = Username,
            Password = Password,
            UseWindowsAuth = UseWindowsAuth
        };
    }

    /// <summary>
    /// 测试连接命令
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsTesting) return;

        IsTesting = true;
        TestResult = "正在测试连接...";

        try
        {
            var config = GetCurrentConfig();
            var (success, message) = await _databaseService.TestConnectionAsync(config);

            TestSuccess = success;
            TestResult = message;

            // 测试完成后通知保存按钮更新状态
            SaveCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            TestSuccess = false;
            TestResult = $"测试失败: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    /// <summary>
    /// 保存配置命令
    /// 同时更新 appsettings.json 和 config.json
    /// 只有测试连接成功后才能保存
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        if (IsSaving) return;

        // 再次检查测试是否成功
        if (!TestSuccess)
        {
            TestResult = "请先测试连接成功后再保存配置";
            return;
        }

        IsSaving = true;

        try
        {
            var dbConfig = GetCurrentConfig();

            // 保存数据库配置到 appsettings.json
            _configService.SaveDatabaseConfig(dbConfig);

            TestResult = "数据库配置已保存到 appsettings.json，重启服务后生效";
            TestSuccess = true;
        }
        catch (Exception ex)
        {
            TestResult = $"保存失败: {ex.Message}";
            TestSuccess = false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// 是否可以测试
    /// </summary>
    private bool CanTest() => !IsTesting;

    /// <summary>
    /// 是否可以保存（必须测试成功）
    /// </summary>
    private bool CanSave() => !IsSaving && TestSuccess;

    /// <summary>
    /// 配置变更时清除测试结果
    /// </summary>
    private void ClearTestResult()
    {
        TestResult = "";
        TestSuccess = false;
        SaveCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Server 属性变更时的处理
    /// </summary>
    partial void OnServerChanged(string value) => ClearTestResult();

    /// <summary>
    /// Port 属性变更时的处理
    /// </summary>
    partial void OnPortChanged(string value) => ClearTestResult();

    /// <summary>
    /// Database 属性变更时的处理
    /// </summary>
    partial void OnDatabaseChanged(string value) => ClearTestResult();

    /// <summary>
    /// Username 属性变更时的处理
    /// </summary>
    partial void OnUsernameChanged(string value) => ClearTestResult();

    /// <summary>
    /// Password 属性变更时的处理
    /// </summary>
    partial void OnPasswordChanged(string value) => ClearTestResult();

    /// <summary>
    /// UseWindowsAuth 属性变更时的处理
    /// </summary>
    partial void OnUseWindowsAuthChanged(bool value) => ClearTestResult();
}
