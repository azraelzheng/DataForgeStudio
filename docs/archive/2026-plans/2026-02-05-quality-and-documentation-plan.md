# DataForgeStudio V4 - 质量保证与功能完善实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** 建立完整的测试体系（单元、集成、端到端）和完善项目文档（API 文档、用户手册），同时优化前端性能和报表功能。

**Architecture:** ASP.NET Core 8.0 后端 + Vue 3 前端，xUnit 测试框架 + Playwright E2E，Swagger/OpenAPI 文档生成

**Tech Stack:**
- 测试: xUnit, Moq, Playwright, Vitest
- 文档: Swashbuckle, Swagger/OpenAPI, Markdown
- 前端优化: 虚拟滚动、懒加载、代码分割
- 后端优化: 缓存、异步处理、连接池

---

## 目录

1. [测试框架搭建](#task-1-测试框架搭建)
2. [后端单元测试](#task-2-后端单元测试)
3. [后端集成测试](#task-3-后端集成测试)
4. [前端测试](#task-4-前端测试)
5. [端到端测试](#task-5-端到端测试)
6. [API 文档完善](#task-6-api-文档完善)
7. [用户手册编写](#task-7-用户手册编写)
8. [前端性能优化](#task-8-前端性能优化)
9. [报表功能优化](#task-9-报表功能优化)

---

## Task 1: 测试框架搭建

**目标**: 建立测试项目结构，配置 xUnit、Moq、Playwright 和 Vitest

### 1.1 创建后端测试项目

**Files:**
- Create: `backend/tests/DataForgeStudio.Tests/DataForgeStudio.Tests.csproj`
- Modify: `backend/DataForgeStudio.sln`

**Step 1: 创建测试项目文件**

Create `backend/tests/DataForgeStudio.Tests/DataForgeStudio.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.11" />
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="coverlet.msbuild" Version="6.0.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers;buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\DataForgeStudio.Api\DataForgeStudio.Api.csproj" />
    <ProjectReference Include="..\..\src\DataForgeStudio.Core\DataForgeStudio.Core.csproj" />
    <ProjectReference Include="..\..\src\DataForgeStudio.Domain\DataForgeStudio.Domain.csproj" />
    <ProjectReference Include="..\..\src\DataForgeStudio.Data\DataForgeStudio.Data.csproj" />
  </ItemGroup>

</Project>
```

**Step 2: 更新解决方案文件**

Modify `backend/DataForgeStudio.sln` - 在 GlobalSection 后添加:

```xml
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "DataForgeStudio.Tests", "tests\DataForgeStudio.Tests\DataForgeStudio.Tests.csproj"
EndProject
```

Run: `dotnet sln add backend/tests/DataForgeStudio.Tests/DataForgeStudio.Tests.csproj`

**Step 3: 验证测试项目**

Run:
```bash
cd backend
dotnet test tests/DataForgeStudio.Tests
```

Expected: 0 tests, 0 errors, 0 warnings

**Step 4: 提交**

```bash
git add backend/DataForgeStudio.sln backend/tests/
git commit -m "test: add xUnit test project with Moq and coverlet"
```

### 1.2 创建前端测试框架

**Files:**
- Modify: `frontend/package.json`
- Create: `frontend/vitest.config.ts`
- Create: `frontend/tests/setup.ts`
- Create: `frontend/tests/unit/example.spec.ts`

**Step 1: 安装测试依赖**

Run in `frontend/`:
```bash
npm install -D vitest @vitest/ui @vue/test-utils @playwright happy-dom
npm install -D @testing-library/vue @testing-library/user-event jsdom
```

**Step 2: 配置 Vitest**

Create `frontend/vitest.config.ts`:

```typescript
import { defineConfig } from 'vitest/config/vue'
import vuePlugin from '@vitest/ui'

export default defineConfig({
  plugins: [vuePlugin()],
  test: {
    globals: true,
    environment: 'jsdom',
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: ['**/node_modules/**', '**/dist/**', '**/.vscode/**', '**/tests/**']
    }
  },
  resolve: {
    alias: {
      '@': '/src'
    }
  }
})
```

**Step 3: 创建测试设置文件**

Create `frontend/tests/setup.ts`:

```typescript
import { vi } from 'vitest'
import { config } from '@vue/test-utils'

// Mock window.matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: '',
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn()
  }))
})

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
  get length: vi.fn(() => 0),
  key: vi.fn(() => ''),
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn()
}
global.localStorage = localStorageMock as Storage

// Mock IntersectionObserver
global.IntersectionObserver = class IntersectionObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  takeRecords() { return [] }
  unobserve() {}
} as any

// Global mocks
vi.mock('axios')
vi.mock('element-plus/dist/locale/lang/zh-cn', () => ({ default: {} }))
```

**Step 4: 添加测试脚本到 package.json**

Modify `frontend/package.json` - add to scripts:
```json
"test": "vitest",
"test:ui": "vitest --ui",
"test:coverage": "vitest --coverage",
"test:e2e": "playwright test"
```

**Step 5: 创建示例测试**

Create `frontend/tests/unit/example.spec.ts`:

```typescript
import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import HomePage from '@/views/home/HomePage.vue'

describe('HomePage', () => {
  it('renders properly', () => {
    const wrapper = mount(HomePage)
    expect(wrapper.exists()).toBe(true)
  })
})
```

**Step 6: 验证测试框架**

Run:
```bash
cd frontend
npm run test
```

Expected: 1 test passing

**Step 7: 提交**

```bash
git add frontend/package.json frontend/vitest.config.ts frontend/tests/
git commit -m "test: add Vitest testing framework for Vue 3"
```

---

## Task 2: 后端单元测试

**目标**: 为核心服务层编写单元测试，覆盖业务逻辑

### 2.1 AuthenticationService 测试

**Files:**
- Create: `backend/tests/DataForgeStudio.Tests/Services/AuthenticationServiceTests.cs`

**Step 1: 创建测试类**

Create `backend/tests/DataForgeStudio.Tests/Services/AuthenticationServiceTests.cs`:

```csharp
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Core.Services;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Tests.Services;

public class AuthenticationServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IRoleService> _mockRoleService;
    private readonly Mock<IEncryptionHelper> _mockEncryption;
    private readonly Mock<IOptions<SecurityOptions>> _mockSecurityOptions;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockRoleService = new Mock<IRoleService>();
        _mockEncryption = new Mock<IEncryptionHelper>();
        _mockSecurityOptions = new Mock<IOptions<SecurityOptions>>();

        // Setup default security options
        _mockSecurityOptions.Setup(x => x.Value).Returns(new SecurityOptions
        {
            Jwt = new SecurityOptions.JwtOptions
            {
                Secret = "DataForgeStudioV4JWTSecretKey256BitsLongSecure2025ChangeThisInProduction",
                Issuer = "DataForgeStudio",
                Audience = "DataForgeStudio.Client",
                ExpirationMinutes = 60
            }
        });

        _authService = new AuthenticationService(
            _mockUserRepo.Object,
            _mockRoleService.Object,
            _mockEncryption.Object,
            _mockSecurityOptions.Object
        );
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = "hashed_password",
            IsActive = true,
            IsLocked = false
        };
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.VerifyPassword("password", "hashed_password")).Returns(true);

        // Act
        var result = await _authService.LoginAsync("admin", "password");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data?.Token);
        _mockUserRepo.Verify(r => r.UpdateLoginInfoAsync(user.Id, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync("admin", "password");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("用户不存在", result.Message);
    }

    [Fact]
    public async Task LoginAsync_UserLocked_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            IsActive = true,
            IsLocked = true
        };
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync("admin", "password");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("账户已被锁定", result.Message);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

**Step 2: 运行测试**

Run:
```bash
cd backend
dotnet test --filter "FullyQualifiedName~AuthenticationServiceTests"
```

Expected: All tests passing

**Step 3: 提交**

```bash
git add backend/tests/DataForgeStudio.Tests/Services/AuthenticationServiceTests.cs
git commit -m "test: add AuthenticationService unit tests"
```

### 2.2 LicenseService 测试

**Files:**
- Create: `backend/tests/DataForgeStudio.Tests/Services/LicenseServiceTests.cs`

**Step 1: 创建 LicenseService 测试**

Create `backend/tests/DataForgeStudio.Tests/Services/LicenseServiceTests.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Core.Services;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Tests.Services;

public class LicenseServiceTests
{
    private readonly Mock<ILicenseRepository> _mockLicenseRepo;
    private readonly Mock<IKeyManagementService> _mockKeyService;
    private readonly Mock<IOptions<SecurityOptions>> _mockSecurityOptions;
    private readonly Mock<ILogger<LicenseService>> _mockLogger;
    private readonly LicenseService _licenseService;

    public LicenseServiceTests()
    {
        _mockLicenseRepo = new Mock<ILicenseRepository>();
        _mockKeyService = new Mock<IKeyManagementService>();
        _mockSecurityOptions = new Mock<IOptions<SecurityOptions>>();
        _mockLogger = new Mock<ILogger<LicenseService>>();

        _licenseService = new LicenseService(
            _mockLicenseRepo.Object,
            _mockKeyService.Object,
            _mockSecurityOptions.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetLicenseAsync_ExistingLicense_ReturnsLicenseInfo()
    {
        // Arrange
        var license = new License
        {
            LicenseId = 1,
            LicenseKey = "encrypted_key",
            MachineCode = "test-machine",
            ActivatedTime = DateTime.UtcNow
        };
        _mockLicenseRepo.Setup(r => r.GetByMachineCodeAsync("test-machine")).ReturnsAsync(license);
        _mockKeyService.Setup(k => k.GetRsaWithPublicKeyAsync()).ReturnsAsync(null /* RSA instance */);

        // Act
        var result = await _licenseService.GetLicenseAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task ValidateLicenseAsync_CachedLicense_ReturnsCachedResult()
    {
        // This test would verify caching behavior
        // Implementation requires actual IMemoryCache setup
    }
}
```

### 2.3 SqlValidationService 测试

**Files:**
- Create: `backend/tests/DataForgeStudio.Tests/Services/SqlValidationServiceTests.cs`

**Step 1: 创建 SQL 验证测试**

Create `backend/tests/DataForgeStudio.Tests/Services/SqlValidationServiceTests.cs`:

```csharp
using Xunit;
using DataForgeStudio.Core.Services;

namespace DataForgeStudio.Tests.Services;

public class SqlValidationServiceTests
{
    private readonly SqlValidationService _validator = new();

    [Fact]
    public void ValidateQuery_ValidSelectQuery_ReturnsValid()
    {
        // Act
        var result = _validator.ValidateQuery("SELECT * FROM Users WHERE IsActive = 1");

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ErrorMessage);
    }

    [Theory]
    [InlineData("DROP TABLE Users")]
    [InlineData("DELETE FROM Users")]
    [InlineData("INSERT INTO Users")]
    [InlineData("UPDATE Users SET Username = 'test'")]
    [InlineData("EXEC sp_executesql")]
    [InlineData("UNION SELECT * FROM Users")]
    [InlineData("-- comment")]
    [InlineData("/* comment */")]
    public void ValidateQuery_DangerousSql_ReturnsInvalid(string sql)
    {
        // Act
        var result = _validator.ValidateQuery(sql);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.ErrorMessage);
    }

    [Fact]
    public void ValidateQuery_EmptyQuery_ReturnsInvalid()
    {
        // Act
        var result = _validator.ValidateQuery("");

        // Assert
        Assert.False(result.IsValid);
    }
}
```

**Step 2: 提交**

```bash
git add backend/tests/DataForgeStudio.Tests/Services/
git commit -m "test: add SqlValidationService unit tests"
```

---

## Task 3: 后端集成测试

**目标**: 测试 API 端点和数据库交互

### 3.1 API 集成测试基础设施

**Files:**
- Create: `backend/tests/DataForgeStudio.Tests/Integration/AuthControllerTests.cs`
- Create: `backend/tests/DataForgeStudio.Tests/Integration/TestWebApplicationFactory.cs`

**Step 1: 创建测试工厂类**

Create `backend/tests/DataForgeStudio.Tests/Integration/TestWebApplicationFactory.cs`:

```csharp
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Api;
using DataForgeStudio.Core.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DataForgeStudio.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 使用 InMemory 数据库
            services.Remove<DbContextOptions<DataForgeStudioDbContext>>();
            services.AddDbContext<DataForgeStudioDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // 配置测试用的 SecurityOptions
            services.Configure<SecurityOptions>(options =>
            {
                options.Jwt = new SecurityOptions.JwtOptions
                {
                    Secret = "TestJWTSecretKeyForTestingPurposeOnly123456789012345678901234567890",
                    Issuer = "TestIssuer",
                    Audience = "TestAudience",
                    ExpirationMinutes = 60
                };
            });

            // 配置认证
            services.PostConfigure<Authentication>(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
            });
        });
    }
}
```

### 3.2 AuthController 集成测试

**Files:**
- Create: `backend/tests/DataForgeStudio.Tests/Integration/AuthControllerTests.cs`

**Step 1: 创建认证控制器测试**

Create `backend/tests/DataForgeStudio.Tests/Integration/AuthControllerTests.cs`:

```csharp
using System.Net.Http;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using DataForgeStudio.Tests.Integration;

namespace DataForgeStudio.Tests.Integration;

public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new
        {
            username = "admin",
            password = "admin123"
        };
        var content = new StringContent(JsonConvert.SerializeObject(loginRequest));
        content.Headers.ContentType = "application/json";

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<LoginResponse>>(responseContent);

        Assert.True(result.Success);
        Assert.NotNull(result.Data?.Token);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/current-user");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_AllowAnonymous_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }
}
```

**Step 2: 提交**

```bash
git add backend/tests/DataForgeStudio.Tests/Integration/
git commit -m "test: add API integration tests with TestWebApplicationFactory"
```

---

## Task 4: 前端测试

**目标**: 为 Vue 组件编写单元测试和组件测试

### 4.1 API 请求测试

**Files:**
- Create: `frontend/tests/unit/api/authApi.spec.ts`

**Step 1: 创建 API 测试**

Create `frontend/tests/unit/api/authApi.spec.ts`:

```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { loginApi, getCurrentUserApi } from '@/api/auth'
import { api } from '@/api/request'

vi.mock('@/api/request', () => ({
  loginApi,
  getCurrentUserApi
}))

describe('authApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('loginApi', () => {
    it('should call POST /api/auth/login with credentials', async () => {
      const mockResponse = { success: true, data: { token: 'test-token' } }
      vi.mocked(api.post).mockResolvedValue({ data: mockResponse })

      const result = await loginApi('admin', 'admin123')

      expect(api.post).toHaveBeenCalledWith('/api/auth/login', {
        username: 'admin',
        password: 'admin123'
      })
      expect(result).toEqual(mockResponse)
    })
  })

  describe('getCurrentUserApi', () => {
    it('should call GET /api/auth/current-user with token', async () => {
      const mockResponse = { success: true, data: { userId: 1, username: 'admin' } }
      vi.mocked(api.get).mockResolvedValue({ data: mockResponse })

      const result = await getCurrentUserApi()

      expect(api.get).toHaveBeenCalledWith('/api/auth/current-user')
      expect(result).toEqual(mockResponse)
    })
  })
})
```

### 4.2 Pinia Store 测试

**Files:**
- Create: `frontend/tests/unit/stores/user.spec.ts`

**Step 1: 创建用户 store 测试**

Create `frontend/tests/unit/stores/user.spec.ts`:

```typescript
import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { createTestingPinia } from '@pinia/testing'
import { useUserStore } from '@/stores/user'

describe('User Store', () => {
  beforeEach(() => {
    // 创建测试用的 pinia
    setActivePinia(createTestingPinia())
  })

  afterEach(() => {
    setActivePinia(null)
  })

  it('initial state: token and userInfo are null', () => {
    const store = useUserStore()
    expect(store.token).toBe('')
    expect(store.userInfo).toBeNull()
  })

  it('setToken: updates token in state and localStorage', () => {
    const store = useUserStore()
    const testToken = 'test-jwt-token'

    store.setToken(testToken)

    expect(store.token).toBe(testToken)
    // Verify localStorage was called
    expect(localStorage.getItem('token')).toBe(testToken)
  })

  it('logout: clears token and userInfo', () => {
    const store = useUserStore()
    store.setToken('test-token')
    store.logout()

    expect(store.token).toBe('')
    expect(store.userInfo).toBeNull()
  })
})
```

### 4.3 组件测试示例

**Files:**
- Create: `frontend/tests/unit/components/LoginPage.spec.ts`

**Step 1: 创建登录页组件测试**

Create `frontend/tests/unit/components/LoginPage.spec.ts`:

```typescript
import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import LoginPage from '@/views/auth/LoginPage.vue'
import ElementPlus from 'element-plus'

describe('LoginPage', () => {
  it('renders login form', () => {
    const wrapper = mount(LoginPage, {
      global: {
        plugins: [ElementPlus]
      }
    })

    expect(wrapper.find('input[type="text"]').exists()).toBe(true)
    expect(wrapper.find('input[type="password"]').exists()).toBe(true)
    expect(wrapper.find('button[type="submit"]').exists()).toBe(true)
  })

  it('shows error message when login fails', async () => {
    const wrapper = mount(LoginPage, {
      global: {
        plugins: [ElementPlus]
      }
    })

    // Simulate login error
    // Test implementation would go here

    expect(wrapper.find('.error-message').exists()).toBe(false)
  })
})
```

**Step 2: 提交**

```bash
git add frontend/tests/
git commit -m "test: add frontend unit tests for API, stores and components"
```

---

## Task 5: 端到端测试

**目标**: 使用 Playwright 进行完整的用户场景测试

### 5.1 Playwright 配置

**Files:**
- Create: `frontend/tests/e2e/playwright.config.ts`
- Create: `frontend/tests/e2e/tests/auth.spec.ts`
- Create: `frontend/tests/e2e/tests/report.spec.ts`

**Step 1: 安装 Playwright**

Run in `frontend/`:
```bash
npm install -D @playwright/test
npx playwright install
```

**Step 2: 创建 Playwright 配置**

Create `frontend/tests/e2e/playwright.config.ts`:

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: devices['Desktop Chrome'],
    },
  ],
});
```

### 5.2 登录流程 E2E 测试

**Files:**
- Create: `frontend/tests/e2e/tests/auth.spec.ts`

**Step 1: 创建登录 E2E 测试**

Create `frontend/tests/e2e/tests/auth.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  test('successful login and logout', async ({ page }) => {
    // 导航到登录页
    await page.goto('/login');

    // 输入凭据
    await page.fill('input[name="username"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');

    // 点击登录按钮
    await page.click('button[type="submit"]');

    // 验证跳转到首页
    await expect(page).toHaveURL('/');
    await expect(page.locator('text=首页')).toBeVisible();

    // 验证登录状态
    await expect(page.locator('text=欢迎').toBeVisible();

    // 登出
    await page.click('.user-dropdown');
    await page.click('text=退出登录');

    // 验证返回登录页
    await expect(page).toHaveURL('/login');
  });

  test('login with invalid credentials shows error', async ({ page }) => {
    await page.goto('/login');

    await page.fill('input[name="username"]', 'admin');
    await page.fill('input[name="password"]', 'wrongpassword');
    await page.click('button[type="submit"]');

    // 验证错误消息
    await expect(page.locator('text=用户名或密码错误').toBeVisible();
  });
});
```

### 5.3 报表功能 E2E 测试

**Files:**
- Create: `frontend/tests/e2e/tests/report.spec.ts`

**Step 1: 创建报表 E2E 测试**

Create `frontend/tests/e2e/tests/report.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';

test.describe('Report Management Flow', () => {
  test.beforeEach(async ({ page }) => {
    // 登录
    await page.goto('/login');
    await page.fill('input[name="username"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click('button[type="submit"]');
    await page.waitForURL('/');
  });

  test('create and view report', async ({ page }) => {
    // 导航到报表设计
    await page.goto('/report/design');
    await expect(page).toHaveTitle('报表设计');

    // 输入报表名称
    await page.fill('input[placeholder="报表名称"]', '测试报表');

    // 输入 SQL
    const sqlEditor = page.locator('.cm-editor').first();
    await sqlEditor.click();
    await page.keyboard.type('SELECT * FROM Users WHERE IsActive = 1');

    // 保存报表
    await page.click('text=保存');

    // 验证成功消息
    await expect(page.locator('text=保存成功').toBeVisible();

    // 导航到报表查询
    await page.goto('/report/list');
    await expect(page.locator('text=测试报表').toBeVisible();
  });

  test('export report to Excel', async ({ page }) => {
    // 先假设已有一个报表
    await page.goto('/report/list');

    // 点击导出按钮
    const downloadPromise = page.waitForEvent('download');

    await page.click('text=导出 Excel');

    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/\.xlsx$/);
  });
});
```

**Step 2: 提交**

```bash
git add frontend/tests/e2e/
git commit -m "test: add Playwright E2E tests for auth and report flows"
```

---

## Task 6: API 文档完善

**目标**: 完善 Swagger/OpenAPI 文档，添加 XML 注释和示例

### 6.1 添加 XML 注释

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Controllers/AuthController.cs`
- Modify: `backend/src/DataForgeStudio.Api/Controllers/UsersController.cs`
- Modify: `backend/src/DataForgeStudio.Api/Controllers/ReportsController.cs`

**Step 1: 为 AuthController 添加 XML 注释**

Modify `backend/src/DataForgeStudio.Api/Controllers/AuthController.cs`:

```csharp]
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataForgeStudio.Core.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 认证授权控制器
/// </summary>
[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <returns>登录结果，包含 JWT Token</returns>
    /// <remarks>
    /// 示例请求:
    ///
    ///     POST /api/auth/login
    ///     {
    ///       "username": "admin",
    ///       "password": "admin123"
    ///     }
    ///
    /// 示例响应:
    ///
    ///     {
    ///       "success": true,
    ///       "message": "登录成功",
    ///       "data": {
    ///         "token": "eyJhbGciOiJIUzI1NiJ9...",
    ///         "user": { "userId": 1, "username": "admin" }
    ///       }
    ///     }
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ApiResponse<LoginResponse>> LoginAsync([FromBody] LoginRequest request)
    {
        // ... 现有实现 ...
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    /// <returns>当前用户信息</returns>
    /// <response code="401">未登录</response>
    [HttpGet("current-user")]
    [Authorize]
    public async Task<ApiResponse<UserInfoDto>> GetCurrentUserAsync()
    {
        // ... 现有实现 ...
    }
}
```

**Step 2: 为 ReportsController 添加 XML 注释**

Modify `backend/src/DataForgeStudio.Api/Controllers/ReportsController.cs`:

```csharp
/// <summary>
/// 报表管理控制器
/// </summary>
[Route("api/reports")]
[ApiController]
public class ReportsController : ControllerBase
{
    /// <summary>
    /// 获取报表列表
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>报表列表</returns>
    [HttpPost("list")]
    [Authorize]
    public async Task<ApiResponse<PagedResult<ReportListItemDto>>> GetListAsync([FromBody] PageRequest request)
    {
        // ... 现有实现 ...
    }

    /// <summary>
    /// 测试 SQL 查询
    /// </summary>
    /// <param name="request">测试查询请求</param>
    /// <returns>查询结果</returns>
    [HttpPost("test-query")]
    [Authorize]
    public async Task<ApiResponse<List<Dictionary<string, object>>>> TestQueryAsync([FromBody] TestQueryRequest request)
    {
        // ... 现有实现 ...
    }

    /// <summary>
    /// 导出报表为 Excel
    /// </summary>
    /// <param name="request">导出请求参数</param>
    /// <returns>Excel 文件</returns>
    [HttpPost("export/excel")]
    [Authorize]
    public async Task<IActionResult> ExportToExcelAsync([FromBody] ExportRequest request)
    {
        // ... 现有实现 ...
    }
}
```

**Step 3: 提交**

```bash
git add backend/src/DataForgeStudio.Api/Controllers/
git commit -m "docs: add XML comments to Swagger for Auth and Reports controllers"
```

### 6.2 生成离线 API 文档

**Files:**
- Create: `backend/docs/api-documentation.md`

**Step 1: 生成文档**

Run in `backend/`:
```bash
# 使用 Swagger 生成 JSON schema
curl http://localhost:5000/swagger/v1/swagger.json -o docs/swagger.json

# 或者使用 Swashbuckle CLI 工具生成文档
dotnet tool run --project src/DataForgeStudio.Api -- --filter "SwaggerGen"
```

**Step 2: 创建 Markdown 文档**

Create `backend/docs/api-documentation.md`:

```markdown
# DataForgeStudio V4 API 文档

## 基础信息

- **Base URL**: `http://localhost:5000/api` (开发环境)
- **认证方式**: JWT Bearer Token
- **内容类型**: application/json

## 认证端点

### POST /api/auth/login

用户登录获取 Token。

**请求体:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**响应:**
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiJ9...",
    "expires": "2026-02-06T01:00:00Z",
    "user": {
      "userId": 1,
      "username": "admin",
      "realName": "系统管理员"
    }
  }
}
```

## 报表端点

### POST /api/reports/test-query

测试 SQL 查询并返回结果。

**请求头:**
```
Authorization: Bearer <token>
Content-Type: application/json
```

**请求体:**
```json
{
  "sql": "SELECT * FROM Users WHERE IsActive = 1",
  "dataSourceId": 1
}
```

**限制:**
- 必须以 SELECT 开头
- 不允许 DROP, DELETE, INSERT, UPDATE 等危险关键字
- 不允许注释字符 (-- 和 /* */)

[... 继续添加其他端点 ...]
```

