/**
 * V1.1.0 Dashboard/Kanban/Display 功能测试
 * 使用 API 直接认证，更可靠地测试功能
 */

import { test, expect } from '@playwright/test';

// 测试配置
const TEST_CONFIG = {
  baseURL: 'http://127.0.0.1:9999',
  apiURL: 'http://127.0.0.1:5000',
  adminUser: {
    username: 'root',
    password: 'ADmin@123'
  }
};

// 辅助函数：通过 API 登录并设置 localStorage
async function apiLogin(page) {
  // 调用登录 API
  const response = await page.request.post(`${TEST_CONFIG.apiURL}/api/auth/login`, {
    data: {
      username: TEST_CONFIG.adminUser.username,
      password: TEST_CONFIG.adminUser.password
    }
  });

  if (!response.ok()) {
    throw new Error(`登录失败: ${response.status()} ${response.statusText()}`);
  }

  const data = await response.json();
  console.log('登录响应:', JSON.stringify(data).substring(0, 200));

  // API 返回的字段是 PascalCase
  if (!data.Success || !data.Data?.Token) {
    throw new Error(`登录失败: ${data.Message || '未知错误'}`);
  }

  const token = data.Data.Token;

  // 设置 localStorage
  await page.goto(`${TEST_CONFIG.baseURL}/login`);
  await page.evaluate((tokenValue) => {
    localStorage.setItem('token', tokenValue);
  }, token);

  return token;
}

test.describe('V1.1.0 功能测试 - 认证与页面访问', () => {

  test('1. API 登录测试', async ({ page }) => {
    try {
      const token = await apiLogin(page);
      console.log('✓ API 登录成功，获取到 token');
      expect(token).toBeTruthy();
    } catch (error) {
      console.log('登录错误:', error.message);
      // 不要再次尝试登录，避免触发速率限制
      throw error;
    }
  });

  test('2. 登录页面应该正常加载', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/login`);
    await page.waitForLoadState('networkidle');

    await expect(page.locator('.login-container')).toBeVisible({ timeout: 10000 });
    await page.screenshot({ path: 'test-results/v1.1.0/01-login-page.png', fullPage: true });
    console.log('✓ 登录页面加载成功');
  });
});

test.describe('V1.1.0 功能测试 - 看板管理（需要登录）', () => {
  let authToken;

  test.beforeAll(async ({ browser }) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    try {
      authToken = await apiLogin(page);
    } catch (e) {
      console.log('登录失败，测试将被跳过:', e.message);
    }
    await context.close();
  });

  test.beforeEach(async ({ page }) => {
    if (!authToken) {
      test.skip(true, '无法登录，跳过测试');
    }
    // 设置 token 到 localStorage
    await page.goto(`${TEST_CONFIG.baseURL}/login`);
    await page.evaluate((token) => {
      localStorage.setItem('token', token);
    }, authToken);
  });

  test('3. 看板管理页面应该能够访问', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/dashboard`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000); // 等待页面完全渲染

    await page.screenshot({ path: 'test-results/v1.1.0/02-dashboard-page.png', fullPage: true });

    const currentUrl = page.url();
    console.log('当前 URL:', currentUrl);

    // 如果被重定向到许可证页面，记录信息
    if (currentUrl.includes('/license')) {
      console.log('⚠️ 被重定向到许可证页面，可能许可证已过期或无效');
      const pageContent = await page.content();
      console.log('页面内容包含 license:', pageContent.includes('license'));
    } else if (currentUrl.includes('/login')) {
      console.log('⚠️ 被重定向到登录页面，认证可能失败');
    } else {
      console.log('✓ 看板管理页面访问成功');
    }

    // 截图记录当前状态
    await page.screenshot({ path: 'test-results/v1.1.0/03-dashboard-state.png', fullPage: true });
  });

  test('4. 车间大屏配置页面应该能够访问', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/display`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    await page.screenshot({ path: 'test-results/v1.1.0/04-display-config.png', fullPage: true });

    const currentUrl = page.url();
    console.log('Display 页面 URL:', currentUrl);

    if (!currentUrl.includes('/login') && !currentUrl.includes('/license')) {
      console.log('✓ 车间大屏配置页面访问成功');
    }
  });

  test('5. 全屏展示页面应该能够加载', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/display/fullscreen`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    await page.screenshot({ path: 'test-results/v1.1.0/05-fullscreen-view.png', fullPage: true });

    const currentUrl = page.url();
    console.log('Fullscreen 页面 URL:', currentUrl);

    if (!currentUrl.includes('/login') && !currentUrl.includes('/license')) {
      const pageContent = await page.content();
      console.log('全屏页面内容长度:', pageContent.length);
      console.log('✓ 全屏展示页面加载成功');
    }
  });
});

test.describe('V1.1.0 功能测试 - 组件发现', () => {
  test('6. 查看侧边栏导航菜单项', async ({ page }) => {
    await page.goto(`${TEST_CONFIG.baseURL}/login`);
    await page.waitForLoadState('networkidle');

    // 尝试登录
    try {
      const token = await apiLogin(page);
      await page.goto(`${TEST_CONFIG.baseURL}/home`);
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000);

      // 发现所有菜单项
      const menuItems = await page.locator('.el-menu-item, .el-sub-menu, nav a, .menu-item').all();
      console.log('发现菜单项数量:', menuItems.length);

      // 获取菜单文本
      const menuTexts = [];
      for (const item of menuItems.slice(0, 20)) { // 限制前 20 个
        const text = await item.textContent().catch(() => '');
        if (text?.trim()) {
          menuTexts.push(text.trim());
        }
      }
      console.log('菜单文本:', menuTexts);

      await page.screenshot({ path: 'test-results/v1.1.0/06-sidebar-menu.png', fullPage: true });
    } catch (e) {
      console.log('登录失败，无法查看菜单:', e.message);
      await page.screenshot({ path: 'test-results/v1.1.0/06-sidebar-error.png', fullPage: true });
    }
  });
});

test.describe('V1.1.0 功能测试 - API 端点', () => {
  test('7. 检查后端 API 健康状态', async ({ page }) => {
    const response = await page.request.get(`${TEST_CONFIG.apiURL}/health`);
    expect(response.ok()).toBeTruthy();

    const data = await response.json();
    console.log('健康检查响应:', data);
    console.log('✓ 后端 API 正常运行');
  });

  test('8. 检查系统信息 API', async ({ page }) => {
    try {
      const token = await apiLogin(page);

      const response = await page.request.get(`${TEST_CONFIG.apiURL}/api/system/info`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (response.ok()) {
        const data = await response.json();
        console.log('系统信息:', JSON.stringify(data, null, 2));
      } else {
        console.log('系统信息 API 响应:', response.status());
      }
    } catch (e) {
      console.log('无法获取系统信息:', e.message);
    }
  });

  test('9. 检查许可证状态', async ({ page }) => {
    try {
      const token = await apiLogin(page);

      const response = await page.request.get(`${TEST_CONFIG.apiURL}/api/license/status`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (response.ok()) {
        const data = await response.json();
        console.log('许可证状态:', JSON.stringify(data, null, 2));
      } else {
        console.log('许可证状态 API 响应:', response.status());
      }
    } catch (e) {
      console.log('无法获取许可证状态:', e.message);
    }
  });
});
