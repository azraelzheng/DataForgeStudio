-- 为 OperationLogs 表添加缺失的列
USE DataForgeStudio;
GO

-- 检查并添加列
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OperationLogs') AND name = 'ActionType')
BEGIN
    ALTER TABLE OperationLogs ADD ActionType NVARCHAR(20);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OperationLogs') AND name = 'UserAgent')
BEGIN
    ALTER TABLE OperationLogs ADD UserAgent NVARCHAR(500);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OperationLogs') AND name = 'RequestUrl')
BEGIN
    ALTER TABLE OperationLogs ADD RequestUrl NVARCHAR(500);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OperationLogs') AND name = 'RequestMethod')
BEGIN
    ALTER TABLE OperationLogs ADD RequestMethod NVARCHAR(10);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OperationLogs') AND name = 'RequestData')
BEGIN
    ALTER TABLE OperationLogs ADD RequestData NVARCHAR(MAX);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OperationLogs') AND name = 'ResponseData')
BEGIN
    ALTER TABLE OperationLogs ADD ResponseData NVARCHAR(MAX);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OperationLogs') AND name = 'Duration')
BEGIN
    ALTER TABLE OperationLogs ADD Duration INT;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OperationLogs') AND name = 'IsSuccess')
BEGIN
    ALTER TABLE OperationLogs ADD IsSuccess BIT DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OperationLogs') AND name = 'ErrorMessage')
BEGIN
    ALTER TABLE OperationLogs ADD ErrorMessage NVARCHAR(MAX);
END
GO

PRINT 'OperationLogs 表列更新完成';
