# 数据库管理指南

## 重要说明

**DataForgeStudio 使用 Entity Framework Core Code First 方式管理数据库。**

- 应用首次启动时会**自动创建**数据库和种子数据
- SQL 脚本仅用于**手动初始化**或**参考**
- 数据库结构由代码定义：`backend/src/DataForgeStudio.Domain/Entities/`

## 自动初始化（推荐）

应用程序启动时会自动执行以下操作：

1. **创建数据库** - 如果不存在
2. **创建表结构** - 根据实体定义
3. **创建种子数据**：
   - root 用户（系统管理员）
   - 默认角色（超级管理员、管理员、开发者、查看者）
   - 系统权限

### 默认账户

| 用户名 | 密码 | 角色 | 说明 |
|--------|------|------|------|
| root | Admin@123 | 超级管理员 | 系统内置用户，UI不可见 |

## 手动初始化（可选）

如果需要手动创建数据库：

### 方式一：使用 SQL 脚本

```bash
# 使用 sqlcmd
sqlcmd -S localhost -E -i database/scripts/01_init_database.sql

# 或在 SSMS 中执行
# 打开 database/scripts/01_init_database.sql 并执行
```

### 方式二：使用 EF Core CLI

```bash
cd backend/src/DataForgeStudio.Api

# 生成迁移
dotnet ef migrations add InitialCreate

# 应用迁移
dotnet ef database update
```

## 数据库结构

### 核心表

| 表名 | 说明 |
|------|------|
| Users | 用户表 |
| Roles | 角色表 |
| UserRoles | 用户角色关联表 |
| Permissions | 权限表 |
| RolePermissions | 角色权限关联表 |

### 业务表

| 表名 | 说明 |
|------|------|
| DataSources | 数据源配置 |
| Reports | 报表定义 |
| ReportFields | 报表字段配置 |
| ReportParameters | 报表参数配置 |

### 系统表

| 表名 | 说明 |
|------|------|
| OperationLogs | 操作日志 |
| LoginLogs | 登录日志 |
| SystemConfigs | 系统配置 |
| BackupRecords | 备份记录 |
| BackupSchedules | 备份计划 |
| Licenses | 许可证 |

## 连接字符串配置

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost;Initial Catalog=DataForgeStudio;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

### 常见连接字符串格式

**Windows 身份验证（推荐）：**
```
Data Source=localhost;Initial Catalog=DataForgeStudio;Integrated Security=True;TrustServerCertificate=True;
```

**SQL Server 身份验证：**
```
Data Source=localhost;Initial Catalog=DataForgeStudio;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

**远程服务器：**
```
Data Source=192.168.1.100,1433;Initial Catalog=DataForgeStudio;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

**SQL Server Express：**
```
Data Source=.\SQLEXPRESS;Initial Catalog=DataForgeStudio;Integrated Security=True;TrustServerCertificate=True;
```

## 故障排除

### 连接失败

1. 确认 SQL Server 服务正在运行
2. 检查防火墙是否允许 1433 端口
3. 确认 TCP/IP 协议已启用（SQL Server Configuration Manager）

### 权限问题

```sql
-- 授予数据库访问权限
USE DataForgeStudio;
CREATE USER [your-username] FOR LOGIN [your-username];
ALTER ROLE db_owner ADD MEMBER [your-username];
```

### 重置数据库

```sql
-- 警告：此操作会删除所有数据！
USE master;
DROP DATABASE IF EXISTS DataForgeStudio;
-- 然后重新启动应用程序，会自动创建新数据库
```
