# 大屏创建流程优化 实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 优化大屏创建流程，实现向导式创建、预置模板、发布状态管理和公开访问功能

**Architecture:** 前端使用 Vue 3 + Element Plus 实现向导对话框和状态管理；后端使用 ASP.NET Core 扩展 Dashboard 实体和 API；预置模板定义为静态配置，创建时自动生成组件

**Tech Stack:** Vue 3, Element Plus, ASP.NET Core 8.0, Entity Framework Core, SQL Server

---

## Task 1: 后端 - 扩展 Dashboard 实体

**Files:**
- Modify: `backend/src/DataForgeStudio.Domain/Entities/Dashboard.cs`

**Step 1: 添加新字段到 Dashboard 实体**

```csharp
// 在 Dashboard.cs 中添加以下属性

/// <summary>
/// 大屏状态：draft(草稿) / published(已发布)
/// </summary>
[StringLength(20)]
[DefaultValue("draft")]
public string Status { get; set; } = "draft";

/// <summary>
/// 是否公开访问
/// </summary>
[DefaultValue(false)]
public bool IsPublic { get; set; } = false;

/// <summary>
/// 公开访问URL标识
/// </summary>
[StringLength(50)]
public string? PublicUrl { get; set; }

/// <summary>
/// 授权用户ID列表（JSON数组）
/// </summary>
[StringLength(maxLength: 4000)]
public string? AuthorizedUserIds { get; set; }
```

**Step 2: 编译验证**

Run: `cd H:/DataForge/backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Domain/Entities/Dashboard.cs
git commit -m "feat(domain): add Status, IsPublic, PublicUrl, AuthorizedUserIds to Dashboard"
```

---

## Task 2: 后端 - 更新 DbContext 配置

**Files:**
- Modify: `backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs`

**Step 1: 更新 Dashboard 实体配置**

在 `OnModelCreating` 方法的 Dashboard 配置部分添加：

```csharp
// 在 Dashboard 配置中添加
entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("draft");
entity.Property(e => e.IsPublic).HasDefaultValue(false);
entity.Property(e => e.PublicUrl).HasMaxLength(50);
entity.Property(e => e.AuthorizedUserIds).HasMaxLength(4000);

// 添加索引
entity.HasIndex(e => e.Status);
entity.HasIndex(e => e.PublicUrl).IsUnique().HasFilter("[PublicUrl] IS NOT NULL");
```

**Step 2: 编译验证**

Run: `cd H:/DataForge/backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs
git commit -m "feat(data): configure Dashboard new fields in DbContext"
```

---

## Task 3: 后端 - 创建 DTO 类

**Files:**
- Modify: `backend/src/DataForgeStudio.Domain/Dto/DashboardDto.cs`

**Step 1: 更新 CreateDashboardDto**

```csharp
public class CreateDashboardDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public string Theme { get; set; } = "dark";
    public string? BackgroundColor { get; set; }
    public int RefreshInterval { get; set; } = 0;

    // 新增：模板ID
    public string? TemplateId { get; set; }
}
```

**Step 2: 添加新的 DTO 类**

```csharp
/// <summary>
/// 设置大屏访问权限请求
/// </summary>
public class SetDashboardAccessDto
{
    public bool IsPublic { get; set; }
    public List<int>? AuthorizedUserIds { get; set; }
}

/// <summary>
/// 大屏访问设置响应
/// </summary>
public class DashboardAccessDto
{
    public bool IsPublic { get; set; }
    public string? PublicUrl { get; set; }
    public List<int>? AuthorizedUserIds { get; set; }
}
```

**Step 3: 更新 DashboardInfoDto**

在 DashboardInfoDto 中添加：

```csharp
public string Status { get; set; } = "draft";
public bool IsPublic { get; set; }
public string? PublicUrl { get; set; }
```

**Step 4: 编译验证**

