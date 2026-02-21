@echo off
echo ========================================
echo DataForgeStudio 测试服务安装脚本
echo ========================================
echo.

cd /d "%~dp0"

echo [1/3] 构建测试服务...
dotnet build -c Release
if errorlevel 1 (
    echo 构建失败!
    pause
    exit /b 1
)

echo.
echo [2/3] 发布测试服务...
dotnet publish -c Release -o bin\Release\publish --self-contained true -r win-x64
if errorlevel 1 (
    echo 发布失败!
    pause
    exit /b 1
)

set SERVICE_NAME=DFAppService
set SERVICE_PATH=%~dp0bin\Release\publish\DataForgeStudio.exe

echo.
echo [3/3] 安装 Windows 服务...
echo 服务名称: %SERVICE_NAME%
echo 服务路径: %SERVICE_PATH%
echo.

REM 检查服务是否已存在
sc query "DFAppService" >nul 2>&1
if %errorlevel% equ 0 (
    echo 服务已存在，先删除旧服务...
    sc stop "DFAppService" >nul 2>&1
    timeout /t 2 >nul
    sc delete "DFAppService" >nul 2>&1
    timeout /t 2 >nul
)

REM 创建服务
sc create "DFAppService" binPath= "%SERVICE_PATH%" start= auto DisplayName= "DataForgeStudio API Service"
if errorlevel 1 (
    echo 服务创建失败! 请以管理员身份运行此脚本。
    pause
    exit /b 1
)

REM 设置服务描述
sc description "DFAppService" "DataForgeStudio 后端 API 服务（测试用）"

echo.
echo ========================================
echo 服务安装成功!
echo.
echo 服务名称: DFAppService
echo 启动类型: 自动
echo.
echo 使用以下命令管理服务:
echo   启动: sc start "DFAppService"
echo   停止: sc stop "DFAppService"
echo   状态: sc query "DFAppService"
echo   删除: sc delete "DFAppService"
echo ========================================
pause
