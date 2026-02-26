# 代码混淆设计文档

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为关键程序集添加代码混淆，防止许可证验证逻辑被反编译破解

**Architecture:** 使用 Obfuscar 工具在发布构建时对 Core 和 Shared 程序集进行符号重命名、控制流混淆和字符串加密

**Tech Stack:** .NET 8.0, Obfuscar 2.x, PowerShell

---

## 1. 目标程序集

| 程序集 | 原因 | 优先级 |
|--------|------|--------|
| `DataForgeStudio.Core.dll` | LicenseService, KeyManagementService, 验证逻辑 | 高 |
| `DataForgeStudio.Shared.dll` | ProductionKeys, EncryptionHelper, 加密工具 | 高 |

## 2. 混淆级别

使用 **标准级别** 混淆：

| 功能 | 说明 |
|------|------|
| 符号重命名 | 类名、方法名、字段名、属性名重命名为无意义字符 |
| 控制流混淆 | 使代码逻辑难以追踪 |
| 字符串加密 | 加密硬编码的字符串常量 |

## 3. 保留规则

以下内容**不进行混淆**，确保功能正常：

- 公共接口 (`I*Service`)
- DTO 类 (`*Dto`, `*Request`, `*Response`)
- Entity Framework 实体
- API 控制器公共方法
- 依赖注入注册的类型

## 4. 集成时机

仅在运行 `build-installer.ps1` 时执行混淆：

```
[1/5] 构建 Backend API
      ↓
[NEW] 混淆 Core.dll 和 Shared.dll
      ↓
[2/5] 构建 Frontend
      ↓
...
```

## 5. 文件变更

| 文件 | 操作 | 说明 |
|------|------|------|
| `backend/obfuscar.xml` | 新增 | 混淆配置文件 |
| `backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj` | 修改 | 添加 Obfuscar 包引用 |
| `backend/src/DataForgeStudio.Shared/DataForgeStudio.Shared.csproj` | 修改 | 添加 Obfuscar 包引用 |
| `scripts/build-installer.ps1` | 修改 | 集成混淆步骤 |

## 6. 混淆效果示例

**混淆前:**
```csharp
public class LicenseService : ILicenseService
{
    private readonly string _aesKey = "secret-key";

    public bool ValidateLicense(string licenseKey)
    {
        return !string.IsNullOrEmpty(licenseKey);
    }
}
```

**混淆后:**
```csharp
public class a : ILicenseService
{
    private readonly string b = "\u0061\u0062\u0063"; // 加密的字符串

    public bool a(string b)
    {
        // 控制流被打乱，难以理解
        return b != null && b.Length > 0;
    }
}
```

## 7. 验证方式

1. 构建安装包
2. 使用 ILSpy 或 dnSpy 打开混淆后的 DLL
3. 确认 LicenseService 和 ProductionKeys 相关代码已混淆
4. 运行完整功能测试，确保许可证激活和验证正常

## 8. 注意事项

- 混淆仅影响发布版本，开发调试不受影响
- 如果出现运行时错误，检查是否遗漏了需要保留的类型
- Obfuscar 是开源工具，MIT 许可证，可商用
