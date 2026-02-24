# Windows 认证 SQL Server 权限自动授权实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在安装过程中自动授予 NT AUTHORITY\SYSTEM SQL Server 权限，使 Windows 认证模式可以正常工作。

**Architecture:** 在 Configurator 的 InitializeDatabase 方法末尾添加授权逻辑，仅当用户选择 Windows 认证时执行。授权失败时不中断安装，仅记录警告。

**Tech Stack:** C#, Microsoft.Data.SqlClient, SQL Server T-SQL

---

### Task 1: 添加 GrantSystemServicePermissions 方法

**Files:**
- Modify: `backend/tools/Configurator/Program.cs`

**Step 1: 在 Program.cs 中添加新方法**

在 `InitializeDatabase` 方法之后添加以下方法：

```csharp
/// <summary>
/// 授予 NT AUTHORITY\SYSTEM SQL Server 权限
/// 使 Windows 服务能够使用 Windows 认证连接数据库
/// </summary>
static async Task GrantSystemServicePermissions(SqlConnection connection)
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
        Console.WriteLine($"     服务启动时可能无法连接数据库，请手动授权");
    }
}
```

**Step 2: 验证编译**

Run: `dotnet build backend/tools/Configurator/Configurator.csproj`
Expected: Build succeeded

---

### Task 2: 修改 InitializeDatabase 方法调用授权

**Files:**
- Modify: `backend/tools/Configurator/Program.cs:645-650`

**Step 1: 在 InitializeDatabase 方法末尾添加授权调用**

找到 `InitializeDatabase` 方法中以下代码：

```csharp
        // 插入初始数据
        await ExecuteSqlScriptAsync(masterConnection, GetSeedDataSql());

        Console.WriteLine("  数据库初始化完成");
    }
```

替换为：

```csharp
        // 插入初始数据
        await ExecuteSqlScriptAsync(masterConnection, GetSeedDataSql());

        Console.WriteLine("  数据库初始化完成");

        // 如果是 Windows 认证，授权服务账户
        if (config.DbAuth.Equals("windows", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("  授权服务账户...");
            await GrantSystemServicePermissions(masterConnection);
        }
    }
```

**Step 2: 验证编译**

Run: `dotnet build backend/tools/Configurator/Configurator.csproj`
Expected: Build succeeded

**Step 3: 提交更改**

```bash
git add backend/tools/Configurator/Program.cs
git commit -m "feat: auto-grant SQL Server permissions for Windows auth

When Windows authentication is selected during installation, automatically
grant NT AUTHORITY\SYSTEM sysadmin role to enable the service to connect
to SQL Server using Windows authentication.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

### Task 3: 重新构建安装包

**Step 1: 执行构建脚本**

Run: `powershell -File scripts/build-installer.ps1`
Expected: Installer created at `dist/DataForgeStudio-Setup.exe`

**Step 2: 验证安装包**

确认安装包包含更新后的 Configurator。

---

## 测试清单

安装后验证：

1. [ ] **Windows 认证 + 全新安装**：服务启动成功，能够登录系统
2. [ ] **SQL 认证**：安装流程不受影响
3. [ ] **重复安装**：不会重复授权（检测到已存在则跳过）
