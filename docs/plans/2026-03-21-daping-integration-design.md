# 高级大屏模块集成设计文档

**日期**: 2026-03-21
**状态**: 已批准
**作者**: Claude Code

---

## 1. 概述

### 1.1 项目背景

DataForgeStudio 现有大屏系统功能较为简单，而 `daping` 目录中的 go-view 是一个功能完整的低代码大屏设计器（50+ 组件，商业级代码质量）。本设计旨在将 go-view 集成到 DataForge 中，完全替换现有简单大屏系统。

### 1.2 目标

- 提供功能完整的大屏可视化设计器
- 复用 DataForge 认证体系
- 移除不需要的功能（地图、模板市场）
- 保持系统稳定性

### 1.3 核心决策

| 决策项 | 选择 |
|--------|------|
| 集成方式 | 完全替换现有大屏系统 |
| 代码位置 | 整体迁移到 frontend/src/daping |
| UI 框架 | 保持 Naive UI |
| 认证系统 | 复用 DataForge JWT 认证 |
| 后端 API | 新建大屏专用 API |
| 地图功能 | 移除 |
| 模板市场 | 移除 |
| 定时刷新 | 保留 |

---

## 2. 系统架构

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         DataForge 前端                                    │
├───────────────────────────┬─────────────────────────────────────────────┤
│   主应用 (Element Plus)    │    高级大屏模块 (Naive UI)                   │
│   - 首页                   │    frontend/src/daping/                     │
│   - 报表管理               │    - 大屏列表页                              │
│   - 系统管理               │    - 大屏设计器                              │
│   - 简单大屏 (已移除)       │    - 大屏预览                              │
│                           │    - 组件库                                  │
├───────────────────────────┴─────────────────────────────────────────────┤
│                              路由层                                        │
│   /daping         - 大屏列表                                              │
│   /daping/design/:id? - 大屏设计器                                        │
│   /daping/preview/:id - 大屏预览                                          │
├─────────────────────────────────────────────────────────────────────────┤
│                         DataForge 后端                                    │
├───────────────────────────┬─────────────────────────────────────────────┤
│   现有 API                 │    新增大屏 API                              │
│   /api/reports/*           │    /api/daping/*                            │
│   /api/users/*             │    - 项目 CRUD                              │
│   /api/dashboards/* (弃用)  │    - 组件配置保存                           │
│                           │    - 数据源管理                              │
└───────────────────────────┴─────────────────────────────────────────────┘
```

### 2.2 前端目录结构

```
frontend/src/
├── daping/                      # 高级大屏模块（保持原有结构）
│   ├── api/                     # API 请求（需修改认证）
│   ├── components/              # 公共组件
│   ├── packages/                # 组件库
│   │   ├── components/          # 可视化组件
│   │   │   ├── Charts/          # 图表（移除 Maps）
│   │   │   ├── Tables/          # 表格
│   │   │   ├── Decorates/       # 装饰
│   │   │   └── Informations/    # 信息组件
│   │   └── public/              # 公共配置
│   ├── views/                   # 页面
│   │   ├── chart/               # 设计器
│   │   ├── preview/             # 预览
│   │   └── project/             # 项目管理
│   ├── store/                   # 状态管理
│   ├── hooks/                   # 组合式函数
│   ├── enums/                   # 枚举定义
│   ├── settings/                # 配置文件
│   ├── styles/                  # 样式文件
│   └── utils/                   # 工具函数
├── views/dashboard/             # 简单大屏（将被替换）
└── router/                      # 路由配置
```

---

## 3. 后端设计

### 3.1 API 端点设计

#### DapingController

| 方法 | 端点 | 说明 | 权限 |
|------|------|------|------|
| GET | `/api/daping/projects` | 获取项目列表 | `dashboard:view` |
| GET | `/api/daping/projects/{id}` | 获取项目详情 | `dashboard:view` |
| POST | `/api/daping/projects` | 创建项目 | `dashboard:create` |
| PUT | `/api/daping/projects/{id}` | 更新项目 | `dashboard:edit` |
| DELETE | `/api/daping/projects/{id}` | 删除项目 | `dashboard:delete` |
| POST | `/api/daping/projects/{id}/publish` | 发布项目 | `dashboard:edit` |
| POST | `/api/daping/projects/{id}/unpublish` | 取消发布 | `dashboard:edit` |
| POST | `/api/daping/data/fetch` | 根据配置获取数据 | `dashboard:view` |
| GET | `/api/daping/public/{publicUrl}` | 公开访问（无需认证） | 无 |

### 3.2 数据库设计

#### DapingProjects 表

```sql
CREATE TABLE DapingProjects (
    ProjectId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,           -- 项目名称
    State INT DEFAULT 1,                   -- 状态：1=草稿 2=发布
    Content NVARCHAR(MAX) NOT NULL,        -- 完整项目配置（JSON）
    PublicUrl NVARCHAR(50),                -- 公开访问 URL 标识
    CreatedBy INT,                         -- 创建人ID
    CreatedTime DATETIME DEFAULT GETDATE(),
    UpdatedTime DATETIME,
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- 索引
CREATE INDEX IX_DapingProjects_CreatedBy ON DapingProjects(CreatedBy);
CREATE UNIQUE INDEX IX_DapingProjects_PublicUrl ON DapingProjects(PublicUrl) WHERE PublicUrl IS NOT NULL;
```

### 3.3 Content JSON 结构说明

go-view 将整个大屏配置存储为单个 JSON，结构如下：

```json
{
  "editCanvas": {
    "width": 1920,
    "height": 1080,
    "background": "#0a1628"
  },
  "componentList": [
    {
      "id": "chart_001",
      "key": "ChartsBarsCommon",
      "chartConfig": { /* 图表配置 */ },
      "position": { "x": 0, "y": 0, "w": 500, "h": 300 },
      "request": { /* 数据请求配置 */ }
    }
  ]
}
```

---

## 4. 前端集成设计

### 4.1 路由配置

```typescript
// frontend/src/router/index.ts
{
  path: '/daping',
  name: 'DapingList',
  component: () => import('@/daping/views/project/index.vue'),
  meta: { permission: 'dashboard:view' }
},
{
  path: '/daping/design/:id?',
  name: 'DapingDesigner',
  component: () => import('@/daping/views/chart/index.vue'),
  meta: { permission: 'dashboard:edit' }
},
{
  path: '/daping/preview/:id',
  name: 'DapingPreview',
  component: () => import('@/daping/views/preview/index.vue'),
  meta: { permission: 'dashboard:view' }
},
{
  path: '/public/d/:publicUrl',
  name: 'PublicDaping',
  component: () => import('@/daping/views/preview/index.vue'),
  meta: { requiresAuth: false }
}
```

### 4.2 认证集成

修改 `frontend/src/daping/api/axios.ts`：

```typescript
import axios from 'axios'
import { useUserStore } from '@/stores/user'
import router from '@/router'

