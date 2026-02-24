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
Write-Host "注意: 可能需要重启计算机才能完全移除服务"
