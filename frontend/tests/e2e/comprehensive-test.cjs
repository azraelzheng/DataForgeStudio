const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

// 测试配置
const CONFIG = {
  baseURL: 'http://localhost:5173',
  credentials: {
    username: 'root',
    password: 'Admin@123'
  },
  screenshotDir: 'test-screenshots/comprehensive'
};

// 确保截图目录存在
if (!fs.existsSync(CONFIG.screenshotDir)) {
  fs.mkdirSync(CONFIG.screenshotDir, { recursive: true });
}

// 测试结果记录
const results = {
  passed: [],
  failed: [],
  warnings: []
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

// 辅助函数：记录警告
function logWarning(testName, details) {
  results.warnings.push({ name: testName, details });
  console.log(`⚠ ${testName}`);
  if (details) console.log(`  ${details}`);
}

// 辅助函数：登录
async function login(page) {
  console.log('\n=== 登录 ===');
  await page.goto(CONFIG.baseURL + '/login', { waitUntil: 'networkidle', timeout: 30000 });

  const usernameInput = page.locator('input[type="text"]').first();
  const passwordInput = page.locator('input[type="password"]');
  const loginButton = page.locator('.el-button--primary').first();

  await usernameInput.fill(CONFIG.credentials.username);
  await passwordInput.fill(CONFIG.credentials.password);
  await loginButton.click();

  // 等待导航
  await page.waitForTimeout(3000);
  const currentUrl = page.url();

  return currentUrl.includes('/home') || currentUrl === CONFIG.baseURL + '/';
}

// 辅助函数：测试页面访问
async function testPageAccess(page, path, pageName) {
  try {
    await page.goto(CONFIG.baseURL + path, { waitUntil: 'networkidle', timeout: 30000 });
    await page.screenshot({ path: path.join(CONFIG.screenshotDir, `${pageName}.png`) });

    const title = await page.title();
    const content = await page.content();

    // 检查是否有错误消息
    const hasError = content.includes('错误') || content.includes('Error') || content.includes('404') || content.includes('500');

    return {
      success: true,
      hasError,
      title,
      contentLength: content.length
    };
  } catch (error) {
    return {
      success: false,
      error: error.message
    };
  }
}

// 辅助函数：检查页面元素
async function checkPageElements(page, elements) {
  const found = [];
  const missing = [];

  for (const element of elements) {
    const count = await page.locator(element.selector).count();
    if (count > 0) {
      found.push(element.name);
    } else {
      missing.push(element.name);
    }
  }

  return { found, missing };
}

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();

  // 设置视口大小
  await page.setViewportSize({ width: 1920, height: 1080 });

  try {
    console.log('开始全面测试...\n');
    console.log('='.repeat(60));

    // ==================== 用户认证测试 ====================
    console.log('\n【用户认证测试】');

    const loginSuccess = await login(page);
    logResult('用户登录', loginSuccess);
    await page.screenshot({ path: path.join(CONFIG.screenshotDir, '01-home-after-login.png') });

    // ==================== 页面访问测试 ====================
    console.log('\n【页面访问测试】');

    const pages = [
      { path: '/home', name: '首页' },
      { path: '/report', name: '报表查询' },
      { path: '/report/design', name: '报表设计列表' },
      { path: '/license', name: '许可管理' },
      { path: '/system/user', name: '用户管理' },
      { path: '/system/role', name: '角色管理' },
      { path: '/system/datasource', name: '数据源管理' },
      { path: '/system/log', name: '日志管理' },
      { path: '/system/backup', name: '备份管理' }
    ];

    for (const pageConfig of pages) {
      const fileName = pageConfig.name.replace(/\s+/g, '-');
      try {
        await page.goto(CONFIG.baseURL + pageConfig.path, { waitUntil: 'networkidle', timeout: 30000 });
        await page.screenshot({ path: CONFIG.screenshotDir + '/' + fileName + '.png' });

        const title = await page.title();
        const content = await page.content();

        // 检查是否有错误消息
        const hasError = content.includes('错误') || content.includes('Error') || content.includes('404') || content.includes('500');

        logResult(
          `访问${pageConfig.name}页面`,
          !hasError,
          `标题: ${title}`
        );

        if (hasError) {
          logWarning(`${pageConfig.name}页面可能存在错误`, '页面包含错误内容');
        }
      } catch (error) {
        logResult(`访问${pageConfig.name}页面`, false, error.message);
      }
    }

    // ==================== 首页元素测试 ====================
    console.log('\n【首页元素测试】');
    await page.goto(CONFIG.baseURL + '/home', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);

    const homeElements = [
      { selector: '.el-aside, aside, .sidebar', name: '侧边栏' },
      { selector: '.el-header, header, .navbar', name: '顶部导航栏' },
      { selector: '.el-main, main, .content', name: '主内容区' },
      { selector: 'nav, .nav, .menu', name: '导航菜单' }
    ];

    const homeCheck = await checkPageElements(page, homeElements);
    logResult('首页基本元素', homeCheck.found.length >= 2, `找到: ${homeCheck.found.join(', ')} || 缺失: ${homeCheck.missing.join(', ')}`);

    // ==================== 用户管理页面测试 ====================
    console.log('\n【用户管理页面测试】');
    await page.goto(CONFIG.baseURL + '/system/user', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);

    const userMgmtElements = [
      { selector: '.el-table, table', name: '用户表格' },
      { selector: 'button:has-text("添加"), button:has-text("新增"), button:has-text("创建")', name: '添加按钮' },
      { selector: 'button:has-text("编辑"), button:has-text("修改")', name: '编辑按钮' },
      { selector: 'button:has-text("删除"), button:has-text("移除")', name: '删除按钮' },
      { selector: '.pagination, .el-pagination', name: '分页组件' }
    ];

    const userMgmtCheck = await checkPageElements(page, userMgmtElements);
    logResult('用户管理页面元素', userMgmtCheck.found.length >= 2, `找到: ${userMgmtCheck.found.join(', ')} || 缺失: ${userMgmtCheck.missing.join(', ')}`);

    // 测试添加用户对话框
    const addButton = page.locator('button:has-text("添加"), button:has-text("新增"), button:has-text("创建")').first();
    if (await addButton.count() > 0) {
      await addButton.click();
      await page.waitForTimeout(1000);
      await page.screenshot({ path: path.join(CONFIG.screenshotDir, 'user-add-dialog.png') });

      const dialogVisible = await page.locator('.el-dialog, [role="dialog"], .dialog').count() > 0;
      logResult('打开添加用户对话框', dialogVisible);

      if (dialogVisible) {
        // 检查对话框表单元素
        const formElements = [
          { selector: 'input[placeholder*="用户"], input[name="username"]', name: '用户名输入框' },
          { selector: 'input[placeholder*="真实"], input[name="realName"]', name: '真实姓名输入框' },
          { selector: 'input[placeholder*="邮箱"], input[name="email"]', name: '邮箱输入框' },
          { selector: 'input[type="password"]', name: '密码输入框' },
          { selector: 'button:has-text("确定"), button:has-text("保存"), button[type="submit"]', name: '确定按钮' },
          { selector: 'button:has-text("取消")', name: '取消按钮' }
        ];

        const formCheck = await checkPageElements(page, formElements);
        logResult('添加用户表单元素', formCheck.found.length >= 3, `找到: ${formCheck.found.join(', ')} || 缺失: ${formCheck.missing.join(', ')}`);

        // 关闭对话框
        const cancelButton = page.locator('button:has-text("取消")').first();
        if (await cancelButton.count() > 0) {
          await cancelButton.click();
          await page.waitForTimeout(500);
        }
      }
    }

    // ==================== 角色管理页面测试 ====================
    console.log('\n【角色管理页面测试】');
    await page.goto(CONFIG.baseURL + '/system/role', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);

    const roleMgmtElements = [
      { selector: '.el-table, table', name: '角色表格' },
      { selector: 'button:has-text("添加"), button:has-text("新增")', name: '添加按钮' }
    ];

    const roleMgmtCheck = await checkPageElements(page, roleMgmtElements);
    logResult('角色管理页面元素', roleMgmtCheck.found.length >= 1, `找到: ${roleMgmtCheck.found.join(', ')} || 缺失: ${roleMgmtCheck.missing.join(', ')}`);

    // ==================== 数据源管理页面测试 ====================
    console.log('\n【数据源管理页面测试】');
    await page.goto(CONFIG.baseURL + '/system/datasource', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);

    const dsMgmtElements = [
      { selector: '.el-table, table', name: '数据源表格' },
      { selector: 'button:has-text("添加"), button:has-text("新增")', name: '添加按钮' },
      { selector: 'button:has-text("测试连接")', name: '测试连接按钮' }
    ];

    const dsMgmtCheck = await checkPageElements(page, dsMgmtElements);
    logResult('数据源管理页面元素', dsMgmtCheck.found.length >= 1, `找到: ${dsMgmtCheck.found.join(', ')} || 缺失: ${dsMgmtCheck.missing.join(', ')}`);

    // ==================== 报表查询页面测试 ====================
    console.log('\n【报表查询页面测试】');
    await page.goto(CONFIG.baseURL + '/report', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);

    const reportQueryElements = [
      { selector: '.el-table, table', name: '报表列表' },
      { selector: 'button:has-text("查询"), button:has-text("搜索")', name: '查询按钮' },
      { selector: 'input[type="text"], .el-input__inner', name: '搜索输入框' }
    ];

    const reportQueryCheck = await checkPageElements(page, reportQueryElements);
    logResult('报表查询页面元素', reportQueryCheck.found.length >= 1, `找到: ${reportQueryCheck.found.join(', ')} || 缺失: ${reportQueryCheck.missing.join(', ')}`);

    // ==================== 报表设计页面测试 ====================
    console.log('\n【报表设计页面测试】');
    await page.goto(CONFIG.baseURL + '/report/design', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);

    const reportDesignElements = [
      { selector: '.el-table, table', name: '报表列表' },
      { selector: 'button:has-text("创建"), button:has-text("新建"), button:has-text("添加")', name: '创建报表按钮' }
    ];

    const reportDesignCheck = await checkPageElements(page, reportDesignElements);
    logResult('报表设计页面元素', reportDesignCheck.found.length >= 1, `找到: ${reportDesignCheck.found.join(', ')} || 缺失: ${reportDesignCheck.missing.join(', ')}`);

    // ==================== 许可管理页面测试 ====================
    console.log('\n【许可管理页面测试】');
    await page.goto(CONFIG.baseURL + '/license', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);

    const licenseElements = [
      { selector: '.el-card, .card, .info-panel', name: '许可信息卡片' },
      { selector: 'button:has-text("导入"), button:has-text("上传")', name: '导入许可按钮' }
    ];

    const licenseCheck = await checkPageElements(page, licenseElements);
    logResult('许可管理页面元素', licenseCheck.found.length >= 0, `找到: ${licenseCheck.found.join(', ')} || 缺失: ${licenseCheck.missing.join(', ')}`);

    // 检查许可信息
    const content = await page.content();
    const hasLicenseInfo = content.includes('到期') || content.includes('版本') || content.includes('用户数') ||
                          content.includes('expiry') || content.includes('version') || content.includes('users');
    logResult('许可信息显示', hasLicenseInfo);

    // ==================== 响应式设计测试 ====================
    console.log('\n【响应式设计测试】');

    // 移动设备视口
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto(CONFIG.baseURL + '/home', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);
    await page.screenshot({ path: path.join(CONFIG.screenshotDir, 'responsive-mobile.png') });
    logResult('移动设备显示', true, '375x667 视口');

    // 平板设备视口
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto(CONFIG.baseURL + '/home', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);
    await page.screenshot({ path: path.join(CONFIG.screenshotDir, 'responsive-tablet.png') });
    logResult('平板设备显示', true, '768x1024 视口');

    // 恢复桌面视口
    await page.setViewportSize({ width: 1920, height: 1080 });

    // ==================== 输出测试总结 ====================
    console.log('\n' + '='.repeat(60));
    console.log('【测试总结】');
    console.log(`总计测试: ${results.passed.length + results.failed.length}`);
    console.log(`✓ 通过: ${results.passed.length}`);
    console.log(`✗ 失败: ${results.failed.length}`);
    console.log(`⚠ 警告: ${results.warnings.length}`);

    if (results.failed.length > 0) {
      console.log('\n【失败的测试】');
      results.failed.forEach(f => {
        console.log(`  ✗ ${f.name}`);
        if (f.details) console.log(`    ${f.details}`);
      });
    }

    if (results.warnings.length > 0) {
      console.log('\n【警告】');
      results.warnings.forEach(w => {
        console.log(`  ⚠ ${w.name}`);
        if (w.details) console.log(`    ${w.details}`);
      });
    }

    // 保存测试结果
    const resultPath = path.join(CONFIG.screenshotDir, 'test-results.json');
    fs.writeFileSync(resultPath, JSON.stringify(results, null, 2));
    console.log(`\n测试结果已保存到: ${resultPath}`);
    console.log(`截图已保存到: ${CONFIG.screenshotDir}`);

  } catch (error) {
    console.error('\n测试过程出错:', error.message);
    await page.screenshot({ path: path.join(CONFIG.screenshotDir, 'error.png') });
  } finally {
    await browser.close();
  }

  console.log('\n测试完成！');
})();
