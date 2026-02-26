# 前端服务 Windows 服务化实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将前端 Nginx 服务包装为 Windows 服务 (DFWebService)，实现统一服务管理。

**Architecture:** 在现有 WebServiceManager 中增加 Windows 服务管理模式。Nginx 模式使用 ServiceController 管理 DFWebService；IIS 模式保持现有 IIS API 管理方式。

**Tech Stack:** .NET 8.0 WPF, System.ServiceProcess.ServiceController, NSSM

---

## Task 1: 添加 Web 服务名常量

**Files:**
- Modify: `backend/tools/DeployManager/Services/ConfigService.cs:32`
- Modify: `backend/tools/DeployManager/Services/IConfigService.cs:18`

**Step 1: 在 IConfigService 接口添加属性定义**

在 `IConfigService.cs` 第 18 行后添加：

```csharp
    /// <summary>
    /// 服务名称常量
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Web 服务名称常量
    /// </summary>
    string WebServiceName { get; }
```

**Step 2: 在 ConfigService 实现类添加常量和属性**

在 `ConfigService.cs` 第 32 行后添加常量：

```csharp
    /// <summary>
    /// 服务名称常量
    /// </summary>
    public const string ServiceName = "DFAppService";

    /// <summary>
    /// Web 服务名称常量
    /// </summary>
    public const string WebServiceName = "DFWebService";
```

在 `ConfigService.cs` 第 74 行后添加属性实现：

```csharp
    /// <summary>
    /// 服务名称（接口实现）
    /// </summary>
    string IConfigService.ServiceName => ServiceName;

    /// <summary>
    /// Web 服务名称（接口实现）
    /// </summary>
    string IConfigService.WebServiceName => WebServiceName;
```

**Step 3: 验证构建**

Run: `dotnet build backend/tools/DeployManager/DeployManager.csproj`

Expected: 构建成功，0 错误

**Step 4: 提交**

```bash
git add backend/tools/DeployManager/Services/ConfigService.cs
git add backend/tools/DeployManager/Services/IConfigService.cs
git commit -m "feat: add WebServiceName constant for Windows service"
```

---

## Task 2: 修改 WebServiceManager 支持 Windows 服务

**Files:**
- Modify: `backend/tools/DeployManager/Services/WebServiceManager.cs`

**Step 1: 添加 Windows 服务管理器字段**

在 `WebServiceManager.cs` 第 14-19 行后添加：

```csharp
    private readonly IConfigService _configService;
    private readonly string _mode;
    private readonly string _iisSiteName;
    private readonly string _nginxPath;
    private ServiceController? _webServiceController;  // 新增
    private bool _disposed = false;
```

**Step 2: 在构造函数中初始化服务控制器**

在构造函数（第 30-39 行）末尾添加：

```csharp
    public WebServiceManager(IConfigService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));

        _mode = _configService.GetFrontendMode();
        _iisSiteName = "DataForgeStudio";
        _nginxPath = _configService.GetNginxPath();

        // 初始化 Web 服务控制器
        _webServiceController = new ServiceController(_configService.WebServiceName);

        Debug.WriteLine($"[WebServiceManager] 初始化，模式: {_mode}, IIS站点: {_iisSiteName}, Nginx路径: {_nginxPath}");
    }
```

**Step 3: 修改 GetNginxStatus 方法使用服务状态**

将 `GetNginxStatus()` 方法（第 104-126 行）替换为：

```csharp
    /// <summary>
    /// 获取 Nginx Windows 服务状态
    /// </summary>
    private ServiceStatus GetNginxStatus()
    {
        try
        {
            if (_webServiceController == null)
            {
                return ServiceStatus.Unknown;
            }

            _webServiceController.Refresh();
            var status = _webServiceController.Status;

            Debug.WriteLine($"[WebServiceManager] Nginx 服务状态: {status}");
            return status switch
            {
                ServiceControllerStatus.Running => ServiceStatus.Running,
                ServiceControllerStatus.Stopped => ServiceStatus.Stopped,
                ServiceControllerStatus.StartPending => ServiceStatus.Running,
                ServiceControllerStatus.StopPending => ServiceStatus.Stopped,
                _ => ServiceStatus.Unknown
            };
        }
        catch (InvalidOperationException ex)
        {
            // 服务未安装
            Debug.WriteLine($"[WebServiceManager] Nginx 服务未安装: {ex.Message}");
            return ServiceStatus.Unknown;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 获取 Nginx 服务状态失败: {ex.Message}");
            return ServiceStatus.Unknown;
        }
    }
```

