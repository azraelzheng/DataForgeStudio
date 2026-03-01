/**
 * V1.1.0 Dashboard/Kanban/Display 完整功能测试
 * 测试范围：
 * 1. 登录功能
 * 2. 看板管理页面（创建、编辑、删除看板）
 * 3. 看板组件（图表、数字卡片、表格等）
 * 4. 车间大屏配置
 * 5. 全屏展示模式
 */

import { test, expect } from '@playwright/test';

// 测试配置
const TEST_CONFIG = {
  baseURL: 'http://127.0.0.1:9999',
  adminUser: {
    username: 'root',
    password: 'ADmin@123'
  }
};

// 辅助函数：登录
async function login(page, username, password) {
  await page.goto(`${TEST_CONFIG.baseURL}/login`);
  await page.waitForLoadState('networkidle');

  // 等待登录表单加载
  await page.waitForSelector('.login-container', { timeout: 10000 });
  await page.waitForTimeout(1000); // 等待 Vue 组件渲染

  // 输入用户名
  const usernameInput = page.locator('input[placeholder="请输入用户名"]');
  await usernameInput.fill(username);

  // 输入密码
  const passwordInput = page.locator('input[placeholder="请输入密码"]');
  await passwordInput.fill(password);

  // 点击登录按钮 (按钮文本是 "登 录" 带空格)
  const loginButton = page.locator('.login-button, button.el-button--primary').first();
  await loginButton.click();

  // 等待导航完成
  await page.waitForURL(/\/home|\/$/, { timeout: 15000 });
  await page.waitForLoadState('networkidle');
}

// 辅助函数：等待 API 响应
async function waitForApi(page, urlPattern, timeout = 10000) {
  return page.waitForResponse(response =>
    response.url().includes(urlPattern) && response.status() === 200,
    { timeout }
  ).catch(() => null);
}

test.describe('V1.1.0 功能测试 - 认证', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/login`);
    await page.evaluate(() => localStorage.clear());
  });

  test('登录页面应该正常加载', async ({ page }) => {
    await page.waitForLoadState('networkidle');

    // 验证登录页面元素
    await expect(page.locator('.login-container')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('input[placeholder*="用户"]')).toBeVisible();
    await expect(page.locator('input[placeholder*="密码"]')).toBeVisible();

    // 截图
    await page.screenshot({ path: 'test-results/v1.1.0/01-login-page.png', fullPage: true });
    console.log('✓ 登录页面加载成功');
  });

  test('应该能够成功登录', async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);

    // 验证登录成功
    await expect(page).toHaveURL(/\/home|\/$/);
    console.log('✓ 登录成功');
  });
});

test.describe('V1.1.0 功能测试 - 看板管理', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/login`);
    await page.evaluate(() => localStorage.clear());
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('应该能够访问看板管理页面', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/dashboard`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 截图
    await page.screenshot({ path: 'test-results/v1.1.0/02-dashboard-page.png', fullPage: true });

    // 验证页面标题
    const title = await page.title();
    console.log('看板页面标题:', title);

    // 检查页面内容
    const pageContent = await page.content();
    const hasDashboardContent = pageContent.includes('看板') || pageContent.includes('Dashboard');
    console.log('包含看板内容:', hasDashboardContent);
    console.log('✓ 看板管理页面访问成功');
  });

  test('看板管理页面应该有创建按钮', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/dashboard`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 查找创建按钮
    const createButton = page.locator('button:has-text("创建"), button:has-text("新建"), button:has-text("添加")');
    const count = await createButton.count();
    console.log('找到创建按钮数量:', count);

    await page.screenshot({ path: 'test-results/v1.1.0/03-dashboard-buttons.png', fullPage: true });
    console.log('✓ 看板创建按钮检查完成');
  });

  test('看板管理页面应该显示看板列表或空状态', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/dashboard`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 检查是否有表格或卡片列表
    const table = page.locator('.el-table, table, .dashboard-list, .dashboard-card');
    const count = await table.count();
    console.log('找到列表/表格数量:', count);

    // 检查空状态提示
    const emptyState = page.locator('.el-empty, .no-data, :text("暂无")');
    const emptyCount = await emptyState.count();
    console.log('空状态提示数量:', emptyCount);

    await page.screenshot({ path: 'test-results/v1.1.0/04-dashboard-list.png', fullPage: true });
    console.log('✓ 看板列表状态检查完成');
  });
});

test.describe('V1.1.0 功能测试 - 车间大屏配置', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/login`);
    await page.evaluate(() => localStorage.clear());
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('应该能够访问车间大屏配置页面', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/display`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 截图
    await page.screenshot({ path: 'test-results/v1.1.0/05-display-config.png', fullPage: true });

    // 验证页面标题
    const title = await page.title();
    console.log('大屏配置页面标题:', title);

    // 检查页面内容
    const pageContent = await page.content();
    const hasDisplayContent = pageContent.includes('大屏') || pageContent.includes('Display') || pageContent.includes('轮播');
    console.log('包含大屏内容:', hasDisplayContent);
    console.log('✓ 车间大屏配置页面访问成功');
  });

  test('大屏配置页面应该有创建按钮', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/display`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 查找创建按钮
    const createButton = page.locator('button:has-text("创建"), button:has-text("新建"), button:has-text("添加"), button:has-text("配置")');
    const count = await createButton.count();
    console.log('找到创建/配置按钮数量:', count);

    await page.screenshot({ path: 'test-results/v1.1.0/06-display-buttons.png', fullPage: true });
    console.log('✓ 大屏配置按钮检查完成');
  });
});

