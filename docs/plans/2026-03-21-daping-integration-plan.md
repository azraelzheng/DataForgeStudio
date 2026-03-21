# Daping 高级大屏模块集成实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 go-view 大屏设计器集成到 DataForge 前端，替换现有简单大屏系统。

**Architecture:** 将 daping 作为子模块整体迁移到 frontend/src/daping，保持原有结构，复用 DataForge 认证，新建专用后端 API。

**Tech Stack:** Vue 3 + TypeScript + Vite + Naive UI + ECharts + ASP.NET Core 8.0

---

## 前置条件

- 确保 `daping/` 目录存在于项目根目录
- 确保后端项目可正常运行
- 确保前端项目可正常运行

---

## 阶段 1: 基础代码迁移

### Task 1.1: 创建新分支

**Files:**
- N/A

**Step 1: 创建并切换到新分支**

```bash
cd H:/DataForge
git checkout -b feature/daping-integration
git push -u origin feature/daping-integration
```

**预期输出:** 新分支创建成功

---

### Task 1.2: 复制 daping 代码到 frontend/src

**Files:**
- Create: `frontend/src/daping/` (整个目录)

**Step 1: 复制代码**

```bash
cd H:/DataForge
cp -r daping frontend/src/daping
```

**Step 2: 验证复制成功**

```bash
ls -la frontend/src/daping/
```

**预期输出:** 显示 `api/`, `components/`, `packages/`, `views/` 等目录

---

### Task 1.3: 移除地图组件

**Files:**
- Delete: `frontend/src/daping/packages/components/Maps/`

**Step 1: 删除地图组件目录**

```bash
rm -rf H:/DataForge/frontend/src/daping/packages/components/Maps
```

**Step 2: 验证删除成功**

```bash
ls H:/DataForge/frontend/src/daping/packages/components/
```

**预期输出:** 不包含 `Maps` 目录

---

### Task 1.4: 移除模板市场

**Files:**
- Delete: `frontend/src/daping/views/project/templateMarket/`

**Step 1: 删除模板市场目录**

```bash
rm -rf H:/DataForge/frontend/src/daping/views/project/templateMarket
```

**Step 2: 验证删除成功**

```bash
ls H:/DataForge/frontend/src/daping/views/project/
```

**预期输出:** 不包含 `templateMarket` 目录

---

### Task 1.5: 移除登录页面

**Files:**
- Delete: `frontend/src/daping/views/login/`
- Delete: `frontend/src/daping/components/Verifition/`

**Step 1: 删除登录页面和验证码组件**

```bash
rm -rf H:/DataForge/frontend/src/daping/views/login
rm -rf H:/DataForge/frontend/src/daping/components/Verifition
```

**Step 2: 验证删除成功**

```bash
ls H:/DataForge/frontend/src/daping/views/
ls H:/DataForge/frontend/src/daping/components/
```

**预期输出:** 不包含 `login` 和 `Verifition` 目录

---

### Task 1.6: 修改 package.json 移除不需要的依赖

**Files:**
- Modify: `frontend/src/daping/package.json`

**Step 1: 读取当前 package.json**

```bash
cat H:/DataForge/frontend/src/daping/package.json
```

**Step 2: 移除地图相关依赖**

修改 `frontend/src/daping/package.json`，删除以下依赖：
- `@amap/amap-jsapi-loader`
- `cesium`
- `xbsj-xe2`
- `xbsj-xe2-assets`

同时修改以下配置：
- `name` 改为 `daping`
- 移除 `scripts` 中的独立脚本（保留 `build` 即可）

**Step 3: 验证修改**

```bash
grep -E "amap|cesium|xe2" H:/DataForge/frontend/src/daping/package.json
```

**预期输出:** 无匹配（表示已移除）

---

### Task 1.7: 合并依赖到主 package.json

**Files:**
- Modify: `frontend/package.json`

**Step 1: 读取需要合并的依赖**

```bash
cat H:/DataForge/frontend/src/daping/package.json | grep -A 100 '"dependencies"'
```

**Step 2: 将以下依赖添加到 `frontend/package.json`**

