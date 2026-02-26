# build-installer.ps1
# DataForgeStudio Installer Build Script

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipBackend = $false,
    [switch]$SkipFrontend = $false
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$BuildDir = Join-Path $ProjectRoot "build\installer"
$DistDir = Join-Path $ProjectRoot "dist"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DataForgeStudio Installer Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host "Project Root: $ProjectRoot"
Write-Host ""

function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

# Step 1: Build Backend API
if (-not $SkipBackend) {
    Write-Host "[1/8] Building Backend API..." -ForegroundColor Yellow
    Push-Location $ProjectRoot
    try {
        dotnet publish backend/src/DataForgeStudio.Api/DataForgeStudio.Api.csproj `
            -c $Configuration -r $Runtime --self-contained true `
            -o "$BuildDir/Server" /p:PublishSingleFile=false
        if ($LASTEXITCODE -ne 0) { throw "Backend API build failed" }
        Write-Host "      Backend API built" -ForegroundColor Green
    }
    finally { Pop-Location }
} else {
    Write-Host "[1/8] Skipping backend build" -ForegroundColor Gray
}

# Step 2: Obfuscate critical assemblies
if (-not $SkipBackend) {
    Write-Host "[2/8] Obfuscating critical assemblies..." -ForegroundColor Yellow
    Push-Location (Join-Path $ProjectRoot "backend")
    try {
        dotnet obfuscar.console obfuscar.xml
        if ($LASTEXITCODE -ne 0) { throw "Obfuscation failed" }

        # Copy obfuscated Shared.dll to Server directory
        # Note: Core.dll is NOT obfuscated to preserve EF Core expression trees
        # Obfuscar outputs to backend/Obfuscated/ by default
        $sharedObfuscated = Join-Path $ProjectRoot "backend\Obfuscated\DataForgeStudio.Shared.dll"

        if (Test-Path $sharedObfuscated) {
            Copy-Item $sharedObfuscated "$BuildDir/Server/DataForgeStudio.Shared.dll" -Force
            Write-Host "      DataForgeStudio.Shared.dll obfuscated" -ForegroundColor Green
        } else {
            throw "Obfuscated Shared.dll not found: $sharedObfuscated"
        }

        Write-Host "      Assemblies obfuscated successfully" -ForegroundColor Green
    }
    finally { Pop-Location }
} else {
    Write-Host "[2/8] Skipping obfuscation (backend skipped)" -ForegroundColor Gray
}

# Step 3: Build Frontend
if (-not $SkipFrontend) {
    Write-Host "[3/8] Building Frontend..." -ForegroundColor Yellow
    $FrontendDir = Join-Path $ProjectRoot "frontend"
    Push-Location $FrontendDir
    try {
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "Frontend build failed" }
        $WebSiteDir = Join-Path $BuildDir "WebSite"
        if (Test-Path $WebSiteDir) { Remove-Item $WebSiteDir -Recurse -Force }
        Copy-Item -Path "dist" -Destination $WebSiteDir -Recurse
        Write-Host "      Frontend built" -ForegroundColor Green
    }
    finally { Pop-Location }
} else {
    Write-Host "[3/8] Skipping frontend build" -ForegroundColor Gray
}

# Step 4: Build DeployManager (self-contained)
Write-Host "[4/8] Building DeployManager..." -ForegroundColor Yellow
Push-Location $ProjectRoot
try {
    $ManagerDir = Join-Path $BuildDir "manager"
    if (Test-Path $ManagerDir) { Remove-Item $ManagerDir -Recurse -Force }
    Ensure-Directory $ManagerDir

    dotnet publish backend/tools/DeployManager/DeployManager.csproj `
        -c $Configuration -r $Runtime --self-contained true `
        -o $ManagerDir /p:PublishSingleFile=false
    if ($LASTEXITCODE -ne 0) { throw "DeployManager build failed" }

    # Copy native SNI.dll to runtimes directory (fixes Microsoft.Data.SqlClient platform compatibility)
    $NativeDir = Join-Path $ManagerDir "runtimes\$Runtime\native"
    Ensure-Directory $NativeDir

    $NuGetCache = Join-Path $env:USERPROFILE ".nuget\packages\microsoft.data.sqlclient.sni.runtime"
    $SniVersions = Get-ChildItem $NuGetCache -Directory | Sort-Object { [version]$_.Name } -Descending
    $LatestSniVersion = $SniVersions | Select-Object -First 1

    if ($LatestSniVersion) {
        $SniSourcePath = Join-Path $LatestSniVersion.FullName "runtimes\$Runtime\native\Microsoft.Data.SqlClient.SNI.dll"
        if (Test-Path $SniSourcePath) {
            Copy-Item $SniSourcePath $NativeDir -Force
            Write-Host "      Native SNI.dll copied to runtimes/$Runtime/native/" -ForegroundColor Green
        } else {
            throw "Native SNI.dll not found: $SniSourcePath"
        }
    } else {
        throw "Microsoft.Data.SqlClient.SNI.runtime package not found"
    }

    # Copy nssm.exe to manager directory (for service management)
    $NssmSourceDir = Join-Path $ProjectRoot "resources\nssm"
    $NssmSourceExe = Join-Path $NssmSourceDir "nssm.exe"
    if (Test-Path $NssmSourceExe) {
        Copy-Item $NssmSourceExe $ManagerDir -Force
        Write-Host "      NSSM copied to manager directory" -ForegroundColor Green
    } else {
        Write-Host "      WARNING: NSSM not found in resources\nssm" -ForegroundColor Yellow
        Write-Host "      Download from: https://nssm.cc/download" -ForegroundColor Yellow
    }

    Write-Host "      DeployManager built" -ForegroundColor Green
}
finally { Pop-Location }

