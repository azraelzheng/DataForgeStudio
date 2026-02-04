-- DataForgeStudio V4 - Add Permissions Table
-- This script adds the Permissions table and updates RolePermissions to use foreign keys

USE DataForgeStudio_V4;
GO

-- Drop existing RolePermissions table (will be recreated with foreign key)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolePermissions]') AND type in (N'U'))
BEGIN
    DROP TABLE [dbo].[RolePermissions];
END
GO

-- Create Permissions table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Permissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Permissions](
        [PermissionId] [int] IDENTITY(1,1) NOT NULL,
        [PermissionCode] [nvarchar](100) NOT NULL,
        [PermissionName] [nvarchar](100) NOT NULL,
        [Module] [nvarchar](50) NOT NULL,
        [Action] [nvarchar](50) NOT NULL,
        [Description] [nvarchar](200) NULL,
        [ParentId] [int] NULL,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([PermissionId] ASC),
        CONSTRAINT [UQ_Permissions_Code] UNIQUE NONCLUSTERED ([PermissionCode] ASC)
    );
END
GO

-- Create RolePermissions table with foreign key to Permissions
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolePermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RolePermissions](
        [RolePermissionId] [int] IDENTITY(1,1) NOT NULL,
        [RoleId] [int] NOT NULL,
        [PermissionId] [int] NOT NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([RolePermissionId] ASC),
        CONSTRAINT [UQ_RolePermissions_Role_Permission] UNIQUE NONCLUSTERED ([RoleId] ASC, [PermissionId] ASC),
        CONSTRAINT [FK_RolePermissions_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]),
        CONSTRAINT [FK_RolePermissions_Permissions] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions] ([PermissionId])
    );
END
GO

PRINT 'Permissions and RolePermissions tables created successfully!';
GO

-- Seed default permissions
INSERT INTO [dbo].[Permissions] ([PermissionCode], [PermissionName], [Module], [Action], [Description], [ParentId], [SortOrder], [IsSystem])
VALUES
    -- Report permissions
    ('report:view', '查看报表', 'Report', 'View', '查看报表列表', NULL, 1, 1),
    ('report:query', '查询报表', 'Report', 'Query', '执行报表查询', NULL, 2, 1),
    ('report:create', '创建报表', 'Report', 'Create', '创建新报表', NULL, 3, 1),
    ('report:edit', '编辑报表', 'Report', 'Edit', '编辑报表配置', NULL, 4, 1),
    ('report:delete', '删除报表', 'Report', 'Delete', '删除报表', NULL, 5, 1),
    ('report:export', '导出报表', 'Report', 'Export', '导出报表数据', NULL, 6, 1),

    -- DataSource permissions
    ('datasource:view', '查看数据源', 'DataSource', 'View', '查看数据源列表', NULL, 11, 1),
    ('datasource:create', '创建数据源', 'DataSource', 'Create', '创建新数据源', NULL, 12, 1),
    ('datasource:edit', '编辑数据源', 'DataSource', 'Edit', '编辑数据源配置', NULL, 13, 1),
    ('datasource:delete', '删除数据源', 'DataSource', 'Delete', '删除数据源', NULL, 14, 1),
    ('datasource:test', '测试数据源', 'DataSource', 'Test', '测试数据源连接', NULL, 15, 1),

    -- User management permissions
    ('user:view', '查看用户', 'User', 'View', '查看用户列表', NULL, 21, 1),
    ('user:create', '创建用户', 'User', 'Create', '创建新用户', NULL, 22, 1),
    ('user:edit', '编辑用户', 'User', 'Edit', '编辑用户信息', NULL, 23, 1),
    ('user:delete', '删除用户', 'User', 'Delete', '删除用户', NULL, 24, 1),
    ('user:reset-password', '重置密码', 'User', 'ResetPassword', '重置用户密码', NULL, 25, 1),

    -- Role management permissions
    ('role:view', '查看角色', 'Role', 'View', '查看角色列表', NULL, 31, 1),
    ('role:create', '创建角色', 'Role', 'Create', '创建新角色', NULL, 32, 1),
    ('role:edit', '编辑角色', 'Role', 'Edit', '编辑角色权限', NULL, 33, 1),
    ('role:delete', '删除角色', 'Role', 'Delete', '删除角色', NULL, 34, 1),

    -- System permissions
    ('system:view', '查看系统信息', 'System', 'View', '查看系统信息', NULL, 41, 1),
    ('system:config', '系统配置', 'System', 'Config', '修改系统配置', NULL, 42, 1),
    ('system:backup', '数据备份', 'System', 'Backup', '执行数据备份', NULL, 43, 1),
    ('system:restore', '数据恢复', 'System', 'Restore', '执行数据恢复', NULL, 44, 1),

    -- License permissions
    ('license:view', '查看许可证', 'License', 'View', '查看许可证信息', NULL, 51, 1),
    ('license:activate', '激活许可证', 'License', 'Activate', '激活产品许可证', NULL, 52, 1);

PRINT 'Permissions seeded: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';
GO

-- Assign permissions to roles
-- Super Admin gets all permissions
INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId], [CreatedBy], [CreatedTime])
SELECT r.RoleId, p.PermissionId, 1, GETUTCDATE()
FROM Roles r
CROSS JOIN Permissions p
WHERE r.RoleCode = 'SUPER_ADMIN';
PRINT 'Super Admin permissions assigned: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';
GO

-- Admin gets most permissions (except license activation)
INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId], [CreatedBy], [CreatedTime])
SELECT r.RoleId, p.PermissionId, 1, GETUTCDATE()
FROM Roles r
CROSS JOIN Permissions p
WHERE r.RoleCode = 'ADMIN'
  AND p.PermissionCode NOT IN ('license:activate');
PRINT 'Admin permissions assigned: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';
GO

-- Regular User gets basic permissions
INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId], [CreatedBy], [CreatedTime])
SELECT r.RoleId, p.PermissionId, 1, GETUTCDATE()
FROM Roles r
CROSS JOIN Permissions p
WHERE r.RoleCode = 'USER'
  AND p.PermissionCode IN ('report:view', 'report:query', 'report:export');
PRINT 'User permissions assigned: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';
GO

-- Guest gets minimal permissions
INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId], [CreatedBy], [CreatedTime])
SELECT r.RoleId, p.PermissionId, 1, GETUTCDATE()
FROM Roles r
CROSS JOIN Permissions p
WHERE r.RoleCode = 'GUEST'
  AND p.PermissionCode IN ('report:view');
PRINT 'Guest permissions assigned: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';
GO

PRINT '';
PRINT '============================================================================';
PRINT 'Permissions Table Migration Completed!';
PRINT '============================================================================';
PRINT 'Total Permissions: ' + CAST((SELECT COUNT(*) FROM Permissions) AS VARCHAR);
PRINT 'Total RolePermissions: ' + CAST((SELECT COUNT(*) FROM RolePermissions) AS VARCHAR);
PRINT '============================================================================';
PRINT '';
GO