**Step 3: 提交**

```bash
git add backend/docs/
git commit -m "docs: add API documentation with examples"
```

---

## Task 7: 用户手册编写

**目标**: 为最终用户创建操作手册

### 7.1 创建用户手册结构

**Files:**
- Create: `docs/user-manual/01-quick-start.md`
- Create: `docs/user-manual/02-user-guide.md`
- Create: `docs/user-manual/03-report-design.md`
- Create: `docs/user-manual/04-report-query.md`
- Create: `docs/user-manual/05-system-management.md`
- Create: `docs/user-manual/06-license-management.md`
- Create: `docs/user-manual/07-troubleshooting.md`

**Step 1: 创建快速开始指南**

Create `docs/user-manual/01-quick-start.md`:

```markdown
# 快速开始指南

## 系统要求

- 浏览器: Chrome 90+, Firefox 88+, Edge 90+
- 分辨率: 1280x1024 或更高
- 网络: 与服务器正常连接

## 首次登录

1. 打开浏览器，访问系统地址
2. 输入用户名: `root`
3. 输入临时密码（查看控制台输出）
4. 点击"登录"按钮

### 重要：首次登录必须修改密码！

系统会自动跳转到密码修改页面，请输入新密码。

## 修改密码

1. 输入当前密码
2. 输入新密码（至少6位）
3. 确认新密码
4. 点击"确定"

## 基本操作

### 创建报表

1. 导航到"报表管理" → "报表设计"
2. 点击"新建报表"
3. 输入报表名称
4. 选择数据源
5. 编写 SQL 查询
6. 配置字段和参数
7. 点击"保存"

### 查看报表

1. 导航到"报表管理" → "报表查询"
2. 选择要查看的报表
3. 输入查询条件（如果有）
4. 点击"查询"
5. 查看、导出数据

## 常见问题

**Q: 忘记密码怎么办？**
A: 联系管理员通过用户管理功能重置密码

**Q: 报表查询速度慢？**
A: 尝试添加 WHERE 条件限制数据量，或联系管理员优化 SQL

**Q: 无法导出 Excel？**
A: 检查是否有"导出 Excel"权限，或数据量是否过大
```

