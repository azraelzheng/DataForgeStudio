# 许可证系统 Dashboard 功能支持 - 实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为许可证系统添加 Dashboard 数量限制和功能权限控制

**Architecture:** 在现有许可证验证框架中添加 MaxDashboards 字段和 CheckDashboardLimitAsync 方法，在 DashboardService 中调用许可证检查，**Tech Stack:** ASP.NET Core 8.0, Entity Framework Core, Vue 3

---

## Task 1: 更新 LicenseData 数据模型

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/DTO/LicenseData.cs`

**Step 1: 添加 MaxDashboards 属性**
```csharp
/// <summary>
/// 最大大屏数量（0 表示无限制）
/// </summary>
public int MaxDashboards { get; set; }
```

**Step 2: 验证编译通过**
Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**
```bash
git add backend/src/DataForgeStudio.Core/DTO/LicenseData.cs
git commit -m "feat(license): add MaxDashboards field to LicenseData"
```
---

## Task 2: 更新 LicenseInfoDto
**Files:**
- Modify: `backend/src/DataForgeStudio.Core/DTO/LicenseInfoDto.cs`
**Step 1: 添加 MaxDashboards 属性**
```csharp
/// <summary>
/// 最大大屏数量
/// </summary>
public int MaxDashboards { get; set; }
```

**Step 2: 验证编译通过**
Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**
```bash
git add backend/src/DataForgeStudio.Core/DTO/LicenseInfoDto.cs
git commit -m "feat(license): add MaxDashboards to LicenseInfoDto"
```
---

## Task 3: 更新 LicenseUsageStatsDto
**Files:**
- Modify: `backend/src/DataForgeStudio.Core/DTO/LicenseUsageStatsDto.cs`

**Step 1: 添加 CurrentDashboards 属性**
```csharp
/// <summary>
/// 当前大屏数量
/// </summary>
public int CurrentDashboards { get; set; }
```

**Step 2: 验证编译通过**
Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**
```bash
git add backend/src/DataForgeStudio.Core/DTO/LicenseUsageStatsDto.cs
git commit -m "feat(license): add CurrentDashboards to LicenseUsageStatsDto"
```
---

## Task 4: 更新 ILicenseService 接口
**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Interfaces/ILicenseService.cs`

**Step 1: 添加 CheckDashboardLimitAsync 方法**
在 `CheckDataSourceLimitAsync()` 方法后添加：
```csharp
/// <summary>
/// 检查是否可以创建新的大屏
/// </summary>
/// <returns>如果可以创建返回成功响应，否则返回失败响应并包含限制信息</returns>
Task<ApiResponse> CheckDashboardLimitAsync();
```

**Step 2: 验证编译通过**
Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded (接口未实现会有警告)

**Step 3: Commit**
```bash
git add backend/src/DataForgeStudio.Core/Interfaces/ILicenseService.cs
git commit -m "feat(license): add CheckDashboardLimitAsync to interface"
```
---

## Task 5: 更新 LicenseService 实现（核心）
**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/LicenseService.cs`

**Step 1: 更新 MapToLicenseInfoDto 映射**
在 `MapToLicenseInfoDto` 方法中添加：
```csharp
MaxDashboards = licenseData.MaxDashboards,
```

**Step 2: 更新签名验证 JSON**
在 `ValidateLicenseAsync` 和 `ActivateLicenseAsync` 方法的签名验证 JSON 中添加：
```csharp
MaxDashboards = licenseData.MaxDashboards,
```

**Step 3: 更新 GetUsageStatsAsync**
在统计中添加大屏计数：
```csharp
// 统计大屏数量
var currentDashboards = await _context.Dashboards.CountAsync();

