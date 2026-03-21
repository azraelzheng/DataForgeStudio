# Daping 大屏模块集成实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 go-view 高级大屏设计器完全集成到 DataForge 系统，替换现有的简单大屏模块。

**Architecture:** 后端注册服务并创建公开访问 API，前端使用 daping 组件替换现有大屏路由，保持 DataForge 路由风格。

**Tech Stack:** ASP.NET Core 8.0, Entity Framework Core, Vue 3, NaiveUI, ECharts

---

## 阶段 1: 后端服务注册

### Task 1.1: 注册 DapingService 到 DI 容器

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Program.cs:176`

**Step 1: 添加服务注册**

在 `Program.cs` 第 176 行附近（其他服务注册之后），添加：

```csharp
builder.Services.AddScoped<IDapingService, DapingService>();
```

**Step 2: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Program.cs
git commit -m "feat(daping): register DapingService in DI container"
```

---

### Task 1.2: 添加公开访问 API 端点

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Controllers/DapingController.cs:95`

**Step 1: 添加公开访问端点**

在 `DapingController.cs` 末尾（第 95 行之后），添加：

```csharp
/// <summary>
/// 获取公开项目详情（无需认证）
/// </summary>
[HttpGet("/api/public/daping/{publicUrl}")]
[AllowAnonymous]
public async Task<ApiResponse<DapingProjectDetailDto>> GetPublicProject(string publicUrl)
{
    return await _dapingService.GetPublicProjectAsync(publicUrl);
}
```

**Step 2: 确保引用正确**

确认文件顶部有以下引用（如果没有则添加）：

```csharp
using Microsoft.AspNetCore.Authorization;
```

**Step 3: 验证编译**

Run: `cd backend && dotnet build DataForgeStudio.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Controllers/DapingController.cs
git commit -m "feat(daping): add public API endpoint for anonymous access"
```

---

## 阶段 2: 数据库迁移

### Task 2.1: 创建 DapingProjects 表迁移

**Files:**
- Create: `backend/src/DataForgeStudio.Data/Migrations/20260321000000_AddDapingProjectsTable.cs`

**Step 1: 使用 EF Core CLI 创建迁移**

Run: `cd backend/src/DataForgeStudio.Api && dotnet ef migrations add AddDapingProjectsTable --project ../DataForgeStudio.Data --startup-project .`

Expected: 迁移文件创建成功

**Step 2: 验证迁移文件内容**

检查生成的迁移文件，确认包含：
- 创建 `DapingProjects` 表
- 添加主键、索引
- 添加外键关系到 `Users` 表

**Step 3: 应用迁移到数据库**

Run: `cd backend/src/DataForgeStudio.Api && dotnet ef database update --project ../DataForgeStudio.Data --startup-project .`

Expected: 数据库更新成功

**Step 4: Commit**

```bash
git add backend/src/DataForgeStudio.Data/Migrations/
git commit -m "feat(daping): add database migration for DapingProjects table"
```

---

## 阶段 3: 前端路由集成

### Task 3.1: 创建大屏路由包装组件

**Files:**
- Create: `frontend/src/views/daping/DapingProjectList.vue`
- Create: `frontend/src/views/daping/DapingDesigner.vue`
- Create: `frontend/src/views/daping/DapingPreview.vue`
- Create: `frontend/src/views/daping/DapingPublic.vue`

**Step 1: 创建项目列表包装组件**

Create `frontend/src/views/daping/DapingProjectList.vue`:

```vue
<template>
  <ProjectIndex />
</template>

<script setup>
// go-view 项目列表页
import ProjectIndex from '@/daping/views/project/index.vue'
</script>
```

**Step 2: 创建设计器包装组件**

Create `frontend/src/views/daping/DapingDesigner.vue`:

```vue
<template>
  <ChartIndex />
</template>

<script setup>
// go-view 设计器页面
import ChartIndex from '@/daping/views/chart/index.vue'
</script>
```

**Step 3: 创建预览包装组件**

Create `frontend/src/views/daping/DapingPreview.vue`:

```vue
<template>
  <PreviewIndex />
</template>

