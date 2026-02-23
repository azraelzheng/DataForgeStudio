-- DataForgeStudio V4 Database Migration Script
-- This script upgrades an existing database to the new structure
-- Run this script on existing installations before deploying new code

USE DataForgeStudio;
GO

PRINT 'Starting database migration...';
GO

-- ============================================================================
-- 1. Update OperationLogs table
-- ============================================================================
PRINT 'Updating OperationLogs table...';
GO

-- Check if OperationLogs needs updating (has old 'Operation' column)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'OperationLogs' AND COLUMN_NAME = 'Operation')
BEGIN
    -- Create new table with correct structure
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OperationLogs_New]'))
        DROP TABLE [dbo].[OperationLogs_New];

    CREATE TABLE [dbo].[OperationLogs_New](
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
        CONSTRAINT [PK_OperationLogs_New] PRIMARY KEY CLUSTERED ([LogId] ASC)
    );

    -- Migrate data (map old columns to new)
    INSERT INTO [dbo].[OperationLogs_New] (
        [UserId], [Username], [Module], [Action], [Description],
        [IpAddress], [UserAgent], [RequestUrl], [RequestMethod],
        [RequestData], [ResponseData], [Duration], [IsSuccess],
        [ErrorMessage], [CreatedTime]
    )
    SELECT
        NULL AS [UserId],
        [Username],
        ISNULL([Module], 'System') AS [Module],
        ISNULL([Operation], [Action], 'Unknown') AS [Action],
        [Description],
        [IpAddress],
        [Browser] AS [UserAgent],
        [RequestPath] AS [RequestUrl],
        [RequestMethod],
        [RequestParams] AS [RequestData],
        NULL AS [ResponseData],
        [Duration],
        CASE WHEN [ResponseStatus] = 200 THEN 1 ELSE 0 END AS [IsSuccess],
        [ErrorMessage],
        [CreatedTime]
    FROM [dbo].[OperationLogs];

    -- Drop old table and rename new one
    DROP TABLE [dbo].[OperationLogs];
    EXEC sp_rename 'OperationLogs_New', 'OperationLogs';
    EXEC sp_rename 'PK_OperationLogs_New', 'PK_OperationLogs', 'OBJECT';

    -- Recreate indexes
    CREATE INDEX [IX_OperationLogs_CreatedTime] ON [dbo].[OperationLogs] ([CreatedTime] DESC);
    CREATE INDEX [IX_OperationLogs_Username] ON [dbo].[OperationLogs] ([Username] ASC);
    CREATE INDEX [IX_OperationLogs_Module] ON [dbo].[OperationLogs] ([Module] ASC);

    PRINT 'OperationLogs table updated successfully.';
END
ELSE
BEGIN
    PRINT 'OperationLogs table already up to date.';
END
GO

-- ============================================================================
-- 2. Update Licenses table (Zero Trust Architecture)
-- ============================================================================
PRINT 'Updating Licenses table...';
GO

-- Check if Licenses needs updating (has old 'CompanyName' column)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Licenses' AND COLUMN_NAME = 'CompanyName')
BEGIN
    -- Create new table with correct structure
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Licenses_New]'))
        DROP TABLE [dbo].[Licenses_New];

    CREATE TABLE [dbo].[Licenses_New](
        [LicenseId] [int] IDENTITY(1,1) NOT NULL,
        [LicenseKey] [nvarchar](max) NOT NULL,
        [Signature] [nvarchar](512) NOT NULL DEFAULT '',
        [MachineCode] [nvarchar](64) NOT NULL,
        [ActivatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [ActivatedIP] [nvarchar](50) NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Licenses_New] PRIMARY KEY CLUSTERED ([LicenseId] ASC)
    );

    -- Note: Old license data cannot be migrated to new structure
    -- Users will need to re-activate their licenses

    -- Drop old table and rename new one
    DROP TABLE [dbo].[Licenses];
    EXEC sp_rename 'Licenses_New', 'Licenses';
    EXEC sp_rename 'PK_Licenses_New', 'PK_Licenses', 'OBJECT';

    -- Create index
    CREATE INDEX [IX_Licenses_MachineCode] ON [dbo].[Licenses] ([MachineCode] ASC);

    PRINT 'Licenses table updated successfully. Note: Existing licenses need to be re-activated.';
