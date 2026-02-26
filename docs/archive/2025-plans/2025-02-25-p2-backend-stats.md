# P2: 后端数据统计修复实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复许可证信息和首页仪表盘数据统计不准确的问题

**Architecture:** 确保后端统计接口返回数据库实时计数，前端正确使用接口数据

**Tech Stack:** ASP.NET Core 8.0, Entity Framework Core, Vue 3

**Related Tasks:** fix2.md 任务 1, 10

---

## Task 1: 修复许可证信息统计不一致 (fix2.md #1)

**问题描述:** 许可证信息页面显示的"已使用的用户/报表/数据源"数量与数据库实际记录不符

**Files:**
- Review: `backend/src/DataForgeStudio.Core/Services/LicenseService.cs:470-501`
- Review: `frontend/src/views/license/LicenseManagement.vue:350-362`

**Step 1: 验证后端统计逻辑**

检查 `LicenseService.GetUsageStatsAsync()` 方法：

```csharp
public async Task<ApiResponse<LicenseUsageStatsDto>> GetUsageStatsAsync()
{
    try
    {
        // 统计非系统用户数量
        var currentUsers = await _context.Users
            .Where(u => !u.IsSystem)
            .CountAsync();

        // 统计报表数量
        var currentReports = await _context.Reports
            .CountAsync();

        // 统计数据源数量
        var currentDataSources = await _context.DataSources
            .CountAsync();

        var stats = new LicenseUsageStatsDto
        {
            CurrentUsers = currentUsers,
            CurrentReports = currentReports,
            CurrentDataSources = currentDataSources
        };

        return ApiResponse<LicenseUsageStatsDto>.Ok(stats);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取许可证使用统计失败");
        return ApiResponse<LicenseUsageStatsDto>.Fail($"获取统计数据失败: {ex.Message}", "GET_STATS_FAILED");
    }
}
```

**分析:** 后端代码看起来是正确的，直接从数据库查询计数。

**Step 2: 检查前端是否正确调用**

检查 `LicenseManagement.vue` 的 `loadStats` 函数：

```javascript
const loadStats = async () => {
  try {
    const response = await licenseApi.getLicenseStats()
    if (response.success && response.data) {
      stats.currentUsers = response.data.currentUsers
      stats.currentReports = response.data.currentReports
      stats.currentDataSources = response.data.currentDataSources
    }
  } catch {
    stats.currentUsers = 0
    stats.currentReports = 0
    stats.currentDataSources = 0
  }
}
```

**Step 3: 检查 API 请求定义**

在 `frontend/src/api/request.js` 中确认 `getLicenseStats` 方法定义正确。

**Step 4: 检查数据源状态过滤**

如果数据源有"已停用"状态，需要确认是否只统计活跃数据源：

```csharp
// 可选：只统计活跃的数据源
var currentDataSources = await _context.DataSources
    .Where(ds => ds.IsActive)  // 如果需要只统计活跃的
    .CountAsync();
```

**Step 5: 添加日志调试**

在 `GetUsageStatsAsync` 方法中添加详细日志：

```csharp
_logger.LogInformation("统计用户数: {UserCount}, 报表数: {ReportCount}, 数据源数: {DataSourceCount}",
    currentUsers, currentReports, currentDataSources);
```

**Step 6: 验证修复**

1. 启动后端 API
2. 直接调用 `/api/license/stats` 接口，检查返回值
3. 与数据库实际记录对比：`SELECT COUNT(*) FROM Users WHERE IsSystem = 0`
4. 检查前端页面显示是否与接口返回一致

---

## Task 2: 修复首页仪表盘数据统计不准确 (fix2.md #10)

**问题描述:** 首页显示的仪表数据与实际数据库记录不一致

**Files:**
- Review: `frontend/src/views/home/HomePage.vue:175-191`
- Review: `backend/src/DataForgeStudio.Core/Services/LicenseService.cs:470-501`

**Step 1: 检查首页数据获取逻辑**

查看 `HomePage.vue` 的 `loadStats` 函数：

```javascript
const loadStats = async () => {
  try {
    const response = await licenseApi.getLicenseStats()
    if (response.success && response.data) {
      stats.value.reportCount = response.data.currentReports || 0
      stats.value.userCount = response.data.currentUsers || 0
      stats.value.dataSourceCount = response.data.currentDataSources || 0
    }
  } catch {
    // 加载失败保持默认值
  }

  const startDate = getSystemStartDate()
  const now = new Date()
  const days = Math.floor((now - startDate) / (1000 * 60 * 60 * 24))
  stats.value.systemDays = days
}
```

**分析:** 首页使用的是同一个 `licenseApi.getLicenseStats()` 接口。

**Step 2: 确认问题根因**

可能的问题：
1. **缓存问题**: 许可证验证结果被缓存了 30 分钟
2. **数据不同步**: 创建/删除记录后未刷新统计

**Step 3: 检查缓存配置**

在 `LicenseService.cs` 中，统计接口没有使用缓存：

```csharp
// GetUsageStatsAsync 没有缓存，直接查询数据库
// 这是正确的做法
```

但 `ValidateLicenseAsync` 有缓存：

```csharp
private const int CACHE_DURATION_MINUTES = 30;
```

**Step 4: 验证前端页面刷新行为**

确认首页在以下情况是否重新加载数据：
1. 页面加载时（onMounted）
2. 从其他页面返回时（需要检查是否使用 keep-alive）

**Step 5: 添加页面激活时刷新**

如果使用 keep-alive，需要添加 `onActivated` 钩子：

```javascript
import { onMounted, onActivated } from 'vue'

onMounted(async () => {
  await loadStats()
  await loadRecentReports()
})

// 如果使用 keep-alive，页面激活时刷新数据
onActivated(async () => {
  await loadStats()
})
```

**Step 6: 添加刷新按钮（可选）**

在首页添加手动刷新按钮：

```vue
<el-button @click="loadStats" :loading="loading">
  <el-icon><Refresh /></el-icon>
  刷新统计
</el-button>
```

**Step 7: 验证修复**

1. 记录当前数据库中的用户数、报表数、数据源数
2. 打开首页，确认统计数字一致
3. 创建一个新用户，刷新首页，确认用户数+1
4. 删除该用户，刷新首页，确认用户数-1

---

## 执行顺序

1. Task 1 → Task 2 (可以并行执行)

## 潜在问题总结

1. **如果统计数据仍然不准确，检查：**
   - 数据库连接是否正确
   - 是否有软删除（IsDeleted 标记）但未过滤
   - 是否有缓存层干扰

2. **如果前端显示不一致，检查：**
   - API 返回的字段名是否与前端匹配（currentUsers vs CurrentUsers）
   - 是否有响应拦截器修改了数据格式

## 测试 SQL

```sql
-- 验证用户数
SELECT COUNT(*) AS UserCount FROM Users WHERE IsSystem = 0;

-- 验证报表数
SELECT COUNT(*) AS ReportCount FROM Reports;

-- 验证数据源数
SELECT COUNT(*) AS DataSourceCount FROM DataSources;
```
