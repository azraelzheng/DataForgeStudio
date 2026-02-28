# DataForgeStudio 看板系统设计文档

> **版本**: V1.1.0
> **创建日期**: 2026-02-28
> **作者**: Claude Agent Teams

## 1. 概述

### 1.1 目标
为 DataForgeStudio 添加完整的看板/仪表盘系统，支持：
- 数据仪表盘（多组件、网格布局、拖拽配置）
- 看板视图（Trello 风格任务卡片）
- 组合图表（报表内多图表配置）
- 车间大屏（基于报表 SQL，全局轮播，定时刷新）

### 1.2 版本区分
| 功能 | 标准版 | 专业版 |
|------|--------|--------|
| 看板绑定报表数据源 | ✅ | ✅ |
| 看板独立配置 SQL | ❌ | ✅ |
| 车间大屏轮播 | ✅ | ✅ |
| 多数据源绑定 | ❌ | ✅ |

## 2. 系统架构

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        前端 (Vue 3)                              │
├─────────────────┬─────────────────┬─────────────────┬───────────┤
│   Dashboard     │    Kanban       │   Chart         │ Display   │
│   Engine        │    View         │   Components    │ Mode      │
│   (Agent 1)     │    (Agent 3)    │   (Agent 2)     │ (Agent 4) │
├─────────────────┴─────────────────┴─────────────────┴───────────┤
│                      共享核心层                                  │
│  - ComponentRegistry  - DataBinder  - StateStore (Pinia)        │
├─────────────────────────────────────────────────────────────────┤
│                        后端 (.NET 8)                             │
├─────────────────┬─────────────────┬─────────────────────────────┤
│ DashboardController │ KanbanController │ DisplayController       │
├─────────────────┴─────────────────┴─────────────────────────────┤
│                      数据访问层                                  │
│  - DashboardService   - KanbanService   - DataSourceService     │
├─────────────────────────────────────────────────────────────────┤
│                      数据库 (SQL Server)                         │
│  - Dashboards   - DashboardWidgets   - KanbanCards              │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 模块依赖关系

```
Agent 1 (核心引擎)
    ↓ 提供基础组件
Agent 2 (图表组件) → 依赖 Agent 1 的 WidgetWrapper
Agent 3 (看板视图) → 依赖 Agent 1 的 LayoutEngine
Agent 4 (车间大屏) → 依赖 Agent 1 的 DataBinder + Agent 2 的图表组件
```

### 2.3 开发时序

```
Phase 1 (串行): Agent 1 完成核心引擎
    ↓
Phase 2 (并行): Agent 2, 3, 4 同时开发
    ↓
Phase 3 (串行): 整合测试、权限配置
```

## 3. 数据库设计

### 3.1 看板主表 (Dashboards)

```sql
CREATE TABLE Dashboards (
    DashboardId INT IDENTITY(1,1) PRIMARY KEY,
    DashboardName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    Category NVARCHAR(50),
    LayoutConfig NVARCHAR(MAX) NOT NULL,  -- JSON: columns, rowHeight, gap
    DataSourceId INT NULL,                 -- 标准版：绑定报表数据源
    SqlStatement NVARCHAR(MAX) NULL,       -- 专业版：独立 SQL
    RefreshInterval INT DEFAULT 60,        -- 数据刷新间隔（秒）
    IsPublished BIT DEFAULT 0,
    CreatedBy INT NOT NULL,
    CreatedTime DATETIME DEFAULT GETDATE(),
    UpdatedTime DATETIME,
    FOREIGN KEY (DataSourceId) REFERENCES DataSources(DataSourceId)
);
```

### 3.2 看板组件表 (DashboardWidgets)

