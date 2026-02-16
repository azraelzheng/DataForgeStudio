using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataForgeStudio.Data.Data;

namespace DataForgeStudio.Tests.Integration;

/// <summary>
/// 测试用 WebApplicationFactory - 使用 TestServer 直接创建测试服务器
/// </summary>
public class TestWebApplicationFactory : IDisposable
{
    private readonly IHost _host;
    private readonly TestServer _server;
    private readonly HttpClient _client;

    public TestWebApplicationFactory()
    {
        // 设置测试环境变量
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseEnvironment("Testing");
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    // 添加必要的服务 - 模拟 Program.cs 中的服务注册
                    services.AddControllers();
                    services.AddEndpointsApiExplorer();
                    services.AddSwaggerGen();
                    services.AddHttpContextAccessor();
                    services.AddMemoryCache();

                    // 使用 InMemory 数据库
                    services.AddDbContext<DataForgeStudioDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
                        options.EnableSensitiveDataLogging();
                    });
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();

                        // 添加健康检查端点
                        endpoints.MapGet("/health", () => new
                        {
                            Status = "Healthy",
                            Timestamp = DateTime.UtcNow,
                            Version = "1.0.0"
                        }).AllowAnonymous();

                        // 添加 API 信息端点
                        endpoints.MapGet("/api", () => new
                        {
                            Name = "DataForgeStudio V4 API",
                            Version = "1.0.0",
                            Description = "报表管理系统 API",
                            Documentation = "/swagger"
                        }).AllowAnonymous();
                    });
                });
            });

        _host = hostBuilder.Build();
        _host.Start();
        _server = _host.GetTestServer();
        _client = _server.CreateClient();
    }

    public HttpClient CreateClient()
    {
        return _client;
    }

    public void Dispose()
    {
        _client?.Dispose();
        _server?.Dispose();
        _host?.Dispose();
    }
}

/// <summary>
/// 公开的 Program 类用于集成测试
/// </summary>
public partial class Program { }
