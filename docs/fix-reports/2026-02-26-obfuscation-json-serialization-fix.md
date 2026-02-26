# 修复报告：代码混淆导致 502 错误（JSON 序列化失败）

## 问题描述

**错误信息：**
```
POST http://127.0.0.1:8089/api/auth/login 502 (Bad Gateway)
```

**问题特征：**
- 服务启动成功，但登录页面点击登录时报 502 错误
- **仅在使用代码混淆后出现**
- 未使用混淆时，登录功能正常

## 根本原因分析

### 问题根源：Obfuscar 的 SkipType 行为

**误区：** 认为 `<SkipType name="DataForgeStudio.Shared.DTO.*" />` 会跳过整个类型，包括其属性。

**实际行为：** `SkipType` **只跳过类型名称**，**不跳过类型的成员（属性、字段、方法）**！

### 证据

**修复前的 Mapping.txt：**
```
[DataForgeStudio.Shared]DataForgeStudio.Shared.DTO.LoginRequest skipped:  type rule in configuration
[DataForgeStudio.Shared]DataForgeStudio.Shared.DTO.LoginRequest::get_Password[0]( ) ->     ← 被混淆！
[DataForgeStudio.Shared]DataForgeStudio.Shared.DTO.LoginRequest::get_Token[0]( ) ->        ← 被混淆！
```

**结果：**
- 前端发送 `{"username": "admin", "password": "xxx"}`
- 后端 `LoginRequest.Password` 属性被混淆为 ` `（不可见字符）
- JSON 反序列化时 `password` 字段无法映射到 ` ` 属性
- `Password` 属性值为 null
- 认证逻辑失败，导致 502 错误

### 为什么是 502 而不是 400？

- 502 是 Nginx 返回的
- 后端服务可能因为反序列化问题抛出未处理异常
- 服务进程崩溃或无响应
- Nginx 作为代理无法连接后端，返回 502

## 修复方案

### 修改文件：`backend/obfuscar.xml`

**修复内容：** 为 `SkipType` 添加 `skipProperties="true"` 和 `skipFields="true"` 属性

**修复前：**
```xml
<!-- 跳过所有 DTO 类 (API 响应/请求模型) -->
<SkipType name="DataForgeStudio.Shared.DTO.*" />
```

**修复后：**
```xml
<!-- 跳过所有 DTO 类及其属性 (API 响应/请求模型，JSON 序列化需要属性名) -->
<!-- skipProperties="true" 是关键！否则属性名会被混淆导致序列化失败 -->
<SkipType name="DataForgeStudio.Shared.DTO.*" skipProperties="true" skipFields="true" />

<!-- 跳过异常类及其成员 (需要保留异常消息) -->
<SkipType name="DataForgeStudio.Shared.Exceptions.*" skipProperties="true" skipFields="true" />

<!-- 跳过常量类 (公共访问需要) -->
<SkipType name="DataForgeStudio.Shared.Constants.*" skipProperties="true" skipFields="true" />
```

### 验证结果

**修复后的 Mapping.txt：**
```
[DataForgeStudio.Shared]DataForgeStudio.Shared.DTO.LoginRequest skipped:  type rule in configuration
[DataForgeStudio.Shared]DataForgeStudio.Shared.DTO.LoginRequest::get_Password[0]( ) skipped:  skip by property   ← 保留原名称！
[DataForgeStudio.Shared]DataForgeStudio.Shared.DTO.LoginRequest::get_Token[0]( ) skipped:  skip by property      ← 保留原名称！
```

## Obfuscar SkipType 属性详解

| 属性 | 作用 | 示例 |
|------|------|------|
| `skipMethods="true"` | 跳过方法混淆 | 保留方法名 |
| `skipProperties="true"` | 跳过属性混淆 | 保留属性名（JSON 序列化需要） |
| `skipFields="true"` | 跳过字段混淆 | 保留字段名 |
| `skipEvents="true"` | 跳过事件混淆 | 保留事件名 |
| `skipStringHiding="true"` | 跳过字符串隐藏 | 保留字符串常量 |

### 最佳实践

对于需要 **JSON 序列化** 的类（DTO、请求/响应模型）：
```xml
<SkipType name="MyApp.DTO.*" skipProperties="true" skipFields="true" />
```

对于 **EF Core 实体**：
```xml
<SkipType name="MyApp.Entities.*" skipProperties="true" skipFields="true" />
```

对于 **需要反射访问** 的类：
```xml
<SkipType name="MyApp.Reflection.*" skipMethods="true" skipProperties="true" skipFields="true" />
```

## 修改文件清单

| 文件 | 修改类型 | 说明 |
|------|---------|------|
| `backend/obfuscar.xml` | 添加属性 | DTO/异常/常量类的 SkipType 添加 skipProperties 和 skipFields |

## 验证步骤

1. 重新构建安装程序：`./scripts/build-installer.ps1`
2. 安装新版本
3. 启动服务
4. 访问登录页面，输入用户名密码
5. 点击登录，验证是否成功

## 相关参考

- [Obfuscar SkipType 文档](https://github.com/obfuscar/obfuscar/wiki)
- 之前修复：`c1a0ef3` - 跳过服务类防止 TypeLoadException

## 版本信息

- **修复日期：** 2026-02-26
- **影响版本：** DataForgeStudio V1.0.0
- **修复构建：** DataForgeStudio-Setup.exe (121.28 MB)
