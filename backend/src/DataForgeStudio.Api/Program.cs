using System.Text;
using DataForgeStudio.Api.Middleware;
using DataForgeStudio.Api.Services;
using DataForgeStudio.Core.Configuration;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Core.Services;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Data.Repositories;
using DataForgeStudio.Domain.Interfaces;
using DataForgeStudio.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NetEscapades.AspNetCore.SecurityHeaders;
using Serilog;
using Serilog.Events;

// 在创建 builder 之前检测测试环境
var isTestingEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing" ||
                           (args != null && args.Contains("--testing"));

var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());

// 配置 Serilog 日志
var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "logs", "api-.log");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();
Console.WriteLine($"日志文件路径: {Path.GetFullPath(logPath)}");

// 添加 Windows 服务支持
builder.Services.AddWindowsService();

// 再次检查 builder 中的环境
if (builder.Environment.IsEnvironment("Testing"))
{
    isTestingEnvironment = true;
}

// 使用硬编码的生产密钥（编译时嵌入）
Console.WriteLine("=== 安全配置 ===");
Console.WriteLine($"使用版本内置密钥");

// JWT 配置
var jwtSecret = ProductionKeys.JwtSecret;
var jwtIssuer = ProductionKeys.JwtIssuer;
var jwtAudience = ProductionKeys.JwtAudience;

Console.WriteLine($"JWT 配置完成 (密钥长度: {jwtSecret.Length})");

// 加密配置
var aesKey = ProductionKeys.AesKey;
var aesIV = ProductionKeys.AesIV;

Console.WriteLine($"AES 配置完成 (密钥长度: {aesKey.Length}, IV长度: {aesIV.Length})");

// 许可证配置
var licenseAesKey = ProductionKeys.LicenseAesKey;
var licenseAesIV = ProductionKeys.LicenseAesIV;

Console.WriteLine($"许可证配置完成");

// 将配置写入 Configuration 供其他服务读取
builder.Configuration["Security:Jwt:Secret"] = jwtSecret;
builder.Configuration["Security:Jwt:Issuer"] = jwtIssuer;
builder.Configuration["Security:Jwt:Audience"] = jwtAudience;
builder.Configuration["Security:Encryption:AesKey"] = aesKey;
builder.Configuration["Security:Encryption:AesIV"] = aesIV;
builder.Configuration["Security:License:AesKey"] = licenseAesKey;
builder.Configuration["Security:License:AesIv"] = licenseAesIV;

// 配置 CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" };
var corsMethods = builder.Configuration.GetSection("Cors:AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
var corsHeaders = builder.Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>() ?? new[] { "Authorization", "Content-Type", "X-Requested-With" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .WithMethods(corsMethods)
              .WithHeaders(corsHeaders)
              .AllowCredentials();
    });
});

// 配置数据库 - 支持加密的连接字符串
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var masterConnectionString = builder.Configuration.GetConnectionString("MasterConnection");

// 检查是否需要解密连接字符串（如果包含 EncryptedPassword= 则表示密码已加密）
connectionString = ConnectionStringHelper.DecryptIfNeeded(connectionString, builder.Configuration);
masterConnectionString = ConnectionStringHelper.DecryptIfNeeded(masterConnectionString, builder.Configuration);

// 验证连接字符串并测试连接
Console.WriteLine("=== 数据库连接配置 ===");
Console.WriteLine($"连接字符串（脱敏）: {SanitizeConnectionString(connectionString)}");

try
{
    using var testConnection = new SqlConnection(masterConnectionString);
    testConnection.Open();
    Console.WriteLine("✅ 数据库连接测试成功");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 数据库连接测试失败: {ex.Message}");
    Console.WriteLine($"   连接字符串详情: {SanitizeConnectionString(connectionString)}");
    Console.WriteLine($"   请检查:");
    Console.WriteLine($"   1. SQL Server 服务是否运行");
    Console.WriteLine($"   2. 服务器地址和端口是否正确");
    Console.WriteLine($"   3. 认证方式（Windows/SQL）是否正确");
    Console.WriteLine($"   4. 用户名密码是否正确（SQL 认证）");
    Console.WriteLine($"   5. Windows 账户是否有权限（Windows 认证）");
    // 不阻止启动，让应用继续运行以便诊断
}

// 脱敏连接字符串（隐藏密码）
static string SanitizeConnectionString(string? connectionString)
{
    if (string.IsNullOrEmpty(connectionString)) return "(空)";
    return System.Text.RegularExpressions.Regex.Replace(
        connectionString,
        @"(Password|Pwd|EncryptedPassword)\s*=\s*[^;]+",
        "$1=***",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}

builder.Services.AddDbContext<DataForgeStudioDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

// 存储解密后的连接字符串供其他服务使用
builder.Services.AddSingleton<IDbConnectionStringProvider>(sp =>
    new DbConnectionStringProvider(connectionString, masterConnectionString));

// 注册仓储和服务
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IDataSourceService, DataSourceService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<ISystemService, SystemService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
builder.Services.AddScoped<ISqlValidationService, SqlValidationService>();
builder.Services.AddScoped<IReportCacheService, ReportCacheService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IDirectoryService, DirectoryService>();

// 试用期追踪器（用于防止试用期重置）
builder.Services.AddScoped<ITrialLicenseTracker, TrialLicenseTracker>();

// 注册备份计划后台服务
builder.Services.AddHostedService<DataForgeStudio.Api.Services.BackupBackgroundService>();

// 注册内存缓存（用于许可证验证缓存和报表查询缓存）
builder.Services.AddMemoryCache();

// 配置表单大小限制，防止 DoS 攻击
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

// 配置 Kestrel 服务器限制，防止 DoS 攻击
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// 配置 JWT 认证（从 ProductionKeys 读取）
var secretKey = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = builder.Environment.IsProduction();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero
    };
});

