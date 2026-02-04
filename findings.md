# 研究发现

## 项目分析

### 项目名称
DataForgeStudio V4 - 报表管理系统

### 核心需求
1. 动态 SQL 报表设计器
2. 报表查询与展示
3. 完整的权限管理系统
4. 数据源管理
5. 系统运维功能（日志、备份）

### 技术环境
- 开发: Windows 11, SQL Server 2016
- 生产: Windows Server, SQL Server 2005+
- 客户端: Windows 10+, 主流浏览器

## 技术栈研究（2025-02-03）

### 后端框架选择

| 框架 | 许可证 | 商业使用 | 结论 |
|------|--------|----------|------|
| ASP.NET Core 8.0 | MIT | ✅ 免费 | **选择** |
| .NET Framework | 免费 | ✅ 免费 | 过时，不推荐 |
| Go + Gin | MIT | ✅ 免费 | 生态不如 .NET |
| Java Spring | Apache 2.0 | ✅ 免费 | 学习成本高 |

**选择 ASP.NET Core 8.0 的原因**:
- 完全免费开源 (MIT)
- 与现有 Windows 生态无缝集成
- 性能优秀
- 微软官方长期支持至 2026 年 11 月

### 前端框架选择

| 框架 | 许可证 | 商业使用 | 结论 |
|------|--------|----------|------|
| Vue 3 | MIT | ✅ 免费 | **选择** |
| React | MIT | ✅ 免费 | 学习曲线陡 |
| Angular | MIT | ✅ 免费 | 过于复杂 |

**选择 Vue 3 的原因**:
- MIT 许可，完全免费
- 中文文档完善
- 学习曲线平缓
- 单文件组件开发体验好

### UI 组件库选择

| 库 | 许可证 | 商业使用 | 结论 |
|------|--------|----------|------|
| Element Plus | MIT | ✅ 免费 | **选择** |
| Ant Design Vue | MIT | ✅ 免费 | 备选 |
| Naive UI | MIT | ✅ 免费 | API 设计特别 |

**选择 Element Plus 的原因**:
- MIT 许可
- 组件丰富（60+ 组件）
- 中文文档完善
- 与 Vue 3 配合最佳

### 图表库选择

| 库 | 许可证 | 商业使用 | 结论 |
|------|--------|----------|------|
| Apache ECharts | Apache 2.0 | ✅ 免费 | **选择** |
| Chart.js | MIT | ✅ 免费 | 功能较弱 |
| Highcharts | 商业 | ❌ 需付费 | 不选 |

**选择 ECharts 的原因**:
- Apache 2.0 许可，商业友好
- 百度开源，功能强大
- 图表类型丰富（20+ 系列）
- 性能优秀，支持百万级数据

### Excel 导出选择

| 库 | 许可证 | 商业使用 | 结论 |
|------|--------|----------|------|
| **ClosedXML** | MIT | ✅ 免费 | **推荐** |
| NPOI | Apache 2.0 | ✅ 免费 | 备选 |
| EPPlus 5+ | Polyform Noncommercial | ❌ 商业需付费 | **不选** |
| EPPlus 4.x | LGPL | ⚠️ 需注意 | 过时 |

**⚠️ 重要**: EPPlus 5.0+ 改为 Polyform Noncommercial 许可证，**商业使用需要付费**

**选择 ClosedXML 的原因**:
- MIT 许可，完全免费商用
- API 简洁易用
- 不依赖 Excel 安装
- 支持现代 .xlsx 格式
- 活跃维护中

### PDF 导出选择

| 方案 | 许可证 | 商业使用 | 结论 |
|------|--------|----------|------|
| jsPDF + html2canvas | MIT | ✅ 免费 | **选择** |
| iTextSharp (AGPL) | AGPL | ❌ 商业需付费 | 不选 |
| PdfSharp | MIT | ✅ 免费 | 备选 |

### SQL 编辑器选择

| 库 | 许可证 | 商业使用 | 结论 |
|------|--------|----------|------|
| CodeMirror 6 | MIT | ✅ 免费 | **选择** |
| Monaco Editor | MIT | ✅ 免费 | 备选 |
| Ace Editor | BSD | ✅ 免费 | 过时 |

### 许可证加密方案

| 组件 | 技术 | 许可证 |
|------|------|--------|
| 非对称加密 | RSA | .NET 内置 |
| 对称加密 | AES | .NET 内置 |
| 签名验证 | RSA/SHA256 | .NET 内置 |
| 硬件绑定 | WMI | Windows API |

