# DataForgeStudio V4 - Security Hardening Implementation Plan

> **For Claude:** Execute using superpowers:subagent-driven-development after plan approval

**Goal:** Fix 6 critical security vulnerabilities to make the system production-ready

**Architecture:** ASP.NET Core 8.0 backend with environment-based configuration, layered security services, and middleware pipeline

**Tech Stack:** ASP.NET Core 8.0, Entity Framework Core, SQL Server 2005+, AspNetCoreRateLimit

---

## Overview

This plan addresses 6 CRITICAL security vulnerabilities identified in production code review:

1. **Hardcoded Encryption Keys** - All keys in appsettings.json
2. **Weak Default Password** - "admin123" for root user
3. **SQL Injection** - Raw SQL execution without validation
4. **No Rate Limiting** - Brute force vulnerability
5. **CORS Too Permissive** - AllowAnyOrigin()
6. **HTTPS Not Required** - Tokens over HTTP

---

## Task 1: Move Encryption Keys to Environment Variables

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/appsettings.json`
- Modify: `backend/src/DataForgeStudio.Api/Program.cs`
- Modify: `backend/src/DataForgeStudio.Shared/Utils/EncryptionHelper.cs`
- Create: `backend/src/DataForgeStudio.Core/Configuration/SecurityOptions.cs`

**Step 1: Create Security Options Class**

Create `backend/src/DataForgeStudio.Core/Configuration/SecurityOptions.cs`:

```csharp
namespace DataForgeStudio.Core.Configuration;

public class SecurityOptions
{
    public const string SectionName = "Security";

    public JwtOptions Jwt { get; set; } = new();
    public EncryptionOptions Encryption { get; set; } = new();
    public LicenseOptions License { get; set; } = new();

    public class JwtOptions
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = "DataForgeStudio";
        public string Audience { get; set; } = "DataForgeStudio.Client";
        public int ExpirationMinutes { get; set; } = 60;
    }

    public class EncryptionOptions
    {
        public string AesKey { get; set; } = string.Empty;
        public string AesIV { get; set; } = string.Empty;
    }

    public class LicenseOptions
    {
        public string AesKey { get; set; } = string.Empty;
        public string AesIv { get; set; } = string.Empty;
    }
}
```

**Step 2: Update Program.cs**

Add to `backend/src/DataForgeStudio.Api/Program.cs` before JWT configuration:

```csharp
// 配置安全选项（从环境变量读取）
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));

var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>();

// 验证必需的安全配置
if (string.IsNullOrEmpty(securityOptions?.Jwt?.Secret) ||
    securityOptions.Jwt.Secret.Length < 64)
{
    throw new InvalidOperationException(
        "JWT Secret 未配置或长度不足64位。请设置环境变量 DFS_JWT_SECRET (64+字符)");
}

if (string.IsNullOrEmpty(securityOptions?.Encryption?.AesKey) ||
    securityOptions.Encryption.AesKey.Length != 32)
{
    throw new InvalidOperationException(
        "AES Key 未配置或长度不是32位。请设置环境变量 DFS_ENCRYPTION_AES_KEY (32字符)");
}
```

**Step 3: Update JWT Configuration**

Replace lines 46-48 in Program.cs:

```csharp
// 从安全选项读取 JWT 配置
var jwtSecret = securityOptions.Jwt.Secret;
var secretKey = Encoding.UTF8.GetBytes(jwtSecret);
```

**Step 4: Update appsettings.json**

Replace lines 14-42 in `backend/src/DataForgeStudio.Api/appsettings.json`:

```json
"Jwt": {
  "Secret": "${DFS_JWT_SECRET}",
  "Issuer": "DataForgeStudio",
  "Audience": "DataForgeStudio.Client",
  "ExpirationMinutes": 60
},
"Encryption": {
  "AesKey": "${DFS_ENCRYPTION_AES_KEY}",
  "AesIV": "${DFS_ENCRYPTION_AES_IV}"
},
"License": {
  "PublicKeyPath": "keys/public_key.pem",
  "PrivateKeyPath": "keys/private_key.pem",
  "AesKey": "${DFS_LICENSE_AES_KEY}",
  "AesIv": "${DFS_LICENSE_AES_IV}"
}
```

**Step 5: Remove Hardcoded Keys from EncryptionHelper**

In `backend/src/DataForgeStudio.Shared/Utils/EncryptionHelper.cs`, remove lines 240-243 and 252-253 (fallback keys).

**Environment Variables Required:**
```bash
# Windows (Command Prompt)
setx DFS_JWT_SECRET "your-64-character-random-secret-key-here-change-in-production"
setx DFS_ENCRYPTION_AES_KEY "your-32-character-aes-key-here"
setx DFS_ENCRYPTION_AES_IV "your-16-character-aes-iv"
setx DFS_LICENSE_AES_KEY "your-32-character-license-aes-key"
setx DFS_LICENSE_AES_IV "your-16-character-license-aes-iv"

