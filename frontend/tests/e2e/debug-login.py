# -*- coding: utf-8 -*-
"""
Debug Login Test - 可见浏览器模式调试登录问题
"""

from playwright.sync_api import sync_playwright
import sys
import time

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

CONFIG = {
    'base_url': 'http://localhost:9999',
    'username': 'root',
    'password': 'ADmin@123',
}

def debug_login():
    with sync_playwright() as p:
        # 启动可见浏览器
        browser = p.chromium.launch(headless=False, slow_mo=500)
        context = browser.new_context(
            viewport={'width': 1920, 'height': 1080},
            locale='zh-CN',
        )
        page = context.new_page()

        # 启用控制台日志
        page.on('console', lambda msg: print(f'[Console] {msg.type}: {msg.text}'))
        page.on('pageerror', lambda err: print(f'[Page Error] {err}'))

        try:
            print("1. 导航到登录页面...")
            page.goto(f"{CONFIG['base_url']}/login")
            page.wait_for_load_state('networkidle')
            time.sleep(2)

            print("2. 填写登录表单...")
            username_input = page.locator('input[placeholder="请输入用户名"]')
            password_input = page.locator('input[placeholder="请输入密码"]')

            print(f"   用户名输入框存在: {username_input.count() > 0}")
            print(f"   密码输入框存在: {password_input.count() > 0}")

            if username_input.count() > 0:
                username_input.fill(CONFIG['username'])
                print(f"   已填写用户名: {CONFIG['username']}")

            if password_input.count() > 0:
                password_input.fill(CONFIG['password'])
                print(f"   已填写密码: {CONFIG['password']}")

            time.sleep(1)

            print("3. 查找登录按钮...")
            # 检查各种可能的按钮选择器
            selectors = [
                'button:has-text("登")',
                'button:has-text("登录")',
                'button:has-text("登 录")',
                '.el-button--primary',
                'button[type="submit"]',
                '.login-button',
            ]

            for selector in selectors:
                count = page.locator(selector).count()
                if count > 0:
                    text = page.locator(selector).first.text_content()
                    print(f"   找到按钮: {selector} -> '{text}'")

            print("4. 点击登录按钮...")
            login_btn = page.locator('button:has-text("登")').first
            if login_btn.count() > 0:
                login_btn.click()
                print("   已点击登录按钮")
            else:
                print("   未找到登录按钮!")
                return

            print("5. 等待登录响应...")
            time.sleep(5)
            page.wait_for_load_state('networkidle')

            print(f"6. 当前 URL: {page.url}")

            # 检查是否有错误消息
            error_msg = page.locator('.el-message--error')
            if error_msg.count() > 0:
                print(f"   错误消息: {error_msg.text_content()}")

            success_msg = page.locator('.el-message--success')
            if success_msg.count() > 0:
                print(f"   成功消息: {success_msg.text_content()}")

            # 检查 localStorage
            token = page.evaluate('() => localStorage.getItem("token")')
            print(f"   Token: {token[:50] if token else 'None'}...")

            # 截图
            page.screenshot(path='test-results/debug-login.png')
            print("7. 截图已保存: test-results/debug-login.png")

            # 保持浏览器打开
            print("\n浏览器将保持打开30秒，请查看页面状态...")
            time.sleep(30)

        except Exception as e:
            print(f"错误: {e}")
            page.screenshot(path='test-results/debug-login-error.png')
            time.sleep(10)

        finally:
            browser.close()

if __name__ == '__main__':
    debug_login()
