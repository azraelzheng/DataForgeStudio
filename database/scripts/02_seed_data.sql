-- DataForgeStudio V4 Seed Data Script
-- This script inserts initial seed data including the root user with proper BCrypt hash

USE DataForgeStudio_V4;
GO

-- ============================================================================
-- Root User with BCrypt hashed password (admin123)
-- Hash generated with BCrypt work factor 12
-- ============================================================================
DECLARE @RootUserId INT;

-- Delete existing root user if exists (for re-seeding)
IF EXISTS (SELECT * FROM [dbo].[Users] WHERE [Username] = 'root')
BEGIN
    DECLARE @RootId INT = (SELECT [UserId] FROM [dbo].[Users] WHERE [Username] = 'root');
    DELETE FROM [dbo].[UserRoles] WHERE [UserId] = @RootId;
    DELETE FROM [dbo].[Users] WHERE [Username] = 'root';
END
GO

-- Insert root user with password 'admin123'
-- BCrypt hash for 'admin123' with work factor 12: $2a$12$i7WZ22HWlMouVCSLGfH1hu2QSKFbVICzCKo4MHWXUUpcmuuRoiGam
INSERT INTO [dbo].[Users] (
    [Username],
    [PasswordHash],
    [RealName],
    [Email],
    [IsActive],
    [IsSystem],
    [CreatedTime]
)
VALUES (
    'root',
    '$2a$12$i7WZ22HWlMouVCSLGfH1hu2QSKFbVICzCKo4MHWXUUpcmuuRoiGam',
    '系统管理员',
    'admin@dataforgerstudio.com',
    1,
    1,
    GETUTCDATE()
);

SET @RootUserId = SCOPE_IDENTITY();
GO

-- ============================================================================
-- System Roles
-- ============================================================================
DECLARE @SuperAdminRoleId INT, @AdminRoleId INT, @UserId INT, @GuestRoleId INT;

-- Delete existing system roles
DELETE FROM [dbo].[Roles] WHERE [IsSystem] = 1;

-- Insert Super Admin Role
INSERT INTO [dbo].[Roles] ([RoleName], [RoleCode], [Description], [Permissions], [IsSystem], [SortOrder])
VALUES ('超级管理员', 'SUPER_ADMIN', '*', '1', 1);

SET @SuperAdminRoleId = SCOPE_IDENTITY();

-- Insert Admin Role
INSERT INTO [dbo].[Roles] ([RoleName], [RoleCode], [Description], [Permissions], [IsSystem], [SortOrder])
VALUES ('管理员', 'ADMIN', '["report:*","system:*","license:*"]', '1', 2);

SET @AdminRoleId = SCOPE_IDENTITY();

-- Insert User Role
INSERT INTO [dbo].[Roles] ([RoleName], [RoleCode], [Description], [Permissions], [IsSystem], [SortOrder])
VALUES ('普通用户', 'USER', '["report:view","report:query"]', '1', 3);

SET @UserId = SCOPE_IDENTITY();

-- Insert Guest Role
INSERT INTO [dbo].[Roles] ([RoleName], [RoleCode], [Description], [Permissions], [IsSystem], [SortOrder])
VALUES ('访客', 'GUEST', '["report:view"]', '1', 4);

SET @GuestRoleId = SCOPE_IDENTITY();

-- ============================================================================
-- Assign Super Admin Role to root user
-- ============================================================================
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
VALUES (@RootUserId, @SuperAdminRoleId);
GO

-- ============================================================================
-- Sample Users for Testing
-- ============================================================================

-- Create test users with different roles
-- Password for all test users is: Test123!

IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [Username] = 'admin')
BEGIN
    INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [Email], [IsActive], [IsSystem], [CreatedTime])
    VALUES ('admin', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '管理员', 'admin@test.com', 1, 0, GETUTCDATE());

    DECLARE @AdminUserId INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
    VALUES (@AdminUserId, @AdminRoleId);
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [Username] = 'user01')
BEGIN
    INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [Email], [IsActive], [IsSystem], [CreatedTime])
    VALUES ('user01', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '测试用户01', 'user01@test.com', 1, 0, GETUTCDATE());

    DECLARE @User01Id INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
    VALUES (@User01Id, @UserId);
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [Username] = 'guest')
BEGIN
    INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [Email], [IsActive], [IsSystem], [CreatedTime])
    VALUES ('guest', '$2a$12$73x2blx6jSTSgimznE/OR.Gy2B223NICaiX9cuK40troxxJoUL8fa', '访客用户', 'guest@test.com', 1, 0, GETUTCDATE());

    DECLARE @GuestUserId INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
    VALUES (@GuestUserId, @GuestRoleId);
END
GO

-- ============================================================================
-- Sample DataSource (points to the current database)
-- ============================================================================
DECLARE @DataSourceId INT;

IF NOT EXISTS (SELECT * FROM [dbo].[DataSources] WHERE [DataSourceCode] = 'DS_DEFAULT')
BEGIN
    -- Encrypt password for 'sa' (if using SQL Server auth) or leave empty for Windows auth
    -- For Windows Integrated Security, password is NULL

    INSERT INTO [dbo].[DataSources] (
        [DataSourceName],
        [DataSourceCode],
        [DbType],
        [ServerAddress],
        [Port],
        [DatabaseName],
        [Username],
        [Password],
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
        NULL,
        NULL,
        1,
        1,
        1,
        GETUTCDATE()
    );

    SET @DataSourceId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SET @DataSourceId = (SELECT [DataSourceId] FROM [dbo].[DataSources] WHERE [DataSourceCode] = 'DS_DEFAULT');
END
GO

-- ============================================================================
-- Sample Reports
-- ============================================================================

-- Sample Report 1: User List
DECLARE @ReportId INT;

IF NOT EXISTS (SELECT * FROM [dbo].[Reports] WHERE [ReportCode] = 'RPT_USER_LIST')
BEGIN
    INSERT INTO [dbo].[Reports] (
        [ReportName],
        [ReportCode],
        [ReportCategory],
        [DataSourceId],
        [SqlStatement],
        [Description],
        [IsPaged],
        [PageSize],
        [IsEnabled],
        [IsSystem],
        [CreatedTime]
    )
    VALUES (
        '用户列表',
        'RPT_USER_LIST',
        '系统',
        @DataSourceId,
        'SELECT
            u.UserId,
            u.Username,
            u.RealName,
            u.Email,
            u.Phone,
            u.IsActive,
            u.CreatedTime
        FROM Users u
        WHERE u.IsSystem = 0
        ORDER BY u.CreatedTime DESC',
        '显示系统中的所有用户',
        1,
        20,
        1,
        1,
        GETUTCDATE()
    );

    SET @ReportId = SCOPE_IDENTITY();

    -- Insert fields for User List report
    INSERT INTO [dbo].[ReportFields] ([ReportId], [FieldName], [DisplayName], [DataType], [Width], [IsVisible], [IsSortable], [Align], [SortOrder])
    VALUES
        (@ReportId, 'UserId', '用户ID', 'Number', 80, 1, 1, 'center', 1),
        (@ReportId, 'Username', '用户名', 'String', 120, 1, 1, 'left', 2),
        (@ReportId, 'RealName', '真实姓名', 'String', 120, 1, 1, 'left', 3),
        (@ReportId, 'Email', '邮箱', 'String', 200, 1, 1, 'left', 4),
        (@ReportId, 'Phone', '手机号', 'String', 120, 1, 1, 'left', 5),
        (@ReportId, 'IsActive', '状态', 'Boolean', 80, 1, 1, 'center', 6),
        (@ReportId, 'CreatedTime', '创建时间', 'DateTime', 160, 1, 1, 'center', 7);
END
GO

