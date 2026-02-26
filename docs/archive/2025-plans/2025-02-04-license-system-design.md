# 许可证管理系统实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标:** 实现完整的许可证管理系统，包括 RSA 密钥管理、许可证生成工具、Web 应用许可证验证和试用版自动生成。

**架构:**
- 使用 RSA 非对称加密保护许可证完整性
- 使用 AES 对称加密保护许可证内容
- 许可证绑定服务器机器码防止迁移使用
- 独立控制台程序生成许可证
- Web 应用验证许可证（支持试用版自动生成）

**技术栈:**
- ASP.NET Core 8.0
- RSA 2048位加密（.NET 内置）
- AES 加密（.NET 内置）
- Entity Framework Core 8.0
- Vue 3 + Element Plus（前端）

---

## 任务概述

本实现计划分为 5 个主要任务：

1. **密钥管理** - 生成并配置 RSA 密钥对和 AES 密钥
2. **许可证数据模型** - 完善 License 实体和数据库结构
3. **许可证生成工具** - 创建独立控制台应用
4. **Web 应用验证** - 完整实现 LicenseService
5. **前端集成** - 更新许可证管理界面

---

## 任务 1: 密钥管理配置

**文件:**
- 创建: `backend/src/DataForgeStudio.Api/keys/` 目录（运行时生成）
- 修改: `backend/src/DataForgeStudio.Api/appsettings.json`

### 步骤 1: 添加密钥配置到 appsettings.json

**文件:** `backend/src/DataForgeStudio.Api/appsettings.json`

```json
{
  "License": {
    "PublicKeyPath": "keys/public_key.pem",
    "PrivateKeyPath": "keys/private_key.pem",
    "AesKey": "将自动生成",
    "AesIv": "将自动生成"
  }
}
```

### 步骤 2: 创建密钥生成服务

**文件:** `backend/src/DataForgeStudio.Core/Services/KeyManagementService.cs`

```csharp
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 密钥管理服务 - 用于生成和管理许可证密钥
/// </summary>
public class KeyManagementService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeyManagementService> _logger;

    public KeyManagementService(
        IConfiguration configuration,
        ILogger<KeyManagementService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 确保 RSA 密钥对存在，不存在则生成
    /// </summary>
    public async Task EnsureKeyPairExistsAsync()
    {
        var publicKeyPath = _configuration["License:PublicKeyPath"];
        var privateKeyPath = _configuration["License:PrivateKeyPath"];
        var keysDir = Path.GetDirectoryName(publicKeyPath);

        // 确保目录存在
        if (!Directory.Exists(keysDir))
        {
            Directory.CreateDirectory(keysDir);
        }

        // 检查密钥文件是否存在
        if (File.Exists(publicKeyPath) && File.Exists(privateKeyPath))
        {
            _logger.LogInformation("RSA 密钥对已存在，跳过生成");
            return;
        }

        _logger.LogInformation("RSA 密钥对不存在，开始生成...");

        // 生成 RSA 密钥对
        using var rsa = RSA.Create(2048);
        var publicKeyBytes = rsa.ExportRSAPublicKey();
        var privateKeyBytes = rsa.ExportRSAPrivateKey();

        // 保存到文件
        await File.WriteAllBytesAsync(publicKeyPath, publicKeyBytes);
        await File.WriteAllBytesAsync(privateKeyPath, privateKeyBytes);

        // 设置文件权限（仅管理员可读写）
        // Windows: 使用 File.SetAttributes
        // Linux/Mac: 使用 chmod

        _logger.LogInformation($"RSA 密钥对已生成：{publicKeyPath}");
    }

    /// <summary>
    /// 读取公钥
    /// </summary>
    public async Task<string> GetPublicKeyAsync()
    {
        var publicKeyPath = _configuration["License:PublicKeyPath"];
        return await File.ReadAllTextAsync(publicKeyPath);
    }

    /// <summary>
    /// 获取 AES 密钥
    /// </summary>
    public string GetAesKey()
    {
        return _configuration["License:AesKey"];
    }

    /// <summary>
    /// 获取 AES IV
    /// </summary>
    public string GetAesIv()
    {
        return _configuration["License:AesIv"];
    }

    /// <summary>
    /// 确保 AES 密钥存在，不存在则生成
    /// </summary>
    public async Task EnsureAesKeyExistsAsync()
    {
        var aesKey = _configuration["License:AesKey"];
        var aesIv = _configuration["License:AesIv"];

        if (!string.IsNullOrEmpty(aesKey) && !string.IsNullOrEmpty(aesIv))
        {
            _logger.LogInformation("AES 密钥已配置");
            return;
        }

        _logger.LogInformation("生成 AES 密钥...");

        // 生成 AES 密钥
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();

        var key = Convert.ToBase64String(aes.Key);
        var iv = Convert.ToBase64String(aes.IV);

        _logger.LogInformation($"AES 密钥已生成（请手动添加到 appsettings.json）：");
        _logger.LogInformation($"License:AesKey = \"{key}\"");
        _logger.LogInformation($"License:AesIv = \"{iv}\"");
    }
}
```

