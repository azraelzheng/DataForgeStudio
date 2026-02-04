#!/bin/bash

# 简化的测试脚本 - 直接测试日志功能

echo "===== 操作日志测试 ====="
echo ""

# 清空现有日志
echo "1. 清空现有日志..."
sqlcmd -S localhost -d DataForgeStudio_V4 -E -Q "DELETE FROM OperationLogs" -h -1
echo "✓ 日志已清空"
echo ""

# 插入测试日志 - 使用本地时间
echo "2. 插入测试日志..."
CURRENT_TIME=$(date "+%Y-%m-%d %H:%M:%S")

sqlcmd -S localhost -d DataForgeStudio_V4 -E -Q "
INSERT INTO OperationLogs (Username, UserId, Action, Module, Description, IpAddress, RequestUrl, RequestMethod, Duration, IsSuccess, CreatedTime)
VALUES ('root', 3, 'Create', 'User', N'用户创建: testuser_001', '127.0.0.1', '/api/users', 'POST', 100, 1, '$CURRENT_TIME');

INSERT INTO OperationLogs (Username, UserId, Action, Module, Description, IpAddress, RequestUrl, RequestMethod, Duration, IsSuccess, CreatedTime)
VALUES ('root', 3, 'Create', 'Role', N'角色创建: 测试角色', '127.0.0.1', '/api/roles', 'POST', 150, 1, '$CURRENT_TIME');

INSERT INTO OperationLogs (Username, UserId, Action, Module, Description, IpAddress, RequestUrl, RequestMethod, Duration, IsSuccess, CreatedTime)
VALUES ('root', 3, 'Create', 'DataSource', N'数据源创建: 生产数据库', '127.0.0.1', '/api/datasources', 'POST', 200, 1, '$CURRENT_TIME');

INSERT INTO OperationLogs (Username, UserId, Action, Module, Description, IpAddress, RequestUrl, RequestMethod, Duration, IsSuccess, CreatedTime)
VALUES ('root', 3, 'Update', 'User', N'用户更新: admin', '127.0.0.1', '/api/users/4', 'PUT', 80, 1, '$CURRENT_TIME');

INSERT INTO OperationLogs (Username, UserId, Action, Module, Description, IpAddress, RequestUrl, RequestMethod, Duration, IsSuccess, CreatedTime)
VALUES ('root', 3, 'Delete', 'User', N'用户删除: testuser_old', '127.0.0.1', '/api/users/999', 'DELETE', 50, 1, '$CURRENT_TIME');

INSERT INTO OperationLogs (Username, UserId, Action, Module, Description, IpAddress, RequestUrl, RequestMethod, Duration, IsSuccess, CreatedTime)
VALUES ('root', 3, 'Toggle', 'User', N'用户切换状态: admin', '127.0.0.1', '/api/users/4/toggle-active', 'POST', 60, 1, '$CURRENT_TIME');

INSERT INTO OperationLogs (Username, UserId, Action, Module, Description, IpAddress, RequestUrl, RequestMethod, Duration, IsSuccess, CreatedTime)
VALUES ('root', 3, 'TestConnection', 'DataSource', N'测试数据源连接: 测试库', '127.0.0.1', '/api/datasources/test', 'POST', 300, 1, '$CURRENT_TIME');
" -h -1

echo "✓ 插入了7条测试日志"
echo ""

# 查询并显示日志
echo "3. 查询日志结果："
echo "========================================"
sqlcmd -S localhost -d DataForgeStudio_V4 -E -Q "
SET NOCOUNT ON;
SELECT
  LogId as 'ID',
  Username as '操作人',
  Action as '操作',
  Module as '模块',
  Description as '描述',
  CONVERT(varchar, CreatedTime, 120) as '时间'
FROM OperationLogs
ORDER BY LogId DESC" -W -s "|"
echo "========================================"
echo ""

# 统计
echo "4. 统计信息："
TOTAL_COUNT=$(sqlcmd -S localhost -d DataForgeStudio_V4 -E -Q "SELECT COUNT(*) FROM OperationLogs" -h -1)
echo "总日志数: $TOTAL_COUNT"

# 按操作类型统计
echo ""
echo "按操作类型统计:"
sqlcmd -S localhost -d DataForgeStudio_V4 -E -Q "
SET NOCOUNT ON;
SELECT Action as '操作类型', COUNT(*) as '数量'
FROM OperationLogs
GROUP BY Action
ORDER BY COUNT(*) DESC" -W -h -1

# 按模块统计
echo ""
echo "按模块统计:"
sqlcmd -S localhost -d DataForgeStudio_V4 -E -Q "
SET NOCOUNT ON;
SELECT Module as '模块', COUNT(*) as '数量'
FROM OperationLogs
GROUP BY Module
ORDER BY COUNT(*) DESC" -W -h -1

echo ""
echo "===== 检查要点 ====="
echo "1. 时间是否为本地时间（不是UTC）"
echo "2. 描述是否显示资源名称而不是ID"
echo "3. 操作类型和模块是否正确"
echo "4. 是否有重复日志"
echo ""

# 检查时间是否为本地时间
echo "当前时间: $(date '+%Y-%m-%d %H:%M:%S')"
LATEST_LOG=$(sqlcmd -S localhost -d DataForgeStudio_V4 -E -Q "SELECT TOP 1 CONVERT(varchar, CreatedTime, 120) FROM OperationLogs ORDER BY LogId DESC" -h -1)
echo "最新日志时间: $LATEST_LOG"
echo ""

echo "===== 测试完成 ====="
