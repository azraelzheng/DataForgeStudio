# P3: 数据库初始化改进实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 安装后自动创建三个默认权限组和默认管理员用户

**Architecture:** 修改 DbInitializer.cs，在数据库初始化时插入预设数据

**Tech Stack:** ASP.NET Core 8.0, Entity Framework Core

**Related Tasks:** fix2.md 任务 5, 6

---

## Task 1: 预置三个默认权限组 (fix2.md #5)

**问题描述:** 系统中应预置三个权限组（管理员、开发者、查看者），但当前安装后未自动创建

**预期行为:**
- 管理员：拥有所有权限
- 开发者：拥有报表设计、数据源管理权限
- 查看者：只有查看报表的权限

**Files:**
- Modify: `backend/src/DataForgeStudio.Data/Data/DbInitializer.cs`

**Step 1: 定义默认角色配置**

在 `DbInitializer.cs` 中添加默认角色定义：

```csharp
/// <summary>
/// 默认角色配置
/// </summary>
private static readonly (string RoleName, string RoleCode, string Description, string[] Permissions)[] DefaultRoles =
{
    ("管理员", "ROLE_ADMIN", "系统管理员，拥有大部分管理权限", new[]
    {
        "user:view", "user:create", "user:edit", "user:delete", "user:resetPassword",
        "role:view", "role:edit", "role:assignPermissions",
        "report:query", "report:execute", "report:design", "report:create", "report:edit", "report:delete", "report:export",
        "datasource:view", "datasource:create", "datasource:edit", "datasource:delete", "datasource:test",
        "log:view", "log:export",
        "backup:view", "backup:create", "backup:restore",
        "license:view",
        "system:view", "system:edit"
    }),
    ("开发者", "ROLE_DEVELOPER", "报表开发者，可以设计报表和管理数据源", new[]
    {
        "report:query", "report:execute", "report:design", "report:create", "report:edit", "report:delete", "report:export",
        "datasource:view", "datasource:create", "datasource:edit", "datasource:delete", "datasource:test",
        "log:view"
    }),
    ("查看者", "ROLE_VIEWER", "普通用户，只能查看和执行报表", new[]
    {
        "report:query", "report:execute", "report:export"
    })
};
```

**Step 2: 添加创建默认角色方法**

```csharp
/// <summary>
/// 创建默认角色
/// </summary>
private static async Task CreateDefaultRolesAsync(DataForgeStudioDbContext context)
{
    foreach (var (roleName, roleCode, description, permissions) in DefaultRoles)
    {
        // 检查角色是否已存在
        var existingRole = await context.Roles
            .FirstOrDefaultAsync(r => r.RoleCode == roleCode);

        if (existingRole == null)
        {
            // 创建新角色
            var role = new Role
            {
                RoleName = roleName,
                RoleCode = roleCode,
                Description = description,
                IsSystem = false, // 非系统角色，可以被删除
                IsActive = true,
                CreatedTime = DateTime.UtcNow
            };

            context.Roles.Add(role);
            await context.SaveChangesAsync();

            // 获取权限并关联
            var permissionEntities = await context.Permissions
                .Where(p => permissions.Contains(p.PermissionCode))
                .ToListAsync();

            foreach (var permission in permissionEntities)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.RoleId,
                    PermissionId = permission.PermissionId
                });
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"✅ 默认角色已创建: {roleName}");
        }
        else
        {
            // 更新现有角色的权限
            var existingPermissions = await context.RolePermissions
                .Where(rp => rp.RoleId == existingRole.RoleId)
                .ToListAsync();
            context.RolePermissions.RemoveRange(existingPermissions);
            await context.SaveChangesAsync();

            var permissionEntities = await context.Permissions
                .Where(p => permissions.Contains(p.PermissionCode))
                .ToListAsync();

            foreach (var permission in permissionEntities)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = existingRole.RoleId,
                    PermissionId = permission.PermissionId
                });
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"✓ 默认角色权限已更新: {roleName}");
        }
    }
}
```

**Step 3: 在 InitializeAsync 中调用**

修改 `InitializeAsync` 方法，在创建超级管理员角色之后添加：

```csharp
public static async Task InitializeAsync(DataForgeStudioDbContext context, bool forceResetPermissions = false)
{
    // ... 现有代码 ...

    // 为 root 用户分配超级管理员角色
    // ... 现有代码 ...

    // 创建默认角色（管理员、开发者、查看者）
    await CreateDefaultRolesAsync(context);
}
```

**Step 4: 验证修复**

1. 删除数据库或使用新数据库
2. 启动 API，观察控制台输出
3. 确认看到：`✅ 默认角色已创建: 管理员`、`✅ 默认角色已创建: 开发者`、`✅ 默认角色已创建: 查看者`
4. 登录系统，进入角色管理，确认三个角色存在

