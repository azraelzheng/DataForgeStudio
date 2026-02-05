using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataForgeStudio.Data.Data;

namespace DataForgeStudio.Tests.Integration;

/// <summary>
/// 测试用 WebApplicationFactory - 使用 InMemory 数据库
/// 使用自定义的 Program 入口点来避免顶级语句的问题
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 移除原有的 DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DataForgeStudioDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 添加 InMemory 数据库
            services.AddDbContext<DataForgeStudioDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
                options.EnableSensitiveDataLogging();
            });
        });
    }
}

/// <summary>
/// 公开的 Program 类用于集成测试
/// 在实际的 Program.cs 中添加: public partial class Program { }
/// </summary>
public partial class Program { }
