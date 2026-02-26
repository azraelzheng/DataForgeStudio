-- ============================================================================
-- DataForgeStudio V1.0.0 Database Initialization Script
-- Compatible with SQL Server 2005+
-- Generated from Entity Definitions: 2026-02-27
-- ============================================================================
--
-- IMPORTANT: This script is for MANUAL database setup only.
-- The application uses Entity Framework Core Code First approach.
-- This script serves as a reference for the expected database schema.
--
-- Normal installation: Application auto-creates database via DbInitializer
-- Manual setup: Run this script if you prefer to create database manually
-- ============================================================================

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
-- TABLES
-- ============================================================================

-- ---------------------------------------------------------------------------
-- Users Table
-- ---------------------------------------------------------------------------
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
        [LastLoginTime] [datetime2] NULL,
        [LastLoginIP] [nvarchar](50) NULL,
        [PasswordFailCount] [int] NOT NULL DEFAULT 0,
        [MustChangePassword] [bit] NOT NULL DEFAULT 0,
        [Remark] [nvarchar](500) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime2] NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [CK_Users_IsSystem] CHECK ([IsSystem] = 0 OR [Username] = 'root')
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Username] ON [dbo].[Users]([Username]);
    CREATE NONCLUSTERED INDEX [IX_Users_IsActive] ON [dbo].[Users]([IsActive]);
    CREATE NONCLUSTERED INDEX [IX_Users_IsSystem] ON [dbo].[Users]([IsSystem]);
END
GO

-- ---------------------------------------------------------------------------
-- Roles Table
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Roles](
        [RoleId] [int] IDENTITY(1,1) NOT NULL,
        [RoleName] [nvarchar](50) NOT NULL,
        [RoleCode] [nvarchar](50) NOT NULL,
        [Description] [nvarchar](200) NULL,
        [Permissions] [nvarchar](MAX) NULL,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime2] NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC)
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Roles_RoleCode] ON [dbo].[Roles]([RoleCode]);
    CREATE NONCLUSTERED INDEX [IX_Roles_IsActive] ON [dbo].[Roles]([IsActive]);
END
GO

-- ---------------------------------------------------------------------------
-- UserRoles Table
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserRoles](
        [UserRoleId] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NOT NULL,
        [RoleId] [int] NOT NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserRoleId] ASC),
        CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([RoleId]) ON DELETE CASCADE
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserRoles_UserId_RoleId] ON [dbo].[UserRoles]([UserId], [RoleId]);
END
GO

-- ---------------------------------------------------------------------------
-- Permissions Table
-- ---------------------------------------------------------------------------
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
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([PermissionId] ASC),
        CONSTRAINT [FK_Permissions_Parent] FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Permissions]([PermissionId]) ON DELETE RESTRICT
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Permissions_PermissionCode] ON [dbo].[Permissions]([PermissionCode]);
    CREATE NONCLUSTERED INDEX [IX_Permissions_Module] ON [dbo].[Permissions]([Module]);
    CREATE NONCLUSTERED INDEX [IX_Permissions_ParentId] ON [dbo].[Permissions]([ParentId]);
END
GO

-- ---------------------------------------------------------------------------
-- RolePermissions Table
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolePermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RolePermissions](
        [RolePermissionId] [int] IDENTITY(1,1) NOT NULL,
        [RoleId] [int] NOT NULL,
        [PermissionId] [int] NOT NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([RolePermissionId] ASC),
        CONSTRAINT [FK_RolePermissions_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([RoleId]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Permissions] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions]([PermissionId]) ON DELETE CASCADE
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_RolePermissions_RoleId_PermissionId] ON [dbo].[RolePermissions]([RoleId], [PermissionId]);
END
GO

-- ---------------------------------------------------------------------------
-- DataSources Table
-- ---------------------------------------------------------------------------
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
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime2] NULL,
        [LastTestTime] [datetime2] NULL,
        [LastTestResult] [bit] NULL,
        [LastTestMessage] [nvarchar](500) NULL,
        CONSTRAINT [PK_DataSources] PRIMARY KEY CLUSTERED ([DataSourceId] ASC)
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_DataSources_DataSourceCode] ON [dbo].[DataSources]([DataSourceCode]);
    CREATE NONCLUSTERED INDEX [IX_DataSources_IsActive] ON [dbo].[DataSources]([IsActive]);
    CREATE NONCLUSTERED INDEX [IX_DataSources_IsDefault] ON [dbo].[DataSources]([IsDefault]);
END
GO

