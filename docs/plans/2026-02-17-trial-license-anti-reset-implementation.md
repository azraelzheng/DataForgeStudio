# 试用许可证防重置机制实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 实现多位置 DPAPI 加密存储机制，防止用户通过卸载重置试用期

**Architecture:** 新增 `TrialLicenseTracker` 服务类，使用 DPAPI 加密试用期数据，存储在注册表、ProgramData、应用目录三个位置，启动时交叉验证并取最早时间

**Tech Stack:** .NET 8.0, DPAPI (System.Security.Cryptography.ProtectedData), Registry, File System

---

## Task 1: 添加 DPAPI NuGet 包依赖

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj`

**Step 1: 添加 ProtectedData 包引用**

在 `<ItemGroup>` 中添加：

```xml
<PackageReference Include="System.Security.Cryptography.ProtectedData" Version="8.0.0" />
```

**Step 2: 验证包安装**

Run: `cd backend && dotnet restore && dotnet build DataForgeStudio.sln --no-restore`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj
git commit -m "feat: add DPAPI package for trial license protection"
```

---

## Task 2: 创建试用期追踪器服务

**Files:**
- Create: `backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs`

**Step 1: 创建服务接口和基础结构**

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Shared.Utils;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 试用期许可证追踪器 - 防止卸载重置试用期
/// 使用 DPAPI 加密，多位置存储，交叉验证
/// </summary>
public interface ITrialLicenseTracker
{
    /// <summary>
    /// 获取首次运行时间（如果已过期返回 null）
    /// </summary>
    TrialLicenseStatus CheckTrialStatus();

    /// <summary>
    /// 记录首次运行（如果不存在）
    /// </summary>
    void RecordFirstRun();
}

/// <summary>
/// 试用期状态
/// </summary>
public class TrialLicenseStatus
{
    public bool IsValid { get; set; }
    public DateTime? FirstRunTime { get; set; }
    public DateTime? ExpiryTime { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsFirstRun { get; set; }
}

/// <summary>
/// 试用期追踪器实现
/// </summary>
public class TrialLicenseTracker : ITrialLicenseTracker
{
    private readonly ILogger<TrialLicenseTracker> _logger;
    private const int TRIAL_DAYS = 15;
    private const int DATA_VERSION = 1;

    // 存储位置配置
    private static readonly string RegistryPath = @"SOFTWARE\Microsoft\CryptoAPI\v2\machine";
    private static readonly string RegistryValueName = "CacheData";
    private static readonly string ProgramDataBasePath = @"Microsoft\Crypto\RSA\MachineKeys";
    private static readonly string AppCacheFileName = ".runtime_cache";

    public TrialLicenseTracker(ILogger<TrialLicenseTracker> logger)
    {
        _logger = logger;
    }

