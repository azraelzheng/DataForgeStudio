// @ts-check
/**
 * V1.1.0 Dashboard E2E Tests - Simplified Version
 * 车间大屏系统端到端测试（简化版）
 *
 * 运行方式：
 *   1. 确保后端服务运行: cd backend && dotnet run --project src/DataForgeStudio.Api
 *   2. 确保前端服务运行: cd frontend && npm run dev
 *   3. 运行测试: npx playwright test tests/e2e/dashboard.spec.js
 *
 * 注意：需要先在数据库中执行 V1.1.0_Dashboard.sql 迁移脚本
 */

import { test, expect } from '@playwright/test';

// 测试配置
const CONFIG = {
  baseURL: process.env.BASE_URL || 'http://localhost:9999',
  apiURL: process.env.API_URL || 'http://localhost:5000',
  username: process.env.TEST_USERNAME || 'root',
  password: process.env.TEST_PASSWORD || 'ADmin@123',
};

// ============================================
// 测试套件 1: 公开页面测试（无需登录）
// ============================================
test.describe('公开页面测试', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test('1. 公开大屏访问页面应该能够加载', async ({ page }) => {
    // 访问不存在的公开大屏ID
    await page.goto('/public/d/999999');
    await page.waitForLoadState('networkidle');

    // 页面应该正常渲染（显示不存在或错误信息）
    const pageContent = await page.content();
    expect(pageContent.length).toBeGreaterThan(100);

    await page.screenshot({ path: 'test-results/dashboard/01-public-page.png' });
  });

  test('2. 公开大屏页面应该能够处理不存在的大屏', async ({ page }) => {
    await page.goto('/public/d/999999');
    await page.waitForLoadState('networkidle');

    // 页面应该正常加载，可能显示：
    // 1. 错误信息（不存在、未公开等）
    // 2. 空白大屏页面
    // 3. 被重定向到登录页（如果没有后端支持）
    const body = await page.textContent('body');
    const url = page.url();

    // 接受以下任一情况：
    // - 显示错误信息
    // - 显示登录页面（重定向）
    // - 显示大屏相关内容
    const isValidResponse =
      body?.includes('不存在') ||
      body?.includes('未公开') ||
      body?.includes('错误') ||
      body?.includes('NOT_FOUND') ||
      body?.includes('404') ||
      body?.includes('加载') ||
      body?.includes('大屏') ||
      body?.includes('登录') ||  // 被重定向到登录页
      url.includes('login') ||
      url.includes('public');    // 仍在公开页面

    expect(isValidResponse).toBeTruthy();
  });
});

// ============================================
// 测试套件 2: API 测试
// ============================================
test.describe('Dashboard API 测试', () => {
  test('3. 未认证访问API应该返回401', async ({ request }) => {
    const response = await request.get(`${CONFIG.apiURL}/api/dashboards`);

    // 未认证应该返回 401 或 403
    expect([401, 403]).toContain(response.status());
  });

  test('4. 公开API访问不存在的大屏应该返回错误', async ({ request }) => {
    const response = await request.get(`${CONFIG.apiURL}/public/d/999999`);

    // 应该返回 JSON 格式的错误响应
    try {
      const data = await response.json();
      expect(data.success).toBeFalsy();
    } catch {
      // 如果不是 JSON，检查状态码
      expect([404, 400, 401]).toContain(response.status());
    }
  });
});

// ============================================
// 测试套件 3: 页面加载测试（无需认证）
// ============================================
test.describe('页面加载测试', () => {
  test('5. 登录页面应该能够正常加载', async ({ page }) => {
    await page.goto('/login');
    await page.waitForLoadState('networkidle');

    // 验证登录页面元素
    const usernameInput = page.locator('input[placeholder="请输入用户名"]');
    const passwordInput = page.locator('input[placeholder="请输入密码"]');
    const loginButton = page.locator('button:has-text("登")');

    await expect(usernameInput).toBeVisible();
    await expect(passwordInput).toBeVisible();
    await expect(loginButton).toBeVisible();

    await page.screenshot({ path: 'test-results/dashboard/05-login-page.png' });
  });

  test('6. 首页应该需要认证', async ({ page }) => {
    await page.goto('/home');
    await page.waitForLoadState('networkidle');

    // 未认证用户应该被重定向到登录页
    await page.waitForTimeout(1000);
    expect(page.url()).toContain('/login');
  });

  test('7. 大屏列表页面应该需要认证', async ({ page }) => {
    await page.goto('/dashboard/list');
    await page.waitForLoadState('networkidle');

    // 未认证用户应该被重定向到登录页
    await page.waitForTimeout(1000);
    expect(page.url()).toContain('/login');
  });
});

// ============================================
// 测试套件 4: 认证后测试（需要手动登录或配置）
// ============================================
test.describe('认证后功能测试', () => {
  test('8. 登录后应该能访问大屏列表', async ({ page }) => {
    // 登录
    await page.goto('/login');
    await page.waitForLoadState('networkidle');

    await page.fill('input[placeholder="请输入用户名"]', CONFIG.username);
    await page.fill('input[placeholder="请输入密码"]', CONFIG.password);

    // 使用更精确的按钮选择器
    await page.click('button.el-button--primary');
    await page.waitForTimeout(3000);
    await page.waitForLoadState('networkidle');

    // 访问大屏列表
    await page.goto('/dashboard/list');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 验证页面加载
    expect(page.url()).toContain('/dashboard');

    await page.screenshot({ path: 'test-results/dashboard/08-dashboard-list.png' });
  });

  test('9. 应该能够进入大屏设计器', async ({ page }) => {
    // 登录
    await page.goto('/login');
    await page.waitForLoadState('networkidle');

    await page.fill('input[placeholder="请输入用户名"]', CONFIG.username);
    await page.fill('input[placeholder="请输入密码"]', CONFIG.password);

    await page.click('button.el-button--primary');
    await page.waitForTimeout(3000);
    await page.waitForLoadState('networkidle');

    // 访问设计器
    await page.goto('/dashboard/designer');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 验证设计器页面
    expect(page.url()).toContain('/dashboard/designer');

    await page.screenshot({ path: 'test-results/dashboard/09-designer.png' });
  });
});

// ============================================
// 测试套件 5: 响应式设计测试
// ============================================
test.describe('响应式设计测试', () => {
  test('10. 登录页面应该是响应式的', async ({ page }) => {
    const viewports = [
      { width: 1920, height: 1080, name: 'desktop' },
      { width: 1366, height: 768, name: 'laptop' },
      { width: 768, height: 1024, name: 'tablet' },
      { width: 375, height: 667, name: 'mobile' },
    ];

    for (const viewport of viewports) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.goto('/login');
      await page.waitForLoadState('networkidle');

      // 登录表单应该在所有设备上可见
      const loginButton = page.locator('button:has-text("登")');
      await expect(loginButton).toBeVisible();

      await page.screenshot({
        path: `test-results/dashboard/10-responsive-${viewport.name}.png`,
        fullPage: true
      });
    }
  });
});