需要添加的主要依赖：
```json
{
  "dependencies": {
    "naive-ui": "^2.34.3",
    "vue-echarts": "^6.0.2",
    "echarts-liquidfill": "^3.0.0",
    "echarts-wordcloud": "^2.0.0",
    "echarts-stat": "^1.2.0",
    "@vicons/ionicons5": "^0.12.0",
    "gsap": "^3.11.5",
    "animate.css": "^4.1.1",
    "monaco-editor": "^0.34.1",
    "screenfull": "^6.0.2",
    "vuedraggable": "^4.1.0",
    "crypto-js": "^4.1.1",
    "vue-i18n": "^9.1.9",
    "@vueuse/core": "^8.6.0",
    "highlight.js": "^11.5.1"
  }
}
```

**Step 3: 安装依赖**

```bash
cd H:/DataForge/frontend
npm install
```

**预期输出:** 依赖安装成功，无错误

---

### Task 1.8: 配置 Vite 别名

**Files:**
- Modify: `frontend/vite.config.ts`

**Step 1: 读取当前 vite.config.ts**

```bash
cat H:/DataForge/frontend/vite.config.ts
```

**Step 2: 添加 daping 路径别名**

在 `resolve.alias` 中添加：

```typescript
resolve: {
  alias: {
    '@': path.resolve(__dirname, 'src'),
    '/@/': path.resolve(__dirname, 'src'),
    'vue-i18n': 'vue-i18n/dist/vue-i18n.cjs.js'
  }
}
```

**Step 3: 添加 Naive UI 样式优化**

在 `vite.config.ts` 中添加：

```typescript
optimizeDeps: {
  include: [
    'naive-ui',
    'echarts',
    'vue-echarts',
    '@vueuse/core'
  ]
}
```

---

### Task 1.9: 提交阶段 1 代码

**Files:**
- N/A

**Step 1: 暂存所有更改**

```bash
cd H:/DataForge
git add frontend/src/daping/
git add frontend/package.json
git add frontend/vite.config.ts
```

**Step 2: 提交**

```bash
git commit -m "$(cat <<'EOF'
feat(daping): add go-view dashboard designer module

- Copy daping code to frontend/src/daping
- Remove map components (Maps)
- Remove template market
- Remove login pages
- Remove unused dependencies (amap, cesium)
- Add required dependencies to main package.json
- Configure Vite aliases

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## 阶段 2: 认证集成

### Task 2.1: 重写 API 拦截器

**Files:**
- Modify: `frontend/src/daping/api/axios.ts`

**Step 1: 读取当前 axios 配置**

```bash
cat H:/DataForge/frontend/src/daping/api/axios.ts
```

**Step 2: 重写 axios.ts**

完全替换文件内容为：

```typescript
import axios from 'axios'
import { useUserStore } from '@/stores/user'
import router from '@/router'

const service = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api',
  timeout: 30000
})