-- ---------------------------------------------------------------------------
-- Reports Table
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Reports]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Reports](
        [ReportId] [int] IDENTITY(1,1) NOT NULL,
        [ReportName] [nvarchar](100) NOT NULL,
        [ReportCode] [nvarchar](50) NOT NULL,
        [ReportCategory] [nvarchar](50) NULL,
        [DataSourceId] [int] NOT NULL,
        [SqlStatement] [nvarchar](MAX) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsPaged] [bit] NOT NULL DEFAULT 1,
        [PageSize] [int] NOT NULL DEFAULT 50,
        [CacheDuration] [int] NOT NULL DEFAULT 0,
        [IsEnabled] [bit] NOT NULL DEFAULT 1,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [ViewCount] [int] NOT NULL DEFAULT 0,
        [LastViewTime] [datetime2] NULL,
        [Remark] [nvarchar](500) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime2] NULL,
        [ChartConfig] [nvarchar](2000) NULL,
        [EnableChart] [bit] NOT NULL DEFAULT 0,
        [QueryConditions] [nvarchar](2000) NULL,
        CONSTRAINT [PK_Reports] PRIMARY KEY CLUSTERED ([ReportId] ASC),
        CONSTRAINT [FK_Reports_DataSources] FOREIGN KEY ([DataSourceId]) REFERENCES [dbo].[DataSources]([DataSourceId]) ON DELETE RESTRICT,
        CONSTRAINT [CK_Reports_SqlStatement] CHECK ([SqlStatement] LIKE 'SELECT%' OR [SqlStatement] LIKE 'select%')
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Reports_ReportCode] ON [dbo].[Reports]([ReportCode]);
    CREATE NONCLUSTERED INDEX [IX_Reports_IsEnabled] ON [dbo].[Reports]([IsEnabled]);
    CREATE NONCLUSTERED INDEX [IX_Reports_ReportCategory] ON [dbo].[Reports]([ReportCategory]);
    CREATE NONCLUSTERED INDEX [IX_Reports_DataSourceId] ON [dbo].[Reports]([DataSourceId]);
END
GO

-- ---------------------------------------------------------------------------
-- ReportFields Table
-- ---------------------------------------------------------------------------
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
        [SummaryType] [nvarchar](10) NULL DEFAULT 'none',
        [SummaryDecimals] [int] NULL,
        [IsFilterable] [bit] NOT NULL DEFAULT 0,
        [IsGroupable] [bit] NOT NULL DEFAULT 0,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [Align] [nvarchar](10) NOT NULL DEFAULT 'left',
        [FormatString] [nvarchar](50) NULL,
        [AggregateFunction] [nvarchar](20) NULL,
        [CssClass] [nvarchar](100) NULL,
        [Remark] [nvarchar](200) NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ReportFields] PRIMARY KEY CLUSTERED ([FieldId] ASC),
        CONSTRAINT [FK_ReportFields_Reports] FOREIGN KEY ([ReportId]) REFERENCES [dbo].[Reports]([ReportId]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_ReportFields_ReportId] ON [dbo].[ReportFields]([ReportId]);
END
GO

-- ---------------------------------------------------------------------------
-- ReportParameters Table
-- ---------------------------------------------------------------------------
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
        [Options] [nvarchar](MAX) NULL,
        [QueryOptions] [nvarchar](MAX) NULL,
        [Remark] [nvarchar](200) NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ReportParameters] PRIMARY KEY CLUSTERED ([ParameterId] ASC),
        CONSTRAINT [FK_ReportParameters_Reports] FOREIGN KEY ([ReportId]) REFERENCES [dbo].[Reports]([ReportId]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_ReportParameters_ReportId] ON [dbo].[ReportParameters]([ReportId]);
END
GO

-- ---------------------------------------------------------------------------
-- OperationLogs Table
-- ---------------------------------------------------------------------------
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
        [RequestData] [nvarchar](MAX) NULL,
        [ResponseData] [nvarchar](MAX) NULL,
        [Duration] [int] NULL,
        [IsSuccess] [bit] NOT NULL DEFAULT 1,
        [ErrorMessage] [nvarchar](MAX) NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_OperationLogs] PRIMARY KEY CLUSTERED ([LogId] ASC),
        CONSTRAINT [FK_OperationLogs_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE SET NULL
    );

    CREATE NONCLUSTERED INDEX [IX_OperationLogs_UserId] ON [dbo].[OperationLogs]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_OperationLogs_Module] ON [dbo].[OperationLogs]([Module]);
    CREATE NONCLUSTERED INDEX [IX_OperationLogs_CreatedTime] ON [dbo].[OperationLogs]([CreatedTime]);
END
GO