### 步骤 3: 注册密钥管理服务

**文件:** `backend/src/DataForgeStudio.Api/Program.cs`

在 `var builder = WebApplication.CreateBuilder(args);` 之后添加：

```csharp
// 注册密钥管理服务
builder.Services.AddScoped<KeyManagementService>();
```

### 步骤 4: 修改 Program.cs 初始化密钥

**文件:** `backend/src/DataForgeStudio.Api/Program.cs`

在 `var app = builder.Build();` 之前添加：

```csharp
// 确保密钥已生成（首次部署时自动生成）
using (var scope = app.Services.CreateScope())
{
    var keyService = scope.ServiceProvider.GetRequiredService<KeyManagementService>();
    await keyService.EnsureKeyPairExistsAsync();
    await keyService.EnsureAesKeyExistsAsync();
}
```

### 步骤 5: 提交密钥管理功能

```bash
cd H:\开发项目\DataForgeStudio_V4\.worktrees\license-system
git add backend/src/DataForgeStudio.Core/Services/KeyManagementService.cs
git add backend/src/DataForgeStudio.Api/Program.cs
git commit -m "feat: add RSA key management service with auto-generation"
```

---

## 任务 2: 许可证数据模型

**文件:**
- 修改: `backend/src/DataForgeStudio.Domain\Entities/Logs.cs` (License 实体)

### 步骤 1: 更新 License 实体

**文件:** `backend/src/DataForgeStudio.Domain\Entities/Logs.cs`

找到 `License` 类，确保包含以下字段：

```csharp
/// <summary>
/// 许可证表
/// </summary>
public class License
{
    [Key]
    public int LicenseId { get; set; }

    /// <summary>
    /// AES 加密的完整许可证 JSON
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string LicenseKey { get; set; } = string.Empty;

    /// <summary>
    /// RSA 签名（Base64）
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// 绑定的机器码
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string MachineCode { get; set; } = string.Empty;

    /// <summary>
    /// 激活时间
    /// </summary>
    public DateTime ActivatedTime { get; set; }

    /// <summary>
    /// 激活时的 IP 地址
    /// </summary>
    [MaxLength(50)]
    public string? ActivatedIP { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }
}
```

### 步骤 2: 更新 DbContext

**文件:** `backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs`

确保 `DbSet<License>` 存在（应该已经存在）。

### 步骤 3: 提交数据模型更新

```bash
git add backend/src/DataForgeStudio.Domain/Entities/Logs.cs
git commit -m "refactor: update License entity for encrypted license storage"
```

---

## 任务 3: 许可证生成工具

### 步骤 1: 创建许可证生成工具项目

**项目路径:** `backend/tools/LicenseGenerator/LicenseGenerator.csproj`

```bash
cd backend/tools
dotnet new console -n LicenseGenerator -o LicenseGenerator
cd LicenseGenerator
```

### 步骤 2: 添加项目引用

**文件:** `backend/tools/LicenseGenerator/LicenseGenerator.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\DataForgeStudio.Shared\DataForgeStudio.Shared.csproj" />
  </ItemGroup>
</Project>
```

### 步骤 3: 实现许可证生成工具主程序

**文件:** `backend/tools/LicenseGenerator/Program.cs`

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DataForgeStudio.Shared.Utils;

