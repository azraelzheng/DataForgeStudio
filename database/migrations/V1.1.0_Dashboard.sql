-- =============================================
-- 大屏系统数据库迁移脚本
-- 版本: V1.1.0
-- 日期: 2026-03-02
-- 说明: 创建车间大屏可视化系统所需的数据表
-- 兼容: SQL Server 2005+
-- =============================================

-- 检查表是否已存在，如果存在则跳过
-- 注意: SQL Server 2005 不支持 IF NOT EXISTS，使用 OBJECT_ID 检查

PRINT '开始执行大屏系统数据库迁移...';

-- =============================================
-- 1. 创建 Dashboards 表（大屏主表）
-- =============================================
IF OBJECT_ID('Dashboards', 'U') IS NULL
BEGIN
    PRINT '创建 Dashboards 表...';

    CREATE TABLE [Dashboards] (
        [DashboardId] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Theme] NVARCHAR(20) NULL DEFAULT 'dark',
        [RefreshInterval] INT NULL DEFAULT 30,
        [IsPublic] BIT NULL DEFAULT 0,
        [LayoutConfig] NVARCHAR(MAX) NULL,
        [ThemeConfig] NVARCHAR(MAX) NULL,
        [CreatedBy] INT NULL,
        [CreatedTime] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedTime] DATETIME NULL,

        CONSTRAINT [PK_Dashboards] PRIMARY KEY CLUSTERED ([DashboardId] ASC)
    );

    -- 创建索引
    CREATE NONCLUSTERED INDEX [IX_Dashboards_IsPublic] ON [Dashboards] ([IsPublic]);
    CREATE NONCLUSTERED INDEX [IX_Dashboards_CreatedTime] ON [Dashboards] ([CreatedTime] DESC);

    -- 添加外键约束
    ALTER TABLE [Dashboards] WITH CHECK
    ADD CONSTRAINT [FK_Dashboards_Users_CreatedBy]
    FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([UserId])
    ON DELETE SET NULL;

    PRINT 'Dashboards 表创建完成';
END
ELSE
BEGIN
    PRINT 'Dashboards 表已存在，跳过创建';
END
GO

-- =============================================
-- 2. 创建 DashboardWidgets 表（组件表）
-- =============================================
IF OBJECT_ID('DashboardWidgets', 'U') IS NULL
BEGIN
    PRINT '创建 DashboardWidgets 表...';

    CREATE TABLE [DashboardWidgets] (
        [WidgetId] INT IDENTITY(1,1) NOT NULL,
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
        [CreatedTime] DATETIME NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_DashboardWidgets] PRIMARY KEY CLUSTERED ([WidgetId] ASC)
    );

    -- 创建索引
    CREATE NONCLUSTERED INDEX [IX_DashboardWidgets_DashboardId_ReportId]
    ON [DashboardWidgets] ([DashboardId], [ReportId]);

    -- 添加外键约束
    ALTER TABLE [DashboardWidgets] WITH CHECK
    ADD CONSTRAINT [FK_DashboardWidgets_Dashboards_DashboardId]
    FOREIGN KEY ([DashboardId]) REFERENCES [Dashboards] ([DashboardId])
    ON DELETE CASCADE;

    ALTER TABLE [DashboardWidgets] WITH CHECK
    ADD CONSTRAINT [FK_DashboardWidgets_Reports_ReportId]
    FOREIGN KEY ([ReportId]) REFERENCES [Reports] ([ReportId])
    ON DELETE NO ACTION;

    PRINT 'DashboardWidgets 表创建完成';
END
ELSE
BEGIN
    PRINT 'DashboardWidgets 表已存在，跳过创建';
END
GO