```sql
CREATE TABLE DashboardWidgets (
    WidgetId INT IDENTITY(1,1) PRIMARY KEY,
    DashboardId INT NOT NULL,
    WidgetType NVARCHAR(50) NOT NULL,      -- chart, card, table, kanban
    WidgetName NVARCHAR(100),
    PositionConfig NVARCHAR(MAX) NOT NULL, -- JSON: x, y, width, height
    Config NVARCHAR(MAX),                  -- JSON: 组件特定配置
    DataBinding NVARCHAR(MAX),             -- JSON: 数据绑定配置
    DisplayOrder INT DEFAULT 0,
    FOREIGN KEY (DashboardId) REFERENCES Dashboards(DashboardId) ON DELETE CASCADE
);
```

### 3.3 看板卡片表 (KanbanCards)

```sql
CREATE TABLE KanbanCards (
    CardId INT IDENTITY(1,1) PRIMARY KEY,
    DashboardId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Status NVARCHAR(50) NOT NULL,          -- 列状态
    Priority NVARCHAR(20) DEFAULT 'medium',
    Assignee NVARCHAR(100),
    DueDate DATETIME,
    Tags NVARCHAR(500),                    -- JSON array
    CustomFields NVARCHAR(MAX),            -- JSON
    DisplayOrder INT DEFAULT 0,
    CreatedTime DATETIME DEFAULT GETDATE(),
    UpdatedTime DATETIME,
    FOREIGN KEY (DashboardId) REFERENCES Dashboards(DashboardId) ON DELETE CASCADE
);
```

### 3.4 车间大屏配置表 (DisplayConfigs)

```sql
CREATE TABLE DisplayConfigs (
    DisplayId INT IDENTITY(1,1) PRIMARY KEY,
    DisplayName NVARCHAR(100) NOT NULL,
    DashboardIds NVARCHAR(MAX) NOT NULL,   -- JSON array of dashboard IDs
    Interval INT DEFAULT 30,               -- 轮播间隔（秒）
    AutoRefresh INT DEFAULT 60,            -- 数据刷新间隔（秒）
    Transition NVARCHAR(20) DEFAULT 'fade',
    ShowClock BIT DEFAULT 1,
    ShowDashboardName BIT DEFAULT 1,
    IsEnabled BIT DEFAULT 1,
    CreatedTime DATETIME DEFAULT GETDATE()
);
```

## 4. API 设计

### 4.1 看板管理 API

```
GET    /api/dashboards              # 获取看板列表
GET    /api/dashboards/{id}         # 获取看板详情（含组件）
POST   /api/dashboards              # 创建看板
PUT    /api/dashboards/{id}         # 更新看板
DELETE /api/dashboards/{id}         # 删除看板

POST   /api/dashboards/{id}/widgets           # 添加组件
PUT    /api/dashboards/{id}/widgets/{wid}     # 更新组件
DELETE /api/dashboards/{id}/widgets/{wid}     # 删除组件
POST   /api/dashboards/{id}/widgets/reorder   # 重排序组件

POST   /api/dashboards/{id}/data    # 执行看板数据查询
```

### 4.2 看板卡片 API

```
GET    /api/kanban/{dashboardId}/cards           # 获取卡片列表
POST   /api/kanban/{dashboardId}/cards           # 创建卡片
PUT    /api/kanban/{dashboardId}/cards/{cardId}  # 更新卡片
DELETE /api/kanban/{dashboardId}/cards/{cardId}  # 删除卡片
POST   /api/kanban/{dashboardId}/cards/move      # 移动卡片（拖拽）
```

### 4.3 车间大屏 API

```
GET    /api/display                    # 获取大屏配置列表
GET    /api/display/{id}               # 获取大屏详情
POST   /api/display                    # 创建大屏配置
PUT    /api/display/{id}               # 更新大屏配置
DELETE /api/display/{id}               # 删除大屏配置
GET    /api/display/{id}/data          # 获取大屏数据（聚合查询）
```

## 5. 前端组件结构