// 请求拦截器：添加 DataForge JWT Token
service.interceptors.request.use(
  (config) => {
    const userStore = useUserStore()
    if (userStore.token) {
      config.headers.Authorization = `Bearer ${userStore.token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// 响应拦截器：处理 401 跳转登录页
service.interceptors.response.use(
  (response) => {
    return response
  },
  (error) => {
    if (error.response?.status === 401) {
      // 清除用户信息
      const userStore = useUserStore()
      userStore.logout()
      // 跳转到 DataForge 登录页
      router.push('/login')
    }
    return Promise.reject(error)
  }
)

export default service
```

---

### Task 2.2: 更新 API 路径配置

**Files:**
- Modify: `frontend/src/daping/api/path/project.api.ts`

**Step 1: 读取当前 API 路径**

```bash
cat H:/DataForge/frontend/src/daping/api/path/project.api.ts
```

**Step 2: 修改 API 路径为 DataForge 格式**

修改 `project.api.ts`，将 API 路径从：
- `/report/go-view/project/...`
改为：
- `/api/daping/...`

示例修改：

```typescript
import { http } from '../http'

// 项目列表
export const projectListApi = (data: object) => {
  return http.post('/api/daping/projects/list', data)
}

// 创建项目
export const createProjectApi = (data: object) => {
  return http.post('/api/daping/projects', data)
}

// 获取项目详情
export const fetchProjectApi = (id: number) => {
  return http.get(`/api/daping/projects/${id}`)
}

// 保存项目
export const saveProjectApi = (data: object) => {
  return http.put('/api/daping/projects', data)
}

// 删除项目
export const deleteProjectApi = (data: object) => {
  return http.delete('/api/daping/projects', data)
}
```

---

### Task 2.3: 更新环境变量

**Files:**
- Delete: `frontend/src/daping/.env`
- Modify: `frontend/.env` (如果不存在则创建)

**Step 1: 删除 daping 的 .env**

```bash
rm H:/DataForge/frontend/src/daping/.env
```

**Step 2: 确保 frontend/.env 配置正确**

```bash
cat H:/DataForge/frontend/.env
```

确认包含：
```
VITE_API_URL=/api
```

---

### Task 2.4: 修改 SystemStore 使用 DataForge 用户信息

**Files:**
- Modify: `frontend/src/daping/store/modules/systemStore/systemStore.ts`

**Step 1: 读取当前 systemStore**

```bash
cat H:/DataForge/frontend/src/daping/store/modules/systemStore/systemStore.ts
```

**Step 2: 修改用户信息获取方式**

修改为从 DataForge userStore 获取用户信息：

```typescript
import { defineStore } from 'pinia'
import { useUserStore } from '@/stores/user'

export const useSystemStore = defineStore({
  id: 'daping-system',
  state: (): SystemStateType => ({
    userInfo: null,
    tenantId: ''
  }),
  getters: {
    getUserInfo(): UserInfoType {
      // 从 DataForge userStore 获取用户信息
      const userStore = useUserStore()
      return {
        nickName: userStore.userInfo?.username || '',
        username: userStore.userInfo?.username || '',
        userId: userStore.userInfo?.userId || 0
      }
    }
  },
  actions: {
    setUserInfo(data: UserInfoType) {
      this.userInfo = data
    }
  }
})
```

---

### Task 2.5: 移除租户和验证码相关代码

**Files:**
- Modify: `frontend/src/daping/views/project/index.vue`
- Modify: `frontend/src/daping/hooks/useSystemInit.hook.ts`

**Step 1: 搜索租户相关代码**

```bash
grep -r "tenantId\|TENANT\|租户" H:/DataForge/frontend/src/daping/
```

**Step 2: 移除或注释租户相关代码**

在 `useSystemInit.hook.ts` 中移除租户初始化逻辑。

---

### Task 2.6: 提交阶段 2 代码

**Files:**
- N/A

**Step 1: 暂存所有更改**

```bash
cd H:/DataForge
git add frontend/src/daping/
git add frontend/.env
```

**Step 2: 提交**

```bash
git commit -m "$(cat <<'EOF'
feat(daping): integrate with DataForge authentication

- Rewrite axios interceptors to use DataForge JWT
- Update API paths to /api/daping/*
- Remove daping .env, use main .env
- Modify systemStore to use DataForge userStore
- Remove tenant and captcha related code

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## 阶段 3: 后端开发

### Task 3.1: 创建数据库迁移

**Files:**
- Create: `backend/src/DataForgeStudio.Data/Migrations/20260321000000_AddDapingProjectsTable.cs`

**Step 1: 创建迁移文件**

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataForgeStudio.Data.Migrations;

public partial class AddDapingProjectsTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DapingProjects",
            columns: table => new
            {
                ProjectId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                State = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PublicUrl = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                CreatedBy = table.Column<int>(type: "int", nullable: true),
                CreatedTime = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                UpdatedTime = table.Column<DateTime>(type: "datetime", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DapingProjects", x => x.ProjectId);
                table.ForeignKey(
                    name: "FK_DapingProjects_Users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DapingProjects_CreatedBy",
            table: "DapingProjects",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_DapingProjects_PublicUrl",
            table: "DapingProjects",
            column: "PublicUrl",
            unique: true,
            filter: "[PublicUrl] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DapingProjects");
    }
}
```

**Step 2: 运行迁移**

```bash
cd H:/DataForge/backend
dotnet ef migrations add AddDapingProjectsTable --project src/DataForgeStudio.Data --startup-project src/DataForgeStudio.Api
```

---

### Task 3.2: 创建 DapingProject Entity

**Files:**
- Create: `backend/src/DataForgeStudio.Domain/Entities/DapingProject.cs`

**Step 1: 创建实体类**

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
    /// 状态：1=草稿 2=发布
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
```

---

### Task 3.3: 添加 DbSet 到 DbContext

**Files:**
- Modify: `backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs`

**Step 1: 读取当前 DbContext**

```bash
grep -A 5 "DbSet" H:/DataForge/backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs | head -30
```

**Step 2: 添加 DapingProjects DbSet**

在 `DataForgeStudioDbContext.cs` 中添加：

```csharp
/// <summary>
/// 高级大屏项目
/// </summary>
public DbSet<DapingProject> DapingProjects { get; set; }
```

---

### Task 3.4: 创建 DTO 类

**Files:**
- Create: `backend/src/DataForgeStudio.Domain/DTOs/DapingDto.cs`

**Step 1: 创建 DTO**

```csharp
using System.ComponentModel.DataAnnotations;

namespace DataForgeStudio.Domain.DTOs;

/// <summary>
/// 大屏项目 DTO
/// </summary>
public class DapingProjectDto
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int State { get; set; }
    public string? PublicUrl { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}

/// <summary>
/// 大屏项目详情 DTO（包含完整配置）
/// </summary>
public class DapingProjectDetailDto : DapingProjectDto
{
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 创建大屏项目请求
/// </summary>
public class CreateDapingProjectRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 更新大屏项目请求
/// </summary>
public class UpdateDapingProjectRequest
{
    public string? Name { get; set; }

    public string? Content { get; set; }
}

/// <summary>
/// 大屏项目列表请求
/// </summary>
public class DapingProjectListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Name { get; set; }
    public int? State { get; set; }
}
```

---

### Task 3.5: 创建 IDapingService 接口

**Files:**
- Create: `backend/src/DataForgeStudio.Core/Interfaces/IDapingService.cs`

**Step 1: 创建服务接口**

```csharp
using DataForgeStudio.Domain.DTOs;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 高级大屏服务接口
/// </summary>
public interface IDapingService
{
    /// <summary>
    /// 获取项目列表
    /// </summary>
    Task<ApiResponse<PagedResponse<DapingProjectDto>>> GetProjectsAsync(DapingProjectListRequest request, int userId);

    /// <summary>
    /// 获取项目详情
    /// </summary>
    Task<ApiResponse<DapingProjectDetailDto>> GetProjectByIdAsync(int projectId, int userId);

    /// <summary>
    /// 创建项目
    /// </summary>
    Task<ApiResponse<DapingProjectDto>> CreateProjectAsync(CreateDapingProjectRequest request, int userId);

    /// <summary>
    /// 更新项目
    /// </summary>
    Task<ApiResponse> UpdateProjectAsync(int projectId, UpdateDapingProjectRequest request, int userId);

    /// <summary>
    /// 删除项目
    /// </summary>
    Task<ApiResponse> DeleteProjectAsync(int projectId, int userId);

    /// <summary>
    /// 发布项目
    /// </summary>
    Task<ApiResponse<DapingProjectDto>> PublishProjectAsync(int projectId, int userId);

    /// <summary>
    /// 取消发布
    /// </summary>
    Task<ApiResponse<DapingProjectDto>> UnpublishProjectAsync(int projectId, int userId);

    /// <summary>
    /// 获取公开项目（无需认证）
    /// </summary>
    Task<ApiResponse<DapingProjectDetailDto>> GetPublicProjectAsync(string publicUrl);
}
```

---

### Task 3.6: 实现 DapingService

**Files:**
- Create: `backend/src/DataForgeStudio.Core/Services/DapingService.cs`

**Step 1: 创建服务实现**

```csharp
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.DTOs;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 高级大屏服务实现
/// </summary>
public class DapingService : IDapingService
{
    private readonly DataForgeStudioDbContext _context;

    public DapingService(DataForgeStudioDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PagedResponse<DapingProjectDto>>> GetProjectsAsync(
        DapingProjectListRequest request, int userId)
    {
        var query = _context.DapingProjects
            .Include(p => p.Creator)
            .Where(p => p.CreatedBy == userId);

        if (!string.IsNullOrEmpty(request.Name))
        {
            query = query.Where(p => p.Name.Contains(request.Name));
        }

        if (request.State.HasValue)
        {
            query = query.Where(p => p.State == request.State.Value);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.UpdatedTime ?? p.CreatedTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new DapingProjectDto
            {
                ProjectId = p.ProjectId,
                Name = p.Name,
                State = p.State,
                PublicUrl = p.PublicUrl,
                CreatedBy = p.CreatedBy,
                CreatorName = p.Creator != null ? p.Creator.Username : null,
                CreatedTime = p.CreatedTime,
                UpdatedTime = p.UpdatedTime
            })
            .ToListAsync();

        return ApiResponse<PagedResponse<DapingProjectDto>>.Success(
            new PagedResponse<DapingProjectDto>(items, total, request.Page, request.PageSize));
    }

    public async Task<ApiResponse<DapingProjectDetailDto>> GetProjectByIdAsync(int projectId, int userId)
    {
        var project = await _context.DapingProjects
            .Include(p => p.Creator)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CreatedBy == userId);

        if (project == null)
        {
            return ApiResponse<DapingProjectDetailDto>.Fail("项目不存在或无权访问");
        }

        return ApiResponse<DapingProjectDetailDto>.Success(MapToDetailDto(project));
    }

    public async Task<ApiResponse<DapingProjectDto>> CreateProjectAsync(
        CreateDapingProjectRequest request, int userId)
    {
        var project = new DapingProject
        {
            Name = request.Name,
            Content = request.Content,
            State = 1,
            CreatedBy = userId,
            CreatedTime = DateTime.UtcNow
        };

        _context.DapingProjects.Add(project);
        await _context.SaveChangesAsync();

        return ApiResponse<DapingProjectDto>.Success(MapToDto(project));
    }

    public async Task<ApiResponse> UpdateProjectAsync(
        int projectId, UpdateDapingProjectRequest request, int userId)
    {
        var project = await _context.DapingProjects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CreatedBy == userId);

        if (project == null)
        {
            return ApiResponse.Fail("项目不存在或无权访问");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            project.Name = request.Name;
        }

        if (!string.IsNullOrEmpty(request.Content))
        {
            project.Content = request.Content;
        }

        project.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse.Success("更新成功");
    }

    public async Task<ApiResponse> DeleteProjectAsync(int projectId, int userId)
    {
        var project = await _context.DapingProjects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CreatedBy == userId);

        if (project == null)
        {
            return ApiResponse.Fail("项目不存在或无权访问");
        }

        _context.DapingProjects.Remove(project);
        await _context.SaveChangesAsync();

        return ApiResponse.Success("删除成功");
    }

    public async Task<ApiResponse<DapingProjectDto>> PublishProjectAsync(int projectId, int userId)
    {
        var project = await _context.DapingProjects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CreatedBy == userId);

        if (project == null)
        {
            return ApiResponse<DapingProjectDto>.Fail("项目不存在或无权访问");
        }

        project.State = 2;
        project.PublicUrl ??= Guid.NewGuid().ToString("N")[..8].ToUpper();
        project.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<DapingProjectDto>.Success(MapToDto(project));
    }

    public async Task<ApiResponse<DapingProjectDto>> UnpublishProjectAsync(int projectId, int userId)
    {
        var project = await _context.DapingProjects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CreatedBy == userId);

        if (project == null)
        {
            return ApiResponse<DapingProjectDto>.Fail("项目不存在或无权访问");
        }

        project.State = 1;
        project.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<DapingProjectDto>.Success(MapToDto(project));
    }

    public async Task<ApiResponse<DapingProjectDetailDto>> GetPublicProjectAsync(string publicUrl)
    {
        var project = await _context.DapingProjects
            .FirstOrDefaultAsync(p => p.PublicUrl == publicUrl && p.State == 2);

        if (project == null)
        {
            return ApiResponse<DapingProjectDetailDto>.Fail("项目不存在或未发布");
        }

        return ApiResponse<DapingProjectDetailDto>.Success(MapToDetailDto(project));
    }

    private static DapingProjectDto MapToDto(DapingProject p) => new()
    {
        ProjectId = p.ProjectId,
        Name = p.Name,
        State = p.State,
        PublicUrl = p.PublicUrl,
        CreatedBy = p.CreatedBy,
        CreatedTime = p.CreatedTime,
        UpdatedTime = p.UpdatedTime
    };

    private static DapingProjectDetailDto MapToDetailDto(DapingProject p) => new()
    {
        ProjectId = p.ProjectId,
        Name = p.Name,
        State = p.State,
        Content = p.Content,
        PublicUrl = p.PublicUrl,
        CreatedBy = p.CreatedBy,
        CreatedTime = p.CreatedTime,
        UpdatedTime = p.UpdatedTime
    };
}
```

---

### Task 3.7: 注册服务

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Program.cs`

