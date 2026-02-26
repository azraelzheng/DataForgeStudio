# 代码混淆实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为 DataForgeStudio.Core.dll 和 DataForgeStudio.Shared.dll 添加 Obfuscar 代码混淆

**Architecture:** 在 build-installer.ps1 中集成混淆步骤，使用 Obfuscar 工具对关键程序集进行符号重命名、控制流混淆和字符串加密

**Tech Stack:** .NET 8.0, Obfuscar 2.2.x, PowerShell

---

## Task 1: 添加 Obfuscar 包引用

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj`
- Modify: `backend/src/DataForgeStudio.Shared/DataForgeStudio.Shared.csproj`

**Step 1: 读取现有的 csproj 文件**

Run: Read both csproj files to understand current structure

**Step 2: 添加 Obfuscar 包引用到 Core 项目**

在 `<ItemGroup>` 中添加：

```xml
<PackageReference Include="Obfuscar" Version="2.2.38" PrivateAssets="all" />
```

**Step 3: 添加 Obfuscar 包引用到 Shared 项目**

同上，添加相同的包引用

**Step 4: 还原包**

Run: `cd H:/DataForge/backend && dotnet restore`

**Step 5: 提交**

```bash
git add backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj backend/src/DataForgeStudio.Shared/DataForgeStudio.Shared.csproj
git commit -m "feat: add Obfuscar package reference to Core and Shared projects"
```

---

## Task 2: 创建 Obfuscar 配置文件

**Files:**
- Create: `backend/obfuscar.xml`

**Step 1: 创建配置文件**

```xml
<?xml version='1.0'?>
<Obfuscator>
  <!-- 基本设置 -->
  <Var name="InPath" value="src/DataForgeStudio.Core/bin/Release/net8.0" />
  <Var name="OutPath" value="src/DataForgeStudio.Core/bin/Release/net8.0/Obfuscated" />
  <Var name="KeepPublicApi" value="false" />
  <Var name="HidePrivateApi" value="true" />
  <Var name="RenameProperties" value="true" />
  <Var name="RenameEvents" value="true" />
  <Var name="RenameFields" value="true" />
  <Var name="UseUnicodeNames" value="true" />
  <Var name="HideStrings" value="true" />

  <!-- Core 程序集 -->
  <Module file="$(InPath)/DataForgeStudio.Core.dll">
    <!-- 保留公共接口 -->
    <SkipType name="DataForgeStudio.Core.Interfaces.*" />

    <!-- 保留依赖注入扩展方法 -->
    <SkipMethod type="DataForgeStudio.Core.Extensions.*" name="*" />

    <!-- 保留异常类 -->
    <SkipType name="DataForgeStudio.Core.Exceptions.*" />
  </Module>

  <!-- Shared 程序集 -->
  <Module file="$(InPath)/../DataForgeStudio.Shared/DataForgeStudio.Shared.dll">
    <!-- 保留 DTO 类 -->
    <SkipType name="DataForgeStudio.Shared.DTO.*" />

    <!-- 保留异常类 -->
    <SkipType name="DataForgeStudio.Shared.Exceptions.*" />

    <!-- 保留常量类的公共 API（但混淆内部值） -->
    <SkipType name="DataForgeStudio.Shared.Constants.ProductionKeys" />
  </Module>
</Obfuscator>
```

**Step 2: 提交**

```bash
git add backend/obfuscar.xml
git commit -m "feat: add Obfuscar configuration file"
```

---

## Task 3: 集成混淆到构建脚本

**Files:**
- Modify: `scripts/build-installer.ps1`

**Step 1: 读取现有构建脚本**

找到后端构建步骤的位置（约第 32-45 行）

**Step 2: 在后端构建后添加混淆步骤**

在后端构建成功后（`Write-Host "      Backend API built"` 之后）添加：

```powershell
# Step 1.5: Obfuscate critical assemblies
Write-Host "[1.5/5] Obfuscating critical assemblies..." -ForegroundColor Yellow
Push-Location (Join-Path $ProjectRoot "backend")
try {
    # 检查 Obfuscar 是否安装
    $obfuscarPath = Join-Path $ProjectRoot "backend\src\DataForgeStudio.Core\bin\$Configuration\net8.0\obfuscar\Obfuscar.Console.exe"
    if (-not (Test-Path $obfuscarPath)) {
        # 使用 dotnet tool 运行
        dotnet tool restore
        dotnet obfuscar.console obfuscar.xml
    } else {
        & $obfuscarPath obfuscar.xml
    }

    if ($LASTEXITCODE -ne 0) { throw "Obfuscation failed" }

    # 复制混淆后的程序集到输出目录
    $coreObfuscated = Join-Path $ProjectRoot "backend\src\DataForgeStudio.Core\bin\$Configuration\net8.0\Obfuscated\DataForgeStudio.Core.dll"
    $sharedObfuscated = Join-Path $ProjectRoot "backend\src\DataForgeStudio.Shared\bin\$Configuration\net8.0\Obfuscated\DataForgeStudio.Shared.dll"

    Copy-Item $coreObfuscated "$BuildDir/Server/DataForgeStudio.Core.dll" -Force
    Copy-Item $sharedObfuscated "$BuildDir/Server/DataForgeStudio.Shared.dll" -Force

    Write-Host "      Assemblies obfuscated" -ForegroundColor Green
}
finally { Pop-Location }
```

**Step 3: 提交**

```bash
git add scripts/build-installer.ps1
git commit -m "feat: integrate Obfuscar into build script"
```

---

## Task 4: 配置 dotnet tool manifest

**Files:**
- Create: `backend/.dotnet-tools.json`

**Step 1: 创建 dotnet tool manifest**

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "obfuscar.globaltool": {
      "version": "2.2.38",
      "commands": [
        "obfuscar.console"
      ]
    }
  }
}
```

**Step 2: 提交**

```bash
git add backend/.dotnet-tools.json
git commit -m "feat: add dotnet tool manifest for Obfuscar"
```

---

## Task 5: 测试混淆效果

**Step 1: 运行完整构建**

```bash
cd H:/DataForge/scripts
./build-installer.ps1
```

**Step 2: 验证混淆**

使用 ILSpy 或 dnSpy 打开混淆后的 DLL：
- `build/installer/Server/DataForgeStudio.Core.dll`
- `build/installer/Server/DataForgeStudio.Shared.dll`

检查：
- LicenseService 类名是否被重命名
- ProductionKeys 字段名是否被重命名
- 字符串是否被加密

**Step 3: 功能测试**

运行安装包，验证：
1. 许可证激活功能正常
2. 许可证验证功能正常
3. 登录功能正常

**Step 4: 最终提交**

```bash
git add -A
git commit -m "feat: complete code obfuscation integration"
git push origin master
```

---

## 文件变更摘要

| 文件 | 操作 | 任务 |
|------|------|------|
| `backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj` | 修改 | Task 1 |
| `backend/src/DataForgeStudio.Shared/DataForgeStudio.Shared.csproj` | 修改 | Task 1 |
| `backend/obfuscar.xml` | 新增 | Task 2 |
| `scripts/build-installer.ps1` | 修改 | Task 3 |
| `backend/.dotnet-tools.json` | 新增 | Task 4 |

## 注意事项

- 混淆只在运行 `build-installer.ps1` 时执行
- 开发时 `dotnet run` 不会触发混淆
- 如果出现运行时错误，检查 obfuscar.xml 中的 SkipType 配置
