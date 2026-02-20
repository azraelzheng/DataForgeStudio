using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DeployManager;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 服务提供者
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// 构造函数
    /// </summary>
    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    /// <summary>
    /// 配置依赖注入服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // 注册服务
        services.AddSingleton<Services.IConfigService, Services.ConfigService>();
        services.AddSingleton<Services.IWindowsServiceManager, Services.WindowsServiceManager>();
        services.AddSingleton<Services.IIisManager, Services.IisManager>();
        services.AddSingleton<Services.INginxManager, Services.NginxManager>();
        services.AddSingleton<Services.IDatabaseConnectionService, Services.DatabaseConnectionService>();

        // 注册 ViewModels
        services.AddSingleton<ViewModels.MainViewModel>();
        services.AddSingleton<ViewModels.ServiceControlViewModel>();
        services.AddSingleton<ViewModels.DatabaseConfigViewModel>();
        services.AddSingleton<ViewModels.PortConfigViewModel>();
        services.AddSingleton<ViewModels.FrontendModeViewModel>();
    }

    /// <summary>
    /// 应用启动时
    /// </summary>
    /// <param name="e">启动事件参数</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new Views.MainWindow
        {
            DataContext = Services.GetRequiredService<ViewModels.MainViewModel>()
        };
        mainWindow.Show();
    }
}
