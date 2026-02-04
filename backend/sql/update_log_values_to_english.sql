-- Update operation log values from Chinese to English
-- This ensures consistency with the middleware which now uses English values

USE DataForgeStudio_V4;
GO

-- Update Action values (Chinese to English)
UPDATE OperationLogs SET Action = 'Create' WHERE Action = '创建';
UPDATE OperationLogs SET Action = 'Update' WHERE Action = '更新';
UPDATE OperationLogs SET Action = 'Delete' WHERE Action = '删除';
UPDATE OperationLogs SET Action = 'Toggle' WHERE Action = '切换状态';
UPDATE OperationLogs SET Action = 'TestConnection' WHERE Action = '测试连接';
UPDATE OperationLogs SET Action = 'GetDatabases' WHERE Action = '获取数据库列表';
UPDATE OperationLogs SET Action = 'Modify' WHERE Action = '修改';
UPDATE OperationLogs SET Action = 'Login' WHERE Action = '登录';
UPDATE OperationLogs SET Action = 'Logout' WHERE Action = '登出';
GO

-- Update Module values (Chinese to English)
UPDATE OperationLogs SET Module = 'User' WHERE Module = '用户管理';
UPDATE OperationLogs SET Module = 'Role' WHERE Module = '角色管理';
UPDATE OperationLogs SET Module = 'DataSource' WHERE Module = '数据源管理';
UPDATE OperationLogs SET Module = 'Report' WHERE Module = '报表管理';
UPDATE OperationLogs SET Module = 'License' WHERE Module = '许可管理';
UPDATE OperationLogs SET Module = 'System' WHERE Module = '系统管理';
UPDATE OperationLogs SET Module = 'Other' WHERE Module = '其他';
GO

-- Display updated records
SELECT Id, Username, Action, Module, Description, CreatedTime
FROM OperationLogs
ORDER BY CreatedTime DESC;
GO

PRINT 'Operation log values have been updated to English.';
GO