<script setup>
// go-view 预览页面
import PreviewIndex from '@/daping/views/preview/index.vue'
</script>
```

**Step 4: 创建公开访问包装组件**

Create `frontend/src/views/daping/DapingPublic.vue`:

```vue
<template>
  <div class="daping-public-container">
    <PreviewIndex v-if="projectData" :project-data="projectData" />
    <div v-else-if="loading" class="loading-state">
      <el-icon class="is-loading" :size="40"><Loading /></el-icon>
      <p>加载中...</p>
    </div>
    <div v-else-if="error" class="error-state">
      <el-icon :size="40"><WarningFilled /></el-icon>
      <p>{{ error }}</p>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { Loading, WarningFilled } from '@element-plus/icons-vue'
import PreviewIndex from '@/daping/views/preview/index.vue'
import request from '@/api/request'

const route = useRoute()
const projectData = ref(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  const publicUrl = route.params.publicUrl
  if (!publicUrl) {
    error.value = '无效的访问链接'
    loading.value = false
    return
  }

  try {
    const res = await request.get(`/api/public/daping/${publicUrl}`)
    if (res.success && res.data) {
      // 解析项目配置
      projectData.value = JSON.parse(res.data.content || '{}')
    } else {
      error.value = res.message || '大屏不存在或未发布'
    }
  } catch (e) {
    error.value = '加载失败，请稍后重试'
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.daping-public-container {
  width: 100vw;
  height: 100vh;
  overflow: hidden;
}

.loading-state,
.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100vh;
  background: #1a1a1a;
  color: #fff;
}

.error-state {
  color: #f56c6c;
}
</style>
```

**Step 5: Commit**

```bash
git add frontend/src/views/daping/
git commit -m "feat(daping): create wrapper components for daping views"
```

---

### Task 3.2: 更新路由配置

**Files:**
- Modify: `frontend/src/router/index.js`

**Step 1: 替换大屏相关路由**

将现有大屏路由（约第 93-122 行）替换为：

```javascript
// 大屏模块 - 使用 go-view
{
  path: '/dashboard',
  name: 'Dashboard',
  redirect: '/dashboard/list'
},
{
  path: '/dashboard/list',
  name: 'DashboardList',
  component: () => import('../views/daping/DapingProjectList.vue'),
  meta: { title: '大屏管理', requiresAuth: true, permission: 'dashboard:view' }
},
{
  path: '/dashboard/designer/:id?',
  name: 'DashboardDesigner',
  component: () => import('../views/daping/DapingDesigner.vue'),
  meta: { title: '大屏设计器', requiresAuth: true, permission: 'dashboard:edit' }
},
{
  path: '/dashboard/preview/:id',
  name: 'DashboardPreview',
  component: () => import('../views/daping/DapingPreview.vue'),
  meta: { title: '大屏预览', requiresAuth: true, permission: 'dashboard:view' }
},
{
  path: '/public/d/:publicUrl',
  name: 'PublicDashboard',
  component: () => import('../views/daping/DapingPublic.vue'),
  meta: { title: '大屏', requiresAuth: false }
},
```

**Step 2: 验证前端编译**

Run: `cd frontend && npm run build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add frontend/src/router/index.js
git commit -m "feat(daping): update router to use daping components"
```

---

### Task 3.3: 更新 App.vue 全屏预览检测

**Files:**
- Modify: `frontend/src/App.vue:264-272`

**Step 1: 更新全屏预览检测逻辑**

将 `isFullscreenPreviewMode` 计算属性更新为：

```javascript
// 检测是否为全屏预览模式（隐藏侧边栏和顶部栏）
const isFullscreenPreviewMode = computed(() => {
  // 大屏预览页面始终全屏
  if (route.name === 'DashboardPreview' || route.name === 'PublicDashboard') {
    return true
  }
  return false
})
```

**Step 2: Commit**

```bash
git add frontend/src/App.vue
git commit -m "feat(daping): enable fullscreen mode for dashboard preview"
```

---

## 阶段 4: 更新 daping API 适配

### Task 4.1: 确保 daping API 调用正确路径

**Files:**
- Check: `frontend/src/daping/api/path/project.api.ts`

**Step 1: 检查 API 路径配置**

确认 `frontend/src/daping/api/path/project.api.ts` 中的 API 路径已更新为 DataForge 风格：

```typescript
// 项目相关 API
export const projectListApi = (data: object) => {
  return http.post('/api/daping/projects/list', data)
}

export const projectDetailApi = (id: number) => {
  return http.get(`/api/daping/projects/${id}`)
}

export const createProjectApi = (data: object) => {
  return http.post('/api/daping/projects', data)
}

export const updateProjectApi = (id: number, data: object) => {
  return http.put(`/api/daping/projects/${id}`, data)
}

export const deleteProjectApi = (id: number) => {
  return http.delete(`/api/daping/projects/${id}`)
}

export const publishProjectApi = (id: number) => {
  return http.post(`/api/daping/projects/${id}/publish`)
}

export const unpublishProjectApi = (id: number) => {
  return http.post(`/api/daping/projects/${id}/unpublish`)
}
```

**Step 2: 如果需要更新则 Commit**

```bash
git add frontend/src/daping/api/path/project.api.ts
git commit -m "feat(daping): update API paths to use DataForge endpoints"
```

---

## 阶段 5: 清理旧代码

### Task 5.1: 删除旧的大屏组件

**Files:**
- Delete: `frontend/src/views/dashboard/DashboardManagement.vue`
- Delete: `frontend/src/views/dashboard/DashboardList.vue`
- Delete: `frontend/src/views/dashboard/DashboardDesigner.vue`
- Delete: `frontend/src/views/dashboard/DashboardView.vue`
- Keep: `frontend/src/views/dashboard/PublicDashboard.vue` (作为备份)

**Step 1: 删除旧文件**

Run:
```bash
rm frontend/src/views/dashboard/DashboardManagement.vue
rm frontend/src/views/dashboard/DashboardList.vue
rm frontend/src/views/dashboard/DashboardDesigner.vue
rm frontend/src/views/dashboard/DashboardView.vue
```

**Step 2: 验证前端编译**

Run: `cd frontend && npm run build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A frontend/src/views/dashboard/
git commit -m "refactor(daping): remove old dashboard components"
```

---

## 阶段 6: 测试验证

### Task 6.1: 启动后端并验证 API

**Step 1: 启动后端服务**

Run: `cd backend/src/DataForgeStudio.Api && dotnet run`

**Step 2: 测试 API 端点**

使用 Swagger 或 curl 测试：
- `POST /api/daping/projects/list` - 获取项目列表（需认证）
- `GET /api/public/daping/{publicUrl}` - 公开访问（无需认证）

**Step 3: 确认数据库表创建**

连接数据库验证 `DapingProjects` 表存在。

---

### Task 6.2: 启动前端并验证路由

**Step 1: 启动前端开发服务器**

Run: `cd frontend && npm run dev`

**Step 2: 测试路由**

访问并验证以下路由：
- `/dashboard/list` - 项目列表页
- `/dashboard/designer` - 设计器页
- `/dashboard/preview/1` - 预览页
- `/public/d/{publicUrl}` - 公开访问页

**Step 3: 验证菜单入口**

登录后确认侧边栏显示"大屏管理"菜单。

---

### Task 6.3: 最终 Commit

```bash
git add -A
git commit -m "feat(daping): complete daping integration - replace old dashboard with go-view"
```

---

## 验收清单

- [ ] 后端 `DapingService` 已注册到 DI 容器
- [ ] 公开访问 API `/api/public/daping/{publicUrl}` 可正常调用
- [ ] 数据库 `DapingProjects` 表已创建
- [ ] 前端路由 `/dashboard/list` 可访问 daping 项目列表
- [ ] 前端路由 `/dashboard/designer/:id?` 可访问 daping 设计器
- [ ] 前端路由 `/dashboard/preview/:id` 可访问 daping 预览页
- [ ] 前端路由 `/public/d/:publicUrl` 可公开访问
- [ ] 侧边栏菜单显示"大屏管理"入口
- [ ] 旧的大屏组件已删除
