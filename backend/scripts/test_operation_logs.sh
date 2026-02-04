#!/bin/bash

# 测试操作日志功能的脚本
# 此脚本模拟各种操作并检查数据库中的日志记录

API_URL="http://localhost:5000/api"
TOKEN=""

# 颜色输出
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 登录获取token
echo -e "${YELLOW}===== 登录 =====${NC}"
LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"root","password":"root123"}')

TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo -e "${RED}登录失败！${NC}"
  exit 1
fi

echo -e "${GREEN}登录成功，获取到token${NC}"
echo ""

# 清空现有日志
echo -e "${YELLOW}===== 清空现有日志 =====${NC}"
curl -s -X DELETE "$API_URL/system/logs" \
  -H "Authorization: Bearer $TOKEN" | grep -o '"message":"[^"]*"' | cut -d'"' -f4
echo ""
sleep 1

# 测试1: 创建用户
echo -e "${YELLOW}===== 测试1: 创建用户 =====${NC}"
TIMESTAMP=$(date +%s)
curl -s -X POST "$API_URL/users" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"testuser_$TIMESTAMP\",\"realName\":\"测试用户\",\"password\":\"Test123456\",\"email\":\"test@example.com\",\"phone\":\"13800138000\"}" \
  | grep -o '"success":[^,}]*' | cut -d':' -f2
echo "创建用户: testuser_$TIMESTAMP"
sleep 1

# 测试2: 更新用户
echo -e "${YELLOW}===== 测试2: 更新用户 =====${NC}"
curl -s -X PUT "$API_URL/users/2" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"realName":"更新后的用户名","email":"updated@example.com"}' \
  | grep -o '"success":[^,}]*' | cut -d':' -f2
echo "更新用户ID: 2"
sleep 1

# 测试3: 创建角色
echo -e "${YELLOW}===== 测试3: 创建角色 =====${NC}"
curl -s -X POST "$API_URL/roles" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"roleCode\":\"TEST_ROLE_$TIMESTAMP\",\"roleName\":\"测试角色\",\"description\":\"这是一个测试角色\"}" \
  | grep -o '"success":[^,}]*' | cut -d':' -f2
echo "创建角色: 测试角色"
sleep 1

# 测试4: 创建数据源
echo -e "${YELLOW}===== 测试4: 创建数据源 =====${NC}"
curl -s -X POST "$API_URL/datasources" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"dataSourceCode\":\"TEST_DS_$TIMESTAMP\",\"dataSourceName\":\"测试数据源\",\"dbType\":\"SqlServer\",\"serverAddress\":\"localhost\",\"port\":1433,\"databaseName\":\"TestDB\",\"username\":\"sa\",\"description\":\"测试数据源\"}" \
  | grep -o '"success":[^,}]*' | cut -d':' -f2
echo "创建数据源: 测试数据源"
sleep 1

# 测试5: 测试连接
echo -e "${YELLOW}===== 测试5: 测试连接 =====${NC}"
curl -s -X POST "$API_URL/datasources/test" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"serverAddress":"localhost","port":1433,"dbType":"SqlServer","databaseName":"DataForgeStudio_V4","username":"sa","password":""}' \
  | grep -o '"success":[^,}]*' | cut -d':' -f2
echo "测试连接"
sleep 1

# 测试6: 切换状态
echo -e "${YELLOW}===== 测试6: 切换用户状态 =====${NC}"
curl -s -X POST "$API_URL/users/2/toggle-active" \
  -H "Authorization: Bearer $TOKEN" \
  | grep -o '"success":[^,}]*' | cut -d':' -f2
echo "切换用户ID: 2 的状态"
sleep 1

# 测试7: 删除用户（需要先创建一个临时用户）
echo -e "${YELLOW}===== 测试7: 删除用户 =====${NC}"
DELETE_USER_RESPONSE=$(curl -s -X POST "$API_URL/users" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"delete_test_$TIMESTAMP\",\"realName\":\"待删除用户\",\"password\":\"Test123456\"}")

sleep 1

# 获取刚创建的用户ID并删除
DELETE_USER_ID=$(curl -s -X GET "$API_URL/users?page=1&pageSize=100" \
  -H "Authorization: Bearer $TOKEN" \
  | grep -o "\"username\":\"delete_test_$TIMESTAMP\",\"userId\":[0-9]*" | grep -o "[0-9]*$" | head -1)

