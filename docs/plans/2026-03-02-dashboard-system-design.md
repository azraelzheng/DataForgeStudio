# 车间大屏系统设计文档

**日期**: 2026-03-02
**状态**: 已批准
**作者**: Claude Code

---

## 1. 概述

### 1.1 项目背景

DataForgeStudio 当前是一个报表管理系统，专注于数据查询、表格展示和 Excel 导出。为了满足车间生产监控和会议室展示需求，需要新增大屏可视化模块。

### 1.2 目标

- 提供生产监控大屏功能
- 支持数据可视化看板
- 支持会议室多屏轮播展示
- 与现有报表系统无缝集成

### 1.3 核心原则

| 原则 | 说明 |
|------|------|
| 职责分离 | 报表模块负责数据层（SQL + 表格 + 导出），大屏模块负责展示层（可视化） |
| 复用优先 | 复用现有 ReportService 执行 SQL，不重复造轮子 |
| 灵活配置 | 支持拖拽布局、条件样式、主题配置 |

---

## 2. 系统架构

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        前端 (Vue 3)                              │
├──────────────────────────┬──────────────────────────────────────┤
│  报表模块（纯数据）        │  大屏模块（可视化）                    │
│  - SQL 设计器            │  - 大屏设计器（拖拽布局）               │
│  - 字段配置              │  - 图表组件（折线/柱状/饼图）           │
│  - 表格查询              │  - 生产组件（仪表盘/状态灯/看板）        │
│  - Excel 导出            │  - 条件样式（阈值规则）                 │
│                          │  - 全屏/轮播展示                        │
├──────────────────────────┴──────────────────────────────────────┤
│                        后端 (ASP.NET Core 8.0)                   │
├──────────────────────────┬──────────────────────────────────────┤
│  ReportsController       │  DashboardController                  │
│  - 执行 SQL              │  - 大屏 CRUD                          │
│  - 返回表格数据           │  - 组件数据聚合                        │
│  - 导出 Excel            │  - 公开访问端点                        │
├──────────────────────────┴──────────────────────────────────────┤
│  ReportService (复用)    │  DashboardService (新增)              │
│  - SQL 执行引擎           │  - 配置管理                            │
│  - 多数据源支持           │  - 调用 ReportService 获取数据         │
├─────────────────────────────────────────────────────────────────┤
│                        数据库 (SQL Server)                       │
│  Reports (现有) / Dashboards / Widgets / Rules (新增)           │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 数据流

```
用户访问大屏
    │
    ▼
DashboardController.GetDashboard(id)
    │
    ▼
返回大屏配置（含组件列表、绑定报表ID）
    │
    ▼
前端根据组件配置调用 ReportService.ExecuteQuery(reportId)
    │
    ▼
返回报表数据
    │
    ▼
前端渲染组件（图表/表格/状态灯等）
```

---

## 3. 数据库设计

### 3.1 新增表结构

#### 3.1.1 大屏主表 (Dashboards)

```sql
CREATE TABLE Dashboards (
    DashboardId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,           -- 大屏名称
    Description NVARCHAR(500),             -- 描述
    Theme NVARCHAR(20) DEFAULT 'dark',     -- 主题：dark/light
    RefreshInterval INT DEFAULT 30,        -- 自动刷新间隔（秒），0=不刷新
    IsPublic BIT DEFAULT 0,                -- 是否公开（无需登录访问）
    LayoutConfig NVARCHAR(MAX),            -- 布局配置 JSON（组件位置、大小）
    ThemeConfig NVARCHAR(MAX),             -- 主题配置 JSON（颜色、字体等）
    CreatedBy INT,                         -- 创建人
    CreatedTime DATETIME DEFAULT GETDATE(),
    UpdatedTime DATETIME,
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);
```

#### 3.1.2 组件表 (DashboardWidgets)

```sql
CREATE TABLE DashboardWidgets (
    WidgetId INT IDENTITY(1,1) PRIMARY KEY,
    DashboardId INT NOT NULL,
    ReportId INT NOT NULL,                 -- 绑定的报表 ID
    WidgetType NVARCHAR(50) NOT NULL,      -- 组件类型
    Title NVARCHAR(100),                   -- 组件标题
    PositionX INT NOT NULL,                -- 网格 X 位置
    PositionY INT NOT NULL,                -- 网格 Y 位置
    Width INT NOT NULL,                    -- 宽度（网格单位）
    Height INT NOT NULL,                   -- 高度（网格单位）
    DataConfig NVARCHAR(MAX),              -- 数据字段映射 JSON（可选）
    StyleConfig NVARCHAR(MAX),             -- 样式配置 JSON
    FOREIGN KEY (DashboardId) REFERENCES Dashboards(DashboardId),
    FOREIGN KEY (ReportId) REFERENCES Reports(ReportId)
);
```

