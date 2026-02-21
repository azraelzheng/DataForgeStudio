@echo off
echo ========================================
echo DataForgeStudio 测试服务卸载脚本
echo ========================================
echo.

echo 停止服务...
sc stop "DFAppService" >nul 2>&1
timeout /t 2 >nul

echo 删除服务...
sc delete "DFAppService"
if errorlevel 1 (
    echo 服务删除失败或服务不存在。
) else (
    echo 服务已成功删除。
)

echo.
echo ========================================
pause
