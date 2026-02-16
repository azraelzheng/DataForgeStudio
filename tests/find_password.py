"""
Reset root user password using API
"""
import requests
import json

# 尝试使用 API 直接更新密码
API_BASE = "http://localhost:5000"

# 首先，我们需要获取临时密码或者创建一个新的管理员用户
# 由于我们无法直接登录，让我们尝试通过数据库初始化获取密码

# 方法 1: 检查是否有默认测试用户
test_users = [
    {"username": "admin", "password": "admin123"},
    {"username": "test", "password": "test123"},
    {"username": "demo", "password": "demo123"},
]

print("Testing default user credentials...")
for user in test_users:
    response = requests.post(
        f"{API_BASE}/api/auth/login",
        json=user,
        headers={"Content-Type": "application/json"}
    )
    if response.status_code == 200:
        data = response.json()
        if data.get("Success") or data.get("success"):
            print(f"✓ Found working user: {user['username']} / {user['password']}")
            print(f"  Response: {json.dumps(data, indent=2)}")
            break
    else:
        print(f"✗ {user['username']}: {response.status_code} - {response.text[:200]}")

# 方法 2: 尝试注册新用户（如果允许）
print("\nTrying to create a new user...")
# 这通常需要先登录，所以可能不会工作

print("\nNote: The root user has a random temporary password.")
print("You need to either:")
print("1. Check the backend console output when it first started")
print("2. Reset the password using SQL directly")
print("3. Use the password reset tool with correct SQL connection")
