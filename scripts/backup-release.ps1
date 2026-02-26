# backup-release.ps1
# DataForgeStudio Release Backup Script
# Run this before each release

param(
    [string]$Version = "1.0.0",
    [string]$BackupRoot = "H:\DataForge_Backup"
)

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = Join-Path $BackupRoot "v$Version`_$Timestamp"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DataForgeStudio Release Backup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host "Backup to: $BackupDir"
Write-Host ""

# Create backup directory
if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}

# 1. Backup signing keys (CRITICAL!)
Write-Host "[1/5] Backing up signing keys..." -ForegroundColor Yellow
$KeysSource = Join-Path $ProjectRoot "backend\src\DataForgeStudio.Api\keys"
$KeysBackup = Join-Path $BackupDir "keys"
if (Test-Path $KeysSource) {
    Copy-Item $KeysSource $KeysBackup -Recurse -Force
    Write-Host "      Keys backed up to: $KeysBackup" -ForegroundColor Green
} else {
    Write-Host "      WARNING: Keys directory not found!" -ForegroundColor Red
}

# 2. Backup installer
Write-Host "[2/5] Backing up installer..." -ForegroundColor Yellow
$InstallerSource = Join-Path $ProjectRoot "dist\DataForgeStudio-Setup.exe"
$InstallerBackup = Join-Path $BackupDir "DataForgeStudio-Setup-v$Version.exe"
if (Test-Path $InstallerSource) {
    Copy-Item $InstallerSource $InstallerBackup -Force
    $FileSize = (Get-Item $InstallerBackup).Length / 1MB
    Write-Host "      Installer backed up: $([math]::Round($FileSize, 2)) MB" -ForegroundColor Green
} else {
    Write-Host "      WARNING: Installer not found! Run build-installer.ps1 first" -ForegroundColor Red
}

# 3. Create git bundle
Write-Host "[3/5] Creating git bundle..." -ForegroundColor Yellow
$BundlePath = Join-Path $BackupDir "DataForgeStudio-v$Version.bundle"
Push-Location $ProjectRoot
try {
    git bundle create $BundlePath --all
    if ($LASTEXITCODE -eq 0) {
        Write-Host "      Git bundle created: $BundlePath" -ForegroundColor Green
    } else {
        Write-Host "      WARNING: Git bundle creation failed" -ForegroundColor Yellow
    }
}
finally { Pop-Location }

# 4. Export git log
Write-Host "[4/5] Exporting git log..." -ForegroundColor Yellow
$LogPath = Join-Path $BackupDir "git-log-v$Version.txt"
Push-Location $ProjectRoot
try {
    git log --oneline --decorate > $LogPath
    Write-Host "      Git log exported: $LogPath" -ForegroundColor Green
}
finally { Pop-Location }

# 5. Backup important docs
Write-Host "[5/5] Backing up documentation..." -ForegroundColor Yellow
$DocsBackup = Join-Path $BackupDir "docs"
$DocsSource = Join-Path $ProjectRoot "docs"
if (Test-Path $DocsSource) {
    Copy-Item $DocsSource $DocsBackup -Recurse -Force
    Write-Host "      Docs backed up to: $DocsBackup" -ForegroundColor Green
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Backup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "Backup location: $BackupDir" -ForegroundColor White
Write-Host ""
Write-Host "Contents:" -ForegroundColor White
Get-ChildItem $BackupDir -Recurse | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
    $Size = $_.Length / 1KB
    Write-Host "  - $($_.Name) ($([math]::Round($Size, 1)) KB)"
}

Write-Host ""
Write-Host "IMPORTANT: Store this backup in a safe location!" -ForegroundColor Yellow
Write-Host "- Private key (keys/private_key.pem) is CRITICAL for license generation" -ForegroundColor Yellow
Write-Host "- Git bundle allows full repository recovery" -ForegroundColor Yellow
