# build-patch.ps1
# DataForgeStudio Patch Build Script
# 构建独立的补丁包（只包含变更的文件，不是完整安装包）

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,              # 补丁版本号，如 "1.0.1"

    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",

    [switch]$BackendOnly = $false,    # 只构建后端补丁
    [switch]$FrontendOnly = $false,   # 只构建前端补丁
    [switch]$SkipObfuscation = $false # 跳过混淆（调试用）
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$PatchDir = Join-Path $ProjectRoot "dist\patches"
$TempDir = Join-Path $ProjectRoot "build\patch-temp"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DataForgeStudio Patch Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host ""

# 验证版本号格式
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "ERROR: 版本号格式无效，应为 'x.x.x' 格式（如 1.0.1）" -ForegroundColor Red
    exit 1
}

function Ensure-Directory {
    param([string]$Path)
    if (Test-Path $Path) { Remove-Item $Path -Recurse -Force }
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

# 创建临时目录
Ensure-Directory $TempDir
$PatchContentDir = Join-Path $TempDir "patch-content"
Ensure-Directory $PatchContentDir

# 1. 构建后端补丁
if (-not $FrontendOnly) {
    Write-Host "[1/3] Building Backend Patch..." -ForegroundColor Yellow

    # 构建后端
    Push-Location $ProjectRoot
    try {
        dotnet publish backend/src/DataForgeStudio.Api/DataForgeStudio.Api.csproj `
            -c $Configuration -r $Runtime --self-contained true `
            -o "$TempDir/backend-publish" /p:PublishSingleFile=false
        if ($LASTEXITCODE -ne 0) { throw "Backend build failed" }

        # 混淆（如果需要）
        if (-not $SkipObfuscation) {
            Push-Location (Join-Path $ProjectRoot "backend")
            try {
                dotnet obfuscar.console obfuscar.xml
                if ($LASTEXITCODE -eq 0) {
                    $sharedObfuscated = Join-Path $ProjectRoot "backend\Obfuscated\DataForgeStudio.Shared.dll"
                    if (Test-Path $sharedObfuscated) {
                        Copy-Item $sharedObfuscated "$TempDir/backend-publish/DataForgeStudio.Shared.dll" -Force
                        Write-Host "      Shared.dll obfuscated" -ForegroundColor Green
                    }
                }
            }
            finally { Pop-Location }
        }

        # 复制后端文件到补丁目录
        $ServerPatchDir = Join-Path $PatchContentDir "Server"
        New-Item -ItemType Directory -Path $ServerPatchDir -Force | Out-Null

        # 只复制必要的 DLL 文件（不复制运行时文件）
        $DllFiles = @(
            "DataForgeStudio.Api.dll",
            "DataForgeStudio.Core.dll",
            "DataForgeStudio.Data.dll",
            "DataForgeStudio.Domain.dll",
            "DataForgeStudio.Shared.dll"
        )

        foreach ($dll in $DllFiles) {
            $source = Join-Path "$TempDir/backend-publish" $dll
            if (Test-Path $source) {
                Copy-Item $source $ServerPatchDir -Force
                Write-Host "      Copied: $dll" -ForegroundColor Green
            }
        }

        Write-Host "      Backend patch prepared" -ForegroundColor Green
    }
    finally { Pop-Location }
} else {
    Write-Host "[1/3] Skipping backend (FrontendOnly mode)" -ForegroundColor Gray
}

# 2. 构建前端补丁
if (-not $BackendOnly) {
    Write-Host "[2/3] Building Frontend Patch..." -ForegroundColor Yellow

    $FrontendDir = Join-Path $ProjectRoot "frontend"
    Push-Location $FrontendDir
    try {
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "Frontend build failed" }

        # 复制前端文件到补丁目录
        $WebSitePatchDir = Join-Path $PatchContentDir "WebSite"
        Copy-Item -Path "dist" -Destination $WebSitePatchDir -Recurse

        Write-Host "      Frontend patch prepared" -ForegroundColor Green
    }
    finally { Pop-Location }
} else {
    Write-Host "[2/3] Skipping frontend (BackendOnly mode)" -ForegroundColor Gray
}

# 3. 创建版本信息文件和打包
Write-Host "[3/3] Creating Patch Package..." -ForegroundColor Yellow

# 创建版本信息文件
$VersionInfo = @{
    version = $Version
    buildDate = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
    type = if ($BackendOnly) { "backend" } elseif ($FrontendOnly) { "frontend" } else { "full" }
}

$VersionInfo | ConvertTo-Json | Out-File (Join-Path $PatchContentDir "patch-info.json") -Encoding UTF8

# 创建补丁包
Ensure-Directory $PatchDir
$PatchFileName = "DataForgeStudio-Patch-$Version.zip"
$PatchFilePath = Join-Path $PatchDir $PatchFileName

# 使用 PowerShell 压缩
Compress-Archive -Path "$PatchContentDir\*" -DestinationPath $PatchFilePath -Force

# 清理临时目录
Remove-Item $TempDir -Recurse -Force

# 输出结果
$PatchSize = (Get-Item $PatchFilePath).Length / 1MB

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Patch Build Successful!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "Patch File: $PatchFilePath" -ForegroundColor White
Write-Host "Size: $([math]::Round($PatchSize, 2)) MB" -ForegroundColor White
Write-Host ""
Write-Host "部署说明:" -ForegroundColor Cyan
Write-Host "1. 解压补丁包到临时目录" -ForegroundColor White
Write-Host "2. 停止 DataForgeStudio 服务" -ForegroundColor White
Write-Host "3. 备份当前版本" -ForegroundColor White
Write-Host "4. 将 Server 文件夹复制到安装目录覆盖" -ForegroundColor White
Write-Host "5. 将 WebSite 文件夹复制到 WebServer/html 覆盖" -ForegroundColor White
Write-Host "6. 重启服务" -ForegroundColor White