```
src/
├── dashboard/
│   ├── core/
│   │   ├── LayoutEngine.ts           # 网格布局引擎
│   │   ├── ComponentRegistry.ts      # 组件注册中心
│   │   ├── DataBinder.ts             # 数据绑定系统
│   │   └── StateStore.ts             # Pinia 状态管理
│   ├── components/
│   │   ├── DashboardCanvas.vue       # 看板画布
│   │   ├── WidgetWrapper.vue         # 组件包装器（拖拽/缩放）
│   │   ├── WidgetSelector.vue        # 组件选择器
│   │   └── ConfigPanel.vue           # 配置面板
│   ├── widgets/
│   │   ├── ChartWidget.vue           # 图表组件
│   │   ├── CardWidget.vue            # 数字卡片
│   │   ├── TableWidget.vue           # 数据表格
│   │   ├── ProgressBarWidget.vue     # 进度条
│   │   └── StatusIndicatorWidget.vue # 状态指示灯
│   └── views/
│       ├── DashboardList.vue         # 看板列表
│       ├── DashboardDesigner.vue     # 看板设计器
│       └── DashboardPreview.vue      # 看板预览
├── kanban/
│   ├── components/
│   │   ├── KanbanBoard.vue           # 看板主体
│   │   ├── KanbanColumn.vue          # 状态列
│   │   ├── KanbanCard.vue            # 任务卡片
│   │   └── CardDetail.vue            # 卡片详情
│   └── composables/
│       ├── useDragDrop.ts            # 拖拽逻辑
│       └── useKanbanFilter.ts        # 筛选逻辑
├── display/
│   ├── components/
│   │   ├── DisplayMode.vue           # 全屏模式
│   │   └── CarouselPlayer.vue        # 轮播播放器
│   └── composables/
│       ├── useFullscreen.ts          # 全屏 API
│       ├── useCarousel.ts            # 轮播逻辑
│       └── useAutoRefresh.ts         # 自动刷新
```

## 6. Agent 任务分配

### Agent 1: 核心引擎 (dashboard-engine-builder)
- 布局引擎（网格计算、碰撞检测）
- 组件注册机制
- 数据绑定系统
- Pinia 状态管理

### Agent 2: 图表组件 (chart-component-builder)
- ECharts 图表组件
- 数字卡片组件
- 数据表格组件
- 进度条/状态指示灯

### Agent 3: 看板视图 (kanban-view-builder)
- Trello 风格看板
- 拖拽卡片交互
- 状态列管理
- 筛选/搜索功能

### Agent 4: 车间大屏 (fullscreen-display-builder)
- 全屏显示模式
- 多看板轮播
- 定时数据刷新
- 许可证验证

## 7. 工作量评估

| 模块 | Agent | 预计工时 | 说明 |
|------|-------|----------|------|
| 核心引擎 | Agent 1 | 8 天 | 布局+注册+绑定+状态 |
| 图表组件 | Agent 2 | 6 天 | 5 种组件 + 配置面板 |
| 看板视图 | Agent 3 | 6 天 | 拖拽+卡片+详情 |
| 车间大屏 | Agent 4 | 4 天 | 全屏+轮播+刷新 |
| 后端 API | - | 4 天 | 4 个 Controller + Service |
| 数据库 | - | 1 天 | 4 张表 + 迁移脚本 |
| 整合测试 | - | 3 天 | E2E + 手动测试 |

**总计**: 32 人天
**使用 Agent Teams 并行**: 约 12 天 (Phase 1: 8天 → Phase 2: 并行 max(6,6,4)=6天 → Phase 3: 3天 = 17天)

## 8. 技术选型

| 类别 | 技术 | 版本 |
|------|------|------|
| 前端框架 | Vue 3 | ^3.4 |
| UI 组件库 | Element Plus | ^2.5 |
| 状态管理 | Pinia | ^2.1 |
| 图表库 | ECharts | ^5.5 |
| 拖拽库 | @vueuse/core | ^10.x |
| 后端框架 | ASP.NET Core | 8.0 |
| ORM | Entity Framework Core | 8.0 |
| 数据库 | SQL Server | 2005+ |
