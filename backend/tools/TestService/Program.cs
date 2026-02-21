using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestService;

/// <summary>
/// DataForgeStudio 测试服务 (DFAppService)
/// 用于测试系统管理工具的服务启停功能
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService(config =>
            {
                config.ServiceName = "DFAppService";
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddEventLog();
            });
}

/// <summary>
/// 后台工作服务
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _logFile;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "service.log");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DataForgeStudio 服务启动于: {time}", DateTimeOffset.Now);
        LogToFile("服务启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("DataForgeStudio 服务运行中: {time}", DateTimeOffset.Now);
                LogToFile($"服务运行中 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("DataForgeStudio 服务停止于: {time}", DateTimeOffset.Now);
        LogToFile("服务停止");
    }

    private void LogToFile(string message)
    {
        try
        {
            File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
        }
        catch
        {
            // 忽略日志写入错误
        }
    }
}
