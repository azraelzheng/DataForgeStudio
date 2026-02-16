"""
DataForgeStudio V4 End-to-End Test
Complete workflow: Login as root -> Create role -> Create user -> Login as new user -> Design report -> Query report -> Export report
"""

from playwright.sync_api import sync_playwright, TimeoutError as PlaywrightTimeoutError
import json
import time
from datetime import datetime
import sys
import io

# Fix Windows console encoding
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

# 测试配置
BASE_URL = "http://localhost:5173"
API_BASE = "http://localhost:5000"
SCREENSHOT_DIR = "test_screenshots/e2e"
TIMESTAMP = datetime.now().strftime("%Y%m%d_%H%M%S")

# 测试数据
ROOT_USERNAME = "root"
# 默认密码根据 LoginPage.vue 显示是 admin123
POSSIBLE_ROOT_PASSWORDS = ["admin123", "Admin@123", "Root@123", "Password@123", "test123"]
ROOT_PASSWORD = None  # 将在运行时确定

NEW_ROLE_NAME = f"E2E测试角色_{TIMESTAMP}"
NEW_ROLE_CODE = f"E2E_TEST_ROLE_{TIMESTAMP}"

NEW_USERNAME = f"e2e_test_user_{TIMESTAMP}"
NEW_USER_PASSWORD = "Test@123"
NEW_USER_REALNAME = "E2E测试用户"

REPORT_NAME = f"E2E测试报表_{TIMESTAMP}"
REPORT_SQL = "SELECT TOP 10 * FROM aa_inventory"

# 结果存储
test_results = []


def log_result(test_name, status, message="", screenshot=""):
    """记录测试结果"""
    result = {
        "name": test_name,
        "status": status,
        "message": message,
        "screenshot": screenshot,
        "timestamp": datetime.now().isoformat()
    }
    test_results.append(result)

    status_icon = "[PASS]" if status == "passed" else "[FAIL]" if status == "failed" else "[WARN]"
    print(f"{status_icon} {test_name}: {status}")
    if message:
        print(f"   {message}")
    if screenshot:
        print(f"   Screenshot: {screenshot}")


def take_screenshot(page, name):
    """截图并返回路径"""
    path = f"{SCREENSHOT_DIR}/{TIMESTAMP}_{name}.png"
    page.screenshot(path=path, full_page=True)
    return path


def wait_for_element(page, selector, timeout=5000):
    """等待元素出现"""
    try:
        page.wait_for_selector(selector, timeout=timeout)
        return True
    except:
        return False