**Step 1: 添加服务注册**

在 `Program.cs` 的服务注册部分添加：

```csharp
builder.Services.AddScoped<IDapingService, DapingService>();
```

---

### Task 3.8: 创建 DapingController

**Files:**
- Create: `backend/src/DataForgeStudio.Api/Controllers/DapingController.cs`

**Step 1: 创建控制器**

```csharp
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Domain.DTOs;
using DataForgeStudio.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 高级大屏 API 控制器
/// </summary>
[ApiController]
[Route("api/daping")]
[Authorize]
public class DapingController : ControllerBase
{
    private readonly IDapingService _dapingService;

    public DapingController(IDapingService dapingService)
    {
        _dapingService = dapingService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// 获取项目列表
    /// </summary>
    [HttpPost("projects/list")]
    public async Task<ApiResponse<PagedResponse<DapingProjectDto>>> GetProjects(
        [FromBody] DapingProjectListRequest request)
    {
        return await _dapingService.GetProjectsAsync(request, GetCurrentUserId());
    }

    /// <summary>
    /// 获取项目详情
    /// </summary>
    [HttpGet("projects/{id}")]
    public async Task<ApiResponse<DapingProjectDetailDto>> GetProject(int id)
    {
        return await _dapingService.GetProjectByIdAsync(id, GetCurrentUserId());
    }

    /// <summary>
    /// 创建项目
    /// </summary>
    [HttpPost("projects")]
    public async Task<ApiResponse<DapingProjectDto>> CreateProject(
        [FromBody] CreateDapingProjectRequest request)
    {
        return await _dapingService.CreateProjectAsync(request, GetCurrentUserId());
    }

    /// <summary>
    /// 更新项目
    /// </summary>
    [HttpPut("projects/{id}")]
    public async Task<ApiResponse> UpdateProject(int id, [FromBody] UpdateDapingProjectRequest request)
    {
        return await _dapingService.UpdateProjectAsync(id, request, GetCurrentUserId());
    }

    /// <summary>
    /// 删除项目
    /// </summary>
    [HttpDelete("projects/{id}")]
    public async Task<ApiResponse> DeleteProject(int id)
    {
        return await _dapingService.DeleteProjectAsync(id, GetCurrentUserId());
    }

    /// <summary>
    /// 发布项目
    /// </summary>
    [HttpPost("projects/{id}/publish")]
    public async Task<ApiResponse<DapingProjectDto>> PublishProject(int id)
    {
        return await _dapingService.PublishProjectAsync(id, GetCurrentUserId());
    }

    /// <summary>
    /// 取消发布
    /// </summary>
    [HttpPost("projects/{id}/unpublish")]
    public async Task<ApiResponse<DapingProjectDto>> UnpublishProject(int id)
    {
        return await _dapingService.UnpublishProjectAsync(id, GetCurrentUserId());
    }
}
```