Run: `cd H:/DataForge/backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add backend/src/DataForgeStudio.Domain/Dto/DashboardDto.cs
git commit -m "feat(domain): add Dashboard DTOs for status and access control"
```

---

## Task 4: 后端 - 定义预置模板配置

**Files:**
- Create: `backend/src/DataForgeStudio.Core/Configuration/PresetTemplates.cs`

**Step 1: 创建预置模板配置类**

```csharp
namespace DataForgeStudio.Core.Configuration;

/// <summary>
/// 预置模板定义
/// </summary>
public static class PresetTemplates
{
    public class TemplateWidget
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public string WidgetType { get; set; } = "table";
        public string Title { get; set; } = "";
        public object? Config { get; set; }
    }

    public class Template
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public List<TemplateWidget> Widgets { get; set; } = new();
    }

    public static List<Template> All = new()
    {
        new Template
        {
            Id = "production-monitor",
            Name = "生产监控",
            Widgets = new()
            {
                new() { X = 0, Y = 0, W = 8, H = 8, WidgetType = "table", Title = "生产数据监控" },
                new() { X = 8, Y = 0, W = 4, H = 4, WidgetType = "card-number", Title = "当日产量", Config = new { color = "#00d4ff" } },
                new() { X = 8, Y = 4, W = 4, H = 4, WidgetType = "card-number", Title = "设备稼动率", Config = new { color = "#00ff88" } }
            }
        },
        new Template
        {
            Id = "quality-inspection",
            Name = "质检",
            Widgets = new()
            {
                new() { X = 0, Y = 0, W = 8, H = 5, WidgetType = "table", Title = "质检数据" },
                new() { X = 8, Y = 0, W = 4, H = 5, WidgetType = "chart-bar", Title = "合格率统计" },
                new() { X = 0, Y = 5, W = 6, H = 4, WidgetType = "table", Title = "不良品明细" },
                new() { X = 6, Y = 5, W = 6, H = 4, WidgetType = "gauge", Title = "整体合格率" }
            }
        },
        new Template
        {
            Id = "process-progress",
            Name = "工序进度",
            Widgets = new()
            {
                new() { X = 0, Y = 0, W = 12, H = 4, WidgetType = "table", Title = "工序进度表" },
                new() { X = 0, Y = 4, W = 12, H = 5, WidgetType = "chart-bar", Title = "甘特图" },
                new() { X = 0, Y = 9, W = 4, H = 3, WidgetType = "progress-bar", Title = "整体进度" },
                new() { X = 4, Y = 9, W = 4, H = 3, WidgetType = "progress-bar", Title = "完成率" },
                new() { X = 8, Y = 9, W = 4, H = 3, WidgetType = "progress-bar", Title = "延误率" }
            }
        },
        new Template
        {
            Id = "order-progress",
            Name = "订单进度",
            Widgets = new()
            {
                new() { X = 0, Y = 0, W = 8, H = 7, WidgetType = "table", Title = "订单明细" },
                new() { X = 8, Y = 0, W = 4, H = 3, WidgetType = "card-number", Title = "待处理订单" },
                new() { X = 8, Y = 3, W = 4, H = 4, WidgetType = "chart-pie", Title = "订单状态分布" },
                new() { X = 0, Y = 7, W = 6, H = 5, WidgetType = "table", Title = "今日交付" },
                new() { X = 6, Y = 7, W = 6, H = 5, WidgetType = "chart-line", Title = "订单趋势" }
            }
        },
        new Template
        {
            Id = "equipment-status",
            Name = "设备状态",
            Widgets = new()
            {
                new() { X = 0, Y = 0, W = 4, H = 3, WidgetType = "status-light", Title = "运行中" },
                new() { X = 4, Y = 0, W = 4, H = 3, WidgetType = "status-light", Title = "待机" },
                new() { X = 8, Y = 0, W = 4, H = 3, WidgetType = "status-light", Title = "故障" },
                new() { X = 0, Y = 3, W = 12, H = 5, WidgetType = "table", Title = "设备列表" },
                new() { X = 0, Y = 8, W = 6, H = 4, WidgetType = "chart-bar", Title = "设备效率" },
                new() { X = 6, Y = 8, W = 6, H = 4, WidgetType = "chart-line", Title = "温度监控" }
            }
        },
        new Template
        {
            Id = "kanban-board",
            Name = "看板布局",
            Widgets = new()
            {
                new() { X = 0, Y = 0, W = 4, H = 12, WidgetType = "table", Title = "待处理" },
                new() { X = 4, Y = 0, W = 4, H = 12, WidgetType = "table", Title = "进行中" },
                new() { X = 8, Y = 0, W = 4, H = 12, WidgetType = "table", Title = "已完成" }
            }
        }
    };

    public static Template? GetById(string id)
    {
        return All.FirstOrDefault(t => t.Id == id);
    }
}
```

