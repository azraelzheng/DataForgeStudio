-- DataForgeStudio V4 Database Initialization Script
-- Compatible with SQL Server 2005+

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DataForgeStudio_V4')
BEGIN
    CREATE DATABASE DataForgeStudio_V4;
END
GO

USE DataForgeStudio_V4;
GO

-- ============================================================================
-- Tables
-- ============================================================================

-- Users Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users](
        [UserId] [int] IDENTITY(1,1) NOT NULL,
        [Username] [nvarchar](50) NOT NULL,
        [PasswordHash] [nvarchar](256) NOT NULL,
        [RealName] [nvarchar](50) NULL,
        [Email] [nvarchar](100) NULL,
        [Phone] [nvarchar](20) NULL,
        [Department] [nvarchar](100) NULL,
        [Position] [nvarchar](50) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [IsLocked] [bit] NOT NULL DEFAULT 0,
        [LastLoginTime] [datetime] NULL,
        [LastLoginIP] [nvarchar](50) NULL,
        [PasswordFailCount] [int] NOT NULL DEFAULT 0,
        [MustChangePassword] [bit] NOT NULL DEFAULT 0,
        [Remark] [nvarchar](500) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [UQ_Users_Username] UNIQUE NONCLUSTERED ([Username] ASC),
        CONSTRAINT [CK_Users_IsSystem] CHECK ([IsSystem] = 0 OR [Username] = 'root')
    );
END
GO

-- Roles Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Roles](
        [RoleId] [int] IDENTITY(1,1) NOT NULL,
        [RoleName] [nvarchar](50) NOT NULL,
        [RoleCode] [nvarchar](50) NOT NULL,
        [Description] [nvarchar](200) NULL,
        [Permissions] [nvarchar](max) NULL,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC),
        CONSTRAINT [UQ_Roles_RoleCode] UNIQUE NONCLUSTERED ([RoleCode] ASC)
    );
END
GO

-- UserRoles Table (Many-to-Many)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserRoles](
        [UserRoleId] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NOT NULL,
        [RoleId] [int] NOT NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserRoleId] ASC),
        CONSTRAINT [UQ_UserRoles_User_Role] UNIQUE NONCLUSTERED ([UserId] ASC, [RoleId] ASC)
    );
END
GO

-- RolePermissions Table (if using detailed permissions)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolePermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RolePermissions](
        [PermissionId] [int] IDENTITY(1,1) NOT NULL,
        [RoleId] [int] NOT NULL,
        [PermissionCode] [nvarchar](100) NOT NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([PermissionId] ASC),
        CONSTRAINT [UQ_RolePermissions_Role_Code] UNIQUE NONCLUSTERED ([RoleId] ASC, [PermissionCode] ASC)
    );
END
GO

-- DataSources Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataSources]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DataSources](
        [DataSourceId] [int] IDENTITY(1,1) NOT NULL,
        [DataSourceName] [nvarchar](100) NOT NULL,
        [DataSourceCode] [nvarchar](50) NOT NULL,
        [DbType] [nvarchar](20) NOT NULL,
        [ServerAddress] [nvarchar](200) NOT NULL,
        [Port] [int] NULL,
        [DatabaseName] [nvarchar](100) NULL,
        [Username] [nvarchar](100) NULL,
        [Password] [nvarchar](500) NULL,
        [IsIntegratedSecurity] [bit] NOT NULL DEFAULT 0,
        [ConnectionTimeout] [int] NOT NULL DEFAULT 30,
        [CommandTimeout] [int] NOT NULL DEFAULT 60,
        [IsDefault] [bit] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [TestSql] [nvarchar](500) NULL,
        [Remark] [nvarchar](500) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        [LastTestTime] [datetime] NULL,
        [LastTestResult] [bit] NULL,
        [LastTestMessage] [nvarchar](500) NULL,
        CONSTRAINT [PK_DataSources] PRIMARY KEY CLUSTERED ([DataSourceId] ASC)
    );
END
GO

