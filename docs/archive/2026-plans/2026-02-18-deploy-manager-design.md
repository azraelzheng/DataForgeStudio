# DataForgeStudio 部署管理工具设计文档

## 概述

开发一个 Windows 桌面管理工具（WPF），用于管理 DataForgeStudio 的部署配置，目标用户为非技术人员。

## 目标用户

非技术用户（客户公司的普通员工），需要简单易用的图形界面。

## 核心功能

1. **服务管理** - 启动、停止、重启后端服务，查看运行状态
2. **数据库配置** - 配置数据库连接字符串，测试连接
3. **端口配置** - 修改前端和后端端口
4. **前端模式切换** - 在 IIS 和 Nginx 之间切换

## 技术方案

### 方案选择

独立管理工具（非系统托盘应用），便于用户操作和理解。

### 技术栈

| 组件 | 技术 |
|------|------|
| UI 框架 | WPF (.NET 8.0) |
| MVVM 框架 | CommunityToolkit.Mvvm |
| IIS 管理 | Microsoft.Web.Administration |
| 服务管理 | System.ServiceProcess |
| 配置存储 | JSON (System.Text.Json) |
| 密码加密 | AES (复用现有 EncryptionHelper) |

## UI 设计

### 主窗口布局

```
┌─────────────────────────────────────────────────────────────────┐
│  DataForgeStudio 管理工具                              [─][□][×] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  [服务管理]  [数据库配置]  [端口配置]  [前端模式]        │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    (当前选中标签页内容)                  │   │
│  │                                                         │   │
│  │                                                         │   │
│  │                                                         │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  状态: ● 运行中    端口: 5000    模式: IIS              │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### 标签页内容

#### 1. 服务管理

- 服务状态显示（运行中/已停止）
- 启动、停止、重启按钮
- 开机自启复选框
- 运行信息（启动时间、运行时长、内存占用）

#### 2. 数据库配置

- 服务器地址、端口、数据库名、用户名、密码
- 测试连接按钮
- 保存配置按钮
- 连接状态反馈

#### 3. 端口配置

- 后端 API 端口
- 前端访问端口
- 保存配置按钮
- 重启提醒

#### 4. 前端模式

- IIS / Nginx 单选
- 切换模式按钮
- 安装状态检测

## 配置文件结构

```json
// deploy-config.json
{
  "version": "1.0.0",
  "installPath": "C:\\Program Files\\DataForgeStudio",

  "backend": {
    "port": 5000,
    "serviceName": "DataForgeStudio API"
  },

  "frontend": {
    "port": 80,
    "mode": "iis",
    "iisSiteName": "DataForgeStudio"
  },

  "database": {
    "server": "localhost",
    "port": 1433,
    "database": "DataForgeStudio_V4",
    "username": "sa",
    "password": ""
  }
}
```

## 项目结构

```
backend/tools/DeployManager/
├── DeployManager.csproj           # WPF 项目
├── App.xaml                       # 应用入口
├── App.xaml.cs
├── MainWindow.xaml                # 主窗口
├── MainWindow.xaml.cs
│
├── Views/                         # 各标签页
│   ├── ServiceControlView.xaml
│   ├── DatabaseConfigView.xaml
│   ├── PortConfigView.xaml
│   └── FrontendModeView.xaml
│
├── ViewModels/                    # MVVM 视图模型
│   ├── MainViewModel.cs
│   ├── ServiceControlViewModel.cs
│   ├── DatabaseConfigViewModel.cs
│   ├── PortConfigViewModel.cs
│   └── FrontendModeViewModel.cs
│
├── Services/                      # 业务服务
│   ├── IWindowsServiceManager.cs
│   ├── WindowsServiceManager.cs
│   ├── IIisManager.cs
│   ├── IisManager.cs
│   ├── INginxManager.cs
│   ├── NginxManager.cs
│   ├── ConfigService.cs
│   └── DatabaseConnectionService.cs
│
├── Models/                        # 数据模型
│   ├── DeployConfig.cs
│   └── ServiceStatus.cs
│
└── Utils/                         # 工具类
    └── PasswordHelper.cs
```

## 关键实现要点

### 1. Windows 服务管理

```csharp
// 使用 ServiceController 控制服务
using System.ServiceProcess;

public class WindowsServiceManager : IWindowsServiceManager
{
    private readonly string _serviceName;

    public ServiceStatus GetStatus()
    {
        using var controller = new ServiceController(_serviceName);
        return controller.Status == ServiceControllerStatus.Running
            ? ServiceStatus.Running
            : ServiceStatus.Stopped;
    }

    public void Start() { /* ... */ }
    public void Stop() { /* ... */ }
    public void Restart() { /* ... */ }
}
```

### 2. IIS 管理

```csharp
// 使用 Microsoft.Web.Administration
using Microsoft.Web.Administration;

public class IisManager : IIisManager
{
    public void ConfigureSite(string siteName, int port, string physicalPath)
    {
        using var server = new ServerManager();
        var site = server.Sites[siteName];
        // 配置站点...
        server.CommitChanges();
    }
}
```

### 3. 配置文件同步

- 修改端口时，更新 `appsettings.json` 和 IIS/Nginx 配置
- 切换前端模式时，停止旧服务、更新配置、启动新服务
- 数据库密码使用 AES 加密存储

## 权限要求

- 需要**管理员权限**运行
- 用于管理 Windows 服务和 IIS

## 部署位置

```
C:\Program Files\DataForgeStudio\
├── api\                          # 后端服务
├── frontend\                     # 前端文件
├── config\                       # 配置文件
│   ├── deploy-config.json
│   ├── iis\
│   └── nginx\
└── DeployManager.exe             # 管理工具
```

## 成功标准

1. 用户可以一键启动/停止/重启服务
2. 用户可以轻松修改数据库连接并测试
3. 用户可以修改端口配置
4. 用户可以在 IIS 和 Nginx 之间切换
5. 所有操作都有清晰的状态反馈
6. 错误提示友好易懂

---

创建时间: 2026-02-18
版本: 1.0.0