// 生产环境启用 HSTS
if (builder.Environment.IsProduction())
{
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
    });
}

builder.Services.AddAuthorization(options =>
{
    // 默认策略：需要认证
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 配置控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// 配置 Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DataForgeStudio API",
        Version = "v1",
        Description = "DataForgeStudio V1.0 报表管理系统 API",
        Contact = new OpenApiContact
        {
            Name = "DataForgeStudio"
        }
    });

    // 添加 JWT 认证到 Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 注册 HttpContextAccessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 注册应用启动后的初始化回调（异步执行，不阻塞服务启动）
// 这样 Windows 服务可以快速报告"已启动"状态，避免 1053 超时错误
if (!isTestingEnvironment)
{
    app.Lifetime.ApplicationStarted.Register(async () =>
    {
        try
        {
            using var scope = app.Services.CreateScope();

            // 初始化数据库 - 创建 root 用户和默认权限
            var dbContext = scope.ServiceProvider.GetRequiredService<DataForgeStudioDbContext>();
            await DbInitializer.InitializeAsync(dbContext, forceResetPermissions: false);

            // 应用待处理的数据库迁移（仅在数据库已存在时）
            try
            {
                if (await dbContext.Database.CanConnectAsync())
                {
                    var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
                    if (pendingMigrations.Any())
                    {
                        Console.WriteLine($"📦 发现 {pendingMigrations.Count} 个待处理的数据库迁移，正在应用...");
                        await dbContext.Database.MigrateAsync();
                        Console.WriteLine("✅ 数据库迁移已完成");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 数据库迁移检查失败（非致命错误）: {ex.Message}");
            }

            // 初始化密钥 - 生成 RSA 密钥对（如果不存在）
            var keyService = scope.ServiceProvider.GetRequiredService<IKeyManagementService>();
            await keyService.EnsureKeyPairExistsAsync();
            await keyService.EnsureAesKeyExistsAsync();

            // 初始化试用期 - 记录首次运行时间并自动生成试用许可证（如果是首次运行）
            var trialTracker = scope.ServiceProvider.GetRequiredService<ITrialLicenseTracker>();
            var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseService>();

            var trialStatus = trialTracker.CheckTrialStatus();
            if (trialStatus.IsFirstRun)
            {
                Console.WriteLine("🎉 检测到首次运行，正在初始化试用期...");
                trialTracker.RecordFirstRun();
                Console.WriteLine($"✅ 试用期已激活，有效期 {TrialLicenseTracker.TRIAL_DAYS_STRING} 天");
            }

            // 无论是否首次运行，只要试用期有效，就尝试生成试用许可证（如果没有现有许可证）
            if (trialStatus.IsValid || trialStatus.IsFirstRun)
            {
                // 自动生成试用许可证
                var trialResult = await licenseService.GenerateTrialLicenseAsync();
                if (trialResult.Success)
                {
                    Console.WriteLine($"✅ 试用许可证已生成/更新");
                }
                else if (trialResult.ErrorCode == "TRIAL_USED")
                {
                    Console.WriteLine($"📋 试用许可证已存在");
                }
                else
                {
                    Console.WriteLine($"⚠️ 试用许可证生成失败: {trialResult.Message}");
                }

                if (!trialStatus.IsFirstRun && trialStatus.IsValid)
                {
                    Console.WriteLine($"📋 试用期剩余: {trialStatus.DaysRemaining} 天");
                }
            }
            else
            {
                Console.WriteLine($"⚠️ 试用期状态: {trialStatus.ErrorMessage ?? "已过期"}");
            }

            Console.WriteLine("✅ 应用初始化完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 初始化失败: {ex.Message}");
            // 不抛出异常，允许服务继续运行
        }
    });
}

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DataForgeStudio API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// 安全响应头中间件（在 HTTPS 重定向之后）
var isDevelopment = app.Environment.IsDevelopment();
app.UseSecurityHeaders(DataForgeStudio.Api.SecurityHeadersConfig.GetHeaderPolicyCollection(isDevelopment));

// 速率限制中间件（必须在 CORS 之后）
app.UseMiddleware<DataForgeStudio.Api.Middleware.RateLimitMiddleware>();

// 生产环境启用 HSTS
if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// 操作日志记录中间件（必须在认证之后）
app.UseMiddleware<OperationLogMiddleware>();

app.MapControllers();

// 健康检查端点 (允许匿名访问)
app.MapGet("/health", () => new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0"
})
.WithName("HealthCheck")
.WithTags("Health")
.ExcludeFromDescription()
.AllowAnonymous();

// API 信息端点 (允许匿名访问)
app.MapGet("/api", () => new
{
    Name = "DataForgeStudio API",
    Version = "1.0.0",
    Description = "报表管理系统 API",
    Documentation = "/swagger"
})
.WithName("ApiInfo")
.WithTags("Info")
.ExcludeFromDescription()
.AllowAnonymous();

// 确保应用退出时刷新日志
var host = app;
AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

// 集成测试需要公开的 Program 类
public partial class Program { }