-- Reports Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Reports]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Reports](
        [ReportId] [int] IDENTITY(1,1) NOT NULL,
        [ReportName] [nvarchar](100) NOT NULL,
        [ReportCode] [nvarchar](50) NOT NULL,
        [ReportCategory] [nvarchar](50) NULL,
        [DataSourceId] [int] NOT NULL,
        [SqlStatement] [nvarchar](max) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsPaged] [bit] NOT NULL DEFAULT 1,
        [PageSize] [int] NOT NULL DEFAULT 50,
        [CacheDuration] [int] NOT NULL DEFAULT 0,
        [IsEnabled] [bit] NOT NULL DEFAULT 1,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [ViewCount] [int] NOT NULL DEFAULT 0,
        [LastViewTime] [datetime] NULL,
        [Remark] [nvarchar](500) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_Reports] PRIMARY KEY CLUSTERED ([ReportId] ASC),
        CONSTRAINT [UQ_Reports_ReportCode] UNIQUE NONCLUSTERED ([ReportCode] ASC)
    );
END
GO

-- ReportFields Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReportFields]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ReportFields](
        [FieldId] [int] IDENTITY(1,1) NOT NULL,
        [ReportId] [int] NOT NULL,
        [FieldName] [nvarchar](100) NOT NULL,
        [DisplayName] [nvarchar](100) NOT NULL,
        [DataType] [nvarchar](20) NOT NULL,
        [Width] [int] NOT NULL DEFAULT 100,
        [IsVisible] [bit] NOT NULL DEFAULT 1,
        [IsSortable] [bit] NOT NULL DEFAULT 1,
        [IsFilterable] [bit] NOT NULL DEFAULT 0,
        [IsGroupable] [bit] NOT NULL DEFAULT 0,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [Align] [nvarchar](10) NOT NULL DEFAULT 'left',
        [FormatString] [nvarchar](50) NULL,
        [AggregateFunction] [nvarchar](20) NULL,
        [CssClass] [nvarchar](100) NULL,
        [Remark] [nvarchar](200) NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ReportFields] PRIMARY KEY CLUSTERED ([FieldId] ASC)
    );
END
GO

-- ReportParameters Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReportParameters]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ReportParameters](
        [ParameterId] [int] IDENTITY(1,1) NOT NULL,
        [ReportId] [int] NOT NULL,
        [ParameterName] [nvarchar](50) NOT NULL,
        [DisplayName] [nvarchar](100) NOT NULL,
        [DataType] [nvarchar](20) NOT NULL,
        [InputType] [nvarchar](20) NOT NULL,
        [DefaultValue] [nvarchar](500) NULL,
        [IsRequired] [bit] NOT NULL DEFAULT 1,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [Options] [nvarchar](max) NULL,
        [QueryOptions] [nvarchar](max) NULL,
        [Remark] [nvarchar](200) NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ReportParameters] PRIMARY KEY CLUSTERED ([ParameterId] ASC)
    );
END
GO

-- OperationLogs Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OperationLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OperationLogs](
        [LogId] [int] IDENTITY(1,1) NOT NULL,
        [Username] [nvarchar](50) NOT NULL,
        [Action] [nvarchar](50) NOT NULL,
        [Module] [nvarchar](50) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IpAddress] [nvarchar](50) NULL,
        [Browser] [nvarchar](200) NULL,
        [Os] [nvarchar](100) NULL,
        [RequestData] [nvarchar](max) NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_OperationLogs] PRIMARY KEY CLUSTERED ([LogId] ASC)
    );
END
GO

-- LoginLogs Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LoginLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LoginLogs](
        [LogId] [int] IDENTITY(1,1) NOT NULL,
        [Username] [nvarchar](50) NOT NULL,
        [LoginTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [LogoutTime] [datetime] NULL,
        [IpAddress] [nvarchar](50) NULL,
        [IsSuccess] [bit] NOT NULL DEFAULT 1,
        [FailReason] [nvarchar](200) NULL,
        CONSTRAINT [PK_LoginLogs] PRIMARY KEY CLUSTERED ([LogId] ASC)
    );
END
GO

-- SystemConfigs Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SystemConfigs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SystemConfigs](
        [ConfigId] [int] IDENTITY(1,1) NOT NULL,
        [ConfigKey] [nvarchar](100) NOT NULL,
        [ConfigValue] [nvarchar](max) NULL,
        [ConfigType] [nvarchar](20) NOT NULL DEFAULT 'String',
        [Description] [nvarchar](200) NULL,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_SystemConfigs] PRIMARY KEY CLUSTERED ([ConfigId] ASC),
        CONSTRAINT [UQ_SystemConfigs_ConfigKey] UNIQUE NONCLUSTERED ([ConfigKey] ASC)
    );
