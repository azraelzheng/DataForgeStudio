# DataForgeStudio V1.0 项目状态

**更新日期**: 2026-02-21

## 项目概述

**项目名称**: DataForgeStudio
**版本**: V1.0
**项目类型**: Web 报表管理系统
**技术栈**: ASP.NET Core 8.0 + Vue 3 + SQL Server

## 当前状态

### 整体进度: 100% 完成

| 模块 | 状态 | 完成度 |
|------|------|--------|
| 后端 API | ✅ 完成 | 100% |
| 前端界面 | ✅ 完成 | 100% |
| 数据库设计 | ✅ 完成 | 100% |
| 认证授权 | ✅ 完成 | 100% |
| 许可证系统 | ✅ 完成 | 100% |
| 安全加固 | ✅ 完成 | 100% |
| 报表功能 | ✅ 完成 | 100% |
| 系统管理工具 | ✅ 完成 | 100% |
| 安装程序 | ✅ 完成 | 100% |

## 已实现的核心功能

### 1. 认证与授权
- JWT Token 认证
- 用户登录/登出
- 密码哈希存储 (BCrypt)
- 基于角色的访问控制 (RBAC)
- 33 个预定义权限点

### 2. 用户管理
- 用户 CRUD 操作
- 角色分配
- 密码重置
- Root 用户保护 (IsSystem=1)

### 3. 角色管理
- 角色 CRUD 操作
- 权限分配
- 系统角色保护

### 4. 报表功能
- 报表设计器 (SQL 编辑器)
- 字段配置 (显示、隐藏、排序、格式化)
- 参数配置 (查询条件绑定)
- 报表查询
- Excel 导出 (ClosedXML)

### 5. 数据源管理
- 多数据库支持 (SQL Server, MySQL, Oracle, PostgreSQL)
- 连接测试
- 密码 AES 加密存储

### 6. 系统管理工具 (DeployManager)
- Windows 服务管理 (DFAppService)
- 前端服务管理 (IIS/Nginx - DFWebService)
- 数据库配置
- 端口配置（含占用检测）
- 开机自启设置

### 7. 许可证系统
- RSA 2048 位密钥管理
- 许可证生成工具
- 许可证激活/验证
- 机器码绑定
- 功能模块控制
- 用户数限制
- 试用期防重置机制
  - DPAPI 加密存储
  - 多位置冗余（注册表、ProgramData、应用目录）
  - 交叉验证取最早时间
  - 15 天试用期

### 8. 安全加固
- ✅ 环境变量配置 (SecurityOptions)
- ✅ 随机临时密码生成
- ✅ SQL 注入防护 (SqlValidationService)
- ✅ 速率限制 (RateLimitMiddleware)
- ✅ CORS 配置
- ✅ HTTPS 生产环境支持

### 9. 安装程序
- ✅ Inno Setup 打包
- ✅ 安装向导 (WPF)
- ✅ 数据库初始化
- ✅ 服务注册
- ✅ IIS/Nginx 配置

## 技术栈

### 后端
| 组件 | 技术 | 版本 |
|------|------|------|
| 框架 | ASP.NET Core | 8.0 |
| ORM | Entity Framework Core | 8.0.11 |
| 认证 | JWT Bearer | 8.0.11 |
| Excel 导出 | ClosedXML | 0.104.2 |
| CSV 导出 | CsvHelper | 30.0.1 |
| 密码哈希 | BCrypt.Net-Next | 4.0.3 |

### 前端
| 组件 | 技术 |
|------|------|
| 框架 | Vue 3 |
| UI 库 | Element Plus |
| 状态管理 | Pinia |
| 路由 | Vue Router 4 |
| 图表 | Apache ECharts |
| 构建 | Vite |

### 数据库
- SQL Server 2005+ 兼容
- 14 个核心表
- 2 个视图
- 3 个存储过程

## 项目结构

```
DataForgeStudio/
├── backend/                    # 后端项目
│   ├── src/
│   │   ├── DataForgeStudio.Api/         # Web API
│   │   ├── DataForgeStudio.Core/        # 业务逻辑
│   │   ├── DataForgeStudio.Domain/      # 领域模型
│   │   ├── DataForgeStudio.Data/        # 数据访问
│   │   └── DataForgeStudio.Shared/      # 共享工具
│   └── tools/
│       ├── LicenseGenerator/            # 许可证生成工具
│       ├── DeployManager/               # 系统管理工具
│       ├── Installer/                   # 安装向导
│       └── TestService/                 # 测试服务
│
├── frontend/                   # 前端项目
│   └── src/
│       ├── api/                # API 请求
│       ├── components/         # 公共组件
│       ├── views/              # 页面视图
│       ├── router/             # 路由配置
│       ├── stores/             # Pinia 状态管理
│       └── utils/              # 工具函数
│
├── installer/                  # 安装程序
│   └── setup.iss              # Inno Setup 脚本
│
├── scripts/                    # 构建脚本
│   └── build-installer.bat    # 安装包构建脚本
│
├── resources/                  # 资源文件
│   └── nginx/                 # Nginx 发行版
│
├── database/                   # 数据库脚本
│   ├── migrations/
│   └── seeds/
│
└── docs/                       # 文档
    ├── database-design.md      # 数据库设计文档
    ├── license-generation-guide.md # 许可证生成指南
    └── user-manual/            # 用户手册
```

## 运行方式

### 开发环境

**后端:**
```bash
cd backend
dotnet run --project src/DataForgeStudio.Api
```

**前端:**
```bash
cd frontend
npm run dev
```

### 生产环境

运行安装程序 `DataForgeStudio-Setup.exe` 进行安装。

## 服务名称

| 服务 | 名称 | 说明 |
|------|------|------|
| 后端服务 | DFAppService | ASP.NET Core API 服务 |
| 前端服务 | DFWebService | IIS 或 Nginx 托管 |

## 环境变量 (生产环境)

```bash
DFS_JWT_SECRET="your-64-character-random-secret-key-here"
DFS_ENCRYPTION_AES_KEY="your-32-character-aes-key-here"
DFS_ENCRYPTION_AES_IV="your-16-character-aes-iv"
DFS_LICENSE_AES_KEY="your-32-character-license-aes-key"
DFS_LICENSE_AES_IV="your-16-character-license-aes-iv"
```

## 部署要求

### 服务器
- Windows Server 2012 R2+
- SQL Server 2005+
- .NET 8.0 Runtime

### IIS 配置 (可选)
- 安装 ASP.NET Core Hosting Bundle
- 配置应用程序池 (无托管代码)

### Nginx (可选)
- 包含在安装包中
- 自动配置

## 常用命令

### 构建安装包
```bash
cd scripts
build-installer.bat
```

### 生成许可证
```bash
cd backend/tools/LicenseGenerator
dotnet run
```

### 安装测试服务
```bash
cd backend/tools/TestService
install-service.bat  # 以管理员身份运行
```

## 后续计划

### 质量保障
- [ ] 后端单元测试
- [ ] API 集成测试
- [ ] 前端组件测试

### 用户体验
- [ ] 完善用户手册
- [ ] API 文档完善

## 已知问题

无关键问题。系统已可投入生产使用。
