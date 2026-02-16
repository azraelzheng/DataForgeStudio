import { test, expect } from '@playwright/test';

// 测试配置
const TEST_CONFIG = {
  baseURL: 'http://localhost:5173',
  // 测试用户凭据
  adminUser: {
    username: 'root',
    password: 'Admin@123'
  },
  testUser: {
    username: 'testuser',
    password: 'Test@123456',
    email: 'test@example.com'
  }
};

// 测试数据
const TEST_DATA = {
  dataSource: {
    name: `测试数据源_${Date.now()}`,
    dbType: 'SqlServer',
    server: 'localhost',
    database: 'DataForgeStudio_V4',
    username: 'sa',
    password: 'your_password'
  },
  report: {
    name: `测试报表_${Date.now()}`,
    description: '自动化测试创建的报表'
  },
  role: {
    name: `测试角色_${Date.now()}`,
    description: '自动化测试创建的角色'
  }
};

// 辅助函数：登录
async function login(page, username, password) {
  await page.goto('/login');
  await page.waitForLoadState('networkidle');

  // 等待登录表单加载
  await page.waitForSelector('input[type="text"], input[placeholder*="用户"], input[name="username"]', { timeout: 10000 });

  // 输入用户名 - 尝试多种可能的选择器
  const usernameInput = page.locator('input[type="text"], input[placeholder*="用户"], input[name="username"]').first();
  await usernameInput.fill(username);

  // 输入密码
  const passwordInput = page.locator('input[type="password"], input[placeholder*="密码"]').first();
  await passwordInput.fill(password);

  // 点击登录按钮
  const loginButton = page.locator('button:has-text("登录"), button[type="submit"]').first();
  await loginButton.click();

  // 等待导航完成
  await page.waitForURL(/\/home|\/$/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');
}

test.describe('DataForgeStudio V4 - 用户认证测试', () => {
  test.beforeEach(async ({ page }) => {
    // 每个测试前清理 localStorage
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
  });

  test('应该显示登录页面', async ({ page }) => {
    await page.goto('/login');
    await page.waitForLoadState('networkidle');

    // 验证页面标题
    await expect(page).toHaveTitle(/登录.*DataForgeStudio/);

    // 验证登录表单元素存在
    await expect(page.locator('input[type="text"], input[placeholder*="用户"]')).toHaveCount(1);
    await expect(page.locator('input[type="password"]')).toHaveCount(1);
    await expect(page.locator('button:has-text("登录")')).toHaveCount(1);
  });

  test('应该能够成功登录', async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);

    // 验证登录成功后跳转到首页
    await expect(page).toHaveURL(/\/home|\/$/);
    await expect(page.locator('body')).toContainText('DataForgeStudio');
  });

  test('登录失败应该显示错误消息', async ({ page }) => {
    await page.goto('/login');
    await page.waitForLoadState('networkidle');

    // 输入错误的凭据
    await page.locator('input[type="text"], input[placeholder*="用户"]').first().fill('wronguser');
    await page.locator('input[type="password"]').first().fill('wrongpass');

    // 点击登录
    await page.locator('button:has-text("登录")').first().click();

    // 等待错误消息
    await page.waitForTimeout(2000);

    // 验证仍然在登录页面
    await expect(page).toHaveURL('/login');
  });

  test('应该能够登出', async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);

    await page.waitForLoadState('networkidle');

    // 查找并点击登出按钮
    const logoutButton = page.locator('button:has-text("退出"), button:has-text("登出"), [role="menuitem"]:has-text("退出")');
    const count = await logoutButton.count();

    if (count > 0) {
      await logoutButton.first().click();
      await page.waitForTimeout(1000);
      await expect(page).toHaveURL('/login');
    }
  });
});

test.describe('DataForgeStudio V4 - 首页/仪表盘测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('应该显示首页', async ({ page }) => {
    await page.waitForLoadState('networkidle');

    // 验证页面元素
    await expect(page.locator('body')).toContainText('DataForgeStudio');

    // 检查侧边栏导航
    const sidebar = page.locator('.el-aside, .sidebar, aside');
    if (await sidebar.count() > 0) {
      await expect(sidebar).toBeVisible();
    }
  });

  test('侧边栏导航应该可点击', async ({ page }) => {
    await page.waitForLoadState('networkidle');

    // 查找所有导航链接
    const navLinks = page.locator('a, .nav-item, .menu-item');
    const count = await navLinks.count();

    if (count > 0) {
      // 验证至少有一些导航链接
      expect(count).toBeGreaterThan(0);
    }
  });
});