END
GO

-- BackupRecords Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BackupRecords]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BackupRecords](
        [BackupId] [int] IDENTITY(1,1) NOT NULL,
        [BackupName] [nvarchar](200) NOT NULL,
        [BackupType] [nvarchar](20) NOT NULL DEFAULT 'Manual',
        [BackupPath] [nvarchar](500) NOT NULL,
        [DatabaseName] [nvarchar](100) NULL,
        [FileSize] [bigint] NULL,
        [BackupTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [IsSuccess] [bit] NOT NULL DEFAULT 1,
        [ErrorMessage] [nvarchar](max) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_BackupRecords] PRIMARY KEY CLUSTERED ([BackupId] ASC)
    );
END
GO

-- Licenses Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Licenses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Licenses](
        [LicenseId] [int] IDENTITY(1,1) NOT NULL,
        [LicenseKey] [nvarchar](500) NOT NULL,
        [CompanyName] [nvarchar](200) NULL,
        [ContactPerson] [nvarchar](50) NULL,
        [Email] [nvarchar](100) NULL,
        [Phone] [nvarchar](20) NULL,
        [MaxUsers] [int] NULL,
        [MaxReports] [int] NULL,
        [MaxDataSources] [int] NULL,
        [ExpiryDate] [datetime] NULL,
        [Features] [nvarchar](max) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [ActivatedTime] [datetime] NULL,
        [ActivatedIP] [nvarchar](50) NULL,
        [MachineCode] [nvarchar](200) NULL,
        [Remark] [nvarchar](500) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_Licenses] PRIMARY KEY CLUSTERED ([LicenseId] ASC)
    );
END
GO

-- ============================================================================
-- Foreign Keys
-- ============================================================================

-- UserRoles FKs
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_UserRoles_Users')
BEGIN
    ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT [FK_UserRoles_Users]
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_UserRoles_Roles')
BEGIN
    ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT [FK_UserRoles_Roles]
    FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE CASCADE;
END
GO

-- Reports FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Reports_DataSources')
BEGIN
    ALTER TABLE [dbo].[Reports] ADD CONSTRAINT [FK_Reports_DataSources]
    FOREIGN KEY ([DataSourceId]) REFERENCES [dbo].[DataSources] ([DataSourceId]);
END
GO

-- ReportFields FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ReportFields_Reports')
BEGIN
    ALTER TABLE [dbo].[ReportFields] ADD CONSTRAINT [FK_ReportFields_Reports]
    FOREIGN KEY ([ReportId]) REFERENCES [dbo].[Reports] ([ReportId]) ON DELETE CASCADE;
END
GO

-- ReportParameters FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ReportParameters_Reports')
BEGIN
    ALTER TABLE [dbo].[ReportParameters] ADD CONSTRAINT [FK_ReportParameters_Reports]
    FOREIGN KEY ([ReportId]) REFERENCES [dbo].[Reports] ([ReportId]) ON DELETE CASCADE;
END
GO

-- ============================================================================
-- Indexes
-- ============================================================================

-- Users Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('Users'))
BEGIN
    CREATE INDEX [IX_Users_Username] ON [dbo].[Users] ([Username] ASC);
END
GO

-- Reports Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reports_Category' AND object_id = OBJECT_ID('Reports'))
BEGIN
    CREATE INDEX [IX_Reports_Category] ON [dbo].[Reports] ([ReportCategory] ASC);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reports_DataSourceId' AND object_id = OBJECT_ID('Reports'))
BEGIN
    CREATE INDEX [IX_Reports_DataSourceId] ON [dbo].[Reports] ([DataSourceId] ASC);
END
GO

-- OperationLogs Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OperationLogs_CreatedTime' AND object_id = OBJECT_ID('OperationLogs'))
BEGIN
    CREATE INDEX [IX_OperationLogs_CreatedTime] ON [dbo].[OperationLogs] ([CreatedTime] DESC);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OperationLogs_Username' AND object_id = OBJECT_ID('OperationLogs'))