**Step 2: 编译验证**

Run: `cd H:/DataForge/backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Configuration/PresetTemplates.cs
git commit -m "feat(core): add preset templates configuration"
```

---

## Task 5: 后端 - 扩展 DashboardService

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/DashboardService.cs`

**Step 1: 添加 using 语句**

```csharp
using DataForgeStudio.Core.Configuration;
using System.Text.Json;
```

**Step 2: 修改 CreateDashboardAsync 方法**

在创建 Dashboard 后，添加模板处理逻辑：

```csharp
public async Task<DashboardInfoDto> CreateDashboardAsync(CreateDashboardDto dto, int userId)
{
    var dashboard = new Dashboard
    {
        Name = dto.Name,
        Description = dto.Description,
        Theme = dto.Theme,
        ThemeConfig = JsonSerializer.Serialize(new
        {
            width = dto.Width,
            height = dto.Height,
            backgroundColor = dto.BackgroundColor ?? "#0a1628"
        }),
        RefreshInterval = dto.RefreshInterval,
        Status = "draft",
        IsPublic = false,
        CreatedBy = userId
    };

    _context.Dashboards.Add(dashboard);
    await _context.SaveChangesAsync();

    // 如果指定了模板，创建组件
    if (!string.IsNullOrEmpty(dto.TemplateId))
    {
        var template = PresetTemplates.GetById(dto.TemplateId);
        if (template != null)
        {
            foreach (var widget in template.Widgets)
            {
                var dashboardWidget = new DashboardWidget
                {
                    DashboardId = dashboard.DashboardId,
                    WidgetType = widget.WidgetType,
                    Title = widget.Title,
                    PositionX = widget.X,
                    PositionY = widget.Y,
                    Width = widget.W,
                    Height = widget.H,
                    DataConfig = widget.Config != null
                        ? JsonSerializer.Serialize(widget.Config)
                        : "{}"
                };
                _context.DashboardWidgets.Add(dashboardWidget);
            }
            await _context.SaveChangesAsync();
        }
    }

    return await GetDashboardByIdAsync(dashboard.DashboardId);
}
```

**Step 3: 添加发布/取消发布方法**

```csharp
/// <summary>
/// 发布大屏
/// </summary>
public async Task<bool> PublishDashboardAsync(int dashboardId)
{
    var dashboard = await _context.Dashboards.FindAsync(dashboardId);
    if (dashboard == null) return false;

    dashboard.Status = "published";
    dashboard.UpdatedTime = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    return true;
}

/// <summary>
/// 取消发布大屏
/// </summary>
public async Task<bool> UnpublishDashboardAsync(int dashboardId)
{
    var dashboard = await _context.Dashboards.FindAsync(dashboardId);
    if (dashboard == null) return false;

    dashboard.Status = "draft";
    dashboard.IsPublic = false;
    dashboard.UpdatedTime = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    return true;
}