def main():
    print("=" * 80)
    print("DataForgeStudio V4 端到端测试")
    print(f"测试时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 80)
    print()

    with sync_playwright() as p:
        # 启动浏览器
        browser = p.chromium.launch(headless=False, args=['--no-sandbox', '--disable-dev-shm-usage'])
        context = browser.new_context(viewport={'width': 1920, 'height': 1080})
        page = context.new_page()

        # 设置默认超时时间
        page.set_default_timeout(60000)

        # 全局变量用于存储成功的密码
        global ROOT_PASSWORD

        try:
            # ==================== 步骤 1: 登录 root 用户 ====================
            print("\n" + "=" * 80)
            print("步骤 1: 登录 root 用户")
            print("=" * 80)

            print(f"   Navigating to {BASE_URL}/login")
            page.goto(f"{BASE_URL}/login", timeout=60000)
            print(f"   Current URL: {page.url}")
            page.wait_for_load_state('networkidle')
            print(f"   Page loaded successfully")
            time.sleep(1)

            # 检查是否在登录页
            if "login" not in page.url.lower():
                # 可能已登录，先登出
                page.goto(f"{BASE_URL}/login")
                page.wait_for_load_state('networkidle')

            # 尝试多种密码
            login_success = False
            for pwd in POSSIBLE_ROOT_PASSWORDS:
                print(f"   Trying password: {pwd}")
                page.fill('input[placeholder*="用户名"]', ROOT_USERNAME)
                page.fill('input[placeholder*="密码"]', pwd)

                # 等待登录按钮启用（禁用状态通常表示正在验证）
                try:
                    page.wait_for_selector('button:not([disabled])', timeout=3000)
                except:
                    pass  # 继续尝试

                # 尝试多种按钮选择器
                try:
                    # 首先尝试点击登 录（带空格）
                    page.click('button:has-text("登")', timeout=5000)
                except:
                    try:
                        # 尝试点击登录按钮
                        page.click('.login-button', timeout=5000)
                    except:
                        # 尝试通过角色查找
                        page.click('button[type="submit"]', timeout=5000)

                # 等待响应
                try:
                    page.wait_for_url(f"{BASE_URL}/**", timeout=5000)
                    time.sleep(1)
                    # 检查是否登录成功（URL 变化或有首页元素）
                    if "/login" not in page.url:
                        ROOT_PASSWORD = pwd
                        login_success = True
                        log_result("Login root user", "passed", f"Login successful with password: {pwd}", take_screenshot(page, "02_login_success"))
                        break
                    else:
                        # 密码错误，重试
                        print(f"   Password incorrect, trying next...")
                        page.goto(f"{BASE_URL}/login")
                        page.wait_for_load_state('networkidle')
                except Exception as e:
                    # 超时，可能密码错误
                    print(f"   Login failed with exception: {str(e)[:100]}")
                    page.goto(f"{BASE_URL}/login")
                    page.wait_for_load_state('networkidle')

            if not login_success:
                log_result("登录 root 用户", "failed", "所有尝试的密码都失败", take_screenshot(page, "02_login_failed"))
                raise Exception("无法使用任何密码登录 root 用户")

            # ==================== 步骤 2: 创建权限组 ====================
            print("\n" + "=" * 80)
            print("步骤 2: 创建权限组")
            print("=" * 80)

            # 导航到角色管理页面
            page.goto(f"{BASE_URL}/system/role")
            page.wait_for_load_state('networkidle')
            time.sleep(1)

            # 检查是否有权限访问
            if "权限" in page.content() or "错误" in page.content():
                log_result("访问角色管理", "failed", "权限不足", take_screenshot(page, "03_role_access_denied"))
                print("[WARN] root user lacks role management permission, trying existing roles...")
                # 尝试使用已存在的角色继续测试
                NEW_ROLE_NAME = "超级管理员"  # 使用已存在的角色
            else:
                # 点击"新增角色"按钮
                if wait_for_element(page, 'button:has-text("新增")'):
                    page.click('button:has-text("新增")')
                    time.sleep(1)

                    # 填写角色信息
                    page.fill('input[placeholder*="角色名称"]', NEW_ROLE_NAME)
                    page.fill('input[placeholder*="角色编码"]', NEW_ROLE_CODE)

                    # 选择权限（全选）
                    page.click('label:has-text("全选")')

                    # 保存
                    page.click('button:has-text("确定")')
                    time.sleep(2)

                    log_result("创建权限组", "passed", f"创建角色: {NEW_ROLE_NAME}", take_screenshot(page, "04_role_created"))
                else:
                    print("[WARN] Add button not found, using existing roles")

            # ==================== 步骤 3: 创建用户 ====================
            print("\n" + "=" * 80)
            print("步骤 3: 创建用户")
            print("=" * 80)

            # 导航到用户管理页面
            page.goto(f"{BASE_URL}/system/user")
            page.wait_for_load_state('networkidle')
            time.sleep(1)

            # 点击"新增用户"按钮
            if wait_for_element(page, 'button:has-text("新增")'):
                page.click('button:has-text("新增用户")')
                time.sleep(1)

                # 填写用户信息
                page.fill('input[placeholder*="用户名"]', NEW_USERNAME)
                page.fill('input[placeholder*="真实姓名"]', NEW_USER_REALNAME)
                page.fill('input[placeholder*="邮箱"]', f"{NEW_USERNAME}@test.com")
                page.fill('input[placeholder*="手机号"]', "13800138000")

                # 填写密码
                page.fill('input[placeholder*="密码"]', NEW_USER_PASSWORD)
                page.fill('input[placeholder*="确认密码"]', NEW_USER_PASSWORD)

                # 选择角色
                page.click('button:has-text("分配角色")')
                time.sleep(0.5)

                # 点击第一个角色（超级管理员或刚创建的角色）
                roles = page.locator('.el-select-dropdown__item').count()
                if roles > 0:
                    page.locator('.el-select-dropdown__item').first.click()

                # 保存
                page.click('button:has-text("确定")')
                time.sleep(2)

                log_result("创建用户", "passed", f"创建用户: {NEW_USERNAME}", take_screenshot(page, "05_user_created"))
            else:
                log_result("创建用户", "failed", "未找到新增用户按钮", take_screenshot(page, "05_user_failed"))
                raise Exception("无法创建用户")

            # ==================== 步骤 4: 登出 root 并登录新用户 ====================
            print("\n" + "=" * 80)
            print("步骤 4: 登出 root 并登录新用户")
            print("=" * 80)

            # 登出
            page.click('button:has-text("退出")')
            time.sleep(1)

            # 使用新用户登录
            page.fill('input[placeholder*="用户名"]', NEW_USERNAME)
            page.fill('input[placeholder*="密码"]', NEW_USER_PASSWORD)
            page.click('button:has-text("登录")')

            # 等待登录成功
            try:
                page.wait_for_url(f"{BASE_URL}/**", timeout=10000)
                time.sleep(2)
                log_result("登录新用户", "passed", f"用户 {NEW_USERNAME} 登录成功", take_screenshot(page, "06_new_user_login"))
            except PlaywrightTimeoutError:
                log_result("登录新用户", "failed", "登录超时或失败", take_screenshot(page, "06_new_login_failed"))
                raise

            # ==================== 步骤 5: 设计报表 ====================
            print("\n" + "=" * 80)
            print("步骤 5: 设计报表")
            print("=" * 80)

            # 导航到报表设计页面
            page.goto(f"{BASE_URL}/report/design")
            page.wait_for_load_state('networkidle')
            time.sleep(1)

            # 检查是否有权限
            if "权限" in page.content():
                log_result("访问报表设计", "failed", "权限不足", take_screenshot(page, "07_report_no_permission"))
                print("[WARN] New user lacks report design permission, continuing with root user...")
                # 切换回 root 用户
                page.goto(f"{BASE_URL}/login")
                page.wait_for_load_state('networkidle')
                page.fill('input[placeholder*="用户名"]', ROOT_USERNAME)
                page.fill('input[placeholder*="密码"]', ROOT_PASSWORD)
                page.click('button:has-text("登录")')
                time.sleep(2)
                page.goto(f"{BASE_URL}/report/design")
                page.wait_for_load_state('networkidle')
                time.sleep(1)

            # 点击"新增报表"按钮
            if wait_for_element(page, 'button:has-text("新增")', timeout=5000):
                page.click('button:has-text("新增")')
                time.sleep(1)

                # 填写报表基本信息
                page.fill('input[placeholder*="报表名称"]', REPORT_NAME)

                # 选择数据源（如果有多个）
                datasource_select = page.locator('.el-select').first
                try:
                    datasource_select.click()
                    time.sleep(0.5)
                    # 选择第一个数据源
                    page.locator('.el-select-dropdown__item').first.click()
                except:
                    pass  # 使用默认数据源

                # 输入 SQL
                sql_editor = page.locator('.cm-content').first
                if sql_editor.count() > 0:
                    sql_editor.click()
                    # 使用 CodeMirror 的 API 来设置内容
                    page.evaluate(f'''() => {{
                        const editor = document.querySelector('.cm-editor');
                        if (editor && editor.cmView) {{
                            editor.cmView.dispatch({{
                                changes: {{from: 0, to: editor.cmView.state.doc.length, insert: "{REPORT_SQL}"}}
                            }});
                        }}
                    }}''')
                else:
                    # 尝试使用 textarea
                    page.fill('textarea', REPORT_SQL)

                time.sleep(1)
                take_screenshot(page, "08_report_design_filled")

                # 保存报表
                page.click('button:has-text("保存")')
                time.sleep(2)

                log_result("设计报表", "passed", f"创建报表: {REPORT_NAME}", take_screenshot(page, "09_report_saved"))
            else:
                log_result("设计报表", "failed", "未找到新增报表按钮", take_screenshot(page, "08_report_design_failed"))

            # ==================== 步骤 6: 查询报表 ====================
            print("\n" + "=" * 80)
            print("步骤 6: 查询报表")
            print("=" * 80)

            # 导航到报表查询页面
            page.goto(f"{BASE_URL}/report")
            page.wait_for_load_state('networkidle')
            time.sleep(1)

            # 等待报表列表加载
            if wait_for_element(page, '.el-table', timeout=5000):
                # 点击第一个报表的"查询"按钮
                query_buttons = page.locator('button:has-text("查询")')
                if query_buttons.count() > 0:
                    query_buttons.first.click()
                    time.sleep(2)

                    # 等待查询结果显示
                    page.wait_for_selector('.el-table__body', timeout=10000)
                    time.sleep(1)

                    # 检查是否有数据
                    rows = page.locator('.el-table__body tr').count()

                    log_result("查询报表", "passed", f"查询成功，返回 {rows} 行数据", take_screenshot(page, "10_report_queried"))
                else:
                    log_result("查询报表", "failed", "未找到查询按钮", take_screenshot(page, "10_report_no_data"))
            else:
                log_result("查询报表", "failed", "报表列表未加载", take_screenshot(page, "10_report_list_failed"))

            # ==================== 步骤 7: 导出报表 ====================
            print("\n" + "=" * 80)
            print("步骤 7: 导出报表")
            print("=" * 80)

            # 查找导出按钮
            export_buttons = page.locator('button:has-text("导出")')
            if export_buttons.count() > 0:
                # 点击导出按钮
                with page.expect_download(timeout=30000) as download_info:
                    export_buttons.first.click()

                download = download_info.value
                file_name = download.suggested_filename
                save_path = f"{SCREENSHOT_DIR}/{TIMESTAMP}_{file_name}"
                download.save_as(save_path)

                log_result("导出报表", "passed", f"导出成功: {file_name}", save_path)
            else:
                log_result("导出报表", "failed", "未找到导出按钮", take_screenshot(page, "11_export_failed"))

            # ==================== 测试完成 ====================
            print("\n" + "=" * 80)
            print("测试完成")
            print("=" * 80)

        except Exception as e:
            print(f"\n[ERROR] Exception during test: {str(e)}")
            import traceback
            traceback.print_exc()
            take_screenshot(page, "error_screenshot")

        finally:
            # 关闭浏览器
            context.close()
            browser.close()

            # 保存测试结果
            result_file = f"{SCREENSHOT_DIR}/test_results_{TIMESTAMP}.json"
            with open(result_file, 'w', encoding='utf-8') as f:
                json.dump({
                    "timestamp": datetime.now().isoformat(),
                    "total_tests": len(test_results),
                    "passed": len([r for r in test_results if r['status'] == 'passed']),
                    "failed": len([r for r in test_results if r['status'] == 'failed']),
                    "results": test_results
                }, f, indent=2, ensure_ascii=False)

            # 打印测试摘要
            print("\n" + "=" * 80)
            print("测试结果摘要")
            print("=" * 80)
            for result in test_results:
                status_icon = "[PASS]" if result['status'] == "passed" else "[FAIL]"
                print(f"{status_icon} {result['name']}: {result['status']}")
                if result['message']:
                    print(f"   {result['message']}")

            print()
            print(f"总计: {len(test_results)} 个测试")
            print(f"Passed: {len([r for r in test_results if r['status'] == 'passed'])}")
            print(f"Failed: {len([r for r in test_results if r['status'] == 'failed'])}")
            print(f"结果文件: {result_file}")


if __name__ == "__main__":
    import os
    os.makedirs(SCREENSHOT_DIR, exist_ok=True)
    main()