namespace LicenseGenerator;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  DataForgeStudio V4 许可证生成工具");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // 检查密钥文件
        var privateKeyPath = @"..\..\src\DataForgeStudio.Api\keys\private_key.pem";
        var publicKeyPath = @"..\..\src\DataForgeStudio.Api\keys\public_key.pem";

        if (!File.Exists(privateKeyPath) || !File.Exists(publicKeyPath))
        {
            Console.WriteLine("错误: 密钥文件不存在，请先运行 Web 应用生成密钥！");
            Console.WriteLine("  预期路径:");
            Console.WriteLine($"    私钥: {Path.GetFullPath(privateKeyPath)}");
            Console.WriteLine($"    公钥: {Path.GetFullPath(publicKeyPath)}");
            return;
        }

        // 读取私钥
        var privateKey = await File.ReadAllTextAsync(privateKeyPath);

        // 收集许可证信息
        Console.WriteLine("请输入许可证信息:");
        Console.Write("客户名称: ");
        var customerName = Console.ReadLine() ?? "";

        Console.WriteLine("许可证类型:");
        Console.WriteLine("  1. Trial (试用版，30天)");
        Console.WriteLine("  2. Standard (标准版)");
        Console.WriteLine("  3. Professional (专业版)");
        Console.WriteLine("  4. Enterprise (企业版)");
        Console.Write("选择 (1-4): ");
        var licenseTypeChoice = Console.ReadLine();
        var licenseType = licenseTypeChoice switch
        {
            "1" => "Trial",
            "2" => "Standard",
            "3" => "Professional",
            "4" => "Enterprise",
            _ => "Trial"
        };

        Console.Write("有效期（月）: ");
        var monthsStr = Console.ReadLine() ?? "12";
        int months = int.Parse(monthsStr);
        var expiryDate = DateTime.UtcNow.AddMonths(months);

        Console.Write("最大用户数: ");
        var maxUsersStr = Console.ReadLine() ?? "10";
        var maxUsers = int.Parse(maxUsersStr);

        Console.Write("最大报表数: ");
        var maxReportsStr = Console.ReadLine() ?? "50";
        var maxReports = int.Parse(maxReportsStr);

        Console.Write("最大数据源数: ");
        var maxDataSourcesStr = Console.ReadLine() ?? "5";
        var maxDataSources = int.Parse(maxDataSourcesStr);

        Console.WriteLine("启用功能:");
        Console.WriteLine("  [1] 报表设计");
        Console.WriteLine("  [2] 报表查询");
        Console.WriteLine("  [3] 图表展示");
        Console.WriteLine("  [4] Excel导出");
        Console.WriteLine("  [5] PDF导出");
        Console.WriteLine("  [6] 数据源管理");
        Console.WriteLine("  [7] 用户管理");
        Console.WriteLine("  [8] 角色管理");
        Console.WriteLine("输入要启用的功能编号（用逗号分隔，如 1,2,3,4,5,6）: ");
        var featuresInput = Console.ReadLine() ?? "1,2,3,4,5,6";
        var featuresMap = new Dictionary<string, string>
        {
            ["1"] = "报表设计",
            ["2"] = "报表查询",
            ["3"] = "图表展示",
            ["4"] = "Excel导出",
            ["5"] = "PDF导出",
            ["6"] = "数据源管理",
            ["7"] = "用户管理",
            ["8"] = "角色管理"
        };

        var features = featuresInput.Split(',')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => featuresMap.GetValueOrDefault(x, ""))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        Console.Write("客户机器码: ");
        var machineCode = Console.ReadLine() ?? "";

        // 生成许可证数据
        var licenseData = new
        {
            LicenseId = Guid.NewGuid().ToString("N"),
            CustomerName = customerName,
            ExpiryDate = expiryDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MaxUsers = maxUsers,
            MaxReports = maxReports,
            MaxDataSources = maxDataSources,
            Features = features,
            MachineCode = machineCode,
            IssuedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            LicenseType = licenseType
        };

        // 序列化为 JSON
        var licenseJson = JsonSerializer.Serialize(licenseData, new JsonSerializerOptions { WriteIndented = true });

        // 使用私钥签名
        var signature = EncryptionHelper.RsaSignData(licenseJson, privateKey);

        // 将签名添加到 JSON 中
        var licenseDataWithSignature = new
        {
            licenseData,
            Signature = signature
        };

        var licenseJsonWithSignature = JsonSerializer.Serialize(licenseDataWithSignature);

        // 使用 AES 加密
        var aesKey = "DataForgeStudioV4SecretKey32Bytes!!"; // 需要从配置读取
        var aesIv = "DataForgeI";
        var encryptedLicense = EncryptionHelper.AesEncrypt(licenseJsonWithSignature, aesKey, aesIv);

        // 生成文件名
        var fileName = $"{customerName.Replace(" ", "_")}_{licenseType}_{DateTime.Now:yyyyMMddHHmmss}.lic";
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

        // 保存许可证文件
        await File.WriteAllBytesAsync(outputFile, Encoding.UTF8.GetBytes(encryptedLicense));

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("✓ 许可证生成成功！");
        Console.WriteLine($"文件保存位置: {outputFile}");
        Console.WriteLine($"文件名: {fileName}");
        Console.WriteLine();
        Console.WriteLine("许可证信息:");
        Console.WriteLine($"  客户名称: {customerName}");
        Console.WriteLine($"  许可类型: {licenseType}");
        Console.WriteLine($"  有效期至: {expiryDate:yyyy-MM-dd}");
        Console.WriteLine($"  最大用户数: {maxUsers}");
        Console.WriteLine($"  最大报表数: {maxReports}");
        Console.WriteLine($"  最大数据源数: {maxDataSources}");
        Console.WriteLine($"  启用功能: {string.Join(", ", features)}");
        Console.WriteLine($"  机器码: {machineCode}");
        Console.WriteLine("========================================");
    }
}
```

### 步骤 4: 构建许可证生成工具

```bash
cd backend/tools/LicenseGenerator
dotnet build
```

### 步骤 5: 测试许可证生成工具

```bash
cd backend/tools/LicenseGenerator/bin/Debug/net8.0
LicenseGenerator.exe
```

按照提示输入测试信息，验证生成的 .lic 文件。

### 步骤 6: 提交许可证生成工具

```bash
cd H:\开发项目\DataForgeStudio_V4\.worktrees\license-system
git add backend/tools/LicenseGenerator/
git commit -m "feat: add license generator console tool"
```

---

## 任务 4: 完善 LicenseService 实现

**文件:**
- 修改: `backend/src/DataForgeStudio.Core/Services/LicenseService.cs`

### 步骤 1: 实现激活许可证方法

**文件:** `backend/src/DataForgeStudio.Core/Services/LicenseService.cs`

完全替换 `ActivateLicenseAsync` 方法：

```csharp
public async Task<ApiResponse<LicenseInfoDto>> ActivateLicenseAsync(ActivateLicenseRequest request, string? ipAddress)
{
    try
    {
        // 1. 使用 AES 解密许可证
        string licenseJson;
        try
        {
            var aesKey = _configuration["License:AesKey"];
            var aesIv = _configuration["License:AesIv"];
            licenseJson = EncryptionHelper.AesDecrypt(request.LicenseKey, aesKey, aesIv);
        }
        catch
        {
            return ApiResponse<LicenseInfoDto>.Fail("许可证文件格式错误或已损坏", "INVALID_FORMAT");
        }

        // 2. 解析 JSON
        LicenseData licenseData;
        try
        {
            licenseData = JsonSerializer.Deserialize<LicenseData>(licenseJson);
        }
        catch
        {
            return ApiResponse<LicenseInfoDto>.Fail("许可证内容格式错误", "INVALID_CONTENT");
        }

        // 3. 读取公钥验证签名
        var publicKey = await GetPublicKeyAsync();
        bool isValid = EncryptionHelper.RsaVerifyData(
            licenseJson,
            licenseData.Signature,
            publicKey
        );

        if (!isValid)
        {
            return ApiResponse<LicenseInfoDto>.Fail("许可证已被篡改，无法激活。请联系供应商重新获取许可证。", "TAMPERED");
        }

        // 4. 获取当前服务器机器码
        var currentMachineCode = EncryptionHelper.GetMachineCode();

        // 5. 验证机器码是否匹配
        if (licenseData.MachineCode != currentMachineCode)
        {
            return ApiResponse<LicenseInfoDto>.Fail(
                $"许可证与当前服务器不匹配\n许可证绑定机器码: {licenseData.MachineCode}\n当前服务器机器码: {currentMachineCode}\n\n请联系供应商，提供当前服务器机器码以重新生成许可证。",
                "MACHINE_MISMATCH"
            );
        }

        // 6. 检查是否过期
        if (licenseData.ExpiryDate < DateTime.UtcNow)
        {
            return ApiResponse<LicenseInfoDto>.Fail(
                $"许可证已过期（过期日期: {licenseData.ExpiryDate:yyyy-MM-dd}），请联系供应商续费。",
                "EXPIRED"
            );
        }

        // 7. 检查该机器码是否已激活过其他许可证
        var existingLicense = await _context.Licenses
            .Where(l => l.MachineCode == currentMachineCode)
            .FirstOrDefaultAsync();

        if (existingLicense != null)
        {
            // 替换为新的许可证
            _context.Licenses.Remove(existingLicense);
        }

        // 8. 保存许可证到数据库
        var license = new Domain.Entities.License
        {
            LicenseKey = request.LicenseKey, // 加密的完整 JSON
            Signature = licenseData.Signature,
            MachineCode = currentMachineCode,
            ActivatedTime = DateTime.Now,
            ActivatedIP = ipAddress,
            CreatedTime = DateTime.Now
        };

        _context.Licenses.Add(license);
        await _context.SaveChangesAsync();

        // 9. 返回许可证信息
        var licenseInfo = new LicenseInfoDto
        {
            LicenseId = license.LicenseId,
            LicenseType = licenseData.LicenseType,
            CustomerName = licenseData.CustomerName,
            ExpiryDate = licenseData.ExpiryDate,
            MaxUsers = licenseData.MaxUsers,
            MaxReports = licenseData.MaxReports,
            MaxDataSources = licenseData.MaxDataSources,
            Features = licenseData.Features
        };

        return ApiResponse<LicenseInfoDto>.Ok(licenseInfo, "许可证激活成功");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "激活许可证失败");
        return ApiResponse<LicenseInfoDto>.Fail($"许可证激活失败: {ex.Message}", "ACTIVATION_FAILED");
    }
}
```

### 步骤 2: 实现自动生成试用版方法

添加新方法到 `LicenseService.cs`:

```csharp
/// <summary>
/// 自动生成试用版许可证
/// </summary>
private async Task<ApiResponse<LicenseInfoDto>> GenerateTrialLicenseAsync()
{
    try
    {
        var currentMachineCode = EncryptionHelper.GetMachineCode();

        // 检查是否已有试用记录
        var hasTrialBefore = await _context.Licenses
            .AnyAsync(l => l.MachineCode == currentMachineCode);

        if (hasTrialBefore)
        {
            return ApiResponse<LicenseInfoDto>.Fail(
                "该机器已使用过试用版。如需延长试用期或购买正式版，请联系供应商。",
                "TRIAL_USED"
            );
        }

        // 生成试用版数据
        var trialExpiry = DateTime.UtcNow.AddDays(30);
        var trialData = new
        {
            LicenseId = Guid.NewGuid().ToString("N"),
            CustomerName = "试用用户",
            ExpiryDate = trialExpiry.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MaxUsers = 5,
            MaxReports = 10,
            MaxDataSources = 2,
            Features = new List<string> { "报表设计", "报表查询", "图表展示" },
            MachineCode = currentMachineCode,
            IssuedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            LicenseType = "Trial"
        };

        // 序列化并签名
        var trialJson = JsonSerializer.Serialize(trialData);
        var aesKey = _configuration["License:AesKey"];
        var aesIv = _configuration["License:AesIv"];

        // 使用私钥签名
        var privateKey = @"..\..\keys\private_key.pem";
        if (File.Exists(privateKey))
        {
            var privateKeyContent = await File.ReadAllTextAsync(privateKey);
            var signature = EncryptionHelper.RsaSignData(trialJson, privateKeyContent);
            trialData = new { trialData, Signature = signature };
            trialJson = JsonSerializer.Serialize(trialData);
        }

        var encryptedTrial = EncryptionHelper.AesEncrypt(trialJson, aesKey, aesIv);

        // 保存到数据库
        var license = new Domain.Entities.License
        {
            LicenseKey = encryptedTrial,
            Signature = trialData.Signature ?? "",
            MachineCode = currentMachineCode,
            ActivatedTime = DateTime.Now,
            ActivatedIP = "127.0.0.1",
            CreatedTime = DateTime.Now
        };

        _context.Licenses.Add(license);
        await _context.SaveChangesAsync();

        var licenseInfo = new LicenseInfoDto
        {
            LicenseId = license.LicenseId,
            LicenseType = "Trial",
            CustomerName = "试用用户",
            ExpiryDate = trialExpiry,
            MaxUsers = 5,
            MaxReports = 10,
            MaxDataSources = 2,
            Features = trialData.Features
        };

        _logger.LogInformation($"自动生成试用版许可证: {currentMachineCode}");

        return ApiResponse<LicenseInfoDto>.Ok(licenseInfo, "试用版许可证已自动激活");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "生成试用版许可证失败");
        return ApiResponse<LicenseInfoDto>.Fail($"生成试用版许可证失败: {ex.Message}", "TRIAL_GENERATION_FAILED");
    }
}
```

### 步骤 3: 更新验证许可证方法

修改 `ValidateLicenseAsync` 方法以支持缓存和强制刷新：

```csharp
private readonly IMemoryCache _cache;
private const string LicenseCacheKey = "SYSTEM_LICENSE";