END
ELSE
BEGIN
    PRINT 'Licenses table already up to date.';
END
GO

-- ============================================================================
-- 3. Update BackupRecords table
-- ============================================================================
PRINT 'Updating BackupRecords table...';
GO

-- Add Description column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BackupRecords' AND COLUMN_NAME = 'Description')
BEGIN
    ALTER TABLE [dbo].[BackupRecords] ADD [Description] [nvarchar](500) NULL;
    PRINT 'Added Description column to BackupRecords.';
END
ELSE
BEGIN
    PRINT 'BackupRecords table already up to date.';
END
GO

-- ============================================================================
-- 4. Create BackupSchedules table if not exists
-- ============================================================================
PRINT 'Checking BackupSchedules table...';
GO

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
    PRINT 'BackupSchedules table created.';
END
ELSE
BEGIN
    PRINT 'BackupSchedules table already exists.';
END
GO

-- ============================================================================
-- 5. Create TrialRecords table if not exists
-- ============================================================================
PRINT 'Checking TrialRecords table...';
GO

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
    PRINT 'TrialRecords table created.';
END
ELSE
BEGIN
    PRINT 'TrialRecords table already exists.';
END
GO

-- ============================================================================
-- 6. Create/Update Permissions table
-- ============================================================================
PRINT 'Checking Permissions table...';
GO

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
    PRINT 'Permissions table created.';
END
ELSE
BEGIN
    PRINT 'Permissions table already exists.';
END
GO

-- ============================================================================
-- 7. Update RolePermissions table
-- ============================================================================
PRINT 'Checking RolePermissions table...';
GO

-- Check if RolePermissions has old structure (PermissionCode instead of PermissionId)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RolePermissions' AND COLUMN_NAME = 'PermissionCode')
    AND NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RolePermissions' AND COLUMN_NAME = 'PermissionId')
BEGIN
    -- Drop old RolePermissions and recreate with new structure
    DROP TABLE [dbo].[RolePermissions];

    CREATE TABLE [dbo].[RolePermissions](
        [RolePermissionId] [int] IDENTITY(1,1) NOT NULL,
        [RoleId] [int] NOT NULL,
        [PermissionId] [int] NOT NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([RolePermissionId] ASC),
        CONSTRAINT [UQ_RolePermissions_Role_Permission] UNIQUE NONCLUSTERED ([RoleId] ASC, [PermissionId] ASC)
    );

    -- Add foreign keys
    ALTER TABLE [dbo].[RolePermissions] ADD CONSTRAINT [FK_RolePermissions_Roles]
    FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE CASCADE;

    ALTER TABLE [dbo].[RolePermissions] ADD CONSTRAINT [FK_RolePermissions_Permissions]
    FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions] ([PermissionId]) ON DELETE CASCADE;

    PRINT 'RolePermissions table updated to new structure.';
END
ELSE
BEGIN
    PRINT 'RolePermissions table already up to date.';
END
GO

-- ============================================================================
-- 8. Update LoginLogs table
-- ============================================================================
PRINT 'Checking LoginLogs table...';
GO

-- Add missing columns if needed
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'LoginLogs' AND COLUMN_NAME = 'UserId')
BEGIN
    ALTER TABLE [dbo].[LoginLogs] ADD [UserId] [int] NULL;
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'LoginLogs' AND COLUMN_NAME = 'UserAgent')
BEGIN
    ALTER TABLE [dbo].[LoginLogs] ADD [UserAgent] [nvarchar](500) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'LoginLogs' AND COLUMN_NAME = 'LoginStatus')
BEGIN
    ALTER TABLE [dbo].[LoginLogs] ADD [LoginStatus] [nvarchar](20) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'LoginLogs' AND COLUMN_NAME = 'SessionId')
BEGIN
    ALTER TABLE [dbo].[LoginLogs] ADD [SessionId] [nvarchar](100) NULL;
END
GO

PRINT 'LoginLogs table checked.';
GO

PRINT '============================================';
PRINT 'Database migration completed successfully!';
PRINT '============================================';
GO
