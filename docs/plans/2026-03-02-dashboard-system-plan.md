# 车间大屏系统实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为 DataForgeStudio 新增车间大屏可视化模块，支持拖拽布局、多组件类型、条件样式、公开访问和轮播展示。

**Architecture:** 数据库驱动模式，复用现有 ReportService 执行 SQL 查询，大屏配置存储在新增的数据库表中。前端使用 vue-grid-layout 实现拖拽，ECharts 渲染图表。

**Tech Stack:** ASP.NET Core 8.0, Entity Framework Core, Vue 3, vue-grid-layout, Apache ECharts, Element Plus

---

## Phase 1: 数据库层

### Task 1.1: 创建 Dashboard 实体类

**Files:**
- Create: `backend/src/DataForgeStudio.Domain/Entities/Dashboard.cs`

**Step 1: 创建实体文件**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 大屏主表
/// </summary>
[Table("Dashboards")]
public class Dashboard
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DashboardId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// 自动刷新间隔（秒），0=不刷新
    /// </summary>
    public int RefreshInterval { get; set; } = 30;

    /// <summary>
    /// 是否公开（无需登录访问）
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 布局配置 JSON
    /// </summary>
    public string? LayoutConfig { get; set; }

    /// <summary>
    /// 主题配置 JSON
    /// </summary>
    public string? ThemeConfig { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 导航属性 - 组件
    /// </summary>
    public virtual ICollection<DashboardWidget> Widgets { get; set; } = new List<DashboardWidget>();

    /// <summary>
    /// 导航属性 - 创建人
    /// </summary>
    [ForeignKey(nameof(CreatedBy))]
    public virtual User? Creator { get; set; }
}
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Domain/Entities/Dashboard.cs
git commit -m "feat(dashboard): add Dashboard entity"
```

---

### Task 1.2: 创建 DashboardWidget 实体类

**Files:**
- Create: `backend/src/DataForgeStudio.Domain/Entities/DashboardWidget.cs`

**Step 1: 创建实体文件**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 大屏组件表
/// </summary>
[Table("DashboardWidgets")]
public class DashboardWidget
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int WidgetId { get; set; }

    public int DashboardId { get; set; }

    /// <summary>
    /// 绑定的报表 ID
    /// </summary>
    public int ReportId { get; set; }

    /// <summary>
    /// 组件类型：table/card-number/progress-bar/chart-bar/chart-line/chart-pie/gauge/status-light/kanban-card/production-panel
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string WidgetType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Title { get; set; }

    /// <summary>
    /// 网格 X 位置
    /// </summary>
    public int PositionX { get; set; } = 0;

    /// <summary>
    /// 网格 Y 位置
    /// </summary>
    public int PositionY { get; set; } = 0;

    /// <summary>
    /// 宽度（网格单位）
    /// </summary>
    public int Width { get; set; } = 4;

    /// <summary>
    /// 高度（网格单位）
    /// </summary>
    public int Height { get; set; } = 3;

    /// <summary>
    /// 数据字段映射 JSON（可选）
    /// </summary>
    public string? DataConfig { get; set; }

    /// <summary>
    /// 样式配置 JSON
    /// </summary>
    public string? StyleConfig { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 大屏
    /// </summary>
    [ForeignKey(nameof(DashboardId))]
    public virtual Dashboard Dashboard { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 报表
    /// </summary>
    [ForeignKey(nameof(ReportId))]
    public virtual Report Report { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 条件规则
    /// </summary>
    public virtual ICollection<WidgetRule> Rules { get; set; } = new List<WidgetRule>();
}
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Domain/Entities/DashboardWidget.cs
git commit -m "feat(dashboard): add DashboardWidget entity"
```

---

### Task 1.3: 创建 WidgetRule 实体类

**Files:**
- Create: `backend/src/DataForgeStudio.Domain/Entities/WidgetRule.cs`

**Step 1: 创建实体文件**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 组件条件样式规则表
/// </summary>
[Table("WidgetRules")]
public class WidgetRule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RuleId { get; set; }

    public int WidgetId { get; set; }

    [MaxLength(50)]
    public string? RuleName { get; set; }

    /// <summary>
    /// 判断字段
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 操作符：lt/lte/gt/gte/eq/neq/between/contains
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// 阈值
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 动作类型：setColor/setIcon/showText
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// 动作值：颜色值/图标名/文本
    /// </summary>
    [MaxLength(100)]
    public string? ActionValue { get; set; }

    /// <summary>
    /// 优先级（数字越大越优先）
    /// </summary>
    public int Priority { get; set; } = 0;

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 组件
    /// </summary>
    [ForeignKey(nameof(WidgetId))]
    public virtual DashboardWidget Widget { get; set; } = null!;
}
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Domain/Entities/WidgetRule.cs
git commit -m "feat(dashboard): add WidgetRule entity"
```

---

### Task 1.4: 更新 DbContext

**Files:**
- Modify: `backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs`

**Step 1: 添加 DbSet**

在 `#region DbSets` 部分添加：

```csharp
    // 大屏
    public DbSet<Dashboard> Dashboards { get; set; }
    public DbSet<DashboardWidget> DashboardWidgets { get; set; }
    public DbSet<WidgetRule> WidgetRules { get; set; }
```

**Step 2: 添加实体配置**

在 `OnModelCreating` 方法中添加：

```csharp
        // 配置 Dashboard
        modelBuilder.Entity<Dashboard>(entity =>
        {
            entity.HasIndex(e => e.IsPublic);
            entity.HasIndex(e => e.CreatedTime);

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Theme).HasMaxLength(20).HasDefaultValue("dark");

            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 配置 DashboardWidget
        modelBuilder.Entity<DashboardWidget>(entity =>
        {
            entity.HasIndex(e => new { e.DashboardId, e.ReportId });

            entity.Property(e => e.WidgetType).HasMaxLength(50).IsRequired();

            entity.HasOne(e => e.Dashboard)
                .WithMany(d => d.Widgets)
                .HasForeignKey(e => e.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Report)
                .WithMany()
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置 WidgetRule
        modelBuilder.Entity<WidgetRule>(entity =>
        {
            entity.HasIndex(e => e.WidgetId);

            entity.Property(e => e.Field).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Operator).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ActionType).HasMaxLength(50).IsRequired();

            entity.HasOne(e => e.Widget)
                .WithMany(w => w.Rules)
                .HasForeignKey(e => e.WidgetId)
                .OnDelete(DeleteBehavior.Cascade);
        });
```