var stats = new LicenseUsageStatsDto
{
    CurrentUsers = currentUsers,
    CurrentReports = currentReports,
    CurrentDataSources = currentDataSources,
    CurrentDashboards = currentDashboards  // 新增
};
```

**Step 4: 更新试用许可证**
在 `GenerateTrialLicenseAsync` 方法中添加：
```csharp
MaxDashboards = 3,
Features = new List<string> { "报表设计", "报表查询", "数据源管理", "大屏设计", "大屏展示", "全屏模式" },
```

**Step 5: 实现 CheckDashboardLimitAsync**
在 `CheckDataSourceLimitAsync` 方法后添加：
```csharp
/// <summary>
/// 检查是否可以创建新的大屏
/// </summary>
public async Task<ApiResponse> CheckDashboardLimitAsync()
{
    try
    {
        // 验证许可证是否有效
        var validationResult = await ValidateLicenseAsync();
        if (!validationResult.Success || validationResult.Data == null || !validationResult.Data.Valid)
        {
            return ApiResponse.Fail(validationResult.Data?.Message ?? "许可证无效", "LICENSE_INVALID");
        }

        var licenseInfo = validationResult.Data.LicenseInfo;
        if (licenseInfo == null)
        {
            return ApiResponse.Fail("无法获取许可证信息", "LICENSE_INFO_MISSING");
        }

        // 如果 MaxDashboards 为 0，表示无限制
        if (licenseInfo.MaxDashboards == 0)
        {
            return ApiResponse.Ok();
        }

        // 统计当前大屏数量
        var currentDashboards = await _context.Dashboards.CountAsync();

        if (currentDashboards >= licenseInfo.MaxDashboards)
        {
            return ApiResponse.Fail(
                $"已达到许可证大屏数量限制（当前: {currentDashboards}，最大: {licenseInfo.MaxDashboards}），无法创建新大屏",
                "DASHBOARD_LIMIT_EXCEEDED");
        }

        return ApiResponse.Ok();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "检查大屏数量限制失败");
        return ApiResponse.Fail($"检查大屏限制失败: {ex.Message}", "CHECK_LIMIT_FAILED");
    }
}
```

**Step 6: 注入 ILicenseService**
在 DashboardService 中需要注入 ILicenseService（在 Task 6 中处理）

**Step 7: 验证编译通过**
Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 8: Commit**
```bash
git add backend/src/DataForgeStudio.Core/Services/LicenseService.cs
git commit -m "feat(license): implement CheckDashboardLimitAsync and update related methods"
```
---

## Task 6: 更新 DashboardService 添加许可证检查
**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/DashboardService.cs`

**Step 1: 注入 ILicenseService**
在构造函数参数中添加：
```csharp
private readonly ILicenseService _licenseService;

public DashboardService(
    DataForgeStudioDbContext context,
    IReportService reportService,
    ILogger<DashboardService> logger,
    ILicenseService licenseService)  // 新增
{
    _context = context;
    _reportService = reportService;
    _logger = logger;
    _licenseService = licenseService;  // 新增
}
```

**Step 2: 在 CreateDashboardAsync 开头添加许可证检查**
```csharp
public async Task<ApiResponse<DashboardDto>> CreateDashboardAsync(CreateDashboardRequest request, int createdBy)
{
    // 检查许可证限制
    var limitCheck = await _licenseService.CheckDashboardLimitAsync();
    if (!limitCheck.Success)
    {
        return ApiResponse<DashboardDto>.Fail(
            limitCheck.Message,
            limitCheck.ErrorCode ?? "DASHBOARD_LIMIT_EXCEEDED"
        );
    }

    // 原有代码继续...
}
```

**Step 3: 验证编译通过**
Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 4: Commit**
```bash
git add backend/src/DataForgeStudio.Core/Services/DashboardService.cs
git commit -m "feat(dashboard): add license check when creating dashboard"
```
---

## Task 7: 更新 LicenseGenerator 工具
**Files:**
- Modify: `backend/tools/LicenseGenerator/Program.cs`

**Step 1: 更新 AvailableFeatures 数组**
```csharp
private static readonly string[] AvailableFeatures =
{
    "报表设计", "报表查询", "图表展示", "Excel导出", "PDF导出",
    "数据源管理", "用户管理", "角色管理",
    "大屏设计", "大屏展示", "全屏模式"  // 新增
};
```

**Step 2: 更新 LicenseInfo 类**
添加属性：
```csharp
public int MaxDashboards { get; set; }
```