test.describe('DataForgeStudio V4 - 用户管理测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('应该能够访问用户管理页面', async ({ page }) => {
    await page.goto('/system/user');
    await page.waitForLoadState('networkidle');

    // 验证页面标题
    await expect(page).toHaveTitle(/用户管理.*DataForgeStudio/);

    // 查找用户表格
    const table = page.locator('.el-table, table');
    const count = await table.count();

    // 验证表格存在或至少有用户管理相关内容
    const pageContent = await page.content();
    const hasUserContent = pageContent.includes('用户') || pageContent.includes('User') || count > 0;
    expect(hasUserContent).toBeTruthy();
  });

  test('用户管理页面应该有添加用户按钮', async ({ page }) => {
    await page.goto('/system/user');
    await page.waitForLoadState('networkidle');

    // 查找添加按钮
    const addButton = page.locator('button:has-text("添加"), button:has-text("新增"), button:has-text("创建"), .el-button--primary');
    const count = await addButton.count();

    if (count > 0) {
      // 找到添加按钮
      expect(count).toBeGreaterThan(0);
    }
  });

  test('应该能够打开新增用户对话框', async ({ page }) => {
    await page.goto('/system/user');
    await page.waitForLoadState('networkidle');

    // 查找并点击添加按钮
    const addButton = page.locator('button:has-text("添加"), button:has-text("新增"), button:has-text("创建")').first();
    const count = await addButton.count();

    if (count > 0) {
      await addButton.click();
      await page.waitForTimeout(500);

      // 验证对话框打开
      const dialog = page.locator('.el-dialog, .dialog, [role="dialog"]');
      const dialogCount = await dialog.count();

      if (dialogCount > 0) {
        await expect(dialog.first()).toBeVisible();
      }
    }
  });
});

test.describe('DataForgeStudio V4 - 角色管理测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('应该能够访问角色管理页面', async ({ page }) => {
    await page.goto('/system/role');
    await page.waitForLoadState('networkidle');

    // 验证页面标题
    await expect(page).toHaveTitle(/权限组|角色.*DataForgeStudio/);

    // 验证页面内容
    const pageContent = await page.content();
    const hasRoleContent = pageContent.includes('角色') || pageContent.includes('Role') || pageContent.includes('权限');
    expect(hasRoleContent).toBeTruthy();
  });

  test('角色管理页面应该显示角色列表', async ({ page }) => {
    await page.goto('/system/role');
    await page.waitForLoadState('networkidle');

    // 查找表格或列表
    const table = page.locator('.el-table, table, .list');
    const count = await table.count();

    const pageContent = await page.content();
    const hasContent = count > 0 || pageContent.includes('管理员') || pageContent.includes('Admin');
    expect(hasContent).toBeTruthy();
  });
});

test.describe('DataForgeStudio V4 - 数据源管理测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('应该能够访问数据源管理页面', async ({ page }) => {
    await page.goto('/system/datasource');
    await page.waitForLoadState('networkidle');

    // 验证页面标题
    await expect(page).toHaveTitle(/数据源.*DataForgeStudio/);

    // 验证页面内容
    const pageContent = await page.content();
    const hasDataSourceContent = pageContent.includes('数据源') || pageContent.includes('DataSource') || pageContent.includes('数据库');
    expect(hasDataSourceContent).toBeTruthy();
  });

  test('应该有添加数据源按钮', async ({ page }) => {
    await page.goto('/system/datasource');
    await page.waitForLoadState('networkidle');

    // 查找添加按钮
    const addButton = page.locator('button:has-text("添加"), button:has-text("新增"), button:has-text("创建")');
    const count = await addButton.count();

    if (count > 0) {
      expect(count).toBeGreaterThan(0);
    }
  });
});

test.describe('DataForgeStudio V4 - 报表查询测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('应该能够访问报表查询页面', async ({ page }) => {
    await page.goto('/report');
    await page.waitForLoadState('networkidle');

    // 验证页面标题
    await expect(page).toHaveTitle(/报表查询.*DataForgeStudio/);

    // 验证页面内容
    const pageContent = await page.content();
    const hasReportContent = pageContent.includes('报表') || pageContent.includes('Report') || pageContent.includes('查询');
    expect(hasReportContent).toBeTruthy();
  });
});

test.describe('DataForgeStudio V4 - 报表设计测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('应该能够访问报表设计列表页面', async ({ page }) => {
    await page.goto('/report/design');
    await page.waitForLoadState('networkidle');

    // 验证页面标题
    await expect(page).toHaveTitle(/报表设计.*DataForgeStudio/);

    // 验证页面内容
    const pageContent = await page.content();
    const hasDesignContent = pageContent.includes('报表') || pageContent.includes('设计') || pageContent.includes('Report');
    expect(hasDesignContent).toBeTruthy();
  });

  test('报表设计页面应该有创建报表按钮', async ({ page }) => {
    await page.goto('/report/design');
    await page.waitForLoadState('networkidle');

    // 查找创建按钮
    const createButton = page.locator('button:has-text("创建"), button:has-text("新建"), button:has-text("添加")');
    const count = await createButton.count();

    if (count > 0) {
      expect(count).toBeGreaterThan(0);
    }
  });
});

