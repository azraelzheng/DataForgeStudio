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
    Write-Host "[1/5] Building Backend API..." -ForegroundColor Yellow
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
    Write-Host "[1/5] Skipping backend build" -ForegroundColor Gray
}

# Step 2: Build Frontend
if (-not $SkipFrontend) {
    Write-Host "[2/5] Building Frontend..." -ForegroundColor Yellow
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
    Write-Host "[2/5] Skipping frontend build" -ForegroundColor Gray
}

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

    Write-Host "      DeployManager built" -ForegroundColor Green
}
finally { Pop-Location }

# Step 4: Build Configurator
Write-Host "[4/5] Building Configurator..." -ForegroundColor Yellow
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

# Step 5: Ensure tools directory exists
Write-Host "[5/5] Checking tools directory..." -ForegroundColor Yellow

$NssmDir = Join-Path $BuildDir "tools\nssm"
$NssmExe = Join-Path $NssmDir "nssm.exe"
if (-not (Test-Path $NssmExe)) {
    Write-Host "      WARNING: NSSM not found, please download to $NssmDir" -ForegroundColor Yellow
    Write-Host "      Download: https://nssm.cc/download" -ForegroundColor Yellow
}

$ScriptsDir = Join-Path $BuildDir "tools\scripts"
Ensure-Directory $ScriptsDir
$SourceScriptsDir = Join-Path $ProjectRoot "backend\tools\scripts"
if (Test-Path $SourceScriptsDir) {
    Copy-Item "$SourceScriptsDir\*.ps1" $ScriptsDir -Force
    Write-Host "      Service scripts copied" -ForegroundColor Green
}

# Build Inno Setup installer
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