public async Task<ApiResponse<LicenseValidationResponse>> ValidateLicenseAsync(bool forceRefresh = false)
{
    // 如果不强制刷新，先检查缓存
    if (!forceRefresh && _cache.TryGetValue(LicenseValidationResponse, out var cached))
    {
        return ApiResponse<LicenseValidationResponse>.Ok(cached);
    }

    var license = await _context.Licenses
        .Where(l => l.MachineCode == GetCurrentMachineCode())
        .OrderByDescending(l => l.ActivatedTime)
        .FirstOrDefaultAsync();

    if (license == null)
    {
        // 自动生成试用版
        var trialResult = await GenerateTrialLicenseAsync();
        if (!trialResult.Success)
        {
            return ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
            {
                Valid = false,
                Message = trialResult.Message ?? "无法激活许可证"
            });
        }
        license = await _context.Licenses
            .Where(l => l.MachineCode == GetCurrentMachineCode())
            .OrderByDescending(l => l.ActivatedTime)
            .FirstOrDefaultAsync();
    }

    // 验证许可证
    var result = await VerifyLicenseFromDatabase(license);

    if (result.Valid)
    {
        // 缓存 30 分钟
        _cache.Set(LicenseCacheKey, result, TimeSpan.FromMinutes(30));
    }

    return ApiResponse<LicenseValidationResponse>.Ok(result);
}

