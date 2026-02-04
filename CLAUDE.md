# DataForgeStudio V4 项目文档

## 项目概述

**项目名称**: DataForgeStudio V4
**项目类型**: Web 报表管理系统
**开发语言**: ASP.NET Core 8.0 + Vue 3
**数据库**: SQL Server (兼容 2005+)
**许可证**: 商业应用，所有组件免费/开源

## 基本规则
- 回复用中文
- 代码注释用中文

## 项目结构

```
DataForgeStudio_V4/
├── backend/                          # 后端项目
│   ├── src/
│   │   ├── DataForgeStudio.Api/     # Web API 项目
│   │   ├── DataForgeStudio.Core/    # 核心业务逻辑
│   │   ├── DataForgeStudio.Domain/  # 领域模型
│   │   ├── DataForgeStudio.Data/    # 数据访问层 (EF Core)
│   │   └── DataForgeStudio.Shared/  # 共享工具类
│   ├── DataForgeStudio.sln          # 解决方案文件
│   └── tests/                        # 测试项目
│
├── frontend/                         # 前端项目（待创建）
│   ├── src/
│   │   ├── api/                     # API 请求
│   │   ├── components/              # 公共组件
│   │   ├── views/                   # 页面视图
│   │   │   ├── home/               # 首页
│   │   │   ├── report/             # 报表模块
│   │   │   ├── license/            # 许可管理
│   │   │   └── system/             # 系统管理
│   │   ├── router/                 # 路由配置
│   │   ├── stores/                 # Pinia 状态管理
│   │   └── utils/                  # 工具函数
│   └── package.json
│
├── database/                         # 数据库脚本
│   ├── migrations/                  # 迁移脚本
│   ├── seeds/                       # 初始数据
│   └── procedures/                  # 存储过程
│
├── task_plan.md                      # 开发规划
├── findings.md                       # 研究发现
├── progress.md                       # 进度日志
└── database-design.md                # 数据库设计文档
```

## 核心功能模块

### 1. 首页 (HomePage)
- 系统概览
- 快捷入口
- 统计信息展示

### 2. 报表设计 (ReportDesign)
- SQL 查询编辑器
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
- RSA 加密验证
- 功能模块控制
- 用户数限制

### 5. 系统管理 (SystemManagement)

#### 5.1 数据源管理
- 多数据库支持（SQL Server、MySQL、Oracle 等）
- 连接配置（AES 加密密码）
- 连接测试

#### 5.2 用户管理
- 用户增删改查
- 密码管理
- 用户-角色关联
- **Root 用户**: IsSystem=1，前端不显示，不可删除

#### 5.3 权限组管理
- 角色定义
- 权限分配（RBAC）
- 数据权限控制

#### 5.4 日志管理
- 操作日志记录
- 日志查询
- 日志导出

#### 5.5 备份管理
- 手动备份
- 自动备份（Windows 任务计划）
- 备份恢复
- 备份文件管理

## 技术架构

### 后端技术栈

| 组件 | 技术 | 版本 | 许可证 |
|------|------|------|--------|
| 框架 | ASP.NET Core | 8.0 | MIT |
| ORM | Entity Framework Core | 8.0.11 | MIT |
| 认证 | JWT Bearer | 8.0.11 | MIT |
| Excel 导出 | ClosedXML | 0.104.2 | MIT |
| CSV 导出 | CsvHelper | 30.0.1 | MIT |
| 密码哈希 | BCrypt.Net-Next | 4.0.3 | MIT |

### 前端技术栈

| 组件 | 技术 | 版本 | 许可证 |
|------|------|------|--------|
| 框架 | Vue | 3.x | MIT |
| UI 库 | Element Plus | Latest | MIT |
| 图表 | Apache ECharts | Latest | Apache 2.0 |
| 代码编辑 | CodeMirror | 6.x | MIT |
| 构建 | Vite | Latest | MIT |

## 数据库设计要点

### 核心表（14个）

| 表名 | 说明 | 关键字段 |
|------|------|----------|
| Users | 用户表 | IsSystem (系统用户标记) |
| Roles | 角色表 | IsSystem, RoleCode |
| UserRoles | 用户角色关联 | UserId, RoleId |
| Permissions | 权限表 | PermissionCode, Module, Action |
| RolePermissions | 角色权限关联 | RoleId, PermissionId |
| DataSources | 数据源表 | Password (AES 加密) |
| Reports | 报表定义 | SqlStatement, DataSourceId |
| ReportFields | 报表字段配置 | FieldName, DisplayName |
| ReportParameters | 报表参数配置 | ParameterName, DataType |
| OperationLogs | 操作日志 | Module, Action, UserId |
| LoginLogs | 登录日志 | UserId, LoginStatus |
| SystemConfigs | 系统配置 | ConfigKey, ConfigValue |
| BackupRecords | 备份记录 | BackupPath, BackupTime |
| Licenses | 许可证表 | LicenseKey (RSA 加密) |

### Root 用户处理