#### 3.1.3 条件样式规则表 (WidgetRules)

```sql
CREATE TABLE WidgetRules (
    RuleId INT IDENTITY(1,1) PRIMARY KEY,
    WidgetId INT NOT NULL,
    RuleName NVARCHAR(50),                 -- 规则名称
    Field NVARCHAR(100) NOT NULL,          -- 判断字段
    Operator NVARCHAR(20) NOT NULL,        -- 操作符：lt/lte/gt/gte/eq/neq
    Value NVARCHAR(100) NOT NULL,          -- 阈值
    ActionType NVARCHAR(50) NOT NULL,      -- 动作类型：setColor/setIcon/showText
    ActionValue NVARCHAR(100),             -- 动作值：颜色值/图标名/文本
    Priority INT DEFAULT 0,                -- 优先级（数字越大越优先）
    FOREIGN KEY (WidgetId) REFERENCES DashboardWidgets(WidgetId)
);
```

### 3.2 DataConfig 格式说明

DataConfig 是可选字段，用于图表类组件的字段映射：

```json
// 图表类组件（需要映射）
{
  "xField": "日期",
  "yFields": ["产量", "合格率"],
  "seriesType": "line"
}

// 表格/状态灯（可为空或不存在）
{}
```

### 3.3 StyleConfig 格式说明

```json
// 表格组件
{
  "overflowMode": "paginate",    // paginate=自动翻页 | scroll=滚动动画
  "pageInterval": 10,            // 翻页间隔（秒）
  "pageSize": 20,                // 每页行数
  "scrollSpeed": 50              // 滚动速度（像素/秒）
}

// 状态灯组件
{
  "size": "large",               // small/medium/large
  "showLabel": true
}
```

---

## 4. 组件体系

### 4.1 组件分类

```
组件体系
├── 基础组件
│   ├── table           - 表格
│   ├── card-number     - 数字卡片
│   └── progress-bar    - 进度条
│
├── 图表组件
│   ├── chart-bar       - 柱状图
│   ├── chart-line      - 折线图
│   ├── chart-pie       - 饼图
│   └── gauge           - 仪表盘
│
└── 生产组件
    ├── status-light    - 状态灯
    ├── kanban-card     - 看板卡片
    └── production-panel - 生产状态面板
```

### 4.2 组件 DataConfig 需求

| 组件类型 | DataConfig 需求 | 说明 |
|----------|----------------|------|
| table | 不需要 | 直接展示报表所有列 |
| card-number | 可选 | 默认取第一行第一列 |
| progress-bar | 可选 | 默认取第一个数值字段 |
| status-light | 不需要 | 由 WidgetRules 驱动 |
| chart-bar/line | 需要 | 配置 X轴/Y轴字段 |
| chart-pie | 需要 | 配置名称/数值字段 |
| gauge | 需要 | 配置当前值/最大值字段 |

### 4.3 条件样式预设模板

| 模板名 | 规则 |
|--------|------|
| 进度判断 | <70% 黄灯，<100% 红灯，=100% 绿灯 |
| 逾期判断 | 逾期红灯，临期(3天内)黄灯，正常绿灯 |
| 阈值判断 | <下限红灯，>上限黄灯，正常绿灯 |

---

## 5. API 设计

### 5.1 DashboardController（需认证）

| 方法 | 端点 | 说明 | 权限 |
|------|------|------|------|
| GET | `/api/dashboards` | 获取大屏列表 | `dashboard:view` |
| GET | `/api/dashboards/{id}` | 获取大屏详情 | `dashboard:view` |
| POST | `/api/dashboards` | 创建大屏 | `dashboard:create` |
| PUT | `/api/dashboards/{id}` | 更新大屏 | `dashboard:edit` |
| DELETE | `/api/dashboards/{id}` | 删除大屏 | `dashboard:delete` |
| POST | `/api/dashboards/{id}/widgets` | 添加组件 | `dashboard:edit` |
| PUT | `/api/dashboards/{id}/widgets/{widgetId}` | 更新组件 | `dashboard:edit` |
| DELETE | `/api/dashboards/{id}/widgets/{widgetId}` | 删除组件 | `dashboard:edit` |
| POST | `/api/dashboards/{id}/convert` | 一键转换 | `dashboard:create` |
| GET | `/api/dashboards/{id}/data` | 获取组件数据 | `dashboard:view` |