/// <summary>
/// 设置大屏访问权限
/// </summary>
public async Task<DashboardAccessDto?> SetDashboardAccessAsync(int dashboardId, SetDashboardAccessDto dto)
{
    var dashboard = await _context.Dashboards.FindAsync(dashboardId);
    if (dashboard == null) return null;

    dashboard.IsPublic = dto.IsPublic;

    if (dto.IsPublic && string.IsNullOrEmpty(dashboard.PublicUrl))
    {
        // 生成公开URL标识（8位短码）
        dashboard.PublicUrl = Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    if (dto.AuthorizedUserIds != null)
    {
        dashboard.AuthorizedUserIds = JsonSerializer.Serialize(dto.AuthorizedUserIds);
    }

    dashboard.UpdatedTime = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return new DashboardAccessDto
    {
        IsPublic = dashboard.IsPublic,
        PublicUrl = dashboard.IsPublic ? $"/public/d/{dashboard.PublicUrl}" : null,
        AuthorizedUserIds = dto.AuthorizedUserIds
    };
}

/// <summary>
/// 获取大屏访问设置
/// </summary>
public async Task<DashboardAccessDto?> GetDashboardAccessAsync(int dashboardId)
{
    var dashboard = await _context.Dashboards.FindAsync(dashboardId);
    if (dashboard == null) return null;

    List<int>? authorizedUsers = null;
    if (!string.IsNullOrEmpty(dashboard.AuthorizedUserIds))
    {
        authorizedUsers = JsonSerializer.Deserialize<List<int>>(dashboard.AuthorizedUserIds);
    }

    return new DashboardAccessDto
    {
        IsPublic = dashboard.IsPublic,
        PublicUrl = dashboard.IsPublic ? $"/public/d/{dashboard.PublicUrl}" : null,
        AuthorizedUserIds = authorizedUsers
    };
}

/// <summary>
/// 通过公开URL获取大屏
/// </summary>
public async Task<DashboardInfoDto?> GetDashboardByPublicUrlAsync(string publicUrl)
{
    var dashboard = await _context.Dashboards
        .Include(d => d.Widgets)
        .FirstOrDefaultAsync(d => d.PublicUrl == publicUrl && d.IsPublic && d.Status == "published");

    if (dashboard == null) return null;

    return MapToDto(dashboard);
}
```

**Step 4: 编译验证**

Run: `cd H:/DataForge/backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/DashboardService.cs
git commit -m "feat(core): add publish/unpublish and access control to DashboardService"
```

---

## Task 6: 后端 - 更新 DashboardController

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Controllers/DashboardController.cs`

**Step 1: 添加新的 API 端点**

```csharp
/// <summary>
/// 发布大屏
/// </summary>
[HttpPost("{id}/publish")]
public async Task<IActionResult> PublishDashboard(int id)
{
    var result = await _dashboardService.PublishDashboardAsync(id);
    if (!result) return NotFound(new { success = false, message = "大屏不存在" });
    return Success(new { status = "published" });
}

/// <summary>
/// 取消发布大屏
/// </summary>
[HttpDelete("{id}/publish")]
public async Task<IActionResult> UnpublishDashboard(int id)
{
    var result = await _dashboardService.UnpublishDashboardAsync(id);
    if (!result) return NotFound(new { success = false, message = "大屏不存在" });
    return Success(new { status = "draft" });
}

/// <summary>
/// 设置大屏访问权限
/// </summary>
[HttpPut("{id}/access")]
public async Task<IActionResult> SetDashboardAccess(int id, [FromBody] SetDashboardAccessDto dto)
{
    var result = await _dashboardService.SetDashboardAccessAsync(id, dto);
    if (result == null) return NotFound(new { success = false, message = "大屏不存在" });
    return Success(result);
}

/// <summary>
/// 获取大屏访问设置
/// </summary>
[HttpGet("{id}/access")]
public async Task<IActionResult> GetDashboardAccess(int id)
{
    var result = await _dashboardService.GetDashboardAccessAsync(id);
    if (result == null) return NotFound(new { success = false, message = "大屏不存在" });
    return Success(result);
}
```

**Step 2: 编译验证**

Run: `cd H:/DataForge/backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Controllers/DashboardController.cs
git commit -m "feat(api): add publish/access endpoints to DashboardController"
```

---

## Task 7: 后端 - 添加公开访问端点

**Files:**
- Create: `backend/src/DataForgeStudio.Api/Controllers/PublicDashboardController.cs`

**Step 1: 创建公开访问控制器**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataForgeStudio.Core.Interfaces;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 公开大屏访问控制器（无需认证）
/// </summary>
[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicDashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public PublicDashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// 通过公开URL获取大屏
    /// </summary>
    [HttpGet("d/{publicUrl}")]
    public async Task<IActionResult> GetPublicDashboard(string publicUrl)
    {
        var dashboard = await _dashboardService.GetDashboardByPublicUrlAsync(publicUrl);
        if (dashboard == null)
        {
            return NotFound(new { success = false, message = "大屏不存在或未公开" });
        }
        return Ok(new { success = true, data = dashboard });
    }

    /// <summary>
    /// 通过公开URL获取大屏数据
    /// </summary>
    [HttpGet("d/{publicUrl}/data")]
    public async Task<IActionResult> GetPublicDashboardData(string publicUrl)
    {
        var dashboard = await _dashboardService.GetDashboardByPublicUrlAsync(publicUrl);
        if (dashboard == null)
        {
            return NotFound(new { success = false, message = "大屏不存在或未公开" });
        }

        var data = await _dashboardService.GetDashboardDataAsync(dashboard.DashboardId);
        return Ok(new { success = true, data });
    }
}
```

**Step 2: 编译验证**

Run: `cd H:/DataForge/backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Controllers/PublicDashboardController.cs
git commit -m "feat(api): add public dashboard access controller"
```

---

## Task 8: 前端 - 更新 API 接口

**Files:**
- Modify: `frontend/src/api/dashboard.js`

**Step 1: 添加新的 API 方法**

```javascript
/**
 * 发布大屏
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function publishDashboard(id) {
  return request.post(`/dashboards/${id}/publish`)
}

/**
 * 取消发布大屏
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function unpublishDashboard(id) {
  return request.delete(`/dashboards/${id}/publish`)
}

/**
 * 设置大屏访问权限
 * @param {number|string} id - 大屏ID
 * @param {Object} data - 访问设置
 * @param {boolean} data.isPublic - 是否公开
 * @param {Array} data.authorizedUserIds - 授权用户ID列表
 * @returns {Promise}
 */
export function setDashboardAccess(id, data) {
  return request.put(`/dashboards/${id}/access`, data)
}

/**
 * 获取大屏访问设置
 * @param {number|string} id - 大屏ID
 * @returns {Promise}
 */
export function getDashboardAccess(id) {
  return request.get(`/dashboards/${id}/access`)
}

// 在 dashboardApi 导出对象中添加
export const dashboardApi = {
  // ... 现有方法

  // 发布管理
  publishDashboard,
  unpublishDashboard,
  setDashboardAccess,
  getDashboardAccess
}
```

**Step 2: Commit**

```bash
git add frontend/src/api/dashboard.js
git commit -m "feat(api): add publish and access control API methods"
```

---

## Task 9: 前端 - 完善新建大屏对话框

**Files:**
- Modify: `frontend/src/views/dashboard/DashboardList.vue`

**Step 1: 确保 handleConfirmCreate 方法正确处理模板**

```javascript
// 确认创建
const handleConfirmCreate = async () => {
  if (!createFormRef.value) return

  try {
    await createFormRef.value.validate()
  } catch {
    return
  }

  createLoading.value = true
  try {
    const dashboardData = {
      name: createForm.name,
      description: createForm.description,
      width: createForm.width,
      height: createForm.height,
      backgroundColor: createForm.backgroundColor,
      theme: createForm.theme,
      refreshInterval: createForm.refreshInterval,
      templateId: createForm.template || null
    }

    const res = await createDashboard(dashboardData)
    if (res.success) {
      const dashboardId = res.data?.dashboardId || res.data?.DashboardId || res.data?.id
      ElMessage.success('大屏创建成功')
      createDialogVisible.value = false

      if (dashboardId) {
        router.push(`/dashboard/designer/${dashboardId}`)
      } else {
        router.push('/dashboard/designer')
      }
    } else {
      ElMessage.error(res.message || '创建失败')
    }
  } catch (err) {
    ElMessage.error('创建失败: ' + (err.message || '未知错误'))
  } finally {
    createLoading.value = false
  }
}
```

**Step 2: Commit**

```bash
git add frontend/src/views/dashboard/DashboardList.vue
git commit -m "feat(frontend): improve create dashboard dialog with template support"
```

---

## Task 10: 前端 - 更新列表页状态显示

**Files:**
- Modify: `frontend/src/views/dashboard/DashboardList.vue`

**Step 1: 添加状态列**

在表格中添加状态列：

```vue
<el-table-column label="状态" width="100" align="center">
  <template #default="{ row }">
    <el-tag v-if="row.status === 'draft'" type="info" size="small">草稿</el-tag>
    <el-tag v-else-if="row.isPublic" type="success" size="small">公开</el-tag>
    <el-tag v-else type="primary" size="small">已发布</el-tag>
  </template>
</el-table-column>
```

**Step 2: 更新操作按钮**

```vue
<el-table-column label="操作" width="320" fixed="right">
  <template #default="{ row }">
    <el-button type="primary" link size="small" @click="handleEdit(row)">
      <el-icon><Edit /></el-icon>
      设计
    </el-button>
    <el-button type="info" link size="small" @click="handleView(row)">
      <el-icon><View /></el-icon>
      预览
    </el-button>
    <template v-if="row.status === 'draft'">
      <el-button type="success" link size="small" @click="handlePublish(row)">
        <el-icon><Upload /></el-icon>
        发布
      </el-button>
    </template>
    <template v-else>
      <el-button type="warning" link size="small" @click="handleAccessSettings(row)">
        <el-icon><Setting /></el-icon>
        访问设置
      </el-button>
      <el-button type="info" link size="small" @click="handleUnpublish(row)">
        <el-icon><Download /></el-icon>
        取消发布
      </el-button>
      <el-button v-if="row.isPublic" type="success" link size="small" @click="handleCopyPublicUrl(row)">
        <el-icon><Link /></el-icon>
        复制链接
      </el-button>
    </template>
    <el-button type="success" link size="small" @click="handleCopy(row)">
      <el-icon><DocumentCopy /></el-icon>
      复制
    </el-button>
    <el-button type="danger" link size="small" @click="handleDelete(row)">
      <el-icon><Delete /></el-icon>
      删除
    </el-button>
  </template>
</el-table-column>
```

**Step 3: Commit**

```bash
git add frontend/src/views/dashboard/DashboardList.vue
git commit -m "feat(frontend): add status column and conditional action buttons"
```

---

## Task 11: 前端 - 添加发布和访问设置功能

**Files:**
- Modify: `frontend/src/views/dashboard/DashboardList.vue`

**Step 1: 添加访问设置对话框**

```vue
<!-- 访问设置对话框 -->
<el-dialog v-model="accessDialogVisible" title="访问设置" width="500px">
  <el-form :model="accessForm" label-width="100px">
    <el-form-item label="公开访问">
      <el-switch v-model="accessForm.isPublic" />
      <div class="form-hint">开启后，任何人都可以通过链接访问（无需登录）</div>
    </el-form-item>
    <el-form-item v-if="accessForm.isPublic && accessForm.publicUrl" label="公开链接">
      <el-input v-model="accessForm.publicUrl" readonly>
        <template #append>
          <el-button @click="handleCopyUrl">复制</el-button>
        </template>
      </el-input>
    </el-form-item>
    <el-form-item v-if="!accessForm.isPublic" label="授权用户">
      <el-select v-model="accessForm.authorizedUserIds" multiple filterable placeholder="选择可访问的用户" style="width: 100%">
        <el-option v-for="user in userList" :key="user.userId" :label="user.username" :value="user.userId" />
      </el-select>
      <div class="form-hint">未选择时，所有登录用户均可访问</div>
    </el-form-item>
  </el-form>
  <template #footer>
    <el-button @click="accessDialogVisible = false">取消</el-button>
    <el-button type="primary" @click="handleSaveAccessSettings" :loading="accessLoading">保存</el-button>
  </template>
</el-dialog>
```

**Step 2: 添加相关数据和方法**

```javascript
import { publishDashboard, unpublishDashboard, setDashboardAccess, getDashboardAccess } from '../../api/dashboard'
import { Upload, Download, Setting, Link } from '@element-plus/icons-vue'

// 访问设置相关
const accessDialogVisible = ref(false)
const accessLoading = ref(false)
const currentDashboard = ref(null)
const accessForm = reactive({
  isPublic: false,
  publicUrl: '',
  authorizedUserIds: []
})

// 发布大屏
const handlePublish = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要发布大屏"${row.name}"吗？`, '确认发布', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'info'
    })

    const res = await publishDashboard(row.dashboardId || row.id)
    if (res.success) {
      ElMessage.success('发布成功')
      loadData()
    } else {
      ElMessage.error(res.message || '发布失败')
    }
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('发布失败')
    }
  }
}

// 取消发布
const handleUnpublish = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要取消发布大屏"${row.name}"吗？`, '确认取消', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    const res = await unpublishDashboard(row.dashboardId || row.id)
    if (res.success) {
      ElMessage.success('已取消发布')
      loadData()
    } else {
      ElMessage.error(res.message || '操作失败')
    }
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('操作失败')
    }
  }
}

