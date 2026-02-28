# Agent 1 任务书: 核心引擎开发

## 基本信息
- **Agent ID**: dashboard-engine-agent
- **模块**: Dashboard Core Engine
- **预计工时**: 8 天
- **优先级**: P0 (最高，必须最先完成)

## 必须调用的 Skills（按顺序）

```
1. superpowers:using-git-worktrees  → 创建隔离工作区
2. superpowers:brainstorming        → 确认引擎设计细节
3. superpowers:writing-plans        → 编写实施计划
4. dashboard-engine-builder         → 获取引擎开发指南
5. superpowers:test-driven-development → TDD 开发
6. superpowers:requesting-code-review → 代码审查
7. superpowers:verification-before-completion → 完成验证
```

## 任务范围

### 输入
- 设计文档: `docs/plans/2026-02-28-dashboard-kanban-design.md`
- 数据库脚本: 需创建 `Dashboards`, `DashboardWidgets` 表

### 输出文件
```
frontend/src/dashboard/
├── core/
│   ├── LayoutEngine.ts           # 网格布局引擎
│   ├── ComponentRegistry.ts      # 组件注册中心
│   ├── DataBinder.ts             # 数据绑定系统
│   └── StateStore.ts             # Pinia 状态管理
├── types/
│   └── dashboard.ts              # TypeScript 接口
└── components/
    ├── DashboardCanvas.vue       # 看板画布
    ├── WidgetWrapper.vue         # 组件包装器
    └── GridOverlay.vue           # 网格辅助线

backend/src/DataForgeStudio.Domain/Entities/
├── Dashboard.cs
└── DashboardWidget.cs

backend/src/DataForgeStudio.Core/Services/
└── DashboardService.cs

backend/src/DataForgeStudio.Api/Controllers/
└── DashboardController.cs
```

### 不包含
- 图表组件（Agent 2 负责）
- 看板视图（Agent 3 负责）
- 车间大屏（Agent 4 负责）

## 核心功能要求

### 1. LayoutEngine
```typescript
// 必须实现的方法
class LayoutEngine {
  calcPixelPosition(pos: WidgetPosition): PixelPosition
  detectCollision(widgets: WidgetPosition[]): CollisionResult[]
  autoArrange(widgets: WidgetPosition[]): WidgetPosition[]
  snapToGrid(x: number, y: number): GridPosition
}
```

### 2. ComponentRegistry
```typescript
// 必须实现的方法
class ComponentRegistry {
  register(definition: WidgetDefinition): void
  get(type: string): WidgetDefinition | undefined
  getByCategory(category: string): WidgetDefinition[]
  getAll(): WidgetDefinition[]
}
```

### 3. DataBinder
```typescript
// 必须实现的方法
class DataBinder {
  registerSource(source: DataSource): void
  bind(config: BindingConfig): void
  refresh(sourceId: string): Promise<void>
  getData(sourceId: string): Ref<any>
}
```

### 4. StateStore (Pinia)
```typescript
// 必须实现的状态和操作
- widgets: Map<string, WidgetInstance>
- selectedWidgetId: string | null
- isEditing: boolean
- displayMode: 'edit' | 'preview' | 'fullscreen'

- addWidget(), removeWidget(), updatePosition()
- setEditingMode(), setDisplayMode()
- refreshData(), refreshAllData()
```

## 数据库要求

```sql
-- 必须创建的表
CREATE TABLE Dashboards (
    DashboardId INT IDENTITY(1,1) PRIMARY KEY,
    DashboardName NVARCHAR(100) NOT NULL,
    -- ... 见设计文档
);

CREATE TABLE DashboardWidgets (
    WidgetId INT IDENTITY(1,1) PRIMARY KEY,
    DashboardId INT NOT NULL,
    -- ... 见设计文档
);
```

## API 端点要求

```
GET    /api/dashboards              # 获取看板列表
GET    /api/dashboards/{id}         # 获取看板详情
POST   /api/dashboards              # 创建看板
PUT    /api/dashboards/{id}         # 更新看板
DELETE /api/dashboards/{id}         # 删除看板

POST   /api/dashboards/{id}/widgets           # 添加组件
PUT    /api/dashboards/{id}/widgets/{wid}     # 更新组件
DELETE /api/dashboards/{id}/widgets/{wid}     # 删除组件
```

## 验收标准

1. ✅ 网格布局正确计算组件位置
2. ✅ 组件拖拽时有碰撞检测
3. ✅ 自动排列功能正常工作
4. ✅ 组件注册后可动态加载
5. ✅ 数据绑定支持定时刷新
6. ✅ Pinia store 状态正确同步
7. ✅ 所有 API 单元测试通过
8. ✅ 前端组件渲染正确

## 完成标志

完成后提交:
1. Git 分支: `feature/dashboard-core-engine`
2. Pull Request 标题: `feat: dashboard core engine`
3. 在 PR 中 @ 其他 Agent 告知可以开始开发