---

### Task 3.9: 添加公开访问端点

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Controllers/PublicController.cs`

**Step 1: 添加公开访问端点**

在 `PublicController.cs` 中添加：

```csharp
/// <summary>
/// 获取公开大屏项目（无需认证）
/// </summary>
[HttpGet("d/{publicUrl}")]
[AllowAnonymous]
public async Task<ApiResponse<DapingProjectDetailDto>> GetPublicDaping(string publicUrl)
{
    return await _dapingService.GetPublicProjectAsync(publicUrl);
}
```

**Step 2: 注入 IDapingService**

在 `PublicController` 构造函数中添加 `IDapingService` 参数。

---

### Task 3.10: 测试后端 API

**Files:**
- N/A

**Step 1: 构建后端**

```bash
cd H:/DataForge/backend
dotnet build
```

**Step 2: 运行后端**

```bash
dotnet run --project src/DataForgeStudio.Api
```

**Step 3: 测试 API（可选）**

访问 `https://localhost:5000/swagger` 测试 API 端点。

---

### Task 3.11: 提交阶段 3 代码

**Files:**
- N/A

**Step 1: 暂存所有更改**

```bash
cd H:/DataForge
git add backend/
```

**Step 2: 提交**

```bash
git commit -m "$(cat <<'EOF'
feat(backend): add daping API endpoints

- Add DapingProjects table migration
- Add DapingProject entity
- Add DapingService with CRUD operations
- Add DapingController with project management API
- Add public access endpoint for published projects
- Support publish/unpublish functionality

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## 阶段 4: 前端路由和菜单集成

### Task 4.1: 添加路由配置

**Files:**
- Modify: `frontend/src/router/index.ts`

**Step 1: 读取当前路由配置**

```bash
cat H:/DataForge/frontend/src/router/index.ts
```

**Step 2: 添加 daping 路由**

在路由配置中添加：

```typescript
{
  path: '/daping',
  name: 'DapingLayout',
  component: () => import('@/layout/MainLayout.vue'),
  meta: { permission: 'dashboard:view' },
  children: [
    {
      path: '',
      name: 'DapingList',
      component: () => import('@/daping/views/project/index.vue'),
      meta: { permission: 'dashboard:view' }
    },
    {
      path: 'design/:id?',
      name: 'DapingDesigner',
      component: () => import('@/daping/views/chart/index.vue'),
      meta: { permission: 'dashboard:edit' }
    },
    {
      path: 'preview/:id',
      name: 'DapingPreview',
      component: () => import('@/daping/views/preview/index.vue'),
      meta: { permission: 'dashboard:view' }
    }
  ]
},
{
  path: '/public/d/:publicUrl',
  name: 'PublicDaping',
  component: () => import('@/daping/views/preview/index.vue'),
  meta: { requiresAuth: false }
}
```

---

### Task 4.2: 更新菜单配置

**Files:**
- Modify: `frontend/src/layout/components/Sidebar.vue` 或菜单配置文件

**Step 1: 找到菜单配置文件**

```bash
find H:/DataForge/frontend/src -name "*menu*" -o -name "*sidebar*" | head -10
```

**Step 2: 添加大屏管理菜单**

在菜单配置中添加：

```typescript
{
  path: '/daping',
  name: '大屏管理',
  icon: 'Monitor',
  children: [
    {
      path: '/daping',
      name: '大屏列表',
      icon: 'List'
    }
  ]
}
```

---

### Task 4.3: 处理 daping 内部路由

**Files:**
- Modify: `frontend/src/daping/views/chart/index.vue`

**Step 1: 修改返回按钮路由**

在编辑器页面中，修改返回按钮的路由：

```typescript
const handleBack = () => {
  router.push('/daping')
}
```

---

### Task 4.4: 移除旧大屏代码

**Files:**
- Delete: `frontend/src/views/dashboard/` (保留 PublicDashboard.vue 如需兼容)
- Modify: `frontend/src/router/index.ts` (移除旧路由)

**Step 1: 备份旧代码（可选）**

```bash
mv H:/DataForge/frontend/src/views/dashboard H:/DataForge/frontend/src/views/dashboard.bak
```

**Step 2: 确认移除后不影响系统**

测试页面访问正常。

---

### Task 4.5: 测试集成

**Files:**
- N/A

**Step 1: 启动前端**

```bash
cd H:/DataForge/frontend
npm run dev
```

**Step 2: 测试功能**

1. 访问 `/daping` 测试大屏列表
2. 点击新建测试大屏设计器
3. 测试保存、预览功能

---

### Task 4.6: 最终提交

**Files:**
- N/A

**Step 1: 暂存所有更改**

```bash
cd H:/DataForge
git add frontend/src/router/
git add frontend/src/layout/
git add frontend/src/views/dashboard.bak 2>/dev/null || true
git add frontend/src/views/dashboard/ 2>/dev/null || true
```

**Step 2: 提交**

```bash
git commit -m "$(cat <<'EOF'
feat(daping): integrate routing and menu

- Add daping routes to main router
- Update sidebar menu with daping entry
- Fix internal routing in daping editor
- Remove old dashboard code (backed up)

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## 验证清单

完成后验证以下功能：

- [ ] 后端 API 可正常访问 `/api/daping/projects/list`
- [ ] 前端可访问 `/daping` 大屏列表页
- [ ] 前端可访问 `/daping/design` 大屏设计器
- [ ] 大屏可以创建、保存、删除
- [ ] 发布功能正常，生成公开 URL
- [ ] 公开访问 `/public/d/{publicUrl}` 正常工作
- [ ] 定时刷新功能正常
- [ ] 无控制台错误

---

## 回滚方案

如果集成失败，执行以下步骤回滚：

```bash
# 切回主分支
git checkout master

# 删除功能分支
git branch -D feature/daping-integration

# 恢复旧大屏代码
mv H:/DataForge/frontend/src/views/dashboard.bak H:/DataForge/frontend/src/views/dashboard
```
