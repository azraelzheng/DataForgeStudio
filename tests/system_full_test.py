#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
DataForgeStudio V4 完整系统功能测试脚本
使用 Playwright 同步 API 测试所有核心功能模块
"""

import os
import sys
import time
import json
import hashlib
from datetime import datetime
from pathlib import Path
from playwright.sync_api import sync_playwright, expect, TimeoutError as PlaywrightTimeoutError

# 设置 UTF-8 编码输出（Windows 兼容）
if sys.platform == "win32":
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

# ==================== 配置 ====================
FRONTEND_URL = "http://localhost:5173"
BACKEND_URL = "http://localhost:5000"

# 测试凭证
TEST_USERNAME = "root"
TEST_PASSWORD = os.getenv("TEST_PASSWORD", "admin123")

# 截图目录
SCREENSHOT_DIR = Path("test_screenshots")
SCREENSHOT_DIR.mkdir(exist_ok=True)

# 测试结果
TEST_RESULTS = {
    "timestamp": datetime.now().isoformat(),
    "test_duration": None,
    "tests": [],
    "issues": [],
    "summary": {"total": 0, "passed": 0, "failed": 0, "skipped": 0},
    "modules": {}
}

# 测试开始时间
TEST_START_TIME = None

# ==================== 工具函数 ====================
def log(message, level="INFO"):
    """记录日志"""
    timestamp = datetime.now().strftime("%H:%M:%S")
    try:
        print(f"[{timestamp}] [{level}] {message}", flush=True)
    except Exception:
        print(f"[{timestamp}] {level} {message}", flush=True)

def take_screenshot(page, name):
    """截图并保存"""
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"{timestamp}_{name}.png"
    filepath = SCREENSHOT_DIR / filename
    try:
        page.screenshot(path=str(filepath), full_page=True)
        log(f"截图已保存: {filepath}")
        return filepath
    except Exception as e:
        log(f"截图失败: {e}", "WARN")
        return None

def record_test(test_name, passed, message="", screenshot=None, issue=None, module=None):
    """记录测试结果"""
    test_result = {
        "name": test_name,
        "status": "passed" if passed else "failed",
        "message": message,
        "screenshot": str(screenshot) if screenshot else None,
        "timestamp": datetime.now().isoformat()
    }

    TEST_RESULTS["tests"].append(test_result)

    if issue and passed is False:
        TEST_RESULTS["issues"].append({
            "test": test_name,
            "issue": issue,
            "screenshot": str(screenshot) if screenshot else None
        })

    # 按模块统计
    if module:
        if module not in TEST_RESULTS["modules"]:
            TEST_RESULTS["modules"][module] = {"total": 0, "passed": 0, "failed": 0}
        TEST_RESULTS["modules"][module]["total"] += 1
        if passed:
            TEST_RESULTS["modules"][module]["passed"] += 1
        else:
            TEST_RESULTS["modules"][module]["failed"] += 1

    TEST_RESULTS["summary"]["total"] += 1
    if passed:
        TEST_RESULTS["summary"]["passed"] += 1
        log(f"[PASS] {test_name}")
    else:
        TEST_RESULTS["summary"]["failed"] += 1
        log(f"[FAIL] {test_name}: {message}")

def wait_for_page_load(page, timeout=10000):
    """等待页面完全加载"""
    try:
        page.wait_for_load_state("domcontentloaded", timeout=timeout)
        page.wait_for_load_state("networkidle", timeout=timeout)
        time.sleep(1)
        return True
    except:
        return False

def check_backend_health(page):
    """检查后端服务健康状态"""
    log("检查后端服务状态...")
    try:
        response = page.request.get(f"{BACKEND_URL}/health", timeout=5000)
        if response.ok:
            log("后端服务正常", "PASS")
            return True
        else:
            log(f"后端服务异常: {response.status}", "WARN")
            return False
    except Exception as e:
        log(f"后端服务不可用: {str(e)[:100]}", "WARN")
        return False

# ==================== 测试模块 ====================

def test_module_1_authentication(page):
    """模块1: 认证登录测试"""
    log("\n" + "="*60)
    log("模块1: 认证登录测试")
    log("="*60)

    module = "认证登录"

    # 测试1.1: 访问首页重定向到登录页
    log("\n测试1.1: 访问首页重定向")
    try:
        page.goto(FRONTEND_URL, timeout=10000)
        wait_for_page_load(page)

        current_url = page.url
        is_login_page = "/login" in current_url or current_url == FRONTEND_URL + "/"

        screenshot = take_screenshot(page, "01_home_redirect")

        record_test(
            "首页重定向到登录页",
            is_login_page,
            f"当前URL: {current_url}",
            screenshot,
            module=module
        )
    except Exception as e:
        screenshot = take_screenshot(page, "01_home_error")
        record_test(
            "首页重定向到登录页",
            False,
            f"异常: {str(e)[:100]}",
            screenshot,
            str(e),
            module
        )

    # 测试1.2: 登录表单显示
    log("\n测试1.2: 登录表单显示")
    try:
        if "/login" not in page.url:
            page.goto(f"{FRONTEND_URL}/login", timeout=10000)
            wait_for_page_load(page)

        # 检查登录表单元素
        has_username_input = len(page.query_selector_all("input[type='text'], input[placeholder*='用户'], input[placeholder*='账号']")) > 0
        has_password_input = len(page.query_selector_all("input[type='password']")) > 0
        has_login_button = len(page.query_selector_all("button[type='submit'], button:has-text('登'), button:has-text('Login')")) > 0

        form_ok = has_username_input and has_password_input and has_login_button

        screenshot = take_screenshot(page, "02_login_form")

        record_test(
            "登录表单显示完整",
            form_ok,
            f"用户名输入框: {has_username_input}, 密码输入框: {has_password_input}, 登录按钮: {has_login_button}",
            screenshot,
            "登录表单元素缺失" if not form_ok else None,
            module
        )
    except Exception as e:
        record_test("登录表单显示完整", False, f"异常: {str(e)[:100]}", None, str(e), module)

    # 测试1.3: 登录功能
    log("\n测试1.3: 用户登录")
    try:
        # 填写登录表单
        username_inputs = page.query_selector_all("input[type='text'], input:not([type])")
        password_inputs = page.query_selector_all("input[type='password']")

        if not username_inputs or not password_inputs:
            record_test("用户登录", False, "未找到登录表单元素", None, "登录表单元素缺失", module)
            return False

        username_inputs[0].fill(TEST_USERNAME)
        password_inputs[0].fill(TEST_PASSWORD)
        time.sleep(0.5)

        screenshot = take_screenshot(page, "03_form_filled")

        # 点击登录按钮
        login_buttons = page.query_selector_all("button:has-text('登'), button:has-text('Login'), button[type='submit']")

        if not login_buttons:
            record_test("用户登录", False, "未找到登录按钮", screenshot, "登录按钮缺失", module)
            return False

        login_buttons[0].click()
        log("已点击登录按钮，等待响应...")

        # 等待登录响应
        time.sleep(5)

        screenshot = take_screenshot(page, "04_after_login")

        # 检查登录结果
        current_url = page.url
        log(f"登录后URL: {current_url}")

        # 检查错误消息
        error_elements = page.query_selector_all(".el-message--error, .error-message, [class*='error']")
        login_errors = []
        for elem in error_elements:
            try:
                text = elem.inner_text()
                if text and len(text) < 200:
                    login_errors.append(text)
            except:
                pass

        # 判断登录成功
        success_indicators = [
            "/home" in current_url,
            "/dashboard" in current_url.lower(),
        ]
        has_user_info = len(page.query_selector_all("[class*='user'], [class*='avatar'], .username, .el-dropdown")) > 0

        login_success = any(success_indicators) or (has_user_info and not login_errors)

        if login_errors:
            error_msg = f"登录失败: {'; '.join(login_errors[:2])}"
            log(f"登录错误: {error_msg}")
            record_test("用户登录", False, error_msg, screenshot, f"服务器返回登录错误: {'; '.join(login_errors)}", module)
        elif not login_success:
            record_test("用户登录", False, f"未能确认登录成功, URL: {current_url}", screenshot, "登录后未能正确跳转或显示用户信息", module)
        else:
            record_test("用户登录", True, f"登录成功, URL: {current_url}", screenshot, module=module)

        return login_success

    except Exception as e:
        screenshot = take_screenshot(page, "03_login_error")
        record_test("用户登录", False, f"异常: {str(e)[:100]}", screenshot, str(e), module)
        return False

def test_module_2_user_management(page):
    """模块2: 用户管理测试"""
    log("\n" + "="*60)
    log("模块2: 用户管理测试")
    log("="*60)

    module = "用户管理"

    # 测试2.1: 访问用户管理页面
    log("\n测试2.1: 访问用户管理页面")
    try:
        page.goto(f"{FRONTEND_URL}/system/user", timeout=10000)
        wait_for_page_load(page)

        current_url = page.url
        page_loaded = "/user" in current_url

        screenshot = take_screenshot(page, "05_user_management")

        # 检查页面内容
        has_table = len(page.query_selector_all("table, .el-table")) > 0
        has_toolbar = len(page.query_selector_all(".toolbar, .el-toolbar")) > 0

        page_ok = page_loaded and has_table

        record_test(
            "访问用户管理页面",
            page_ok,
            f"URL: {current_url}, 表格: {has_table}, 工具栏: {has_toolbar}",
            screenshot,
            "页面未正确加载或缺少表格组件" if not page_ok else None,
            module
        )
    except Exception as e:
        screenshot = take_screenshot(page, "05_user_error")
        record_test("访问用户管理页面", False, f"异常: {str(e)[:100]}", screenshot, str(e), module)

    # 测试2.2: 新增用户功能
    log("\n测试2.2: 新增用户功能")
    try:
        # 查找新增按钮
        add_buttons = page.query_selector_all("button:has-text('新增'), button:has-text('添加'), button:has-text('Add'), .el-button--primary")

        if not add_buttons:
            record_test("新增用户功能", False, "未找到新增按钮", None, "新增按钮缺失", module)
        else:
            # 点击第一个新增按钮
            add_buttons[0].click()
            time.sleep(1)

            screenshot = take_screenshot(page, "06_add_user_dialog")

            # 检查对话框是否打开
            has_dialog = len(page.query_selector_all(".el-dialog, .el-overlay, dialog")) > 0
            has_username_input = len(page.query_selector_all("input[placeholder*='用户'], input[placeholder*='账号']")) > 0

            if has_dialog:
                # 关闭对话框
                close_buttons = page.query_selector_all(".el-dialog__close, .el-icon-close, button:has-text('取消'), button:has-text('Cancel')")
                if close_buttons:
                    close_buttons[0].click()
                    time.sleep(0.5)

            record_test(
                "新增用户功能",
                has_dialog and has_username_input,
                f"对话框: {has_dialog}, 用户名输入框: {has_username_input}",
                screenshot,
                "新增对话框未正确显示或缺少用户名输入框" if not (has_dialog and has_username_input) else None,
                module
            )
    except Exception as e:
        record_test("新增用户功能", False, f"异常: {str(e)[:100]}", None, str(e), module)

def test_module_3_role_management(page):
    """模块3: 角色管理测试"""
    log("\n" + "="*60)
    log("模块3: 角色管理测试")
    log("="*60)

    module = "角色管理"

    # 测试3.1: 访问角色管理页面
    log("\n测试3.1: 访问角色管理页面")
    try:
        page.goto(f"{FRONTEND_URL}/system/role", timeout=10000)
        wait_for_page_load(page)

        current_url = page.url
        page_loaded = "/role" in current_url

        screenshot = take_screenshot(page, "07_role_management")

        has_table = len(page.query_selector_all("table, .el-table")) > 0

        record_test(
            "访问角色管理页面",
            page_loaded and has_table,
            f"URL: {current_url}, 表格: {has_table}",
            screenshot,
            "页面未正确加载" if not (page_loaded and has_table) else None,
            module
        )
    except Exception as e:
        screenshot = take_screenshot(page, "07_role_error")
        record_test("访问角色管理页面", False, f"异常: {str(e)[:100]}", screenshot, str(e), module)

def test_module_4_datasource_management(page):
    """模块4: 数据源管理测试"""
    log("\n" + "="*60)
    log("模块4: 数据源管理测试")
    log("="*60)

    module = "数据源管理"

    # 测试4.1: 访问数据源管理页面
    log("\n测试4.1: 访问数据源管理页面")
    try:
        page.goto(f"{FRONTEND_URL}/system/datasource", timeout=10000)
        wait_for_page_load(page)

        current_url = page.url
        page_loaded = "/datasource" in current_url

        screenshot = take_screenshot(page, "08_datasource_management")

        has_table = len(page.query_selector_all("table, .el-table")) > 0

        record_test(
            "访问数据源管理页面",
            page_loaded and has_table,
            f"URL: {current_url}, 表格: {has_table}",
            screenshot,
            "页面未正确加载" if not (page_loaded and has_table) else None,
            module
        )
    except Exception as e:
        screenshot = take_screenshot(page, "08_datasource_error")
        record_test("访问数据源管理页面", False, f"异常: {str(e)[:100]}", screenshot, str(e), module)

def test_module_5_report_management(page):
    """模块5: 报表管理测试"""
    log("\n" + "="*60)
    log("模块5: 报表管理测试")
    log("="*60)

    module = "报表管理"

    # 测试5.1: 访问报表列表页面
    log("\n测试5.1: 访问报表列表页面")
    try:
        page.goto(f"{FRONTEND_URL}/report", timeout=10000)
        wait_for_page_load(page)

        current_url = page.url
        page_loaded = "/report" in current_url

        screenshot = take_screenshot(page, "09_report_list")

        has_table = len(page.query_selector_all("table, .el-table")) > 0
        has_toolbar = len(page.query_selector_all(".toolbar, .el-toolbar")) > 0

        record_test(
            "访问报表列表页面",
            page_loaded and has_table,
            f"URL: {current_url}, 表格: {has_table}, 工具栏: {has_toolbar}",
            screenshot,
            "页面未正确加载" if not (page_loaded and has_table) else None,
            module
        )
    except Exception as e:
        screenshot = take_screenshot(page, "09_report_error")
        record_test("访问报表列表页面", False, f"异常: {str(e)[:100]}", screenshot, str(e), module)

    # 测试5.2: 新增报表功能
    log("\n测试5.2: 新增报表功能")
    try:
        # 查找新增按钮
        add_buttons = page.query_selector_all("button:has-text('新增'), button:has-text('添加'), button:has-text('Add')")

        if add_buttons:
            add_buttons[0].click()
            time.sleep(1)

        screenshot = take_screenshot(page, "10_add_report")

        has_dialog = len(page.query_selector_all(".el-dialog, .el-overlay")) > 0
        has_sql_editor = len(page.query_selector_all(".CodeMirror, .cm-editor, [class*='sql'], textarea")) > 0

        if has_dialog:
            # 关闭对话框
            close_buttons = page.query_selector_all(".el-dialog__close, .el-icon-close, button:has-text('取消')")
            if close_buttons:
                close_buttons[0].click()
                time.sleep(0.5)

        record_test(
            "新增报表功能",
            has_dialog and has_sql_editor,
            f"对话框: {has_dialog}, SQL编辑器: {has_sql_editor}",
            screenshot,
            "新增对话框未显示或缺少SQL编辑器" if not (has_dialog and has_sql_editor) else None,
            module
        )
    except Exception as e:
        record_test("新增报表功能", False, f"异常: {str(e)[:100]}", None, str(e), module)

def test_module_6_license_management(page):
    """模块6: 许可证管理测试"""
    log("\n" + "="*60)
    log("模块6: 许可证管理测试")
    log("="*60)

    module = "许可证管理"

    # 测试6.1: 访问许可证管理页面
    log("\n测试6.1: 访问许可证管理页面")
    try:
        page.goto(f"{FRONTEND_URL}/license", timeout=10000)
        wait_for_page_load(page)

        current_url = page.url
        page_loaded = "/license" in current_url

        screenshot = take_screenshot(page, "11_license_management")

        has_content = len(page.query_selector_all(".license-info, .el-card, .el-descriptions")) > 0

        record_test(
            "访问许可证管理页面",
            page_loaded,
            f"URL: {current_url}, 内容: {has_content}",
            screenshot,
            "页面未正确加载" if not page_loaded else None,
            module
        )
    except Exception as e:
        screenshot = take_screenshot(page, "11_license_error")
        record_test("访问许可证管理页面", False, f"异常: {str(e)[:100]}", screenshot, str(e), module)

def test_module_7_operation_logs(page):
    """模块7: 操作日志测试"""
    log("\n" + "="*60)
    log("模块7: 操作日志测试")
    log("="*60)

    module = "操作日志"

    # 测试7.1: 访问操作日志页面
    log("\n测试7.1: 访问操作日志页面")
    try:
        page.goto(f"{FRONTEND_URL}/system/log", timeout=10000)
        wait_for_page_load(page)

        current_url = page.url
        page_loaded = "/log" in current_url

        screenshot = take_screenshot(page, "12_operation_logs")

        has_table = len(page.query_selector_all("table, .el-table")) > 0

        record_test(
            "访问操作日志页面",
            page_loaded and has_table,
            f"URL: {current_url}, 表格: {has_table}",
            screenshot,
            "页面未正确加载" if not (page_loaded and has_table) else None,
            module
        )
    except Exception as e:
        screenshot = take_screenshot(page, "12_log_error")
        record_test("访问操作日志页面", False, f"异常: {str(e)[:100]}", screenshot, str(e), module)

def test_module_8_navigation_and_ui(page):
    """模块8: 导航和UI组件测试"""
    log("\n" + "="*60)
    log("模块8: 导航和UI组件测试")
    log("="*60)

    module = "导航和UI"

    # 测试8.1: 侧边栏导航
    log("\n测试8.1: 侧边栏导航")
    try:
        page.goto(f"{FRONTEND_URL}/home", timeout=10000)
        wait_for_page_load(page)

        screenshot = take_screenshot(page, "13_home_navigation")

        has_sidebar = len(page.query_selector_all(".sidebar, .el-aside, aside, [class*='sidebar'], [class*='aside']")) > 0
        menu_items = page.query_selector_all(".el-menu-item, nav a, [role='menuitem']")
        has_menu = len(menu_items) > 0

        record_test(
            "侧边栏导航显示",
            has_sidebar and has_menu,
            f"侧边栏: {has_sidebar}, 菜单项: {len(menu_items)}",
            screenshot,
            "侧边栏或菜单未正确显示" if not (has_sidebar and has_menu) else None,
            module
        )
    except Exception as e:
        screenshot = take_screenshot(page, "13_nav_error")
        record_test("侧边栏导航显示", False, f"异常: {str(e)[:100]}", screenshot, str(e), module)

    # 测试8.2: 用户信息和登出
    log("\n测试8.2: 用户信息和登出功能")
    try:
        # 查找用户信息区域
        user_dropdowns = page.query_selector_all(".el-dropdown, [class*='user-info'], [class*='user-dropdown']")

        screenshot = take_screenshot(page, "14_user_info")

        has_user_display = len(user_dropdowns) > 0 or len(page.query_selector_all("[class*='avatar'], .username")) > 0

        record_test(
            "用户信息显示",
            has_user_display,
            f"用户信息区域: {has_user_display}",
            screenshot,
            "未找到用户信息显示区域",
            module
        )
    except Exception as e:
        record_test("用户信息显示", False, f"异常: {str(e)[:100]}", None, str(e), module)

# ==================== 主测试流程 ====================
def main():
    global TEST_START_TIME
    TEST_START_TIME = datetime.now()

    log("=" * 60)
    log("DataForgeStudio V4 完整系统功能测试")
    log("=" * 60)
    log(f"前端地址: {FRONTEND_URL}")
    log(f"后端地址: {BACKEND_URL}")
    log(f"测试用户: {TEST_USERNAME}")
    log(f"截图目录: {SCREENSHOT_DIR.absolute()}")
    log("=" * 60)

    with sync_playwright() as p:
        # 启动浏览器
        log("启动浏览器...")
        browser = p.chromium.launch(
            headless=False,
            slow_mo=200,
            args=['--start-maximized']
        )

        # 创建浏览器上下文
        context = browser.new_context(
            viewport={"width": 1920, "height": 1080},
            locale="zh-CN"
        )

        # 创建页面
        page = context.new_page()
        page.set_default_timeout(10000)

        try:
            # 首先检查后端服务
            if not check_backend_health(page):
                log("后端服务不可用，部分测试将失败", "WARN")

            # 执行测试模块
            login_success = test_module_1_authentication(page)

            if login_success:
                # 只有登录成功才执行后续测试
                test_module_2_user_management(page)
                test_module_3_role_management(page)
                test_module_4_datasource_management(page)
                test_module_5_report_management(page)
                test_module_6_license_management(page)
                test_module_7_operation_logs(page)
                test_module_8_navigation_and_ui(page)
            else:
                log("登录失败，跳过需要认证的测试模块", "WARN")

        finally:
            # 等待查看
            log("\n测试完成，等待3秒...")
            time.sleep(3)
            browser.close()

    # 计算测试时长
    test_duration = (datetime.now() - TEST_START_TIME).total_seconds()
    TEST_RESULTS["test_duration"] = f"{test_duration:.2f}s"

    # 保存测试结果
    results_file = SCREENSHOT_DIR / f"test_results_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
    with open(results_file, "w", encoding="utf-8") as f:
        json.dump(TEST_RESULTS, f, ensure_ascii=False, indent=2)

    # 打印测试总结
    print_test_summary(results_file)

    return summary["failed"] == 0

def print_test_summary(results_file):
    """打印测试总结"""
    log("\n" + "=" * 60)
    log("测试总结")
    log("=" * 60)

    global summary
    summary = TEST_RESULTS["summary"]
    log(f"总测试数: {summary['total']}")
    log(f"通过: {summary['passed']}")
    log(f"失败: {summary['failed']}")
    log(f"跳过: {summary['skipped']}")
    log(f"测试时长: {TEST_RESULTS['test_duration']}")

    if summary['total'] > 0:
        pass_rate = (summary['passed'] / summary['total']) * 100
        log(f"通过率: {pass_rate:.1f}%")

    log(f"测试结果: {results_file}")
    log("=" * 60)

    # 打印模块测试结果
    if TEST_RESULTS["modules"]:
        log("\n各模块测试结果:")
        for module, stats in TEST_RESULTS["modules"].items():
            pass_rate = (stats['passed'] / stats['total'] * 100) if stats['total'] > 0 else 0
            log(f"  {module}: {stats['passed']}/{stats['total']} 通过 ({pass_rate:.0f}%)")

    # 打印发现的问题
    if TEST_RESULTS["issues"]:
        log("\n发现的问题:")
        for i, issue in enumerate(TEST_RESULTS["issues"], 1):
            log(f"{i}. {issue['test']}: {issue['issue']}")

    # 打印所有测试结果
    log("\n测试详情:")
    for test in TEST_RESULTS["tests"]:
        status = "[PASS]" if test["status"] == "passed" else "[FAIL]"
        log(f"  {status} {test['name']}: {test['message']}")

if __name__ == "__main__":
    try:
        success = main()
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        log("\n测试被用户中断", "WARN")
        sys.exit(1)
    except Exception as e:
        log(f"\n测试执行失败: {e}", "ERROR")
        import traceback
        traceback.print_exc()
        sys.exit(1)
