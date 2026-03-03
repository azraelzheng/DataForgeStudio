# -*- coding: utf-8 -*-
"""
Dashboard V1.1.0 Full E2E Test
完整端到端测试 - 需要前后端服务运行

测试内容:
1. 登录功能
2. 大屏列表页面
3. 大屏创建
4. 大屏设计器
5. 大屏展示
6. 公开访问
7. API 测试
"""

from playwright.sync_api import sync_playwright
import os
import sys
import time

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

# 配置
CONFIG = {
    'base_url': 'http://localhost:9999',
    'api_url': 'http://localhost:5000',
    'username': 'root',
    'password': 'ADmin@123',
    'screenshot_dir': 'test-results/dashboard-full',
    'headless': False,  # 使用可见模式提高稳定性
}

# 测试数据
TEST_DASHBOARD = {
    'name': f'E2E完整测试_{int(time.time())}',
    'description': 'E2E自动化测试创建的大屏',
}

class TestResult:
    def __init__(self):
        self.passed = 0
        self.failed = 0
        self.skipped = 0
        self.results = []

    def add(self, name, status, message=''):
        self.results.append({'name': name, 'status': status, 'message': message})
        if status == 'pass':
            self.passed += 1
        elif status == 'fail':
            self.failed += 1
        else:
            self.skipped += 1

    def print_summary(self):
        print("\n" + "="*60)
        print("测试结果汇总")
        print("="*60)
        for r in self.results:
            symbol = '✅' if r['status'] == 'pass' else ('❌' if r['status'] == 'fail' else '⏭️')
            print(f"{symbol} {r['name']}: {r['message']}")
        print("-"*60)
        print(f"✅ 通过: {self.passed}")
        print(f"❌ 失败: {self.failed}")
        print(f"⏭️ 跳过: {self.skipped}")
        print("="*60)
        return self.failed == 0


