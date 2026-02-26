# 混淆代码全面审查报告

## 审查日期：2026-02-26

## 审查范围

本次审查检查了 `DataForgeStudio.Core.dll` 和 `DataForgeStudio.Shared.dll` 的混淆配置，确保混淆不会导致运行时错误。

---

## 发现的问题及修复

### 问题 1：Shared.DTO 类属性被混淆（已修复）

**症状：** 登录请求返回 502 错误

**原因：** `SkipType` 只跳过类型名称，不跳过属性/字段

**修复：** 添加 `skipProperties="true" skipFields="true"`

```xml
<SkipType name="DataForgeStudio.Shared.DTO.*" skipProperties="true" skipFields="true" />
```

---

### 问题 2：Core.DTO 类属性被混淆（已修复）

**症状：** 许可证序列化/验证可能失败

**原因：** `DataForgeStudio.Core.DTO.LicenseData` 不在 Shared.DTO 命名空间中

**修复：** 添加 `DataForgeStudio.Core.DTO.*` 跳过规则

```xml
<SkipType name="DataForgeStudio.Core.DTO.*" skipProperties="true" skipFields="true" />
```

---

### 问题 3：服务类被混淆导致 TypeLoadException（之前已修复）

**提交：** `c1a0ef3`

**原因：** `Program.cs` 通过类名注册依赖注入，混淆后找不到类型

**修复：** 跳过 `DataForgeStudio.Core.Services.*`

---

### 问题 4：Utils 工具类被混淆导致 TypeLoadException（已修复）

**症状：** 服务启动失败，错误日志显示：
```
System.TypeLoadException: Could not load type 'DataForgeStudio.Shared.Utils.EncryptionHelper'
```

**原因：** `EncryptionHelper` 类被混淆成不可见字符，导致类型无法加载

**修复：** 跳过 `DataForgeStudio.Shared.Utils.*`

```xml
<SkipType name="DataForgeStudio.Shared.Utils.*" />
```

---

## 最终混淆配置

```xml
<?xml version='1.0'?>
<Obfuscator>
  <!-- 全局设置 -->
  <Var name="KeepPublicApi" value="false" />
  <Var name="HidePrivateApi" value="true" />
  <Var name="RenameProperties" value="true" />
  <Var name="RenameEvents" value="true" />
  <Var name="RenameFields" value="true" />
  <Var name="UseUnicodeNames" value="true" />
  <Var name="HideStrings" value="true" />
  <Var name="OptimizeMethods" value="true" />
  <Var name="SuppressIldasm" value="true" />

  <!-- DataForgeStudio.Core.dll -->
  <Module file="...">
    <!-- 接口：必须跳过，服务注册使用接口 -->
    <SkipType name="DataForgeStudio.Core.Interfaces.*" />

    <!-- 服务实现：必须跳过，Program.cs 通过类名注册 -->
    <SkipType name="DataForgeStudio.Core.Services.*" />

    <!-- 配置类：必须跳过，API 层访问 -->
    <SkipType name="DataForgeStudio.Core.Configuration.*" />

    <!-- 内部 DTO：必须跳过，JSON 序列化 -->
    <SkipType name="DataForgeStudio.Core.DTO.*" skipProperties="true" skipFields="true" />
  </Module>

  <!-- DataForgeStudio.Shared.dll -->
  <Module file="...">
    <!-- API DTO：必须跳过，JSON 序列化 -->
    <SkipType name="DataForgeStudio.Shared.DTO.*" skipProperties="true" skipFields="true" />

    <!-- 异常类：保留消息 -->
    <SkipType name="DataForgeStudio.Shared.Exceptions.*" skipProperties="true" skipFields="true" />

    <!-- 常量类：公共访问 -->
    <SkipType name="DataForgeStudio.Shared.Constants.*" skipProperties="true" skipFields="true" />
  </Module>
</Obfuscator>
```

---

## 不参与混淆的程序集

| 程序集 | 原因 |
|--------|------|
| `DataForgeStudio.Domain.dll` | EF Core 实体，混淆会导致映射失败 |
| `DataForgeStudio.Api.dll` | 入口程序集，需要保持公共 API |
| `DataForgeStudio.Data.dll` | 未包含在混淆配置中 |

---

## 混淆后仍然会被混淆的内容

以下内容会被混淆，但**不会**导致问题：

| 类别 | 说明 |
|------|------|
| 匿名类型 `<>f__AnonymousType*` | LINQ 内部使用，不涉及 JSON 序列化 |
| 验证器内部类 `PasswordValidationResult` | 只在代码中使用，不序列化 |
| 私有方法和字段 | 不影响公共 API |
| 字符串常量 | 被 `HideStrings` 隐藏 |

---

## Obfuscar 配置最佳实践

### SkipType 属性说明

| 属性 | 作用 | 适用场景 |
|------|------|----------|
| `skipMethods="true"` | 跳过方法混淆 | 反射调用的方法 |
| `skipProperties="true"` | 跳过属性混淆 | JSON 序列化类 |
| `skipFields="true"` | 跳过字段混淆 | 数据绑定类 |
| `skipEvents="true"` | 跳过事件混淆 | 事件订阅类 |
| `skipStringHiding="true"` | 跳过字符串隐藏 | 包含重要字符串常量 |

### 需要跳过混淆的类特征

1. **JSON 序列化类** - 属性名用于反序列化
2. **EF Core 实体** - 属性名用于数据库映射
3. **依赖注入服务** - 类型名用于服务注册
4. **反射访问类** - 类型/成员名通过字符串查找
5. **API 控制器** - 路由参数绑定

---

## 验证清单

每次修改混淆配置后，应验证：

- [ ] 登录功能正常
- [ ] 许可证验证正常
- [ ] 报表查询正常
- [ ] 数据源管理正常
- [ ] 用户管理正常
- [ ] 角色权限正常
- [ ] 日志查询正常
- [ ] 备份恢复正常

---

## 相关文件

- 混淆配置：`backend/obfuscar.xml`
- 混淆映射：`backend/Obfuscated/Mapping.txt`
- 构建脚本：`scripts/build-installer.ps1`
- 之前的修复：`docs/fix-reports/2026-02-26-obfuscation-json-serialization-fix.md`

---

## 版本信息

- **审查日期：** 2026-02-26
- **Obfuscar 版本：** 最新 NuGet 版本
- **影响版本：** DataForgeStudio V1.0.0
