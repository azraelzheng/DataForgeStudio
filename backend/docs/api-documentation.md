# DataForgeStudio V1.0.0 API 文档

## 基础信息

- **Base URL**: `http://localhost:5000/api` (开发环境) / `http://localhost:8089/api` (生产环境)
- **认证方式**: JWT Bearer Token
- **内容类型**: `application/json`
- **Swagger UI**: `https://localhost:5000/swagger` (仅开发环境)

## 通用响应格式

所有 API 响应遵循以下格式:

```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... },
  "errorCode": null,
  "timestamp": 1738464000
}
```

**失败响应:**
```json
{
  "success": false,
  "message": "错误描述",
  "data": null,
  "errorCode": "ERROR_CODE",
  "timestamp": 1738464000
}
```

---

## 认证端点 (Auth)

### POST /api/auth/login

用户登录获取 Token。

**请求体:**
```json
{
  "username": "root",
  "password": "Admin@123"
}
```

**成功响应:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiJ9...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "userInfo": {
      "userId": 1,
      "username": "root",
      "realName": "系统管理员",
      "roles": ["管理员"],
      "permissions": ["*"]
    }
  }
}
```

### GET /api/auth/current-user

获取当前登录用户信息。

**请求头:** `Authorization: Bearer <token>`

### POST /api/auth/change-password

修改当前用户密码。

**请求体:**
```json
{
  "oldPassword": "OldPassword@123",
  "newPassword": "NewPassword@123",
  "confirmPassword": "NewPassword@123"
}
```

### POST /api/auth/validate-token

验证Token是否有效。

**请求体:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiJ9..."
}
```

### POST /api/auth/force-password-change

强制修改用户密码（管理员功能）。

---

## 报表端点 (Reports)

### GET /api/reports

获取报表列表（分页）。

**查询参数:**
- `page` (int): 页码，默认1
- `pageSize` (int): 每页大小，默认20
- `reportName` (string): 报表名称筛选

### GET /api/reports/{id}

获取报表详情。

### POST /api/reports

创建新报表。

**请求体:**
```json
{
  "reportName": "用户报表",
  "dataSourceId": 1,
  "sqlQuery": "SELECT * FROM Users WHERE CreatedTime >= @StartDate",
  "description": "查询用户列表",
  "parameters": [
    {
      "name": "StartDate",
      "dataType": "date",
      "defaultValue": "2026-01-01",
      "isRequired": true
    }
  ]
}
```

### PUT /api/reports/{id}

更新报表。

### DELETE /api/reports/{id}

删除报表。

### POST /api/reports/test-query

测试 SQL 查询。

**安全限制:**
- 仅允许 SELECT 语句
- 禁止 DROP, DELETE, INSERT, UPDATE, EXEC
- 禁止注释字符和多语句

**请求体:**
```json
{
  "dataSourceId": 1,
  "sql": "SELECT TOP 10 * FROM Users",
  "parameters": []
}
```

### POST /api/reports/query-schema

查询数据表结构。

**请求体:**
```json
{
  "dataSourceId": 1,
  "tableName": "Users"
}
```

### POST /api/reports/{id}/execute

执行报表查询。

**请求体:**
```json
{
  "parameterValues": {
    "StartDate": "2026-01-01"
  }
}
```

### POST /api/reports/{id}/export

导出报表为文件。

**请求体:**
```json
{
  "format": "excel",
  "parameterValues": {}
}
```

**返回:** 文件流

### GET /api/reports/{id}/statistics

获取报表执行统计。

### POST /api/reports/{id}/copy

复制报表。

### GET /api/reports/export-config

获取导出配置。

### POST /api/reports/{id}/toggle

切换报表启用/禁用状态。

---

## 数据源端点 (DataSources)

### GET /api/datasources

获取数据源列表（分页）。

**查询参数:**
- `page`, `pageSize`: 分页参数
- `sourceName`: 名称筛选

### GET /api/datasources/active

获取所有激活的数据源（下拉选择用）。

### GET /api/datasources/{id}

获取数据源详情。

### POST /api/datasources

创建新数据源。

**请求体:**
```json
{
  "sourceName": "生产数据库",
  "dbType": "SqlServer",
  "server": "localhost",
  "port": 1433,
  "database": "MyDatabase",
  "username": "sa",
  "password": "Password@123"
}
```

**支持的数据库类型:**
- `SqlServer` - SQL Server
- `MySql` - MySQL
- `PostgreSQL` - PostgreSQL
- `Oracle` - Oracle
- `Sqlite` - SQLite

### PUT /api/datasources/{id}

更新数据源。

### DELETE /api/datasources/{id}

删除数据源。

### POST /api/datasources/{id}/test

测试数据源连接。

**响应:**
```json
{
  "success": true,
  "message": "连接成功"
}
```

### POST /api/datasources/test

测试连接参数（不保存）。

**请求体:** 同创建数据源

### POST /api/datasources/databases

获取服务器上的数据库列表。

### POST /api/datasources/{id}/toggle-active

切换数据源激活状态。

### GET /api/datasources/{id}/tables

获取数据源的表列表。

---

## 用户端点 (Users)

### GET /api/users

获取用户列表（分页）。

**查询参数:**
- `page`, `pageSize`: 分页参数
- `username`: 用户名筛选
- `status`: 状态筛选

### GET /api/users/{id}

获取用户详情。

### POST /api/users