**Step 4: 修改 StartNginxAsync 方法使用服务启动**

将 `StartNginxAsync()` 方法（第 201-306 行）替换为：

```csharp
    /// <summary>
    /// 启动 Nginx Windows 服务
    /// </summary>
    private async Task StartNginxAsync()
    {
        try
        {
            if (_webServiceController == null)
            {
                throw new InvalidOperationException("Web 服务控制器未初始化");
            }

            _webServiceController.Refresh();

            if (_webServiceController.Status == ServiceControllerStatus.Running)
            {
                Debug.WriteLine($"[WebServiceManager] Nginx 服务已在运行中");
                return;
            }

            Debug.WriteLine($"[WebServiceManager] 正在启动 Nginx 服务...");
            _webServiceController.Start();

            // 等待服务启动完成
            await Task.Run(() => _webServiceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));

            Debug.WriteLine($"[WebServiceManager] Nginx 服务启动成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 启动 Nginx 服务失败: {ex.Message}");
            throw;
        }
    }
```

**Step 5: 修改 StopNginxAsync 方法使用服务停止**

将 `StopNginxAsync()` 方法（第 405-460 行）替换为：

```csharp
    /// <summary>
    /// 停止 Nginx Windows 服务
    /// </summary>
    private async Task StopNginxAsync()
    {
        try
        {
            if (_webServiceController == null)
            {
                throw new InvalidOperationException("Web 服务控制器未初始化");
            }

            _webServiceController.Refresh();

            if (_webServiceController.Status == ServiceControllerStatus.Stopped)
            {
                Debug.WriteLine($"[WebServiceManager] Nginx 服务已停止");
                return;
            }

            if (!_webServiceController.CanStop)
            {
                throw new InvalidOperationException("Nginx 服务无法停止");
            }

            Debug.WriteLine($"[WebServiceManager] 正在停止 Nginx 服务...");
            _webServiceController.Stop();

            // 等待服务停止完成
            await Task.Run(() => _webServiceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));

            Debug.WriteLine($"[WebServiceManager] Nginx 服务停止成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 停止 Nginx 服务失败: {ex.Message}");
            throw;
        }
    }
```

**Step 6: 修改 RestartNginxAsync 方法使用服务重启**

将 `RestartNginxAsync()` 方法（第 516-531 行）替换为：

```csharp
    /// <summary>
    /// 重启 Nginx Windows 服务
    /// </summary>
    private async Task RestartNginxAsync()
    {
        try
        {
            Debug.WriteLine($"[WebServiceManager] 正在重启 Nginx 服务...");
            await StopNginxAsync();
            await Task.Delay(1000);
            await StartNginxAsync();
            Debug.WriteLine($"[WebServiceManager] Nginx 服务重启成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 重启 Nginx 服务失败: {ex.Message}");
            throw;
        }
    }
```

**Step 7: 修改 IsServiceConfigured 方法**

将 `IsServiceConfigured()` 方法（第 536-557 行）中 Nginx 部分替换为：

```csharp
    /// <summary>
    /// 检查服务是否已配置
    /// </summary>
    public bool IsServiceConfigured()
    {
        try
        {
            if (_mode.Equals("nginx", StringComparison.OrdinalIgnoreCase))
            {
                // 检查 Windows 服务是否已安装
                var services = ServiceController.GetServices();
                return services.Any(s => s.ServiceName.Equals(_configService.WebServiceName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                using var serverManager = new ServerManager();
                var site = serverManager.Sites.FirstOrDefault(s =>
                    s.Name.Equals(_iisSiteName, StringComparison.OrdinalIgnoreCase));
                return site != null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebServiceManager] 检查服务配置失败: {ex.Message}");
            return false;
        }
    }
```

**Step 8: 修改 Dispose 方法释放服务控制器**

在 `Dispose(bool disposing)` 方法（第 572-582 行）中添加：