-- ---------------------------------------------------------------------------
-- LoginLogs Table
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LoginLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LoginLogs](
        [LogId] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NULL,
        [Username] [nvarchar](50) NULL,
        [LoginTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        [LogoutTime] [datetime2] NULL,
        [IpAddress] [nvarchar](50) NULL,
        [UserAgent] [nvarchar](500) NULL,
        [LoginStatus] [nvarchar](20) NULL,
        [FailureReason] [nvarchar](200) NULL,
        [SessionId] [nvarchar](100) NULL,
        CONSTRAINT [PK_LoginLogs] PRIMARY KEY CLUSTERED ([LogId] ASC),
        CONSTRAINT [FK_LoginLogs_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE SET NULL
    );

    CREATE NONCLUSTERED INDEX [IX_LoginLogs_UserId] ON [dbo].[LoginLogs]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_LoginLogs_LoginTime] ON [dbo].[LoginLogs]([LoginTime]);
END
GO

-- ---------------------------------------------------------------------------
-- SystemConfigs Table
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SystemConfigs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SystemConfigs](
        [ConfigId] [int] IDENTITY(1,1) NOT NULL,
        [ConfigKey] [nvarchar](100) NOT NULL,
        [ConfigValue] [nvarchar](MAX) NULL,
        [ConfigType] [nvarchar](20) NOT NULL DEFAULT 'String',
        [Description] [nvarchar](200) NULL,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime2] NULL,
        CONSTRAINT [PK_SystemConfigs] PRIMARY KEY CLUSTERED ([ConfigId] ASC)
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_SystemConfigs_ConfigKey] ON [dbo].[SystemConfigs]([ConfigKey]);
END
GO

-- ---------------------------------------------------------------------------
-- BackupRecords Table
-- ---------------------------------------------------------------------------
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
        [BackupTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        [IsSuccess] [bit] NOT NULL DEFAULT 1,
        [ErrorMessage] [nvarchar](MAX) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_BackupRecords] PRIMARY KEY CLUSTERED ([BackupId] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_BackupRecords_BackupTime] ON [dbo].[BackupRecords]([BackupTime]);
END
GO

-- ---------------------------------------------------------------------------
-- BackupSchedules Table
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BackupSchedules]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BackupSchedules](
        [ScheduleId] [int] IDENTITY(1,1) NOT NULL,
        [ScheduleName] [nvarchar](100) NOT NULL,
        [ScheduleType] [nvarchar](20) NOT NULL DEFAULT 'Recurring',
        [RecurringDays] [nvarchar](50) NULL,
        [ScheduledTime] [nvarchar](10) NULL,
        [OnceDate] [datetime2] NULL,
        [RetentionCount] [int] NOT NULL DEFAULT 10,
        [BackupPath] [nvarchar](500) NULL,
        [IsEnabled] [bit] NOT NULL DEFAULT 1,
        [LastRunTime] [datetime2] NULL,
        [NextRunTime] [datetime2] NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedTime] [datetime2] NULL,
        CONSTRAINT [PK_BackupSchedules] PRIMARY KEY CLUSTERED ([ScheduleId] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_BackupSchedules_NextRunTime] ON [dbo].[BackupSchedules]([NextRunTime]);
    CREATE NONCLUSTERED INDEX [IX_BackupSchedules_IsEnabled] ON [dbo].[BackupSchedules]([IsEnabled]);
END
GO

-- ---------------------------------------------------------------------------
-- Licenses Table
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Licenses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Licenses](
        [LicenseId] [int] IDENTITY(1,1) NOT NULL,
        [LicenseKey] [nvarchar](4096) NOT NULL,
        [Signature] [nvarchar](512) NOT NULL,
        [MachineCode] [nvarchar](64) NOT NULL,
        [ActivatedTime] [datetime2] NOT NULL,
        [ActivatedIP] [nvarchar](50) NULL,
        [CreatedTime] [datetime2] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Licenses] PRIMARY KEY CLUSTERED ([LicenseId] ASC)
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Licenses_MachineCode] ON [dbo].[Licenses]([MachineCode]);
END
GO

PRINT '============================================================================';
PRINT 'DataForgeStudio V1.0.0 Database Schema Created Successfully';
PRINT '============================================================================';
PRINT '';
PRINT 'Tables created:';
PRINT '  - Users, Roles, UserRoles, Permissions, RolePermissions';
PRINT '  - DataSources, Reports, ReportFields, ReportParameters';
PRINT '  - OperationLogs, LoginLogs';
PRINT '  - SystemConfigs, BackupRecords, BackupSchedules, Licenses';
PRINT '';
PRINT 'NOTE: Application uses Entity Framework Core for database initialization.';
PRINT 'This script is for reference or manual database setup.';
PRINT '';
PRINT 'Default credentials (auto-created by application):';
PRINT '  root / Admin@123 (System Administrator)';
PRINT '============================================================================';
GO