BEGIN
    CREATE INDEX [IX_OperationLogs_Username] ON [dbo].[OperationLogs] ([Username] ASC);
END
GO

-- ============================================================================
-- Initial Data
-- ============================================================================

-- Insert default root user (password: admin123, hashed with BCrypt)
IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [Username] = 'root')
BEGIN
    INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [RealName], [IsActive], [IsSystem], [CreatedTime])
    VALUES ('root', '$2a$12$YourBCryptHashedPasswordHere', 'System Administrator', 1, 1, GETUTCDATE());
END
GO

-- Note: The above hash is a placeholder. Replace with actual BCrypt hash for 'admin123'
-- To generate a BCrypt hash, use: BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 12)

-- Insert system roles
IF NOT EXISTS (SELECT * FROM [dbo].[Roles] WHERE [RoleCode] = 'SUPER_ADMIN')
BEGIN
    INSERT INTO [dbo].[Roles] ([RoleName], [RoleCode], [Description], [IsSystem], [SortOrder], [Permissions])
    VALUES ('超级管理员', 'SUPER_ADMIN', '拥有所有权限', 1, 1, '*');
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Roles] WHERE [RoleCode] = 'ADMIN')
BEGIN
    INSERT INTO [dbo].[Roles] ([RoleName], [RoleCode], [Description], [IsSystem], [SortOrder], [Permissions])
    VALUES ('管理员', 'ADMIN', '系统管理员', 1, 2, '["report:*","system:*"]');
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Roles] WHERE [RoleCode] = 'USER')
BEGIN
    INSERT INTO [dbo].[Roles] ([RoleName], [RoleCode], [Description], [IsSystem], [SortOrder], [Permissions])
    VALUES ('普通用户', 'USER', '普通用户', 1, 3, '["report:view"]');
END
GO

-- Assign Super Admin role to root user
IF NOT EXISTS (SELECT * FROM [dbo].[UserRoles] WHERE [UserId] = 1 AND [RoleId] = 1)
BEGIN
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
    SELECT [UserId], [RoleId] FROM [dbo].[Users] CROSS JOIN [dbo].[Roles]
    WHERE [Users].[Username] = 'root' AND [Roles].[RoleCode] = 'SUPER_ADMIN';
END
GO

-- Insert default system configs
IF NOT EXISTS (SELECT * FROM [dbo].[SystemConfigs] WHERE [ConfigKey] = 'System.Name')
BEGIN
    INSERT INTO [dbo].[SystemConfigs] ([ConfigKey], [ConfigValue], [ConfigType], [Description], [IsSystem], [SortOrder])
    VALUES ('System.Name', 'DataForgeStudio V4', 'String', '系统名称', 1, 1);
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[SystemConfigs] WHERE [ConfigKey] = 'System.Version')
BEGIN
    INSERT INTO [dbo].[SystemConfigs] ([ConfigKey], [ConfigValue], [ConfigType], [Description], [IsSystem], [SortOrder])
    VALUES ('System.Version', '1.0.0', 'String', '系统版本', 1, 2);
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[SystemConfigs] WHERE [ConfigKey] = 'Security.Password.MinLength')
BEGIN
    INSERT INTO [dbo].[SystemConfigs] ([ConfigKey], [ConfigValue], [ConfigType], [Description], [IsSystem], [SortOrder])
    VALUES ('Security.Password.MinLength', '6', 'Integer', '密码最小长度', 1, 10);
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[SystemConfigs] WHERE [ConfigKey] = 'Security.Password.MaxFailCount')
BEGIN
    INSERT INTO [dbo].[SystemConfigs] ([ConfigKey], [ConfigValue], [ConfigType], [Description], [IsSystem], [SortOrder])
    VALUES ('Security.Password.MaxFailCount', '5', 'Integer', '密码错误最大次数', 1, 11);
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[SystemConfigs] WHERE [ConfigKey] = 'Session.Timeout')
BEGIN
    INSERT INTO [dbo].[SystemConfigs] ([ConfigKey], [ConfigValue], [ConfigType], [Description], [IsSystem], [SortOrder])
    VALUES ('Session.Timeout', '30', 'Integer', 'Session超时时间（分钟）', 1, 20);
END
GO

PRINT 'Database initialization completed successfully!';
GO