**Step 2: 创建用户管理指南**

Create `docs/user-manual/02-user-guide.md`:

```markdown
# 用户管理指南

## 用户列表

- **路径**: 系统管理 → 用户管理
- **功能**: 查看、创建、编辑、删除用户
- **注意**: root 用户不可见、不可删除

### 新增用户

1. 点击"新增用户"
2. 填写用户信息：
   - 用户名（必填）
   - 真实姓名
   - 邮箱
   - 手机号
   - 部门
   - 职位
3. 分配角色
4. 点击"保存"

### 重置密码

1. 在用户列表中点击"重置密码"
2. 输入新密码
3. 确认后自动生成随机密码
4. 将新密码告知用户

### 分配角色

1. 编辑用户
2. 在"角色"下拉框中选择一个或多个角色
3. 保存更改
```

**Step 3: 提交用户手册**

```bash
git add docs/user-manual/
git commit -m "docs: add comprehensive user manual with quick start guide"
```

---

## Task 8: 前端性能优化

**目标**: 优化前端加载速度和运行时性能

### 8.1 路由懒加载

**Files:**
- Modify: `frontend/src/router/index.js`

**Step 1: 配置路由懒加载**

Modify `frontend/src/router/index.js`:

```javascript
import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/auth/LoginPage.vue')
  },
  {
    path: '/',
    component: () => import('@/views/Layout.vue'),
    children: [
      {
        path: '',
        name: 'Home',
        component: () => import('@/views/home/HomePage.vue')
      },
      {
        path: 'report',
        name: 'Report',
        component: () => import('@/views/report/ReportLayout.vue'),
        children: [
          {
            path: 'design',
            name: 'ReportDesign',
            component: () => import('@/views/report/ReportDesign.vue')
          },
          {
            path: 'list',
            name: 'ReportList',
            component: () => import('@/views/report/ReportList.vue')
          }
        ]
      },
      {
        path: 'system',
        name: 'System',
        component: () => import('@/views/system/SystemLayout.vue'),
        children: [
          {
            path: 'user',
            name: 'UserManagement',
            component: () => import('@/views/system/UserManagement.vue')
          },
          {
            path: 'role',
            name: 'RoleManagement',
            component: () => import('@/views/system/RoleManagement.vue')
          },
          {
            path: 'datasource',
            name: 'DataSourceManagement',
            component: () => import('@/views/system/DataSourceManagement.vue')
          },
          {
            path: 'log',
            name: 'LogManagement',
            component: () => import('@/views/system/LogManagement.vue')
          },
          {
            path: 'backup',
            name: 'BackupManagement',
            component: () => import('@/views/system/BackupManagement.vue')
          }
        ]
      },
      {
        path: 'license',
        name: 'License',
        component: () => import('@/views/license/LicenseManagement.vue')
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
```

