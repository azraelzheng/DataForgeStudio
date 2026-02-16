const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();
  const baseURL = 'http://localhost:5173';

  try {
    console.log('开始测试...\n');

    // 测试 1: 检查登录页面
    console.log('测试 1: 访问登录页面');
    await page.goto(baseURL + '/login', { waitUntil: 'networkidle', timeout: 30000 });
    await page.screenshot({ path: 'test-screenshots/01-login-page.png' });
    console.log('✓ 登录页面加载成功\n');

    // 检查页面内容
    const content = await page.content();
    console.log('页面 HTML 长度:', content.length);
    console.log('是否包含登录表单:', content.includes('input') || content.includes('Input'));

    // 测试 2: 尝试登录
    console.log('\n测试 2: 尝试登录');

    // 尝试多种选择器
    const selectors = {
      username: [
        'input[name="username"]',
        'input[type="text"]',
        'input[placeholder*="用户"]',
        'input[placeholder*="username"]',
        '.el-input__inner',
        '#username'
      ],
      password: [
        'input[name="password"]',
        'input[type="password"]',
        'input[placeholder*="密码"]',
        'input[placeholder*="password"]',
        '.el-input__inner[type="password"]',
        '#password'
      ],
      loginButton: [
        'button[type="submit"]',
        'button:has-text("登录")',
        '.el-button--primary',
        '.login-button',
        '#login-button'
      ]
    };

    // 查找用户名输入框
    let usernameInput = null;
    for (const selector of selectors.username) {
      try {
        const element = page.locator(selector).first();
        if (await element.count() > 0) {
          console.log(`找到用户名输入框: ${selector}`);
          usernameInput = element;
          break;
        }
      } catch (e) {
        // 继续尝试下一个选择器
      }
    }

    if (!usernameInput) {
      console.log('❌ 未找到用户名输入框');
      console.log('页面元素:', await page.locator('input').all());
    } else {
      await usernameInput.fill('root');
      console.log('✓ 输入用户名成功');
    }

    // 查找密码输入框
    let passwordInput = null;
    for (const selector of selectors.password) {
      try {
        const element = page.locator(selector);
        const count = await element.count();
        if (count > 0) {
          // 如果有多个，找最后一个（通常是密码框）
          const targetElement = count > 1 ? element.nth(count - 1) : element.first();
          console.log(`找到密码输入框: ${selector} (共${count}个)`);
          passwordInput = targetElement;
          break;
        }
      } catch (e) {
        // 继续尝试下一个选择器
      }
    }

    if (!passwordInput) {
      console.log('❌ 未找到密码输入框');
    } else {
      await passwordInput.fill('Admin@123');
      console.log('✓ 输入密码成功');
    }

    // 查找登录按钮
    let loginButton = null;
    for (const selector of selectors.loginButton) {
      try {
        const element = page.locator(selector);
        if (await element.count() > 0) {
          console.log(`找到登录按钮: ${selector}`);
          loginButton = element.first();
          break;
        }
      } catch (e) {
        // 继续尝试下一个选择器
      }
    }

    if (!loginButton) {
      console.log('❌ 未找到登录按钮');
      console.log('页面按钮:', await page.locator('button').allTextContents());
    } else {
      await page.screenshot({ path: 'test-screenshots/02-before-login.png' });
      await loginButton.click();
      console.log('✓ 点击登录按钮');

      // 等待导航
      await page.waitForTimeout(3000);
      await page.screenshot({ path: 'test-screenshots/03-after-login.png' });

      const currentUrl = page.url();
      console.log('当前 URL:', currentUrl);

      if (currentUrl.includes('/home') || currentUrl === baseURL + '/') {
        console.log('✓ 登录成功，已跳转到首页');
      } else {
        console.log('⚠ 未跳转到首页，可能登录失败');
      }
    }

    console.log('\n测试完成！');
    console.log('截图已保存到 test-screenshots 目录');

    // 保持浏览器打开 10 秒供查看
    console.log('\n浏览器将在 10 秒后关闭...');
    await page.waitForTimeout(10000);

  } catch (error) {
    console.error('测试出错:', error.message);
    await page.screenshot({ path: 'test-screenshots/error.png' });
  } finally {
    await browser.close();
  }
})();