-- Sample Report 2: Role List
IF NOT EXISTS (SELECT * FROM [dbo].[Reports] WHERE [ReportCode] = 'RPT_ROLE_LIST')
BEGIN
    INSERT INTO [dbo].[Reports] (
        [ReportName],
        [ReportCode],
        [ReportCategory],
        [DataSourceId],
        [SqlStatement],
        [Description],
        [IsPaged],
        [PageSize],
        [IsEnabled],
        [IsSystem],
        [CreatedTime]
    )
    VALUES (
        '角色列表',
        'RPT_ROLE_LIST',
        '系统',
        @DataSourceId,
        'SELECT
            r.RoleId,
            r.RoleName,
            r.RoleCode,
            r.Description,
            r.IsSystem,
            r.IsActive,
            r.CreatedTime,
            (SELECT COUNT(*) FROM UserRoles ur WHERE ur.RoleId = r.RoleId) AS UserCount
        FROM Roles r
        ORDER BY r.SortOrder, r.RoleName',
        '显示系统中的所有角色',
        1,
        20,
        1,
        1,
        GETUTCDATE()
    );

    SET @ReportId = SCOPE_IDENTITY();

    -- Insert fields for Role List report
    INSERT INTO [dbo].[ReportFields] ([ReportId], [FieldName], [DisplayName], [DataType], [Width], [IsVisible], [IsSortable], [Align], [SortOrder])
    VALUES
        (@ReportId, 'RoleId', '角色ID', 'Number', 80, 1, 1, 'center', 1),
        (@ReportId, 'RoleName', '角色名称', 'String', 150, 1, 1, 'left', 2),
        (@ReportId, 'RoleCode', '角色代码', 'String', 120, 1, 1, 'left', 3),
        (@ReportId, 'Description', '描述', 'String', 200, 1, 1, 'left', 4),
        (@ReportId, 'UserCount', '用户数', 'Number', 80, 1, 1, 'center', 5),
        (@ReportId, 'IsActive', '状态', 'Boolean', 80, 1, 1, 'center', 6),
        (@ReportId, 'CreatedTime', '创建时间', 'DateTime', 160, 1, 1, 'center', 7);
END
GO

-- Sample Report 3: Login Statistics (with date parameter)
BEGIN
    INSERT INTO [dbo].[Reports] (
        [ReportName],
        [ReportCode],
        [ReportCategory],
        [DataSourceId],
        [SqlStatement],
        [Description],
        [IsPaged],
        [PageSize],
        [IsEnabled],
        [IsSystem],
        [CreatedTime]
    )
    VALUES (
        '登录统计',
        'RPT_LOGIN_STATS',
        '统计',
        @DataSourceId,
        'SELECT
            CONVERT(VARCHAR(10), ll.LoginTime, 120) AS LoginDate,
            COUNT(*) AS LoginCount,
            COUNT(DISTINCT ll.Username) AS UniqueUsers
        FROM LoginLogs ll
        WHERE ll.IsSuccess = 1
            AND (@StartDate IS NULL OR ll.LoginTime >= @StartDate)
            AND (@EndDate IS NULL OR ll.LoginTime <= @EndDate)
        GROUP BY CONVERT(VARCHAR(10), ll.LoginTime, 120)
        ORDER BY LoginDate DESC',
        '用户登录统计报表',
        1,
        30,
        1,
        1,
        GETUTCDATE()
    );

    SET @ReportId = SCOPE_IDENTITY();

    -- Insert fields for Login Stats report
    INSERT INTO [dbo].[ReportFields] ([ReportId], [FieldName], [DisplayName], [DataType], [Width], [IsVisible], [IsSortable], [Align], [SortOrder])
    VALUES
        (@ReportId, 'LoginDate', '登录日期', 'String', 120, 1, 1, 'center', 1),
        (@ReportId, 'LoginCount', '登录次数', 'Number', 100, 1, 1, 'center', 2),
        (@ReportId, 'UniqueUsers', '独立用户数', 'Number', 100, 1, 1, 'center', 3);

    -- Insert parameters for Login Stats report
    INSERT INTO [dbo].[ReportParameters] ([ReportId], [ParameterName], [DisplayName], [DataType], [InputType], [IsRequired], [DefaultValue], [SortOrder])
    VALUES
        (@ReportId, 'StartDate', '开始日期', 'DateTime', 'Date', 0, NULL, 1),
        (@ReportId, 'EndDate', '结束日期', 'DateTime', 'Date', 0, NULL, 2);
END
GO

-- ============================================================================
-- Sample Operation Logs
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
