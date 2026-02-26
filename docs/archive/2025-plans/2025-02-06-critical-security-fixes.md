# Critical Security Fixes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复代码审查中发现的 5 个关键安全问题，确保生产环境部署安全。

**架构:** 采用分层防御策略，从配置、验证、限流三个层面加固安全性。移除硬编码敏感信息，添加环境变量支持，增强输入验证和请求限制。

**Tech Stack:** ASP.NET Core 8.0, Microsoft.Extensions.Configuration, Environment Variables

---

## Task 1: 移除硬编码密钥，使用环境变量

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/appsettings.json:1-50`
- Modify: `backend/src/DataForgeStudio.Api/Program.cs:60-80`
- Create: `backend/src/DataForgeStudio.Api/appsettings.Example.json`

**Step 1: 创建示例配置文件**

创建 `backend/src/DataForgeStudio.Api/appsettings.Example.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost;Initial Catalog=DataForgeStudio_V4;User Id=sa;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Security": {
    "Jwt": {
      "Secret": "CHANGE_THIS_64_CHARACTER_RANDOM_SECRET_KEY_IN_PRODUCTION",
      "Issuer": "DataForgeStudio",
      "Audience": "DataForgeStudio",
      "ExpirationMinutes": 15
    },
    "Encryption": {
      "AesKey": "CHANGE_THIS_32_CHARACTER_AES_KEY",
      "AesIV": "CHANGE_THIS_16_BYTE_IV"
    },
    "License": {
      "AesKey": "CHANGE_THIS_32_CHARACTER_LICENSE_AES_KEY",
      "AesIv": "CHANGE_THIS_16_BYTE_LICENSE_IV"
    }
  },
  "SecurityOptionsUseDefaultsForTesting": false
}
```

**Step 2: 修改 appsettings.json，移除硬编码密钥**

编辑 `backend/src/DataForgeStudio.Api/appsettings.json`，将所有密钥值替换为占位符：

```json
"Security": {
  "Jwt": {
    "Secret": "",
    "Issuer": "DataForgeStudio",
    "Audience": "DataForgeStudio",
    "ExpirationMinutes": 15
  },
  "Encryption": {
    "AesKey": "",
    "AesIV": ""
  },
  "License": {
    "AesKey": "",
    "AesIv": ""
  }
}
```

同时将 ExpirationMinutes 从 60 改为 15。

**Step 3: 验证 Program.cs 的环境变量读取逻辑**

确认 `backend/src/DataForgeStudio.Api/Program.cs` 中 `SecurityOptions` 已正确配置从环境变量读取（第 60-80 行）：

```csharp
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
```

如果不存在，添加环境变量配置：

```csharp
// 在配置 builder 之后添加
builder.Configuration.AddEnvironmentVariables(prefix: "DFS_");
```

**Step 4: 运行测试验证**

```bash
cd backend/src/DataForgeStudio.Api
dotnet build
```

Expected: 编译成功，无错误

**Step 5: 创建环境变量文档**

创建 `docs/ENVIRONMENT_VARIABLES.md`：

```markdown
# 环境变量配置

生产环境必须配置以下环境变量：

## 安全配置

```bash
# JWT 密钥 (64 字符随机字符串)
DFS_JWT_SECRET="your-64-character-random-secret-key-here-change-in-production"

# AES 加密密钥 (32 字符)
DFS_ENCRYPTION_AESKEY="your-32-character-aes-key-here"

# AES IV (16 字符)
DFS_ENCRYPTION_AESIV="your-16-character-iv-here"

# 许可证 AES 密钥 (32 字符)
DFS_LICENSE_AESKEY="your-32-character-license-key-here"

# 许可证 AES IV (16 字符)
DFS_LICENSE_AESIV="your-16-character-license-iv-here"
```

## 生成密钥

使用以下 PowerShell 命令生成安全密钥：

```powershell
# 生成 64 字符 JWT 密钥
-Join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | % {[char]$_})

# 生成 32 字符 AES 密钥
-Join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})

# 生成 16 字符 IV
-Join ((48..57) + (65..90) + (97..122) | Get-Random -Count 16 | % {[char]$_})
```
```

**Step 6: Commit**

```bash
git add backend/src/DataForgeStudio.Api/appsettings.json backend/src/DataForgeStudio.Api/appsettings.Example.json docs/ENVIRONMENT_VARIABLES.md
git commit -m "security: remove hardcoded secrets and use environment variables

- Replace hardcoded keys with placeholders in appsettings.json
- Add appsettings.Example.json as template
- Set JWT expiration to 15 minutes (down from 60)
- Add environment variables documentation"
```

---

## Task 2: 添加请求体大小限制

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Program.cs:40-50`