// 打开访问设置
const handleAccessSettings = async (row) => {
  currentDashboard.value = row
  const res = await getDashboardAccess(row.dashboardId || row.id)
  if (res.success) {
    accessForm.isPublic = res.data.isPublic || false
    accessForm.publicUrl = res.data.publicUrl || ''
    accessForm.authorizedUserIds = res.data.authorizedUserIds || []
    accessDialogVisible.value = true
  }
}

// 保存访问设置
const handleSaveAccessSettings = async () => {
  accessLoading.value = true
  try {
    const res = await setDashboardAccess(currentDashboard.value.dashboardId || currentDashboard.value.id, {
      isPublic: accessForm.isPublic,
      authorizedUserIds: accessForm.authorizedUserIds
    })
    if (res.success) {
      ElMessage.success('设置保存成功')
      accessForm.publicUrl = res.data.publicUrl || ''
      accessDialogVisible.value = false
      loadData()
    } else {
      ElMessage.error(res.message || '保存失败')
    }
  } catch {
    ElMessage.error('保存失败')
  } finally {
    accessLoading.value = false
  }
}

// 复制公开链接
const handleCopyPublicUrl = (row) => {
  const url = `${window.location.origin}/public/d/${row.publicUrl}`
  navigator.clipboard.writeText(url)
  ElMessage.success('链接已复制到剪贴板')
}