# Windows (PowerShell)
[System.Environment]::SetEnvironmentVariable("DFS_JWT_SECRET", "your-64-character-secret", "Machine")
```

---

## Task 2: Fix Default Root Password Security

**Files:**
- Modify: `backend/src/DataForgeStudio.Data/Data/DbInitializer.cs`
- Modify: `backend/src/DataForgeStudio.Core/Services/AuthenticationService.cs`
- Modify: `backend/src/DataForgeStudio.Api/Controllers/AuthController.cs`
- Modify: `backend/src/DataForgeStudio.Domain/Entities/User.cs`

**Step 1: Add MustChangePassword to User Entity**

Add property to `backend/src/DataForgeStudio.Domain/Entities/User.cs`:

```csharp
/// <summary>
/// 是否必须修改密码（首次登录）
/// </summary>
public bool MustChangePassword { get; set; }
```

**Step 2: Generate Random Password in DbInitializer**

Replace line 37 in `backend/src/DataForgeStudio.Data/Data/DbInitializer.cs`:

```csharp
// 生成强随机临时密码
var temporaryPassword = GenerateTemporaryPassword(16);
rootUser.PasswordHash = EncryptionHelper.HashPassword(temporaryPassword);
rootUser.MustChangePassword = true; // 强制首次登录修改密码

// 输出临时密码到控制台（生产环境需记录到安全位置）
Console.WriteLine("============================================");
Console.WriteLine("⚠️  IMPORTANT: Root User Temporary Password");
Console.WriteLine("============================================");
Console.WriteLine($"Username: root");
Console.WriteLine($"Password: {temporaryPassword}");
Console.WriteLine("⚠️  You MUST change this password on first login!");
Console.WriteLine("============================================");
```

Add helper method to DbInitializer:

```csharp
private static string GenerateTemporaryPassword(int length)
{
    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
    var random = new Random();
    return new string(Enumerable.Repeat(chars, length)
        .Select(s => s[random.Next(s.Length)]).ToArray());
}
```

**Step 3: Add Force Password Change Check**

Add to `LoginAsync` method in `backend/src/DataForgeStudio.Core/Services/AuthenticationService.cs`:

```csharp
// 检查是否需要强制修改密码
if (user.MustChangePassword)
{
    return new LoginResponse
    {
        Success = false,
        RequiresPasswordChange = true,
        Message = "首次登录必须修改密码",
        UserId = user.UserId
    };
}
```

**Step 4: Add Force Password Change Endpoint**

Add to `backend/src/DataForgeStudio.Api/Controllers/AuthController.cs`:

```csharp
[HttpPost("force-password-change")]
[AllowAnonymous]
public async Task<ApiResponse<bool>> ForcePasswordChange([FromBody] ForcePasswordChangeRequest request)
{
    var result = await _authenticationService.ForcePasswordChangeAsync(request);
    return result;
}
```

---

## Task 3: Implement SQL Injection Prevention

**Files:**
- Create: `backend/src/DataForgeStudio.Core/Services/SqlValidationService.cs`
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs`
- Modify: `backend/src/DataForgeStudio.Core/Services/DatabaseService.cs`

**Step 1: Create SQL Validation Service**

Create `backend/src/DataForgeStudio.Core/Services/SqlValidationService.cs`:

```csharp
public interface ISqlValidationService
{
    ValidationResult ValidateQuery(string sql);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class SqlValidationService : ISqlValidationService
{
    private static readonly HashSet<string> DangerousKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "DROP", "DELETE", "INSERT", "UPDATE", "ALTER", "CREATE", "TRUNCATE",
        "EXEC", "EXECUTE", "EXECutesql", "sp_executesql",
        "xp_cmdshell", "sp_oacreate", "sp_configure",
        "GRANT", "REVOKE", "DENY",
        "BULK", "OPENROWSET", "OPENDATASOURCE",
        "UNION", "SELECT INTO", "INTO OUTFILE",
        "sp_", "xp_", "DECLARE", "CURSOR"
    };

    public ValidationResult ValidateQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "SQL 语句不能为空" };
        }

        var trimmedSql = sql.Trim().ToUpper();

        // 必须以 SELECT 开头
        if (!trimmedSql.StartsWith("SELECT"))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "只允许 SELECT 查询" };
        }

        // 检查危险关键字
        foreach (var keyword in DangerousKeywords)
        {
            if (trimmedSql.Contains(keyword))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"SQL 包含危险关键字: {keyword}"
                };
            }
        }

        // 检查注释注入
        if (trimmedSql.Contains("--") || trimmedSql.Contains("/*"))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "SQL 包含注释字符" };
        }

        return new ValidationResult { IsValid = true };
    }
}
```

**Step 2: Register Service**

Add to `backend/src/DataForgeStudio.Api/Program.cs` (line 40):

```csharp
builder.Services.AddScoped<ISqlValidationService, SqlValidationService>();
```

**Step 3: Integrate into ReportService**

Update `TestQueryAsync` in `backend/src/DataForgeStudio.Core/Services/ReportService.cs`:

```csharp
public async Task<ApiResponse<List<Dictionary<string, object>>>> TestQueryAsync(TestQueryRequest request)
{
    // SQL 验证
    var validationResult = _sqlValidationService.ValidateQuery(request.Sql);
    if (!validationResult.IsValid)
    {
        // 记录阻止的查询
        _logger.LogWarning("SQL 查询被阻止: {Message}, SQL: {Sql}",
            validationResult.ErrorMessage, request.Sql);

        return ApiResponse<List<Dictionary<string, object>>>.Fail(validationResult.ErrorMessage);
    }

    // ... 继续执行查询
}
```

---

## Task 4: Add Rate Limiting to Endpoints

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/DataForgeStudio.csproj`
- Create: `backend/src/DataForgeStudio.Api/Configuration/RateLimitConfiguration.cs`
- Modify: `backend/src/DataForgeStudio.Api/Program.cs`

**Step 1: Install Package**

Run in `backend` directory:

```bash
dotnet add package AspNetCoreRateLimit
```

**Step 2: Create Rate Limit Configuration**

Create `backend/src/DataForgeStudio.Api/Configuration/RateLimitConfiguration.cs`:

```csharp
using AspNetCoreRateLimit;

public static class RateLimitConfiguration
{
    public static void ConfigureRateLimit(this IServiceCollection services, IConfiguration config)
    {
        services.AddMemoryCache();

        services.Configure<IpRateLimitOptions>(options =>
        {
            options.EnableEndpointRateLimiting = true;
            options.StackBlockedRequests = false;
            options.RealIpHeader = "X-Forwarded-For";
            options.HttpStatusCode = 429;
        });

        services.Configure<IpRateLimitPolicies>(options =>
        {
            // 登录端点：5次/15分钟
            options.AddPolicy("LoginPolicy", policy =>
            {
                policy.IpLimits.Add(path: "/api/auth/login", period: "15m", limit: 5);
            });

            // SQL测试：10次/5分钟
            options.AddPolicy("SqlTestPolicy", policy =>
            {
                policy.IpLimits.Add(path: "/api/reports/test-query", period: "5m", limit: 10);
            });

            // 通用API：100次/分钟
            options.AddPolicy("GeneralApi", policy =>
            {
                policy.IpLimits.Add(period: "1m", limit: 100);
            });
        });

        services.AddInMemoryRateLimiting();
        services.AddRateLimiter();
    }
}
```

**Step 3: Update Program.cs**

Add after line 43 in `backend/src/DataForgeStudio.Api/Program.cs`:

```csharp
// 配置速率限制
builder.Services.ConfigureRateLimit(builder.Configuration);
```

Add middleware after CORS (line 25):

```csharp
app.UseRateLimiter();
```

---

## Task 5: Fix CORS Configuration

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/appsettings.json`
- Modify: `backend/src/DataForgeStudio.Api/Program.cs`