**Step 3: 更新 CollectLicenseInfo 方法**
在 `MaxDataSources` 输入后添加：
```csharp
// 最大大屏数
Console.Write($"最大大屏数 (默认: {info.MaxDashboards}, 输入 0 表示无限制): ");
var dashboardsInput = Console.ReadLine()?.Trim();
if (!string.IsNullOrEmpty(dashboardsInput) && int.TryParse(dashboardsInput, out var maxDashboards) && maxDashboards >= 0)
{
    info.MaxDashboards = maxDashboards;
}
```

**Step 4: 更新 SetDefaultsForLicenseType 方法**
```csharp
case "Trial":
    info.ExpiryDate = now.AddDays(30);
    info.MaxUsers = 5;
    info.MaxReports = 10;
    info.MaxDataSources = 2;
    info.MaxDashboards = 3;  // 新增
    info.Features = AvailableFeatures.Take(7).ToList(); // 基础功能 + 大屏功能
    break;

case "Standard":
    info.ExpiryDate = now.AddYears(1);
    info.MaxUsers = 20;
    info.MaxReports = 50;
    info.MaxDataSources = 5;
    info.MaxDashboards = 10;  // 新增
    info.Features = AvailableFeatures.Take(10).ToList(); // 标准功能
    break;

case "Professional":
    info.ExpiryDate = now.AddYears(1);
    info.MaxUsers = 100;
    info.MaxReports = 0;
    info.MaxDataSources = 0;
    info.MaxDashboards = 50;  // 新增
    info.Features = AvailableFeatures.ToList();
    break;

case "Enterprise":
    info.ExpiryDate = now.AddYears(2);
    info.MaxUsers = 0;
    info.MaxReports = 0;
    info.MaxDataSources = 0;
    info.MaxDashboards = 0;  // 无限制
    info.Features = AvailableFeatures.ToList();
    break;
```

**Step 5: 更新 GenerateLicenseAsync 方法**
在 JSON 构建中添加 `MaxDashboards`：
```csharp
var licenseData = new
{
    LicenseId = licenseId,
    CustomerName = info.CustomerName,
    ExpiryDate = info.ExpiryDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
    MaxUsers = info.MaxUsers,
    MaxReports = info.MaxReports,
    MaxDataSources = info.MaxDataSources,
    MaxDashboards = info.MaxDashboards,  // 新增
    Features = info.Features,
    MachineCode = info.MachineCode,
    IssuedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
    LicenseType = info.LicenseType,
    Signature = ""
};
```

同样更新 `jsonForSigning` 和 `finalLicenseData` 对象。

**Step 6: 更新预览显示**
```csharp
Console.WriteLine($"  最大大屏数: {(info.MaxDashboards == 0 ? "无限制" : info.MaxDashboards.ToString())}");
```

**Step 7: 验证编译通过**
Run: `cd backend/tools/LicenseGenerator && dotnet build`
Expected: Build succeeded

**Step 8: Commit**
```bash
git add backend/tools/LicenseGenerator/Program.cs
git commit -m "feat(license-generator): add MaxDashboards and dashboard features"
```
---

## Task 8: 更新前端许可证 Store
**Files:**
- Modify: `frontend/src/stores/license.js`

**Step 1: 添加 maxDashboards 状态**
在 state 中添加：
```javascript
maxDashboards: 0,
```

**Step 2: 在 fetchLicense action 中映射字段**
```javascript
maxDashboards: data.maxDashboards || data.MaxDashboards || 0,
```

**Step 3: Commit**
```bash
git add frontend/src/stores/license.js
git commit -m "feat(frontend): add maxDashboards to license store"
```
---

## Task 9: 集成测试
**Step 1: 启动后端**
Run: `cd backend/src/DataForgeStudio.Api && dotnet run`

**Step 2: 启动前端**
Run: `cd frontend && npm run dev`

**Step 3: 测试场景**
1. 访问许可证管理页面，确认显示 MaxDashboards
2. 创建大屏直到达到限制，确认报错
3. 使用 LicenseGenerator 生成新许可证，激活后确认限制更新

**Step 4: Final Commit**
```bash
git add -A
git commit -m "feat(license): complete dashboard license support"
```
---

## 执行选择
Plan complete and saved to `docs/plans/2026-03-03-license-dashboard-implementation.md`. Two execution options:

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

Which approach?
