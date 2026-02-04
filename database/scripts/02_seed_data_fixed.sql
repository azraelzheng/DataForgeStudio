-- DataForgeStudio V4 Seed Data Script - Fixed Version
-- This script inserts initial seed data including the root user with proper BCrypt hash

USE DataForgeStudio_V4;
GO

-- Clear existing data
DELETE FROM [dbo].[UserRoles];
DELETE FROM [dbo].[OperationLogs] WHERE [Username] IN ('root', 'admin', 'user01', 'guest');
DELETE FROM [dbo].[Users] WHERE [Username] IN ('root', 'admin', 'user01', 'guest');
DELETE FROM [dbo].[Roles] WHERE [IsSystem] = 1;
GO

-- ============================================================================
-- Insert System Roles
-- ============================================================================
INSERT INTO [dbo].[Roles] ([RoleName], [RoleCode], [Description], [Permissions], [IsSystem], [SortOrder])
VALUES
    ('超级管理员', 'SUPER_ADMIN', '*', '1', 1),
    ('管理员', 'ADMIN', '["report:*","system:*","license:*"]', '1', 2),
    ('普通用户', 'USER', '["report:view","report:query"]', '1', 3),
    ('访客', 'GUEST', '["report:view"]', '1', 4);
GO

-- ============================================================================
-- Insert Root User with BCrypt hashed password (admin123)
-- Hash: $2a$12$i7WZ22HWlMouVCSLGfH1hu2QSKFbVICzCKo4MHWXUUpcmuuRoiGam
-- ============================================================================
DECLARE @RootUserId INT;

INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [Email], [IsActive], [IsSystem], [CreatedTime])
VALUES ('root', '$2a$12$i7WZ22HWlMouVCSLGfH1hu2QSKFbVICzCKo4MHWXUUpcmuuRoiGam', '系统管理员', 'admin@dataforgerstudio.com', 1, 1, GETUTCDATE());

SELECT @RootUserId = SCOPE_IDENTITY();
GO

-- Assign Super Admin Role to root user
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT @RootUserId, [RoleId] FROM [dbo].[Roles] WHERE [RoleCode] = 'SUPER_ADMIN';
GO

-- ============================================================================
-- Insert Test Users (Password: Test123!)
-- Hash: $2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa
-- ============================================================================

-- Admin user
DECLARE @AdminUserId INT;
INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [Email], [IsActive], [IsSystem], [CreatedTime])
VALUES ('admin', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '管理员', 'admin@test.com', 1, 0, GETUTCDATE());
SELECT @AdminUserId = SCOPE_IDENTITY();

INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT @AdminUserId, [RoleId] FROM [dbo].[Roles] WHERE [RoleCode] = 'ADMIN';

-- User01
DECLARE @User01Id INT;
INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [Email], [IsActive], [IsSystem], [CreatedTime])
VALUES ('user01', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '测试用户01', 'user01@test.com', 1, 0, GETUTCDATE());
SELECT @User01Id = SCOPE_IDENTITY();

INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT @User01Id, [RoleId] FROM [dbo].[Roles] WHERE [RoleCode] = 'USER';

-- Guest user
DECLARE @GuestUserId INT;
INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [Email], [IsActive], [IsSystem], [CreatedTime])
VALUES ('guest', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '访客用户', 'guest@test.com', 1, 0, GETUTCDATE());
SELECT @GuestUserId = SCOPE_IDENTITY();

INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT @GuestUserId, [RoleId] FROM [dbo].[Roles] WHERE [RoleCode] = 'GUEST';
GO

-- ============================================================================
-- Insert Sample Data Source
-- ============================================================================
DECLARE @DataSourceId INT;

IF NOT EXISTS (SELECT * FROM [dbo].[DataSources] WHERE [DataSourceCode] = 'DS_DEFAULT')
BEGIN
    INSERT INTO [dbo].[DataSources] (
        [DataSourceName],
        [DataSourceCode],
        [DbType],
        [ServerAddress],
        [Port],
        [DatabaseName],
        [IsIntegratedSecurity],
        [IsDefault],
        [IsActive],
        [CreatedTime]
    )
    VALUES (
        'Default Data Source',
        'DS_DEFAULT',
        'SqlServer',
        'localhost',
        1433,
        'DataForgeStudio_V4',
        1,
        1,
        1,
        GETUTCDATE()
    );

    SELECT @DataSourceId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SELECT @DataSourceId = [DataSourceId] FROM [dbo].[DataSources] WHERE [DataSourceCode] = 'DS_DEFAULT';
