# 修复报告：Windows 服务启动错误 1053

## 问题描述

**错误信息：**
```
Windows 无法启动 DataForgeStudio API 服务(位于 本地计算机 上)。
错误 1053: 服务没有及时响应启动或控制请求。
```

**问题类型：** Windows 服务启动超时

**影响范围：** 安装完成后启动 DFAppService 时失败

## 根本原因分析

### 原因 1：使用 sc.exe 而非 nssm.exe 创建服务

**问题描述：**
- `DFWebService`（Nginx）使用 `nssm.exe` 创建服务
- `DFAppService`（.NET API）使用 `sc.exe` 创建服务

**为什么这是问题：**
- `sc.exe` 是 Windows 原生服务管理工具，对 .NET Core 应用支持较差
- `sc.exe` 创建的服务使用固定的 30 秒启动超时，无法配置
- .NET Core 应用需要时间初始化依赖注入容器、连接数据库等
- `nssm.exe` 提供了更好的服务管理和超时控制

### 原因 2：启动时同步数据库连接测试

**问题描述：**
- `Program.cs` 在 `app.Run()` 之前同步测试数据库连接
- 如果数据库响应慢或不可用，会阻塞服务启动

**代码位置：** `backend/src/DataForgeStudio.Api/Program.cs` 第 111-128 行

**原始代码：**
```csharp
try
{
    using var testConnection = new SqlConnection(masterConnectionString);
    testConnection.Open();  // 同步阻塞！可能耗时 30+ 秒
    Console.WriteLine("✅ 数据库连接测试成功");
}
```

## 修复方案

### 修复 1：将 DFAppService 从 sc.exe 改为 nssm.exe

**修改文件：** `backend/tools/Configurator/Program.cs`

**修改内容：**
1. 重写 `RegisterWindowsService` 函数，使用 nssm.exe 创建服务
2. 添加 `RegisterWindowsServiceWithSc` 作为后备方案
3. 配置 nssm 参数：
   - 设置工作目录
   - 配置日志输出和轮转
   - 调整服务停止超时参数

**新代码核心：**
```csharp
// 使用 NSSM 创建服务
RunCommand(nssmPath, $"install \"{serviceName}\" \"{serverExePath}\"");

// 设置服务显示名称和描述
RunCommand(nssmPath, $"set \"{serviceName}\" DisplayName \"DataForgeStudio API\"");
RunCommand(nssmPath, $"set \"{serviceName}\" Description \"DataForgeStudio 报表管理系统 API 服务\"");

// 设置启动类型为自动
RunCommand(nssmPath, $"set \"{serviceName}\" Start SERVICE_AUTO_START");

// 设置工作目录
var serverDir = Path.Combine(config.InstallPath, "Server");
RunCommand(nssmPath, $"set \"{serviceName}\" AppDirectory \"{serverDir}\"");

// 配置日志输出
RunCommand(nssmPath, $"set \"{serviceName}\" AppStdout \"{Path.Combine(logPath, "api-service-out.log")}\"");
RunCommand(nssmPath, $"set \"{serviceName}\" AppStderr \"{Path.Combine(logPath, "api-service-err.log")}\"");

// 配置日志轮转
RunCommand(nssmPath, $"set \"{serviceName}\" AppRotateFiles 1");
RunCommand(nssmPath, $"set \"{serviceName}\" AppRotateBytes 1048576");

// 设置服务超时参数
RunCommand(nssmPath, $"set \"{serviceName}\" AppStopMethodConsole 1500");
RunCommand(nssmPath, $"set \"{serviceName}\" AppStopMethodWindow 1500");
RunCommand(nssmPath, $"set \"{serviceName}\" AppStopMethodThreads 1500");
RunCommand(nssmPath, $"set \"{serviceName}\" AppThrottle 1500");
```

### 修复 2：将数据库连接测试改为异步超时模式

**修改文件：** `backend/src/DataForgeStudio.Api/Program.cs`

**修改内容：**
1. 使用 `OpenAsync` 替代 `Open`
2. 添加 10 秒超时控制
3. 超时时跳过测试，不阻止启动

**新代码：**
```csharp
// 使用带超时的异步连接测试，避免阻塞服务启动
var dbTestCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    using var testConnection = new SqlConnection(masterConnectionString);
    await testConnection.OpenAsync(dbTestCts.Token);
    Console.WriteLine("✅ 数据库连接测试成功");
}
catch (OperationCanceledException)
{
    Console.WriteLine($"⚠️ 数据库连接测试超时（10秒），跳过测试");
    Console.WriteLine($"   服务将继续启动，但可能需要在运行时验证数据库连接");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 数据库连接测试失败: {ex.Message}");
    // 不阻止启动，让应用继续运行以便诊断
}
```

## 修改文件清单

| 文件 | 修改类型 | 说明 |
|------|---------|------|
| `backend/tools/Configurator/Program.cs` | 重写函数 | `RegisterWindowsService` 改用 nssm.exe |
| `backend/src/DataForgeStudio.Api/Program.cs` | 优化 | 数据库连接测试添加超时控制 |

## 技术要点

### nssm.exe vs sc.exe 对比

| 特性 | sc.exe | nssm.exe |
|------|--------|----------|
| 服务创建 | 原生支持 | 需要额外工具 |
| 超时配置 | 固定 30 秒 | 可自定义 |
| 日志管理 | 需手动配置 | 内置支持 |
| 工作目录 | 需额外配置 | 直接支持 |
| .NET Core 支持 | 一般 | 优秀 |
| 错误处理 | 基础 | 完善 |

### Windows 服务启动超时说明

- **默认超时：** 30 秒
- **超时后果：** 错误 1053，服务启动失败
- **解决方案：**
  1. 使用 nssm.exe 获得更好的超时控制
  2. 将耗时操作移到应用启动后异步执行
  3. 为必要操作添加超时限制

## 验证步骤

1. 卸载旧版本（如果已安装）
2. 运行新安装程序 `H:\DataForge\dist\DataForgeStudio-Setup.exe`
3. 完成安装向导
4. 验证服务自动启动成功
5. 检查 `C:\Program Files\DataForgeStudio\logs\` 目录下的日志文件

## 日志位置

修复后，API 服务的日志将输出到：
- 标准输出：`{InstallPath}\logs\api-service-out.log`
- 错误输出：`{InstallPath}\logs\api-service-err.log`
- 应用日志：`{InstallPath}\logs\api-{date}.log`

## 后续建议

1. **监控启动时间：** 如果启动仍然较慢，可以考虑进一步优化初始化逻辑
2. **健康检查：** 访问 `http://localhost:5000/health` 验证服务是否正常运行
3. **日志分析：** 如果问题仍然存在，查看日志文件获取详细错误信息

## 版本信息

- **修复日期：** 2026-02-26
- **影响版本：** DataForgeStudio V1.0.0
- **修复构建：** DataForgeStudio-Setup.exe (121.29 MB)