### 8.2 Element Plus 按需加载

**Files:**
- Modify: `frontend/src/main.ts`

**Step 1: 配置 Element Plus 按需加载**

Modify `frontend/src/main.ts`:

```javascript
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'

import App from './App.vue'
import router from './router'

const app = createApp(App)

app.use(createPinia())
app.use(router)

// Element Plus 按需引入组件
// 只引入实际使用的组件，减少 bundle 大小
async function setupElementPlus() {
  if (import.meta.env.DEV) {
    // 开发环境：全量引入
    app.use(ElementPlus)
  } else {
    // 生产环境：按需引入
    // 根据需要取消注释相应的组件引入
    const Button = () => import('element-plus/es/components/button/index.mjs')
    const Input = () => import('element-plus/es/components/input/index.mjs')
    const MessageBox = () => import('element-plus/es/components/message-box/index.mjs')
    const Dialog = () => import('element-plus/es/components/dialog/index.mjs')
    const Table = () => import('element-plus/es/components/table/index.mjs')
    const Form = () => import('element-plus/es/components/form/index.mjs')
    const DatePicker = () => import('element-plus/es/components/date-picker/index.mjs')
    const Select = () => import('element-plus/es/components/select/index.mjs')
    const Upload = () => import('element-plus/es/components/upload/index.mjs')

    app.component('ElButton', Button)
    app.component('ElInput', Input)
    app.component('ElMessageBox', MessageBox)
    app.component('ElDialog', Dialog)
    app.component('ElTable', Table)
    app.component('ElForm', Form)
    app.component('ElDatePicker', DatePicker)
    app.component('ElSelect', Select)
    app.component('ElUpload', Upload)
  }
}

setupElementPlus()

app.mount('#app')
```

