# -*- coding: utf-8 -*-
"""
V1.1.0 Production Feature Test
"""

from playwright.sync_api import sync_playwright
import os
import sys

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

BASE_URL = "http://127.0.0.1:9999"
USERNAME = "root"
PASSWORD = "ADmin@123"
SCREENSHOT_DIR = "test-results/v1.1.0-production"

def test_v1_1_0_features():
    """Test V1.1.0 features"""

    os.makedirs(SCREENSHOT_DIR, exist_ok=True)

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(
            viewport={"width": 1920, "height": 1080},
            locale="zh-CN"
        )
        page = context.new_page()

        results = []

        try:
            # Test 1: Login
            print("\n=== Test 1: Login ===")
            page.goto(f"{BASE_URL}/login")
            page.wait_for_load_state("networkidle")
            page.screenshot(path=f"{SCREENSHOT_DIR}/01-login-page.png")
            print("[OK] Login page loaded")

            # Fill login form
            page.fill('input[placeholder="请输入用户名"]', USERNAME)
            page.fill('input[placeholder="请输入密码"]', PASSWORD)
            page.screenshot(path=f"{SCREENSHOT_DIR}/02-login-filled.png")

            # Click login button (text is '登 录' with space)
            page.click('button:has-text("登")')
            print("[OK] Clicked login button")

            # Wait for login - check for redirect to home or any authenticated page
            page.wait_for_timeout(3000)
            page.wait_for_load_state("networkidle")
            page.screenshot(path=f"{SCREENSHOT_DIR}/03-after-login.png")

            current_url = page.url
            if "/login" not in current_url:
                print(f"[OK] Login successful, redirected to: {current_url}")
                results.append(("Login", "PASS"))
            else:
                print(f"[FAIL] Still on login page: {current_url}")
                results.append(("Login", "FAIL"))
                # Check for error message
                error_msg = page.locator(".el-message--error")
                if error_msg.count() > 0:
                    print(f"[ERROR] Login error: {error_msg.first.inner_text()}")

            # Test 2: Sidebar Menu
            print("\n=== Test 2: Sidebar Menu ===")

            # Navigate to home first
            if "/home" not in page.url:
                page.goto(f"{BASE_URL}/home")
                page.wait_for_load_state("networkidle")

            page.screenshot(path=f"{SCREENSHOT_DIR}/04-home-page.png")

            # Check dashboard menu
            kanban_menu = page.locator('text=看板管理')
            if kanban_menu.count() > 0:
                print("[OK] Sidebar shows 'Dashboard' menu")
                results.append(("Dashboard Menu", "PASS"))
            else:
                print("[FAIL] Sidebar missing 'Dashboard' menu")
                results.append(("Dashboard Menu", "FAIL"))

            # Check display menu
            display_menu = page.locator('text=车间大屏')
            if display_menu.count() > 0:
                print("[OK] Sidebar shows 'Display' menu")
                results.append(("Display Menu", "PASS"))
            else:
                print("[FAIL] Sidebar missing 'Display' menu")
                results.append(("Display Menu", "FAIL"))

            page.screenshot(path=f"{SCREENSHOT_DIR}/05-sidebar-menu.png")

            # Test 3: Dashboard Page
            print("\n=== Test 3: Dashboard Page ===")
            page.goto(f"{BASE_URL}/dashboard")
            page.wait_for_load_state("networkidle")
            page.screenshot(path=f"{SCREENSHOT_DIR}/06-dashboard-page.png")

            if "/dashboard" in page.url:
                print(f"[OK] Dashboard page accessible: {page.url}")
                results.append(("Dashboard Page", "PASS"))
            else:
                print(f"[FAIL] Dashboard page failed: {page.url}")
                results.append(("Dashboard Page", "FAIL"))

            # Test 4: Display Page
            print("\n=== Test 4: Display Page ===")
            page.goto(f"{BASE_URL}/display")
            page.wait_for_load_state("networkidle")
            page.screenshot(path=f"{SCREENSHOT_DIR}/07-display-page.png")

            if "/display" in page.url:
                print(f"[OK] Display page accessible: {page.url}")
                results.append(("Display Page", "PASS"))
            else:
                print(f"[FAIL] Display page failed: {page.url}")
                results.append(("Display Page", "FAIL"))

            # Test 5: Fullscreen Page
            print("\n=== Test 5: Fullscreen Page ===")
            page.goto(f"{BASE_URL}/display/fullscreen")
            page.wait_for_load_state("networkidle")
            page.wait_for_timeout(2000)
            page.screenshot(path=f"{SCREENSHOT_DIR}/08-fullscreen-page.png")

            if "/display/fullscreen" in page.url:
                print(f"[OK] Fullscreen page accessible: {page.url}")
                results.append(("Fullscreen Page", "PASS"))
            else:
                print(f"[FAIL] Fullscreen page failed: {page.url}")
                results.append(("Fullscreen Page", "FAIL"))

        except Exception as e:
            print(f"\n[ERROR] Test failed: {e}")
            page.screenshot(path=f"{SCREENSHOT_DIR}/error-screenshot.png")
            results.append(("Test Execution", f"ERROR: {e}"))

        finally:
            browser.close()

        # Print results
        print("\n" + "=" * 50)
        print("V1.1.0 Production Test Results")
        print("=" * 50)

        passed = sum(1 for _, s in results if s == "PASS")
        failed = len(results) - passed

        for name, status in results:
            icon = "[OK]" if status == "PASS" else "[FAIL]"
            print(f"{icon} {name}: {status}")

        print("-" * 50)
        print(f"Total: {len(results)} | Passed: {passed} | Failed: {failed}")
        print("=" * 50)

        return failed == 0

if __name__ == "__main__":
    success = test_v1_1_0_features()
    exit(0 if success else 1)
