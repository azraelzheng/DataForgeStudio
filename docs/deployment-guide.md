# DataForgeStudio V1.0 部署指南

## 目录

1. [环境要求](#环境要求)
2. [安装步骤](#安装步骤)
3. [配置说明](#配置说明)
4. [服务管理](#服务管理)
5. [常见问题](#常见问题)

---

## 环境要求

### 硬件要求
- CPU: 2 核心以上
- 内存: 4GB 以上
- 磁盘: 1GB 以上可用空间

### 软件要求
- 操作系统: Windows Server 2012 R2+ / Windows 10+
- 数据库: SQL Server 2005+
- 运行时: .NET 8.0 Runtime

### 可选组件
- IIS 7.0+ (用于前端托管)
- Nginx (替代 IIS，包含在安装包中)

---

## 安装步骤

### 1. 准备工作

1. **安装 SQL Server**
   - 确保 SQL Server 已安装并运行
   - 创建或确认有 sa 权限的账户

2. **安装 .NET 8.0 Runtime**
   - 下载地址: https://dotnet.microsoft.com/download/dotnet/8.0
   - 安装 .NET Desktop Runtime

3. **安装 IIS (可选)**
   - 控制面板 → 程序 → 启用或关闭 Windows 功能
   - 勾选 "Internet Information Services"

### 2. 运行安装程序

1. 以**管理员身份**运行 `DataForgeStudio-Setup.exe`
2. 选择安装目录 (默认: `C:\Program Files\DataForgeStudio`)
3. 点击"下一步"完成安装

### 3. 运行配置向导

安装完成后，会自动启动配置向导：

1. **数据库配置**
   - 服务器地址: SQL Server 地址 (默认: localhost)
   - 端口: SQL Server 端口 (默认: 1433)
   - 认证方式: Windows 身份验证 或 SQL Server 身份验证
   - 点击"测试连接"验证配置
   - 点击"下一步"

2. **服务配置**
   - 后端端口: API 服务端口 (默认: 5000)
   - 前端端口: Web 服务端口 (默认: 80)
   - 点击"下一步"

3. **前端模式**
   - IIS: 使用 Windows IIS 托管
   - Nginx: 使用内置 Nginx 托管
   - 点击"下一步"

4. **完成安装**
   - 点击"安装"开始配置
   - 等待安装完成

---

## 配置说明

### 配置文件位置

| 文件 | 路径 | 说明 |
|------|------|------|
| 后端配置 | `{安装目录}\api\appsettings.json` | API 配置 |
| Nginx 配置 | `{安装目录}\nginx\conf\nginx.conf` | Nginx 配置 |
| 系统管理配置 | `{安装目录}\config.json` | 管理工具配置 |

### 环境变量 (生产环境推荐)

在系统环境变量中配置以下变量：

```batch
setx DFS_JWT_SECRET "64字符随机密钥" /M
setx DFS_ENCRYPTION_AES_KEY "32字符AES密钥" /M
setx DFS_ENCRYPTION_AES_IV "16字符AES初始向量" /M
setx DFS_LICENSE_AES_KEY "32字符许可证密钥" /M
setx DFS_LICENSE_AES_IV "16字符许可证初始向量" /M
```

### 防火墙配置

确保以下端口开放：

| 端口 | 用途 | 协议 |
|------|------|------|
| 5000 | 后端 API | TCP |
| 80 | 前端 Web | TCP |
| 1433 | SQL Server | TCP |

---

## 服务管理

### 使用系统管理工具

运行 `{安装目录}\DeployManager.exe`：

- **服务管理**: 启动、停止、重启后端服务
- **数据库配置**: 修改数据库连接
- **端口配置**: 修改服务端口
- **开机自启**: 设置服务开机自启动

### 使用命令行

```batch
# 启动后端服务
sc start DFAppService

# 停止后端服务
sc stop DFAppService

# 查看服务状态
sc query DFAppService

# 启动 Nginx
cd "C:\Program Files\DataForgeStudio\nginx"
start nginx

# 停止 Nginx
nginx -s stop

# 重载 Nginx 配置
nginx -s reload
```

---

## 常见问题

### 1. 无法连接数据库

**原因**: SQL Server 未启用 TCP/IP 或防火墙阻止

**解决方案**:
1. 打开 SQL Server Configuration Manager
2. 启用 TCP/IP 协议
3. 重启 SQL Server 服务
4. 检查防火墙是否开放 1433 端口

### 2. 端口被占用

**原因**: 指定端口已被其他程序使用

**解决方案**:
1. 使用系统管理工具检测端口占用
2. 或使用命令: `netstat -ano | findstr :5000`
3. 更换端口或停止占用端口的程序

### 3. 服务启动失败

**原因**: 配置文件错误或权限不足

**解决方案**:
1. 检查 appsettings.json 格式是否正确
2. 以管理员身份运行服务
3. 查看 Windows 事件日志

### 4. 许可证无效

**原因**: 许可证过期或机器码不匹配

**解决方案**:
1. 联系管理员获取新许可证
2. 使用 LicenseGenerator 工具生成新许可证
3. 在系统中重新激活许可证

### 5. IIS 403 禁止访问

**原因**: IIS 权限配置问题

**解决方案**:
1. 确保 IIS_IUSRS 对 WebSite 目录有读取权限
2. 检查应用程序池身份配置
3. 确保 .NET 8.0 Hosting Bundle 已安装

---

## 技术支持

如遇其他问题，请联系技术支持并提供以下信息：
- 操作系统版本
- 错误信息截图
- 相关日志文件 (`{安装目录}\logs\`)
