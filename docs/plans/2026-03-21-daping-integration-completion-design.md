# Daping 大屏模块集成完成设计文档

> **创建日期**: 2026-03-21
> **状态**: 待实施
> **关联计划**: 2026-03-21-daping-integration-plan.md

## 概述

将 go-view 高级大屏设计器（daping 模块）完全集成到 DataForge 系统，替换现有的简单大屏模块。

## 设计决策

| 决策项 | 选择 | 原因 |
|--------|------|------|
| 集成方式 | 完全替换 | go-view 功能更强大，统一使用一个系统 |
| 路由风格 | DataForge 风格 | 与系统其他模块保持一致 |
| 菜单位置 | 主菜单一级入口 | 大屏设计器是核心功能，需要突出显示 |
| 数据源 | 复用 DataForge 数据源 | 统一数据源管理，避免重复配置 |

## 路由设计

### 认证路由（需登录）

| 路由 | 组件 | 说明 |
|------|------|------|
| `/dashboard` | 重定向到 `/dashboard/list` | 大屏模块入口 |
| `/dashboard/list` | daping 项目列表页 | 大屏项目管理 |
| `/dashboard/designer/:id?` | daping 编辑器 | 大屏设计（id 可选，新建时无 id） |
| `/dashboard/preview/:id` | daping 预览页 | 大屏预览 |

### 公开路由（无需登录）

| 路由 | 组件 | 说明 |
|------|------|------|
| `/public/d/:publicUrl` | 公开访问页 | 通过 publicUrl 访问已发布的大屏 |

## 后端 API

### 认证 API（/api/daping/*）

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/daping/projects/list` | POST | 获取项目列表 |
| `/api/daping/projects/{id}` | GET | 获取项目详情 |
| `/api/daping/projects` | POST | 创建项目 |
| `/api/daping/projects/{id}` | PUT | 更新项目 |
| `/api/daping/projects/{id}` | DELETE | 删除项目 |
| `/api/daping/projects/{id}/publish` | POST | 发布项目 |
| `/api/daping/projects/{id}/unpublish` | POST | 取消发布 |

### 公开 API（/api/public/daping/*）

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/public/daping/{publicUrl}` | GET | 获取公开项目详情（匿名访问） |

## 菜单设计

在侧边栏添加一级菜单项：

```javascript
{
  path: '/dashboard',
  name: 'Dashboard',
  meta: {
    title: '大屏设计器',
    icon: 'DataBoard',  // Element Plus 图标
    permission: 'dashboard:view'
  },
  children: [
    { path: '/dashboard/list', meta: { title: '项目管理' } },
    // 其他子菜单...
  ]
}
```

## 数据源集成

### 集成方式

daping 模块的数据请求将复用 DataForge 的数据源系统：

1. **静态数据**: 保持 daping 原有的静态数据配置方式
2. **动态数据**: 调用 DataForge 的 `/api/datasource/{id}/execute` 接口

### API 适配

修改 daping 的 `api/http.ts`，将数据请求代理到 DataForge 数据源 API：

```typescript
// 原 go-view 数据请求
POST /api/daping/data/getData

// 改为 DataForge 数据源请求
POST /api/datasource/{datasourceId}/execute
```

## 实施步骤

### 阶段 1: 后端集成（高优先级）

1. 在 `Program.cs` 注册 `IDapingService`
2. 创建数据库迁移，添加 `DapingProjects` 表
3. 添加公开访问 API 端点

### 阶段 2: 前端路由集成（高优先级）

1. 修改 `router/index.js`，替换大屏相关路由
2. 配置 daping 路由组件懒加载
3. 处理路由守卫和权限验证

### 阶段 3: 菜单集成（中优先级）

1. 在侧边栏菜单配置中添加大屏设计器入口
2. 配置菜单权限控制

### 阶段 4: 清理旧代码（低优先级）

1. 删除 `frontend/src/views/dashboard/` 下的旧组件
2. 清理不再使用的代码

## 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| daping 组件兼容性问题 | 中 | 逐个路由测试，确保功能正常 |
| 数据源集成复杂度 | 中 | 第一版可先支持静态数据，动态数据后续迭代 |
| 样式冲突 | 低 | daping 使用 NaiveUI，DataForge 使用 Element Plus，需确保样式隔离 |

## 验收标准

1. ✅ 后端 API 可正常调用
2. ✅ 数据库表创建成功
3. ✅ 前端路由可正常访问
4. ✅ 菜单入口正确显示
5. ✅ 公开访问功能正常
6. ✅ 认证和权限控制正常