**Step 1: 定位服务配置位置**

找到 `backend/src/DataForgeStudio.Api/Program.cs` 中 `builder.Services` 配置部分（约第 40-50 行）。

**Step 2: 添加 Kestrel 请求体大小限制**

在 `builder.Services` 配置之后添加：

```csharp
// 配置 Kestrel 服务器限制
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});
```

**Step 3: 添加表单大小限制**

在 `builder.Services` 中添加：

```csharp
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});
```

需要添加命名空间：
```csharp
using Microsoft.AspNetCore.Http.Features;
```

**Step 4: 运行测试验证**

```bash
cd backend/src/DataForgeStudio.Api
dotnet build
```

Expected: 编译成功

**Step 5: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Program.cs
git commit -m "security: add request body size limit to prevent DoS attacks

- Limit MaxRequestBodySize to 10MB
- Limit MultipartBodyLengthLimit to 10MB
- Set RequestHeadersTimeout to 30 seconds"
```

---

## Task 3: 配置安全响应头

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/DataForgeStudio.Api.csproj:1-30`
- Modify: `backend/src/DataForgeStudio.Api/Program.cs:200-220`

**Step 1: 添加 NetEscapades.AspNetCore.SecurityHeaders 包**

```bash
cd backend/src/DataForgeStudio.Api
dotnet add package NetEscapades.AspNetCore.SecurityHeaders
```

**Step 2: 验证包已添加**

检查 `DataForgeStudio.Api.csproj` 中包含：

```xml
<PackageReference Include="NetEscapades.AspNetCore.SecurityHeaders" Version="*.*.*" />
```

**Step 3: 创建安全头配置**

创建 `backend/src/DataForgeStudio.Api/SecurityHeadersConfig.cs`：

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NetEscapades.AspNetCore.SecurityHeaders.Infrastructure;

namespace DataForgeStudio.Api;

public static class SecurityHeadersConfig
{
    public static HeaderPolicyCollection GetHeaderPolicyCollection(bool isDevelopment)
    {
        var policy = new HeaderPolicyCollection()
            .AddFrameOptionsDeny()
            .AddContentTypeOptionsNoSniff()
            .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365) // 1 year
            .AddReferrerPolicyStrictOriginWhenCrossOrigin()
            .AddContentSecurityPolicy(builder =>
            {
                if (isDevelopment)
                {
                    builder
                        .AddDefaultSrc()
                            .WithSelf()
                            .WithUnsafeInline()
                            .WithUnsafeEval()
                        .AddScriptSrc()
                            .WithSelf()
                            .WithUnsafeInline()
                            .WithUnsafeEval()
                        .AddStyleSrc()
                            .WithSelf()
                            .WithUnsafeInline();
                }
                else
                {
                    builder
                        .AddDefaultSrc()
                            .WithSelf()
                        .AddScriptSrc()
                            .WithSelf()
                        .AddStyleSrc()
                            .WithSelf()
                            .WithUnsafeInline()
                        .AddImgSrc()
                            .WithSelf()
                            .WithData()
                            .WithBlob()
                        .AddConnectSrc()
                            .WithSelf();
                }
            })
            .RemoveServerHeader()
            .AddXssProtectionBlock()
            .AddCustomHeader("X-Powered-By", "DataForgeStudio V4");

        return policy;
    }
}
```

**Step 4: 在 Program.cs 中注册安全头中间件**

找到 `backend/src/DataForgeStudio.Api/Program.cs` 中的 `app.Use...` 中间件配置部分（约第 200-220 行）。

在 `app.UseHttpsRedirection()` 之后添加：

```csharp
var isDevelopment = builder.Environment.IsDevelopment();
app.UseSecurityHeaders(SecurityHeadersConfig.GetHeaderPolicyCollection(isDevelopment));
```

需要添加命名空间：
```csharp
using NetEscapades.AspNetCore.SecurityHeaders;
```

**Step 5: 运行测试验证**

```bash
cd backend/src/DataForgeStudio.Api
dotnet build
```

Expected: 编译成功

**Step 6: Commit**

```bash
git add backend/src/DataForgeStudio.Api/DataForgeStudio.Api.csproj backend/src/DataForgeStudio.Api/SecurityHeadersConfig.cs backend/src/DataForgeStudio.Api/Program.cs
git commit -m "security: add security headers middleware

- Add X-Frame-Options: DENY
- Add X-Content-Type-Options: nosniff
- Add HSTS with 1 year max-age
- Add CSP with development/production policies
- Remove Server header
- Add X-XSS-Protection"
```

---

## Task 4: 增强 SQL 注入防护 - 表名白名单验证

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/DatabaseService.cs:470-490`

