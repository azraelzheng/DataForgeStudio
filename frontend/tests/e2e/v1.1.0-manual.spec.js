/**
 * V1.1.0 手动功能测试脚本
 * 此脚本打开浏览器，等待用户手动登录后进行功能截图测试
 *
 * 运行方式：
 * npx playwright test tests/e2e/v1.1.0-manual.spec.js --project=chromium --headed
 */

import { test, expect } from '@playwright/test';

const BASE_URL = 'http://127.0.0.1:9999';

test.describe('V1.1.0 手动功能测试', () => {

  test('手动登录后测试 V1.1.0 功能', async ({ page }) => {
    // 打开登录页面
    await page.goto(`${BASE_URL}/login`);
    await page.waitForLoadState('networkidle');

    console.log('\n========================================');
    console.log('请在浏览器中手动完成以下操作：');
    console.log('1. 输入用户名: root');
    console.log('2. 输入密码: ADmin@123');
    console.log('3. 点击登录按钮');
    console.log('========================================\n');

    // 等待用户登录成功（检测 URL 变化）
    await page.waitForURL('**/home**', { timeout: 120000 }); // 2 分钟等待

    console.log('✓ 检测到登录成功，开始功能测试...');

    // 等待页面加载
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 截图首页
    await page.screenshot({ path: 'test-results/v1.1.0/manual-01-home.png', fullPage: true });
    console.log('✓ 首页截图完成');

    // 测试看板管理页面
    console.log('\n→ 正在测试看板管理页面...');
    await page.goto(`${BASE_URL}/dashboard`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);
    await page.screenshot({ path: 'test-results/v1.1.0/manual-02-dashboard.png', fullPage: true });
    console.log('✓ 看板管理页面截图完成');

    // 测试车间大屏配置页面
    console.log('\n→ 正在测试车间大屏配置页面...');
    await page.goto(`${BASE_URL}/display`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);
    await page.screenshot({ path: 'test-results/v1.1.0/manual-03-display-config.png', fullPage: true });
    console.log('✓ 车间大屏配置页面截图完成');

    // 测试全屏展示页面
    console.log('\n→ 正在测试全屏展示页面...');
    await page.goto(`${BASE_URL}/display/fullscreen`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);
    await page.screenshot({ path: 'test-results/v1.1.0/manual-04-fullscreen.png', fullPage: true });
    console.log('✓ 全屏展示页面截图完成');

    console.log('\n========================================');
    console.log('所有 V1.1.0 功能测试完成！');
    console.log('截图保存在: test-results/v1.1.0/');
    console.log('========================================\n');
  });

  test('仅截图登录页面', async ({ page }) => {
    await page.goto(`${BASE_URL}/login`);
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: 'test-results/v1.1.0/manual-00-login.png', fullPage: true });
    console.log('✓ 登录页面截图完成');
  });
});