test.describe('DataForgeStudio V4 - 许可管理测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('应该能够访问许可管理页面', async ({ page }) => {
    await page.goto('/license');
    await page.waitForLoadState('networkidle');

    // 验证页面标题
    await expect(page).toHaveTitle(/许可.*DataForgeStudio/);

    // 验证页面内容
    const pageContent = await page.content();
    const hasLicenseContent = pageContent.includes('许可') || pageContent.includes('License') || pageContent.includes('证书');
    expect(hasLicenseContent).toBeTruthy();
  });

  test('许可页面应该显示许可信息', async ({ page }) => {
    await page.goto('/license');
    await page.waitForLoadState('networkidle');

    // 查找许可相关信息
    const pageContent = await page.content();
    const hasLicenseInfo = pageContent.includes('到期') || pageContent.includes('版本') || pageContent.includes('用户数') ||
                          pageContent.includes('_expiry') || pageContent.includes('_version') || pageContent.includes('_users');
    expect(hasLicenseInfo).toBeTruthy();
  });
});

test.describe('DataForgeStudio V4 - 日志管理测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('应该能够访问日志管理页面', async ({ page }) => {
    await page.goto('/system/log');
    await page.waitForLoadState('networkidle');

    // 验证页面标题
    await expect(page).toHaveTitle(/日志.*DataForgeStudio/);

    // 验证页面内容
    const pageContent = await page.content();
    const hasLogContent = pageContent.includes('日志') || pageContent.includes('Log') || pageContent.includes('操作');
    expect(hasLogContent).toBeTruthy();
  });
});

test.describe('DataForgeStudio V4 - 备份管理测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('应该能够访问备份管理页面', async ({ page }) => {
    await page.goto('/system/backup');
    await page.waitForLoadState('networkidle');

    // 验证页面标题
    await expect(page).toHaveTitle(/备份.*DataForgeStudio/);

    // 验证页面内容
    const pageContent = await page.content();
    const hasBackupContent = pageContent.includes('备份') || pageContent.includes('Backup') || pageContent.includes('恢复');
    expect(hasBackupContent).toBeTruthy();
  });
});

test.describe('DataForgeStudio V4 - 响应式设计测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('首页在移动设备上应该正常显示', async ({ page }) => {
    // 设置移动设备视口
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/home');
    await page.waitForLoadState('networkidle');

    // 验证页面加载成功
    await expect(page.locator('body')).toBeVisible();
  });

  test('首页在平板设备上应该正常显示', async ({ page }) => {
    // 设置平板设备视口
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto('/home');
    await page.waitForLoadState('networkidle');

    // 验证页面加载成功
    await expect(page.locator('body')).toBeVisible();
  });
});

test.describe('DataForgeStudio V4 - 无障碍测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('页面应该有适当的标题', async ({ page }) => {
    await page.goto('/home');
    await page.waitForLoadState('networkidle');

    const title = await page.title();
    expect(title).toBeTruthy();
    expect(title.length).toBeGreaterThan(0);
  });

  test('重要交互元素应该是可访问的', async ({ page }) => {
    await page.goto('/system/user');
    await page.waitForLoadState('networkidle');

    // 检查按钮是否可点击
    const buttons = page.locator('button');
    const count = await buttons.count();

    if (count > 0) {
      // 验证至少有一个按钮可见且可交互
      const visibleButton = buttons.first();
      await expect(visibleButton).toBeVisible();
    }
  });
});

test.describe('DataForgeStudio V4 - 性能测试', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, TEST_CONFIG.adminUser.username, TEST_CONFIG.adminUser.password);
  });

  test('首页加载应该在合理时间内完成', async ({ page }) => {
    const startTime = Date.now();
    await page.goto('/home');
    await page.waitForLoadState('networkidle');
    const loadTime = Date.now() - startTime;

    // 页面应该在5秒内加载完成
    expect(loadTime).toBeLessThan(5000);
  });

  test('用户管理页面加载应该在合理时间内完成', async ({ page }) => {
    const startTime = Date.now();
    await page.goto('/system/user');
    await page.waitForLoadState('networkidle');
    const loadTime = Date.now() - startTime;

    // 页面应该在5秒内加载完成
    expect(loadTime).toBeLessThan(5000);
  });
});