```csharp
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
                _webServiceController?.Dispose();
                _webServiceController = null;
            }
            _disposed = true;
        }
    }
```

**Step 9: 验证构建**

Run: `dotnet build backend/tools/DeployManager/DeployManager.csproj`

Expected: 构建成功，0 错误

**Step 10: 提交**

```bash
git add backend/tools/DeployManager/Services/WebServiceManager.cs
git commit -m "feat: WebServiceManager now uses Windows service for Nginx"
```

---

## Task 3: 修改 FrontendModeViewModel 切换后重启服务

**Files:**
- Modify: `backend/tools/DeployManager/ViewModels/FrontendModeViewModel.cs`

**Step 1: 添加服务管理器依赖**

在字段区域（第 13-16 行）添加：

```csharp
    private readonly IIisManager _iisManager;
    private readonly INginxManager _nginxManager;
    private readonly IConfigService _configService;
    private readonly IWindowsServiceManager _webServiceManager;  // 新增
```

**Step 2: 修改构造函数注入服务管理器**

修改构造函数（第 66-77 行）：

```csharp
    /// <summary>
    /// 初始化前端模式配置视图模型
    /// </summary>
    /// <param name="iisManager">IIS 管理器</param>
    /// <param name="nginxManager">Nginx 管理器</param>
    /// <param name="configService">配置服务</param>
    /// <param name="webServiceManager">Web 服务管理器</param>
    public FrontendModeViewModel(
        IIisManager iisManager,
        INginxManager nginxManager,
        IConfigService configService,
        IWindowsServiceManager webServiceManager)
    {
        _iisManager = iisManager;
        _nginxManager = nginxManager;
        _configService = configService;
        _webServiceManager = webServiceManager;

        // 检查安装状态
        CheckInstallationStatus();

        // 加载配置
        LoadConfig();
    }
```

**Step 3: 修改 SwitchModeAsync 方法，切换后重启 DFWebService**

在 `SwitchModeAsync` 方法（第 152-256 行）中，修改启动新服务的部分：

将第 217-237 行替换为：

```csharp
            // 启动新服务
            if (targetMode == "iis")
            {
                // IIS 模式：停止 DFWebService，启动 IIS 站点
                try
                {
                    await _webServiceManager.StopAsync();
                }
                catch
                {
                    // 忽略停止失败
                }

                const string siteName = "DataForgeStudio";
                var port = _configService.GetFrontendPort();
                var physicalPath = _configService.GetWebSitePath();

                _iisManager.ConfigureSite(siteName, port, physicalPath);
                _iisManager.StartSite(siteName);
            }
            else
            {
                // Nginx 模式：停止 IIS 站点，启动 DFWebService
                const string iisSiteName = "DataForgeStudio";
                if (_iisManager.IsSiteExists(iisSiteName))
                {
                    _iisManager.StopSite(iisSiteName);
                }

                var nginxConfigPath = System.IO.Path.Combine(_configService.InstallPath, "WebServer", "conf", "nginx.conf");
                var backendPort = _configService.GetBackendPort();
                var frontendPort = _configService.GetFrontendPort();
                var backendUrl = $"http://localhost:{backendPort}";

                _nginxManager.UpdateConfig(nginxConfigPath, frontendPort, backendUrl);

                // 启动 DFWebService
                await _webServiceManager.StartAsync();
            }
```

**Step 4: 验证构建**

Run: `dotnet build backend/tools/DeployManager/DeployManager.csproj`

Expected: 构建成功，0 错误

**Step 5: 提交**

```bash
git add backend/tools/DeployManager/ViewModels/FrontendModeViewModel.cs
git commit -m "feat: FrontendModeViewModel restarts DFWebService on mode switch"
```

---

## Task 4: 更新依赖注入配置

**Files:**
- Modify: `backend/tools/DeployManager/App.xaml.cs`

**Step 1: 查找服务注册位置**

找到 FrontendModeViewModel 的注册位置，添加 WebServiceManager 的注入。

**Step 2: 验证构建**

Run: `dotnet build backend/tools/DeployManager/DeployManager.csproj`

Expected: 构建成功，0 错误

**Step 3: 提交**

```bash
git add backend/tools/DeployManager/App.xaml.cs
git commit -m "feat: update DI for FrontendModeViewModel with WebServiceManager"
```

