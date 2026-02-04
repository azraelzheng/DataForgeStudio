# 许可证系统测试指南

## 快速测试步骤

### 1. 准备数据库

执行 SQL 脚本创建 Licenses 表：
```bash
sqlcmd -S localhost -d DataForgeStudio_V4 -i database/migrations/add_licenses_table.sql
```

或在 SSMS 中打开并执行 `database/migrations/add_licenses_table.sql`

### 2. 启动 API 服务

```bash
cd H:\开发项目\DataForgeStudio_V4\.worktrees\license-system
dotnet run --project backend/src/DataForgeStudio.Api
```

**预期输出**:
- 数据库连接成功
- RSA 密钥对生成（首次运行）
- AES 密钥验证通过
- API 监听在 https://localhost:5000

**验证密钥生成**:
```bash
ls backend/src/DataForgeStudio.Api/keys/
# 应该看到:
# - public_key.pem
# - private_key.pem
```

### 3. 测试许可证生成

**打开新终端**:
```bash
cd backend/tools/LicenseGenerator
dotnet run
```

**交互式输入示例**:
```
请输入客户名称: 测试客户公司
请选择许可证类型 (1-4): 2  # Standard
过期日期 (直接回车使用默认值):
最大用户数 (20):
最大报表数 (50):
最大数据源数 (5):
请输入启用的功能编号: 1,2,3,5  # 报表设计,报表查询,图表展示,数据源管理
机器码 (留空则不绑定):
确认生成许可证? (Y/N): Y
```

**验证文件生成**:
```bash
ls backend/tools/LicenseGenerator/licenses/
# 应该看到类似: 测试客户公司_Standard_20260204123456.lic
```

### 4. 测试 API 端点

#### 4.1 获取机器码
```bash
# 使用 Swagger UI
浏览器打开: https://localhost:5000/swagger
```

#### 4.2 激活许可证
```bash
# 使用 PowerShell
$license = Get-Content "backend/tools/LicenseGenerator/licenses/测试客户公司_Standard_*.lic" -Raw

Invoke-RestMethod -Uri "https://localhost:5000/api/license/activate" `
  -Method POST `
  -ContentType "application/json" `
  -Body "{`"licenseKey`": `"$license`"}"
```

**预期响应**:
```json
{
  "success": true,
  "message": "许可证激活成功",
  "data": {
    "licenseId": 1,
    "licenseType": "Standard",
    "customerName": "测试客户公司",
    "expiryDate": "2027-02-04T00:00:00Z",
    "maxUsers": 20,
    "maxReports": 50,
    "maxDataSources": 5,
    "features": ["报表设计", "报表查询", "图表展示", "数据源管理"]
  }
}
```

#### 4.3 验证许可证
```bash
Invoke-RestMethod -Uri "https://localhost:5000/api/license/validate" -Method POST
```

**预期响应**:
```json
{
  "success": true,
  "data": {
    "valid": true,
    "message": "许可证有效",
    "licenseInfo": { /* ... */ }
  }
}
```

#### 4.4 获取许可证信息（需要认证）
```bash
# 先登录获取 token
$token = (Invoke-RestMethod -Uri "https://localhost:5000/api/auth/login" `
  -Method POST `
  -ContentType "application/json" `
  -Body "{`"username`":`"admin`",`"password`":`"admin123`"}").data.token

# 获取许可证信息
Invoke-RestMethod -Uri "https://localhost:5000/api/license" `
  -Method GET `
  -Headers @{ Authorization = "Bearer $token" }
```

### 5. 测试试用许可证生成

**首次访问验证端点（未激活许可证）**:
```bash
Invoke-RestMethod -Uri "https://localhost:5000/api/license/validate?forceRefresh=true" `
  -Method POST
```

**预期响应**:
```json
{
  "success": true,
  "data": {
    "valid": true,
    "message": "试用许可证已自动生成",
    "licenseInfo": {
      "licenseType": "Trial",
      "customerName": "试用用户",
      "expiryDate": "<当前日期 + 30 天>",
      "maxUsers": 5,
      "maxReports": 10,
      "maxDataSources": 2,
      "features": ["报表设计", "报表查询", "数据源管理"]
    }
  }
}
```

### 6. 测试错误场景

#### 6.1 无效许可证格式
```bash
Invoke-RestMethod -Uri "https://localhost:5000/api/license/activate" `
  -Method POST `
  -ContentType "application/json" `
  -Body "{`"licenseKey`":`"invalid_license_key`"}"
```

**预期**: `{"success": false, "errorCode": "INVALID_FORMAT"}`

#### 6.2 机器码不匹配
生成一个绑定到不同机器码的许可证，然后尝试激活。

#### 6.3 过期许可证
生成一个过期日期为昨天的许可证，尝试激活。

**预期**: `{"success": false, "errorCode": "EXPIRED"}`

## 验证清单

### 后端验证
- [ ] API 成功启动
- [ ] RSA 密钥对生成
- [ ] 数据库连接成功
- [ ] Licenses 表存在
- [ ] Swagger UI 可访问

### 功能验证
- [ ] 许可证生成工具运行成功
- [ ] .lic 文件生成
- [ ] 许可证激活成功
- [ ] 许可证验证通过
- [ ] 试用许可证自动生成
- [ ] 缓存机制工作

### 安全验证
- [ ] 签名验证有效（篡改检测）
- [ ] 机器码绑定有效
- [ ] 过期检查有效
- [ ] 私钥安全存储

### 错误处理
- [ ] 无效格式错误
- [ ] 机器码不匹配错误
- [ ] 过期许可证错误
- [ ] 重复激活处理

## 故障排查

### 问题: 密钥文件不存在
**错误**: "私钥文件不存在"
**解决**: 先运行 API 项目，让它自动生成密钥

### 问题: 数据库表不存在
**错误**: "Invalid object name 'Licenses'"
**解决**: 执行 `database/migrations/add_licenses_table.sql`

### 问题: 端口被占用
**错误**: "Address already in use"
**解决**: 修改 `Properties/launchSettings.json` 中的端口

### 问题: 机器码不匹配
**原因**: 许可证绑定的机器码与当前服务器不同
**解决**: 重新生成不绑定机器码的许可证，或使用正确的机器码

## 测试数据

### 标准测试许可证
```
客户名称: 测试客户
类型: Standard
过期: +1 年
用户: 20
报表: 50
数据源: 5
功能: 1,2,3,5
机器码: (不绑定)
```

### 试用许可证（自动生成）
```
类型: Trial
过期: +30 天
用户: 5
报表: 10
数据源: 2
功能: 报表设计, 报表查询, 数据源管理
```

## 清理测试数据

```sql
-- 删除所有许可证
DELETE FROM Licenses

-- 重置自增 ID
DBCC CHECKIDENT ('Licenses', RESEED, 0)
```

## 下一步

测试通过后：
1. 实现前端许可证管理界面
2. 编写自动化测试
3. 性能测试
4. 用户文档编写
