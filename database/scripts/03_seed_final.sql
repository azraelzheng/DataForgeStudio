-- DataForgeStudio V4 - Final Seed Script
USE DataForgeStudio_V4;
GO

-- Clear existing seed data
DELETE FROM [dbo].[UserRoles] WHERE [UserId] IN (SELECT [UserId] FROM [dbo].[Users] WHERE [Username] IN ('root', 'admin', 'user01', 'guest'));
DELETE FROM [dbo].[Users] WHERE [Username] IN ('root', 'admin', 'user01', 'guest');
DELETE FROM [dbo].[Roles] WHERE [IsSystem] = 1;
DELETE FROM [dbo].[Reports] WHERE [IsSystem] = 1;
DELETE FROM [dbo].[DataSources] WHERE [DataSourceCode] = 'DS_DEFAULT';
GO

PRINT 'Inserting System Roles...';
-- Insert System Roles
INSERT INTO [dbo].[Roles] ([RoleName], [RoleCode], [Description], [Permissions], [IsSystem], [SortOrder])
VALUES
    ('超级管理员', 'SUPER_ADMIN', '拥有所有权限', '*', 1, 1),
    ('管理员', 'ADMIN', '系统管理员', '["report:*","system:*","license:*"]', 1, 2),
    ('普通用户', 'USER', '普通用户', '["report:view","report:query"]', 1, 3),
    ('访客', 'GUEST', '访客用户', '["report:view"]', 1, 4);
PRINT 'Roles inserted: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';
GO

PRINT 'Inserting Users...';
-- Insert root user (password: admin123)
INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [Email], [IsActive], [IsSystem], [CreatedTime])
VALUES ('root', '$2a$12$i7WZ22HWlMouVCSLGfH1hu2QSKFbVICzCKo4MHWXUUpcmuuRoiGam', '系统管理员', 'admin@dataforgerstudio.com', 1, 1, GETUTCDATE());
PRINT 'Root user inserted, UserId = ' + CAST(SCOPE_IDENTITY() AS VARCHAR);
GO

-- Insert test users (password: Test123!)
INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [Email], [IsActive], [IsSystem], [CreatedTime])
VALUES
    ('admin', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '管理员', 'admin@test.com', 1, 0, GETUTCDATE()),
    ('user01', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '测试用户01', 'user01@test.com', 1, 0, GETUTCDATE()),
    ('guest', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '访客用户', 'guest@test.com', 1, 0, GETUTCDATE());
PRINT 'Test users inserted: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';
GO

PRINT 'Assigning roles to users...';
-- Assign Super Admin to root
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT u.[UserId], r.[RoleId]
FROM [dbo].[Users] u, [dbo].[Roles] r
WHERE u.[Username] = 'root' AND r.[RoleCode] = 'SUPER_ADMIN';

-- Assign Admin to admin
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT u.[UserId], r.[RoleId]
FROM [dbo].[Users] u, [dbo].[Roles] r
WHERE u.[Username] = 'admin' AND r.[RoleCode] = 'ADMIN';

-- Assign User to user01
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT u.[UserId], r.[RoleId]
FROM [dbo].[Users] u, [dbo].[Roles] r
WHERE u.[Username] = 'user01' AND r.[RoleCode] = 'USER';

-- Assign Guest to guest
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT u.[UserId], r.[RoleId]
FROM [dbo].[Users] u, [dbo].[Roles] r
WHERE u.[Username] = 'guest' AND r.[RoleCode] = 'GUEST';
PRINT 'User roles assigned: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';
GO

PRINT 'Creating default data source...';
-- Insert Default Data Source
INSERT INTO [dbo].[DataSources] (
    [DataSourceName], [DataSourceCode], [DbType], [ServerAddress], [Port],
    [DatabaseName], [IsIntegratedSecurity], [IsDefault], [IsActive], [CreatedTime]
)
VALUES (
    'Default Data Source', 'DS_DEFAULT', 'SqlServer', 'localhost', 1433,
    'DataForgeStudio_V4', 1, 1, 1, GETUTCDATE()
);
PRINT 'Default data source created';
GO

PRINT 'Creating sample reports...';
-- Insert User List Report
DECLARE @DS_ID INT;
SELECT @DS_ID = [DataSourceId] FROM [dbo].[DataSources] WHERE [DataSourceCode] = 'DS_DEFAULT';

INSERT INTO [dbo].[Reports] (
    [ReportName], [ReportCode], [ReportCategory], [DataSourceId],
    [SqlStatement], [Description], [IsPaged], [PageSize], [IsEnabled], [IsSystem], [CreatedTime]
)
VALUES (
    '用户列表', 'RPT_USER_LIST', '系统', @DS_ID,
    'SELECT u.UserId, u.Username, u.RealName, u.Email, u.IsActive, u.CreatedTime
    FROM Users u WHERE u.IsSystem = 0 ORDER BY u.CreatedTime DESC',
    '显示系统中的所有用户', 1, 20, 1, 1, GETUTCDATE()
);
PRINT 'User List report created';
GO

-- Insert Role List Report
INSERT INTO [dbo].[Reports] (
    [ReportName], [ReportCode], [ReportCategory], [DataSourceId],
    [SqlStatement], [Description], [IsPaged], [PageSize], [IsEnabled], [IsSystem], [CreatedTime]
)
VALUES (
    '角色列表', 'RPT_ROLE_LIST', '系统', @DS_ID,
    'SELECT r.RoleId, r.RoleName, r.RoleCode, r.Description, r.IsActive,
    (SELECT COUNT(*) FROM UserRoles ur WHERE ur.RoleId = r.RoleId) AS UserCount
    FROM Roles r ORDER BY r.SortOrder',
    '显示系统中的所有角色', 1, 20, 1, 1, GETUTCDATE()
);
PRINT 'Role List report created';
GO

PRINT 'Creating operation logs...';
INSERT INTO [dbo].[OperationLogs] ([Username], [Action], [Module], [Description], [IpAddress], [CreatedTime])
VALUES ('root', 'SYSTEM', 'INIT', '数据库初始化完成', '127.0.0.1', GETUTCDATE());
PRINT 'Operation log created';
GO

PRINT '';
PRINT '============================================================================';
PRINT 'Seed Data Completed Successfully!';
PRINT '============================================================================';
PRINT 'Login Credentials:';
PRINT '  Username   Password    Role';
PRINT '  --------   --------    ----';
PRINT '  root       admin123   Super Admin (hidden from UI)';
PRINT '  admin      Test123!    Administrator';
PRINT '  user01     Test123!    Regular User';
PRINT '  guest      Test123!    Guest';
PRINT '============================================================================';
PRINT '';

-- Show summary
SELECT '== Database Summary ==' as '';
SELECT 'Users: ' + CAST(COUNT(*) AS VARCHAR) FROM Users
UNION ALL
SELECT 'Roles: ' + CAST(COUNT(*) AS VARCHAR) FROM Roles
UNION ALL
SELECT 'Reports: ' + CAST(COUNT(*) AS VARCHAR) FROM Reports
UNION ALL
SELECT 'DataSources: ' + CAST(COUNT(*) AS VARCHAR) FROM DataSources
UNION ALL
SELECT 'UserRoles: ' + CAST(COUNT(*) AS VARCHAR) FROM UserRoles;
PRINT '';

-- Show users with roles
SELECT '== Users ==' as '', Username as 'Username', RealName as 'RealName'
FROM Users
ORDER BY UserId;
GO
