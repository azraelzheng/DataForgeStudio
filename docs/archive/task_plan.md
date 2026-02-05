# DataForgeStudio V4 开发规划

## 项目概述
开发一个基于 Web 的报表管理系统，支持动态 SQL 报表设计、查询和权限管理。

## 技术栈（完全免费/开源，可商业使用）

### 后端技术栈
| 组件 | 选择 | 说明 | 许可证 |
|------|------|------|--------|
| **框架** | ASP.NET Core 8.0 | 跨平台，高性能，现代 .NET | MIT |
| **ORM** | Entity Framework Core | 完整 ORM 支持，迁移友好 | MIT |
| **API** | ASP.NET Core Web API | RESTful API | MIT |
| **认证** | ASP.NET Core Identity | 用户认证与授权 | MIT |
| **JWT** | System.IdentityModel.Tokens.Jwt | Token 认证 | MIT |
| **CSV** | CsvHelper | CSV 导入导出 | MIT |
| **配置** | Microsoft.Extensions.* | 配置管理 | MIT |

### 前端技术栈
| 组件 | 选择 | 说明 | 许可证 |
|------|------|------|--------|
| **框架** | Vue 3 | 渐进式框架，中文友好 | MIT |
| **UI 库** | Element Plus | 完整组件库，免费商用 | MIT |
| **图表** | ECharts | 百度开源，功能强大 | Apache 2.0 |
| **代码编辑** | CodeMirror 6 | SQL 编辑器 | MIT |
| **表格** | Element Plus Table + 虚拟滚动 | 大数据表格 | MIT |
| **HTTP** | Axios | HTTP 请求 | MIT |
| **路由** | Vue Router | 官方路由 | MIT |
| **状态管理** | Pinia | 官方状态管理 | MIT |
| **构建工具** | Vite | 快速构建 | MIT |

### 报表导出技术栈
| 功能 | 技术 | 许可证 |
|------|------|--------|
| **Excel 导出** | EPPlus (v5+) | Polyform Noncommercial v1.0.0 ⚠️ |
| **Excel 导出** | ClosedXML | MIT ✅ |
| **Excel 导出** | NPOI | Apache 2.0 ✅ |
| **PDF 导出** | jsPDF + html2canvas | MIT |
| **CSV 导出** | 前端原生 + CsvHelper | MIT |

**推荐**: 使用 **ClosedXML** (Excel) 或 **NPOI**，商业友好

### 数据库相关
| 组件 | 选择 | 说明 |
|------|------|------|
| **数据库** | SQL Server 2005+ | 兼容老版本 |
| **连接** | ADO.NET | 原生高性能 |
| **备份** | SQL Server 命令 + Windows 任务计划 | 无额外成本 |

### 开发工具（免费）
| 工具 | 用途 |
|------|------|
| **Visual Studio Community** | 后端开发（免费） |
| **Visual Studio Code** | 前端开发（免费） |
| **SQL Server Management Studio** | 数据库管理（免费） |
| **Git** | 版本控制（免费） |
| **Postman** | API 测试（免费） |

### 许可证加密（免费方案）
- **RSA 加密**: .NET 内置 System.Security.Cryptography
- **硬件绑定**: CPU ID + 硬盘序列号
- **签名验证**: RSA 数字签名

## 功能模块

### 1. 首页 (HomePage)
- 系统概览
- 快捷入口
- 统计信息展示

### 2. 报表设计 (ReportDesign)
- SQL 查询编辑器
- 数据表格配置
- 字段配置（显示、隐藏、排序、格式化）
- 查询条件配置（参数绑定）
- 报表预览

### 3. 报表查询 (ReportQuery)
- 报表列表展示
- 参数输入界面
- 结果展示（表格、图表）
- 导出功能（Excel、PDF、CSV）

### 4. 许可管理 (LicenseManagement)
- 许可证激活
- 许可证验证
- 功能模块控制
- 用户数限制

### 5. 系统管理 (SystemManagement)

#### 5.1 数据源管理
- 数据源连接配置
- 连接测试
- 多数据源支持

#### 5.2 权限组管理
- 角色定义
- 权限分配
- 数据权限控制

#### 5.3 用户管理
- 用户增删改查
- 密码管理
- 用户-角色关联

#### 5.4 日志管理
- 操作日志记录
- 日志查询
- 日志导出

#### 5.5 备份管理
- 手动备份
- 自动备份（定时任务）
- 备份恢复
- 备份文件管理

## 开发阶段

### 阶段 1: 项目初始化与基础架构 - in_progress
- [x] 创建项目结构 ✅
- [x] 搭建开发框架 ✅
- [x] 配置数据库连接 ✅
- [x] 设计数据库表结构 ✅
- [x] 实现用户认证基础 ✅

### 阶段 2: 核心报表功能 - pending
- [ ] 报表设计器
- [ ] SQL 执行引擎
- [ ] 字段配置功能
- [ ] 查询条件配置
- [ ] 报表保存/加载

### 阶段 3: 报表查询与展示 - pending
- [ ] 报表列表
- [ ] 参数输入界面
- [ ] 结果表格展示
- [ ] 图表展示
- [ ] 导出功能

### 阶段 4: 系统管理功能 - pending
- [ ] 数据源管理
- [ ] 用户管理
- [ ] 权限组管理
- [ ] 日志管理
- [ ] 备份管理

