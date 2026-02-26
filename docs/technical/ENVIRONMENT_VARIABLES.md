# 环境变量配置

生产环境必须配置以下环境变量以确保系统安全性。

## 安全配置

### JWT 认证配置

```bash
# JWT 密钥 (64 字符随机字符串)
DFS_JWT_SECRET="your-64-character-random-secret-key-here-change-in-production"
```

**说明**: 用于签名和验证 JWT 令牌。必须至少 64 个字符，建议使用强随机生成器生成。

### AES 加密配置

```bash
# AES 加密密钥 (32 字符)
DFS_ENCRYPTION_AESKEY="your-32-character-aes-key-here"

# AES 初始化向量 (16 字符)
DFS_ENCRYPTION_AESIV="your-16-character-iv-here"
```

**说明**: 用于加密数据源密码等敏感信息。密钥必须恰好 32 个字符（256 位），IV 必须恰好 16 个字符（128 位）。

### 许可证加密配置

```bash
# 许可证 AES 密钥 (32 字符)
DFS_LICENSE_AESKEY="your-32-character-license-key-here"

# 许可证 AES IV (16 字符)
DFS_LICENSE_AESIV="your-16-character-license-iv-here"
```

**说明**: 用于许可证系统的加密和解密操作。密钥必须恰好 32 个字符，IV 必须恰好 16 个字符。

## 配置优先级

系统按以下优先级读取配置：

1. **环境变量** (最高优先级) - 生产环境推荐
2. **appsettings.json** 配置文件 - 开发环境使用
3. **默认值** (仅用于测试) - 不应在生产环境使用

## 生成安全密钥

### 使用 PowerShell 生成密钥

```powershell
# 生成 64 字符 JWT 密钥
-Join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | % {[char]$_})

# 生成 32 字符 AES 密钥
-Join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})

# 生成 16 字符 IV
-Join ((48..57) + (65..90) + (97..122) | Get-Random -Count 16 | % {[char]$_})
```

### 使用 Linux/Mac 生成密钥

```bash
# 生成 64 字符 JWT 密钥
cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 64 | head -n 1

# 生成 32 字符 AES 密钥
cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1

# 生成 16 字符 IV
cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 16 | head -n 1
```

### 使用在线工具生成密钥

访问以下在线工具生成安全的随机密钥：
- https://www.random.org/strings/
- https://www.grc.com/passwords.htm

## 设置环境变量

### Windows (命令提示符)

```cmd
set DFS_JWT_SECRET=your-64-character-secret-key
set DFS_ENCRYPTION_AESKEY=your-32-character-aes-key
set DFS_ENCRYPTION_AESIV=your-16-character-iv
set DFS_LICENSE_AESKEY=your-32-character-license-key
set DFS_LICENSE_AESIV=your-16-character-license-iv
```

### Windows (PowerShell)

```powershell
$env:DFS_JWT_SECRET="your-64-character-secret-key"
$env:DFS_ENCRYPTION_AESKEY="your-32-character-aes-key"
$env:DFS_ENCRYPTION_AESIV="your-16-character-iv"
$env:DFS_LICENSE_AESKEY="your-32-character-license-key"
$env:DFS_LICENSE_AESIV="your-16-character-license-iv"
```

### Windows (系统环境变量)

1. 右键点击 "此电脑" → "属性"
2. 点击 "高级系统设置"
3. 点击 "环境变量"
4. 在 "系统变量" 或 "用户变量" 中添加上述变量

### Linux/Mac

```bash
export DFS_JWT_SECRET="your-64-character-secret-key"
export DFS_ENCRYPTION_AESKEY="your-32-character-aes-key"
export DFS_ENCRYPTION_AESIV="your-16-character-iv"
export DFS_LICENSE_AESKEY="your-32-character-license-key"
export DFS_LICENSE_AESIV="your-16-character-license-iv"
```

要永久设置，将上述行添加到 `~/.bashrc` 或 `~/.zshrc` 文件中。

### Docker / Docker Compose