**Step 3: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs
git commit -m "feat(dashboard): add Dashboard DbSets to DbContext"
```

---

### Task 1.5: 创建数据库迁移脚本

**Files:**
- Create: `database/migrations/V1.1.0_Dashboard.sql`

**Step 1: 创建迁移 SQL 文件**

```sql
-- =============================================
-- DataForgeStudio V1.1.0 Dashboard Migration
-- =============================================

-- 创建大屏主表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Dashboards]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Dashboards] (
        [DashboardId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Theme] NVARCHAR(20) NOT NULL DEFAULT 'dark',
        [RefreshInterval] INT NOT NULL DEFAULT 30,
        [IsPublic] BIT NOT NULL DEFAULT 0,
        [LayoutConfig] NVARCHAR(MAX) NULL,
        [ThemeConfig] NVARCHAR(MAX) NULL,
        [CreatedBy] INT NULL,
        [CreatedTime] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedTime] DATETIME NULL,
        CONSTRAINT [FK_Dashboards_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([UserId])
    );

    CREATE INDEX [IX_Dashboards_IsPublic] ON [dbo].[Dashboards]([IsPublic]);
    CREATE INDEX [IX_Dashboards_CreatedTime] ON [dbo].[Dashboards]([CreatedTime]);
END
GO

-- 创建大屏组件表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DashboardWidgets]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DashboardWidgets] (
        [WidgetId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [DashboardId] INT NOT NULL,
        [ReportId] INT NOT NULL,
        [WidgetType] NVARCHAR(50) NOT NULL,
        [Title] NVARCHAR(100) NULL,
        [PositionX] INT NOT NULL DEFAULT 0,
        [PositionY] INT NOT NULL DEFAULT 0,
        [Width] INT NOT NULL DEFAULT 4,
        [Height] INT NOT NULL DEFAULT 3,
        [DataConfig] NVARCHAR(MAX) NULL,
        [StyleConfig] NVARCHAR(MAX) NULL,
        [CreatedTime] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_DashboardWidgets_Dashboards] FOREIGN KEY ([DashboardId]) REFERENCES [dbo].[Dashboards]([DashboardId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DashboardWidgets_Reports] FOREIGN KEY ([ReportId]) REFERENCES [dbo].[Reports]([ReportId])
    );

    CREATE INDEX [IX_DashboardWidgets_DashboardId_ReportId] ON [dbo].[DashboardWidgets]([DashboardId], [ReportId]);
END
GO

-- 创建组件规则表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WidgetRules]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[WidgetRules] (
        [RuleId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [WidgetId] INT NOT NULL,
        [RuleName] NVARCHAR(50) NULL,
        [Field] NVARCHAR(100) NOT NULL,
        [Operator] NVARCHAR(20) NOT NULL,
        [Value] NVARCHAR(100) NOT NULL,
        [ActionType] NVARCHAR(50) NOT NULL,
        [ActionValue] NVARCHAR(100) NULL,
        [Priority] INT NOT NULL DEFAULT 0,
        [CreatedTime] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_WidgetRules_DashboardWidgets] FOREIGN KEY ([WidgetId]) REFERENCES [dbo].[DashboardWidgets]([WidgetId]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_WidgetRules_WidgetId] ON [dbo].[WidgetRules]([WidgetId]);
END
GO

-- 添加大屏相关权限
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [PermissionCode] = 'dashboard:view')
BEGIN
    INSERT INTO [dbo].[Permissions] ([PermissionCode], [PermissionName], [Description], [Module], [CreatedTime])
    VALUES
        ('dashboard:view', '查看大屏', '查看大屏列表和详情', 'Dashboard', GETDATE()),
        ('dashboard:create', '创建大屏', '创建新的大屏', 'Dashboard', GETDATE()),
        ('dashboard:edit', '编辑大屏', '编辑大屏配置和组件', 'Dashboard', GETDATE()),
        ('dashboard:delete', '删除大屏', '删除大屏', 'Dashboard', GETDATE());
END
GO

-- 为管理员角色分配大屏权限
DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = [RoleId] FROM [dbo].[Roles] WHERE [RoleCode] = 'admin';

IF @AdminRoleId IS NOT NULL
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId], [CreatedTime])
    SELECT @AdminRoleId, [PermissionId], GETDATE()
    FROM [dbo].[Permissions]
    WHERE [PermissionCode] IN ('dashboard:view', 'dashboard:create', 'dashboard:edit', 'dashboard:delete')
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[RolePermissions] rp
        WHERE rp.[RoleId] = @AdminRoleId AND rp.[PermissionId] = [Permissions].[PermissionId]
    );
END
GO

PRINT 'Dashboard tables created successfully';
```

**Step 2: Commit**

```bash
git add database/migrations/V1.1.0_Dashboard.sql
git commit -m "feat(dashboard): add database migration script"
```

---

## Phase 2: 后端 DTO 和接口

### Task 2.1: 创建 Dashboard DTOs

**Files:**
- Create: `backend/src/DataForgeStudio.Shared/DTO/DashboardDto.cs`

**Step 1: 创建 DTO 文件**

```csharp
namespace DataForgeStudio.Shared.DTO;