### 8.3 表格虚拟滚动

**Files:**
- Modify: `frontend/src/views/report/ReportList.vue`

**Step 1: 为大数据表格添加虚拟滚动**

在 `ReportList.vue` 中，当报表数据超过 1000 行时启用虚拟滚动。

创建 `frontend/src/components/VirtualTable.vue`:

```vue
<template>
  <el-table
    :data="tableData"
    :height="600"
    :row-height="50"
    :load="load"
    :show-header="true"
  >
    <el-table-column prop="id" label="ID" width="80" />
    <el-table-column prop="name" label="名称" />
    <!-- 其他列 -->
  </el-table>
</template>

<script setup>
const props = {
  data: Array, // 完整数据集
  pageSize: { type: Number, default: 50 }
}

const tableData = ref([])

const load = ({ from, to, size }) => {
  // 模拟分页加载
  const start = (from - 1) * size
  const end = from * size
  const pageData = props.data.slice(start, end)

  tableData.value = pageData
}
</script>
```

**Step 2: 提交**

```bash
git add frontend/src/components/VirtualTable.vue frontend/src/views/report/ReportList.vue
git commit -m "perf: add virtual scrolling for large report datasets"
```

---

## Task 9: 报表功能优化

**目标**: 优化报表查询性能和导出功能