---

## Task 5: 创建 Nginx 服务安装脚本

**Files:**
- Create: `backend/tools/scripts/install-web-service.ps1`

**Step 1: 创建安装脚本**

```powershell
# install-web-service.ps1
# 安装 DFWebService (Nginx) Windows 服务

param(
    [string]$InstallPath = $PSScriptRoot,
    [string]$ServiceName = "DFWebService"
)

$ErrorActionPreference = "Stop"

# 检查管理员权限
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "请以管理员身份运行此脚本"
    exit 1
}

# 检查 NSSM
$nssmPath = Join-Path $InstallPath "tools\nssm.exe"
if (-not (Test-Path $nssmPath)) {
    Write-Error "未找到 NSSM: $nssmPath"
    exit 1
}

# 检查 Nginx
$nginxPath = Join-Path $InstallPath "WebServer"
$nginxExe = Join-Path $nginxPath "nginx.exe"
if (-not (Test-Path $nginxExe)) {
    Write-Error "未找到 Nginx: $nginxExe"
    exit 1
}

# 检查服务是否已存在
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "服务 $ServiceName 已存在，跳过安装"
    exit 0
}

# 使用 NSSM 安装服务
Write-Host "正在安装服务 $ServiceName ..."

& $nssmPath install $ServiceName $nginxExe
& $nssmPath set $ServiceName AppDirectory $nginxPath
& $nssmPath set $ServiceName DisplayName "DataForge Studio Web Service"
& $nssmPath set $ServiceName Description "DataForge Studio 前端服务 (Nginx)"
& $nssmPath set $ServiceName Start SERVICE_AUTO_START
& $nssmPath set $ServiceName AppStdout (Join-Path $nginxPath "logs\service_stdout.log")
& $nssmPath set $ServiceName AppStderr (Join-Path $nginxPath "logs\service_stderr.log")

Write-Host "服务 $ServiceName 安装成功"

# 启动服务
Start-Service -Name $ServiceName
Write-Host "服务 $ServiceName 已启动"
```

**Step 2: 创建卸载脚本**

创建文件 `backend/tools/scripts/uninstall-web-service.ps1`:

```powershell
# uninstall-web-service.ps1
# 卸载 DFWebService Windows 服务

param(
    [string]$ServiceName = "DFWebService"
)

$ErrorActionPreference = "Stop"

# 检查管理员权限
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "请以管理员身份运行此脚本"
    exit 1
}

# 检查服务是否存在
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $existingService) {
    Write-Host "服务 $ServiceName 不存在，跳过卸载"
    exit 0
}

# 停止服务
if ($existingService.Status -eq 'Running') {
    Write-Host "正在停止服务 $ServiceName ..."
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
}

# 使用 sc.exe 删除服务
Write-Host "正在卸载服务 $ServiceName ..."
& sc.exe delete $ServiceName

Write-Host "服务 $ServiceName 卸载成功"
```

**Step 3: 提交**

```bash
git add backend/tools/scripts/
git commit -m "feat: add web service install/uninstall scripts"
```

---

## Task 6: 测试验证

**Step 1: 构建整个解决方案**

Run: `dotnet build backend/DataForgeStudio.sln`

Expected: 构建成功，0 错误

**Step 2: 手动测试检查清单**

- [ ] DeployManager 启动无异常
- [ ] 服务管理页签显示两个服务状态
- [ ] 前端模式切换后服务正确重启
- [ ] IIS 模式下 DFWebService 停止
- [ ] Nginx 模式下 DFWebService 运行

**Step 3: 最终提交**

```bash
git add -A
git commit -m "feat: complete frontend Windows service implementation"
```

---

## Summary

| Task | Description | Files |
|------|-------------|-------|
| 1 | 添加 Web 服务名常量 | ConfigService.cs, IConfigService.cs |
| 2 | WebServiceManager 支持 Windows 服务 | WebServiceManager.cs |
| 3 | 模式切换后重启服务 | FrontendModeViewModel.cs |
| 4 | 更新依赖注入 | App.xaml.cs |
| 5 | 创建安装脚本 | scripts/*.ps1 |
| 6 | 测试验证 | - |
