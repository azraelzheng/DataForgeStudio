-- DataForgeStudio V4 - Licenses 表创建脚本
-- 适用于 SQL Server 2005+
-- 生成日期: 2026-02-04

USE [DataForgeStudio_V4]
GO

-- 检查表是否已存在
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Licenses')
BEGIN
    PRINT '正在创建 Licenses 表...'

    CREATE TABLE [dbo].[Licenses](
        [LicenseId] [INT] IDENTITY(1,1) NOT NULL,
        [LicenseKey] [NVARCHAR](512) NOT NULL,
        [Signature] [NVARCHAR](512) NOT NULL,
        [MachineCode] [NVARCHAR](64) NOT NULL,
        [ActivatedTime] [DATETIME2] NOT NULL,
        [ActivatedIP] [NVARCHAR](50) NULL,
        [CreatedTime] [DATETIME2] NOT NULL,
     CONSTRAINT [PK_Licenses] PRIMARY KEY CLUSTERED
    (
        [LicenseId] ASC
    ) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
    )

    PRINT 'Licenses 表创建成功'

    -- 创建唯一索引
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Licenses_MachineCode' AND object_id = OBJECT_ID('dbo.Licenses'))
    BEGIN
        ALTER TABLE [dbo].[Licenses]
        ADD CONSTRAINT [UQ_Licenses_MachineCode] UNIQUE NONCLUSTERED
        (
            [MachineCode] ASC
        ) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)

        PRINT 'UQ_Licenses_MachineCode 唯一索引创建成功'
    END
END
ELSE
BEGIN
    PRINT 'Licenses 表已存在，跳过创建'
END
GO

-- 验证表结构
PRINT ''
PRINT '=== Licenses 表结构验证 ==='
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Licenses'
ORDER BY ORDINAL_POSITION
GO

PRINT ''
PRINT '=== 索引信息 ==='
SELECT
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    STRING_AGG(c.name, ', ') AS Columns
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('Licenses')
GROUP BY i.name, i.type_desc, i.is_unique
ORDER BY i.name
GO

PRINT ''
PRINT '脚本执行完成'
GO