/// <summary>
/// 大屏 DTO
/// </summary>
public class DashboardDto
{
    public int DashboardId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string Theme { get; set; } = "dark";
    public int RefreshInterval { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
    public int WidgetCount { get; set; }
}

/// <summary>
/// 大屏详情 DTO
/// </summary>
public class DashboardDetailDto : DashboardDto
{
    public string? LayoutConfig { get; set; }
    public string? ThemeConfig { get; set; }
    public List<DashboardWidgetDto> Widgets { get; set; } = new();
}

/// <summary>
/// 大屏组件 DTO
/// </summary>
public class DashboardWidgetDto
{
    public int WidgetId { get; set; }
    public int DashboardId { get; set; }
    public int ReportId { get; set; }
    public string ReportName { get; set; } = string.Empty;
    public required string WidgetType { get; set; }
    public string? Title { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? DataConfig { get; set; }
    public string? StyleConfig { get; set; }
    public List<WidgetRuleDto> Rules { get; set; } = new();
}

/// <summary>
/// 组件规则 DTO
/// </summary>
public class WidgetRuleDto
{
    public int RuleId { get; set; }
    public int WidgetId { get; set; }
    public string? RuleName { get; set; }
    public required string Field { get; set; }
    public required string Operator { get; set; }
    public required string Value { get; set; }
    public required string ActionType { get; set; }
    public string? ActionValue { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// 创建/更新大屏请求
/// </summary>
public class CreateDashboardRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string Theme { get; set; } = "dark";
    public int RefreshInterval { get; set; } = 30;
    public bool IsPublic { get; set; }
    public string? LayoutConfig { get; set; }
    public string? ThemeConfig { get; set; }
}

/// <summary>
/// 创建/更新组件请求
/// </summary>
public class CreateWidgetRequest
{
    public int ReportId { get; set; }
    public required string WidgetType { get; set; }
    public string? Title { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; } = 4;
    public int Height { get; set; } = 3;
    public string? DataConfig { get; set; }
    public string? StyleConfig { get; set; }
    public List<CreateWidgetRuleRequest>? Rules { get; set; }
}

/// <summary>
/// 创建/更新规则请求
/// </summary>
public class CreateWidgetRuleRequest
{
    public string? RuleName { get; set; }
    public required string Field { get; set; }
    public required string Operator { get; set; }
    public required string Value { get; set; }
    public required string ActionType { get; set; }
    public string? ActionValue { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// 一键转换请求
/// </summary>
public class ConvertReportToDashboardRequest
{
    public int ReportId { get; set; }
    public string? DashboardName { get; set; }
}

/// <summary>
/// 大屏数据响应（包含所有组件数据）
/// </summary>
public class DashboardDataDto
{
    public int DashboardId { get; set; }
    public int RefreshInterval { get; set; }
    public Dictionary<int, List<Dictionary<string, object>>> WidgetData { get; set; } = new();
}
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Shared/DTO/DashboardDto.cs
git commit -m "feat(dashboard): add Dashboard DTOs"
```

---

### Task 2.2: 创建 IDashboardService 接口

**Files:**
- Create: `backend/src/DataForgeStudio.Core/Interfaces/IDashboardService.cs`

**Step 1: 创建接口文件**

```csharp
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 大屏服务接口
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// 获取大屏分页列表
    /// </summary>
    Task<ApiResponse<PagedResponse<DashboardDto>>> GetDashboardsAsync(PagedRequest request, string? name = null);

    /// <summary>
    /// 获取大屏详情
    /// </summary>
    Task<ApiResponse<DashboardDetailDto>> GetDashboardByIdAsync(int dashboardId);

    /// <summary>
    /// 创建大屏
    /// </summary>
    Task<ApiResponse<DashboardDto>> CreateDashboardAsync(CreateDashboardRequest request, int createdBy);

    /// <summary>
    /// 更新大屏
    /// </summary>
    Task<ApiResponse> UpdateDashboardAsync(int dashboardId, CreateDashboardRequest request);

    /// <summary>
    /// 删除大屏
    /// </summary>
    Task<ApiResponse> DeleteDashboardAsync(int dashboardId);

    /// <summary>
    /// 添加组件
    /// </summary>
    Task<ApiResponse<DashboardWidgetDto>> AddWidgetAsync(int dashboardId, CreateWidgetRequest request);

    /// <summary>
    /// 更新组件
    /// </summary>
    Task<ApiResponse> UpdateWidgetAsync(int dashboardId, int widgetId, CreateWidgetRequest request);

    /// <summary>
    /// 删除组件
    /// </summary>
    Task<ApiResponse> DeleteWidgetAsync(int dashboardId, int widgetId);

    /// <summary>
    /// 批量更新组件位置
    /// </summary>
    Task<ApiResponse> UpdateWidgetPositionsAsync(int dashboardId, List<WidgetPositionRequest> positions);

    /// <summary>
    /// 从报表一键生成大屏
    /// </summary>
    Task<ApiResponse<DashboardDetailDto>> ConvertFromReportAsync(int reportId, string? dashboardName, int createdBy);

    /// <summary>
    /// 获取大屏数据（所有组件的数据）
    /// </summary>
    Task<ApiResponse<DashboardDataDto>> GetDashboardDataAsync(int dashboardId);

    /// <summary>
    /// 获取公开大屏数据（无需认证）
    /// </summary>
    Task<ApiResponse<DashboardDetailDto>> GetPublicDashboardAsync(int dashboardId);
}

/// <summary>
/// 组件位置更新请求
/// </summary>
public class WidgetPositionRequest
{
    public int WidgetId { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Interfaces/IDashboardService.cs
git commit -m "feat(dashboard): add IDashboardService interface"
```

---

## Phase 3: 后端服务实现

### Task 3.1: 创建 DashboardService

**Files:**
- Create: `backend/src/DataForgeStudio.Core/Services/DashboardService.cs`

**Step 1: 创建服务文件**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;
using System.Text.Json;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 大屏服务实现
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly IReportService _reportService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        DataForgeStudioDbContext context,
        IReportService reportService,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _reportService = reportService;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResponse<DashboardDto>>> GetDashboardsAsync(PagedRequest request, string? name = null)
    {
        var query = _context.Dashboards.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(d => d.Name.Contains(name));
        }

        var totalCount = await query.CountAsync();

        var dashboards = await query
            .Include(d => d.Widgets)
            .OrderByDescending(d => d.CreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DashboardDto
            {
                DashboardId = d.DashboardId,
                Name = d.Name,
                Description = d.Description,
                Theme = d.Theme,
                RefreshInterval = d.RefreshInterval,
                IsPublic = d.IsPublic,
                CreatedTime = d.CreatedTime,
                UpdatedTime = d.UpdatedTime,
                WidgetCount = d.Widgets.Count
            })
            .ToListAsync();

        var pagedResponse = new PagedResponse<DashboardDto>(dashboards, totalCount, request.PageIndex, request.PageSize);
        return ApiResponse<PagedResponse<DashboardDto>>.Ok(pagedResponse);
    }

    public async Task<ApiResponse<DashboardDetailDto>> GetDashboardByIdAsync(int dashboardId)
    {
        var dashboard = await _context.Dashboards
            .Include(d => d.Widgets)
                .ThenInclude(w => w.Report)
            .Include(d => d.Widgets)
                .ThenInclude(w => w.Rules)
            .Where(d => d.DashboardId == dashboardId)
            .FirstOrDefaultAsync();

        if (dashboard == null)
        {
            return ApiResponse<DashboardDetailDto>.Fail("大屏不存在", "NOT_FOUND");
        }

        var detail = MapToDetailDto(dashboard);
        return ApiResponse<DashboardDetailDto>.Ok(detail);
    }

    public async Task<ApiResponse<DashboardDto>> CreateDashboardAsync(CreateDashboardRequest request, int createdBy)
    {
        var dashboard = new Dashboard
        {
            Name = request.Name,
            Description = request.Description,
            Theme = request.Theme,
            RefreshInterval = request.RefreshInterval,
            IsPublic = request.IsPublic,
            LayoutConfig = request.LayoutConfig,
            ThemeConfig = request.ThemeConfig,
            CreatedBy = createdBy,
            CreatedTime = DateTime.UtcNow
        };

        _context.Dashboards.Add(dashboard);
        await _context.SaveChangesAsync();

        return ApiResponse<DashboardDto>.Ok(new DashboardDto
        {
            DashboardId = dashboard.DashboardId,
            Name = dashboard.Name,
            Description = dashboard.Description,
            Theme = dashboard.Theme,
            RefreshInterval = dashboard.RefreshInterval,
            IsPublic = dashboard.IsPublic,
            CreatedTime = dashboard.CreatedTime,
            WidgetCount = 0
        }, "大屏创建成功");
    }

    public async Task<ApiResponse> UpdateDashboardAsync(int dashboardId, CreateDashboardRequest request)
    {
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard == null)
        {
            return ApiResponse.Fail("大屏不存在", "NOT_FOUND");
        }

        dashboard.Name = request.Name;
        dashboard.Description = request.Description;
        dashboard.Theme = request.Theme;
        dashboard.RefreshInterval = request.RefreshInterval;
        dashboard.IsPublic = request.IsPublic;
        dashboard.LayoutConfig = request.LayoutConfig;
        dashboard.ThemeConfig = request.ThemeConfig;
        dashboard.UpdatedTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("大屏更新成功");
    }

    public async Task<ApiResponse> DeleteDashboardAsync(int dashboardId)
    {
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard == null)
        {
            return ApiResponse.Fail("大屏不存在", "NOT_FOUND");
        }

        _context.Dashboards.Remove(dashboard);
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("大屏删除成功");
    }

    public async Task<ApiResponse<DashboardWidgetDto>> AddWidgetAsync(int dashboardId, CreateWidgetRequest request)
    {
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard == null)
        {
            return ApiResponse<DashboardWidgetDto>.Fail("大屏不存在", "NOT_FOUND");
        }

        var report = await _context.Reports.FindAsync(request.ReportId);
        if (report == null)
        {
            return ApiResponse<DashboardWidgetDto>.Fail("报表不存在", "REPORT_NOT_FOUND");
        }

        var widget = new DashboardWidget
        {
            DashboardId = dashboardId,
            ReportId = request.ReportId,
            WidgetType = request.WidgetType,
            Title = request.Title,
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            Width = request.Width,
            Height = request.Height,
            DataConfig = request.DataConfig,
            StyleConfig = request.StyleConfig,
            CreatedTime = DateTime.UtcNow
        };

        if (request.Rules != null)
        {
            foreach (var rule in request.Rules)
            {
                widget.Rules.Add(new WidgetRule
                {
                    RuleName = rule.RuleName,
                    Field = rule.Field,
                    Operator = rule.Operator,
                    Value = rule.Value,
                    ActionType = rule.ActionType,
                    ActionValue = rule.ActionValue,
                    Priority = rule.Priority,
                    CreatedTime = DateTime.UtcNow
                });
            }
        }

        _context.DashboardWidgets.Add(widget);
        await _context.SaveChangesAsync();

        // 更新大屏的 UpdatedTime
        dashboard.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<DashboardWidgetDto>.Ok(new DashboardWidgetDto
        {
            WidgetId = widget.WidgetId,
            DashboardId = widget.DashboardId,
            ReportId = widget.ReportId,
            ReportName = report.ReportName,
            WidgetType = widget.WidgetType,
            Title = widget.Title,
            PositionX = widget.PositionX,
            PositionY = widget.PositionY,
            Width = widget.Width,
            Height = widget.Height,
            DataConfig = widget.DataConfig,
            StyleConfig = widget.StyleConfig,
            Rules = widget.Rules.Select(r => new WidgetRuleDto
            {
                RuleId = r.RuleId,
                WidgetId = r.WidgetId,
                RuleName = r.RuleName,
                Field = r.Field,
                Operator = r.Operator,
                Value = r.Value,
                ActionType = r.ActionType,
                ActionValue = r.ActionValue,
                Priority = r.Priority
            }).ToList()
        }, "组件添加成功");
    }

    public async Task<ApiResponse> UpdateWidgetAsync(int dashboardId, int widgetId, CreateWidgetRequest request)
    {
        var widget = await _context.DashboardWidgets
            .Include(w => w.Rules)
            .FirstOrDefaultAsync(w => w.WidgetId == widgetId && w.DashboardId == dashboardId);

        if (widget == null)
        {
            return ApiResponse.Fail("组件不存在", "NOT_FOUND");
        }

        widget.ReportId = request.ReportId;
        widget.WidgetType = request.WidgetType;
        widget.Title = request.Title;
        widget.PositionX = request.PositionX;
        widget.PositionY = request.PositionY;
        widget.Width = request.Width;
        widget.Height = request.Height;
        widget.DataConfig = request.DataConfig;
        widget.StyleConfig = request.StyleConfig;

        // 更新规则：先删除旧的，再添加新的
        _context.WidgetRules.RemoveRange(widget.Rules);
        if (request.Rules != null)
        {
            foreach (var rule in request.Rules)
            {
                widget.Rules.Add(new WidgetRule
                {
                    WidgetId = widgetId,
                    RuleName = rule.RuleName,
                    Field = rule.Field,
                    Operator = rule.Operator,
                    Value = rule.Value,
                    ActionType = rule.ActionType,
                    ActionValue = rule.ActionValue,
                    Priority = rule.Priority,
                    CreatedTime = DateTime.UtcNow
                });
            }
        }

        // 更新大屏的 UpdatedTime
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard != null)
        {
            dashboard.UpdatedTime = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("组件更新成功");
    }

    public async Task<ApiResponse> DeleteWidgetAsync(int dashboardId, int widgetId)
    {
        var widget = await _context.DashboardWidgets
            .FirstOrDefaultAsync(w => w.WidgetId == widgetId && w.DashboardId == dashboardId);

        if (widget == null)
        {
            return ApiResponse.Fail("组件不存在", "NOT_FOUND");
        }

        _context.DashboardWidgets.Remove(widget);

        // 更新大屏的 UpdatedTime
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard != null)
        {
            dashboard.UpdatedTime = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("组件删除成功");
    }

    public async Task<ApiResponse> UpdateWidgetPositionsAsync(int dashboardId, List<WidgetPositionRequest> positions)
    {
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard == null)
        {
            return ApiResponse.Fail("大屏不存在", "NOT_FOUND");
        }

        foreach (var pos in positions)
        {
            var widget = await _context.DashboardWidgets
                .FirstOrDefaultAsync(w => w.WidgetId == pos.WidgetId && w.DashboardId == dashboardId);

            if (widget != null)
            {
                widget.PositionX = pos.PositionX;
                widget.PositionY = pos.PositionY;
                widget.Width = pos.Width;
                widget.Height = pos.Height;
            }
        }

        dashboard.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("组件位置更新成功");
    }

    public async Task<ApiResponse<DashboardDetailDto>> ConvertFromReportAsync(int reportId, string? dashboardName, int createdBy)
    {
        var report = await _context.Reports
            .Include(r => r.Fields)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);

        if (report == null)
        {
            return ApiResponse<DashboardDetailDto>.Fail("报表不存在", "REPORT_NOT_FOUND");
        }

        var dashboard = new Dashboard
        {
            Name = dashboardName ?? $"{report.ReportName} 大屏",
            Description = $"从报表 [{report.ReportName}] 自动生成",
            Theme = "dark",
            RefreshInterval = 30,
            IsPublic = false,
            CreatedBy = createdBy,
            CreatedTime = DateTime.UtcNow
        };

        // 智能生成组件
        var widgets = GenerateWidgetsFromReport(report);
        foreach (var widget in widgets)
        {
            dashboard.Widgets.Add(widget);
        }

        _context.Dashboards.Add(dashboard);
        await _context.SaveChangesAsync();

        var detail = await GetDashboardByIdAsync(dashboard.DashboardId);
        return detail;
    }

    public async Task<ApiResponse<DashboardDataDto>> GetDashboardDataAsync(int dashboardId)
    {
        var dashboard = await _context.Dashboards
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.DashboardId == dashboardId);

        if (dashboard == null)
        {
            return ApiResponse<DashboardDataDto>.Fail("大屏不存在", "NOT_FOUND");
        }

        var result = new DashboardDataDto
        {
            DashboardId = dashboardId,
            RefreshInterval = dashboard.RefreshInterval,
            WidgetData = new Dictionary<int, List<Dictionary<string, object>>>()
        };

        foreach (var widget in dashboard.Widgets)
        {
            var reportResult = await _reportService.ExecuteReportAsync(widget.ReportId, new ExecuteReportRequest());
            if (reportResult.Success && reportResult.Data != null)
            {
                result.WidgetData[widget.WidgetId] = reportResult.Data;
            }
            else
            {
                result.WidgetData[widget.WidgetId] = new List<Dictionary<string, object>>();
            }
        }

        return ApiResponse<DashboardDataDto>.Ok(result);
    }

    public async Task<ApiResponse<DashboardDetailDto>> GetPublicDashboardAsync(int dashboardId)
    {
        var dashboard = await _context.Dashboards
            .Include(d => d.Widgets)
                .ThenInclude(w => w.Report)
            .Include(d => d.Widgets)
                .ThenInclude(w => w.Rules)
            .FirstOrDefaultAsync(d => d.DashboardId == dashboardId);

        if (dashboard == null)
        {
            return ApiResponse<DashboardDetailDto>.Fail("大屏不存在", "NOT_FOUND");
        }

        if (!dashboard.IsPublic)
        {
            return ApiResponse<DashboardDetailDto>.Fail("此大屏未公开", "NOT_PUBLIC");
        }

        var detail = MapToDetailDto(dashboard);
        return ApiResponse<DashboardDetailDto>.Ok(detail);
    }

    #region Private Methods

    private static DashboardDetailDto MapToDetailDto(Dashboard dashboard)
    {
        return new DashboardDetailDto
        {
            DashboardId = dashboard.DashboardId,
            Name = dashboard.Name,
            Description = dashboard.Description,
            Theme = dashboard.Theme,
            RefreshInterval = dashboard.RefreshInterval,
            IsPublic = dashboard.IsPublic,
            LayoutConfig = dashboard.LayoutConfig,
            ThemeConfig = dashboard.ThemeConfig,
            CreatedTime = dashboard.CreatedTime,
            UpdatedTime = dashboard.UpdatedTime,
            WidgetCount = dashboard.Widgets.Count,
            Widgets = dashboard.Widgets.Select(w => new DashboardWidgetDto
            {
                WidgetId = w.WidgetId,
                DashboardId = w.DashboardId,
                ReportId = w.ReportId,
                ReportName = w.Report?.ReportName ?? "",
                WidgetType = w.WidgetType,
                Title = w.Title,
                PositionX = w.PositionX,
                PositionY = w.PositionY,
                Width = w.Width,
                Height = w.Height,
                DataConfig = w.DataConfig,
                StyleConfig = w.StyleConfig,
                Rules = w.Rules.Select(r => new WidgetRuleDto
                {
                    RuleId = r.RuleId,
                    WidgetId = r.WidgetId,
                    RuleName = r.RuleName,
                    Field = r.Field,
                    Operator = r.Operator,
                    Value = r.Value,
                    ActionType = r.ActionType,
                    ActionValue = r.ActionValue,
                    Priority = r.Priority
                }).ToList()
            }).ToList()
        };
    }

    private List<DashboardWidget> GenerateWidgetsFromReport(Report report)
    {
        var widgets = new List<DashboardWidget>();
        var fields = report.Fields.ToList();
        int posX = 0;
        int posY = 0;

        // 生成表格组件（放在上方）
        widgets.Add(new DashboardWidget
        {
            ReportId = report.ReportId,
            WidgetType = "table",
            Title = report.ReportName,
            PositionX = 0,
            PositionY = 0,
            Width = 12,
            Height = 6,
            StyleConfig = JsonSerializer.Serialize(new { overflowMode = "paginate", pageInterval = 10, pageSize = 20 }),
            CreatedTime = DateTime.UtcNow
        });

        posY += 6;

        // 分析字段，智能生成组件
        var numericFields = fields.Where(f => f.DataType == "decimal" || f.DataType == "int" || f.DataType == "double").ToList();
        var dateFields = fields.Where(f => f.DataType == "datetime" || f.DataType == "date").ToList();
        var stringFields = fields.Where(f => f.DataType == "string" || f.DataType == "nvarchar").ToList();

        // 如果有进度/完成率字段，生成状态灯
        var progressField = fields.FirstOrDefault(f =>
            f.FieldName.ToLower().Contains("progress") ||
            f.FieldName.ToLower().Contains("完成率") ||
            f.FieldName.ToLower().Contains("进度"));

        if (progressField != null)
        {
            widgets.Add(new DashboardWidget
            {
                ReportId = report.ReportId,
                WidgetType = "status-light",
                Title = "完成状态",
                PositionX = 0,
                PositionY = posY,
                Width = 4,
                Height = 2,
                Rules = new List<WidgetRule>
                {
                    new WidgetRule { Field = progressField.FieldName, Operator = "lt", Value = "70", ActionType = "setColor", ActionValue = "#E6A23C", Priority = 10, RuleName = "黄灯" },
                    new WidgetRule { Field = progressField.FieldName, Operator = "lt", Value = "100", ActionType = "setColor", ActionValue = "#F56C6C", Priority = 20, RuleName = "红灯" },
                    new WidgetRule { Field = progressField.FieldName, Operator = "gte", Value = "100", ActionType = "setColor", ActionValue = "#67C23A", Priority = 30, RuleName = "绿灯" }
                },
                CreatedTime = DateTime.UtcNow
            });
        }

        return widgets;
    }

    #endregion
}
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/DashboardService.cs
git commit -m "feat(dashboard): implement DashboardService"
```

---

## Phase 4: 后端控制器

### Task 4.1: 创建 DashboardController

**Files:**
- Create: `backend/src/DataForgeStudio.Api/Controllers/DashboardController.cs`

**Step 1: 创建控制器文件**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 大屏管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// 获取大屏列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponse<PagedResponse<DashboardDto>>> GetDashboards(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? name = null)
    {
        var request = new PagedRequest { PageIndex = page, PageSize = pageSize };
        return await _dashboardService.GetDashboardsAsync(request, name);
    }

    /// <summary>
    /// 获取大屏详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<DashboardDetailDto>> GetDashboard(int id)
    {
        return await _dashboardService.GetDashboardByIdAsync(id);
    }

    /// <summary>
    /// 创建大屏
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<DashboardDto>> CreateDashboard([FromBody] CreateDashboardRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return ApiResponse<DashboardDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _dashboardService.CreateDashboardAsync(request, userId);
    }

    /// <summary>
    /// 更新大屏
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse> UpdateDashboard(int id, [FromBody] CreateDashboardRequest request)
    {
        return await _dashboardService.UpdateDashboardAsync(id, request);
    }

    /// <summary>
    /// 删除大屏
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse> DeleteDashboard(int id)
    {
        return await _dashboardService.DeleteDashboardAsync(id);
    }

    /// <summary>
    /// 添加组件
    /// </summary>
    [HttpPost("{id}/widgets")]
    public async Task<ApiResponse<DashboardWidgetDto>> AddWidget(int id, [FromBody] CreateWidgetRequest request)
    {
        return await _dashboardService.AddWidgetAsync(id, request);
    }

    /// <summary>
    /// 更新组件
    /// </summary>
    [HttpPut("{id}/widgets/{widgetId}")]
    public async Task<ApiResponse> UpdateWidget(int id, int widgetId, [FromBody] CreateWidgetRequest request)
    {
        return await _dashboardService.UpdateWidgetAsync(id, widgetId, request);
    }

    /// <summary>
    /// 删除组件
    /// </summary>
    [HttpDelete("{id}/widgets/{widgetId}")]
    public async Task<ApiResponse> DeleteWidget(int id, int widgetId)
    {
        return await _dashboardService.DeleteWidgetAsync(id, widgetId);
    }

    /// <summary>
    /// 批量更新组件位置
    /// </summary>
    [HttpPut("{id}/widgets/positions")]
    public async Task<ApiResponse> UpdateWidgetPositions(int id, [FromBody] List<WidgetPositionRequest> positions)
    {
        return await _dashboardService.UpdateWidgetPositionsAsync(id, positions);
    }

    /// <summary>
    /// 一键转换（从报表生成大屏）
    /// </summary>
    [HttpPost("convert")]
    public async Task<ApiResponse<DashboardDetailDto>> ConvertFromReport([FromBody] ConvertReportToDashboardRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return ApiResponse<DashboardDetailDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _dashboardService.ConvertFromReportAsync(request.ReportId, request.DashboardName, userId);
    }

    /// <summary>
    /// 获取大屏数据（用于刷新）
    /// </summary>
    [HttpGet("{id}/data")]
    public async Task<ApiResponse<DashboardDataDto>> GetDashboardData(int id)
    {
        return await _dashboardService.GetDashboardDataAsync(id);
    }
}
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Controllers/DashboardController.cs
git commit -m "feat(dashboard): add DashboardController"
```

---

### Task 4.2: 创建 PublicController（公开访问）

**Files:**
- Create: `backend/src/DataForgeStudio.Api/Controllers/PublicController.cs`

**Step 1: 创建控制器文件**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 公开访问控制器（无需认证）
/// </summary>
[ApiController]
[Route("public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<PublicController> _logger;

    public PublicController(IDashboardService dashboardService, ILogger<PublicController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// 获取公开大屏配置 + 数据
    /// </summary>
    [HttpGet("d/{id}")]
    public async Task<ApiResponse<DashboardDetailDto>> GetPublicDashboard(int id)
    {
        return await _dashboardService.GetPublicDashboardAsync(id);
    }

    /// <summary>
    /// 获取公开大屏数据（用于刷新）
    /// </summary>
    [HttpGet("d/{id}/data")]
    public async Task<ApiResponse<DashboardDataDto>> GetPublicDashboardData(int id)
    {
        // 先验证是否公开
        var dashboardResult = await _dashboardService.GetPublicDashboardAsync(id);
        if (!dashboardResult.Success)
        {
            return ApiResponse<DashboardDataDto>.Fail(dashboardResult.Message, dashboardResult.ErrorCode);
        }

        return await _dashboardService.GetDashboardDataAsync(id);
    }
}
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Controllers/PublicController.cs
git commit -m "feat(dashboard): add PublicController for public dashboard access"
```

---

### Task 4.3: 注册服务到 Program.cs

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Program.cs`

**Step 1: 添加服务注册**

在服务注册区域添加：

```csharp
// 注册大屏服务
builder.Services.AddScoped<IDashboardService, DashboardService>();
```

**Step 2: 验证编译和运行**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Program.cs
git commit -m "feat(dashboard): register DashboardService in DI"
```

---

## Phase 5: 前端基础

### Task 5.1: 安装前端依赖

**Step 1: 安装 vue-grid-layout**

Run: `cd frontend && npm install vue-grid-layout@3.0.0 --save`

**Step 2: Commit**

```bash
git add frontend/package.json frontend/package-lock.json
git commit -m "feat(dashboard): add vue-grid-layout dependency"
```

---

### Task 5.2: 创建 Dashboard API

**Files:**
- Create: `frontend/src/api/dashboard.js`

**Step 1: 创建 API 文件**

```javascript
import request from './request'

// 获取大屏列表
export function getDashboards(params) {
  return request({
    url: '/api/dashboard',
    method: 'get',
    params
  })
}

// 获取大屏详情
export function getDashboard(id) {
  return request({
    url: `/api/dashboard/${id}`,
    method: 'get'
  })
}

// 创建大屏
export function createDashboard(data) {
  return request({
    url: '/api/dashboard',
    method: 'post',
    data
  })
}

// 更新大屏
export function updateDashboard(id, data) {
  return request({
    url: `/api/dashboard/${id}`,
    method: 'put',
    data
  })
}

// 删除大屏
export function deleteDashboard(id) {
  return request({
    url: `/api/dashboard/${id}`,
    method: 'delete'
  })
}

// 添加组件
export function addWidget(dashboardId, data) {
  return request({
    url: `/api/dashboard/${dashboardId}/widgets`,
    method: 'post',
    data
  })
}

// 更新组件
export function updateWidget(dashboardId, widgetId, data) {
  return request({
    url: `/api/dashboard/${dashboardId}/widgets/${widgetId}`,
    method: 'put',
    data
  })
}

// 删除组件
export function deleteWidget(dashboardId, widgetId) {
  return request({
    url: `/api/dashboard/${dashboardId}/widgets/${widgetId}`,
    method: 'delete'
  })
}

// 批量更新组件位置
export function updateWidgetPositions(dashboardId, positions) {
  return request({
    url: `/api/dashboard/${dashboardId}/widgets/positions`,
    method: 'put',
    data: positions
  })
}

// 一键转换
export function convertFromReport(reportId, dashboardName) {
  return request({
    url: '/api/dashboard/convert',
    method: 'post',
    data: { reportId, dashboardName }
  })
}

// 获取大屏数据
export function getDashboardData(id) {
  return request({
    url: `/api/dashboard/${id}/data`,
    method: 'get'
  })
}

// 公开访问 - 获取大屏配置
export function getPublicDashboard(id) {
  return request({
    url: `/public/d/${id}`,
    method: 'get'
  })
}

// 公开访问 - 获取大屏数据
export function getPublicDashboardData(id) {
  return request({
    url: `/public/d/${id}/data`,
    method: 'get'
  })
}
```

**Step 2: Commit**

```bash
git add frontend/src/api/dashboard.js
git commit -m "feat(dashboard): add dashboard API functions"
```

---

### Task 5.3: 创建 Dashboard Store

**Files:**
- Create: `frontend/src/stores/dashboard.js`

**Step 1: 创建 Store 文件**

```javascript
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getDashboard, getDashboardData } from '@/api/dashboard'

export const useDashboardStore = defineStore('dashboard', () => {
  const currentDashboard = ref(null)
  const dashboardData = ref(null)
  const loading = ref(false)

  // 加载大屏配置
  async function loadDashboard(id) {
    loading.value = true
    try {
      const res = await getDashboard(id)
      if (res.success) {
        currentDashboard.value = res.data
        return res.data
      }
      return null
    } finally {
      loading.value = false
    }
  }

  // 加载大屏数据
  async function loadDashboardData(id) {
    try {
      const res = await getDashboardData(id)
      if (res.success) {
        dashboardData.value = res.data
        return res.data
      }
      return null
    } catch (error) {
      console.error('加载大屏数据失败:', error)
      return null
    }
  }

  // 清除当前大屏
  function clearDashboard() {
    currentDashboard.value = null
    dashboardData.value = null
  }

  return {
    currentDashboard,
    dashboardData,
    loading,
    loadDashboard,
    loadDashboardData,
    clearDashboard
  }
})
```

**Step 2: Commit**

```bash
git add frontend/src/stores/dashboard.js
git commit -m "feat(dashboard): add dashboard Pinia store"
```

---

### Task 5.4: 更新路由配置

**Files:**
- Modify: `frontend/src/router/index.js`

**Step 1: 添加大屏路由**

在 routes 数组中添加：

```javascript
  // 大屏模块
  {
    path: '/dashboard',
    name: 'Dashboard',
    redirect: '/dashboard/list'
  },
  {
    path: '/dashboard/list',
    name: 'DashboardList',
    component: () => import('../views/dashboard/DashboardList.vue'),
    meta: { title: '大屏管理', requiresAuth: true, permission: 'dashboard:view' }
  },
  {
    path: '/dashboard/designer/:id?',
    name: 'DashboardDesigner',
    component: () => import('../views/dashboard/DashboardDesigner.vue'),
    meta: { title: '大屏设计器', requiresAuth: true, permission: 'dashboard:edit' }
  },
  {
    path: '/dashboard/view/:id',
    name: 'DashboardView',
    component: () => import('../views/dashboard/DashboardView.vue'),
    meta: { title: '大屏展示', requiresAuth: true, permission: 'dashboard:view' }
  },
  {
    path: '/public/d/:id',
    name: 'PublicDashboard',
    component: () => import('../views/dashboard/PublicDashboard.vue'),
    meta: { title: '大屏', requiresAuth: false }
  },
```

**Step 2: Commit**

```bash
git add frontend/src/router/index.js
git commit -m "feat(dashboard): add dashboard routes"
```

---

## Phase 6: 前端页面实现

### Task 6.1: 创建大屏列表页面

**Files:**
- Create: `frontend/src/views/dashboard/DashboardList.vue`

**Step 1: 创建页面文件**

（由于文件较长，请参考项目现有页面模式实现大屏列表的 CRUD 操作）

**Step 2: Commit**

```bash
git add frontend/src/views/dashboard/DashboardList.vue
git commit -m "feat(dashboard): add DashboardList page"
```

---

### Task 6.2: 创建大屏设计器页面

**Files:**
- Create: `frontend/src/views/dashboard/DashboardDesigner.vue`
- Create: `frontend/src/views/dashboard/DashboardView.vue`
- Create: `frontend/src/views/dashboard/PublicDashboard.vue`
- Create: `frontend/src/views/dashboard/WidgetConfigPanel.vue`

（详细实现请参考 dashboard-engine-builder 和 fullscreen-display-builder 技能）

---

## Phase 7: 组件库

### Task 7.1: 创建基础组件

**Files:**
- Create: `frontend/src/components/dashboard/DashboardTable.vue`
- Create: `frontend/src/components/dashboard/DashboardCardNumber.vue`
- Create: `frontend/src/components/dashboard/DashboardProgressBar.vue`
- Create: `frontend/src/components/dashboard/DashboardStatusLight.vue`
- Create: `frontend/src/components/dashboard/DashboardChart.vue`
- Create: `frontend/src/components/dashboard/DashboardGauge.vue`

（详细实现请参考 chart-component-builder 技能）

---

## Phase 8: 集成测试与完善

### Task 8.1: 运行数据库迁移

**Step 1: 执行 SQL 迁移脚本**

Run: 在 SQL Server Management Studio 中执行 `database/migrations/V1.1.0_Dashboard.sql`

**Step 2: 验证表创建**

```sql
SELECT * FROM Dashboards;
SELECT * FROM DashboardWidgets;
SELECT * FROM WidgetRules;
```

---

### Task 8.2: 端到端测试

**Step 1: 启动后端**

Run: `cd backend && dotnet run --project src/DataForgeStudio.Api`

**Step 2: 启动前端**

Run: `cd frontend && npm run dev`

**Step 3: 测试功能清单**

- [ ] 创建大屏
- [ ] 编辑大屏
- [ ] 删除大屏
- [ ] 添加组件
- [ ] 拖拽布局
- [ ] 条件样式配置
- [ ] 一键转换
- [ ] 全屏展示
- [ ] 公开访问
- [ ] 自动刷新

---

## 实施注意事项

1. **SQL Server 2005 兼容性**：所有 SQL 使用兼容语法，避免 `OFFSET/FETCH`、`SEQUENCE` 等

2. **权限检查**：确保新权限已添加到 Permissions 表并分配给管理员角色

3. **前端依赖**：vue-grid-layout 需要 Vue 3 兼容版本

4. **ECharts 集成**：图表组件需要根据 DataConfig 动态配置

5. **公开访问安全**：确保只有 `IsPublic=1` 的大屏可以通过公开 API 访问

---

**计划完成，保存到:** `docs/plans/2026-03-02-dashboard-system-plan.md`