    // ... 后续任务实现
}
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded (可能有警告，无错误)

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs
git commit -m "feat: add TrialLicenseTracker interface and base class"
```

---

## Task 3: 实现数据模型和校验和计算

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs`

**Step 1: 添加内部数据模型类**

在 `TrialLicenseTracker` 类内部添加：

```csharp
/// <summary>
/// 试用期数据（存储在文件中）
/// </summary>
private class TrialData
{
    public int V { get; set; }  // 版本号
    public string Mc { get; set; } = string.Empty;  // 机器码
    public string Fr { get; set; } = string.Empty;  // 首次运行时间 (ISO 8601)
    public string Te { get; set; } = string.Empty;  // 试用到期时间 (ISO 8601)
    public string Cs { get; set; } = string.Empty;  // 校验和
    public string Nk { get; set; } = string.Empty;  // 随机数

    public static TrialData Create(string machineCode)
    {
        var now = DateTime.UtcNow;
        var nonce = Guid.NewGuid().ToString("N");
        var data = new TrialData
        {
            V = DATA_VERSION,
            Mc = machineCode,
            Fr = now.ToString("O"),
            Te = now.AddDays(TRIAL_DAYS).ToString("O"),
            Nk = nonce
        };
        data.Cs = ComputeChecksum(data);
        return data;
    }

    public bool ValidateChecksum()
    {
        return Cs == ComputeChecksum(this);
    }

    public bool ValidateMachineCode(string currentMachineCode)
    {
        return Mc == currentMachineCode;
    }

    private static string ComputeChecksum(TrialData data)
    {
        var input = $"{data.V}|{data.Mc}|{data.Fr}|{data.Te}|{data.Nk}";
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs
git commit -m "feat: add TrialData model with checksum validation"
```

---

## Task 4: 实现 DPAPI 加密解密方法

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs`

**Step 1: 添加加密解密方法**

在 `TrialLicenseTracker` 类中添加：

```csharp
#region DPAPI 加密解密

/// <summary>
/// 使用 DPAPI 加密数据
/// </summary>
private byte[] EncryptData(TrialData data)
{
    var json = JsonSerializer.Serialize(data);
    var plainBytes = Encoding.UTF8.GetBytes(json);

    // 使用 MachineKey 作用域，绑定到当前机器
    return ProtectedData.Protect(plainBytes, null, DataProtectionScope.LocalMachine);
}

/// <summary>
/// 使用 DPAPI 解密数据
/// </summary>
private TrialData? DecryptData(byte[] encryptedBytes)
{
    try
    {
        var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
        var json = Encoding.UTF8.GetString(plainBytes);
        return JsonSerializer.Deserialize<TrialData>(json);
    }
    catch (CryptographicException ex)
    {
        _logger.LogWarning(ex, "DPAPI 解密失败，数据可能被篡改或在其他机器生成");
        return null;
    }
    catch (JsonException ex)
    {
        _logger.LogWarning(ex, "试用期数据 JSON 解析失败");
        return null;
    }
}

#endregion
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs
git commit -m "feat: add DPAPI encrypt/decrypt methods"
```

---

## Task 5: 实现存储位置路径生成

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs`

**Step 1: 添加路径生成方法**

在 `TrialLicenseTracker` 类中添加：

```csharp
#region 存储路径

/// <summary>
/// 获取基于机器码的稳定 GUID（用于文件命名）
/// </summary>
private string GetStableGuid()
{
    var machineCode = EncryptionHelper.GetMachineCode();
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineCode + "_trial_tracker"));
    // 取前 16 字节转换为 GUID 格式
    var guidBytes = new byte[16];
    Array.Copy(hash, guidBytes, 16);
    return new Guid(guidBytes).ToString("N");
}

/// <summary>
/// 获取注册表存储路径
/// </summary>
private (string path, string value) GetRegistryPath()
{
    return (RegistryPath, RegistryValueName);
}

/// <summary>
/// 获取 ProgramData 文件路径
/// </summary>
private string GetProgramDataPath()
{
    var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
    var guid = GetStableGuid();
    var dir = Path.Combine(programData, ProgramDataBasePath);

    // 确保目录存在
    if (!Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
    }

    return Path.Combine(dir, $"{guid}.dat");
}

/// <summary>
/// 获取应用目录缓存文件路径
/// </summary>
private string GetAppCachePath()
{
    // 获取应用程序所在目录
    var appDir = AppDomain.CurrentDomain.BaseDirectory;
    var configDir = Path.Combine(appDir, "config");

    // 确保目录存在
    if (!Directory.Exists(configDir))
    {
        Directory.CreateDirectory(configDir);
    }

    return Path.Combine(configDir, AppCacheFileName);
}

#endregion
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs
git commit -m "feat: add storage path generation methods"
```

---

## Task 6: 实现多位置读写方法

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs`

**Step 1: 添加注册表读写方法**

在 `TrialLicenseTracker` 类中添加：

```csharp
#region 注册表操作

private TrialData? ReadFromRegistry()
{
    try
    {
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryPath);
        if (key == null) return null;

        var value = key.GetValue(RegistryValueName) as byte[];
        if (value == null) return null;

        return DecryptData(value);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "读取注册表试用期数据失败");
        return null;
    }
}

private bool WriteToRegistry(TrialData data)
{
    try
    {
        // 确保路径存在
        var pathParts = RegistryPath.Split('\\');
        var currentPath = "";

        foreach (var part in pathParts)
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}\\{part}";
            using var parentKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(currentPath);
        }

        using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(RegistryPath);
        var encrypted = EncryptData(data);
        key.SetValue(RegistryValueName, encrypted, Microsoft.Win32.RegistryValueKind.Binary);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "写入注册表试用期数据失败");
        return false;
    }
}

#endregion
```

**Step 2: 添加文件读写方法**

```csharp
#region 文件操作

private TrialData? ReadFromFile(string filePath)
{
    try
    {
        if (!File.Exists(filePath)) return null;

        var encrypted = File.ReadAllBytes(filePath);
        return DecryptData(encrypted);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "读取文件试用期数据失败: {Path}", filePath);
        return null;
    }
}

private bool WriteToFile(string filePath, TrialData data)
{
    try
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var encrypted = EncryptData(data);
        File.WriteAllBytes(filePath, encrypted);

        // 设置隐藏和只读属性
        var fileInfo = new FileInfo(filePath);
        fileInfo.Attributes |= FileAttributes.Hidden | FileAttributes.ReadOnly;

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "写入文件试用期数据失败: {Path}", filePath);
        return false;
    }
}