END
GO

-- ============================================================================
-- Insert Sample Reports
-- ============================================================================

-- Report 1: User List
IF NOT EXISTS (SELECT * FROM [dbo].[Reports] WHERE [ReportCode] = 'RPT_USER_LIST')
BEGIN
    DECLARE @ReportId1 INT;

    INSERT INTO [dbo].[Reports] (
        [ReportName], [ReportCode], [ReportCategory], [DataSourceId],
        [SqlStatement], [Description], [IsPaged], [PageSize], [IsEnabled], [IsSystem], [CreatedTime]
    )
    VALUES (
        '用户列表', 'RPT_USER_LIST', '系统', @DataSourceId,
        'SELECT u.UserId, u.Username, u.RealName, u.Email, u.Phone, u.IsActive, u.CreatedTime
        FROM Users u WHERE u.IsSystem = 0 ORDER BY u.CreatedTime DESC',
        '显示系统中的所有用户', 1, 20, 1, 1, GETUTCDATE()
    );

    SELECT @ReportId1 = SCOPE_IDENTITY();

    INSERT INTO [dbo].[ReportFields] ([ReportId], [FieldName], [DisplayName], [DataType], [Width], [IsVisible], [IsSortable], [Align], [SortOrder])
    VALUES
        (@ReportId1, 'UserId', '用户ID', 'Number', 80, 1, 1, 'center', 1),
        (@ReportId1, 'Username', '用户名', 'String', 120, 1, 1, 'left', 2),
        (@ReportId1, 'RealName', '真实姓名', 'String', 120, 1, 1, 'left', 3),
        (@ReportId1, 'Email', '邮箱', 'String', 200, 1, 1, 'left', 4),
        (@ReportId1, 'Phone', '手机号', 'String', 120, 1, 1, 'left', 5),
        (@ReportId1, 'IsActive', '状态', 'Boolean', 80, 1, 1, 'center', 6),
        (@ReportId1, 'CreatedTime', '创建时间', 'DateTime', 160, 1, 1, 'center', 7);
END
GO

-- Report 2: Role List
IF NOT EXISTS (SELECT * FROM [dbo].[Reports] WHERE [ReportCode] = 'RPT_ROLE_LIST')
BEGIN
    DECLARE @ReportId2 INT;

    INSERT INTO [dbo].[Reports] (
        [ReportName], [ReportCode], [ReportCategory], [DataSourceId],
        [SqlStatement], [Description], [IsPaged], [PageSize], [IsEnabled], [IsSystem], [CreatedTime]
    )
    VALUES (
        '角色列表', 'RPT_ROLE_LIST', '系统', @DataSourceId,
        'SELECT r.RoleId, r.RoleName, r.RoleCode, r.Description, r.IsSystem, r.IsActive, r.CreatedTime,
        (SELECT COUNT(*) FROM UserRoles ur WHERE ur.RoleId = r.RoleId) AS UserCount
        FROM Roles r ORDER BY r.SortOrder, r.RoleName',
        '显示系统中的所有角色', 1, 20, 1, 1, GETUTCDATE()
    );

    SELECT @ReportId2 = SCOPE_IDENTITY();

    INSERT INTO [dbo].[ReportFields] ([ReportId], [FieldName], [DisplayName], [DataType], [Width], [IsVisible], [IsSortable], [Align], [SortOrder])
    VALUES
        (@ReportId2, 'RoleId', '角色ID', 'Number', 80, 1, 1, 'center', 1),
        (@ReportId2, 'RoleName', '角色名称', 'String', 150, 1, 1, 'left', 2),
        (@ReportId2, 'RoleCode', '角色代码', 'String', 120, 1, 1, 'left', 3),
        (@ReportId2, 'Description', '描述', 'String', 200, 1, 1, 'left', 4),
        (@ReportId2, 'UserCount', '用户数', 'Number', 80, 1, 1, 'center', 5),
        (@ReportId2, 'IsActive', '状态', 'Boolean', 80, 1, 1, 'center', 6),
        (@ReportId2, 'CreatedTime', '创建时间', 'DateTime', 160, 1, 1, 'center', 7);
