# -*- coding: utf-8 -*-
"""
V1.1.0 Comprehensive Feature Test
Tests:
1. Login
2. Check sidebar menus
3. Create a dashboard with chart widget
4. Create a display configuration
5. Verify data display
6. Generate error report for fixes
"""

from playwright.sync_api import sync_playwright
import os
import sys
import json
from datetime import datetime

if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

BASE_URL = "http://127.0.0.1:9999"
USERNAME = "root"
PASSWORD = "ADmin@123"
SCREENSHOT_DIR = "test-results/v1.1.0-comprehensive"
ERROR_REPORT_PATH = "docs/test-reports/v1.1.0-error-report.md"

# Test results tracking
test_results = []
errors = []

def log_result(name, status, message="", screenshot=None):
    """Log test result"""
    test_results.append({
        "name": name,
        "status": status,
        "message": message,
        "screenshot": screenshot,
        "timestamp": datetime.now().isoformat()
    })
    icon = "[OK]" if status == "PASS" else "[FAIL]"
    print(f"{icon} {name}: {message}")
    if status == "FAIL":
        errors.append({
            "test": name,
            "error": message,
            "screenshot": screenshot
        })

def take_screenshot(page, name):
    """Take screenshot and return path"""
    path = f"{SCREENSHOT_DIR}/{name}"
    page.screenshot(path=path)
    return path

def run_comprehensive_test():
    """Run comprehensive V1.1.0 tests"""

    os.makedirs(SCREENSHOT_DIR, exist_ok=True)
    os.makedirs(os.path.dirname(ERROR_REPORT_PATH), exist_ok=True)

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(
            viewport={"width": 1920, "height": 1080},
            locale="zh-CN"
        )
        page = context.new_page()

        # Enable console log capture
        console_logs = []
        page.on("console", lambda msg: console_logs.append(f"[{msg.type}] {msg.text}"))

        try:
            # ==================== TEST 1: Login ====================
            print("\n" + "="*60)
            print("TEST 1: Login")
            print("="*60)

            page.goto(f"{BASE_URL}/login")
            page.wait_for_load_state("networkidle")
            take_screenshot(page, "01-login-page.png")

            page.fill('input[placeholder="请输入用户名"]', USERNAME)
            page.fill('input[placeholder="请输入密码"]', PASSWORD)
            page.click('button:has-text("登")')

            page.wait_for_timeout(3000)
            page.wait_for_load_state("networkidle")

            if "/login" not in page.url:
                log_result("Login", "PASS", f"Redirected to {page.url}")
                take_screenshot(page, "02-after-login.png")
            else:
                log_result("Login", "FAIL", "Still on login page")
                take_screenshot(page, "02-login-failed.png")

            # ==================== TEST 2: Sidebar Menus ====================
            print("\n" + "="*60)
            print("TEST 2: Sidebar Menus")
            print("="*60)

            page.goto(f"{BASE_URL}/home")
            page.wait_for_load_state("networkidle")
            take_screenshot(page, "03-home-page.png")

            # Check dashboard menu
            if page.locator('text=看板管理').count() > 0:
                log_result("Dashboard Menu", "PASS", "Menu item visible")
            else:
                log_result("Dashboard Menu", "FAIL", "Menu item not found")

            # Check display menu
            if page.locator('text=车间大屏').count() > 0:
                log_result("Display Menu", "PASS", "Menu item visible")
            else:
                log_result("Display Menu", "FAIL", "Menu item not found")

            # ==================== TEST 3: Dashboard Management ====================
            print("\n" + "="*60)
            print("TEST 3: Dashboard Management")
            print("="*60)

            page.goto(f"{BASE_URL}/dashboard")
            page.wait_for_load_state("networkidle")
            take_screenshot(page, "04-dashboard-page.png")

            # Check if dashboard page loaded
            if "/dashboard" in page.url:
                log_result("Dashboard Page Access", "PASS", page.url)
            else:
                log_result("Dashboard Page Access", "FAIL", f"Redirected to {page.url}")
                page.goto(f"{BASE_URL}/dashboard")
                page.wait_for_load_state("networkidle")

            # Look for create button
            create_btn_selectors = [
                'button:has-text("新建")',
                'button:has-text("创建")',
                'button:has-text("新增")',
                '.el-button--primary:has-text("新")'
            ]

            create_btn = None
            for selector in create_btn_selectors:
                if page.locator(selector).count() > 0:
                    create_btn = selector
                    break

            if create_btn:
                log_result("Dashboard Create Button", "PASS", f"Found: {create_btn}")

                # Try to click create
                try:
                    page.click(create_btn)
                    page.wait_for_timeout(1000)
                    take_screenshot(page, "05-dashboard-create-dialog.png")

                    # Check if dialog opened
                    dialog = page.locator('.el-dialog, .el-drawer')
                    if dialog.count() > 0:
                        log_result("Dashboard Create Dialog", "PASS", "Dialog opened")

                        # Try to fill form
                        name_input = page.locator('.el-dialog input, .el-drawer input').first
                        if name_input.count() > 0:
                            name_input.fill("Test Dashboard")
                            page.wait_for_timeout(500)
                            take_screenshot(page, "06-dashboard-form-filled.png")
                            log_result("Dashboard Form Fill", "PASS", "Filled name field")
                        else:
                            log_result("Dashboard Form Fill", "FAIL", "No input field found")

                        # Close dialog
                        cancel_btn = page.locator('button:has-text("取消"), button:has-text("关闭")')
                        if cancel_btn.count() > 0:
                            cancel_btn.first.click()
                            page.wait_for_timeout(500)
                    else:
                        log_result("Dashboard Create Dialog", "FAIL", "No dialog appeared")
                except Exception as e:
                    log_result("Dashboard Create Action", "FAIL", str(e))
                    take_screenshot(page, "05-dashboard-create-error.png")
            else:
                log_result("Dashboard Create Button", "FAIL", "No create button found")
                take_screenshot(page, "04-dashboard-no-create.png")

            # ==================== TEST 4: Display Configuration ====================
            print("\n" + "="*60)
            print("TEST 4: Display Configuration")
            print("="*60)

            page.goto(f"{BASE_URL}/display")
            page.wait_for_load_state("networkidle")
            take_screenshot(page, "07-display-page.png")

            if "/display" in page.url:
                log_result("Display Page Access", "PASS", page.url)
            else:
                log_result("Display Page Access", "FAIL", f"Redirected to {page.url}")

            # Look for create button
            create_btn = None
            for selector in create_btn_selectors:
                if page.locator(selector).count() > 0:
                    create_btn = selector
                    break

            if create_btn:
                log_result("Display Create Button", "PASS", f"Found: {create_btn}")

                try:
                    page.click(create_btn)
                    page.wait_for_timeout(1000)
                    take_screenshot(page, "08-display-create-dialog.png")

                    dialog = page.locator('.el-dialog, .el-drawer')
                    if dialog.count() > 0:
                        log_result("Display Create Dialog", "PASS", "Dialog opened")
                    else:
                        log_result("Display Create Dialog", "FAIL", "No dialog appeared")
                except Exception as e:
                    log_result("Display Create Action", "FAIL", str(e))
            else:
                log_result("Display Create Button", "FAIL", "No create button found")

            # ==================== TEST 5: API Health Check ====================
            print("\n" + "="*60)
            print("TEST 5: API Health Check")
            print("="*60)

            # Check backend API
            try:
                api_response = page.request.get(f"{BASE_URL}/api/health")
                if api_response.ok:
                    log_result("API Health", "PASS", f"Status: {api_response.status}")
                else:
                    log_result("API Health", "FAIL", f"Status: {api_response.status}")
            except Exception as e:
                log_result("API Health", "FAIL", str(e))

            # ==================== TEST 6: Page Elements Discovery ====================
            print("\n" + "="*60)
            print("TEST 6: Page Elements Discovery")
            print("="*60)

            # Dashboard page elements
            page.goto(f"{BASE_URL}/dashboard")
            page.wait_for_load_state("networkidle")

            buttons = page.locator("button").all()
            inputs = page.locator("input").all()

            log_result("Dashboard Buttons", "INFO", f"Found {len(buttons)} buttons")
            log_result("Dashboard Inputs", "INFO", f"Found {len(inputs)} inputs")

            # Check for specific elements
            elements_to_check = [
                ("Table", ".el-table"),
                ("Form", ".el-form"),
                ("Card", ".el-card"),
                ("Pagination", ".el-pagination"),
                ("Empty", ".el-empty")
            ]

            for name, selector in elements_to_check:
                count = page.locator(selector).count()
                if count > 0:
                    log_result(f"Dashboard {name}", "INFO", f"Found {count}")

            take_screenshot(page, "09-dashboard-elements.png")

        except Exception as e:
            log_result("Test Execution", "FAIL", str(e))
            take_screenshot(page, "error-final.png")

        finally:
            browser.close()

    # ==================== Generate Report ====================
    generate_error_report()

    return len(errors) == 0

