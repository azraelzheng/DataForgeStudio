# install-web-service.ps1
# 安装 DFWebService (Nginx) Windows 服务

param(
    [string]$InstallPath = "",
    [string]$ServiceName = "DFWebService"
)

$ErrorActionPreference = "Stop"

# 如果未指定安装路径，尝试自动检测
if ([string]::IsNullOrEmpty($InstallPath)) {
    # 尝试从注册表读取
    try {
        $regPath = Get-ItemProperty -Path "HKLM:\SOFTWARE\DataForgeStudio" -ErrorAction SilentlyContinue
        if ($regPath -and $regPath.InstallPath) {
            $InstallPath = $regPath.InstallPath
        }
    }
    catch { }

    # 如果注册表没有，使用脚本所在目录的上两级
    if ([string]::IsNullOrEmpty($InstallPath)) {
        $InstallPath = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    }
}

Write-Host "安装路径: $InstallPath"

# 检查管理员权限
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "请以管理员身份运行此脚本"
    exit 1
}

# 检查 NSSM
$nssmPath = Join-Path $InstallPath "tools\nssm\nssm.exe"
if (-not (Test-Path $nssmPath)) {
    # 尝试备用路径
    $nssmPath = Join-Path $InstallPath "nssm.exe"
    if (-not (Test-Path $nssmPath)) {
        Write-Error "未找到 NSSM: $nssmPath`n请下载 NSSM 并放置在 tools\nssm\ 目录下"
        exit 1
    }
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

# 确保 logs 目录存在
$logsPath = Join-Path $nginxPath "logs"
if (-not (Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
}

# 使用 NSSM 安装服务
Write-Host "正在安装服务 $ServiceName ..."

& $nssmPath install $ServiceName $nginxExe
& $nssmPath set $ServiceName AppDirectory $nginxPath
& $nssmPath set $ServiceName DisplayName "DataForge Studio Web Service"
& $nssmPath set $ServiceName Description "DataForge Studio 前端服务 (Nginx)"
& $nssmPath set $ServiceName Start SERVICE_AUTO_START
& $nssmPath set $ServiceName AppStdout (Join-Path $logsPath "service_stdout.log")
& $nssmPath set $ServiceName AppStderr (Join-Path $logsPath "service_stderr.log")
& $nssmPath set $ServiceName AppRotateFiles 1
& $nssmPath set $ServiceName AppRotateBytes 1048576

Write-Host "服务 $ServiceName 安装成功"

# 启动服务
Start-Service -Name $ServiceName
Write-Host "服务 $ServiceName 已启动"
