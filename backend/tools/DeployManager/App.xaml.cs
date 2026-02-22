using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using DeployManager.Services;

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
        // 添加全局异常处理
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            FileLogger.Error("应用程序初始化失败", ex);
            MessageBox.Show($"应用程序初始化失败:\n{ex.Message}\n\n详细信息请查看日志文件。", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <summary>
    /// 全局异常处理
    /// </summary>
    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        FileLogger.Error("未处理的异常", e.Exception);
        MessageBox.Show($"发生未处理的错误:\n{e.Exception.Message}\n\n详细信息请查看日志文件。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    /// <summary>
    /// 配置依赖注入服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // 注册服务（注意顺序：先注册没有依赖的服务）
        services.AddSingleton<Services.IIisManager, Services.IisManager>();
        services.AddSingleton<Services.IConfigService, Services.ConfigService>();

        // 注册 Lazy<IConfigService> 用于打破循环依赖
        services.AddSingleton<Lazy<Services.IConfigService>>(sp =>
            new Lazy<Services.IConfigService>(() => sp.GetRequiredService<Services.IConfigService>()));

        // NginxManager 使用 Lazy<IConfigService> 避免循环依赖
        services.AddSingleton<Services.INginxManager, Services.NginxManager>();
        services.AddSingleton<Services.IWindowsServiceManager, Services.WindowsServiceManager>();
        services.AddSingleton<Services.IWebServiceManager, Services.WebServiceManager>();
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

        try
        {
            var mainWindow = new Views.MainWindow
            {
                DataContext = Services.GetRequiredService<ViewModels.MainViewModel>()
            };
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            FileLogger.Error("创建主窗口失败", ex);
            MessageBox.Show($"创建主窗口失败:\n{ex.Message}\n\n详细信息请查看日志文件。", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