def run_tests():
    os.makedirs(CONFIG['screenshot_dir'], exist_ok=True)
    result = TestResult()

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=CONFIG['headless'], slow_mo=200)
        context = browser.new_context(
            viewport={'width': 1920, 'height': 1080},
            locale='zh-CN',
        )
        page = context.new_page()

        try:
            # ==================== Test 1: 登录 ====================
            print("\n▶️ Test 1: 登录功能")
            try:
                page.goto(f"{CONFIG['base_url']}/login")
                page.wait_for_load_state('networkidle')
                time.sleep(2)

                # 填写登录表单
                username_input = page.locator('input[placeholder="请输入用户名"]')
                password_input = page.locator('input[placeholder="请输入密码"]')

                if username_input.count() > 0:
                    username_input.fill(CONFIG['username'])
                if password_input.count() > 0:
                    password_input.fill(CONFIG['password'])

                time.sleep(0.5)

                # 点击登录按钮 (使用更精确的选择器)
                login_btn = page.locator('button:has-text("登 录"), button.el-button--primary')
                if login_btn.count() > 0:
                    login_btn.first.click()
                    print("   点击登录按钮")
                else:
                    # 备用选择器
                    page.click('button:has-text("登")')
                    print("   点击登录按钮(备用)")

                # 等待登录成功
                time.sleep(5)
                page.wait_for_load_state('networkidle')

                current_url = page.url
                print(f"   当前URL: {current_url}")

                if '/login' not in current_url:
                    result.add('登录功能', 'pass', '登录成功')
                    page.screenshot(path=f"{CONFIG['screenshot_dir']}/01-login-success.png")
                else:
                    # 检查是否有错误消息
                    error_locator = page.locator('.el-message--error')
                    error_msg = error_locator.text_content() if error_locator.count() > 0 else '无错误提示'
                    result.add('登录功能', 'fail', f'登录后仍在登录页，错误: {error_msg}')
                    page.screenshot(path=f"{CONFIG['screenshot_dir']}/01-login-fail.png")
            except Exception as e:
                result.add('登录功能', 'fail', str(e))
                page.screenshot(path=f"{CONFIG['screenshot_dir']}/01-login-error.png")

            # ==================== Test 2: 大屏列表页面 ====================
            print("\n▶️ Test 2: 大屏列表页面")
            try:
                page.goto(f"{CONFIG['base_url']}/dashboard/list")
                page.wait_for_load_state('networkidle')
                time.sleep(1)

                if '/dashboard' in page.url:
                    content = page.content()
                    if len(content) > 500:
                        result.add('大屏列表页面', 'pass', '页面加载正常')
                        page.screenshot(path=f"{CONFIG['screenshot_dir']}/02-dashboard-list.png")
                    else:
                        result.add('大屏列表页面', 'fail', '页面内容为空')
                else:
                    result.add('大屏列表页面', 'fail', f'URL不正确: {page.url}')
            except Exception as e:
                result.add('大屏列表页面', 'fail', str(e))

            # ==================== Test 3: 创建大屏 ====================
            print("\n▶️ Test 3: 创建大屏")
            try:
                page.goto(f"{CONFIG['base_url']}/dashboard/list")
                page.wait_for_load_state('networkidle')
                time.sleep(0.5)

                # 查找新建按钮
                new_btn_locator = page.locator('button:has-text("新"), button:has-text("创建")')
                if new_btn_locator.count() > 0:
                    new_btn_locator.first.click()
                    page.wait_for_timeout(1000)

                    # 填写表单
                    name_input_locator = page.locator('.el-dialog input, .el-drawer input')
                    if name_input_locator.count() > 0:
                        name_input_locator.first.fill(TEST_DASHBOARD['name'])

                        # 尝试保存
                        save_btn_locator = page.locator('button:has-text("确定"), button:has-text("保存")')
                        if save_btn_locator.count() > 0:
                            save_btn_locator.first.click()
                            page.wait_for_timeout(2000)

                            result.add('创建大屏', 'pass', f'已提交创建: {TEST_DASHBOARD["name"]}')
                            page.screenshot(path=f"{CONFIG['screenshot_dir']}/03-create-dashboard.png")
                        else:
                            result.add('创建大屏', 'fail', '未找到保存按钮')
                    else:
                        result.add('创建大屏', 'fail', '未找到名称输入框')
                else:
                    result.add('创建大屏', 'skip', '未找到新建按钮')
            except Exception as e:
                result.add('创建大屏', 'fail', str(e))

            # ==================== Test 4: 大屏设计器 ====================
            print("\n▶️ Test 4: 大屏设计器")
            try:
                page.goto(f"{CONFIG['base_url']}/dashboard/designer")
                page.wait_for_load_state('networkidle')
                time.sleep(1)

                if '/dashboard/designer' in page.url:
                    result.add('大屏设计器', 'pass', '设计器页面正常')
                    page.screenshot(path=f"{CONFIG['screenshot_dir']}/04-designer.png")
                else:
                    result.add('大屏设计器', 'fail', f'URL不正确: {page.url}')
            except Exception as e:
                result.add('大屏设计器', 'fail', str(e))

            # ==================== Test 5: 大屏展示页面 ====================
            print("\n▶️ Test 5: 大屏展示页面")
            try:
                page.goto(f"{CONFIG['base_url']}/dashboard/view/1")
                page.wait_for_load_state('networkidle')
                time.sleep(1)

                content = page.content()
                if len(content) > 100:
                    result.add('大屏展示页面', 'pass', '页面加载正常')
                    page.screenshot(path=f"{CONFIG['screenshot_dir']}/05-view.png")
                else:
                    result.add('大屏展示页面', 'fail', '页面内容为空')
            except Exception as e:
                result.add('大屏展示页面', 'fail', str(e))

            # ==================== Test 6: 公开访问页面 ====================
            print("\n▶️ Test 6: 公开访问页面")
            try:
                public_context = browser.new_context(locale='zh-CN')
                public_page = public_context.new_page()

                public_page.goto(f"{CONFIG['base_url']}/public/d/999999")
                public_page.wait_for_load_state('networkidle')
                time.sleep(0.5)

                content = public_page.content()
                if len(content) > 100:
                    result.add('公开访问页面', 'pass', '页面正常响应')
                    public_page.screenshot(path=f"{CONFIG['screenshot_dir']}/06-public.png")
                else:
                    result.add('公开访问页面', 'fail', '页面内容为空')

                public_context.close()
            except Exception as e:
                result.add('公开访问页面', 'fail', str(e))

            # ==================== Test 7: 首页认证检查 ====================
            print("\n▶️ Test 7: 首页认证检查")
            try:
                test_context = browser.new_context(locale='zh-CN')
                test_page = test_context.new_page()

                test_page.goto(f"{CONFIG['base_url']}/home")
                test_page.wait_for_load_state('networkidle')
                time.sleep(1)

                if '/login' in test_page.url:
                    result.add('首页认证检查', 'pass', '正确重定向到登录页')
                else:
                    result.add('首页认证检查', 'fail', f'未重定向: {test_page.url}')

                test_context.close()
            except Exception as e:
                result.add('首页认证检查', 'fail', str(e))

            # ==================== Test 8: API 未认证访问 ====================
            print("\n▶️ Test 8: API 未认证访问")
            try:
                response = page.request.get(f"{CONFIG['api_url']}/api/dashboards")

                if response.status in [401, 403]:
                    result.add('API未认证访问', 'pass', f'正确返回 {response.status}')
                else:
                    result.add('API未认证访问', 'fail', f'返回状态码: {response.status}')
            except Exception as e:
                result.add('API未认证访问', 'skip', f'API不可达: {e}')

        except Exception as e:
            print(f"\n❌ 测试执行异常: {e}")
            page.screenshot(path=f"{CONFIG['screenshot_dir']}/error.png")

        finally:
            browser.close()

    return result.print_summary()


if __name__ == '__main__':
    success = run_tests()
    sys.exit(0 if success else 1)
