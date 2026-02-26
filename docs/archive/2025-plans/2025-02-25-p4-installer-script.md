# P4: 安装脚本优化实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 优化安装脚本，包括安装后自动启动服务、移除冗余目录、优化 nssm 存放位置

**Architecture:** 修改构建脚本和 Inno Setup 配置文件

**Tech Stack:** Inno Setup 6, PowerShell, Batch

**Related Tasks:** fix2.md 任务 2, 3, 4

---

## Task 1: 安装完成后自动启动服务 (fix2.md #2)

**问题描述:** 安装完成后需要手动启动服务，体验不佳

**预期行为:** 安装程序在安装流程结束后自动启动 DataForgeStudio 服务

**Files:**
- Modify: `installer/setup.iss`

**Step 1: 检查当前 setup.iss 配置**

首先读取 `installer/setup.iss` 文件了解当前配置。

**Step 2: 添加安装完成后执行命令**

在 `[Run]` 段落添加服务启动命令：

```iss
[Run]
; 安装完成后启动服务
Filename: "{app}\tools\nssm\nssm.exe"; Parameters: "start DataForgeStudio"; Flags: runhidden waituntilterminated; StatusMsg: "正在启动 DataForgeStudio 服务..."

; 可选：打开管理界面
Filename: "http://localhost:5000"; Flags: shellexec postinstall skipifsilent; Description: "打开 DataForgeStudio 管理界面"
```

**Step 3: 添加 [Registry] 段落（如果需要）**

如果需要在安装时配置服务自动启动：

```iss
[Registry]
; 确保服务自动启动
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\DataForgeStudio"; ValueType: dword; ValueName: "Start"; ValueData: "2"; Flags: uninsdeletekey
```

**Step 4: 添加安装后提示**

在 `[Code]` 段落添加完成提示：

```pascal
[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 安装完成后的操作
    MsgBox('安装完成！' + #13#10 + #13#10 +
           'DataForgeStudio 服务已启动。' + #13#10 +
           '请访问 http://localhost:5000 使用系统。' + #13#10 + #13#10 +
           '默认管理员账户：' + #13#10 +
           '  用户名: admin' + #13#10 +
           '  密码: Admin@123',
           mbInformation, MB_OK);
  end;
end;
```

**Step 5: 验证修改**

1. 运行 `scripts/build-installer.bat` 构建安装包
2. 安装并确认服务自动启动
3. 确认浏览器自动打开管理界面

---

## Task 2: 移除不再需要的 tools\scripts 目录 (fix2.md #3)

**问题描述:** `tools\scripts` 目录可能为空或冗余

**预期行为:** 如果目录无实际用途，从安装包中移除

**Files:**
- Modify: `scripts/build-installer.ps1:127-134`
- Modify: `installer/setup.iss`

**Step 1: 检查 scripts 目录内容**

```powershell
# 检查 backend/tools/scripts 目录
ls backend/tools/scripts
```

**Step 2: 分析是否需要保留**

当前 `build-installer.ps1` 中的代码：

```powershell
$ScriptsDir = Join-Path $BuildDir "tools\scripts"
Ensure-Directory $ScriptsDir
$SourceScriptsDir = Join-Path $ProjectRoot "backend\tools\scripts"
if (Test-Path $SourceScriptsDir) {
    Copy-Item "$SourceScriptsDir\*.ps1" $ScriptsDir -Force
    Write-Host "      Service scripts copied" -ForegroundColor Green
}
```

**Step 3: 确认服务脚本是否已迁移到其他位置**

检查 DeployManager 或其他工具是否已经包含了服务管理功能。

**Step 4: 如果不再需要，移除相关代码**

修改 `build-installer.ps1`：

```powershell
# Step 5: Ensure tools directory exists
Write-Host "[5/5] Checking tools directory..." -ForegroundColor Yellow

$NssmDir = Join-Path $BuildDir "tools\nssm"
$NssmExe = Join-Path $NssmDir "nssm.exe"
if (-not (Test-Path $NssmExe)) {
    Write-Host "      WARNING: NSSM not found, please download to $NssmDir" -ForegroundColor Yellow
    Write-Host "      Download: https://nssm.cc/download" -ForegroundColor Yellow
}

# 移除 scripts 目录创建（如果不再需要）
# $ScriptsDir = Join-Path $BuildDir "tools\scripts"
# Ensure-Directory $ScriptsDir
```

**Step 5: 更新 setup.iss**

如果 setup.iss 中有引用 `tools\scripts`，需要移除：

```iss
; 移除以下类似的行（如果存在）
; Source: "{#BuildDir}\tools\scripts\*"; DestDir: "{app}\tools\scripts"; Flags: ignoreversion recursesubdirs
```

**Step 6: 验证修改**

