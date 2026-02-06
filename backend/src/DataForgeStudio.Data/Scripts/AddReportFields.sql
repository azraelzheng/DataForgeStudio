-- 检查列是否已存在，如果不存在则添加
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reports') AND name = 'ChartConfig')
BEGIN
    ALTER TABLE Reports ADD ChartConfig NVARCHAR(2000) NULL;
    PRINT 'Added ChartConfig column';
END
ELSE
BEGIN
    PRINT 'ChartConfig column already exists';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reports') AND name = 'EnableChart')
BEGIN
    ALTER TABLE Reports ADD EnableChart BIT NOT NULL CONSTRAINT DF_Reports_EnableChart DEFAULT 0;
    PRINT 'Added EnableChart column';
END
ELSE
BEGIN
    PRINT 'EnableChart column already exists';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reports') AND name = 'QueryConditions')
BEGIN
    ALTER TABLE Reports ADD QueryConditions NVARCHAR(2000) NULL;
    PRINT 'Added QueryConditions column';
END
ELSE
BEGIN
    PRINT 'QueryConditions column already exists';
END