---

## Task 2: 创建默认管理员用户 (fix2.md #6)

**问题描述:** 安装后缺少默认管理员账户（需要区别于 root 系统用户）

**预期行为:**
- 用户名: admin
- 密码: Admin@123
- 拥有管理员角色权限

**Files:**
- Modify: `backend/src/DataForgeStudio.Data/Data/DbInitializer.cs`

**Step 1: 添加创建默认管理员用户方法**

```csharp
/// <summary>
/// 创建默认管理员用户
/// </summary>
private static async Task CreateDefaultAdminUserAsync(DataForgeStudioDbContext context)
{
    const string adminUsername = "admin";
    const string adminPassword = "Admin@123";

    // 检查 admin 用户是否已存在
    var existingAdmin = await context.Users
        .FirstOrDefaultAsync(u => u.Username == adminUsername);

    if (existingAdmin == null)
    {
        // 创建 admin 用户
        var adminUser = new User
        {
            Username = adminUsername,
            PasswordHash = EncryptionHelper.HashPassword(adminPassword),
            RealName = "管理员",
            Email = "admin@dataforge.com",
            IsActive = true,
            IsSystem = false, // 非系统用户，可以修改/删除
            MustChangePassword = true, // 首次登录需要修改密码
            CreatedTime = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // 获取"管理员"角色
        var adminRole = await context.Roles
            .FirstOrDefaultAsync(r => r.RoleCode == "ROLE_ADMIN");

        if (adminRole != null)
        {
            // 为 admin 用户分配管理员角色
            context.UserRoles.Add(new UserRole
            {
                UserId = adminUser.UserId,
                RoleId = adminRole.RoleId
            });

            await context.SaveChangesAsync();
        }

        Console.WriteLine($"✅ 默认管理员用户已创建: {adminUsername} / {adminPassword}");
    }
    else
    {
        Console.WriteLine("✓ 默认管理员用户已存在，跳过创建");
    }
}
```

**Step 2: 在 InitializeAsync 中调用**

修改 `InitializeAsync` 方法，在创建默认角色之后添加：

```csharp
public static async Task InitializeAsync(DataForgeStudioDbContext context, bool forceResetPermissions = false)
{
    // ... 现有代码 ...

    // 创建默认角色（管理员、开发者、查看者）
    await CreateDefaultRolesAsync(context);

    // 创建默认管理员用户
    await CreateDefaultAdminUserAsync(context);
}
```

**Step 3: 确保执行顺序正确**

完整的 `InitializeAsync` 方法流程：

```csharp
public static async Task InitializeAsync(DataForgeStudioDbContext context, bool forceResetPermissions = false)
{
    // 1. 确保数据库已创建
    await context.Database.EnsureCreatedAsync();

    // 2. 验证并修复数据库架构
    await ValidateAndFixSchemaAsync(context);

    // 3. 强制重建权限（用于开发调试）
    if (forceResetPermissions)
    {
        await ResetPermissionsAsync(context);
    }

    // 4. 创建 root 用户
    // ... 现有代码 ...

    // 5. 创建权限（如果不存在）
    if (!await context.Permissions.AnyAsync())
    {
        await CreateAllPermissionsAsync(context);
    }

    // 6. 创建超级管理员角色并分配所有权限
    // ... 现有代码 ...

    // 7. 创建默认角色（管理员、开发者、查看者）
    await CreateDefaultRolesAsync(context);

    // 8. 创建默认管理员用户
    await CreateDefaultAdminUserAsync(context);
}
```

**Step 4: 验证修复**

1. 删除数据库
2. 启动 API
3. 确认控制台输出：`✅ 默认管理员用户已创建: admin / Admin@123`
4. 使用 admin / Admin@123 登录系统
5. 确认登录成功并提示修改密码
6. 确认 admin 用户拥有管理员角色的权限

---

## 完整的 DbInitializer.cs 修改摘要

在 `DbInitializer.cs` 中添加以下内容：

1. **DefaultRoles 静态数组**: 定义三个默认角色的配置
2. **CreateDefaultRolesAsync 方法**: 创建默认角色并分配权限
3. **CreateDefaultAdminUserAsync 方法**: 创建默认管理员用户
4. **修改 InitializeAsync 方法**: 按正确顺序调用上述方法

## 测试验证

```sql
-- 验证角色
SELECT * FROM Roles WHERE RoleCode IN ('ROLE_ADMIN', 'ROLE_DEVELOPER', 'ROLE_VIEWER');

-- 验证用户
SELECT * FROM Users WHERE Username = 'admin';

-- 验证用户角色关联
SELECT u.Username, r.RoleName
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.Username IN ('root', 'admin');
```

## 执行顺序

1. Task 1 → Task 2 (Task 2 依赖 Task 1 创建的角色)