1. 运行构建脚本
2. 检查 `build\installer` 目录，确认 `tools\scripts` 不存在
3. 安装并确认功能正常

---

## Task 3: 优化 nssm 文件存放位置 (fix2.md #4)

**问题描述:** nssm 文件位于 `tools\nssm`，建议移至 `webserver` 目录或更合适的位置

**预期行为:** nssm 与服务管理工具放在一起，便于维护

**Files:**
- Modify: `scripts/build-installer.ps1`
- Modify: `installer/setup.iss`

**Step 1: 分析最佳存放位置**

可选方案：
1. **方案 A**: 移动到 `Server` 目录（与 API 服务放在一起）
2. **方案 B**: 移动到 `manager` 目录（与系统管理工具放在一起）
3. **方案 C**: 保持 `tools\nssm` 但在 setup.iss 中正确引用

**建议采用方案 B**: 将 nssm 移动到 `manager` 目录，因为：
- manager 是系统管理工具，负责服务管理
- 减少安装目录的层级复杂度

**Step 2: 修改 build-installer.ps1**

```powershell
# Step 3: Build DeployManager (self-contained)
Write-Host "[3/5] Building DeployManager..." -ForegroundColor Yellow
Push-Location $ProjectRoot
try {
    $ManagerDir = Join-Path $BuildDir "manager"
    if (Test-Path $ManagerDir) { Remove-Item $ManagerDir -Recurse -Force }
    Ensure-Directory $ManagerDir

    dotnet publish backend/tools/DeployManager/DeployManager.csproj `
        -c $Configuration -r $Runtime --self-contained true `
        -o $ManagerDir /p:PublishSingleFile=false
    if ($LASTEXITCODE -ne 0) { throw "DeployManager build failed" }

    # 复制 nssm 到 manager 目录
    $NssmSourceDir = Join-Path $ProjectRoot "resources\nssm"
    $NssmSourceExe = Join-Path $NssmSourceDir "nssm.exe"
    if (Test-Path $NssmSourceExe) {
        Copy-Item $NssmSourceExe $ManagerDir -Force
        Write-Host "      NSSM copied to manager directory" -ForegroundColor Green
    } else {
        Write-Host "      WARNING: NSSM not found in resources\nssm" -ForegroundColor Yellow
    }

    Write-Host "      DeployManager built" -ForegroundColor Green
}
finally { Pop-Location }
```

**Step 3: 移除旧的 tools\nssm 目录创建代码**

```powershell
# 移除以下代码（已合并到 Step 3）
# Step 5: Ensure tools directory exists
# $NssmDir = Join-Path $BuildDir "tools\nssm"
```

**Step 4: 更新 setup.iss 中的路径**

```iss
[Files]
; ... 其他文件 ...

; 将 nssm 从 tools\nssm 改为 manager
Source: "{#BuildDir}\manager\nssm.exe"; DestDir: "{app}\manager"; Flags: ignoreversion

; 移除以下行（如果存在）
; Source: "{#BuildDir}\tools\nssm\*"; DestDir: "{app}\tools\nssm"; Flags: ignoreversion recursesubdirs
```

**Step 5: 更新服务管理相关代码**

如果 DeployManager 或其他脚本中引用了 nssm 路径，需要更新：

```csharp
// 在 DeployManager 中更新 nssm 路径
string nssmPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nssm.exe");
```

**Step 6: 验证修改**

1. 运行 `scripts/build-installer.bat` 构建安装包
2. 检查 `build\installer\manager` 目录，确认 nssm.exe 存在
3. 安装并确认服务注册/启动功能正常

---

## 完整的目录结构（修改后）

```
C:\Program Files\DataForgeStudio\
├── Server\              # API 服务
│   ├── DataForgeStudio.Api.exe
│   ├── appsettings.json
│   └── ...
├── WebSite\             # 前端静态文件
│   ├── index.html
│   └── ...
├── WebServer\           # Nginx（如果使用）
│   └── ...
├── manager\             # 系统管理工具 + nssm
│   ├── DeployManager.exe
│   ├── nssm.exe         # 移动到这里
│   └── ...
├── configurator\        # 配置工具
│   └── ...
└── keys\                # 许可证密钥
    └── public_key.pem
```

---

## 执行顺序

1. Task 2 (移除 scripts) → Task 3 (移动 nssm) → Task 1 (自动启动)

Task 2 和 Task 3 应该先执行，因为会影响目录结构，然后再配置 Task 1 的自动启动。

---

## 注意事项

1. **备份**: 修改前先备份现有的 `setup.iss` 和 `build-installer.ps1`
2. **测试**: 每次修改后都应运行完整的构建和安装测试
3. **版本控制**: 确保修改被提交到版本控制系统
4. **文档更新**: 如果目录结构变化，需要更新相关文档