#endregion
```

**Step 3: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs
git commit -m "feat: add registry and file read/write methods"
```

---

## Task 7: 实现核心验证逻辑

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs`

**Step 1: 实现 CheckTrialStatus 方法**

```csharp
public TrialLicenseStatus CheckTrialStatus()
{
    var machineCode = EncryptionHelper.GetMachineCode();
    var validRecords = new List<(TrialData data, string location)>();

    // 1. 从三个位置读取数据
    var registryData = ReadFromRegistry();
    if (registryData != null && ValidateRecord(registryData, machineCode))
    {
        validRecords.Add((registryData, "Registry"));
    }

    var programDataPath = GetProgramDataPath();
    var programDataRecord = ReadFromFile(programDataPath);
    if (programDataRecord != null && ValidateRecord(programDataRecord, machineCode))
    {
        validRecords.Add((programDataRecord, "ProgramData"));
    }

    var appCachePath = GetAppCachePath();
    var appCacheRecord = ReadFromFile(appCachePath);
    if (appCacheRecord != null && ValidateRecord(appCacheRecord, machineCode))
    {
        validRecords.Add((appCacheRecord, "AppCache"));
    }

    // 2. 判断情况
    if (validRecords.Count == 0)
    {
        // 首次运行
        _logger.LogInformation("未找到有效的试用期记录，判定为首次运行");
        return new TrialLicenseStatus
        {
            IsValid = true,
            IsFirstRun = true,
            DaysRemaining = TRIAL_DAYS
        };
    }

    // 3. 取最早的首次运行时间（防篡改）
    var earliestRecord = validRecords
        .OrderBy(r => DateTime.Parse(r.data.Fr))
        .First();

    var firstRun = DateTime.Parse(earliestRecord.data.Fr);
    var expiry = DateTime.Parse(earliestRecord.data.Te);
    var now = DateTime.UtcNow;

    // 4. 检查是否有位置需要修复
    if (validRecords.Count < 3)
    {
        _logger.LogInformation("检测到 {Count} 个位置缺失数据，正在修复", 3 - validRecords.Count);
        WriteToAllLocations(earliestRecord.data);
    }

    // 5. 验证试用期
    var isValid = now < expiry;
    var daysRemaining = Math.Max(0, (int)(expiry - now).TotalDays);

    _logger.LogInformation("试用期验证完成: 有效={IsValid}, 剩余天数={DaysRemaining}", isValid, daysRemaining);

    return new TrialLicenseStatus
    {
        IsValid = isValid,
        FirstRunTime = firstRun,
        ExpiryTime = expiry,
        DaysRemaining = daysRemaining,
        IsFirstRun = false
    };
}

/// <summary>
/// 验证单条记录是否有效
/// </summary>
private bool ValidateRecord(TrialData data, string currentMachineCode)
{
    // 验证校验和
    if (!data.ValidateChecksum())
    {
        _logger.LogWarning("试用期数据校验和不匹配，可能被篡改");
        return false;
    }

    // 验证机器码
    if (!data.ValidateMachineCode(currentMachineCode))
    {
        _logger.LogWarning("试用期数据机器码不匹配");
        return false;
    }

    return true;
}
```

**Step 2: 实现 RecordFirstRun 和 WriteToAllLocations 方法**

```csharp
public void RecordFirstRun()
{
    var machineCode = EncryptionHelper.GetMachineCode();
    var data = TrialData.Create(machineCode);

    WriteToAllLocations(data);

    _logger.LogInformation("已记录首次运行时间，试用期 {Days} 天，到期时间: {Expiry}",
        TRIAL_DAYS, data.Te);
}

/// <summary>
/// 写入所有存储位置
/// </summary>
private void WriteToAllLocations(TrialData data)
{
    // 写入注册表
    WriteToRegistry(data);

    // 写入 ProgramData
    WriteToFile(GetProgramDataPath(), data);

    // 写入应用目录
    WriteToFile(GetAppCachePath(), data);
}
```

**Step 3: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/TrialLicenseTracker.cs
git commit -m "feat: implement CheckTrialStatus and RecordFirstRun methods"
```

---

## Task 8: 注册服务到 DI 容器

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Program.cs`

**Step 1: 注册 TrialLicenseTracker 服务**

在服务注册区域添加：

```csharp
// 试用期追踪器
builder.Services.AddScoped<ITrialLicenseTracker, TrialLicenseTracker>();
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Program.cs
git commit -m "feat: register TrialLicenseTracker in DI container"
```

---

## Task 9: 修改 LicenseService 集成试用期追踪

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/LicenseService.cs`

