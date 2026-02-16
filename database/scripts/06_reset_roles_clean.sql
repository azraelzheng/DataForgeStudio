-- 彻底重置角色数据 - 只保留标准的 4 个角色
USE DataForgeStudio_V4;
GO

PRINT '============================================================================';
PRINT '开始重置角色数据...';
PRINT '============================================================================';
GO

-- 显示当前所有角色
PRINT '=== 当前所有角色 ===';
SELECT RoleId, RoleName, RoleCode, IsSystem, CreatedTime
FROM Roles
ORDER BY CreatedTime;
GO

-- 第一步：删除角色权限关联（先删除这个，因为有外键约束）
PRINT '删除角色权限关联...';
DELETE FROM [dbo].[RolePermissions];
GO

PRINT '角色权限关联已删除，影响行数: ' + CAST(@@ROWCOUNT AS VARCHAR);
GO

-- 第二步：删除所有用户角色关联
PRINT '删除所有用户角色关联...';
DELETE FROM [dbo].[UserRoles];
GO

PRINT '用户角色关联已删除，影响行数: ' + CAST(@@ROWCOUNT AS VARCHAR);
GO

-- 第三步：删除所有角色
PRINT '删除所有角色...';
DELETE FROM [dbo].[Roles];
GO

PRINT '角色已删除，影响行数: ' + CAST(@@ROWCOUNT AS VARCHAR);
GO

-- 第四步：重新插入标准的 4 个角色
PRINT '插入标准角色...';
INSERT INTO [dbo].[Roles] ([RoleName], [RoleCode], [Description], [IsSystem], [IsActive], [SortOrder], [CreatedTime])
VALUES
    ('超级管理员', 'ROLE_SUPER_ADMIN', '系统超级管理员，拥有所有权限（UI中隐藏）', 1, 1, 1, GETUTCDATE()),
    ('管理员', 'ROLE_ADMIN', '系统管理员，拥有所有管理权限', 1, 1, 2, GETUTCDATE()),
    ('报表设计组', 'ROLE_REPORT_DESIGNER', '报表设计人员，可设计和执行报表', 1, 1, 3, GETUTCDATE()),
    ('报表查询组', 'ROLE_REPORT_VIEWER', '报表查询人员，只能查看和执行报表', 1, 1, 4, GETUTCDATE());
GO

PRINT '标准角色已插入';
GO

-- 第五步：为超级管理员角色分配所有权限
PRINT '为超级管理员角色分配所有权限...';
DECLARE @SuperAdminRoleId INT;
SELECT @SuperAdminRoleId = RoleId FROM Roles WHERE RoleCode = 'ROLE_SUPER_ADMIN';

INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId], [CreatedBy], [CreatedTime])
SELECT @SuperAdminRoleId, PermissionId, 'system', GETUTCDATE()
FROM Permissions;
GO

PRINT '超级管理员权限已分配';
GO

-- 第六步：为 root 用户分配超级管理员角色
PRINT '为 root 用户分配超级管理员角色...';
DECLARE @RootUserId INT;
SELECT @RootUserId = UserId FROM Users WHERE Username = 'root';

IF @RootUserId IS NOT NULL AND @SuperAdminRoleId IS NOT NULL
BEGIN
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
    VALUES (@RootUserId, @SuperAdminRoleId);
    PRINT 'root 用户已分配超级管理员角色';
END
ELSE
BEGIN
    PRINT '警告: root 用户或超级管理员角色不存在！';
END
GO

-- 显示最终结果
PRINT '';
PRINT '============================================================================';
PRINT '重置完成！最终角色列表：';
PRINT '============================================================================';
SELECT RoleId, RoleName, RoleCode, IsSystem, IsActive, SortOrder, CreatedTime
FROM Roles
ORDER BY SortOrder;
GO

PRINT '';
PRINT '============================================================================';
PRINT '用户角色关联：';
PRINT '============================================================================';
SELECT u.Username, r.RoleName, r.RoleCode
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.RoleId
ORDER BY u.Username;
GO

PRINT '';
PRINT '============================================================================';
PRINT '重置完成！现在数据库中只有 4 个标准角色。';
PRINT '请在浏览器中按 Ctrl+Shift+R 强制刷新页面。';
PRINT '============================================================================';
GO