### 阶段 5: 许可管理 - pending
- [ ] 许可证生成
- [ ] 许可证验证
- [ ] 功能控制
- [ ] 用户数限制

### 阶段 6: 首页与优化 - pending
- [ ] 首页开发
- [ ] UI/UX 优化
- [ ] 性能优化
- [ ] 响应式适配

### 阶段 7: 测试与部署 - pending
- [ ] 单元测试
- [ ] 集成测试
- [ ] 部署文档
- [ ] 用户手册

## 数据库设计要点

### ✅ 数据库设计已完成

详细设计文档见: `database-design.md`

### 核心表 (14个表)
- `Users` - 用户表（含 IsSystem 字段标记系统用户）
- `Roles` - 角色表
- `UserRoles` - 用户角色关联表
- `DataSources` - 数据源表
- `Reports` - 报表定义表
- `ReportFields` - 报表字段配置表
- `ReportParameters` - 报表参数表
- `Permissions` - 权限表
- `RolePermissions` - 角色权限关联表
- `OperationLogs` - 操作日志表
- `SystemConfigs` - 系统配置表
- `BackupRecords` - 备份记录表
- `Licenses` - 许可证表
- `LoginLogs` - 登录日志表

### 视图 (2个)
- `v_UserPermissions` - 用户权限视图
- `v_ReportDetails` - 报表完整信息视图

### 存储过程 (3个)
- `sp_UserLogin` - 用户登录验证
- `sp_GetUserPermissions` - 获取用户权限
- `sp_CreateBackup` - 创建备份

### Root 用户处理方案

| 层面 | 处理方式 |
|------|----------|
| **数据库** | `IsSystem = 1` 标记系统用户，约束确保只有 root 可设置为系统用户 |
| **后端 API** | 查询时 `WHERE IsSystem = 0`，创建用户时强制 `IsSystem = 0` |
| **前端** | 用户列表不显示 IsSystem=1 的用户 |

### 内置数据

**内置用户**:
- root (超级管理员) - IsSystem=1

**内置角色**:
- 超级管理员 (SUPER_ADMIN)
- 管理员 (ADMIN)
- 普通用户 (USER)
- 访客 (GUEST)

**内置权限**: 约 40+ 个权限，覆盖所有功能模块

## 技术决策总结

### ✅ 已确定的技术栈

#### 后端 - ASP.NET Core 8.0
- **选择理由**:
  - 完全免费且开源 (MIT 许可)
  - 跨平台支持（虽然主要部署 Windows）
  - 高性能，比 .NET Framework 快很多
  - 现代化的开发体验
  - 微软官方长期支持

#### 前端 - Vue 3 + Element Plus
- **选择理由**:
  - Vue 3: MIT 许可，完全免费
  - Element Plus: MIT 许可，组件丰富
  - 中文文档完善，社区活跃
  - 学习曲线平缓
  - 与后端 .NET 技术栈配合良好

#### 数据库 ORM - Entity Framework Core
- **选择理由**:
  - MIT 许可，完全免费
  - 代码迁移功能强大
  - LINQ 查询，类型安全
  - 支持多种数据库

#### 图表库 - Apache ECharts
- **选择理由**:
  - Apache 2.0 许可，商业友好
  - 功能强大，图表类型丰富
  - 性能优秀，支持大数据
  - 中文文档完善

#### Excel 导出 - ClosedXML
- **选择理由**:
  - MIT 许可，完全免费商用
  - API 简单易用
  - 不依赖 Excel 安装
  - 活跃维护

#### SQL 编辑器 - CodeMirror 6
- **选择理由**:
  - MIT 许可
  - 支持语法高亮
  - 可扩展性强

### 项目结构

```
DataForgeStudio_V4/
├── backend/                          # 后端项目
│   ├── src/
│   │   ├── DataForgeStudio.Api/     # Web API 项目
│   │   ├── DataForgeStudio.Core/    # 核心业务逻辑
│   │   ├── DataForgeStudio.Domain/  # 领域模型
│   │   ├── DataForgeStudio.Data/    # 数据访问层 (EF Core)
│   │   └── DataForgeStudio.Shared/  # 共享工具类
│   └── tests/
│
├── frontend/                         # 前端项目
│   ├── src/
│   │   ├── api/                     # API 请求
│   │   ├── assets/                  # 静态资源
│   │   ├── components/              # 公共组件
│   │   ├── views/                   # 页面视图
│   │   │   ├── home/               # 首页
│   │   │   ├── report/             # 报表模块
│   │   │   ├── license/            # 许可管理
│   │   │   └── system/             # 系统管理
│   │   ├── router/                 # 路由配置
│   │   ├── stores/                 # Pinia 状态管理
│   │   └── utils/                  # 工具函数
│   └── public/
│
└── database/                         # 数据库脚本
    ├── migrations/                  # 迁移脚本
    ├── seeds/                       # 初始数据
    └── procedures/                  # 存储过程
```

## 遇到的错误
| 错误 | 尝试 | 解决方案 |
|------|------|----------|
| - | - | - |

## 技术约束
- 需兼容 SQL Server 2005+
- 需支持主流浏览器（Chrome, Firefox, Edge, Safari）
- 需支持 Windows 10+ 客户端
