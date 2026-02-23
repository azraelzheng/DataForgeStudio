-- DataForgeStudio V4 Database Initialization Script
-- Compatible with SQL Server 2005+
-- Updated: 2026-02-23 - Sync with current entity definitions

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DataForgeStudio')
BEGIN
    CREATE DATABASE DataForgeStudio;
END
GO

USE DataForgeStudio;
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

-- Permissions Table
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
        CONSTRAINT [UQ_Permissions_PermissionCode] UNIQUE NONCLUSTERED ([PermissionCode] ASC)
    );
END
GO

-- RolePermissions Table (Updated structure)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolePermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RolePermissions](
        [RolePermissionId] [int] IDENTITY(1,1) NOT NULL,
        [RoleId] [int] NOT NULL,
        [PermissionId] [int] NOT NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([RolePermissionId] ASC),
        CONSTRAINT [UQ_RolePermissions_Role_Permission] UNIQUE NONCLUSTERED ([RoleId] ASC, [PermissionId] ASC)
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

-- OperationLogs Table (Updated structure - matches OperationLog entity)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OperationLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OperationLogs](
        [LogId] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NULL,
        [Username] [nvarchar](50) NULL,
        [Module] [nvarchar](50) NOT NULL,
        [Action] [nvarchar](50) NOT NULL,
        [ActionType] [nvarchar](20) NULL,
        [Description] [nvarchar](500) NULL,
        [IpAddress] [nvarchar](50) NULL,
        [UserAgent] [nvarchar](500) NULL,
        [RequestUrl] [nvarchar](500) NULL,
        [RequestMethod] [nvarchar](10) NULL,
        [RequestData] [nvarchar](max) NULL,
        [ResponseData] [nvarchar](max) NULL,
        [Duration] [int] NULL,
        [IsSuccess] [bit] NOT NULL DEFAULT 1,
        [ErrorMessage] [nvarchar](max) NULL,
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
        [UserId] [int] NULL,
        [Username] [nvarchar](50) NULL,
        [LoginTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [LogoutTime] [datetime] NULL,
        [IpAddress] [nvarchar](50) NULL,
        [UserAgent] [nvarchar](500) NULL,
        [LoginStatus] [nvarchar](20) NULL,
        [FailureReason] [nvarchar](200) NULL,
        [SessionId] [nvarchar](100) NULL,
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

-- BackupRecords Table (Updated - added Description field)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BackupRecords]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BackupRecords](
        [BackupId] [int] IDENTITY(1,1) NOT NULL,
        [BackupName] [nvarchar](200) NOT NULL,
        [BackupType] [nvarchar](20) NOT NULL DEFAULT 'Manual',
        [BackupPath] [nvarchar](500) NOT NULL,
        [DatabaseName] [nvarchar](100) NULL,
        [Description] [nvarchar](500) NULL,
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

-- BackupSchedules Table (New)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BackupSchedules]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BackupSchedules](
        [ScheduleId] [int] IDENTITY(1,1) NOT NULL,
        [ScheduleName] [nvarchar](100) NOT NULL,
        [ScheduleType] [nvarchar](20) NOT NULL DEFAULT 'Recurring',
        [RecurringDays] [nvarchar](50) NULL,
        [ScheduledTime] [nvarchar](10) NULL,
        [OnceDate] [datetime] NULL,
        [RetentionCount] [int] NOT NULL DEFAULT 10,
        [IsEnabled] [bit] NOT NULL DEFAULT 1,
        [LastRunTime] [datetime] NULL,
        [NextRunTime] [datetime] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_BackupSchedules] PRIMARY KEY CLUSTERED ([ScheduleId] ASC)
    );
END
GO

-- Licenses Table (Updated - Zero Trust Architecture)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Licenses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Licenses](
        [LicenseId] [int] IDENTITY(1,1) NOT NULL,
        [LicenseKey] [nvarchar](max) NOT NULL,
        [Signature] [nvarchar](512) NOT NULL DEFAULT '',
        [MachineCode] [nvarchar](64) NOT NULL,
        [ActivatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [ActivatedIP] [nvarchar](50) NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Licenses] PRIMARY KEY CLUSTERED ([LicenseId] ASC)
    );
END
GO

-- TrialRecords Table (New)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrialRecords]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TrialRecords](
        [TrialRecordId] [int] IDENTITY(1,1) NOT NULL,
        [MachineCode] [nvarchar](64) NOT NULL,
        [FirstRunTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [CreatedAt] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_TrialRecords] PRIMARY KEY CLUSTERED ([TrialRecordId] ASC),
        CONSTRAINT [UQ_TrialRecords_MachineCode] UNIQUE NONCLUSTERED ([MachineCode] ASC)
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

-- RolePermissions FKs
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_RolePermissions_Roles')
BEGIN
    ALTER TABLE [dbo].[RolePermissions] ADD CONSTRAINT [FK_RolePermissions_Roles]
    FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_RolePermissions_Permissions')
BEGIN
    ALTER TABLE [dbo].[RolePermissions] ADD CONSTRAINT [FK_RolePermissions_Permissions']
    FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions] ([PermissionId]) ON DELETE CASCADE;
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

-- OperationLogs FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_OperationLogs_Users')
BEGIN
    ALTER TABLE [dbo].[OperationLogs] ADD CONSTRAINT [FK_OperationLogs_Users]
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE SET NULL;
END
GO

-- LoginLogs FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_LoginLogs_Users')
BEGIN
    ALTER TABLE [dbo].[LoginLogs] ADD CONSTRAINT [FK_LoginLogs_Users]
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE SET NULL;
END
GO

-- Permissions Parent FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Permissions_Parent')
BEGIN
    ALTER TABLE [dbo].[Permissions] ADD CONSTRAINT [FK_Permissions_Parent]
    FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Permissions] ([PermissionId]);
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

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OperationLogs_Module' AND object_id = OBJECT_ID('OperationLogs'))
BEGIN
    CREATE INDEX [IX_OperationLogs_Module] ON [dbo].[OperationLogs] ([Module] ASC);
END
GO

-- LoginLogs Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LoginLogs_LoginTime' AND object_id = OBJECT_ID('LoginLogs'))
BEGIN
    CREATE INDEX [IX_LoginLogs_LoginTime] ON [dbo].[LoginLogs] ([LoginTime] DESC);
END
GO

-- Licenses Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Licenses_MachineCode' AND object_id = OBJECT_ID('Licenses'))
BEGIN
    CREATE INDEX [IX_Licenses_MachineCode] ON [dbo].[Licenses] ([MachineCode] ASC);
END
GO

PRINT 'Database initialization completed successfully!';
GO