private async Task<LicenseValidationResponse> VerifyLicenseFromDatabase(Domain.Entities.License license)
{
    try
    {
        // 解密许可证
        var aesKey = _configuration["License:AesKey"];
        var aesIv = _configuration["License:AesIv"];
        var licenseJson = EncryptionHelper.AesDecrypt(license.LicenseKey, aesKey, aesIv);

        // 验证签名
        var publicKey = await GetPublicKeyAsync();
        var isValid = EncryptionHelper.RsaVerifyData(licenseJson, license.Signature, publicKey);

        if (!isValid)
        {
            return new LicenseValidationResponse
            {
                Valid = false,
                Message = "许可证已被篡改，请联系供应商重新获取许可证"
            };
        }

        // 解析许可证数据
        var licenseData = JsonSerializer.Deserialize<LicenseData>(licenseJson);

        // 检查过期
        if (licenseData.ExpiryDate < DateTime.UtcNow)
        {
            var daysExpired = (DateTime.UtcNow - licenseData.ExpiryDate).Days;
            return new LicenseValidationResponse
            {
                Valid = false,
                Message = $"许可证已过期 {daysExpired} 天"
            };
        }

        return new LicenseValidationResponse
        {
            Valid = true,
            Message = "许可证有效",
            LicenseInfo = new LicenseInfoDto
            {
                LicenseType = licenseData.LicenseType,
                CustomerName = licenseData.CustomerName,
                ExpiryDate = licenseData.ExpiryDate,
                MaxUsers = licenseData.MaxUsers,
                MaxReports = licenseData.MaxReports,
                MaxDataSources = licenseData.MaxDataSources,
                Features = licenseData.Features
            }
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "验证许可证失败");
        return new LicenseValidationResponse
        {
            Valid = false,
            Message = "许可证验证失败"
        };
    }
}

