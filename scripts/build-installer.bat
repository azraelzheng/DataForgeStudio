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
mkdir "%BUILD_DIR%\api"
mkdir "%BUILD_DIR%\frontend"
mkdir "%BUILD_DIR%\nginx"
mkdir "%BUILD_DIR%\manager"
mkdir "%BUILD_DIR%\installer"
echo 目录创建完成
echo.

echo [1/6] 构建后端 API...
dotnet publish "%PROJECT_ROOT%\backend\src\DataForgeStudio.Api\DataForgeStudio.Api.csproj" -c Release -o "%BUILD_DIR%\api" --self-contained true -r win-x64 /p:WarningLevel=0 /p:TreatWarningsAsErrors=false
if errorlevel 1 (
    echo 后端 API 构建失败!
    exit /b 1
)
echo API 构建完成
echo.

echo [2/6] 构建前端...
cd /d "%PROJECT_ROOT%\frontend"
call npm install
call npm run build
if errorlevel 1 (
    echo 前端构建失败!
    exit /b 1
)
xcopy /s /e /y "%PROJECT_ROOT%\frontend\dist\*" "%BUILD_DIR%\frontend\"
cd /d "%PROJECT_ROOT%"
echo 前端构建完成
echo.

echo [3/6] 构建系统管理工具...
dotnet publish "%PROJECT_ROOT%\backend\tools\DeployManager\DeployManager.csproj" -c Release -o "%BUILD_DIR%\manager" --self-contained true -r win-x64 /p:WarningLevel=0 /p:TreatWarningsAsErrors=false
if errorlevel 1 (
    echo 系统管理工具构建失败!
    exit /b 1
)
echo 系统管理工具构建完成
echo.

echo [4/6] 复制 Nginx...
xcopy /s /e /y "%PROJECT_ROOT%\resources\nginx\*" "%BUILD_DIR%\nginx\"
if exist "%BUILD_DIR%\nginx\html" rd /s /q "%BUILD_DIR%\nginx\html"
if exist "%BUILD_DIR%\nginx\docs" rd /s /q "%BUILD_DIR%\nginx\docs"
if exist "%BUILD_DIR%\nginx\contrib" rd /s /q "%BUILD_DIR%\nginx\contrib"
echo Nginx 复制完成
echo.

echo [5/6] 构建安装程序...
dotnet publish "%PROJECT_ROOT%\backend\tools\Installer\Installer.csproj" -c Release -o "%BUILD_DIR%\installer" --self-contained true -r win-x64 /p:WarningLevel=0 /p:TreatWarningsAsErrors=false
if errorlevel 1 (
    echo 安装程序构建失败!
    exit /b 1
)
echo 安装程序构建完成
echo.

echo [6/6] 使用 Inno Setup 打包...
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
