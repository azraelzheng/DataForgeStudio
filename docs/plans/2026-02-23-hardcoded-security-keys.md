# 硬编码版本密钥方案实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将安全密钥硬编码到程序集中，用户无需配置，开发者可备份密钥文件。

**Architecture:** 创建 ProductionKeys 类存储所有密钥常量，Program.cs 和 ConfigService.cs 从此类读取。密钥在编译时嵌入程序集，运行时无需配置。

**Tech Stack:** C#, .NET 8.0, Conditional Compilation

---

## 概述

### 当前问题
- 用户需要配置 `Security:UseDefaultsForTesting: true` 或设置环境变量
- 密钥分散在多个文件中，容易不一致
- 发布流程复杂

### 解决方案
- 创建统一的 `ProductionKeys.cs` 存储所有密钥
- 移除 `UseDefaultsForTesting` 逻辑
- 创建 `secrets.production.json` 供开发者备份
- 生产环境 appsettings.json 不再需要 Security 配置

### 文件变更清单
| 文件 | 操作 |
|------|------|
| `backend/src/DataForgeStudio.Shared/Constants/ProductionKeys.cs` | 创建 |
| `backend/secrets.production.json` | 创建（不上传git） |
| `backend/secrets.development.json` | 创建（示例文件，上传git） |
| `backend/src/DataForgeStudio.Api/Program.cs` | 修改 |
| `backend/tools/DeployManager/Services/ConfigService.cs` | 修改 |
| `.gitignore` | 修改 |
| `backend/src/DataForgeStudio.Api/appsettings.json` | 修改 |

---

## Task 1: 创建 ProductionKeys 常量类

**Files:**
- Create: `backend/src/DataForgeStudio.Shared/Constants/ProductionKeys.cs`

**Step 1: 创建目录和文件**

```csharp
// backend/src/DataForgeStudio.Shared/Constants/ProductionKeys.cs
namespace DataForgeStudio.Shared.Constants;

/// <summary>
/// 生产环境安全密钥
///
/// 重要说明：
/// 1. 此文件包含生产环境使用的所有安全密钥
/// 2. 发布新版本前，请更新这些密钥
/// 3. 备份 secrets.production.json 到安全位置
/// 4. 密钥长度要求：
///    - AES Key: 精确 32 字符
///    - AES IV: 精确 16 字符
///    - JWT Secret: 至少 64 字符
/// </summary>
public static class ProductionKeys
{
    /// <summary>
    /// 数据加密 AES 密钥（精确 32 字符）
    /// 用于：数据库连接字符串密码加密、数据源密码加密
    /// </summary>
    public const string AesKey = "DataForgeStudioV4AESKey32Bytes";

    /// <summary>
    /// 数据加密 AES IV（精确 16 字符）
    /// </summary>
    public const string AesIV = "DataForgeIV16Byte!";

    /// <summary>
    /// JWT 签名密钥（至少 64 字符）
    /// 用于：用户登录 Token 签名
    /// </summary>
    public const string JwtSecret = "DataForgeStudioV4JWTSecretKey256BitsLongSecure2025ChangeThisInProduction";

    /// <summary>
    /// 许可证加密 AES 密钥（精确 32 字符）
    /// 用于：许可证文件加密
    /// </summary>
    public const string LicenseAesKey = "DataForgeStudioV4AESLicenseKey32Bytes";

    /// <summary>
    /// 许可证加密 AES IV（精确 16 字符）
    /// </summary>
    public const string LicenseAesIV = "DataForgeIV16Byte!";

    // ====== JWT 配置（通常不需要修改） ======

    public const string JwtIssuer = "DataForgeStudio";
    public const string JwtAudience = "DataForgeStudio.Client";
    public const int JwtExpirationMinutes = 15;
}
```

**Step 2: 验证文件创建成功**

Run: `ls backend/src/DataForgeStudio.Shared/Constants/`
Expected: `ProductionKeys.cs`

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Shared/Constants/ProductionKeys.cs
git commit -m "feat: add ProductionKeys constant class for security keys"
```

---

## Task 2: 创建密钥备份文件

**Files:**
- Create: `backend/secrets.production.json`
- Create: `backend/secrets.development.json.example`

**Step 1: 创建生产密钥备份文件**

```json
// backend/secrets.production.json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "Version": "4.0.0",
  "ReleaseDate": "2026-02-23",
  "Description": "DataForgeStudio 生产环境安全密钥 - 请妥善备份",

  "Keys": {
    "AesKey": "DataForgeStudioV4AESKey32Bytes",
    "AesIV": "DataForgeIV16Byte!",
    "JwtSecret": "DataForgeStudioV4JWTSecretKey256BitsLongSecure2025ChangeThisInProduction",
    "LicenseAesKey": "DataForgeStudioV4AESLicenseKey32Bytes",
    "LicenseAesIV": "DataForgeIV16Byte!"
  },

  "JwtConfig": {
    "Issuer": "DataForgeStudio",
    "Audience": "DataForgeStudio.Client",
    "ExpirationMinutes": 15
  },

  "Notes": [
    "发布新版本前，请生成新密钥并更新 ProductionKeys.cs",
    "将此文件备份到安全位置（密码管理器）",
    "此文件不应上传到 Git"
  ]
}
```

**Step 2: 创建开发示例文件**

```json
// backend/secrets.development.json.example
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "Version": "4.0.0",
  "Description": "开发环境密钥示例 - 复制为 secrets.development.json 使用",

  "Keys": {
    "AesKey": "DataForgeStudioV4AESKey32Bytes",
    "AesIV": "DataForgeIV16Byte!",
    "JwtSecret": "DataForgeStudioV4JWTSecretKey256BitsLongSecure2025ChangeThisInProduction",
    "LicenseAesKey": "DataForgeStudioV4AESLicenseKey32Bytes",
    "LicenseAesIV": "DataForgeIV16Byte!"
  },

  "Instructions": [
    "1. 复制此文件为 secrets.production.json",
    "2. 生成新的随机密钥替换默认值",
    "3. 更新 ProductionKeys.cs 中的常量",
    "4. 备份 secrets.production.json"
  ]
}
```

**Step 3: Commit**

```bash
git add backend/secrets.development.json.example
git commit -m "docs: add secrets backup file templates"
```

---

## Task 3: 更新 .gitignore

**Files:**
- Modify: `.gitignore`

**Step 1: 添加生产密钥文件到忽略列表**

在 `.gitignore` 的 `# ------------- Keys & Secrets -------------` 部分添加：