**Step 1: 创建表名验证服务**

创建 `backend/src/DataForgeStudio.Core/Services/SqlTableNameValidator.cs`：

```csharp
using System.Text.RegularExpressions;

namespace DataForgeStudio.Core.Services;

public static class SqlTableNameValidator
{
    private static readonly Regex TableNameRegex = new(
        @"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)?$",
        RegexOptions.Compiled | RegexOptions.Singleline
    );

    private static readonly HashSet<string> AllowedSystemTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "information_schema.tables",
        "information_schema.columns",
        "sys.tables",
        "sys.columns",
        "sys.views"
    };

    public static (bool IsValid, string? Error) ValidateTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return (false, "表名不能为空");
        }

        // 检查长度限制
        if (tableName.Length > 128)
        {
            return (false, "表名过长");
        }

        // 检查格式
        if (!TableNameRegex.IsMatch(tableName))
        {
            return (false, $"表名格式无效: {tableName}");
        }

        // 检查危险关键词
        var lowerName = tableName.ToLowerInvariant();
        var dangerousKeywords = new[] { "drop", "delete", "truncate", "insert", "update", "alter", "create", "exec", "execute", "--", ";", "/*", "*/" };
        foreach (var keyword in dangerousKeywords)
        {
            if (lowerName.Contains(keyword))
            {
                return (false, $"表名包含危险关键词: {keyword}");
            }
        }

        return (true, null);
    }

    public static void ValidateAndThrow(string tableName)
    {
        var (isValid, error) = ValidateTableName(tableName);
        if (!isValid)
        {
            throw new ArgumentException($"无效的表名: {error}", nameof(tableName));
        }
    }
}
```

**Step 2: 修改 DatabaseService 使用验证器**

编辑 `backend/src/DataForgeStudio.Core/Services/DatabaseService.cs`，找到 `GetTableStructureAsync` 方法（约第 470-490 行）。

在方法开始处添加验证：

```csharp
public async Task<ApiResponse<List<TableColumnDto>>> GetTableStructureAsync(int dataSourceId, string tableName)
{
    // 验证表名
    SqlTableNameValidator.ValidateAndThrow(tableName);

    // ... 现有代码 ...
}
```

**Step 3: 添加日志记录**

在验证失败时记录安全事件：

```csharp
public static void ValidateAndThrow(string tableName, ILogger? logger = null)
{
    var (isValid, error) = ValidateTableName(tableName);
    if (!isValid)
    {
        logger?.LogWarning("检测到无效表名访问尝试: TableName={TableName}, Error={Error}", tableName, error);
        throw new ArgumentException($"无效的表名: {error}", nameof(tableName));
    }
}
```

同时修改 `DatabaseService.cs` 中的调用：

```csharp
SqlTableNameValidator.ValidateAndThrow(tableName, _logger);
```

**Step 4: 运行测试验证**

```bash
cd backend/src/DataForgeStudio.Api
dotnet build
```

Expected: 编译成功

**Step 5: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/SqlTableNameValidator.cs backend/src/DataForgeStudio.Core/Services/DatabaseService.cs
git commit -m "security: add SQL table name whitelist validation

- Add SqlTableNameValidator with format and keyword checking
- Validate table names before database queries
- Add security logging for invalid table name attempts
- Prevent SQL injection via table name parameters"
```

---

## Task 5: 强制新用户使用强密码

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/UserService.cs:110-130`
- Create: `backend/src/DataForge.Core/Validators/PasswordValidator.cs`

**Step 1: 创建密码验证器**

创建 `backend/src/DataForgeStudio.Core/Validators/PasswordValidator.cs`：

```csharp
namespace DataForgeStudio.Core.Validators;

public static class PasswordValidator
{
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public static PasswordValidationResult ValidatePassword(string? password)
    {
        var result = new PasswordValidationResult();

        if (string.IsNullOrWhiteSpace(password))
        {
            result.Errors.Add("密码不能为空");
            return result;
        }

        if (password.Length < 8)
        {
            result.Errors.Add("密码长度至少为 8 个字符");
        }

        if (password.Length > 128)
        {
            result.Errors.Add("密码长度不能超过 128 个字符");
        }

        if (!password.Any(char.IsLower))
        {
            result.Errors.Add("密码必须包含至少一个小写字母");
        }

        if (!password.Any(char.IsUpper))
        {
            result.Errors.Add("密码必须包含至少一个大写字母");
        }

        if (!password.Any(char.IsDigit))
        {
            result.Errors.Add("密码必须包含至少一个数字");
        }

        // 检查弱密码
        var weakPasswords = new[] { "password", "12345678", "abcdefgh", "qwerty123" };
        if (weakPasswords.Any(weak => password.ToLowerInvariant().Contains(weak)))
        {
            result.Errors.Add("密码过于简单，请使用更复杂的密码");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    public static void ValidateAndThrow(string? password)
    {
        var result = ValidatePassword(password);
        if (!result.IsValid)
        {
            throw new ArgumentException(string.Join("; ", result.Errors));
        }
    }
}
```

