using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Installer.Models;
using Installer.Services;

namespace Installer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IInstallService _installService;
    private readonly IDatabaseTestService _databaseTestService;

    [ObservableProperty] private int _currentPageIndex;
    [ObservableProperty] private string _title = "DataForgeStudio 安装向导";
    [ObservableProperty] private InstallConfig _config = new();
    [ObservableProperty] private bool _isInstalling;
    [ObservableProperty] private bool _installComplete;
    [ObservableProperty] private string _installError = "";
    [ObservableProperty] private int _installProgress;
    [ObservableProperty] private string _currentStep = "";
    [ObservableProperty] private bool _launchManager = true;

    public ObservableCollection<string> InstallLogs { get; } = new();

    public bool CanGoBack => CurrentPageIndex > 0 && !IsInstalling;
    public bool CanGoNext => CurrentPageIndex < Pages.Count - 1 && !IsInstalling && !InstallComplete;

    public List<string> Pages { get; } = new()
    {
        "欢迎", "安装路径", "数据库配置", "端口配置", "确认安装", "安装进度", "完成"
    };

    public MainViewModel(IInstallService installService, IDatabaseTestService databaseTestService)
    {
        _installService = installService;
        _databaseTestService = databaseTestService;
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        if (CurrentPageIndex > 0)
        {
            CurrentPageIndex--;
            UpdateCommands();
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void GoNext()
    {
        if (CurrentPageIndex < Pages.Count - 1)
        {
            if (!ValidateCurrentPage()) return;
            CurrentPageIndex++;
            UpdateCommands();

            if (CurrentPageIndex == Pages.Count - 2)
                _ = StartInstallAsync();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (IsInstalling)
        {
            var result = MessageBox.Show("安装正在进行中，确定要取消吗？", "确认取消", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;
        }
        Application.Current.Shutdown();
    }

    [RelayCommand]
    private void Finish()
    {
        if (LaunchManager)
        {
            try
            {
                var managerPath = System.IO.Path.Combine(Config.InstallPath, "DeployManager.exe");
                if (System.IO.File.Exists(managerPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = managerPath,
                        UseShellExecute = true,
                        WorkingDirectory = Config.InstallPath
                    });
                }
            }
            catch { }
        }
        Application.Current.Shutdown();
    }

    private bool ValidateCurrentPage()
    {
        return CurrentPageIndex switch
        {
            1 => ValidateInstallPath(),
            2 => ValidateDatabase(),
            3 => ValidatePorts(),
            _ => true
        };
    }

    private bool ValidateInstallPath()
    {
        var (valid, message) = _installService.ValidateInstallPath(Config.InstallPath);
        if (!valid)
        {
            MessageBox.Show(message, "路径无效", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (_installService.IsInstalled(Config.InstallPath))
        {
            var result = MessageBox.Show(
                "检测到该目录下已有安装，继续安装将覆盖现有文件。\n\n数据库数据不会被修改。\n\n是否继续？",
                "已安装", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.No) return false;
        }
        return true;
    }

    private bool ValidateDatabase()
    {
        if (!Config.Database.UseWindowsAuth)
        {
            if (string.IsNullOrWhiteSpace(Config.Database.Username))
            {
                MessageBox.Show("请输入数据库用户名", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(Config.Database.Password))
            {
                MessageBox.Show("请输入数据库密码", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        return true;
    }

    private bool ValidatePorts()
    {
        if (Config.Frontend.Port < 1 || Config.Frontend.Port > 65535)
        {
            MessageBox.Show("前端端口必须在 1-65535 之间", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (Config.Backend.Port < 1 || Config.Backend.Port > 65535)
        {
            MessageBox.Show("后端端口必须在 1-65535 之间", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (Config.Frontend.Port == Config.Backend.Port)
        {
            MessageBox.Show("前端端口和后端端口不能相同", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    private async Task StartInstallAsync()
    {
        IsInstalling = true;
        InstallComplete = false;
        InstallError = "";
        InstallLogs.Clear();
        UpdateCommands();

        var progress = new Progress<InstallProgress>(p =>
        {
            InstallProgress = p.Percentage;
            CurrentStep = p.CurrentStep;
            Application.Current.Dispatcher.Invoke(() => InstallLogs.Add(p.LogMessage));
        });

        try
        {
            await _installService.InstallAsync(Config, progress);
            InstallComplete = true;
            InstallProgress = 100;
            CurrentPageIndex++;
            UpdateCommands();
        }
        catch (OperationCanceledException)
        {
            InstallError = "安装已取消";
        }
        catch (Exception ex)
        {
            InstallError = $"安装失败: {ex.Message}";
            InstallLogs.Add($"错误: {ex.Message}");
        }
        finally
        {
            IsInstalling = false;
            UpdateCommands();
        }
    }

    private void UpdateCommands()
    {
        GoBackCommand.NotifyCanExecuteChanged();
        GoNextCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
    }

    public async Task<(bool Success, string Message)> TestDatabaseConnectionAsync()
    {
        return await _databaseTestService.TestConnectionAsync(Config.Database);
    }
}
