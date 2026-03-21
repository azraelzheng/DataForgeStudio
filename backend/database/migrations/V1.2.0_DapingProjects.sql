-- =============================================
-- DataForgeStudio V1.2.0 DapingProjects Migration
-- =============================================
-- 创建时间: 2026-03-22
-- 功能说明: 创建高级大屏(go-view)项目表
-- 兼容性: SQL Server 2005+

-- 创建高级大屏项目表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DapingProjects]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DapingProjects] (
        [ProjectId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [State] INT NOT NULL DEFAULT 1,
        [Content] NVARCHAR(MAX) NOT NULL DEFAULT '{}',
        [PublicUrl] NVARCHAR(50) NULL,
        [CreatedBy] INT NULL,
        [CreatedTime] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedTime] DATETIME NULL,
        CONSTRAINT [FK_DapingProjects_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([UserId])
    );

    CREATE INDEX [IX_DapingProjects_State] ON [dbo].[DapingProjects]([State]);
    CREATE INDEX [IX_DapingProjects_CreatedBy] ON [dbo].[DapingProjects]([CreatedBy]);
    CREATE INDEX [IX_DapingProjects_PublicUrl] ON [dbo].[DapingProjects]([PublicUrl]);
    CREATE INDEX [IX_DapingProjects_CreatedTime] ON [dbo].[DapingProjects]([CreatedTime]);

    PRINT 'Table [DapingProjects] created successfully';
END
ELSE
BEGIN
    PRINT 'Table [DapingProjects] already exists, skipping...';
END
GO

-- =============================================
-- 迁移完成
-- =============================================
PRINT '========================================';
PRINT 'V1.2.0 DapingProjects Migration Completed';
PRINT '========================================';
