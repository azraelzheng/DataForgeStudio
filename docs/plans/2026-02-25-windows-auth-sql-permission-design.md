# Windows 认证 SQL Server 权限自动授权设计

## 日期
2026-02-25

## 背景
当用户选择 Windows 认证模式安装 DataForgeStudio 时，DFAppService 以 `NT AUTHORITY\SYSTEM` (Local System) 账户运行。SQL Server 默认没有为该账户配置登录权限，导致服务启动后无法连接数据库。

## 目标
在安装过程中自动授予 `NT AUTHORITY\SYSTEM` SQL Server 权限，使 Windows 认证模式可以正常工作。

## 设计决策

### 授权时机
- **在创建数据库之后**：数据库存在后再授权，可以复用现有连接

### 权限范围
- **sysadmin 服务器角色**：最高权限，简化管理，适用于单机部署

### 失败处理
- **跳过并记录日志**：授权失败时不中断安装，依赖服务启动时的错误提示

## 实现方案

### 修改位置
- **文件**：`backend/tools/Configurator/Program.cs`
- **方法**：`InitializeDatabase` 方法末尾

### 新增方法

```csharp
/// <summary>
/// 授予 NT AUTHORITY\SYSTEM SQL Server 权限
/// </summary>
static async Task GrantSystemServicePermissions(SqlConnection connection, string databaseName)
{
    const string serviceAccount = "NT AUTHORITY\\SYSTEM";

    try
    {
        // 1. 检查登录是否存在
        var checkLoginCmd = new SqlCommand(
            "SELECT 1 FROM sys.server_principals WHERE name = @name",
            connection);
        checkLoginCmd.Parameters.AddWithValue("@name", serviceAccount);

        var loginExists = await checkLoginCmd.ExecuteScalarAsync();

        if (loginExists == null)
        {
            // 2. 创建 Windows 登录
            var createLoginCmd = new SqlCommand(
                $"CREATE LOGIN [{serviceAccount}] FROM WINDOWS",
                connection);
            await createLoginCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"  创建登录: {serviceAccount}");
        }

        // 3. 检查是否已在 sysadmin 角色中
        var checkRoleCmd = new SqlCommand(
            @"SELECT 1 FROM sys.server_role_members rm
              JOIN sys.server_principals p ON rm.member_principal_id = p.principal_id
              JOIN sys.server_principals r ON rm.role_principal_id = r.principal_id
              WHERE p.name = @name AND r.name = 'sysadmin'",
            connection);
        checkRoleCmd.Parameters.AddWithValue("@name", serviceAccount);

        var inRole = await checkRoleCmd.ExecuteScalarAsync();

        if (inRole == null)
        {
            // 4. 添加到 sysadmin 角色
            var addRoleCmd = new SqlCommand(
                $"ALTER SERVER ROLE sysadmin ADD MEMBER [{serviceAccount}]",
                connection);
            await addRoleCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"  授予 sysadmin 权限: {serviceAccount}");
        }
        else
        {
            Console.WriteLine($"  {serviceAccount} 已拥有 sysadmin 权限");
        }
    }
    catch (Exception ex)
    {
        // 授权失败不中断安装，仅记录警告
        Console.WriteLine($"  ⚠️ 授权服务账户失败: {ex.Message}");
        Console.WriteLine($"  服务启动时可能无法连接数据库，请手动授权");
    }
}
```

### 修改 InitializeDatabase

在 `InitializeDatabase` 方法末尾添加：

```csharp
// 如果是 Windows 认证，授权服务账户
if (config.DbAuth.Equals("windows", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("  授权服务账户...");
    await GrantSystemServicePermissions(masterConnection, config.DbName);
}
```

## 执行的 SQL 逻辑

```sql
-- 1. 检查登录是否存在
SELECT 1 FROM sys.server_principals WHERE name = 'NT AUTHORITY\SYSTEM'

-- 2. 不存在则创建登录
CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS

-- 3. 检查是否已在 sysadmin 角色中
SELECT 1 FROM sys.server_role_members rm
JOIN sys.server_principals p ON rm.member_principal_id = p.principal_id
JOIN sys.server_principals r ON rm.role_principal_id = r.principal_id
WHERE p.name = 'NT AUTHORITY\SYSTEM' AND r.name = 'sysadmin'

-- 4. 添加到 sysadmin 角色
ALTER SERVER ROLE sysadmin ADD MEMBER [NT AUTHORITY\SYSTEM]
```

## 测试要点

1. **Windows 认证 + 全新安装**：验证服务账户被正确授权
2. **Windows 认证 + 已有数据库**：验证不会重复授权
3. **SQL 认证**：验证不会执行授权逻辑
4. **授权失败场景**：验证安装不中断，仅显示警告

## 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| SQL Server 不允许创建 Windows 登录 | try-catch 捕获异常，继续安装 |
| 当前用户没有授权权限 | try-catch 捕获异常，提示用户手动处理 |
| 安全性考虑（sysadmin 权限过高） | 设计决策已确认，适用于单机部署场景 |
