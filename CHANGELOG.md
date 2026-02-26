# 更新日志

所有重要的更改都将记录在此文件中。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)

## [1.0.0] - 2026-02-27

### 新增
- 动态SQL报表设计器，支持参数化查询和条件筛选
- 多数据源支持：SQL Server、MySQL、PostgreSQL、Oracle、SQLite
- 报表查询与导出功能（Excel、CSV格式）
- 用户管理与角色权限控制
- 许可证管理系统（机器码绑定，15天试用期）
- 数据源管理（支持连接测试和密码加密存储）
- 系统备份与恢复功能
- 操作日志记录
- Windows服务一键部署
- Inno Setup安装程序

### 安全
- 密码使用BCrypt加密存储
- 数据源密码使用AES加密
- 许可证使用RSA签名验证
- 敏感代码混淆保护
- JWT令牌认证

### 技术栈
- 后端：ASP.NET Core 8.0, Entity Framework Core
- 前端：Vue 3, Element Plus, Vite
- 数据库：SQL Server 2005+
- 安装程序：Inno Setup 6