// 复制URL
const handleCopyUrl = () => {
  const url = `${window.location.origin}${accessForm.publicUrl}`
  navigator.clipboard.writeText(url)
  ElMessage.success('链接已复制到剪贴板')
}
```

**Step 3: Commit**

```bash
git add frontend/src/views/dashboard/DashboardList.vue
git commit -m "feat(frontend): add publish/unpublish and access settings functionality"
```

---

## Task 12: 前端 - 添加公开访问路由

**Files:**
- Create: `frontend/src/views/dashboard/PublicDashboard.vue`
- Modify: `frontend/src/router/index.js`

**Step 1: 创建公开访问组件**

```vue
<template>
  <div class="public-dashboard">
    <div v-if="loading" class="loading-container">
      <el-icon class="loading-icon" :size="48"><Loading /></el-icon>
      <span>加载中...</span>
    </div>
    <div v-else-if="error" class="error-container">
      <el-icon :size="64" color="#f56c6c"><WarningFilled /></el-icon>
      <p>{{ error }}</p>
    </div>
    <DashboardViewContent v-else :dashboard-data="dashboardData" />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { Loading, WarningFilled } from '@element-plus/icons-vue'
import { getPublicDashboard, getPublicDashboardData } from '../../api/dashboard'

const route = useRoute()
const loading = ref(true)
const error = ref(null)
const dashboardData = ref(null)

