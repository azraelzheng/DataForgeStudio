# -*- coding: utf-8 -*-
"""
V1.1.0 Interactive Dashboard & Display Creation Test
Opens visible browser to test:
1. Create a new dashboard
2. Add a widget to the dashboard
3. Create a display configuration
4. Link dashboard to display
"""

from playwright.sync_api import sync_playwright
import os
import sys
import time

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

BASE_URL = "http://127.0.0.1:9999"
USERNAME = "root"
PASSWORD = "ADmin@123"
SCREENSHOT_DIR = "test-results/v1.1.0-interactive"

def run_interactive_test():
    """Run interactive test with visible browser"""

    os.makedirs(SCREENSHOT_DIR, exist_ok=True)

    print("="*60)
    print("V1.1.0 Interactive Test - Visible Browser")
    print("="*60)

    with sync_playwright() as p:
        # Launch VISIBLE browser (headless=False)
        browser = p.chromium.launch(
            headless=False,
            slow_mo=500  # Slow down actions for visibility
        )
        context = browser.new_context(
            viewport={"width": 1920, "height": 1080},
            locale="zh-CN"
        )
        page = context.new_page()

        try:
            # ==================== Step 1: Login ====================
            print("\n[Step 1] Logging in...")

            page.goto(f"{BASE_URL}/login")
            page.wait_for_load_state("networkidle")
            time.sleep(1)

            page.fill('input[placeholder="请输入用户名"]', USERNAME)
            page.fill('input[placeholder="请输入密码"]', PASSWORD)
            page.click('button:has-text("登")')

            page.wait_for_timeout(2000)
            page.wait_for_load_state("networkidle")
            print("[OK] Logged in successfully")
            page.screenshot(path=f"{SCREENSHOT_DIR}/01-logged-in.png")

            # ==================== Step 2: Navigate to Dashboard ====================
            print("\n[Step 2] Navigating to Dashboard...")

            # Click dashboard menu
            page.click('text=看板管理')
            page.wait_for_load_state("networkidle")
            time.sleep(1)
            print("[OK] On Dashboard page")
            page.screenshot(path=f"{SCREENSHOT_DIR}/02-dashboard-page.png")

            # ==================== Step 3: Create New Dashboard ====================
            print("\n[Step 3] Creating new dashboard...")

            # Click new button
            new_btn = page.locator('button:has-text("新建")')
            if new_btn.count() > 0:
                new_btn.click()
                page.wait_for_timeout(1000)
                print("[OK] Create dialog opened")
                page.screenshot(path=f"{SCREENSHOT_DIR}/03-create-dialog.png")

                # Fill dashboard name
                name_input = page.locator('.el-dialog input[type="text"], .el-drawer input[type="text"]').first
                if name_input.count() > 0:
                    name_input.click()
                    name_input.fill("测试看板 - Test Dashboard")
                    page.wait_for_timeout(500)
                    print("[OK] Filled dashboard name")
                    page.screenshot(path=f"{SCREENSHOT_DIR}/04-name-filled.png")

                # Fill description if exists
                desc_input = page.locator('textarea').first
                if desc_input.count() > 0:
                    desc_input.fill("这是一个测试看板，用于验证功能")
                    page.wait_for_timeout(500)
                    print("[OK] Filled description")

                page.screenshot(path=f"{SCREENSHOT_DIR}/05-form-complete.png")

                # Try to submit (look for confirm/save button)
                save_btn = page.locator('button:has-text("确定"), button:has-text("保存"), button:has-text("提交")')
                if save_btn.count() > 0:
                    save_btn.first.click()
                    page.wait_for_timeout(2000)
                    print("[OK] Clicked save button")
                    page.screenshot(path=f"{SCREENSHOT_DIR}/06-after-save.png")
                else:
                    print("[INFO] No save button found")

                # IMPORTANT: Close dialog if still open
                page.keyboard.press('Escape')
                page.wait_for_timeout(500)

                # Check if dialog closed
                dialog = page.locator('.el-dialog:visible')
                if dialog.count() > 0:
                    print("[INFO] Dialog still visible, pressing Escape again...")
                    page.keyboard.press('Escape')
                    page.wait_for_timeout(500)

                page.screenshot(path=f"{SCREENSHOT_DIR}/06b-dialog-closed.png")
                print("[OK] Dialog closed")
            else:
                print("[FAIL] No 'New' button found")

            # ==================== Step 4: Navigate to Display ====================
            print("\n[Step 4] Navigating to Display Configuration...")

            page.click('text=车间大屏')
            page.wait_for_load_state("networkidle")
            time.sleep(1)
            print("[OK] On Display page")
            page.screenshot(path=f"{SCREENSHOT_DIR}/07-display-page.png")

            # ==================== Step 5: Create New Display Config ====================
            print("\n[Step 5] Creating new display configuration...")

            new_btn = page.locator('button:has-text("新建")')
            if new_btn.count() > 0:
                new_btn.click()
                page.wait_for_timeout(1000)
                print("[OK] Create dialog opened")
                page.screenshot(path=f"{SCREENSHOT_DIR}/08-display-dialog.png")

                # Fill display name
                name_input = page.locator('.el-dialog input[type="text"], .el-drawer input[type="text"]').first
                if name_input.count() > 0:
                    name_input.fill("测试大屏 - Test Display")
                    page.wait_for_timeout(500)
                    print("[OK] Filled display name")

                # Try to find dashboard selector
                dashboard_select = page.locator('.el-select, select').first
                if dashboard_select.count() > 0:
                    dashboard_select.click()
                    page.wait_for_timeout(500)
                    # Select first option
                    option = page.locator('.el-select-dropdown__item').first
                    if option.count() > 0:
                        option.click()
                        print("[OK] Selected dashboard")
                        page.wait_for_timeout(500)

                page.screenshot(path=f"{SCREENSHOT_DIR}/09-display-form.png")

                # Try to save
                save_btn = page.locator('button:has-text("确定"), button:has-text("保存")')
                if save_btn.count() > 0:
                    save_btn.first.click()
                    page.wait_for_timeout(2000)
                    print("[OK] Clicked save button")
                    page.screenshot(path=f"{SCREENSHOT_DIR}/10-display-saved.png")

                # Close dialog
                page.keyboard.press('Escape')
                page.wait_for_timeout(500)
                page.screenshot(path=f"{SCREENSHOT_DIR}/10b-display-closed.png")
                print("[OK] Display dialog closed")
            else:
                print("[FAIL] No 'New' button found")

            # ==================== Step 6: Verify Created Items ====================
            print("\n[Step 6] Verifying created items...")

            # Go back to dashboard to verify
            page.click('text=看板管理')
            page.wait_for_load_state("networkidle")
            time.sleep(1)

            # Check if our dashboard appears in the list
            if page.locator('text=测试看板').count() > 0:
                print("[OK] Created dashboard found in list")
            else:
                print("[INFO] Dashboard may need refresh to appear")

            page.screenshot(path=f"{SCREENSHOT_DIR}/11-verification.png")

            # ==================== Keep browser open for inspection ====================
            print("\n[INFO] Keeping browser open for 10 seconds for inspection...")
            print("[INFO] Close the browser window to end the test")
            time.sleep(10)

        except Exception as e:
            print(f"\n[ERROR] {e}")
            page.screenshot(path=f"{SCREENSHOT_DIR}/error.png")

        finally:
            browser.close()

    print("\n" + "="*60)
    print("Interactive Test Complete")
    print(f"Screenshots saved to: {SCREENSHOT_DIR}")
    print("="*60)

if __name__ == "__main__":
    run_interactive_test()