**Step 1: Add CORS Configuration to appsettings.json**

Add to `backend/src/DataForgeStudio.Api/appsettings.json`:

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:5173",
    "http://localhost:5174",
    "https://dataforge.example.com"
  ],
  "AllowedMethods": [ "GET", "POST", "PUT", "DELETE", "PATCH" ],
  "AllowedHeaders": [ "Authorization", "Content-Type", "X-Requested-With" ]
}
```

**Step 2: Update Program.cs CORS Configuration**

Replace lines 16-24 in `backend/src/DataForgeStudio.Api/Program.cs`:

```csharp
// 配置 CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
var corsMethods = builder.Configuration.GetSection("Cors:AllowedMethods").Get<string[]>();
var corsHeaders = builder.Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>();

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
```

---

## Task 6: Enable HTTPS for Production

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/appsettings.json`
- Modify: `backend/src/DataForgeStudio.Api/Program.cs`
- Modify: `backend/src/DataForgeStudio.Api/Properties/launchSettings.json`

**Step 1: Enable HTTPS Metadata**

Change line 56 in `backend/src/DataForgeStudio.Api/Program.cs`:

```csharp
options.RequireHttpsMetadata = builder.Environment.IsProduction();
```

**Step 2: Add HSTS**

Add to Program.cs after HTTPS redirection:

```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
    });
}
```

Add to middleware pipeline:

```csharp
if (builder.Environment.IsProduction())
{
    app.UseHsts();
}
```

**Step 3: Configure Production HTTPS**

Add to `backend/src/DataForgeStudio.Api/appsettings.json`:

```json
"Kestrel": {
  "Endpoints": {
    "Http": {
      "Url": "http://*:5000"
    },
    "Https": {
      "Url": "https://*:5001",
      "Certificate": {
        "Path": "certificates/https.pfx",
        "Password": "${DFS_CERTIFICATE_PASSWORD}"
      }
    }
  }
}
```

---

## Verification

### Testing Checklist

**Task 1 (Environment Variables):**
- [ ] API starts with environment variables set
- [ ] API fails to start without environment variables
- [ ] JWT tokens work with new configuration
- [ ] Encryption/decryption works correctly

**Task 2 (Password Security):**
- [ ] Root user created with random password
- [ ] Temporary password displayed in console
- [ ] Login blocked until password changed
- [ ] Password change endpoint works

**Task 3 (SQL Injection):**
- [ ] DROP TABLE blocked
- [ ] UNION SELECT blocked
- [ ] Comments blocked
- [ ] Valid SELECT queries allowed
- [ ] Blocked queries logged

**Task 4 (Rate Limiting):**
- [ ] Login blocked after 5 attempts
- [ ] 429 status code returned
- [ ] Retry-After header present
- [ ] Rate limit resets after period

**Task 5 (CORS):**
- [ ] Allowed origins work
- [ ] Disallowed origins blocked
- [ ] Preflight OPTIONS handled
- [ ] Credentials sent correctly

**Task 6 (HTTPS):**
- [ ] Development: HTTP works
- [ ] Development: HTTPS not required
- [ ] Production: HTTPS required
- [ ] HSTS headers present

### End-to-End Test

1. Set all environment variables
2. Start API - should succeed
3. Try login with valid credentials - should work
4. Try login 6 times - should be rate limited
5. Try SQL injection - should be blocked
6. Try from disallowed origin - should be blocked by CORS
7. Verify HTTPS requirement in production

---

## Deployment Notes

1. **Environment Setup**: Set all required environment variables before starting API
2. **Certificate**: Install SSL certificate for production HTTPS
3. **Monitoring**: Check logs for SQL injection attempts
4. **Database Migration**: Run migration to add MustChangePassword column
5. **Testing**: Complete all verification steps before deploying