if [ ! -z "$DELETE_USER_ID" ]; then
  curl -s -X DELETE "$API_URL/users/$DELETE_USER_ID" \
    -H "Authorization: Bearer $TOKEN" \
    | grep -o '"success":[^,}]*' | cut -d':' -f2
  echo "删除用户ID: $DELETE_USER_ID (用户名: delete_test_$TIMESTAMP)"
fi
sleep 2

# 查询并显示日志结果
echo -e "${YELLOW}===== 查询日志结果 =====${NC}"
sleep 1
LOGS=$(sqlcmd -S localhost -d DataForgeStudio_V4 -E -Q "SET NOCOUNT ON; SELECT LogId, Username, Action, Module, Description, CONVERT(varchar, CreatedTime, 120) as CreatedTime FROM OperationLogs ORDER BY LogId DESC" -h -1 -s "|" -W)

echo "$LOGS"
echo ""

# 统计日志数量
LOG_COUNT=$(echo "$LOGS" | wc -l)
echo -e "${YELLOW}===== 检查结果 =====${NC}"
echo "总日志条数: $LOG_COUNT"
echo ""

# 检查各项功能
echo -e "${YELLOW}检查项目：${NC}"

# 检查创建用户日志
CREATE_USER_LOG=$(echo "$LOGS" | grep -i "用户创建" | grep -i "testuser")
if [ ! -z "$CREATE_USER_LOG" ]; then
  echo -e "${GREEN}✓ 创建用户日志正常${NC}"
  echo "$CREATE_USER_LOG" | head -1
else
  echo -e "${RED}✗ 创建用户日志缺失${NC}"
fi

# 检查创建角色日志
CREATE_ROLE_LOG=$(echo "$LOGS" | grep -i "角色创建" | grep -i "测试角色")
if [ ! -z "$CREATE_ROLE_LOG" ]; then
  echo -e "${GREEN}✓ 创建角色日志正常${NC}"
  echo "$CREATE_ROLE_LOG" | head -1
else
  echo -e "${RED}✗ 创建角色日志缺失${NC}"
fi

# 检查创建数据源日志
CREATE_DS_LOG=$(echo "$LOGS" | grep -i "数据源创建" | grep -i "测试数据源")
if [ ! -z "$CREATE_DS_LOG" ]; then
  echo -e "${GREEN}✓ 创建数据源日志正常${NC}"
  echo "$CREATE_DS_LOG" | head -1
else
  echo -e "${RED}✗ 创建数据源日志缺失${NC}"
fi

# 检查删除用户日志（应该显示用户名而不是ID）
DELETE_USER_LOG=$(echo "$LOGS" | grep -i "用户删除" | grep -i "delete_test")
if [ ! -z "$DELETE_USER_LOG" ]; then
  echo -e "${GREEN}✓ 删除用户日志显示用户名${NC}"
  echo "$DELETE_USER_LOG" | head -1
else
  # 检查是否显示的是ID
  DELETE_USER_ID_LOG=$(echo "$LOGS" | grep -i "用户删除.*[0-9]")
  if [ ! -z "$DELETE_USER_ID_LOG" ]; then
    echo -e "${RED}✗ 删除用户日志显示ID而不是用户名${NC}"
    echo "$DELETE_USER_ID_LOG" | head -1
  else
    echo -e "${RED}✗ 删除用户日志缺失${NC}"
  fi
fi

# 检查时间是否正确
CURRENT_TIME=$(date +%Y-%m-%d" "%H:%M)
LATEST_LOG_TIME=$(echo "$LOGS" | head -1 | cut -d'|' -f6)
echo ""
echo -e "${YELLOW}时间检查：${NC}"
echo "当前时间: $CURRENT_TIME"
echo "最新日志时间: $LATEST_LOG_TIME"

# 检查是否有重复日志
echo ""
echo -e "${YELLOW}重复日志检查：${NC}"
CREATE_COUNT=$(echo "$LOGS" | grep -i "创建" | wc -l)
echo "创建操作日志数: $CREATE_COUNT"
if [ $CREATE_COUNT -gt 3 ]; then
  echo -e "${RED}⚠ 可能存在重复日志（创建操作过多）${NC}"
fi

echo ""
echo -e "${GREEN}===== 测试完成 =====${NC}"
