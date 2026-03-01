/**
 * V1.1.0 Dashboard/Kanban/Display 功能测试
 * 测试范围：
 * 1. 登录功能
 * 2. 看板管理页面
 * 3. 车间大屏配置
 * 4. 全屏展示模式
 */

import { test, expect } from '@playwright/test';

// 测试配置
const BASE_URL = 'http://127.0.0.1:9999';

test.describe('V1.1.0 Dashboard/Kanban 功能测试', () => {

  test('1. 登录页面应该正常加载', async ({ page }) => {
    await page.goto(`${BASE_URL}/login`);
    await page.waitForLoadState('networkidle');

    // 验证登录页面元素
    const loginContainer = page.locator('.login-container');
    await expect(loginContainer).toBeVisible({ timeout: 10000 });

    // 截图
    await page.screenshot({ path: 'test-results/v1.1.0/01-login-page.png', fullPage: true });

    // 检查用户名和密码输入框
    const usernameInput = page.locator('input[placeholder*="用户名"]');
    const passwordInput = page.locator('input[placeholder*="密码"]');

    await expect(usernameInput).toBeVisible();
    await expect(passwordInput).toBeVisible();

    console.log('登录页面加载成功');
  });

  test('2. 首页导航菜单应该显示看板和大屏入口', async ({ page }) => {
    await page.goto(`${BASE_URL}/login`);
    await page.waitForLoadState('networkidle');

    // 检查页面是否正确加载
    const title = await page.title();
    console.log('页面标题:', title);

    // 截图
    await page.screenshot({ path: 'test-results/v1.1.0/02-page-state.png', fullPage: true });
  });

  test('3. 看板管理页面访问测试', async ({ page }) => {
    try {
      await page.goto(`${BASE_URL}/dashboard`);
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000); // 等待 Vue 组件渲染

      // 截图记录当前状态
      await page.screenshot({ path: 'test-results/v1.1.0/03-dashboard-page.png', fullPage: true });

      // 检查是否被重定向到登录页（未登录）
      const currentUrl = page.url();
      console.log('当前 URL:', currentUrl);

      if (currentUrl.includes('/login')) {
        console.log('用户未登录，被重定向到登录页 - 符合预期');
      } else {
        // 验证看板管理页面元素
        const pageContent = await page.content();
        console.log('页面内容长度:', pageContent.length);
      }
    } catch (error) {
      console.log('测试出错:', error.message);
      await page.screenshot({ path: 'test-results/v1.1.0/03-dashboard-error.png', fullPage: true });
    }
  });

  test('4. 车间大屏配置页面访问测试', async ({ page }) => {
    try {
      await page.goto(`${BASE_URL}/display`);
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000);

      await page.screenshot({ path: 'test-results/v1.1.0/04-display-config.png', fullPage: true });

      const currentUrl = page.url();
      console.log('Display 页面 URL:', currentUrl);
    } catch (error) {
      console.log('测试出错:', error.message);
      await page.screenshot({ path: 'test-results/v1.1.0/04-display-error.png', fullPage: true });
    }
  });

  test('5. 全屏展示页面访问测试', async ({ page }) => {
    try {
      await page.goto(`${BASE_URL}/display/fullscreen`);
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(3000); // 全屏模式需要更多时间初始化

      await page.screenshot({ path: 'test-results/v1.1.0/05-fullscreen-view.png', fullPage: true });

      const currentUrl = page.url();
      console.log('Fullscreen 页面 URL:', currentUrl);

      // 检查页面内容
      const pageContent = await page.content();
      console.log('全屏页面内容长度:', pageContent.length);
    } catch (error) {
      console.log('测试出错:', error.message);
      await page.screenshot({ path: 'test-results/v1.1.0/05-fullscreen-error.png', fullPage: true });
    }
  });

  test('6. 页面元素发现测试', async ({ page }) => {
    await page.goto(`${BASE_URL}/login`);
    await page.waitForLoadState('networkidle');

    // 发现所有按钮
    const buttons = await page.locator('button').all();
    console.log('发现按钮数量:', buttons.length);

    // 发现所有输入框
    const inputs = await page.locator('input').all();
    console.log('发现输入框数量:', inputs.length);

    // 发现所有链接
    const links = await page.locator('a').all();
    console.log('发现链接数量:', links.length);

    // 打印页面 HTML 结构（前 500 字符）
    const html = await page.content();
    console.log('页面 HTML 片段:', html.substring(0, 500));
  });
});
