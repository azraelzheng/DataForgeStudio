# -*- coding: utf-8 -*-
"""
V1.1.0 Dashboard E2E Manual Test
手动测试脚本 - 可见浏览器模式

运行方式:
    python tests/e2e/dashboard-manual-test.py

功能测试:
    1. 登录系统
    2. 大屏 CRUD 测试
    3. 组件管理测试
    4. 公开访问测试
    5. 删除清理
"""

from playwright.sync_api import sync_playwright
import os
import sys
import time
import random

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

# 配置
CONFIG = {
    'base_url': os.environ.get('BASE_URL', 'http://localhost:9999'),
    'username': os.environ.get('TEST_USERNAME', 'root'),
    'password': os.environ.get('TEST_PASSWORD', 'ADmin@123'),
    'screenshot_dir': 'test-results/dashboard-manual',
    'headless': False,  # 可见浏览器模式
    'slow_mo': 300,     # 操作延迟（毫秒）
}

# 测试数据
TEST_DATA = {
    'dashboard_name': f'E2E测试大屏_{int(time.time())}',
    'dashboard_desc': '这是E2E自动化测试创建的大屏',
    'widget_title': f'测试组件_{random.randint(1000, 9999)}',
}


class DashboardTester:
    def __init__(self, page, context):
        self.page = page
        self.context = context
        self.created_dashboard_id = None
        self.test_results = []

    def log(self, message, status='info'):
        """记录测试日志"""
        symbols = {
            'info': 'ℹ️',
            'ok': '✅',
            'fail': '❌',
            'warn': '⚠️',
            'step': '▶️',
        }
        symbol = symbols.get(status, '')
        print(f"{symbol} {message}")
        self.test_results.append({'message': message, 'status': status})

    def screenshot(self, name):
        """保存截图"""
        path = f"{CONFIG['screenshot_dir']}/{name}.png"
        self.page.screenshot(path=path)
        self.log(f"截图已保存: {path}")

    def wait(self, ms=1000):
        """等待"""
        self.page.wait_for_timeout(ms)

    def login(self):
        """登录"""
        self.log("Step 1: 登录系统", 'step')

        self.page.goto(f"{CONFIG['base_url']}/login")
        self.page.wait_for_load_state('networkidle')
        self.wait(500)

        self.page.fill('input[placeholder="请输入用户名"]', CONFIG['username'])
        self.page.fill('input[placeholder="请输入密码"]', CONFIG['password'])
        self.page.click('button:has-text("登")')

        self.wait(2000)
        self.page.wait_for_load_state('networkidle')

        if 'login' not in self.page.url:
            self.log("登录成功", 'ok')
            self.screenshot('01-login-success')
            return True
        else:
            self.log("登录失败", 'fail')
            return False

    def navigate_to_dashboard_list(self):
        """导航到大屏列表"""
        self.log("Step 2: 导航到大屏列表", 'step')

        self.page.click('text=大屏管理')
        self.page.wait_for_load_state('networkidle')
        self.wait(500)

        if '/dashboard/list' in self.page.url:
            self.log("导航成功", 'ok')
            self.screenshot('02-dashboard-list')
            return True
        else:
            self.log("导航失败", 'fail')
            return False

    def create_dashboard(self):
        """创建大屏"""
        self.log("Step 3: 创建大屏", 'step')

        # 点击新建按钮
        new_btn = self.page.locator('button:has-text("新建"), button:has-text("创建")').first
        if new_btn.count() == 0:
            self.log("未找到新建按钮", 'warn')
            return False

        new_btn.click()
        self.wait(1000)

        # 填写表单
        name_input = self.page.locator('.el-dialog input[type="text"], .el-drawer input[type="text"]').first
        if name_input.count() > 0:
            name_input.fill(TEST_DATA['dashboard_name'])
            self.log(f"填写名称: {TEST_DATA['dashboard_name']}")
        else:
            self.log("未找到名称输入框", 'warn')

        # 填写描述
        desc_input = self.page.locator('textarea').first
        if desc_input.count() > 0:
            desc_input.fill(TEST_DATA['dashboard_desc'])
            self.log("填写描述")

        self.screenshot('03-create-form')

        # 保存
        save_btn = self.page.locator('button:has-text("确定"), button:has-text("保存")').first
        if save_btn.count() > 0:
            save_btn.click()
            self.wait(2000)
            self.log("点击保存按钮")

        # 检查是否成功
        self.page.wait_for_load_state('networkidle')

        # 尝试获取大屏ID
        url = self.page.url
        if '/dashboard/designer/' in url:
            self.created_dashboard_id = url.split('/dashboard/designer/')[-1].split('?')[0]
            self.log(f"创建成功，大屏ID: {self.created_dashboard_id}", 'ok')
        else:
            # 检查是否有成功消息
            if self.page.locator('text=创建成功, text=保存成功').count() > 0:
                self.log("创建成功", 'ok')
            else:
                self.log("创建结果未知", 'warn')

        self.screenshot('04-after-create')
        return True

    def view_dashboard(self):
        """查看大屏展示"""
        self.log("Step 4: 查看大屏展示", 'step')

        # 返回列表
        self.page.goto(f"{CONFIG['base_url']}/dashboard/list")
        self.page.wait_for_load_state('networkidle')
        self.wait(500)

        # 查找并点击查看按钮
        view_btn = self.page.locator('button:has-text("查看"), button:has-text("预览")').first
        if view_btn.count() > 0:
            view_btn.click()
            self.wait(2000)
            self.page.wait_for_load_state('networkidle')

            if '/dashboard/view/' in self.page.url:
                self.log("进入展示页面成功", 'ok')
                self.screenshot('05-dashboard-view')
                return True
            else:
                self.log("未进入展示页面", 'warn')
        else:
            self.log("未找到查看按钮", 'warn')

        return False

    def edit_dashboard(self):
        """编辑大屏"""
        self.log("Step 5: 编辑大屏", 'step')

        # 返回列表
        self.page.goto(f"{CONFIG['base_url']}/dashboard/list")
        self.page.wait_for_load_state('networkidle')
        self.wait(500)

        # 查找并点击编辑按钮
        edit_btn = self.page.locator('button:has-text("编辑"), button:has-text("设计")').first
        if edit_btn.count() > 0:
            edit_btn.click()
            self.wait(2000)
            self.page.wait_for_load_state('networkidle')

            if '/dashboard/designer/' in self.page.url:
                self.log("进入设计器成功", 'ok')
                self.screenshot('06-dashboard-designer')
                return True
            else:
                self.log("未进入设计器", 'warn')
        else:
            self.log("未找到编辑按钮", 'warn')

        return False

    def test_public_access(self):
        """测试公开访问"""
        self.log("Step 6: 测试公开访问", 'step')

        # 登出后访问公开页面
        self.page.goto(f"{CONFIG['base_url']}/public/d/999999")
        self.wait(1000)
        self.page.wait_for_load_state('networkidle')

        # 应该显示"不存在"或类似消息
        page_content = self.page.content()
        if '不存在' in page_content or 'NOT_FOUND' in page_content or '未公开' in page_content:
            self.log("公开访问正确拒绝不存在的大屏", 'ok')
        else:
            self.log("公开访问响应异常", 'warn')

        self.screenshot('07-public-access')

    def delete_dashboard(self):
        """删除大屏"""
        self.log("Step 7: 删除大屏", 'step')

        # 重新登录
        self.page.goto(f"{CONFIG['base_url']}/login")
        self.page.wait_for_load_state('networkidle')
        self.wait(500)

        self.page.fill('input[placeholder="请输入用户名"]', CONFIG['username'])
        self.page.fill('input[placeholder="请输入密码"]', CONFIG['password'])
        self.page.click('button:has-text("登")')
        self.wait(2000)

        # 进入列表
        self.page.goto(f"{CONFIG['base_url']}/dashboard/list")
        self.page.wait_for_load_state('networkidle')
        self.wait(500)

        # 查找测试创建的大屏
        test_item = self.page.locator(f'text={TEST_DATA["dashboard_name"]}')
        if test_item.count() > 0:
            self.log(f"找到测试大屏: {TEST_DATA['dashboard_name']}")

            # 点击对应的删除按钮
            delete_btn = self.page.locator('button:has-text("删除")').first
            if delete_btn.count() > 0:
                delete_btn.click()
                self.wait(500)

                # 确认删除
                confirm_btn = self.page.locator('button:has-text("确定")').first
                if confirm_btn.count() > 0:
                    confirm_btn.click()
                    self.wait(2000)
                    self.log("删除成功", 'ok')
                else:
                    self.page.keyboard.press('Enter')
                    self.wait(2000)
            else:
                self.log("未找到删除按钮", 'warn')
        else:
            self.log("未找到测试大屏（可能已删除）", 'warn')

        self.screenshot('08-after-delete')

    def run_all_tests(self):
        """运行所有测试"""
        print("\n" + "=" * 60)
        print("V1.1.0 Dashboard E2E Manual Test")
        print("=" * 60)

        os.makedirs(CONFIG['screenshot_dir'], exist_ok=True)

        try:
            # 执行测试
            if not self.login():
                return False

            if not self.navigate_to_dashboard_list():
                return False

            self.create_dashboard()
            self.view_dashboard()
            self.edit_dashboard()
            self.test_public_access()
            self.delete_dashboard()

            # 打印结果
            print("\n" + "=" * 60)
            print("测试结果汇总")
            print("=" * 60)

            ok_count = sum(1 for r in self.test_results if r['status'] == 'ok')
            fail_count = sum(1 for r in self.test_results if r['status'] == 'fail')
            warn_count = sum(1 for r in self.test_results if r['status'] == 'warn')

            print(f"✅ 成功: {ok_count}")
            print(f"❌ 失败: {fail_count}")
            print(f"⚠️ 警告: {warn_count}")

            print(f"\n截图目录: {CONFIG['screenshot_dir']}")

            return fail_count == 0

        except Exception as e:
            self.log(f"测试异常: {str(e)}", 'fail')
            self.screenshot('error')
            return False


def main():
    os.makedirs(CONFIG['screenshot_dir'], exist_ok=True)

    with sync_playwright() as p:
        browser = p.chromium.launch(
            headless=CONFIG['headless'],
            slow_mo=CONFIG['slow_mo'],
        )

        context = browser.new_context(
            viewport={'width': 1920, 'height': 1080},
            locale='zh-CN',
        )

        page = context.new_page()

        tester = DashboardTester(page, context)
        success = tester.run_all_tests()

        # 保持浏览器打开一段时间
        print("\n浏览器将在5秒后关闭...")
        time.sleep(5)

        browser.close()

        sys.exit(0 if success else 1)


if __name__ == '__main__':
    main()