### 5.2 PublicController（无需认证）

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/public/d/{id}` | 获取公开大屏配置 + 数据 |
| GET | `/public/d/{id}/data` | 仅获取大屏数据 |

**安全措施**：
- 验证 `IsPublic=1` 才返回数据
- 限制请求频率

---

## 6. 公开访问与轮播

### 6.1 公开访问流程

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  大屏 TV    │────▶│ /public/d/:id │────▶│  检查       │
│  (无登录)   │     │              │     │  IsPublic   │
└─────────────┘     └─────────────┘     └──────┬──────┘
                                               │
                                        ┌──────┴──────┐
                                        │             │
                                       Yes           No
                                        │             │
                                        ▼             ▼
                                   正常展示      返回 403
```

### 6.2 轮播机制

```sql
-- 轮播配置（可独立表或扩展 Dashboards 表）
CREATE TABLE DashboardCarousels (
    CarouselId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    DashboardIds NVARCHAR(MAX),      -- 大屏 ID 列表 JSON [1, 3, 5]
    Interval INT DEFAULT 30,         -- 轮播间隔（秒）
    ShowNavigation BIT DEFAULT 1,    -- 是否显示导航点
    CreatedTime DATETIME DEFAULT GETDATE()
);
```

---

## 7. 一键转换

### 7.1 转换流程

```
选择报表 → 系统分析报表结构 → 生成推荐布局 → 用户调整 → 保存大屏
```

### 7.2 智能推荐规则

| 报表特征 | 自动生成组件 |
|----------|-------------|
| 有数值字段 + 日期字段 | 折线图（趋势） |
| 有分类字段 + 数值字段 | 柱状图（对比） |
| 有完成率/进度字段 | 进度条 + 状态灯 |
| 多列数据 | 表格 |
| 单个数值（如总数） | 数字卡片 |

---

## 8. 权限与菜单

### 8.1 新增权限点

| 权限名 | 说明 |
|--------|------|
| `dashboard:view` | 查看大屏 |
| `dashboard:create` | 创建大屏 |
| `dashboard:edit` | 编辑大屏 |
| `dashboard:delete` | 删除大屏 |

### 8.2 菜单结构

```
侧边栏菜单
├── 首页
├── 报表管理
│   ├── 报表查询
│   └── 报表设计
├── 大屏管理 ← 新增
│   ├── 大屏列表
│   └── 大屏设计器
├── 系统管理
│   ├── 用户管理
│   └── ...
```

### 8.3 路由配置

```javascript
// 新增路由
{ path: '/dashboard', name: 'DashboardList', meta: { permission: 'dashboard:view' } }
{ path: '/dashboard/designer/:id?', name: 'DashboardDesigner', meta: { permission: 'dashboard:edit' } }
{ path: '/dashboard/view/:id', name: 'DashboardView', meta: { permission: 'dashboard:view' } }
{ path: '/public/d/:id', name: 'PublicDashboard', meta: { requiresAuth: false } }
```

---

## 9. 技术选型

### 9.1 前端

| 技术 | 用途 |
|------|------|
| vue-grid-layout | 拖拽布局 |
| Apache ECharts | 图表渲染 |
| Element Plus | UI 组件 |
| Pinia | 状态管理 |

### 9.2 后端

| 技术 | 用途 |
|------|------|
| ASP.NET Core 8.0 | Web API |
| Entity Framework Core | ORM |
| 现有 ReportService | SQL 执行 |

---

## 10. 核心决策总结

| 决策项 | 选择 |
|--------|------|
| 架构模式 | 数据库驱动，复用现有报表服务 |
| 模块职责 | 报表=数据层，大屏=展示层 |
| 布局方式 | vue-grid-layout 自由拖拽 |
| 刷新方式 | 手动 + 自动轮询 |
| 表格溢出 | 自动翻页 / 滚动动画（可配置） |
| 条件样式 | 预设模板 + 自定义规则 |
| 公开访问 | 自增 ID + IsPublic 验证 |
| 权限控制 | 继承现有权限体系 |

---

## 11. 后续步骤

1. 使用 writing-plans skill 创建详细实施计划
2. 按计划分阶段实施