onMounted(async () => {
  const publicUrl = route.params.url
  try {
    const res = await getPublicDashboard(publicUrl)
    if (res.success) {
      dashboardData.value = res.data
    } else {
      error.value = res.message || '大屏不存在或未公开'
    }
  } catch {
    error.value = '加载失败'
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.public-dashboard {
  width: 100vw;
  height: 100vh;
  background: #0a1628;
}

.loading-container,
.error-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #fff;
  gap: 16px;
}

.loading-icon {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>
```

**Step 2: 添加路由**

在 router/index.js 中添加：

```javascript
{
  path: '/public/d/:url',
  name: 'PublicDashboard',
  component: () => import('../views/dashboard/PublicDashboard.vue'),
  meta: { requiresAuth: false }
}
```

**Step 3: Commit**

```bash
git add frontend/src/views/dashboard/PublicDashboard.vue frontend/src/router/index.js
git commit -m "feat(frontend): add public dashboard access route and component"
```

---

## Task 13: 数据库迁移

**Files:**
- Create: `database/migrations/V1.2.0_Dashboard_Status.sql`

**Step 1: 创建迁移脚本**

```sql
-- 大屏状态和公开访问字段
ALTER TABLE Dashboards ADD Status NVARCHAR(20) NOT NULL DEFAULT 'draft';
ALTER TABLE Dashboards ADD IsPublic BIT NOT NULL DEFAULT 0;
ALTER TABLE Dashboards ADD PublicUrl NVARCHAR(50) NULL;
ALTER TABLE Dashboards ADD AuthorizedUserIds NVARCHAR(4000) NULL;

-- 创建索引
CREATE INDEX IX_Dashboards_Status ON Dashboards(Status);
CREATE UNIQUE INDEX IX_Dashboards_PublicUrl ON Dashboards(PublicUrl) WHERE PublicUrl IS NOT NULL;
```

**Step 2: Commit**

```bash
git add database/migrations/V1.2.0_Dashboard_Status.sql
git commit -m "feat(db): add migration for Dashboard status fields"
```

---

## Task 14: 集成测试

**Step 1: 启动后端**

Run: `cd H:/DataForge/backend && dotnet run --project src/DataForgeStudio.Api`

**Step 2: 启动前端**

Run: `cd H:/DataForge/frontend && npm run dev`

**Step 3: 验证测试场景**

1. 创建空白大屏 - 成功进入设计器
2. 创建带模板大屏 - 组件自动添加
3. 发布大屏 - 状态变为"已发布"
4. 设置公开访问 - 生成公开链接
5. 匿名访问公开链接 - 无需登录可查看

**Step 4: Commit**

```bash
git add -A
git commit -m "test: verify dashboard creation flow integration"
```

---

## 执行选项

计划已完成并保存到 `docs/plans/2026-03-03-dashboard-creation-flow-plan.md`。

**两种执行方式：**

**1. Subagent-Driven (当前会话)** - 我为每个任务派发新的子代理，任务间进行代码审查，快速迭代

**2. Parallel Session (单独会话)** - 在新会话中使用 executing-plans，批量执行并进行检查点审查

**您选择哪种方式？**