private string GetCurrentMachineCode()
{
    return EncryptionHelper.GetMachineCode();
}

private async Task<string> GetPublicKeyAsync()
{
    var publicKeyPath = _configuration["License:PublicKeyPath"];
    return await File.ReadAllTextAsync(publicKeyPath);
}
```

### 步骤 4: 添加辅助类

**文件:** `backend/src/DataForgeStudio.Core/DTO/LicenseData.cs`

```csharp
using System.Text.Json.Serialization;

namespace DataForgeStudio.Core.DTO;

/// <summary>
/// 许可证数据（用于序列化）
/// </summary>
public class LicenseData
{
    public string LicenseId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int MaxUsers { get; set; }
    public int MaxReports { get; set; }
    public int MaxDataSources { get; set; }
    public List<string> Features { get; set; } = new();
    public string MachineCode { get; set; } = string.Empty;
    public string IssuedDate { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}
```

### 步骤 5: 更新接口定义

**文件:** `backend/src/DataForgeStudio.Core\Interfaces\ILicenseService.cs`

添加方法签名：

```csharp
Task<ApiResponse<LicenseInfoDto>> GenerateTrialLicenseAsync();
```

### 步骤 6: 提交 LicenseService 更新

```bash
git add backend/src/DataForgeStudio.Core/Services/LicenseService.cs
git add backend/src/DataForgeStudio.Core/DTO/LicenseData.cs
git add backend/src/DataForgeStudio.Core/Interfaces/ILicenseService.cs
git commit -m "feat: implement complete license verification with trial generation"
```

---

## 任务 5: 前端许可证管理界面

**文件:**
- 修改: `frontend/src/views/license/LicenseManagement.vue`

### 步骤 1: 添加许可证状态 Pinia Store

**文件:** `frontend/src/stores/license.js`

```javascript
import { defineStore } from 'pinia'
import { systemApi } from '../api/request'
import { ElNotification } from 'element-plus'

export const useLicenseStore = defineStore('license', {
  state: () => ({
    license: null,
    licenseStatus: 'unknown', // unknown, valid, expired, trial
    restrictions: {
      isReadOnly: false,
      maxUsers: 0,
      maxReports: 0,
      maxDataSources: 0,
      features: []
    },
    warningMessage: null,
    warningLevel: null // info, warning, error
  }),

  getters: {
    // 检查是否可以创建用户
    canCreateUser: (state) => {
      if (state.restrictions.isReadOnly) return false
      const usedUsers = state.license?.usedUsers || 0
      return usedUsers < state.restrictions.maxUsers
    },

    // 检查是否有某个功能权限
    hasFeature: (state) => (feature) => {
      return state.restrictions.features.includes(feature)
    },

    // 检查是否试用版
    isTrial: (state) => {
      return state.license?.licenseType === 'Trial'
    },

    // 获取剩余天数
    daysRemaining: (state) => {
      if (!state.license?.expiryDate) return 0
      const expiry = new Date(state.license.expiryDate)
      const now = new Date()
      const diff = expiry - now
      return Math.ceil(diff / (1000 * 60 * 60 * 24))
    }
  },

  actions: {
    // 加载许可证信息
    async loadLicense({ forceRefresh = false } = {}) {
      try {
        const res = await systemApi.validateLicense({ forceRefresh })
        if (res.success) {
          this.license = res.data
          this.licenseStatus = res.data.valid ? 'valid' : 'expired'
          this.updateRestrictions()
        }
      } catch (error) {
        console.error('加载许可证失败:', error)
      }
    },

    // 激活许可证
    async activateLicense(licenseFile) {
      try {
        // 读取文件内容
        const reader = new FileReader()
        const content = await new Promise((resolve) => {
          reader.onload = (e) => {
            const text = e.target.result
            const base64 = btoa(text)
            resolve(base64)
          }
          reader.readAsText(licenseFile)
        })

        const res = await systemApi.activateLicense({
          licenseKey: content
        })

        if (res.success) {
          ElNotification.success({
            title: '许可证激活成功',
            message: res.message,
            type: 'success'
          })
          await this.loadLicense({ forceRefresh: true })
        } else {
          ElNotification.error({
            title: '许可证激活失败',
            message: res.message,
            duration: 0 // 不自动关闭
          })
        }
      } catch (error) {
        ElNotification.error({
          title: '许可证激活失败',
          message: error.message || '未知错误',
          duration: 0
        })
      }
    },

    // 检查操作权限
    checkOperation(operation, context) {
      if (this.restrictions.isReadOnly) {
        ElNotification.warning({
          title: '许可证限制',
          message: this.getReadOnlyMessage(),
          duration: 0
        })
        return false
      }

      switch (operation) {
        case 'createUser':
          if (!this.canCreateUser) {
            ElNotification.warning({
              title: '用户数超限',
              message: `当前用户数已达上限 (${this.license?.usedUsers || 0}/${this.restrictions.maxUsers})，请联系升级许可证。`,
              duration: 5000
            })
            return false
          }
          break

        case 'createReport':
          if (!this.hasFeature('报表设计')) {
            ElNotification.warning({
              title: '功能未授权',
              message: '该功能需要升级到正式版许可证才能使用。',
              duration: 5000
            })
            return false
          }
          break
      }

      return true
    },

    updateRestrictions() {
      if (!this.license) return

      this.restrictions.maxUsers = this.license.maxUsers
      this.restrictions.maxReports = this.license.maxReports
      this.restrictions.maxDataSources = this.license.maxDataSources
      this.restrictions.features = this.license.features || []

      // 检查是否只读模式
      if (this.licenseStatus === 'expired') {
        const daysOverdue = -this.daysRemaining
        if (daysOverdue > 30) {
          this.restrictions.isReadOnly = true
          this.warningMessage = `许可证已过期超过30天，当前为只读模式。请联系供应商续费。`
          this.warningLevel = 'error'
        } else {
          this.warningMessage = `许可证已过期 ${daysOverdue} 天，请及时续费以继续使用全部功能。`
          this.warningLevel = 'warning'
        }
      } else {
        this.restrictions.isReadOnly = false
        this.warningMessage = null
        this.warningLevel = null
      }
    },

    getReadOnlyMessage() {
      const daysOverdue = -this.daysRemaining
      if (daysOverdue > 30) {
        return '许可证已过期超过30天，当前为只读模式。请联系供应商续费。'
      }
      return `许可证已过期 ${daysOverdue} 天，请及时续费以继续使用全部功能。`
    }
  }
})
```

### 步骤 2: 更新 LicenseManagement.vue

**文件:** `frontend/src/views/license/LicenseManagement.vue`

更新许可证管理界面，添加：
- 机器码显示和复制功能
- 激活许可证功能（上传 .lic 文件）
- 许可证详细信息显示
- 分级处理的 UI 提示

### 步骤 3: 提交前端更新

```bash
cd H:\开发项目\DataForgeStudio_V4\.worktrees\license-system
git add frontend/src/stores/license.js
git add frontend/src/views/license/LicenseManagement.vue
git commit -m "feat: implement license store with permission checking"
```

---

## 任务 6: 测试和验证

### 步骤 1: 测试密钥生成

```bash
cd H:\开发项目\DataForgeStudio_V4\.worktrees\license-system
cd backend/src/DataForgeStudio.Api
dotnet run
```

检查输出确认密钥自动生成。

### 步骤 2: 测试许可证生成工具

```bash
cd backend/tools/LicenseGenerator/bin/Debug/net8.0
LicenseGenerator.exe
```

输入测试信息生成许可证，验证生成的 .lic 文件。

### 步骤 3: 测试许可证激活

启动前端和后端，在许可证管理页面上传 .lic 文件，验证激活功能。

### 步骤 4: 测试试用版自动生成

1. 删除数据库中的许可证记录
2. 重新访问系统
3. 验证是否自动生成 30 天试用版

### 步骤 5: 测试机器码验证

1. 激活一个许可证
2. 修改 `EncryptionHelper.GetMachineCode()` 返回不同的值
3. 验证是否提示机器码不匹配

### 步骤 6: 测试分级处理

1. 模拟许可证过期（修改过期日期）
2. 验证只读模式和功能禁用
3. 测试用户数超限提示

### 步骤 7: 提交测试更新

```bash
git add .
git commit -m "test: add license system tests and verify all scenarios"
```

---

## 完成检查清单

完成后验证以下功能：

- [ ] RSA 密钥对自动生成（首次部署时）
- [ ] 许可证生成工具正常工作
- [ ] 激活许可证功能正常
- [ ] 试用版自动生成（首次访问）
- [ ] 机器码验证正常
- [ ] 过期许可证正确检测
- [ ] 分级处理正确实现
- [ ] 前端许可证状态显示正确

---

## 部署注意事项

1. **首次部署**：首次运行会自动生成 RSA 密钥对
2. **密钥备份**：生成密钥后立即备份 `backend/keys/` 目录
3. **密钥迁移**：如需更换服务器，需要迁移密钥文件
4. **许可证过期**：过期后系统进入限制模式，需要续费
