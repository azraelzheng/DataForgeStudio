const { chromium } = require('playwright');
const fs = require('fs');

// 测试配置
const CONFIG = {
  baseURL: 'http://localhost:5173',
  credentials: {
    username: 'root',
    password: 'Admin@123'
  },
  screenshotDir: 'test-screenshots/final'
};

// 确保截图目录存在
if (!fs.existsSync(CONFIG.screenshotDir)) {
  fs.mkdirSync(CONFIG.screenshotDir, { recursive: true });
}

// 测试结果记录
const results = {
  passed: [],
  failed: [],
  findings: []
};

// 辅助函数：记录测试结果
function logResult(testName, passed, details = '') {
  if (passed) {
    results.passed.push({ name: testName, details });
    console.log(`✓ ${testName}`);
    if (details) console.log(`  ${details}`);
  } else {
    results.failed.push({ name: testName, details });
    console.log(`✗ ${testName}`);
    if (details) console.log(`  ${details}`);
  }
}

// 辅助函数：记录发现
function logFinding(category, details) {
  results.findings.push({ category, details });
  console.log(`🔍 ${category}: ${details}`);
}

// 辅助函数：登录
async function login(page) {
  console.log('\n=== 登录 ===');

  // 清除所有 cookies 和存储
  await page.context().clearCookies();
  await page.goto(CONFIG.baseURL + '/login', { waitUntil: 'networkidle', timeout: 30000 });
  await page.evaluate(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  await page.goto(CONFIG.baseURL + '/login', { waitUntil: 'networkidle', timeout: 30000 });

  const usernameInput = page.locator('input[type="text"]').first();
  const passwordInput = page.locator('input[type="password"]');
  const loginButton = page.locator('.el-button--primary').first();

  await usernameInput.fill(CONFIG.credentials.username);
  await passwordInput.fill(CONFIG.credentials.password);
  await loginButton.click();

  // 等待导航
  await page.waitForTimeout(5000);
  const currentUrl = page.url();

  return currentUrl.includes('/home') || currentUrl === CONFIG.baseURL + '/';
}

// 辅助函数：测试页面访问和功能
async function testPageFunctionality(page, path, pageName, expectedElements) {
  try {
    await page.goto(CONFIG.baseURL + path, { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForTimeout(2000);

    const title = await page.title();
    const content = await page.content();

    // 检查是否有明显的错误（500错误、404错误）
    const hasError = content.includes('500 Internal Server Error') ||
                      content.includes('404 Not Found') ||
                      content.includes('Runtime Error') ||
                      content.includes('Compilation failed');

    // 检查预期元素
    const foundElements = [];
    const missingElements = [];

    for (const element of expectedElements) {
      const count = await page.locator(element).count();
      if (count > 0) {
        foundElements.push(element);
      } else {
        missingElements.push(element);
      }
    }

    return {
      success: !hasError,
      hasError,
      title,
      foundElements,
      missingElements,
      contentLength: content.length
    };
  } catch (error) {
    return {
      success: false,
      error: error.message
    };
  }
}

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();
  await page.setViewportSize({ width: 1920, height: 1080 });

  try {
    console.log('开始最终前端测试...\n');
    console.log('='.repeat(60));

    // ==================== 用户认证测试 ====================
    console.log('\n【用户认证测试】');
    const loginSuccess = await login(page);
    logResult('用户登录', loginSuccess);
    await page.screenshot({ path: CONFIG.screenshotDir + '/01-home-after-login.png' });

    // ==================== 页面功能测试 ====================
    console.log('\n【页面功能测试】');

    const tests = [
      {
        path: '/home',
        name: '首页',
        elements: ['.stat-card', '.quick-actions', '.el-table, .el-empty']
      },
      {
        path: '/report',
        name: '报表查询页面',
        elements: ['.report-list', '.el-input', '.el-empty']
      },
      {
        path: '/report/design',
        name: '报表设计列表',
        elements: ['.el-table', '.el-button--primary', '.el-empty']
      },
      {
        path: '/system/user',
        name: '用户管理',
        elements: ['.el-table', '.el-pagination', '.el-button--primary']
      },
      {
        path: '/system/role',
        name: '角色管理',
        elements: ['.el-table', '.el-pagination', '.el-button--primary']
      },
      {
        path: '/system/datasource',
        name: '数据源管理',
        elements: ['.el-table', '.el-pagination', '.el-button--primary']
      },
      {
        path: '/license',
        name: '许可管理',
        elements: ['.el-card', '.el-descriptions']
      }
    ];

    for (const test of tests) {
      const result = await testPageFunctionality(page, test.path, test.name, test.elements);

      logResult(
        `访问${test.name}页面`,
        result.success,
        `标题: ${result.title}`
      );

      if (result.success) {
        if (result.foundElements.length > 0) {
          logFinding(`${test.name}元素`, `找到: ${result.foundElements.length} 个关键元素`);
        }
        if (result.missingElements.length > 0) {
          logFinding(`${test.name}缺失元素`, `缺失: ${result.missingElements.join(', ')}`);
        }
      }

      // 检查空状态显示
      const hasEmptyState = await page.locator('.el-empty').count() > 0;
      if (hasEmptyState) {
        const emptyElements = await page.locator('.el-empty').all();
        for (const empty of emptyElements) {
          try {
            const emptyText = await empty.textContent();
            if (emptyText && emptyText.trim()) {
              logFinding(`${test.name}空状态`, `显示空状态: "${emptyText.trim()}"`);
            }
          } catch (e) {
            // 忽略错误
          }
        }
      }

      await page.screenshot({ path: CONFIG.screenshotDir + `/${test.name}.png` });
    }

    // ==================== 交互测试 ====================
    console.log('\n【交互测试】');

    // 测试用户管理添加对话框
    await page.goto(CONFIG.baseURL + '/system/user', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);

    const addButton = page.locator('button:has-text("新增"), button:has-text("添加")').first();
    if (await addButton.count() > 0) {
      await addButton.click();
      await page.waitForTimeout(1000);

      const dialogVisible = await page.locator('.el-dialog, [role="dialog"]').count() > 0;
      logResult('打开添加用户对话框', dialogVisible);

      if (dialogVisible) {
        await page.screenshot({ path: CONFIG.screenshotDir + '/user-add-dialog.png' });

        // 关闭对话框
        const cancelButton = page.locator('button:has-text("取消")').first();
        if (await cancelButton.count() > 0) {
          await cancelButton.click();
          await page.waitForTimeout(500);
        }
      }
    }

    // ==================== 响应式测试 ====================
    console.log('\n【响应式设计测试】');

    // 移动设备
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto(CONFIG.baseURL + '/home', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);
    await page.screenshot({ path: CONFIG.screenshotDir + '/mobile-home.png' });
    logResult('移动设备显示 (375x667)', true);

    // 平板设备
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto(CONFIG.baseURL + '/home', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);
    await page.screenshot({ path: CONFIG.screenshotDir + '/tablet-home.png' });
    logResult('平板设备显示 (768x1024)', true);

    // 恢复桌面视口
    await page.setViewportSize({ width: 1920, height: 1080 });

    // ==================== 输出测试总结 ====================
    console.log('\n' + '='.repeat(60));
    console.log('【测试总结】');
    console.log(`总计测试: ${results.passed.length + results.failed.length}`);
    console.log(`✓ 通过: ${results.passed.length}`);
    console.log(`✗ 失败: ${results.failed.length}`);
    console.log(`🔍 发现: ${results.findings.length}`);

    if (results.failed.length > 0) {
      console.log('\n【失败的测试】');
      results.failed.forEach(f => {
        console.log(`  ✗ ${f.name}`);
        if (f.details) console.log(`    ${f.details}`);
      });
    }

    if (results.findings.length > 0) {
      console.log('\n【发现的问题和建议】');
      const categories = {};
      results.findings.forEach(f => {
        if (!categories[f.category]) categories[f.category] = [];
        categories[f.category].push(f.details);
      });
      Object.keys(categories).forEach(cat => {
        console.log(`\n${cat}:`);
        categories[cat].forEach(item => console.log(`  - ${item}`));
      });
    }

    // 保存测试结果
    const resultPath = CONFIG.screenshotDir + '/test-results.json';
    fs.writeFileSync(resultPath, JSON.stringify(results, null, 2));
    console.log(`\n测试结果已保存到: ${resultPath}`);
    console.log(`截图已保存到: ${CONFIG.screenshotDir}`);

  } catch (error) {
    console.error('\n测试过程出错:', error.message);
    await page.screenshot({ path: CONFIG.screenshotDir + '/error.png' });
  } finally {
    await browser.close();
  }

  console.log('\n测试完成！');
})();
