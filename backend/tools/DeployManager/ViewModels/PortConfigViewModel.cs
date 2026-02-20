using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployManager.Services;

namespace DeployManager.ViewModels;

/// <summary>
/// 端口配置视图模型
/// </summary>
public partial class PortConfigViewModel : ObservableObject
{
    private readonly IConfigService _configService;

    /// <summary>
    /// 后端端口
    /// </summary>
    [ObservableProperty]
    private int _backendPort = 5000;

    /// <summary>
    /// 前端端口
    /// </summary>
    [ObservableProperty]
    private int _frontendPort = 80;

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
    /// 是否正在保存
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isSaving;

    /// <summary>
    /// 初始化端口配置视图模型
    /// </summary>
    /// <param name="configService">配置服务</param>
    public PortConfigViewModel(IConfigService configService)
    {
        _configService = configService;

        // 加载配置
        LoadConfig();
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public void LoadConfig()
    {
        try
        {
            var config = _configService.Load();

            BackendPort = config.Backend.Port;
            FrontendPort = config.Frontend.Port;

            SaveResult = "";
            SaveSuccess = false;
        }
        catch (Exception ex)
        {
            SaveResult = $"加载配置失败: {ex.Message}";
            SaveSuccess = false;
        }
    }

    /// <summary>
    /// 保存配置命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        if (IsSaving) return;

        IsSaving = true;

        try
        {
            // 验证端口范围
            if (BackendPort < 1 || BackendPort > 65535)
            {
                SaveResult = "后端端口必须在 1-65535 范围内";
                SaveSuccess = false;
                return;
            }

            if (FrontendPort < 1 || FrontendPort > 65535)
            {
                SaveResult = "前端端口必须在 1-65535 范围内";
                SaveSuccess = false;
                return;
            }

            var config = _configService.Load();
            config.Backend.Port = BackendPort;
            config.Frontend.Port = FrontendPort;
            _configService.Save(config);

            SaveResult = "端口配置已保存，重启服务后生效";
            SaveSuccess = true;
        }
        catch (Exception ex)
        {
            SaveResult = $"保存失败: {ex.Message}";
            SaveSuccess = false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// 是否可以保存
    /// </summary>
    private bool CanSave() => !IsSaving;
}
