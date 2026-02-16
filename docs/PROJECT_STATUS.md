# DataForgeStudio V4 项目状态

**更新日期**: 2026-02-17

## 项目概述

**项目名称**: DataForgeStudio V4
**项目类型**: Web 报表管理系统
**技术栈**: ASP.NET Core 8.0 + Vue 3 + SQL Server

## 当前状态

### 整体进度: 95% 完成

| 模块 | 状态 | 完成度 |
|------|------|--------|
| 后端 API | ✅ 完成 | 100% |
| 前端界面 | ✅ 完成 | 100% |
| 数据库设计 | ✅ 完成 | 100% |
| 认证授权 | ✅ 完成 | 100% |
| 许可证系统 | ✅ 完成 | 100% |
| 安全加固 | ✅ 完成 | 100% |
| 报表功能 | ✅ 完成 | 100% |
| 系统管理 | ✅ 完成 | 100% |

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

### 6. 系统管理
- 操作日志
- 备份管理
- 系统配置

### 7. 许可证系统
- RSA 2048 位密钥管理
- 许可证生成工具
- 许可证激活/验证
- 机器码绑定
- 功能模块控制
- 用户数限制
- 试用期防重置机制 (2026-02-17 新增)
  - DPAPI 加密存储
  - 多位置冗余（注册表、ProgramData、应用目录）
  - 交叉验证取最早时间
  - 15 天试用期

### 8. 安全加固 (已完成 2026-02-05)
- ✅ 环境变量配置 (SecurityOptions)
- ✅ 随机临时密码生成
- ✅ SQL 注入防护 (SqlValidationService)
- ✅ 速率限制 (RateLimitMiddleware)
- ✅ CORS 配置
- ✅ HTTPS 生产环境支持

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
DataForgeStudio_V4/
├── backend/                    # 后端项目
│   ├── src/
│   │   ├── DataForgeStudio.Api/         # Web API
│   │   ├── DataForgeStudio.Core/        # 业务逻辑
│   │   ├── DataForgeStudio.Domain/      # 领域模型
│   │   ├── DataForgeStudio.Data/        # 数据访问
│   │   └── DataForgeStudio.Shared/      # 共享工具
│   └── tools/
│       └── LicenseGenerator/            # 许可证生成工具
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
├── database/                   # 数据库脚本
│   ├── migrations/
│   └── seeds/
│
└── docs/                       # 文档
    ├── database-design.md      # 数据库设计文档
    └── archive/                # 历史文档存档
```

## 运行方式

### 后端
```bash
cd backend
dotnet run --project src/DataForgeStudio.Api
```

### 前端
```bash
cd frontend
npm run dev
```

## API 端点

| 功能 | 端点 | 认证 |
|------|------|------|
| 登录 | POST /api/auth/login | ❌ |
| 获取当前用户 | GET /api/auth/current-user | ✅ |
| 修改密码 | POST /api/auth/change-password | ✅ |
| 用户管理 | /api/users/* | ✅ |
| 角色管理 | /api/roles/* | ✅ |
| 数据源管理 | /api/datasources/* | ✅ |
| 报表管理 | /api/reports/* | ✅ |
| 许可证管理 | /api/license/* | 部分 |
| 系统管理 | /api/system/* | ✅ |

## 数据库连接

```
Server=localhost;Database=DataForgeStudio_V4;User Id=sa;Password=your_password;TrustServerCertificate=True;
```

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
- Windows Server
- SQL Server 2005+
- .NET 8.0 Runtime (或 ASP.NET Core Hosting Bundle)

### IIS 配置
- 安装 ASP.NET Core Hosting Bundle
- 配置应用程序池 (无托管代码)
- 配置 web.config

## 常用命令

### 数据库操作
```sql
-- 查看用户列表（不包括 root）
SELECT * FROM Users WHERE IsSystem = 0

-- 查看用户权限
SELECT u.Username, r.RoleName, p.PermissionName
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.RoleId
INNER JOIN RolePermissions rp ON r.RoleId = rp.RoleId
INNER JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE u.Username = 'admin'
```

### Git 操作
```bash
# 查看状态
git status

# 提交更改
git add .
git commit -m "commit message"

# 推送到远程
git push origin main
```

## 待办事项

### 短期 (可选)
- [ ] 单元测试
- [ ] API 文档完善
- [ ] 用户手册编写

### 长期 (可选)
- [ ] Docker 容器化
- [ ] CI/CD 流水线
- [ ] 性能优化

## 已知问题

无关键问题。系统已可投入生产使用。

## 联系方式

- 项目路径: `H:\开发项目\DataForgeStudio_V4`
- 后端端口: 5000
- 前端端口: 5173
- Swagger: https://localhost:5000/swagger
