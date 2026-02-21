using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Installer.Services;
using Installer.ViewModels;
using Microsoft.Win32;

namespace Installer;

public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();

        // 从注册表读取安装路径（Inno Setup 写入的）
        var installPath = GetInstallPathFromRegistry();
        if (!string.IsNullOrEmpty(installPath))
        {
            viewModel.Config.InstallPath = installPath;
        }

        var mainWindow = new MainWindow
        {
            DataContext = viewModel
        };

        mainWindow.Show();
    }

    /// <summary>
    /// 从注册表读取安装路径
    /// Inno Setup 会在安装时写入 HKLM\Software\DataForgeStudio\InstallPath
    /// </summary>
    private static string? GetInstallPathFromRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DataForgeStudio");
            var path = key?.GetValue("InstallPath") as string;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                return path;
            }
        }
        catch
        {
            // 忽略注册表读取错误
        }
        return null;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IInstallService, InstallService>();
        services.AddSingleton<IDatabaseTestService, DatabaseTestService>();
        services.AddSingleton<MainViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
