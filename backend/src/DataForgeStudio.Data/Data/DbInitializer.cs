using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.Utils;

namespace DataForgeStudio.Data.Data;

/// <summary>
/// 数据库初始化器 - 用于创建初始数据和种子数据
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// 初始化数据库 - 创建 root 用户和默认权限
    /// </summary>
    public static async Task InitializeAsync(DataForgeStudioDbContext context, bool forceResetPermissions = false)
    {
        // 确保数据库已创建
        await context.Database.EnsureCreatedAsync();

        // 验证并修复数据库架构（处理旧版 SQL 脚本创建的数据库）
        await ValidateAndFixSchemaAsync(context);

        // 强制重建权限（用于开发调试）
        if (forceResetPermissions)
        {
            await ResetPermissionsAsync(context);
        }

        // 检查是否已存在 root 用户
        var rootUser = await context.Users
            .FirstOrDefaultAsync(u => u.Username == "root");

        // 固定的 root 用户密码
        const string rootPassword = "Admin@123";

        if (rootUser == null)
        {
            // 创建 root 用户
            rootUser = new User
            {
                Username = "root",
                PasswordHash = EncryptionHelper.HashPassword(rootPassword),
                RealName = "系统管理员",
                Email = "root@dataforge.com",
                IsActive = true,
                IsSystem = true,
                MustChangePassword = false, // 不强制修改密码
                CreatedTime = DateTime.UtcNow
            };

            context.Users.Add(rootUser);
            await context.SaveChangesAsync();
            Console.WriteLine("✅ Root 用户已创建，密码: Admin@123");
        }
        else if (rootUser.PasswordHash.Contains("PLACEHOLDER"))
        {
            // 检测到 SQL 脚本创建的占位符密码，更新为实际密码
            rootUser.PasswordHash = EncryptionHelper.HashPassword(rootPassword);
            rootUser.MustChangePassword = false;
            await context.SaveChangesAsync();
            Console.WriteLine("✅ Root 用户密码已更新为: Admin@123");
        }

        // 创建权限（如果不存在）
        // 注意：如果 forceResetPermissions=true，权限已经在 ResetPermissionsAsync 中创建了
        if (!await context.Permissions.AnyAsync())
        {
            await CreateAllPermissionsAsync(context);
        }

        // 创建或更新超级管理员角色
        var adminRole = await context.Roles
            .FirstOrDefaultAsync(r => r.RoleCode == "ROLE_SUPER_ADMIN");

        if (adminRole == null)
        {
            adminRole = new Role
            {
                RoleName = "超级管理员",
                RoleCode = "ROLE_SUPER_ADMIN",
                Description = "系统超级管理员，拥有所有权限",
                IsSystem = true,
                IsActive = true,
                CreatedTime = DateTime.UtcNow
            };

            context.Roles.Add(adminRole);
            await context.SaveChangesAsync();
        }

        // 删除该角色的旧权限关联
        var oldRolePermissions = await context.RolePermissions
            .Where(rp => rp.RoleId == adminRole.RoleId)
            .ToListAsync();
        context.RolePermissions.RemoveRange(oldRolePermissions);
        await context.SaveChangesAsync();

        // 为超级管理员角色分配所有权限
        var allPermissions = await context.Permissions.ToListAsync();
        foreach (var permission in allPermissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.RoleId,
                PermissionId = permission.PermissionId
            });
        }

        await context.SaveChangesAsync();

        // 检查 root 用户是否已关联超级管理员角色
        var existingUserRole = await context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == rootUser.UserId && ur.RoleId == adminRole.RoleId);

        if (existingUserRole == null)
        {
            // 为 root 用户分配超级管理员角色
            context.UserRoles.Add(new UserRole
            {
                UserId = rootUser.UserId,
                RoleId = adminRole.RoleId
            });

            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 创建所有系统权限
    /// </summary>
    private static async Task CreateAllPermissionsAsync(DataForgeStudioDbContext context)
    {
        var permissions = new List<Permission>
        {
            // 用户管理权限
            new Permission { PermissionCode = "user:view", PermissionName = "查看用户", Module = "User", Action = "View", Description = "查看用户列表" },
            new Permission { PermissionCode = "user:create", PermissionName = "创建用户", Module = "User", Action = "Create", Description = "创建新用户" },
            new Permission { PermissionCode = "user:edit", PermissionName = "编辑用户", Module = "User", Action = "Edit", Description = "编辑用户信息" },
            new Permission { PermissionCode = "user:delete", PermissionName = "删除用户", Module = "User", Action = "Delete", Description = "删除用户" },
            new Permission { PermissionCode = "user:resetPassword", PermissionName = "重置密码", Module = "User", Action = "ResetPassword", Description = "重置用户密码" },

            // 角色管理权限
            new Permission { PermissionCode = "role:view", PermissionName = "查看角色", Module = "Role", Action = "View", Description = "查看角色列表" },
            new Permission { PermissionCode = "role:create", PermissionName = "创建角色", Module = "Role", Action = "Create", Description = "创建新角色" },
            new Permission { PermissionCode = "role:edit", PermissionName = "编辑角色", Module = "Role", Action = "Edit", Description = "编辑角色信息" },
            new Permission { PermissionCode = "role:delete", PermissionName = "删除角色", Module = "Role", Action = "Delete", Description = "删除角色" },
            new Permission { PermissionCode = "role:assignPermissions", PermissionName = "分配权限", Module = "Role", Action = "AssignPermissions", Description = "为角色分配权限" },

            // 报表管理权限
            // 报表查询相关
            new Permission { PermissionCode = "report:query", PermissionName = "访问报表查询", Module = "Report", Action = "Query", Description = "访问报表查询页面" },
            new Permission { PermissionCode = "report:execute", PermissionName = "执行报表查询", Module = "Report", Action = "Execute", Description = "执行报表查询并查看结果" },

            // 报表设计相关
            new Permission { PermissionCode = "report:design", PermissionName = "访问报表设计", Module = "Report", Action = "Design", Description = "访问报表设计管理页面" },
            new Permission { PermissionCode = "report:create", PermissionName = "创建报表", Module = "Report", Action = "Create", Description = "创建新报表" },
            new Permission { PermissionCode = "report:edit", PermissionName = "编辑报表", Module = "Report", Action = "Edit", Description = "编辑报表配置" },
            new Permission { PermissionCode = "report:delete", PermissionName = "删除报表", Module = "Report", Action = "Delete", Description = "删除报表" },
            new Permission { PermissionCode = "report:toggle", PermissionName = "停用启用报表", Module = "Report", Action = "Toggle", Description = "停用或启用报表" },
            new Permission { PermissionCode = "report:export", PermissionName = "导出报表", Module = "Report", Action = "Export", Description = "导出报表数据" },

            // 数据源管理权限
            new Permission { PermissionCode = "datasource:view", PermissionName = "查看数据源", Module = "DataSource", Action = "View", Description = "查看数据源列表" },
            new Permission { PermissionCode = "datasource:create", PermissionName = "创建数据源", Module = "DataSource", Action = "Create", Description = "创建新数据源" },
            new Permission { PermissionCode = "datasource:edit", PermissionName = "编辑数据源", Module = "DataSource", Action = "Edit", Description = "编辑数据源" },
            new Permission { PermissionCode = "datasource:delete", PermissionName = "删除数据源", Module = "DataSource", Action = "Delete", Description = "删除数据源" },
            new Permission { PermissionCode = "datasource:test", PermissionName = "测试连接", Module = "DataSource", Action = "Test", Description = "测试数据源连接" },

            // 日志管理权限
            new Permission { PermissionCode = "log:view", PermissionName = "查看日志", Module = "Log", Action = "View", Description = "查看操作日志" },
            new Permission { PermissionCode = "log:clear", PermissionName = "清空日志", Module = "Log", Action = "Clear", Description = "清空操作日志" },
            new Permission { PermissionCode = "log:export", PermissionName = "导出日志", Module = "Log", Action = "Export", Description = "导出操作日志" },

            // 备份管理权限
            new Permission { PermissionCode = "backup:view", PermissionName = "查看备份", Module = "Backup", Action = "View", Description = "查看备份列表" },
            new Permission { PermissionCode = "backup:create", PermissionName = "创建备份", Module = "Backup", Action = "Create", Description = "创建数据备份" },
            new Permission { PermissionCode = "backup:restore", PermissionName = "恢复备份", Module = "Backup", Action = "Restore", Description = "恢复数据备份" },
            new Permission { PermissionCode = "backup:delete", PermissionName = "删除备份", Module = "Backup", Action = "Delete", Description = "删除备份" },

            // 许可管理权限
            new Permission { PermissionCode = "license:view", PermissionName = "查看许可", Module = "License", Action = "View", Description = "查看许可证信息" },
            new Permission { PermissionCode = "license:activate", PermissionName = "激活许可", Module = "License", Action = "Activate", Description = "激活许可证" },

            // 系统设置权限
            new Permission { PermissionCode = "system:view", PermissionName = "查看系统设置", Module = "System", Action = "View", Description = "查看系统配置" },
            new Permission { PermissionCode = "system:edit", PermissionName = "编辑系统设置", Module = "System", Action = "Edit", Description = "编辑系统配置" }
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 强制重建所有权限（开发调试用）
    /// </summary>
    private static async Task ResetPermissionsAsync(DataForgeStudioDbContext context)
    {
        // 删除所有角色-权限关联
        var existingRolePermissions = await context.RolePermissions.ToListAsync();
        context.RolePermissions.RemoveRange(existingRolePermissions);

        // 删除所有现有权限
        var existingPermissions = await context.Permissions.ToListAsync();
        context.Permissions.RemoveRange(existingPermissions);

        await context.SaveChangesAsync();

        // 创建所有权限
        await CreateAllPermissionsAsync(context);
    }

    /// <summary>
    /// 验证并修复数据库架构
    /// 处理旧版 SQL 脚本创建的数据库与实体定义不匹配的问题
    /// </summary>
    private static async Task ValidateAndFixSchemaAsync(DataForgeStudioDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();

            // 1. 检查并修复 RolePermissions 表
            command.CommandText = @"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'RolePermissions'";

            var rolePermissionColumns = new List<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    rolePermissionColumns.Add(reader.GetString(0));
                }
            }

            // 检查是否缺少 RolePermissionId 列（旧版结构）
            if (!rolePermissionColumns.Contains("RolePermissionId", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine("检测到 RolePermissions 表结构为旧版本，正在修复...");

                // 备份现有数据
                command.CommandText = "SELECT * INTO #TempRolePermissions FROM RolePermissions";
                await command.ExecuteNonQueryAsync();

                // 删除旧表
                command.CommandText = "DROP TABLE RolePermissions";
                await command.ExecuteNonQueryAsync();

                // 创建新表结构
                command.CommandText = @"
                    CREATE TABLE RolePermissions (
                        RolePermissionId INT IDENTITY(1,1) PRIMARY KEY,
                        RoleId INT NOT NULL,
                        PermissionId INT NOT NULL,
                        CreatedBy INT NULL,
                        CreatedTime DATETIME2 DEFAULT GETDATE()
                    )";
                await command.ExecuteNonQueryAsync();

                Console.WriteLine("✓ RolePermissions 表结构已修复");
            }

            // 2. 检查并修复 Permissions 表
            command.CommandText = @"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'Permissions'";

            var permissionColumns = new List<string>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    permissionColumns.Add(reader.GetString(0));
                }
            }

            // 检查是否缺少必要的列
            var requiredColumns = new[] { "Module", "Action", "ParentId", "IsSystem", "CreatedTime" };
            var missingColumns = requiredColumns.Where(c => !permissionColumns.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();

            if (missingColumns.Count > 0)
            {
                Console.WriteLine($"检测到 Permissions 表缺少列: {string.Join(", ", missingColumns)}，正在修复...");

                foreach (var column in missingColumns)
                {
                    var columnDef = column switch
                    {
                        "Module" => "Module NVARCHAR(50) NOT NULL DEFAULT ''",
                        "Action" => "Action NVARCHAR(50) NOT NULL DEFAULT ''",
                        "ParentId" => "ParentId INT NULL",
                        "IsSystem" => "IsSystem BIT NOT NULL DEFAULT 0",
                        "CreatedTime" => "CreatedTime DATETIME2 NULL DEFAULT GETDATE()",
                        _ => null
                    };

                    if (columnDef != null)
                    {
                        command.CommandText = $"ALTER TABLE Permissions ADD {columnDef}";
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // 更新现有数据的默认值
                command.CommandText = @"
                    UPDATE Permissions SET CreatedTime = GETDATE() WHERE CreatedTime IS NULL;
                    UPDATE Permissions SET Module = 'System' WHERE Module = '' OR Module IS NULL;
                    UPDATE Permissions SET Action = 'View' WHERE Action = '' OR Action IS NULL;
                    UPDATE Permissions SET IsSystem = 1 WHERE IsSystem IS NULL";
                await command.ExecuteNonQueryAsync();

                Console.WriteLine("✓ Permissions 表结构已修复");
            }

            // 3. 检查 BackupSchedules 表是否存在
            command.CommandText = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = 'BackupSchedules'";

            var result = await command.ExecuteScalarAsync();
            var tableExists = Convert.ToInt32(result) > 0;

            if (!tableExists)
            {
                Console.WriteLine("检测到 BackupSchedules 表不存在，正在创建...");

                command.CommandText = @"
                    CREATE TABLE BackupSchedules (
                        ScheduleId INT IDENTITY(1,1) PRIMARY KEY,
                        ScheduleName NVARCHAR(100) NOT NULL,
                        ScheduleType NVARCHAR(20) NOT NULL DEFAULT 'Recurring',
                        RecurringDays NVARCHAR(50) NULL,
                        ScheduledTime NVARCHAR(10) NULL,
                        OnceDate DATETIME2 NULL,
                        RetentionCount INT NOT NULL DEFAULT 10,
                        IsEnabled BIT NOT NULL DEFAULT 1,
                        LastRunTime DATETIME2 NULL,
                        NextRunTime DATETIME2 NULL,
                        CreatedTime DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedTime DATETIME2 NULL
                    )";
                await command.ExecuteNonQueryAsync();

                Console.WriteLine("✓ BackupSchedules 表已创建");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"数据库架构验证失败: {ex.Message}");
            // 不抛出异常，允许继续执行
        }
    }
}
