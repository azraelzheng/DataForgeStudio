# DataForgeStudio V4 API 文档

## 基础信息

- **Base URL**: `http://localhost:5000/api` (开发环境)
- **认证方式**: JWT Bearer Token
- **内容类型**: `application/json`
- **Swagger UI**: `https://localhost:5000/swagger` (仅开发环境)

## 认证流程

### 1. 获取 Token

**端点**: `POST /api/auth/login`

**请求体**:
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**成功响应** (200 OK):
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiJ9.eyJ1c2VySWQiOiIxIiwidXNlcm5hbWUiOiJhZG1pbiIsImp0aSI6Imd1aWQiLCJpYXQiOjE3Mzg0NjQwMDAsImV4cCI6MTczODU1MDQwMH0.xyz...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "userInfo": {
      "userId": 1,
      "username": "admin",
      "realName": "系统管理员",
      "email": "admin@example.com",
      "roles": ["管理员"],
      "permissions": ["user:view", "user:create", "report:view", "report:create"]
    }
  },
  "errorCode": null,
  "timestamp": 1738464000
}
```

**失败响应** (200 OK, success=false):
```json
{
  "success": false,
  "message": "用户名或密码错误",
  "data": null,
  "errorCode": "LOGIN_FAILED",
  "timestamp": 1738464000
}
```

### 2. 使用 Token

在请求头中添加:
```
Authorization: Bearer <your_token_here>
```

---

## 认证端点

### POST /api/auth/login

用户登录获取 Token。

**请求体**:
```json
{
  "username": "admin",
  "password": "admin123"
}
```

### GET /api/auth/current-user

获取当前登录用户信息。

**请求头**:
```
Authorization: Bearer <token>
```

**响应**:
```json
{
  "success": true,
  "data": {
    "userId": 1,
    "username": "admin",
    "realName": "系统管理员",
    "email": "admin@example.com",
    "roles": ["管理员"],
    "permissions": ["permission:code"]
  }
}
```

### POST /api/auth/change-password

修改当前用户密码。

**请求头**:
```
Authorization: Bearer <token>
```

**请求体**:
```json
{
  "oldPassword": "oldpassword",
  "newPassword": "newpassword",
  "confirmPassword": "newpassword"
}
```

---

## 报表端点

### GET /api/reports

获取报表列表（分页）。

**查询参数**:
- `page` (int, 可选): 页码，默认 1
- `pageSize` (int, 可选): 每页大小，默认 20
- `reportName` (string, 可选): 报表名称筛选
- `category` (string, 可选): 分类筛选

**请求头**:
```
Authorization: Bearer <token>
```

**响应**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "reportId": 1,
        "reportName": "用户列表",
        "category": "系统管理",
        "createdTime": "2026-01-01T00:00:00Z"
      }
    ],
    "totalCount": 100,
    "pageIndex": 1,
    "pageSize": 20
  }
}
```

### GET /api/reports/{id}

获取报表详情。

### POST /api/reports

创建新报表。

**请求体**:
```json
{
  "reportName": "用户报表",
  "category": "系统管理",
  "dataSourceId": 1,
  "sqlQuery": "SELECT * FROM Users WHERE IsActive = 1",
  "description": "查询所有激活用户"
}
```

### POST /api/reports/test-query

测试 SQL 查询（用于报表设计器）。

**限制**:
- 必须以 `SELECT` 开头
- 不允许 `DROP`, `DELETE`, `INSERT`, `UPDATE` 等危险关键字
- 不允许注释字符 (`--` 和 `/* */`)
- 不允许分号（多语句注入）
- 不允许 `UNION` 注入

**请求体**:
```json
{
  "dataSourceId": 1,
  "sql": "SELECT * FROM Users WHERE IsActive = 1",
  "parameters": []
}
```

**响应**:
```json
{
  "success": true,
  "data": [
    {
      "UserId": 1,
      "Username": "admin",
      "RealName": "管理员"
    }
  ]
}
```

### POST /api/reports/{id}/execute

执行报表查询。

**请求体**:
```json
{
  "parameterValues": {
    "StartDate": "2026-01-01",
    "EndDate": "2026-12-31"
  }
}
```

### POST /api/reports/{id}/export

导出报表为 Excel 文件。

**返回**: Excel 文件流 (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`)

---

## 用户管理端点

### GET /api/users

获取用户列表。

### POST /api/users

创建新用户。

**请求体**:
```json
{
  "username": "newuser",
  "password": "password123",
  "realName": "新用户",
  "email": "user@example.com",
  "roleId": 2
}
```

**注意**: root 用户 (IsSystem=1) 不可见、不可删除。

### PUT /api/users/{id}

更新用户信息。

### DELETE /api/users/{id}

删除用户。

### POST /api/users/{id}/reset-password

重置用户密码为随机密码。

---

## 角色管理端点

### GET /api/roles

获取角色列表。

### POST /api/roles

创建新角色。

### PUT /api/roles/{id}

更新角色。

### DELETE /api/roles/{id}

删除角色。

---

## 数据源管理端点

### GET /api/datasources

获取数据源列表。

### POST /api/datasources

创建新数据源。

**请求体**:
```json
{
  "sourceName": "生产数据库",
  "dbType": "SqlServer",
  "server": "localhost",
  "port": 1433,
  "database": "MyDatabase",
  "username": "sa",
  "password": "password"  // 将被 AES 加密存储
}
```

### POST /api/datasources/{id}/test

测试数据源连接。

---

## 许可证管理端点

### GET /api/license

获取当前许可证信息。

**响应**:
```json
{
  "success": true,
  "data": {
    "licenseType": "专业版",
    "expirationDate": "2027-01-01T00:00:00Z",
    "maxUsers": 100,
    "features": ["报表导出", "PDF导出", "多数据源"]
  }
}
```

### POST /api/license/activate

激活许可证。

**请求体**:
```json
{
  "licenseKey": "xxxxx-xxxxx-xxxxx-xxxxx-xxxxx"
}
```

---

## 系统管理端点

### GET /api/system/logs

获取操作日志。

### GET /api/system/backups

获取备份列表。

### POST /api/system/backups

创建数据库备份。

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
| SQL_VALIDATION_FAILED | SQL 语句不安全 |

---

## 通用响应格式

所有 API 响应遵循以下格式:

```json
{
  "success": true/false,
  "message": "操作结果描述",
  "data": { ... },  // 业务数据
  "errorCode": "ERROR_CODE",  // 仅失败时存在
  "timestamp": 1738464000
}
```

---

## 开发环境

- **本地运行**: `dotnet run --project backend/src/DataForgeStudio.Api`
- **Swagger UI**: 访问 `https://localhost:5000/swagger`
- **健康检查**: `GET /health`
