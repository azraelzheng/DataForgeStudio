from playwright.sync_api import sync_playwright
import time

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page()

    # Capture console logs
    page.on("console", lambda msg: print(f"[Console] {msg.type}: {msg.text}"))

    # Capture network responses
    def handle_response(response):
        if '/api/' in response.url:
            print(f"[API] {response.status} {response.url}")
            try:
                body = response.text()
                if len(body) < 500:
                    print(f"  Response: {body}")
                else:
                    print(f"  Response: {body[:500]}...")
            except:
                pass

    page.on("response", handle_response)

    # Navigate to the app
    print("Navigating to http://127.0.0.1:9999")
    page.goto('http://127.0.0.1:9999')
    page.wait_for_load_state('networkidle')

    # Take screenshot of initial state
    page.screenshot(path='H:/开发项目/DataForgeStudio_V4/test_screenshots/01_initial.png')
    print("Screenshot saved: 01_initial.png")

    # Check if we need to login
    if '/login' in page.url or page.locator('input[placeholder*="用户名"]').count() > 0:
        print("Need to login...")
        # Login
        page.fill('input[placeholder*="用户名"]', 'root')
        page.fill('input[placeholder*="密码"]', 'Root@2024')
        page.click('button:has-text("登")')
        page.wait_for_load_state('networkidle')
        time.sleep(2)
        page.screenshot(path='H:/开发项目/DataForgeStudio_V4/test_screenshots/02_after_login.png')
        print("Screenshot saved: 02_after_login.png")

    # Navigate to report query page
    print("Navigating to report query page...")
    page.goto('http://127.0.0.1:9999/#/report/query')
    page.wait_for_load_state('networkidle')
    time.sleep(2)
    page.screenshot(path='H:/开发项目/DataForgeStudio_V4/test_screenshots/03_report_query.png')
    print("Screenshot saved: 03_report_query.png")

    # Check if there are any reports in the list
    report_items = page.locator('.report-item').all()
    print(f"Found {len(report_items)} report items")

    if len(report_items) > 0:
        # Click on the first report
        print("Clicking on first report...")
        report_items[0].click()
        time.sleep(2)
        page.wait_for_load_state('networkidle')

        # Take screenshot after selecting report
        page.screenshot(path='H:/开发项目/DataForgeStudio_V4/test_screenshots/04_after_select.png')
        print("Screenshot saved: 04_after_select.png")

        # Check if the right panel has content
        query_area = page.locator('.query-area')
        empty_state = page.locator('.empty-state')

        print(f"Query area visible: {query_area.count() > 0}")
        print(f"Empty state visible: {empty_state.count() > 0}")

        # Get the content of the right panel
        right_panel = page.locator('.el-col-16 .el-card')
        if right_panel.count() > 0:
            print(f"Right panel content: {right_panel.inner_text()[:500]}")
    else:
        print("No reports found in the list")

    browser.close()