```csharp
// 数据库约束
[IsSystem] = 0 OR [Username] = 'root'

// API 查询
WHERE IsSystem = 0  // 不返回 root 用户

// 创建用户
IsSystem = false  // 强制为普通用户
```

### SQL Server 2005 兼容性

**避免使用**:
- `SEQUENCE` → 使用 `IDENTITY`
- `OFFSET/FETCH` → 使用 `ROW_NUMBER()`
- `IIF` → 使用 `CASE WHEN`
- `CONCAT` → 使用 `+` 连接
- `STRING_SPLIT` → 自定义函数

## 安全设计

### 密码存储
- 使用 BCrypt 哈希（工作因子 12）
- 不存储明文密码
- 密码复杂度验证

### 数据源安全
- 密码使用 AES 加密存储
- 连接字符串加密
- 不在前端暴露密码

### SQL 注入防护
- 使用参数化查询
- 表名/字段名白名单验证
- 只允许 SELECT 语句

### 认证授权
- JWT Token 认证
- 基于 RBAC 的权限控制
- Token 有效期 24 小时可配置

## API 规范

### 请求格式
```json
{
  "success": true,
  "message": "操作成功",
  "data": {...},
  "errorCode": null,
  "timestamp": 1738464000
}
```

### 认证方式
```
Authorization: Bearer <JWT_TOKEN>
```

### 分页参数
```json
{
  "pageIndex": 1,
  "pageSize": 20,
  "sortField": "CreatedTime",
  "sortOrder": "desc",
  "keyword": "搜索关键词"
}
```

## 开发工作流

### 1. 添加新功能
1. 在 `task_plan.md` 更新任务状态
2. 设计数据库表（如需要）
3. 创建 Domain 实体类
4. 创建 Repository 接口和实现
5. 创建 Service 服务
6. 创建 API 控制器
7. 更新 `progress.md`

### 2. 测试 API
1. 运行后端: `dotnet run`
2. 访问 Swagger: `https://localhost:5000/swagger`
3. 使用 Thunder Client 测试

### 3. 数据库迁移
1. 更新 `DataForgeStudioDbContext`
2. 创建迁移脚本
3. 在 SSMS 中执行脚本

## 关键文件位置

### 配置文件
- `backend/src/DataForgeStudio.Api/appsettings.json` - API 配置
- `backend/src/DataForgeStudio.Api/Program.cs` - 服务注册

### 实体类
- `backend/src/DataForgeStudio.Domain/Entities/` - 所有实体类

### 仓储接口
- `backend/src/DataForgeStudio.Domain/Interfaces/IRepository.cs`
- `backend/src/DataForgeStudio.Domain/Interfaces/IUserRepository.cs`

### 服务
- `backend/src/DataForgeStudio.Core/Interfaces/IAuthenticationService.cs`
- `backend/src/DataForgeStudio.Core/Services/AuthenticationService.cs`

### 工具类
- `backend/src/DataForgeStudio.Shared/Utils/EncryptionHelper.cs` - 加密工具
- `backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs` - API 响应
- `backend/src/DataForgeStudio.Shared/Exceptions/AppException.cs` - 自定义异常

## 常用命令

### 后端开发
```bash
# 进入后端目录
cd H:\开发项目\DataForgeStudio_V4\backend

# 运行 API
dotnet run --project src/DataForgeStudio.Api

# 构建项目
dotnet build

# 清理和重建
dotnet clean
dotnet restore
dotnet build
```

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

## 部署配置

### 生产环境要求
- **服务器**: Windows Server
- **数据库**: SQL Server 2005+
- **运行时**: .NET 8.0 Runtime (桌面应用) 或 ASP.NET Core Hosting Bundle (Web API)

### 发布命令
```bash
# 发布桌面应用（自包含）
dotnet publish -c Release -r win-x64 --self-contained -o publish/desktop

# 发布 Web API（依赖框架）
dotnet publish -c Release -o publish/api
```

### IIS 部署
1. 安装 ASP.NET Core Hosting Bundle
2. 在 IIS 中创建网站
3. 配置应用程序池（无托管代码）
4. 配置 web.config
5. 配置防火墙和端口

## 特殊注意事项

### 不使用 Docker
所有部署直接在 Windows 上进行：
- Web API 部署到 IIS
- 桌面应用直接运行 .exe
- 使用 Windows 任务计划替代 Docker 容器

### Root 用户
- **用户名**: root
- **默认密码**: admin123（首次登录需修改）
- **IsSystem**: 1
- **前端**: 所有用户列表查询自动过滤
- **后端**: 不可删除、不可禁用

### SQL Server 2005 兼容性
数据库设计中已考虑兼容性，所有 SQL 语法均使用 SQL Server 2005 支持的语法。

### 许可证管理
- 使用 RSA 2048 位加密
- 机器码绑定（CPU ID + 硬盘序列号）
- 功能模块控制
- 用户数限制

## 参考文档

- [数据库设计](database-design.md)
- [开发规划](task_plan.md)
- [进度日志](progress.md)
- [研究发现](findings.md)
