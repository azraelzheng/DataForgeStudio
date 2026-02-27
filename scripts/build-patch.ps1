# build-patch.ps1
# DataForgeStudio Patch Build Script
# Build single EXE patch installer with embedded resources

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",

    [switch]$BackendOnly = $false,
    [switch]$FrontendOnly = $false,
    [switch]$SkipObfuscation = $false,

    [string]$SqlScriptPath = ""
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$PatchDir = Join-Path $ProjectRoot "dist\patches"
$TempDir = Join-Path $ProjectRoot "build\patch-temp"
$PatchContentDir = Join-Path $TempDir "patch-content"
$PatchInstallerDir = Join-Path $ProjectRoot "backend\tools\PatchInstaller"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DataForgeStudio Patch Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host ""

# Validate version format
if ($Version -notmatch "^\d+\.\d+\.\d+$") {
    Write-Host "ERROR: Invalid version format. Expected: x.x.x (e.g. 1.0.1)" -ForegroundColor Red
    exit 1
}

function Ensure-Directory {
    param([string]$Path)
    if (Test-Path $Path) { Remove-Item $Path -Recurse -Force }
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

# Clean up
Ensure-Directory $TempDir
Ensure-Directory $PatchContentDir
Ensure-Directory $PatchDir

# Remove old patch.zip if exists
$PatchZipPath = Join-Path $PatchInstallerDir "patch.zip"
if (Test-Path $PatchZipPath) { Remove-Item $PatchZipPath -Force }

# 1. Build backend
if (-not $FrontendOnly) {
    Write-Host "[1/4] Building Backend..." -ForegroundColor Yellow

    Push-Location $ProjectRoot
    try {
        dotnet publish backend/src/DataForgeStudio.Api/DataForgeStudio.Api.csproj `
            -c $Configuration -r $Runtime --self-contained true `
            -o "$TempDir/backend-publish" /p:PublishSingleFile=false
        if ($LASTEXITCODE -ne 0) { throw "Backend build failed" }

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

        # Copy DLLs to patch content
        $ServerDir = Join-Path $PatchContentDir "Server"
        New-Item -ItemType Directory -Path $ServerDir -Force | Out-Null

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
                Copy-Item $source $ServerDir -Force
            }
        }

        Write-Host "      Backend prepared" -ForegroundColor Green
    }
    finally { Pop-Location }
} else {
    Write-Host "[1/4] Skipping backend (FrontendOnly mode)" -ForegroundColor Gray
}

# 2. Build frontend
if (-not $BackendOnly) {
    Write-Host "[2/4] Building Frontend..." -ForegroundColor Yellow

    $FrontendDir = Join-Path $ProjectRoot "frontend"
    Push-Location $FrontendDir
    try {
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "Frontend build failed" }

        $WebSiteDir = Join-Path $PatchContentDir "WebSite"
        Copy-Item -Path "dist" -Destination $WebSiteDir -Recurse

        Write-Host "      Frontend prepared" -ForegroundColor Green
    }
    finally { Pop-Location }
} else {
    Write-Host "[2/4] Skipping frontend (BackendOnly mode)" -ForegroundColor Gray
}

# 3. Add SQL scripts and version info
Write-Host "[3/4] Preparing patch package..." -ForegroundColor Yellow

# Copy SQL scripts
if ($SqlScriptPath -and (Test-Path $SqlScriptPath)) {
    $SqlTargetDir = Join-Path $PatchContentDir "sql"
    Copy-Item -Path $SqlScriptPath -Destination $SqlTargetDir -Recurse
    $SqlFiles = Get-ChildItem $SqlTargetDir -Filter "*.sql" -Recurse
    Write-Host "      Added $($SqlFiles.Count) SQL script(s)" -ForegroundColor Green
}

# Create version info
$VersionInfo = @{
    version = $Version
    buildDate = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
    type = if ($BackendOnly) { "backend" } elseif ($FrontendOnly) { "frontend" } else { "full" }
}
$VersionInfo | ConvertTo-Json | Out-File (Join-Path $PatchContentDir "patch-info.json") -Encoding UTF8

# Copy docs folder (for help documents)
$DocsSourceDir = Join-Path $ProjectRoot "resources\docs"
if (Test-Path $DocsSourceDir) {
    $DocsTargetDir = Join-Path $PatchContentDir "docs"
    Copy-Item -Path $DocsSourceDir -Destination $DocsTargetDir -Recurse
    Write-Host "      Added docs folder" -ForegroundColor Green
}

# Copy DeployManager for Manager directory update
$DeployManagerSource = Join-Path $ProjectRoot "backend\tools\DeployManager\bin\Release\net8.0-windows\DeployManager.dll"
if (Test-Path $DeployManagerSource) {
    $ManagerDir = Join-Path $PatchContentDir "Manager"
    New-Item -ItemType Directory -Path $ManagerDir -Force | Out-Null
    Copy-Item -Path $DeployManagerSource -Destination $ManagerDir -Force
    # Also copy Shared.dll which DeployManager depends on
    $SharedSource = Join-Path $ProjectRoot "backend\tools\DeployManager\bin\Release\net8.0-windows\DataForgeStudio.Shared.dll"
    if (Test-Path $SharedSource) {
        Copy-Item -Path $SharedSource -Destination $ManagerDir -Force
    }
    Write-Host "      Added DeployManager" -ForegroundColor Green
}

# Create patch.zip in PatchInstaller directory (will be embedded)
Compress-Archive -Path "$PatchContentDir\*" -DestinationPath $PatchZipPath -Force
Write-Host "      patch.zip created" -ForegroundColor Green

# 4. Build PatchInstaller with embedded zip
Write-Host "[4/4] Building PatchInstaller EXE..." -ForegroundColor Yellow

Push-Location $ProjectRoot
try {
    dotnet publish backend/tools/PatchInstaller/PatchInstaller.csproj `
        -c $Configuration -r $Runtime --self-contained false `
        -o "$TempDir/patchinstaller-output"
    if ($LASTEXITCODE -ne 0) { throw "PatchInstaller build failed" }

    # Copy final EXE
    $OutputPath = Join-Path $PatchDir "DataForgeStudio-Patch-$Version.exe"
    Copy-Item (Join-Path "$TempDir/patchinstaller-output" "PatchInstaller.exe") $OutputPath -Force

    $PatchSize = (Get-Item $OutputPath).Length / 1MB

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Patch Build Successful!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Patch File: $OutputPath" -ForegroundColor White
    Write-Host "Size: $([math]::Round($PatchSize, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Cyan
    Write-Host "  DataForgeStudio-Patch-$Version.exe -i `"C:\Program Files\DataForgeStudio`"" -ForegroundColor White
    Write-Host ""
    Write-Host "With database update:" -ForegroundColor Cyan
    Write-Host "  DataForgeStudio-Patch-$Version.exe -i `"C:\Program Files\DataForgeStudio`" --db-server localhost --db-name DataForgeStudio --db-user sa --db-password xxx" -ForegroundColor White
}
finally { Pop-Location }

# Cleanup
Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue
if (Test-Path $PatchZipPath) { Remove-Item $PatchZipPath -Force -ErrorAction SilentlyContinue }
