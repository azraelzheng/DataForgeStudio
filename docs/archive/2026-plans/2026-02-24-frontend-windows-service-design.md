# 前端服务 Windows 服务化设计

## 概述

将前端服务（Nginx）包装为 Windows 服务，实现统一服务管理。

## 现有架构

```
DeployManager
├── 服务管理页签
│   ├── API 服务 (DFAppService) - Windows 服务
│   └── Web 服务 - IIS/Nginx 进程管理
└── 前端模式页签
    └── IIS/Nginx 切换
```

## 改动方案

### 1. 新增 Windows 服务

| 服务名 | 用途 | 类型 |
|--------|------|------|
| `DFAppService` | 后端 API 服务 | 现有 |
| `DFWebService` | 前端 Nginx 服务 | 新增 |

### 2. 修改文件

| 文件 | 改动 |
|------|------|
| `ConfigService.cs` | 添加 `WebServiceName` 常量 |
| `IWebServiceManager.cs` | 无需改动 |
| `WebServiceManager.cs` | Nginx 模式改用 Windows 服务管理 |
| `FrontendModeViewModel.cs` | 切换模式后重启 DFWebService 服务 |

### 3. 安装流程

部署时使用 NSSM 安装 Nginx 服务：
```powershell
nssm install DFWebService "C:\...\WebServer\nginx.exe"
nssm set DFWebService AppDirectory "C:\...\WebServer"
nssm set DFWebService DisplayName "DataForge Studio Web Service"
nssm set DFWebService Description "DataForge Studio 前端服务"
nssm set DFWebService Start SERVICE_AUTO_START
```

### 4. 服务管理流程

```
服务管理页签:
┌─────────────────────────────────────┐
│ API 服务 (DFAppService)             │
│ 状态: 运行中  [启动] [停止] [重启]   │
├─────────────────────────────────────┤
│ Web 服务 (DFWebService)             │
│ 状态: 运行中  [启动] [停止] [重启]   │
└─────────────────────────────────────┘

前端模式页签:
┌─────────────────────────────────────┐
│ 当前模式: ○ IIS  ○ Nginx            │
│                                     │
│         [应用并重启服务]             │
└─────────────────────────────────────┘
```

### 5. IIS 模式处理

IIS 模式下：
- DFWebService 服务应处于停止状态
- 由 IIS 服务直接管理站点
- WebServiceManager 检测到 IIS 模式时，直接操作 IIS API

### 6. Nginx 模式处理

Nginx 模式下：
- 通过 DFWebService Windows 服务管理 Nginx
- WebServiceManager 使用 ServiceController 管理 DFWebService

## 实施步骤

1. 修改 `ConfigService.cs` 添加 Web 服务名常量
2. 修改 `WebServiceManager.cs` 支持 Windows 服务模式
3. 修改 `FrontendModeViewModel.cs` 切换后重启服务
4. 创建部署脚本安装 Nginx 服务
5. 测试验证

## 风险评估

- **低风险**：改动集中在服务管理层，不影响业务逻辑
- **兼容性**：IIS 模式保持现有行为不变
