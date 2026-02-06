using System.Text;
using DataForgeStudio.Api.Middleware;
using DataForgeStudio.Core.Configuration;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Core.Services;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Data.Repositories;
using DataForgeStudio.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 配置安全选项（优先从环境变量读取，其次从配置文件读取）
var securityOptionsConfig = new SecurityOptions();
var jwtOptions = securityOptionsConfig.GetJwtOptions(builder.Configuration);
var encryptionOptions = securityOptionsConfig.GetEncryptionOptions(builder.Configuration);
var licenseOptions = securityOptionsConfig.GetLicenseOptions(builder.Configuration);

// 验证必需的安全配置（开发环境可以使用默认值，生产环境必须配置）
var envName = builder.Environment.EnvironmentName;
Console.WriteLine($"Environment: {envName}");
Console.WriteLine($"IsDevelopment: {builder.Environment.IsDevelopment()}");

// 对于命令行运行，如果没有设置环境变量，使用默认值
// 生产环境部署时必须设置环境变量
var useDefaultsForTesting = builder.Configuration.GetValue<bool>("SecurityOptionsUseDefaultsForTesting", true);

if (string.IsNullOrEmpty(jwtOptions.Secret) || jwtOptions.Secret.Length < 64)
{
    if (useDefaultsForTesting)
    {
        // 测试环境使用默认值
        Console.WriteLine("⚠️  WARNING: Using default JWT Secret for testing. Set DFS_JWT_SECRET environment variable for production!");
        jwtOptions.Secret = "DataForgeStudioV4JWTSecretKey256BitsLongSecure2025ChangeThisInProduction";
    }
    else
    {
        throw new InvalidOperationException(
            "JWT Secret 未配置或长度不足64位。请设置环境变量 DFS_JWT_SECRET (64+字符)");
    }
}

if (string.IsNullOrEmpty(encryptionOptions.AesKey) || encryptionOptions.AesKey.Length != 32)
{
    if (useDefaultsForTesting)
    {
        // 测试环境使用默认值
        Console.WriteLine("⚠️  WARNING: Using default AES Key for testing. Set DFS_ENCRYPTION_AESKEY environment variable for production!");
        encryptionOptions.AesKey = "DataForgeStudioAESKey32BytesLong123456";
        if (string.IsNullOrEmpty(encryptionOptions.AesIV))
        {
            encryptionOptions.AesIV = "DataForgeIV16Byte!";
        }
    }
    else
    {
        throw new InvalidOperationException(
            "AES Key 未配置或长度不是32位。请设置环境变量 DFS_ENCRYPTION_AESKEY (32字符)");
    }
}

// 同时也需要为 License 配置设置默认值（用于测试）
if (string.IsNullOrEmpty(licenseOptions.AesKey))
{
    if (useDefaultsForTesting)
    {
        Console.WriteLine("⚠️  WARNING: Using default License AES Key for testing. Set DFS_LICENSE_AESKEY environment variable for production!");
        licenseOptions.AesKey = "DataForgeStudioV4AESLicenseKey32Bytes!!";
        if (string.IsNullOrEmpty(licenseOptions.AesIv))
        {
            licenseOptions.AesIv = "DataForgeIV16Byte!";
        }
    }
}

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

// 配置数据库
builder.Services.AddDbContext<DataForgeStudioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// 配置 JWT 认证（从安全选项读取）
var jwtSecret = jwtOptions.Secret;
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
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
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
        Title = "DataForgeStudio V4 API",
        Version = "v1",
        Description = "DataForgeStudio V4 报表管理系统 API",
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

// 初始化数据库 - 创建 root 用户和默认权限
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DataForgeStudioDbContext>();
    // 开发环境可以设置为 true 强制重建权限，生产环境设置为 false
    await DbInitializer.InitializeAsync(dbContext, forceResetPermissions: false);
}

// 初始化密钥 - 生成 RSA 密钥对（如果不存在）
using (var scope = app.Services.CreateScope())
{
    var keyService = scope.ServiceProvider.GetRequiredService<IKeyManagementService>();
    await keyService.EnsureKeyPairExistsAsync();
    await keyService.EnsureAesKeyExistsAsync();
}

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DataForgeStudio V4 API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

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
    Name = "DataForgeStudio V4 API",
    Version = "1.0.0",
    Description = "报表管理系统 API",
    Documentation = "/swagger"
})
.WithName("ApiInfo")
.WithTags("Info")
.ExcludeFromDescription()
.AllowAnonymous();

app.Run();

// 集成测试需要公开的 Program 类
public partial class Program { }
