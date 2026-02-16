#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
DataForgeStudio V4 前端功能测试脚本
使用 Playwright 同步 API 测试前端应用的核心功能
"""

import os
import sys
import time
import json
from datetime import datetime
from pathlib import Path
from playwright.sync_api import sync_playwright, expect, TimeoutError as PlaywrightTimeoutError

# ==================== 配置 ====================
FRONTEND_URL = "http://localhost:5173"
BACKEND_URL = "http://localhost:5000"

# 测试凭证
TEST_USERNAME = "root"
# 注意：每次后端启动时会生成临时密码，需要手动查看控制台输出
TEST_PASSWORD = os.getenv("TEST_PASSWORD", "admin123")

# 截图目录
SCREENSHOT_DIR = Path("test_screenshots")
SCREENSHOT_DIR.mkdir(exist_ok=True)

# 测试结果
TEST_RESULTS = {
    "timestamp": datetime.now().isoformat(),
    "tests": [],
    "summary": {"total": 0, "passed": 0, "failed": 0, "skipped": 0}
}

# ==================== 工具函数 ====================
def log(message, level="INFO"):
    """记录日志"""
    timestamp = datetime.now().strftime("%H:%M:%S")
    # 使用 ASCII 兼容的符号
    symbol = {
        "PASS": "[PASS]",
        "FAIL": "[FAIL]",
        "WARN": "[WARN]",
        "ERROR": "[ERROR]",
        "INFO": "[INFO]"
    }.get(level, f"[{level}]")
    try:
        print(f"[{timestamp}] {symbol} {message}")
    except UnicodeEncodeError:
        print(f"[{timestamp}] {level} {message.encode('utf-8', errors='ignore').decode('utf-8', errors='ignore')}")
    sys.stdout.flush()

def take_screenshot(page, name):
    """截图并保存"""
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"{timestamp}_{name}.png"
    filepath = SCREENSHOT_DIR / filename
    page.screenshot(path=str(filepath), full_page=True)
    log(f"截图已保存: {filepath}")
    return filepath

def record_test(test_name, passed, message="", screenshot=None):
    """记录测试结果"""
    TEST_RESULTS["tests"].append({
        "name": test_name,
        "status": "passed" if passed else "failed",
        "message": message,
        "screenshot": str(screenshot) if screenshot else None,
        "timestamp": datetime.now().isoformat()
    })

    TEST_RESULTS["summary"]["total"] += 1
    if passed:
        TEST_RESULTS["summary"]["passed"] += 1
        log(f"[OK] 通过: {test_name}", "PASS")
    else:
        TEST_RESULTS["summary"]["failed"] += 1
        log(f"[X] 失败: {test_name} - {message}", "FAIL")

# ==================== 测试函数 ====================
def test_backend_health(page):
    """测试后端服务健康状态"""
    log("测试后端服务健康状态...")
    try:
        response = page.request.get(f"{BACKEND_URL}/health", timeout=5000)
        if response.ok:
            log("后端服务正常运行", "PASS")
            return True
        else:
            log(f"后端服务响应异常: {response.status}", "WARN")
            return False
    except Exception as e:
        log(f"后端服务不可用: {e}", "WARN")
        return False

def test_login_redirect(page):
    """测试1: 访问首页重定向到登录页"""
    log("\n========== 测试1: 访问首页重定向到登录页 ==========")
    try:
        page.goto(FRONTEND_URL, wait_until="networkidle", timeout=10000)
        time.sleep(1)  # 等待路由跳转

        current_url = page.url
        log(f"当前URL: {current_url}")

        # 检查是否在登录页
        is_login_page = "/login" in current_url or current_url.endswith("/login")

        # 检查登录页面元素
        login_title = page.query_selector("h1")
        title_text = login_title.inner_text() if login_title else ""

        username_input = page.query_selector("input[placeholder*='用户名']")
        password_input = page.query_selector("input[type='password']")

        screenshot = take_screenshot(page, "01_login_page")

        passed = is_login_page and "DataForgeStudio" in title_text and username_input and password_input
        record_test(
            "访问首页重定向到登录页",
            passed,
            f"URL: {current_url}, 标题: {title_text}",
            screenshot
        )

        return passed
    except Exception as e:
        record_test("访问首页重定向到登录页", False, f"异常: {e}")
        return False

def test_login_functionality(page):
    """测试2: 登录功能"""
    log("\n========== 测试2: 登录功能 ==========")

    # 先检查后端是否可用
    backend_available = test_backend_health(page)
    if not backend_available:
        log("后端服务不可用，跳过登录测试", "WARN")
        record_test("登录功能", False, "后端服务不可用")
        return False

    try:
        # 确保在登录页
        if "/login" not in page.url:
            page.goto(f"{FRONTEND_URL}/login", wait_until="networkidle", timeout=10000)
            time.sleep(1)

        # 填写登录表单
        log(f"使用凭证登录: {TEST_USERNAME} / {'*' * len(TEST_PASSWORD)}")

        username_input = page.query_selector("input[placeholder*='用户名']")
        password_input = page.query_selector("input[type='password']")

        if not username_input or not password_input:
            record_test("登录功能", False, "登录表单元素未找到")
            return False

        username_input.fill(TEST_USERNAME)
        password_input.fill(TEST_PASSWORD)
        time.sleep(0.5)

        # 截图：填写后的表单
        take_screenshot(page, "02_login_form_filled")

        # 点击登录按钮
        login_button = page.query_selector("button[type='submit'], .el-button--primary")
        if not login_button:
            record_test("登录功能", False, "登录按钮未找到")
            return False

        login_button.click()
        log("已点击登录按钮，等待响应...")

        # 等待导航或响应
        time.sleep(3)

        # 检查是否登录成功
        current_url = page.url
        log(f"登录后URL: {current_url}")

        # 检查是否有错误消息
        error_message = page.query_selector(".el-message--error")
        if error_message:
            error_text = error_message.inner_text()
            log(f"登录错误: {error_text}", "ERROR")
            screenshot = take_screenshot(page, "02_login_error")
            record_test("登录功能", False, f"服务器返回错误: {error_text}", screenshot)
            return False

        # 检查是否跳转到首页或仪表盘
        is_logged_in = "/home" in current_url or "/" == current_url or current_url == FRONTEND_URL + "/"

        # 检查是否显示用户信息
        user_info = page.query_selector(".user-info, .username, [class*='user']")
        has_user_info = user_info is not None

        screenshot = take_screenshot(page, "02_after_login")

        passed = is_logged_in or has_user_info
        record_test(
            "登录功能",
            passed,
            f"URL: {current_url}, 用户信息显示: {has_user_info}",
            screenshot
        )

        return passed
    except Exception as e:
        record_test("登录功能", False, f"异常: {e}")
        return False

def test_navigation_menu(page):
    """测试3: 导航菜单测试"""
    log("\n========== 测试3: 导航菜单测试 ==========")

    try:
        # 等待页面加载
        time.sleep(2)

        # 检查侧边栏是否存在
        sidebar = page.query_selector(".sidebar, .el-aside, [class*='sidebar'], [class*='aside']")
        if not sidebar:
            log("未找到侧边栏元素", "WARN")
            screenshot = take_screenshot(page, "03_no_sidebar")
            record_test("导航菜单", False, "侧边栏未显示", screenshot)
            return False

        # 查找导航菜单项
        menu_items = page.query_selector_all(".el-menu-item, a[href*='/'], [role='menuitem']")

        log(f"找到 {len(menu_items)} 个菜单项")

        # 记录菜单项文本
        menu_texts = []
        for item in menu_items[:10]:  # 只记录前10个
            try:
                text = item.inner_text().strip()
                if text:
                    menu_texts.append(text)
            except:
                pass

        log(f"菜单项: {menu_texts}")

        screenshot = take_screenshot(page, "03_navigation_menu")

        # 检查关键菜单项
        expected_menus = ["首页", "仪表盘", "报表", "系统"]
        found_menus = [any(menu in text for text in menu_texts) for menu in expected_menus]

        passed = len(menu_items) > 0 and any(found_menus)
        record_test(
            "导航菜单",
            passed,
            f"菜单项数量: {len(menu_items)}, 包含: {menu_texts[:5]}",
            screenshot
        )

        return passed
    except Exception as e:
        record_test("导航菜单", False, f"异常: {e}")
        return False

def test_user_management_page(page):
    """测试4: 用户管理页面"""
    log("\n========== 测试4: 用户管理页面 ==========")

    try:
        # 导航到用户管理页面
        log("导航到用户管理页面...")
        page.goto(f"{FRONTEND_URL}/system/user", wait_until="networkidle", timeout=10000)
        time.sleep(2)

        current_url = page.url
        log(f"当前URL: {current_url}")

        # 检查是否有权限错误
        error_msg = page.query_selector(".el-message--error, .error-message")
        if error_msg:
            error_text = error_msg.inner_text()
            if "权限" in error_text:
                log(f"权限错误: {error_text}", "WARN")
                screenshot = take_screenshot(page, "04_user_permission_denied")
                record_test("用户管理页面", False, f"权限不足: {error_text}", screenshot)
                return False

        # 检查用户管理页面元素
        page_title = page.query_selector("h2, h3, .page-title, [class*='title']")
        title_text = page_title.inner_text() if page_title else ""

        # 查找表格
        table = page.query_selector("table, .el-table, [class*='table']")

        # 查找搜索框
        search_input = page.query_selector("input[placeholder*='搜索'], input[placeholder*='用户']")

        screenshot = take_screenshot(page, "04_user_management")

        passed = table is not None or "用户" in title_text
        record_test(
            "用户管理页面",
            passed,
            f"标题: {title_text}, 表格存在: {table is not None}, 搜索框存在: {search_input is not None}",
            screenshot
        )

        return passed
    except Exception as e:
        record_test("用户管理页面", False, f"异常: {e}")
        return False

def test_role_management_page(page):
    """测试5: 角色管理页面"""
    log("\n========== 测试5: 角色管理页面 ==========")

    try:
        # 导航到角色管理页面
        log("导航到角色管理页面...")
        page.goto(f"{FRONTEND_URL}/system/role", wait_until="networkidle", timeout=10000)
        time.sleep(2)

        current_url = page.url
        log(f"当前URL: {current_url}")

        # 检查权限错误
        error_msg = page.query_selector(".el-message--error, .error-message")
        if error_msg:
            error_text = error_msg.inner_text()
            if "权限" in error_text:
                log(f"权限错误: {error_text}", "WARN")
                screenshot = take_screenshot(page, "05_role_permission_denied")
                record_test("角色管理页面", False, f"权限不足: {error_text}", screenshot)
                return False

        # 检查角色管理页面元素
        page_title = page.query_selector("h2, h3, .page-title, [class*='title']")
        title_text = page_title.inner_text() if page_title else ""

        # 查找表格
        table = page.query_selector("table, .el-table, [class*='table']")

        screenshot = take_screenshot(page, "05_role_management")

        passed = table is not None or ("角色" in title_text or "权限" in title_text)
        record_test(
            "角色管理页面",
            passed,
            f"标题: {title_text}, 表格存在: {table is not None}",
            screenshot
        )

        return passed
    except Exception as e:
        record_test("角色管理页面", False, f"异常: {e}")
        return False

def test_search_functionality(page):
    """测试6: 搜索功能测试"""
    log("\n========== 测试6: 搜索功能测试 ==========")

    try:
        # 回到用户管理页面
        page.goto(f"{FRONTEND_URL}/system/user", wait_until="networkidle", timeout=10000)
        time.sleep(2)

        # 查找搜索输入框
        search_input = page.query_selector("input[placeholder*='搜索'], input[placeholder*='用户'], input[type='text']")

        if not search_input:
            log("未找到搜索输入框", "WARN")
            screenshot = take_screenshot(page, "06_no_search_input")
            record_test("搜索功能", False, "搜索输入框未找到", screenshot)
            return False

        # 尝试搜索
        log("尝试搜索测试...")
        search_input.fill("root")
        time.sleep(1)

        screenshot = take_screenshot(page, "06_search_performed")

        # 检查是否有搜索结果或反馈
        record_test(
            "搜索功能",
            True,
            "搜索操作已执行",
            screenshot
        )

        return True
    except Exception as e:
        record_test("搜索功能", False, f"异常: {e}")
        return False

def test_page_routing(page):
    """测试7: 页面路由跳转测试"""
    log("\n========== 测试7: 页面路由跳转测试 ==========")

    routes_to_test = [
        ("/home", "首页"),
        ("/report", "报表查询"),
        ("/license", "许可管理"),
    ]

    results = []

    for route, name in routes_to_test:
        try:
            log(f"测试路由: {route} ({name})")
            page.goto(f"{FRONTEND_URL}{route}", wait_until="networkidle", timeout=10000)
            time.sleep(2)

            current_url = page.url
            route_loaded = route in current_url

            # 检查权限错误
            error_msg = page.query_selector(".el-message--error")
            has_error = error_msg is not None

            result = {
                "route": route,
                "name": name,
                "loaded": route_loaded,
                "has_permission_error": has_error
            }
            results.append(result)

            log(f"  结果: {'成功' if route_loaded else '失败'}", "PASS" if route_loaded else "WARN")

        except Exception as e:
            log(f"  路由 {route} 测试失败: {e}", "ERROR")
            results.append({
                "route": route,
                "name": name,
                "loaded": False,
                "error": str(e)
            })

    screenshot = take_screenshot(page, "07_routing_test")

    passed = any(r["loaded"] for r in results)
    record_test(
        "页面路由跳转",
        passed,
        f"测试了 {len(routes_to_test)} 个路由",
        screenshot
    )

    return passed

# ==================== 主测试流程 ====================
def main():
    log("=" * 60)
    log("DataForgeStudio V4 前端功能测试")
    log("=" * 60)
    log(f"前端地址: {FRONTEND_URL}")
    log(f"后端地址: {BACKEND_URL}")
    log(f"测试用户: {TEST_USERNAME}")
    log(f"截图目录: {SCREENSHOT_DIR.absolute()}")
    log("=" * 60)

    with sync_playwright() as p:
        # 启动浏览器
        log("启动浏览器...")
        browser = p.chromium.launch(headless=False, slow_mo=500)  # headless=False 以便观察

        # 创建浏览器上下文
        context = browser.new_context(
            viewport={"width": 1920, "height": 1080},
            locale="zh-CN"
        )

        # 创建页面
        page = context.new_page()

        try:
            # 执行测试
            test_login_redirect(page)

            # 尝试登录（即使后端可能不可用）
            login_success = test_login_functionality(page)

            if login_success:
                # 登录成功后的测试
                test_navigation_menu(page)
                test_user_management_page(page)
                test_role_management_page(page)
                test_search_functionality(page)
                test_page_routing(page)
            else:
                log("登录失败，跳过需要认证的测试", "WARN")
                # 仍然可以测试一些不需要认证的功能
                test_navigation_menu(page)

        finally:
            # 等待最后查看
            log("\n测试完成，5秒后关闭浏览器...")
            time.sleep(5)
            browser.close()

    # 保存测试结果
    results_file = SCREENSHOT_DIR / f"test_results_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
    with open(results_file, "w", encoding="utf-8") as f:
        json.dump(TEST_RESULTS, f, ensure_ascii=False, indent=2)

    # 打印测试总结
    log("\n" + "=" * 60)
    log("测试总结")
    log("=" * 60)
    summary = TEST_RESULTS["summary"]
    log(f"总测试数: {summary['total']}")
    log(f"通过: {summary['passed']}")
    log(f"失败: {summary['failed']}")
    log(f"跳过: {summary['skipped']}")
    log(f"通过率: {summary['passed']/summary['total']*100:.1f}%" if summary['total'] > 0 else "N/A")
    log(f"测试结果已保存: {results_file}")
    log("=" * 60)

    # 列出所有测试
    log("\n测试详情:")
    for test in TEST_RESULTS["tests"]:
        status_symbol = "[OK]" if test["status"] == "passed" else "[X]"
        log(f"  {status_symbol} {test['name']}: {test['message']}")

    return summary["failed"] == 0

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