END
GO

-- Report 3: Login Statistics
IF NOT EXISTS (SELECT * FROM [dbo].[Reports] WHERE [ReportCode] = 'RPT_LOGIN_STATS')
BEGIN
    DECLARE @ReportId3 INT;

    INSERT INTO [dbo].[Reports] (
        [ReportName], [ReportCode], [ReportCategory], [DataSourceId],
        [SqlStatement], [Description], [IsPaged], [PageSize], [IsEnabled], [IsSystem], [CreatedTime]
    )
    VALUES (
        '登录统计', 'RPT_LOGIN_STATS', '统计', @DataSourceId,
        'SELECT CONVERT(VARCHAR(10), ll.LoginTime, 120) AS LoginDate,
        COUNT(*) AS LoginCount, COUNT(DISTINCT ll.Username) AS UniqueUsers
        FROM LoginLogs ll WHERE ll.IsSuccess = 1
        ORDER BY LoginDate DESC',
        '用户登录统计报表', 1, 30, 1, 1, GETUTCDATE()
    );

    SELECT @ReportId3 = SCOPE_IDENTITY();

    INSERT INTO [dbo].[ReportFields] ([ReportId], [FieldName], [DisplayName], [DataType], [Width], [IsVisible], [IsSortable], [Align], [SortOrder])
    VALUES
        (@ReportId3, 'LoginDate', '登录日期', 'String', 120, 1, 1, 'center', 1),
        (@ReportId3, 'LoginCount', '登录次数', 'Number', 100, 1, 1, 'center', 2),
        (@ReportId3, 'UniqueUsers', '独立用户数', 'Number', 100, 1, 1, 'center', 3);
END
GO

-- ============================================================================
-- Insert Sample Operation Logs
-- ============================================================================
INSERT INTO [dbo].[OperationLogs] ([Username], [Action], [Module], [Description], [IpAddress], [CreatedTime])
SELECT 'root', 'CREATE', 'USER', '创建超级管理员 root', '127.0.0.1', GETUTCDATE()
WHERE NOT EXISTS (SELECT * FROM [dbo].[OperationLogs] WHERE [Username] = 'root' AND [Action] = 'CREATE' AND [Module] = 'USER');
GO

INSERT INTO [dbo].[OperationLogs] ([Username], [Action], [Module], [Description], [IpAddress], [CreatedTime])
SELECT 'root', 'CREATE', 'ROLE', '创建系统角色', '127.0.0.1', GETUTCDATE()
WHERE NOT EXISTS (SELECT * FROM [dbo].[OperationLogs] WHERE [Username] = 'root' AND [Action] = 'CREATE' AND [Module] = 'ROLE');
GO

INSERT INTO [dbo].[OperationLogs] ([Username], [Action], [Module], [Description], [IpAddress], [CreatedTime])
SELECT 'root', 'LOGIN', 'AUTH', '系统初始化完成', '127.0.0.1', GETUTCDATE()
WHERE NOT EXISTS (SELECT * FROM [dbo].[OperationLogs] WHERE [Username] = 'root' AND [Action] = 'LOGIN' AND [Description] = '系统初始化完成');
GO

PRINT 'Seed data inserted successfully!';
PRINT '============================================================================';
PRINT 'Login Credentials:';
PRINT '  root / admin123   - Super Administrator (hidden from UI)';
PRINT '  admin / Test123!  - Administrator';
PRINT '  user01 / Test123! - Regular User';
PRINT '  guest / Test123!  - Guest User';
PRINT '============================================================================';
GO
-- Verify data
SELECT 'Users:' as Info, COUNT(*) as Count FROM Users
UNION ALL
SELECT 'Roles:', COUNT(*) FROM Roles
UNION ALL
SELECT 'Reports:', COUNT(*) FROM Reports
UNION ALL
SELECT 'DataSources:', COUNT(*) FROM DataSources
UNION ALL
SELECT 'UserRoles:', COUNT(*) FROM UserRoles;
GO
-- Show users
SELECT '=== Users ===' as Info, Username, RealName FROM Users;
GO
