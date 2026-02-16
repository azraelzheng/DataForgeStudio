#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
DataForgeStudio V4 前端功能测试脚本 (改进版)
使用 Playwright 同步 API 测试前端应用的核心功能
"""

import os
import sys
import time
import json
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
    "tests": [],
    "issues": [],
    "summary": {"total": 0, "passed": 0, "failed": 0, "skipped": 0}
}

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

def record_test(test_name, passed, message="", screenshot=None, issue=None):
    """记录测试结果"""
    TEST_RESULTS["tests"].append({
        "name": test_name,
        "status": "passed" if passed else "failed",
        "message": message,
        "screenshot": str(screenshot) if screenshot else None,
        "timestamp": datetime.now().isoformat()
    })

    if issue and passed is False:
        TEST_RESULTS["issues"].append({
            "test": test_name,
            "issue": issue,
            "screenshot": str(screenshot) if screenshot else None
        })

    TEST_RESULTS["summary"]["total"] += 1
    if passed:
        TEST_RESULTS["summary"]["passed"] += 1
        log(f"[PASS] {test_name}")
    else:
        TEST_RESULTS["summary"]["failed"] += 1
        log(f"[FAIL] {test_name}: {message}")

# ==================== 测试函数 ====================
def check_backend_status(page):
    """检查后端服务状态"""
    log("检查后端服务状态...")
    try:
        response = page.request.get(f"{BACKEND_URL}/health", timeout=3000)
        status = {
            "available": response.ok,
            "status_code": response.status,
            "response": response.text()[:100] if response.ok else None
        }
        if response.ok:
            log("后端服务正常", "PASS")
        else:
            log(f"后端服务异常: {response.status}", "WARN")
        return status
    except Exception as e:
        log(f"后端服务不可用: {str(e)[:100]}", "WARN")
        return {"available": False, "error": str(e)}

def test_initial_page_load(page):
    """测试1: 初始页面加载和路由"""
    log("\n========== 测试1: 初始页面加载和路由 ==========")

    try:
        page.goto(FRONTEND_URL, wait_until="domcontentloaded", timeout=10000)
        time.sleep(2)

        current_url = page.url
        log(f"当前URL: {current_url}")

        # 获取页面内容
        page_content = page.content()

        # 检查关键元素
        checks = {
            "has_login_form": False,
            "has_username_input": False,
            "has_password_input": False,
            "has_login_button": False,
            "has_app_title": False,
            "is_login_page": False
        }

        # 检查是否是登录页
        checks["is_login_page"] = "/login" in current_url

        # 检查页面标题
        title = page.title()
        log(f"页面标题: {title}")
        checks["has_app_title"] = "DataForgeStudio" in title

        # 检查表单元素
        try:
            checks["has_username_input"] = len(page.query_selector_all("input[type='text'], input[placeholder*='用户']")) > 0
            checks["has_password_input"] = len(page.query_selector_all("input[type='password']")) > 0
            checks["has_login_button"] = len(page.query_selector_all("button[type='submit'], button:has-text('登'), button:has-text('Login')")) > 0
            checks["has_login_form"] = checks["has_username_input"] and checks["has_password_input"]
        except:
            pass

        screenshot = take_screenshot(page, "01_initial_load")

        passed = checks["is_login_page"] or checks["has_login_form"]
        message = f"URL: {current_url}, 登录表单: {checks['has_login_form']}"

        record_test("初始页面加载", passed, message, screenshot)

        if not passed:
            issue = "未正确重定向到登录页或登录表单未显示"
            record_test("初始页面加载", False, message, screenshot, issue)

        return {
            "passed": passed,
            "checks": checks,
            "current_url": current_url
        }
    except Exception as e:
        screenshot = take_screenshot(page, "01_load_error")
        record_test("初始页面加载", False, f"异常: {str(e)[:100]}", screenshot, str(e))
        return {"passed": False, "error": str(e)}

def test_login_with_backend(page):
    """测试2: 登录功能（需要后端）"""
    log("\n========== 测试2: 登录功能测试 ==========")

    # 检查后端状态
    backend_status = check_backend_status(page)

    if not backend_status.get("available"):
        log("后端服务不可用，跳过完整登录测试", "WARN")
        issue = "后端服务未启动或不响应 (http://localhost:5000)"
        record_test("登录功能", False, "后端服务不可用", None, issue)
        return {"passed": False, "reason": "backend_unavailable"}

    try:
        # 确保在登录页
        if "/login" not in page.url:
            page.goto(f"{FRONTEND_URL}/login", wait_until="domcontentloaded", timeout=10000)
            time.sleep(1)

        # 填写登录表单
        log(f"填写登录表单: {TEST_USERNAME}")

        # 查找输入框
        username_inputs = page.query_selector_all("input[type='text'], input:not([type])")
        password_inputs = page.query_selector_all("input[type='password']")

        if not username_inputs or not password_inputs:
            screenshot = take_screenshot(page, "02_no_form")
            record_test("登录功能", False, "未找到登录表单元素", screenshot, "登录表单元素缺失")
            return {"passed": False, "reason": "no_form"}

        # 填写表单
        username_inputs[0].fill(TEST_USERNAME)
        password_inputs[0].fill(TEST_PASSWORD)
        time.sleep(0.5)

        screenshot = take_screenshot(page, "02_form_filled")

        # 点击登录按钮
        login_buttons = page.query_selector_all("button:has-text('登'), button:has-text('Login'), button[type='submit']")

        if not login_buttons:
            screenshot = take_screenshot(page, "02_no_button")
            record_test("登录功能", False, "未找到登录按钮", screenshot, "登录按钮缺失")
            return {"passed": False, "reason": "no_button"}

        login_buttons[0].click()
        log("已点击登录按钮")

        # 等待响应
        time.sleep(5)

        # 检查结果
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

        screenshot = take_screenshot(page, "02_after_login")

        # 判断是否登录成功
        success_indicators = [
            "/home" in current_url,
            "/dashboard" in current_url.lower(),
            current_url == FRONTEND_URL + "/",
        ]

        # 检查是否有用户信息显示
        has_user_info = len(page.query_selector_all("[class*='user'], [class*='avatar'], .username")) > 0

        passed = any(success_indicators) or (has_user_info and not login_errors)

        if login_errors:
            message = f"登录失败: {'; '.join(login_errors[:2])}"
            log(f"登录错误: {message}")
            issue = f"服务器返回登录错误: {'; '.join(login_errors)}"
            record_test("登录功能", False, message, screenshot, issue)
        elif not passed:
            message = f"未能确认登录成功, URL: {current_url}"
            issue = "登录后未能正确跳转或显示用户信息"
            record_test("登录功能", False, message, screenshot, issue)
        else:
            message = f"登录成功, URL: {current_url}"
            record_test("登录功能", True, message, screenshot)

        return {
            "passed": passed,
            "current_url": current_url,
            "has_user_info": has_user_info,
            "errors": login_errors
        }

    except Exception as e:
        screenshot = take_screenshot(page, "02_login_error")
        record_test("登录功能", False, f"异常: {str(e)[:100]}", screenshot, str(e))
        return {"passed": False, "error": str(e)}

def test_navigation_and_pages(page):
    """测试3: 导航和页面访问"""
    log("\n========== 测试3: 导航和页面访问 ==========")

    results = {}

    # 测试的路由
    routes = [
        ("/home", "首页"),
        ("/system/user", "用户管理"),
        ("/system/role", "角色管理"),
        ("/report", "报表查询"),
        ("/license", "许可管理"),
    ]

    for route, name in routes:
        try:
            log(f"测试路由: {route} ({name})")
            page.goto(f"{FRONTEND_URL}{route}", wait_until="domcontentloaded", timeout=10000)
            time.sleep(2)

            current_url = page.url
            page_title = page.title()

            # 检查页面元素
            has_content = len(page.query_selector_all("*")) > 50  # 基本内容检查

            # 检查错误消息
            error_elements = page.query_selector_all(".el-message--error, [class*='error']")
            permission_errors = []
            for elem in error_elements:
                try:
                    text = elem.inner_text()
                    if "权限" in text:
                        permission_errors.append(text)
                except:
                    pass

            screenshot = take_screenshot(page, f"03_{name.replace(' ', '_')}_page")

            route_loaded = route in current_url
            has_permission_error = len(permission_errors) > 0

            result = {
                "route": route,
                "name": name,
                "loaded": route_loaded,
                "has_permission_error": has_permission_error,
                "title": page_title,
                "has_content": has_content
            }
            results[route] = result

            # 记录结果
            if has_permission_error:
                log(f"  权限不足: {permission_errors[0][:50]}", "WARN")
                record_test(f"访问{name}页面", False, "权限不足", screenshot, "需要相应权限")
            elif route_loaded:
                log(f"  页面加载成功", "PASS")
                record_test(f"访问{name}页面", True, "页面加载成功", screenshot)
            else:
                log(f"  页面可能未正确加载", "WARN")
                record_test(f"访问{name}页面", False, "页面可能未正确加载", screenshot)

        except Exception as e:
            log(f"  测试失败: {str(e)[:50]}", "ERROR")
            results[route] = {"error": str(e)}
            record_test(f"访问{name}页面", False, f"异常: {str(e)[:50]}", None, str(e))

    return results

def test_ui_components(page):
    """测试4: UI 组件测试"""
    log("\n========== 测试4: UI 组件测试 ==========")

    results = {}

    # 导航到首页
    try:
        page.goto(f"{FRONTEND_URL}/home", wait_until="domcontentloaded", timeout=10000)
        time.sleep(2)
    except:
        page.goto(FRONTEND_URL, wait_until="domcontentloaded", timeout=10000)
        time.sleep(2)

    # 检查侧边栏
    sidebar = page.query_selector(".sidebar, .el-aside, aside, [class*='sidebar'], [class*='aside']")
    results["has_sidebar"] = sidebar is not None

    # 检查导航菜单
    menu_items = page.query_selector_all(".el-menu-item, nav a, [role='menuitem']")
    results["menu_count"] = len(menu_items)

    # 检查按钮
    buttons = page.query_selector_all("button, .el-button")
    results["button_count"] = len(buttons)

    # 检查表单元素
    inputs = page.query_selector_all("input, select, textarea")
    results["input_count"] = len(inputs)

    screenshot = take_screenshot(page, "04_ui_components")

    log(f"侧边栏: {results['has_sidebar']}")
    log(f"菜单项: {results['menu_count']}")
    log(f"按钮: {results['button_count']}")
    log(f"表单元素: {results['input_count']}")

    passed = results["has_sidebar"] and results["menu_count"] > 0
    record_test(
        "UI组件显示",
        passed,
        f"侧边栏: {results['has_sidebar']}, 菜单: {results['menu_count']}",
        screenshot
    )

    return results

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
        browser = p.chromium.launch(
            headless=False,
            slow_mo=300,
            args=['--start-maximized']
        )

        # 创建浏览器上下文
        context = browser.new_context(
            viewport={"width": 1920, "height": 1080},
            locale="zh-CN"
        )

        # 创建页面
        page = context.new_page()

        # 设置默认超时
        page.set_default_timeout(10000)

        try:
            # 执行测试
            result1 = test_initial_page_load(page)
            result2 = test_login_with_backend(page)
            result3 = test_navigation_and_pages(page)
            result4 = test_ui_components(page)

        finally:
            # 等待查看
            log("\n测试完成，等待3秒...")
            time.sleep(3)
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
    if summary['total'] > 0:
        log(f"通过率: {summary['passed']/summary['total']*100:.1f}%")
    log(f"测试结果: {results_file}")
    log("=" * 60)

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
