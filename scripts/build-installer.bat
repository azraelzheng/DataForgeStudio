@echo off
setlocal

echo ========================================
echo DataForgeStudio 安装包构建脚本
echo ========================================
echo.

cd /d "%~dp0.."
set PROJECT_ROOT=%CD%
set BUILD_DIR=%PROJECT_ROOT%\build\installer
set DIST_DIR=%PROJECT_ROOT%\dist

echo PROJECT_ROOT=%PROJECT_ROOT%
echo.

echo 清理旧构建...
if exist "%BUILD_DIR%" rd /s /q "%BUILD_DIR%"
mkdir "%BUILD_DIR%"
mkdir "%DIST_DIR%"
mkdir "%BUILD_DIR%\Server"
mkdir "%BUILD_DIR%\WebSite"
mkdir "%BUILD_DIR%\WebServer"
mkdir "%BUILD_DIR%\DBServer"
mkdir "%BUILD_DIR%\manager"
mkdir "%BUILD_DIR%\configurator"
echo 目录创建完成
echo.

echo [1/7] 构建后端 API...
dotnet publish "%PROJECT_ROOT%\backend\src\DataForgeStudio.Api\DataForgeStudio.Api.csproj" -c Release -o "%BUILD_DIR%\Server" --self-contained true -r win-x64 /p:WarningLevel=0 /p:TreatWarningsAsErrors=false
if errorlevel 1 (
    echo 后端 API 构建失败!
    exit /b 1
)
echo API 构建完成

echo 复制许可证验证公钥...
if not exist "%BUILD_DIR%\Server\keys" mkdir "%BUILD_DIR%\Server\keys"
if exist "%PROJECT_ROOT%\backend\src\DataForgeStudio.Api\keys\public_key.pem" (
    copy /y "%PROJECT_ROOT%\backend\src\DataForgeStudio.Api\keys\public_key.pem" "%BUILD_DIR%\Server\keys\"
    echo public_key.pem 已复制
) else (
    echo.
    echo ========================================
    echo 警告: 许可证密钥不存在！
    echo ========================================
    echo 请先运行一次 API 以生成密钥对：
    echo   cd backend\src\DataForgeStudio.Api
    echo   dotnet run
    echo 然后重新运行此构建脚本。
    echo.
    echo 或者手动生成密钥后重新构建。
    echo ========================================
    exit /b 1
)
echo.

echo [2/7] 构建前端...
cd /d "%PROJECT_ROOT%\frontend"
call npm install
call npm run build
if errorlevel 1 (
    echo 前端构建失败!
    exit /b 1
)
xcopy /s /e /y "%PROJECT_ROOT%\frontend\dist\*" "%BUILD_DIR%\WebSite\"
cd /d "%PROJECT_ROOT%"
echo 前端构建完成
echo.

echo [3/7] 构建系统管理工具...
dotnet publish "%PROJECT_ROOT%\backend\tools\DeployManager\DeployManager.csproj" -c Release -o "%BUILD_DIR%\manager" --self-contained true -r win-x64 /p:WarningLevel=0 /p:TreatWarningsAsErrors=false
if errorlevel 1 (
    echo 系统管理工具构建失败!
    exit /b 1
)
echo 清理多余的语言资源文件...
for %%d in (cs de es fr it ja ko pl pt-BR ru tr zh-Hant) do (
    if exist "%BUILD_DIR%\manager\%%d" rd /s /q "%BUILD_DIR%\manager\%%d"
)
echo 系统管理工具构建完成
echo.

echo [4/7] 复制 Nginx...
xcopy /s /e /y "%PROJECT_ROOT%\resources\nginx\*" "%BUILD_DIR%\WebServer\"
if exist "%BUILD_DIR%\WebServer\html" rd /s /q "%BUILD_DIR%\WebServer\html"
if exist "%BUILD_DIR%\WebServer\docs" rd /s /q "%BUILD_DIR%\WebServer\docs"
if exist "%BUILD_DIR%\WebServer\contrib" rd /s /q "%BUILD_DIR%\WebServer\contrib"
echo Nginx 复制完成
echo.

echo [5/7] 构建配置器...
dotnet publish "%PROJECT_ROOT%\backend\tools\Configurator\Configurator.csproj" -c Release -o "%BUILD_DIR%\configurator" --self-contained true -r win-x64 /p:WarningLevel=0 /p:TreatWarningsAsErrors=false
if errorlevel 1 (
    echo 配置器构建失败!
    exit /b 1
)
echo 配置器构建完成
echo.

echo [6/7] 使用 Inno Setup 打包...
set ISCC_PATH=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe
if not exist "%ISCC_PATH%" set ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe
if not exist "%ISCC_PATH%" set ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe

if not exist "%ISCC_PATH%" (
    echo 警告: 未找到 Inno Setup
    echo 构建完成！文件位于: %BUILD_DIR%
    exit /b 0
)

echo 使用 Inno Setup: %ISCC_PATH%
"%ISCC_PATH%" "%PROJECT_ROOT%\installer\setup.iss" /DProjectRoot="%PROJECT_ROOT%" /DBuildDir="%BUILD_DIR%" /DDistDir="%DIST_DIR%"
if errorlevel 1 (
    echo Inno Setup 打包失败!
    exit /b 1
)

echo.
echo ========================================
echo 构建完成！
echo 安装包位于: %DIST_DIR%\DataForgeStudio-Setup.exe
echo ========================================