-- =============================================
-- 3. 创建 WidgetRules 表（组件规则表）
-- =============================================
IF OBJECT_ID('WidgetRules', 'U') IS NULL
BEGIN
    PRINT '创建 WidgetRules 表...';

    CREATE TABLE [WidgetRules] (
        [RuleId] INT IDENTITY(1,1) NOT NULL,
        [WidgetId] INT NOT NULL,
        [RuleName] NVARCHAR(50) NULL,
        [Field] NVARCHAR(100) NOT NULL,
        [Operator] NVARCHAR(20) NOT NULL,
        [Value] NVARCHAR(100) NOT NULL,
        [ActionType] NVARCHAR(50) NOT NULL,
        [ActionValue] NVARCHAR(100) NULL,
        [Priority] INT NOT NULL DEFAULT 0,
        [CreatedTime] DATETIME NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_WidgetRules] PRIMARY KEY CLUSTERED ([RuleId] ASC)
    );

    -- 创建索引
    CREATE NONCLUSTERED INDEX [IX_WidgetRules_WidgetId] ON [WidgetRules] ([WidgetId]);

    -- 添加外键约束
    ALTER TABLE [WidgetRules] WITH CHECK
    ADD CONSTRAINT [FK_WidgetRules_DashboardWidgets_WidgetId]
    FOREIGN KEY ([WidgetId]) REFERENCES [DashboardWidgets] ([WidgetId])
    ON DELETE CASCADE;

    PRINT 'WidgetRules 表创建完成';
END
ELSE
BEGIN
    PRINT 'WidgetRules 表已存在，跳过创建';
END
GO

-- =============================================
-- 4. 添加大屏相关权限
-- =============================================
PRINT '添加大屏相关权限...';

-- 检查权限是否已存在
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = 'dashboard:view')
BEGIN
    -- 查找系统模块的父级ID
    DECLARE @SystemModuleId INT
    SELECT @SystemModuleId = [PermissionId] FROM [Permissions] WHERE [PermissionCode] = 'system' OR [PermissionName] = N'系统管理'

    INSERT INTO [Permissions] ([PermissionName], [PermissionCode], [Description], [Module], [ParentId], [SortOrder], [IsActive], [CreatedTime])
    VALUES
        (N'大屏查看', 'dashboard:view', N'查看大屏列表和详情', N'大屏管理', NULL, 1, 1, GETUTCDATE()),
        (N'大屏编辑', 'dashboard:edit', N'创建和编辑大屏', N'大屏管理', NULL, 2, 1, GETUTCDATE()),
        (N'大屏删除', 'dashboard:delete', N'删除大屏', N'大屏管理', NULL, 3, 1, GETUTCDATE());

    PRINT '大屏权限添加完成';
END
ELSE
BEGIN
    PRINT '大屏权限已存在，跳过添加';
END
GO

-- =============================================
-- 5. 为管理员角色分配大屏权限
-- =============================================
PRINT '为管理员角色分配大屏权限...';

-- 查找管理员角色
DECLARE @AdminRoleId INT
SELECT @AdminRoleId = [RoleId] FROM [Roles] WHERE [RoleCode] = 'admin' OR [RoleName] = N'管理员'

IF @AdminRoleId IS NOT NULL
BEGIN
    -- 为管理员角色添加大屏权限
    INSERT INTO [RolePermissions] ([RoleId], [PermissionId], [CreatedTime])
    SELECT @AdminRoleId, [PermissionId], GETUTCDATE()
    FROM [Permissions]
    WHERE [PermissionCode] IN ('dashboard:view', 'dashboard:edit', 'dashboard:delete')
    AND [PermissionId] NOT IN (
        SELECT [PermissionId] FROM [RolePermissions] WHERE [RoleId] = @AdminRoleId
    );

    PRINT '管理员角色大屏权限分配完成';
END
ELSE
BEGIN
    PRINT '未找到管理员角色，跳过权限分配';
END
GO

-- =============================================
-- 验证表创建结果
-- =============================================
PRINT '';
PRINT '========================================';
PRINT '验证表创建结果:';
PRINT '========================================';

IF OBJECT_ID('Dashboards', 'U') IS NOT NULL
    PRINT '[OK] Dashboards 表存在'
ELSE
    PRINT '[ERROR] Dashboards 表不存在'

IF OBJECT_ID('DashboardWidgets', 'U') IS NOT NULL
    PRINT '[OK] DashboardWidgets 表存在'
ELSE
    PRINT '[ERROR] DashboardWidgets 表不存在'

IF OBJECT_ID('WidgetRules', 'U') IS NOT NULL
    PRINT '[OK] WidgetRules 表存在'
ELSE
    PRINT '[ERROR] WidgetRules 表不存在'

DECLARE @PermissionCount INT
SELECT @PermissionCount = COUNT(*) FROM [Permissions] WHERE [PermissionCode] LIKE 'dashboard:%'
PRINT '大屏权限数量: ' + CAST(@PermissionCount AS NVARCHAR(10))

PRINT '';
PRINT '========================================';
PRINT '大屏系统数据库迁移完成!';
PRINT '========================================';