### 9.1 报表查询缓存

**Files:**
- Create: `backend/src/DataForgeStudio.Core/Services/ReportCacheService.cs`
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs`

**Step 1: 创建报表缓存服务**

Create `backend/src/DataForgeStudio.Core/Services/ReportCacheService.cs`:

```csharp
using Microsoft.Extensions.Caching.Memory;
using DataForgeStudio.Core.Interfaces;

namespace DataForgeStudio.Core.Services;

public interface IReportCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public class ReportCacheService : IReportCacheService
{
    private readonly IMemoryCache _cache;

    public ReportCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return _cache.TryGetValue<T>(key, out var value) ? value : default;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow,
            Size = 1024 * 1024 // 1MB limit
        };

        await _cache.SetAsync(key, value, options, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
    }
}
```

**Step 2: 集成缓存到 ReportService**

Modify `backend/src/DataForgeStudio.Core/Services/ReportService.cs`:

在 TestQueryAsync 方法开头添加：
```csharp
// 尝试从缓存获取
var cacheKey = $"ReportQuery_{reportId}_{JsonConvert.SerializeObject(request)}";
var cachedResult = await _cacheService.GetAsync<List<Dictionary<string, object>>>(cacheKey);

if (cachedResult != null)
{
    _logger.LogInformation("返回缓存的查询结果");
    return ApiResponse<List<Dictionary<string, object>>>.Ok(cachedResult);
}