**全部使用 .NET 内置类库，无需第三方依赖**

## 关键挑战与解决方案

### 1. SQL Server 2005 兼容性
- **问题**: SQL Server 2005 不支持现代 SQL 特性
- **解决**: 使用基础 SQL 语法，避免新特性
- **测试**: 需在 SQL Server 2005 环境测试

### 2. 动态 SQL 安全性
- **问题**: SQL 注入风险
- **解决**:
  - 参数化查询
  - 白名单验证表名/字段名
  - 限制 SQL 语句类型（只允许 SELECT）

### 3. 大数据导出性能
- **问题**: 导出百万行数据内存溢出
- **解决**:
  - 分批次查询
  - 流式写入
  - 提供进度反馈

### 4. 许可证破解风险
- **问题**: 客户端可能破解许可证验证
- **解决**:
  - 服务端验证
  - 混淆加密代码
  - 在线验证机制

## 技术约束
- ✅ 需兼容 SQL Server 2005+
- ✅ 需支持主流浏览器（Chrome, Firefox, Edge, Safari）
- ✅ 需支持 Windows 10+ 客户端
- ✅ 所有技术栈必须可免费商用

## 数据库设计研究（2025-02-03）

### 设计原则
- 兼容 SQL Server 2005+（避免使用新特性）
- 使用 nvarchar 支持中文
- 主键使用 IDENTITY 自增
- 统一字段命名规范

### Root 用户方案

| 方案 | 说明 |
|------|------|
| **IsSystem 字段** | BIT 类型，1=系统用户，0=普通用户 |
| **约束保护** | CHECK 约束确保只有 root 可设置为 IsSystem=1 |
| **查询过滤** | 前端查询使用 `WHERE IsSystem = 0` |
| **API 保护** | 创建用户时强制 IsSystem=0 |

### 数据库表设计（14个表）

| 表名 | 说明 | 关键字段 |
|------|------|----------|
| Users | 用户表 | IsSystem (系统用户标记) |
| Roles | 角色表 | IsSystem (内置角色) |
| UserRoles | 用户角色关联 | UserId, RoleId |
| Permissions | 权限表 | PermissionCode, Module, Action |
| RolePermissions | 角色权限关联 | RoleId, PermissionId |
| DataSources | 数据源表 | DbType, ServerAddress, Password(AES加密) |
| Reports | 报表定义 | SqlStatement, DataSourceId |
| ReportFields | 报表字段配置 | FieldName, DisplayName, DataType |
| ReportParameters | 报表参数配置 | ParameterName, DataType, InputType |
| OperationLogs | 操作日志 | Module, Action, UserId, CreatedTime |
| SystemConfigs | 系统配置 | ConfigKey, ConfigValue, ConfigType |
| BackupRecords | 备份记录 | BackupPath, FileSize, BackupTime |
| Licenses | 许可证表 | LicenseKey(RSA加密), ExpiryDate |
| LoginLogs | 登录日志 | UserId, LoginStatus, IpAddress |

### SQL Server 2005 兼容性处理

**避免使用的特性**:
- `SEQUENCE` (2012+) → 使用 `IDENTITY`
- `OFFSET/FETCH` (2012+) → 使用 `ROW_NUMBER()`
- `IIF` (2012+) → 使用 `CASE WHEN`
- `CONCAT` (2012+) → 使用 `+` 连接
- `STRING_SPLIT` (2016+) → 使用自定义函数

### 安全性设计

| 措施 | 实现 |
|------|------|
| 密码存储 | HASHBYTES('SHA2_256', Password + Salt) |
| 数据源密码 | AES 加密存储 |
| SQL 注入防护 | 参数化查询 + 白名单验证 |
| 审计日志 | 所有操作记录到 OperationLogs |
| 登录保护 | 5次失败自动锁定 |

### 分页方案（兼容 SQL Server 2005）

```sql
-- 使用 ROW_NUMBER() 实现分页
WITH PaginatedData AS (
    SELECT *,
           ROW_NUMBER() OVER (ORDER BY CreatedTime DESC) AS RowNum
    FROM Users
    WHERE IsSystem = 0
)
SELECT * FROM PaginatedData
WHERE RowNum BETWEEN 1 AND 20
```

### 初始数据

**内置用户**:
- root / 密码: admin123 (首次登录需修改)

**内置角色**:
- 超级管理员 (SUPER_ADMIN)
- 管理员 (ADMIN)
- 普通用户 (USER)
- 访客 (GUEST)

**内置权限**: 约 40+ 个权限点
