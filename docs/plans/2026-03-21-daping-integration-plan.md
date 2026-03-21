# Daping 高级大屏模块集成实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 go-view 大屏设计器集成到 DataForge 前端，替换现有简单大屏系统。

**Architecture:** 将 daping 代码作为子模块整体迁移到 frontend/src/daping，保持原有结构。复用 DataForge 认证，新建专用后端 API。移除不需要的功能（地图、模板市场）。

**Tech Stack:** Vue 3 + TypeScript + Vite + Naive UI + ECharts + ASP.NET Core 8.0

---

## 齶段 1: 巻加 DbSet 到 DbContext

### Task 1.1: 添加 DapingProjects DbSet

**Files:**
- Create: `backend/src/DataForgeStudio.Data/Migrations/20260321000000_AddDapingProjectsTable.cs`
- Create: `backend/src/DataForgeStudio.Domain/Entities/DapingProject.cs`

**Step 1: 创建迁移文件**

Create `backend/src/DataForgeStudio.Data/Migrations/20260321000000_AddDapingProjectsTable.cs`

**Step 2: 运行迁移**

Run: `dotnet ef migrations add AddDapingProjectsTable --project src/DataForgeStudio.Data --startup-project src/DataForgeStudio.Api`

Expected: 迁移文件创建成功

---

### Task 1.2: 创建 DapingProject 实体类
**Files:**
- Create: `backend/src/DataForgeStudio.Domain/Entities/DapingProject.cs`

**Step 1: 创建实体类文件**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 高级大屏项目表
/// </summary>
[Table("DapingProjects")]
public class DapingProject
{
    /// <summary>
    /// 项目ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProjectId { get; set; }

    /// <summary>
    /// 项目名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 状态： 1=草稿 2=发布
    /// </summary>
    public int State { get; set; } = 1;

    /// <summary>
    /// 完整项目配置（JSON）
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 公开访问 URL 标识
    /// </summary>
    [MaxLength(50)]
    public string? PublicUrl { get; set; }

    /// <summary>
    /// 创建人ID
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 导航属性 - 创建人
    /// </summary>
    [ForeignKey(nameof(CreatedBy))]
    public virtual User? Creator { get; set; }
}