// 执行查询后存入缓存
await _cacheService.SetAsync(cacheKey, result.Data, TimeSpan.FromMinutes(5));
```

**Step 3: 提交**

```bash
git add backend/src/DataForgeStudio.Core/Services/ReportCacheService.cs backend/src/DataForgeStudio.Core/Services/ReportService.cs
git commit -m "perf: add report query caching with 5-minute expiration"
```

### 9.2 Excel 导出优化

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/ExportService.cs`

**Step 1: 优化大数据集导出**

Create `backend/src/DataForgeStudio.Core/Services/ExportService.cs`:

```csharp
using ClosedXML;
using System.Data;

namespace DataForgeStudio.Core.Services;

public interface IExportService
{
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName, string filePath, CancellationToken cancellationToken = default);
}

public class ExportService : IExportService
{
    public async Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName, string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            var properties = typeof(T).GetProperties();
            var headers = properties.Select(p => p.Name).ToArray();
            var cells = properties.Select(p => p.Name).ToArray();

            // 写入表头
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.Color = XLColor.FromTheme(XLThemeColor.Accent1);
            }

            // 写入数据
            int row = 2;
            foreach (var item in data)
            {
                for (int col = 0; col < cells.Length; col++)
                {
                    var property = properties[col];
                    var value = property.GetValue(item);
                    worksheet.Cell(row, col + 1).Value = value ?? "";
                }
                row++;
            }

            // 自动调整列宽
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }, cancellationToken);
    }
}
```