# Step 5: Build Configurator
Write-Host "[5/8] Building Configurator..." -ForegroundColor Yellow
Push-Location $ProjectRoot
try {
    $ConfiguratorDir = Join-Path $BuildDir "configurator"
    if (Test-Path $ConfiguratorDir) { Remove-Item $ConfiguratorDir -Recurse -Force }
    Ensure-Directory $ConfiguratorDir

    dotnet publish backend/tools/Configurator/Configurator.csproj `
        -c $Configuration -r $Runtime --self-contained true `
        -o $ConfiguratorDir /p:PublishSingleFile=false
    if ($LASTEXITCODE -ne 0) { throw "Configurator build failed" }
    Write-Host "      Configurator built" -ForegroundColor Green
}
finally { Pop-Location }

# Step 6: Copy nginx to WebServer directory
Write-Host "[6/8] Copying nginx to WebServer..." -ForegroundColor Yellow
$NginxSourceDir = Join-Path $ProjectRoot "resources\nginx"
$WebServerDir = Join-Path $BuildDir "WebServer"

if (Test-Path $NginxSourceDir) {
    # Remove old WebServer directory if exists
    if (Test-Path $WebServerDir) { Remove-Item $WebServerDir -Recurse -Force }

    # Copy nginx files
    Copy-Item -Path $NginxSourceDir -Destination $WebServerDir -Recurse
    Write-Host "      Nginx copied to WebServer\" -ForegroundColor Green
} else {
    throw "Nginx source directory not found: $NginxSourceDir"
}

# Step 7: Copy public key to Server/keys directory
# 注意：只分发公钥，私钥保留在公司用于签署正式许可证
# 试用许可证不需要 RSA 签名，使用特殊标记 "TRIAL_LOCAL"
Write-Host "[7/8] Copying public key to Server..." -ForegroundColor Yellow
$SourceKeysDir = Join-Path $ProjectRoot "backend\src\DataForgeStudio.Api\keys"
$TargetKeysDir = Join-Path $BuildDir "Server\keys"

if (Test-Path $SourceKeysDir) {
    Ensure-Directory $TargetKeysDir

    # 只复制公钥（私钥不分发给用户，用于签署正式许可证）
    $PublicKeySource = Join-Path $SourceKeysDir "public_key.pem"
    $PublicKeyTarget = Join-Path $TargetKeysDir "public_key.pem"

    if (Test-Path $PublicKeySource) {
        Copy-Item $PublicKeySource $PublicKeyTarget -Force
        Write-Host "      Public key copied to Server\keys\" -ForegroundColor Green
        Write-Host "      Note: Private key is NOT distributed (security best practice)" -ForegroundColor Gray
    } else {
        Write-Host "      WARNING: public_key.pem not found in source" -ForegroundColor Yellow
    }
} else {
    Write-Host "      WARNING: Source keys directory not found" -ForegroundColor Yellow
    Write-Host "      Keys will be auto-generated on first run" -ForegroundColor Gray
}

# Step 8: Build Inno Setup installer
Write-Host "[8/8] Building Inno Setup Installer..." -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Inno Setup Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$InnoSetupPath = Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $InnoSetupPath)) {
    $InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $InnoSetupPath)) {
    $InnoSetupPath = "C:\Program Files\Inno Setup 6\ISCC.exe"
}

if (-not (Test-Path $InnoSetupPath)) {
    Write-Host "ERROR: Inno Setup not found" -ForegroundColor Red
    Write-Host "Install: winget install JRSoftware.InnoSetup" -ForegroundColor Yellow
    exit 1
}

$SetupScript = Join-Path $ProjectRoot "installer\setup.iss"
Ensure-Directory $DistDir

& $InnoSetupPath $SetupScript

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Build Successful!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Installer: $DistDir\DataForgeStudio-Setup.exe" -ForegroundColor White
    $SetupSize = (Get-Item "$DistDir\DataForgeStudio-Setup.exe").Length / 1MB
    Write-Host "Size: $([math]::Round($SetupSize, 2)) MB" -ForegroundColor White
} else {
    Write-Host "Installer build failed" -ForegroundColor Red
    exit 1
}