创建新用户。

**请求体:**
```json
{
  "username": "newuser",
  "password": "Password@123",
  "realName": "新用户",
  "email": "user@example.com",
  "roleIds": [2, 3]
}
```

> **注意:** root 用户不可见、不可删除

### PUT /api/users/{id}

更新用户信息。

### DELETE /api/users/{id}

删除用户。

### POST /api/users/{id}/reset-password

重置用户密码为随机密码。

**响应:**
```json
{
  "success": true,
  "data": {
    "newPassword": "RandomPass@123"
  }
}
```

### POST /api/users/{id}/roles

分配用户角色。

**请求体:**
```json
{
  "roleIds": [2, 3]
}
```

---

## 角色端点 (Roles)

### GET /api/roles

获取角色列表（分页）。

### GET /api/roles/all

获取所有角色（下拉选择用）。

### GET /api/roles/{id}

获取角色详情（包含权限列表）。

### POST /api/roles

创建新角色。

**请求体:**
```json
{
  "roleName": "报表管理员",
  "description": "管理报表和数据源",
  "permissionIds": [1, 2, 3]
}
```

### PUT /api/roles/{id}

更新角色。

### DELETE /api/roles/{id}

删除角色。

### POST /api/roles/{id}/permissions

更新角色权限。

**请求体:**
```json
{
  "permissionIds": [1, 2, 3, 4, 5]
}
```

---

## 许可证端点 (License)

### GET /api/license

获取当前许可证信息。

**响应:**
```json
{
  "success": true,
  "data": {
    "isActivated": true,
    "licenseType": "专业版",
    "expirationDate": "2027-02-27T00:00:00Z",
    "maxUsers": 100,
    "currentUsers": 5,
    "machineCode": "XXXXX-XXXXX-XXXXX",
    "remainingDays": 365,
    "isTrial": false
  }
}
```

### POST /api/license/activate

激活许可证。

**请求体:**
```json
{
  "licenseKey": "BASE64_ENCODED_LICENSE_DATA"
}
```

### POST /api/license/validate

验证许可证状态。

### GET /api/license/stats

获取许可证使用统计。

---

## 系统端点 (System)

### GET /api/system/machine-code

获取机器码（用于申请许可证）。

**响应:**
```json
{
  "success": true,
  "data": {
    "machineCode": "XXXXX-XXXXX-XXXXX-XXXXX"
  }
}
```

### GET /api/system/configs

获取系统配置。

### GET /api/system/directories

获取系统目录信息。

---

### 操作日志

### GET /api/system/logs

获取操作日志（分页）。

**查询参数:**
- `page`, `pageSize`: 分页参数
- `startTime`, `endTime`: 时间范围
- `username`: 用户筛选
- `operation`: 操作类型筛选

### DELETE /api/system/logs

清空所有日志。

### DELETE /api/system/logs/delete-by-query

按条件删除日志。

**请求体:**
```json
{
  "startTime": "2026-01-01",
  "endTime": "2026-02-01"
}
```

### DELETE /api/system/logs/delete-by-ids

按ID批量删除日志。

**请求体:**
```json
{
  "ids": [1, 2, 3]
}
```

### GET /api/system/logs/export

导出日志文件。

### POST /api/system/logs/export-selected

导出选中的日志。

**请求体:**
```json
{
  "ids": [1, 2, 3]
}
```

---

### 备份管理

### GET /api/system/backups

获取备份列表。

### POST /api/system/backup

创建数据库备份。

**响应:**
```json
{
  "success": true,
  "data": {
    "backupId": 1,
    "fileName": "backup_20260227_120000.bak",
    "fileSize": 1024000,
    "createdTime": "2026-02-27T12:00:00Z"
  }
}
```

### DELETE /api/system/backups/{id}

删除备份文件。

### POST /api/system/backups/{id}/restore

从备份恢复数据库。

> **警告:** 此操作会覆盖现有数据！

---

### 备份计划

### GET /api/system/backup-schedules

获取备份计划列表。

### POST /api/system/backup-schedules

创建备份计划。

**请求体:**
```json
{
  "scheduleName": "每日备份",
  "cronExpression": "0 0 2 * * ?",
  "retentionDays": 30,
  "isEnabled": true
}
```

### PUT /api/system/backup-schedules/{id}

更新备份计划。

### DELETE /api/system/backup-schedules/{id}

删除备份计划。

### POST /api/system/backup-schedules/{id}/toggle

启用/禁用备份计划。

---

## 错误码

| 错误码 | 说明 |
|--------|------|
| UNAUTHORIZED | 未登录或 Token 无效 |
| FORBIDDEN | 无权限访问 |
| NOT_FOUND | 资源不存在 |
| VALIDATION_ERROR | 请求参数验证失败 |
| LOGIN_FAILED | 登录失败 |
| LICENSE_EXPIRED | 许可证已过期 |
| LICENSE_NOT_ACTIVATED | 许可证未激活 |
| SQL_VALIDATION_FAILED | SQL 语句不安全 |
| DUPLICATE_NAME | 名称已存在 |
| OPERATION_FAILED | 操作失败 |

---

## 开发环境

- **本地运行**: `dotnet run --project backend/src/DataForgeStudio.Api`
- **Swagger UI**: `https://localhost:5000/swagger`
- **健康检查**: `GET /health`

---

## 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| V1.0.0 | 2026-02-27 | 初始版本 |