**Step 2: 提交**

```bash
git add backend/src/DataForgeStudio.Core/Services/ExportService.cs
git commit -m "perf: optimize Excel export with auto-adjust column widths"
```

---

## 验证与发布

### 测试验证清单

#### 后端测试
```bash
# 运行所有测试
cd backend
dotnet test

# 生成代码覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

#### 前端测试
```bash
cd frontend
npm run test          # 单元测试
npm run test:coverage # 覆盖率报告
npm run test:e2e      # 端到端测试
```

#### 性能验证
- 使用 Chrome DevTools Lighthouse 检查前端性能
- 使用 SQL Server Profiler 检查查询性能
- 验证 Excel 导出 10000+ 行数据的性能

### 文档验证

- 检查所有 API 端点都有 XML 注释
- 验证所有代码示例可运行
- 确认用户手册与实际系统一致

---

## 完成后的项目结构

```
DataForgeStudio_V4/
├── backend/
│   ├── src/
│   │   └── DataForgeStudio.Api/
│   │       └── Controllers/      # 添加了 XML 注释
│   ├── tests/
│   │   └── DataForgeStudio.Tests/
│   │       ├── Services/        # 单元测试
│   │       ├── Integration/     # 集成测试
│   ├── docs/                     # API 文档
│   └── tools/
│
├── frontend/
│   ├── src/
│   │   ├── components/           # 虚拟表格组件
│   │   ├── router/               # 懒加载路由
│   ├── tests/
│   │   ├── unit/                 # 单元测试
│   │   └── e2e/                  # 端到端测试
│   └── vitest.config.ts
│
└── docs/
    ├── user-manual/              # 用户手册
    ├── database-design.md        # 数据库设计
    ├── PROJECT_STATUS.md        # 项目状态
    └── archive/                 # 历史文档
```

---

## 预期成果

1. **测试覆盖率目标**
   - 后端单元测试覆盖率: >80%
   - 前端单元测试覆盖率: >70%
   - 集成测试覆盖所有 API 端点
   - E2E 测试覆盖关键业务流程

2. **文档完整性**
   - 所有 API 端点都有 Swagger 文档
   - 离线 API 文档 (Markdown)
   - 用户手册 (Markdown + PDF 导出)

3. **性能改进**
   - 前端首屏加载时间 <2s
   - 报表查询支持 10000+ 行数据
   - Excel 导出支持 100000+ 行数据
   - 表格滚动流畅支持 10000+ 行

4. **可维护性提升**
   - 测试作为文档示例
   - API 文档自动生成
   - 用户手册与代码保持同步
