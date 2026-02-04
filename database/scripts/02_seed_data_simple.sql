-- DataForgeStudio V4 Seed Data Script - Simple Version
USE DataForgeStudio_V4;
GO

-- Clear existing data
DELETE FROM [dbo].[UserRoles];
DELETE FROM [dbo].[OperationLogs] WHERE [Username] IN ('root', 'admin', 'user01', 'guest');
DELETE FROM [dbo].[Users] WHERE [Username] IN ('root', 'admin', 'user01', 'guest');
DELETE FROM [dbo].[Roles] WHERE [IsSystem] = 1;
DELETE FROM [dbo].[DataSources] WHERE [DataSourceCode] = 'DS_DEFAULT';
GO

-- Insert System Roles
INSERT INTO [dbo].[Roles] ([RoleName], [RoleCode], [Description], [Permissions], [IsSystem], [SortOrder])
VALUES
    ('超级管理员', 'SUPER_ADMIN', '拥有所有权限', '*', 1, 1),
    ('管理员', 'ADMIN', '系统管理员', '["report:*","system:*","license:*"]', 1, 2),
    ('普通用户', 'USER', '普通用户', '["report:view","report:query"]', 1, 3),
    ('访客', 'GUEST', '访客用户', '["report:view"]', 1, 4);
GO

-- Insert Root User (password: admin123)
INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [Email], [IsActive], [IsSystem], [CreatedTime])
VALUES ('root', '$2a$12$i7WZ22HWlMouVCSLGfH1hu2QSKFbVICzCKo4MHWXUUpcmuuRoiGam', '系统管理员', 'admin@dataforgerstudio.com', 1, 1, GETUTCDATE());
GO

-- Assign Super Admin Role to root user
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT u.[UserId], r.[RoleId]
FROM [dbo].[Users] u
CROSS JOIN [dbo].[Roles] r
WHERE u.[Username] = 'root' AND r.[RoleCode] = 'SUPER_ADMIN';
GO

-- Insert Test Users (password: Test123!)
INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [Email], [IsActive], [IsSystem], [CreatedTime])
VALUES
    ('admin', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '管理员', 'admin@test.com', 1, 0, GETUTCDATE()),
    ('user01', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '测试用户01', 'user01@test.com', 1, 0, GETUTCDATE()),
    ('guest', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '访客用户', 'guest@test.com', 1, 0, GETUTCDATE());
GO

-- Assign roles to test users
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT u.[UserId], r.[RoleId]
FROM [dbo].[Users] u
CROSS JOIN [dbo].[Roles] r
WHERE u.[Username] = 'admin' AND r.[RoleCode] = 'ADMIN';
GO

INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT u.[UserId], r.[RoleId]
FROM [dbo].[Users] u
CROSS JOIN [dbo].[Roles] r
WHERE u.[Username] = 'user01' AND r.[RoleCode] = 'USER';
GO

INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT u.[UserId], r.[RoleId]
FROM [dbo].[Users] u
CROSS JOIN [dbo].[Roles] r
WHERE u.[Username] = 'guest' AND r.[RoleCode] = 'GUEST';
GO

-- Insert Default Data Source
INSERT INTO [dbo].[DataSources] (
    [DataSourceName], [DataSourceCode], [DbType], [ServerAddress], [Port],
    [DatabaseName], [IsIntegratedSecurity], [IsDefault], [IsActive], [CreatedTime]
)
VALUES (
    'Default Data Source', 'DS_DEFAULT', 'SqlServer', 'localhost', 1433,
    'DataForgeStudio_V4', 1, 1, 1, GETUTCDATE()
);
GO

-- Insert Sample Report: User List
INSERT INTO [dbo].[Reports] (
    [ReportName], [ReportCode], [ReportCategory], [DataSourceId],
    [SqlStatement], [Description], [IsPaged], [PageSize], [IsEnabled], [IsSystem], [CreatedTime]
)
VALUES (
    '用户列表', 'RPT_USER_LIST', '系统',
    (SELECT [DataSourceId] FROM [dbo].[DataSources] WHERE [DataSourceCode] = 'DS_DEFAULT'),
    'SELECT u.UserId, u.Username, u.RealName, u.Email, u.IsActive, u.CreatedTime
    FROM Users u WHERE u.IsSystem = 0 ORDER BY u.CreatedTime DESC',
    '显示系统中的所有用户', 1, 20, 1, 1, GETUTCDATE()
);
GO

-- Insert Sample Report: Role List
INSERT INTO [dbo].[Reports] (
    [ReportName], [ReportCode], [ReportCategory], [DataSourceId],
    [SqlStatement], [Description], [IsPaged], [PageSize], [IsEnabled], [IsSystem], [CreatedTime]
)
VALUES (
    '角色列表', 'RPT_ROLE_LIST', '系统',
    (SELECT [DataSourceId] FROM [dbo].[DataSources] WHERE [DataSourceCode] = 'DS_DEFAULT'),
    'SELECT r.RoleId, r.RoleName, r.RoleCode, r.Description, r.IsActive,
    (SELECT COUNT(*) FROM UserRoles ur WHERE ur.RoleId = r.RoleId) AS UserCount
    FROM Roles r ORDER BY r.SortOrder',
    '显示系统中的所有角色', 1, 20, 1, 1, GETUTCDATE()
);
GO

-- Insert Operation Logs
INSERT INTO [dbo].[OperationLogs] ([Username], [Action], [Module], [Description], [IpAddress], [CreatedTime])
SELECT 'root', 'CREATE', 'SYSTEM', '系统初始化完成', '127.0.0.1', GETUTCDATE()
WHERE NOT EXISTS (SELECT * FROM [dbo].[OperationLogs] WHERE [Description] = '系统初始化完成');
GO

-- Verify and display results
PRINT '============================================================================';
PRINT 'Seed Data Inserted Successfully!';
PRINT '============================================================================';
PRINT 'Login Credentials:';
PRINT '  Username / Password';
PRINT '  root / admin123   - Super Administrator (hidden from UI)';
PRINT '  admin / Test123!  - Administrator';
PRINT '  user01 / Test123! - Regular User';
PRINT '  guest / Test123!  - Guest User';
PRINT '============================================================================';
PRINT '';

-- Show data summary
SELECT 'Database Summary:' as Info, '' as Value
UNION ALL
SELECT 'Users:', CAST(COUNT(*) AS VARCHAR) + ' users' FROM Users
UNION ALL
SELECT 'Roles:', CAST(COUNT(*) AS VARCHAR) + ' roles' FROM Roles
UNION ALL
SELECT 'Reports:', CAST(COUNT(*) AS VARCHAR) + ' reports' FROM Reports
UNION ALL
SELECT 'DataSources:', CAST(COUNT(*) AS VARCHAR) + ' datasources' FROM DataSources
UNION ALL
SELECT 'UserRoles:', CAST(COUNT(*) AS VARCHAR) + ' user-role mappings' FROM UserRoles;
GO

-- Show all users
SELECT '' as Separator, '=== All Users ===' as Info, '' as Value
UNION ALL
SELECT '', 'Username: ' + Username + ' | RealName: ' + RealName + ' | IsActive: ' + CAST(IsActive AS VARCHAR), ''
FROM Users
ORDER BY UserId;
GO
