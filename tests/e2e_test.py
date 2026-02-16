# -*- coding: utf-8 -*-
"""
DataForgeStudio V4 E2E Tests
Tests the full application stack with Playwright
"""
import sys
import os
import time

# Set encoding for stdout
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# Default frontend port - can be overridden
FRONTEND_PORT = int(os.environ.get('FRONTEND_PORT', 8080))
BACKEND_PORT = int(os.environ.get('BACKEND_PORT', 5000))

def run_tests():
    from playwright.sync_api import sync_playwright

    test_results = {"passed": 0, "failed": 0, "tests": []}

    def log_test(name, passed, message=""):
        status = "[PASS]" if passed else "[FAIL]"
        print(f"{status}: {name}")
        if message:
            print(f"    {message}")
        test_results["tests"].append({"name": name, "passed": passed, "message": message})
        if passed:
            test_results["passed"] += 1
        else:
            test_results["failed"] += 1

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(base_url=f'http://localhost:{FRONTEND_PORT}')
        page = context.new_page()

        os.makedirs('test_screenshots', exist_ok=True)

        # ============================================
        # Test 0: Reconnaissance - See what's served
        # ============================================
        print("\n=== Test 0: Reconnaissance ===")
        try:
            page.goto('/')
            page.wait_for_load_state('networkidle')
            page.screenshot(path='test_screenshots/00_recon_root.png')

            content = page.content()
            print(f"Root page content length: {len(content)}")
            print(f"Root page title: {page.title()}")

            # Check if it's the Vue app or 404
            if 'DataForgeStudio' in content or 'Vue' in content:
                print("Found Vue app content")
            else:
                print("No Vue app content found")
                print(f"First 500 chars: {content[:500]}")

        except Exception as e:
            print(f"Reconnaissance error: {e}")

        # ============================================
        # Test 1: Health Check API
        # ============================================
        try:
            print("\n=== Test 1: Health Check API ===")
            response = page.request.get(f'http://localhost:{BACKEND_PORT}/health')
            health_data = response.json()

            if health_data.get('status') == 'Healthy':
                log_test("Backend health check", True, f"Status: {health_data.get('status')}")
            else:
                log_test("Backend health check", False, f"Unexpected status: {health_data}")

        except Exception as e:
            log_test("Backend health check", False, str(e))

        # ============================================
        # Test 2: API Info Endpoint
        # ============================================
        try:
            print("\n=== Test 2: API Info Endpoint ===")
            response = page.request.get(f'http://localhost:{BACKEND_PORT}/api')
            api_data = response.json()

            if 'DataForgeStudio' in api_data.get('name', ''):
                log_test("API info endpoint", True, f"Name: {api_data.get('name')}")
            else:
                log_test("API info endpoint", False, f"Unexpected response: {api_data}")

        except Exception as e:
            log_test("API info endpoint", False, str(e))

        # ============================================
        # Test 3: Login Page Display
        # ============================================
        try:
            print("\n=== Test 3: Login Page Display ===")
            page.goto('/login')
            page.wait_for_load_state('networkidle')
            page.screenshot(path='test_screenshots/03_login_page.png')

            title = page.title()
            print(f"Page title: {title}")

            # Check page content
            content = page.content()
            print(f"Login page content length: {len(content)}")

            # Try multiple selectors for login form
            all_inputs = page.locator('input').all()
            print(f"Total inputs on page: {len(all_inputs)}")

            username_input = page.locator('input[type="text"]').first
            password_input = page.locator('input[type="password"]').first
            login_button = page.locator('button').first

            username_count = 1 if username_input.count() > 0 else 0
            password_count = 1 if password_input.count() > 0 else 0
            button_count = 1 if login_button.count() > 0 else 0

            print(f"Username inputs: {username_count}, Password inputs: {password_count}, Buttons: {button_count}")

            if username_count > 0 and password_count > 0 and button_count > 0:
                log_test("Login page displays correctly", True, f"Found form elements")
            else:
                # Print first 1000 chars for debugging
                print(f"Page content (first 1000 chars): {content[:1000]}")
                log_test("Login page displays correctly", False, f"Missing elements")

        except Exception as e:
            log_test("Login page displays correctly", False, str(e))

        # ============================================
        # Test 4: Login with invalid credentials
        # ============================================
        try:
            print("\n=== Test 4: Login with invalid credentials ===")
            page.goto('/login')
            page.wait_for_load_state('networkidle')

            username_input = page.locator('input[type="text"]').first
            password_input = page.locator('input[type="password"]').first
            login_button = page.locator('button').first

            if username_input.count() > 0:
                username_input.fill('wronguser')
                password_input.fill('wrongpassword')
                login_button.click()

                page.wait_for_timeout(2000)

                current_url = page.url
                if '/login' in current_url:
                    log_test("Invalid login rejected", True, f"Still on login page")
                else:
                    log_test("Invalid login rejected", False, f"Redirected to: {current_url}")
            else:
                log_test("Invalid login rejected", False, "Could not find login form elements")

        except Exception as e:
            log_test("Invalid login rejected", False, str(e))

        # ============================================
        # Test 5: Check frontend loads
        # ============================================
        try:
            print("\n=== Test 5: Frontend loads correctly ===")
            page.goto('/')
            page.wait_for_load_state('networkidle')
            page.screenshot(path='test_screenshots/05_frontend_root.png')

            # Check if Vue app exists
            app_div = page.locator('#app')

            if app_div.count() > 0:
                content = app_div.inner_html(timeout=5000)
                if len(content) > 100:
                    log_test("Frontend app container loaded", True, f"App div has {len(content)} chars")
                else:
                    log_test("Frontend app container loaded", False, f"App div only has {len(content)} chars")
            else:
                log_test("Frontend app container loaded", False, "No #app element found")

        except Exception as e:
            log_test("Frontend app container loaded", False, str(e))

        # ============================================
        # Test 6: Check login form input functionality
        # ============================================
        try:
            print("\n=== Test 6: Login form input functionality ===")
            page.goto('/login')
            page.wait_for_load_state('networkidle')

            username_input = page.locator('input[type="text"]').first

            if username_input.count() > 0:
                test_value = "testuser123"
                username_input.fill(test_value)
                actual_value = username_input.input_value()

                if actual_value == test_value:
                    log_test("Login form input works", True, f"Input value: {actual_value}")
                else:
                    log_test("Login form input works", False, f"Expected: {test_value}, Got: {actual_value}")
            else:
                log_test("Login form input works", False, "Could not find username input")

        except Exception as e:
            log_test("Login form input works", False, str(e))

        browser.close()

    # Print summary
    print("\n" + "="*50)
    print("TEST SUMMARY")
    print("="*50)
    print(f"Passed: {test_results['passed']}")
    print(f"Failed: {test_results['failed']}")
    print(f"Total:  {test_results['passed'] + test_results['failed']}")
    print("="*50)

    return 0 if test_results['failed'] == 0 else 1

if __name__ == "__main__":
    exit_code = run_tests()
    sys.exit(exit_code)