**Step 1: 注入 ITrialLicenseTracker 依赖**

修改 `LicenseService` 构造函数：

```csharp
private readonly ITrialLicenseTracker _trialTracker;

public LicenseService(
    DataForgeStudioDbContext context,
    ILogger<LicenseService> logger,
    IKeyManagementService keyManagementService,
    IMemoryCache memoryCache,
    ITrialLicenseTracker trialTracker)  // 新增
{
    _context = context;
    _logger = logger;
    _keyManagementService = keyManagementService;
    _memoryCache = memoryCache;
    _trialTracker = trialTracker;  // 新增
}
```

**Step 2: 修改 GenerateTrialLicenseAsync 方法**

在方法开头添加试用期追踪检查：

```csharp
private async Task<ApiResponse<LicenseInfoDto>> GenerateTrialLicenseAsync()
{
    try
    {
        var machineCode = EncryptionHelper.GetMachineCode();

        // ===== 新增：使用 TrialLicenseTracker 检查试用期 =====
        var trialStatus = _trialTracker.CheckTrialStatus();

        if (trialStatus.IsFirstRun)
        {
            // 首次运行，记录时间
            _trialTracker.RecordFirstRun();
            _logger.LogInformation("首次运行，已记录试用期起始时间");
        }
        else if (!trialStatus.IsValid)
        {
            // 试用期已过期
            var expiryDate = trialStatus.ExpiryTime?.ToString("yyyy-MM-dd") ?? "未知";
            return ApiResponse<LicenseInfoDto>.Fail(
                $"试用期已过期（过期日期: {expiryDate}），请联系供应商购买正式许可证。",
                "TRIAL_EXPIRED");
        }

        // 试用期有效，继续生成数据库中的试用许可证记录
        _logger.LogInformation("试用期有效，剩余 {Days} 天", trialStatus.DaysRemaining);
        // ===== 新增结束 =====

        // 检查是否已有试用许可证（原有逻辑保持不变）
        var existingLicenses = await _context.Licenses
            .Where(l => l.MachineCode == machineCode)
            .ToListAsync();

        // ... 后续代码保持不变 ...
```

**Step 3: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/LicenseService.cs
git commit -m "feat: integrate TrialLicenseTracker into LicenseService"
```

---

## Task 10: 功能测试

**Files:**
- Test: 手动测试

**Step 1: 启动后端服务**

Run: `cd backend && dotnet run --project src/DataForgeStudio.Api`
Expected: 服务启动成功

**Step 2: 测试首次运行**

1. 确保数据库为空或删除现有许可证
2. 删除三个存储位置的数据（如果存在）：
   - 注册表: `HKLM\SOFTWARE\Microsoft\CryptoAPI\v2\machine`
   - ProgramData: `C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\*.dat`
   - 应用目录: `config\.runtime_cache`
3. 调用 `/api/license/validate`
Expected: 返回试用期有效，剩余15天

**Step 3: 验证数据已写入**

1. 检查注册表是否存在
2. 检查 ProgramData 文件是否存在
3. 检查应用目录文件是否存在
Expected: 三个位置都有数据

**Step 4: 测试防重置**

1. 删除数据库中的许可证记录
2. 只删除应用目录的缓存文件
3. 重启服务并调用 `/api/license/validate`
Expected: 仍然显示正确的试用期（从其他两个位置读取）

**Step 5: 测试过期场景**

1. 修改存储数据中的到期时间为过去日期（或等待15天）
2. 调用 `/api/license/validate`
Expected: 返回试用期已过期

**Step 6: Commit 测试通过标记**

```bash
git add -A
git commit -m "test: verify trial license anti-reset mechanism works correctly"
```

---

## Task 11: 更新文档

**Files:**
- Modify: `docs/PROJECT_STATUS.md` 或相关文档

**Step 1: 添加试用期防护说明**

在项目状态文档中添加试用期防护机制的说明。

**Step 2: Commit**

```bash
git add docs/
git commit -m "docs: update documentation with trial license protection mechanism"
```

---

## 验收标准

- [ ] 首次运行时，三个位置都写入加密的试用期数据
- [ ] 删除部分位置数据后，仍能正确读取试用期状态
- [ ] 试用期15天后，系统正确拒绝访问
- [ ] 卸载重装后，试用期状态保持（不重置）
- [ ] 所有单元测试和集成测试通过