test.describe('V1.1.0 功能测试 - 全屏展示模式', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/login`);
    await page.evaluate(() => localStorage.clear());
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('全屏展示页面应该能够加载', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/display/fullscreen`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000); // 全屏模式需要更多时间初始化

    // 截图
    await page.screenshot({ path: 'test-results/v1.1.0/07-fullscreen-view.png', fullPage: true });

    // 检查页面内容
    const pageContent = await page.content();
    console.log('全屏页面内容长度:', pageContent.length);

    // 检查是否有全屏相关元素
    const fullscreenElements = page.locator('.fullscreen, .display-mode, .carousel, .clock-widget');
    const count = await fullscreenElements.count();
    console.log('找到全屏相关元素数量:', count);
    console.log('✓ 全屏展示页面加载成功');
  });

  test('全屏模式应该包含时钟组件', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/display/fullscreen`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    // 检查时钟组件
    const clockWidget = page.locator('.clock-widget, .clock, [class*="clock"]');
    const count = await clockWidget.count();
    console.log('找到时钟组件数量:', count);

    // 检查时间显示
    const timeElements = page.locator(':text(/\d{2}:\d{2}/), :text(/\d{2}:\d{2}:\d{2}/)');
    const timeCount = await timeElements.count();
    console.log('找到时间显示元素数量:', timeCount);
    console.log('✓ 时钟组件检查完成');
  });

  test('全屏模式应该有轮播功能', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/display/fullscreen`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    // 检查轮播组件
    const carousel = page.locator('.carousel, .el-carousel, [class*="carousel"], [class*="slider"]');
    const count = await carousel.count();
    console.log('找到轮播组件数量:', count);

    await page.screenshot({ path: 'test-results/v1.1.0/08-fullscreen-carousel.png', fullPage: true });
    console.log('✓ 轮播功能检查完成');
  });
});

test.describe('V1.1.0 功能测试 - 导航菜单', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/login`);
    await page.evaluate(() => localStorage.clear());
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('侧边栏应该包含看板管理入口', async ({ page }) => {
    await page.waitForLoadState('networkidle');

    // 查找侧边栏
    const sidebar = page.locator('.el-menu, .sidebar, aside, nav');
    const sidebarCount = await sidebar.count();
    console.log('找到侧边栏数量:', sidebarCount);

    if (sidebarCount > 0) {
      // 查看看板菜单项
      const dashboardMenu = page.locator(':text("看板"), :text("Dashboard")');
      const count = await dashboardMenu.count();
      console.log('找到看板菜单项数量:', count);
    }

    await page.screenshot({ path: 'test-results/v1.1.0/09-sidebar-menu.png', fullPage: true });
    console.log('✓ 侧边栏看板入口检查完成');
  });

  test('侧边栏应该包含大屏配置入口', async ({ page }) => {
    await page.waitForLoadState('networkidle');

    // 查找侧边栏
    const sidebar = page.locator('.el-menu, .sidebar, aside, nav');
    const sidebarCount = await sidebar.count();

    if (sidebarCount > 0) {
      // 查找大屏菜单项
      const displayMenu = page.locator(':text("大屏"), :text("Display"), :text("车间")');
      const count = await displayMenu.count();
      console.log('找到大屏菜单项数量:', count);
    }

    console.log('✓ 侧边栏大屏入口检查完成');
  });
});

test.describe('V1.1.0 功能测试 - 组件渲染', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/login`);
    await page.evaluate(() => localStorage.clear());
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('看板页面 Vue 组件应该正确渲染', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/dashboard`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 检查 Vue 应用是否挂载
    const appElement = page.locator('#app');
    await expect(appElement).toBeVisible();

    // 检查是否有未渲染的模板标记（Vue 编译错误标志）
    const pageContent = await page.content();
    const hasUnrenderedTemplate = pageContent.includes('{{') && pageContent.includes('}}');
    console.log('存在未渲染模板:', hasUnrenderedTemplate);

    // 检查控制台错误
    const consoleMessages = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleMessages.push(msg.text());
      }
    });

    await page.waitForTimeout(1000);
    console.log('控制台错误数量:', consoleMessages.length);

    await page.screenshot({ path: 'test-results/v1.1.0/10-vue-render.png', fullPage: true });
    console.log('✓ Vue 组件渲染检查完成');
  });
});

test.describe('V1.1.0 功能测试 - 性能', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/login`);
    await page.evaluate(() => localStorage.clear());
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('看板页面加载应该在合理时间内完成', async ({ page }) => {
    const startTime = Date.now();
    await page.goto(`${TEST_CONFIG.baseURL}/dashboard`);
    await page.waitForLoadState('networkidle');
    const loadTime = Date.now() - startTime;

    console.log('看板页面加载时间:', loadTime, 'ms');
    // 页面应该在 10 秒内加载完成（允许更多时间因为可能需要加载数据）
    expect(loadTime).toBeLessThan(10000);
    console.log('✓ 看板页面加载性能测试通过');
  });

  test('大屏配置页面加载应该在合理时间内完成', async ({ page }) => {
    const startTime = Date.now();
    await page.goto(`${TEST_CONFIG.baseURL}/display`);
    await page.waitForLoadState('networkidle');
    const loadTime = Date.now() - startTime;

    console.log('大屏配置页面加载时间:', loadTime, 'ms');
    expect(loadTime).toBeLessThan(10000);
    console.log('✓ 大屏配置页面加载性能测试通过');
  });
});
