-- =============================================
-- DataForgeStudio V1.1.0 Dashboard Migration
-- =============================================
-- 创建时间: 2026-03-02
-- 功能说明: 创建大屏展示功能所需的数据库表和权限配置
-- 兼容性: SQL Server 2005+

-- 创建大屏主表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Dashboards]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Dashboards] (
        [DashboardId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Theme] NVARCHAR(20) NOT NULL DEFAULT 'dark',
        [RefreshInterval] INT NOT NULL DEFAULT 30,
        [IsPublic] BIT NOT NULL DEFAULT 0,
        [LayoutConfig] NVARCHAR(MAX) NULL,
        [ThemeConfig] NVARCHAR(MAX) NULL,
        [CreatedBy] INT NULL,
        [CreatedTime] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedTime] DATETIME NULL,
        CONSTRAINT [FK_Dashboards_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([UserId])
    );

    CREATE INDEX [IX_Dashboards_IsPublic] ON [dbo].[Dashboards]([IsPublic]);
    CREATE INDEX [IX_Dashboards_CreatedTime] ON [dbo].[Dashboards]([CreatedTime]);

    PRINT 'Table [Dashboards] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [Dashboards] already exists, skipping...';
END
GO

-- 创建大屏组件表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DashboardWidgets]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DashboardWidgets] (
        [WidgetId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [DashboardId] INT NOT NULL,
        [ReportId] INT NOT NULL,
        [WidgetType] NVARCHAR(50) NOT NULL,
        [Title] NVARCHAR(100) NULL,
        [PositionX] INT NOT NULL DEFAULT 0,
        [PositionY] INT NOT NULL DEFAULT 0,
        [Width] INT NOT NULL DEFAULT 4,
        [Height] INT NOT NULL DEFAULT 3,
        [DataConfig] NVARCHAR(MAX) NULL,
        [StyleConfig] NVARCHAR(MAX) NULL,
        [CreatedTime] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_DashboardWidgets_Dashboards] FOREIGN KEY ([DashboardId]) REFERENCES [dbo].[Dashboards]([DashboardId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DashboardWidgets_Reports] FOREIGN KEY ([ReportId]) REFERENCES [dbo].[Reports]([ReportId])
    );

    CREATE INDEX [IX_DashboardWidgets_DashboardId_ReportId] ON [dbo].[DashboardWidgets]([DashboardId], [ReportId]);

    PRINT 'Table [DashboardWidgets] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [DashboardWidgets] already exists, skipping...';
END
GO

-- 创建组件规则表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WidgetRules]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[WidgetRules] (
        [RuleId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [WidgetId] INT NOT NULL,
        [RuleName] NVARCHAR(50) NULL,
        [Field] NVARCHAR(100) NOT NULL,
        [Operator] NVARCHAR(20) NOT NULL,
        [Value] NVARCHAR(100) NOT NULL,
        [ActionType] NVARCHAR(50) NOT NULL,
        [ActionValue] NVARCHAR(100) NULL,
        [Priority] INT NOT NULL DEFAULT 0,
        [CreatedTime] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_WidgetRules_DashboardWidgets] FOREIGN KEY ([WidgetId]) REFERENCES [dbo].[DashboardWidgets]([WidgetId]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_WidgetRules_WidgetId] ON [dbo].[WidgetRules]([WidgetId]);

    PRINT 'Table [WidgetRules] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [WidgetRules] already exists, skipping...';
END
GO

-- 添加大屏相关权限
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [PermissionCode] = 'dashboard:view')
BEGIN
    INSERT INTO [dbo].[Permissions] ([PermissionCode], [PermissionName], [Description], [Module], [CreatedTime])
    VALUES
        ('dashboard:view', '查看大屏', '查看大屏列表和详情', 'Dashboard', GETDATE()),
        ('dashboard:create', '创建大屏', '创建新的大屏', 'Dashboard', GETDATE()),
        ('dashboard:edit', '编辑大屏', '编辑大屏配置和组件', 'Dashboard', GETDATE()),
        ('dashboard:delete', '删除大屏', '删除大屏', 'Dashboard', GETDATE());

    PRINT 'Dashboard permissions added successfully';
END
ELSE
BEGIN
    PRINT 'Dashboard permissions already exist, skipping...';
END
GO

-- 为管理员角色分配大屏权限
DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = [RoleId] FROM [dbo].[Roles] WHERE [RoleCode] = 'admin';

IF @AdminRoleId IS NOT NULL
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId], [CreatedTime])
    SELECT @AdminRoleId, [PermissionId], GETDATE()
    FROM [dbo].[Permissions]
    WHERE [PermissionCode] IN ('dashboard:view', 'dashboard:create', 'dashboard:edit', 'dashboard:delete')
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[RolePermissions] rp
        WHERE rp.[RoleId] = @AdminRoleId AND rp.[PermissionId] = [Permissions].[PermissionId]
    );

    PRINT 'Dashboard permissions assigned to admin role successfully';
END
ELSE
BEGIN
    PRINT 'Admin role not found, skipping permission assignment...';
END
GO

-- =============================================
-- 迁移完成
-- =============================================
PRINT '========================================';
PRINT 'V1.1.0 Dashboard Migration Completed';
PRINT '========================================';