**Step 2: 修改 UserService 使用密码验证器**

编辑 `backend/src/DataForgeStudio.Core/Services/UserService.cs`，找到 `CreateUserAsync` 方法（约第 110-130 行）。

移除默认密码逻辑：
```csharp
// 修改前
PasswordHash = EncryptionHelper.HashPassword(request.Password ?? "123456"),

// 修改后
PasswordHash = EncryptionHelper.HashPassword(request.Password ?? throw new ArgumentException("密码不能为空")),
```

在方法开始处添加密码验证：

```csharp
public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request, int createdBy)
{
    // 验证密码强度
    var passwordValidation = PasswordValidator.ValidatePassword(request.Password);
    if (!passwordValidation.IsValid)
    {
        return ApiResponse<UserDto>.Fail(string.Join("; ", passwordValidation.Errors));
    }

    // ... 现有代码 ...
}
```

**Step 3: 同时修改 ChangePasswordAsync 方法**

找到 `ChangePasswordAsync` 方法（约第 180-200 行），添加密码验证：

```csharp
public async Task<ApiResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
{
    // 验证新密码强度
    var passwordValidation = PasswordValidator.ValidatePassword(request.NewPassword);
    if (!passwordValidation.IsValid)
    {
        return ApiResponse.Fail(string.Join("; ", passwordValidation.Errors));
    }

    // ... 现有代码 ...
}
```

**Step 4: 更新 DTO 添加密码要求注释**

编辑 `backend/src/DataForgeStudio.Shared/DTO/UserDto.cs`，在 `CreateUserRequest` 类的 `Password` 属性添加注释：

```csharp
/// <summary>
/// 密码（至少 8 个字符，必须包含大小写字母和数字）
/// </summary>
public string Password { get; set; } = string.Empty;
```

**Step 5: 运行测试验证**

```bash
cd backend/src/DataForgeStudio.Api
dotnet build
```

Expected: 编译成功

**Step 6: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Validators/PasswordValidator.cs backend/src/DataForgeStudio.Core/Services/UserService.cs backend/src/DataForgeStudio.Shared/DTO/UserDto.cs
git commit -m "security: enforce strong password policy

- Add PasswordValidator with complexity requirements
- Require min 8 chars with uppercase, lowercase, and numbers
- Remove default password fallback
- Validate password in CreateUser and ChangePassword
- Add password requirements documentation"
```

---

## Task 6: 验证所有修复

**Files:**
- Test: All modified files

**Step 1: 构建后端项目**

```bash
cd backend/src/DataForgeStudio.Api
dotnet build --no-incremental
```

Expected: 编译成功，0 个错误

**Step 2: 验证配置文件**

```bash
cd backend/src/DataForgeStudio.Api
grep -n "Secret" appsettings.json
```

Expected: Secret 值为空字符串

**Step 3: 检查环境变量文档**

```bash
cat docs/ENVIRONMENT_VARIABLES.md
```

Expected: 文档存在且包含所有必需的环境变量

**Step 4: 运行应用启动测试**

```bash
cd backend/src/DataForgeStudio.Api
dotnet run --no-launch-profile &
PID=$!
sleep 5
kill $PID 2>/dev/null || true
```

Expected: 应用启动，显示缺少环境变量警告（但不崩溃）

**Step 5: 最终 Commit**

```bash
git add -A
git commit -m "docs: add security fixes validation and testing notes"
```

---

## 完成标准

- [ ] 所有硬编码密钥已移除
- [ ] JWT 过期时间缩短至 15 分钟
- [ ] 请求体大小限制已配置（10MB）
- [ ] 安全响应头中间件已添加
- [ ] SQL 表名验证器已实现
- [ ] 密码强度验证已实现
- [ ] 环境变量文档已创建
- [ ] 后端编译成功，无错误
- [ ] 所有更改已提交到 git

---

## 部署前检查清单

生产部署前必须配置以下环境变量：

```bash
DFS_JWT_SECRET="<64字符随机密钥>"
DFS_ENCRYPTION_AESKEY="<32字符AES密钥>"
DFS_ENCRYPTION_AESIV="<16字符IV>"
DFS_LICENSE_AESKEY="<32字符许可证密钥>"
DFS_LICENSE_AESIV="<16字符许可证IV>"
```

如果没有配置环境变量，应用将使用开发模式默认值（仅用于测试）。