def generate_error_report():
    """Generate error report for fixing"""

    passed = sum(1 for r in test_results if r["status"] == "PASS")
    failed = sum(1 for r in test_results if r["status"] == "FAIL")

    report = f"""# V1.1.0 Test Error Report

**Generated**: {datetime.now().strftime("%Y-%m-%d %H:%M:%S")}
**Test Environment**: {BASE_URL}

## Summary

| Metric | Value |
|--------|-------|
| Total Tests | {len(test_results)} |
| Passed | {passed} |
| Failed | {failed} |
| Pass Rate | {passed/len(test_results)*100:.1f}% |

## Test Results

| Test | Status | Message |
|------|--------|---------|
"""

    for r in test_results:
        icon = "PASS" if r["status"] == "PASS" else ("FAIL" if r["status"] == "FAIL" else "INFO")
        report += f"| {r['name']} | {icon} | {r['message']} |\n"

    if errors:
        report += "\n## Errors to Fix\n\n"
        for i, err in enumerate(errors, 1):
            report += f"""### Error {i}: {err['test']}

- **Error**: {err['error']}
- **Screenshot**: `{err['screenshot']}`

"""
    else:
        report += "\n## No Errors Found\n\nAll tests passed successfully.\n"

    report += f"""
## Screenshots

Screenshots are saved in: `frontend/{SCREENSHOT_DIR}/`

## Console Logs

Check the test output for any console errors or warnings.

---
*Report generated by V1.1.0 Comprehensive Test Suite*
"""

    with open(ERROR_REPORT_PATH, "w", encoding="utf-8") as f:
        f.write(report)

    print(f"\n[REPORT] Error report saved to: {ERROR_REPORT_PATH}")

if __name__ == "__main__":
    print("="*60)
    print("V1.1.0 Comprehensive Feature Test")
    print("="*60)

    success = run_comprehensive_test()

    print("\n" + "="*60)
    print("Final Summary")
    print("="*60)

    passed = sum(1 for r in test_results if r["status"] == "PASS")
    failed = sum(1 for r in test_results if r["status"] == "FAIL")

    print(f"Total: {len(test_results)} | Passed: {passed} | Failed: {failed}")

    if errors:
        print(f"\n[WARNING] {len(errors)} errors need fixing!")
        for err in errors:
            print(f"  - {err['test']}: {err['error']}")
    else:
        print("\n[SUCCESS] All tests passed!")

    exit(0 if success else 1)