```gitignore
# ------------- Keys & Secrets -------------
backend/keys/
*.pem
*.key
**/keys/
*.lic
secrets.production.json
secrets.*.local.json
```

**Step 2: Commit**

```bash
git add .gitignore
git commit -m "chore: ignore production secrets file"
```

---

## Task 4: 修改 Program.cs 使用 ProductionKeys

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Program.cs`

**Step 1: 添加 using 语句**

在文件顶部添加：

```csharp
using DataForgeStudio.Shared.Constants;
```

**Step 2: 替换密钥配置逻辑**

将第 33-107 行的安全配置逻辑替换为：

```csharp
// 使用硬编码的生产密钥（编译时嵌入）
Console.WriteLine("=== 安全配置 ===");
Console.WriteLine($"使用版本内置密钥: v{ProductionKeys.JwtIssuer}");

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
```

**Step 3: 验证编译**

Run: `dotnet build backend/src/DataForgeStudio.Api/DataForgeStudio.Api.csproj`
Expected: Build succeeded, 0 errors

**Step 4: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Program.cs
git commit -m "refactor: use ProductionKeys for security configuration"
```

---

## Task 5: 修改 ConfigService.cs 使用 ProductionKeys

**Files:**
- Modify: `backend/tools/DeployManager/Services/ConfigService.cs`

**Step 1: 添加 using 语句**

在文件顶部添加：

```csharp
using DataForgeStudio.Shared.Constants;
```

**Step 2: 简化 GetEncryptionKeys 方法**

将 `GetEncryptionKeys()` 方法（约第 724-744 行）替换为：

```csharp
/// <summary>
/// 获取加密密钥（使用 ProductionKeys 硬编码密钥）
/// </summary>
private static (string key, string iv) GetEncryptionKeys()
{
    return (ProductionKeys.AesKey, ProductionKeys.AesIV);
}
```

**Step 3: 验证编译**

Run: `dotnet build backend/tools/DeployManager/DeployManager.csproj`
Expected: Build succeeded, 0 errors

**Step 4: Commit**

```bash
git add backend/tools/DeployManager/Services/ConfigService.cs
git commit -m "refactor: use ProductionKeys in ConfigService"
```

---

## Task 6: 更新 appsettings.json 模板

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/appsettings.json`

**Step 1: 移除 Security 配置段**

将 appsettings.json 简化为：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://*:5000"
      }
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost",
      "http://localhost:5173",
      "http://localhost:8089"
    ],
    "AllowedMethods": [ "GET", "POST", "PUT", "DELETE", "PATCH" ],
    "AllowedHeaders": [ "Authorization", "Content-Type", "X-Requested-With" ]
  }
}
```

**Note**: `ConnectionStrings` 由 Configurator 在安装时动态添加

**Step 2: Commit**

```bash
git add backend/src/DataForgeStudio.Api/appsettings.json
git commit -m "refactor: remove Security section from appsettings template"
```

---

## Task 7: 验证完整构建

**Step 1: 构建整个解决方案**

Run: `dotnet build backend/DataForgeStudio.sln -c Release`
Expected: Build succeeded, 0 errors, 0 warnings

**Step 2: 运行单元测试（如有）**

Run: `dotnet test backend/DataForgeStudio.sln`
Expected: All tests passed

**Step 3: 构建安装包**

Run: `scripts\build-installer.bat`
Expected: 构建成功，生成 DataForgeStudio-Setup.exe

**Step 4: Commit（如果需要）**

```bash
git status
# 确认所有更改已提交
```

---

## 完成后清单

- [ ] `ProductionKeys.cs` 已创建并包含所有密钥
- [ ] `secrets.production.json` 已创建并备份到安全位置
- [ ] `.gitignore` 已更新，排除生产密钥文件
- [ ] `Program.cs` 已简化，使用 ProductionKeys
- [ ] `ConfigService.cs` 已简化，使用 ProductionKeys
- [ ] `appsettings.json` 已移除 Security 配置段
- [ ] 完整构建成功
- [ ] 安装包生成成功

---

## 密钥更新流程（发布新版本时）

1. 生成新密钥：
   ```powershell
   # AES Key (32字符)
   -join ((65..90) + (97..122) + (48..57) + 33..47 | Get-Random -Count 32 | ForEach-Object {[char]$_})

   # AES IV (16字符)
   -join ((65..90) + (97..122) + (48..57) + 33..47 | Get-Random -Count 16 | ForEach-Object {[char]$_})

   # JWT Secret (64字符)
   -join ((65..90) + (97..122) + (48..57) + 33..47 | Get-Random -Count 64 | ForEach-Object {[char]$_})
   ```

2. 更新 `ProductionKeys.cs` 中的常量

3. 更新 `secrets.production.json` 并备份

4. 构建新版本安装包

5. 使用新密钥重新生成许可证文件