const service = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: 30000
})

// 请求拦截器：添加 DataForge JWT Token
service.interceptors.request.use((config) => {
  const userStore = useUserStore()
  if (userStore.token) {
    config.headers.Authorization = `Bearer ${userStore.token}`
  }
  return config
})

// 响应拦截器：处理 401 跳转登录页
service.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) {
      router.push('/login')
    }
    return Promise.reject(error)
  }
)

export default service
```

### 4.3 需要移除的功能

| 功能 | 文件/目录 | 原因 |
|------|----------|------|
| 地图组件 | `packages/components/Maps/` | 不需要 |
| 登录页面 | `views/login/` | 复用 DataForge 登录 |
| 模板市场 | `views/project/templateMarket/` | 不需要 |
| 租户功能 | `.env` 中的 `VITE_APP_TENANT_ENABLE` | 不需要 |
| 验证码功能 | `views/login/`, `components/Verifition/` | 不需要 |

### 4.4 需要移除的依赖

```json
{
  "dependencies": {
    "@amap/amap-jsapi-loader": "移除",
    "cesium": "移除",
    "xbsj-xe2": "移除"
  }
}
```

### 4.5 Vite 配置调整

```typescript
// frontend/vite.config.ts
export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(__dirname, 'src')
    }
  },
  optimizeDeps: {
    include: ['naive-ui', 'echarts', 'vue-echarts']
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'naive-ui': ['naive-ui'],
          'echarts': ['echarts', 'vue-echarts']
        }
      }
    }
  }
})
```

---

## 5. 菜单集成

### 5.1 菜单结构

```
侧边栏菜单
├── 首页
├── 报表管理
│   ├── 报表查询
│   └── 报表设计
├── 大屏管理 ← 修改
│   ├── 大屏列表     (/daping)
│   └── 新建大屏     (/daping/design)
├── 系统管理
│   ├── 用户管理
│   └── ...
```

### 5.2 权限点（复用现有）

| 权限名 | 说明 |
|--------|------|
| `dashboard:view` | 查看大屏 |
| `dashboard:create` | 创建大屏 |
| `dashboard:edit` | 编辑大屏 |
| `dashboard:delete` | 删除大屏 |

---

## 6. 实施步骤

### 阶段 1: 基础迁移（2 天）

| 任务 | 描述 |
|------|------|
| 复制代码 | 将 daping 目录复制到 frontend/src/daping |
| 移除地图 | 删除 packages/components/Maps/ 和相关依赖 |
| 移除模板 | 删除 views/project/templateMarket/ |
| 移除登录 | 删除 views/login/ 和验证码组件 |
| 配置 Vite | 添加别名和构建配置 |
| 安装依赖 | 在 frontend 目录运行 npm install |

### 阶段 2: 认证集成（1 天）

| 任务 | 描述 |
|------|------|
| 修改 API 拦截器 | 复用 DataForge JWT Token |
| 修改登录逻辑 | 移除租户和验证码相关代码 |
| 修改 store | 适配 DataForge 用户信息格式 |

### 阶段 3: 后端开发（2 天）

| 任务 | 描述 |
|------|------|
| 创建数据库表 | DapingProjects 表 |
| 创建 Entity | DapingProject.cs |
| 创建 Service | IDapingService, DapingService |
| 创建 Controller | DapingController |
| 实现 API 端点 | CRUD + 发布 + 公开访问 |

### 阶段 4: 功能完善（2 天）

| 任务 | 描述 |
|------|------|
| 集成菜单 | 更新侧边栏菜单 |
| 集成路由 | 添加路由配置 |
| 移除旧代码 | 删除 views/dashboard/ 中的旧大屏代码 |
| 测试修复 | 功能测试和 Bug 修复 |

**总计: 约 7 天**

---

## 7. 风险与缓解措施

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| UI 框架冲突 | Naive UI 与 Element Plus 样式冲突 | 使用 CSS Scope 隔离，确保样式不互相污染 |
| ECharts 版本差异 | 版本不一致导致渲染问题 | 统一 ECharts 版本到最新稳定版 |
| API 格式差异 | go-view 期望格式与 DataForge 不同 | 编写适配层转换请求/响应格式 |
| 状态管理冲突 | Pinia store 与主项目冲突 | 使用命名空间前缀 `daping/` |
| 包体积增大 | Naive UI 额外增加 ~1MB | 使用 Tree-shaking 和代码分割 |

---

## 8. 代码精简

### 移除内容预估

| 移除内容 | 预估减少 |
|----------|----------|
| 高德地图 SDK | ~500KB |
| Cesium | ~50MB |
| 地图 GeoJSON | ~2MB |
| 模板市场代码 | ~100KB |

### 保留的核心功能

- 图表组件（柱状图、折线图、饼图、仪表盘等）
- 表格组件（滚动表格、排名列表）
- 装饰组件（边框、装饰元素）
- 信息组件（文字、图片、视频）
- 数据请求和定时刷新
- 组件拖拽和布局
- 撤销/重做
- 导出图片

---

## 9. 后续步骤

1. 使用 writing-plans skill 创建详细实施计划
2. 按阶段分步实施
3. 每阶段完成后进行测试验证
4. 完成后更新文档和 CHANGELOG
