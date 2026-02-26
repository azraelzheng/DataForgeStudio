# DeployManager 实现计划

> **For Claude:** 使用 subagent-driven-development 或直接实现

**目标:** 开发 WPF 桌面管理工具，用于管理 DataForgeStudio 部署配置

**架构:** MVVM 模式，4个标签页（服务管理、数据库配置、端口配置、前端模式）

**技术栈:** WPF .NET 8.0, CommunityToolkit.Mvvm, Microsoft.Web.Administration, System.ServiceProcess

---

## Task 1: 创建项目结构

**操作:**
```bash
cd backend/tools
mkdir -p DeployManager/{Views,ViewModels,Services,Models,Utils}
```

**创建文件:** `backend/tools/DeployManager/DeployManager.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AssemblyName>DeployManager</AssemblyName>
    <RootNamespace>DeployManager</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.5" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Web.Administration" Version="11.1.0" />
    <PackageReference Include="System.ServiceProcess.ServiceController" Version="8.0.0" />
  </ItemGroup>
</Project>
```

**添加到解决方案:**
```bash
dotnet sln add tools/DeployManager/DeployManager.csproj
```

---

## Task 2: Models - 配置模型

**文件:** `Models/DeployConfig.cs`

**关键类:**
- `DeployConfig` - 根配置（version, installPath, backend, frontend, database）
- `BackendConfig` - 端口、服务名
- `FrontendConfig` - 端口、模式(iis/nginx)、IIS站点名
- `DatabaseConfig` - 复用 Installer 的实现（服务器、端口、数据库名、用户名、密码）
- `ServiceStatus` - 枚举（Running, Stopped, Unknown）

---

## Task 3: Services - 接口定义

**文件:** `Services/IWindowsServiceManager.cs`
```csharp
public interface IWindowsServiceManager
{
    ServiceStatus GetStatus();
    Task StartAsync();
    Task StopAsync();
    Task RestartAsync();
    bool IsServiceInstalled();
}
```

**文件:** `Services/IIisManager.cs`
```csharp
public interface IIisManager
{
    bool IsIisInstalled();
    bool IsSiteExists(string siteName);
    void ConfigureSite(string siteName, int port, string physicalPath);
    void StartSite(string siteName);
    void StopSite(string siteName);
}
```

**文件:** `Services/INginxManager.cs`
```csharp
public interface INginxManager
{
    bool IsNginxInstalled();
    Task StartAsync(string configPath);
    Task StopAsync();
    void UpdateConfig(string configPath, int port, string backendUrl);
}
```

**文件:** `Services/IConfigService.cs`
```csharp
public interface IConfigService
{
    DeployConfig Load();
    void Save(DeployConfig config);
    string ConfigPath { get; }
}
```

**文件:** `Services/IDatabaseConnectionService.cs`
```csharp
public interface IDatabaseConnectionService
{
    Task<(bool Success, string Message)> TestConnectionAsync(DatabaseConfig config);
}
```

---

## Task 4: Services - 实现

**文件:** `Services/WindowsServiceManager.cs`
- 使用 `ServiceController` 管理服务

**文件:** `Services/IisManager.cs`
- 使用 `Microsoft.Web.Administration.ServerManager`

**文件:** `Services/NginxManager.cs`
- 使用 `Process` 启动/停止 nginx

**文件:** `Services/ConfigService.cs`
- JSON 序列化/反序列化

**文件:** `Services/DatabaseConnectionService.cs`
- 复用 Installer 的 `DatabaseTestService` 逻辑

---

## Task 5: ViewModels - 主框架

**文件:** `ViewModels/MainViewModel.cs`
- 继承 `ObservableObject`
- 属性: `CurrentView`, `StatusColor`, `StatusText`, `PortText`, `ModeText`
- 命令: 切换标签页
- 依赖注入各个子 ViewModel

**文件:** `ViewModels/ServiceControlViewModel.cs`
- 属性: `IsRunning`, `StartTime`, `MemoryUsage`, `AutoStart`
- 命令: `StartCommand`, `StopCommand`, `RestartCommand`
- 定时刷新状态

**文件:** `ViewModels/DatabaseConfigViewModel.cs`
- 属性: `Server`, `Port`, `Database`, `Username`, `Password`, `UseWindowsAuth`
- 命令: `TestConnectionCommand`, `SaveCommand`

**文件:** `ViewModels/PortConfigViewModel.cs`
- 属性: `BackendPort`, `FrontendPort`
- 命令: `SaveCommand`

**文件:** `ViewModels/FrontendModeViewModel.cs`
- 属性: `IsIisMode`, `IsNginxMode`, `IisInstalled`, `NginxInstalled`
- 命令: `SwitchModeCommand`

---

## Task 6: Views - XAML 界面

**文件:** `App.xaml` / `App.xaml.cs`
- 配置依赖注入容器
- 设置 MainWindow

**文件:** `MainWindow.xaml`
- TabControl 布局
- 底部状态栏
- 样式和主题

**文件:** `Views/ServiceControlView.xaml`
- 状态显示（圆点+文字）
- 三个操作按钮
- 运行信息（启动时间、内存）

**文件:** `Views/DatabaseConfigView.xaml`
- 表单布局
- 测试连接按钮
- 保存按钮

**文件:** `Views/PortConfigView.xaml`
- 两个端口输入框
- 保存按钮
- 重启提醒

**文件:** `Views/FrontendModeView.xaml`
- RadioButton 选择 IIS/Nginx
- 切换按钮
- 安装状态显示

---

## Task 7: Utils - 工具类

**文件:** `Utils/PasswordHelper.cs`
- 使用 `EncryptionHelper.AesEncrypt/Decrypt` 加密数据库密码

---

## Task 8: 管理员权限清单

**文件:** `app.manifest`
```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

---

## 执行顺序

1. Task 1 → 2 → 3 (项目结构、模型、接口)
2. Task 4 (服务实现)
3. Task 5 (ViewModels)
4. Task 6 (Views)
5. Task 7 → 8 (工具类、权限)

---

**参考:** 详细设计见 `docs/plans/2026-02-18-deploy-manager-design.md`