**docker-compose.yml**:
```yaml
services:
  api:
    environment:
      - DFS_JWT_SECRET=your-64-character-secret-key
      - DFS_ENCRYPTION_AESKEY=your-32-character-aes-key
      - DFS_ENCRYPTION_AESIV=your-16-character-iv
      - DFS_LICENSE_AESKEY=your-32-character-license-key
      - DFS_LICENSE_AESIV=your-16-character-license-iv
```

或使用 `.env` 文件：
```env
DFS_JWT_SECRET=your-64-character-secret-key
DFS_ENCRYPTION_AESKEY=your-32-character-aes-key
DFS_ENCRYPTION_AESIV=your-16-character-iv
DFS_LICENSE_AESKEY=your-32-character-license-key
DFS_LICENSE_AESIV=your-16-character-license-iv
```

### IIS 部署

在 IIS 中设置环境变量：

1. 打开 IIS 管理器
2. 选择您的网站或应用程序池
3. 双击 "配置编辑器"
4. 在 "Section" 下拉菜单中选择 "system.webServer/aspNetCore"
5. 设置 "environmentVariables" 集合
6. 添加上述环境变量

或直接在 `web.config` 中配置：

```xml
<aspNetCore processPath="dotnet" arguments=".\DataForgeStudio.Api.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout">
  <environmentVariables>
    <environmentVariable name="DFS_JWT_SECRET" value="your-64-character-secret-key" />
    <environmentVariable name="DFS_ENCRYPTION_AESKEY" value="your-32-character-aes-key" />
    <environmentVariable name="DFS_ENCRYPTION_AESIV" value="your-16-character-iv" />
    <environmentVariable name="DFS_LICENSE_AESKEY" value="your-32-character-license-key" />
    <environmentVariable name="DFS_LICENSE_AESIV" value="your-16-character-license-iv" />
  </environmentVariables>
</aspNetCore>
```

### systemd 服务 (Linux)

创建或编辑服务文件 `/etc/systemd/system/dataforge.service`：

```ini
[Unit]
Description=DataForgeStudio V4 API
After=network.target

[Service]
Type=notify
WorkingDirectory=/var/www/dataforge
ExecStart=/usr/bin/dotnet /var/www/dataforge/DataForgeStudio.Api.dll
Restart=always
RestartSec=10

Environment="DFS_JWT_SECRET=your-64-character-secret-key"
Environment="DFS_ENCRYPTION_AESKEY=your-32-character-aes-key"
Environment="DFS_ENCRYPTION_AESIV=your-16-character-iv"
Environment="DFS_LICENSE_AESKEY=your-32-character-license-key"
Environment="DFS_LICENSE_AESIV=your-16-character-license-iv"

[Install]
WantedBy=multi-user.target
```

## 验证配置

启动应用后，检查控制台输出：

**测试环境（使用默认值）**:
```
⚠️  WARNING: Using default JWT Secret for testing. Set DFS_JWT_SECRET environment variable for production!
⚠️  WARNING: Using default AES Key for testing. Set DFS_ENCRYPTION_AESKEY environment variable for production!
⚠️  WARNING: Using default License AES Key for testing. Set DFS_LICENSE_AESKEY environment variable for production!
```

**生产环境（正确配置）**:
不应看到上述警告信息。

如果未配置且设置了 `SecurityOptionsUseDefaultsForTesting: false`，应用将抛出异常并拒绝启动：

```
System.InvalidOperationException: JWT Secret 未配置或长度不足64位。请设置环境变量 DFS_JWT_SECRET (64+字符)
```

## 安全建议

1. **永不提交密钥到版本控制**: 确保 `.gitignore` 包含 `appsettings.json` 和 `.env` 文件
2. **使用强随机密钥**: 始终使用加密安全的随机生成器
3. **定期轮换密钥**: 建议每 6-12 个月更换一次密钥
4. **分别使用不同密钥**: JWT、加密、许可证应使用不同的密钥
5. **安全存储**: 使用密钥管理服务（如 Azure Key Vault、AWS Secrets Manager）存储生产密钥
6. **最小权限原则**: 限制对环境变量的访问权限

## 禁用测试默认值

在生产环境中，设置以下配置以确保必须使用环境变量：

```json
{
  "SecurityOptionsUseDefaultsForTesting": false
}
```

当此选项设置为 `false` 时，如果未正确配置环境变量，应用将无法启动。
