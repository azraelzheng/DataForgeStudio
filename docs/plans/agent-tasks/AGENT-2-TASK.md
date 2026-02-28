# Agent 2 任务书: 图表组件开发

## 基本信息
- **Agent ID**: chart-component-agent
- **模块**: Chart Components
- **预计工时**: 6 天
- **优先级**: P1 (Agent 1 完成后开始)
- **依赖**: Agent 1 (核心引擎)

## 必须调用的 Skills（按顺序）

```
1. superpowers:using-git-worktrees  → 创建隔离工作区
2. superpowers:brainstorming        → 确认图表组件设计
3. superpowers:writing-plans        → 编写实施计划
4. chart-component-builder          → 获取图表开发指南
5. superpowers:frontend-design      → UI/UX 设计
6. superpowers:test-driven-development → TDD 开发
7. superpowers:requesting-code-review → 代码审查
8. superpowers:verification-before-completion → 完成验证
```

## 前置条件

等待 Agent 1 完成以下组件:
- ✅ `WidgetWrapper.vue` - 组件包装器
- ✅ `ComponentRegistry.ts` - 组件注册中心
- ✅ `StateStore.ts` - 状态管理
- ✅ `DataBinder.ts` - 数据绑定

## 任务范围

### 输出文件
```
frontend/src/dashboard/widgets/
├── ChartWidget.vue               # ECharts 图表组件
├── NumberCardWidget.vue          # 数字卡片
├── DataTableWidget.vue           # 数据表格
├── ProgressBarWidget.vue         # 进度条
├── StatusIndicatorWidget.vue     # 状态指示灯
└── config/
    ├── ChartConfigPanel.vue      # 图表配置面板
    ├── NumberCardConfig.vue      # 数字卡片配置
    └── TableConfig.vue           # 表格配置

frontend/src/dashboard/composables/
└── useECharts.ts                 # ECharts composable
```

### 不包含
- 核心引擎（Agent 1 负责）
- 看板视图（Agent 3 负责）
- 车间大屏（Agent 4 负责）

## 核心功能要求

### 1. ChartWidget.vue

```vue
<!-- 支持的图表类型 -->
- bar       柱状图
- line      折线图
- pie       饼图
- doughnut  环形图
- gauge     仪表盘
- radar     雷达图
```

```typescript
// Props 接口
interface ChartWidgetProps {
  widgetId: string
  chartType: 'bar' | 'line' | 'pie' | 'doughnut' | 'gauge' | 'radar'
  title?: string
  dataSource?: DataSourceConfig
  xField?: string
  yFields?: string[]
  colors?: string[]
  showLegend?: boolean
  showTooltip?: boolean
  animation?: boolean
}
```

### 2. NumberCardWidget.vue

```typescript
// Props 接口
interface NumberCardProps {
  widgetId: string
  title: string
  dataSource?: DataSourceConfig
  field: string              // 数值字段
  prefix?: string            // 前缀 (如 ¥, $)
  suffix?: string            // 后缀 (如 %, 件)
  decimals?: number          // 小数位数
  showTrend?: boolean        // 显示趋势
  trendField?: string        // 趋势对比字段
  colorScheme?: 'primary' | 'success' | 'warning' | 'danger' | 'custom'
  customColor?: string
}
```

### 3. DataTableWidget.vue

```typescript
// Props 接口
interface DataTableProps {
  widgetId: string
  dataSource?: DataSourceConfig
  columns: TableColumnConfig[]
  pageSize?: number
  showPagination?: boolean
  showSummary?: boolean
  summaryType?: 'sum' | 'avg' | 'count'
  striped?: boolean
  border?: boolean
}
```

### 4. ProgressBarWidget.vue

```typescript
// Props 接口
interface ProgressBarProps {
  widgetId: string
  title?: string
  currentField: string       // 当前值字段
  totalField: string         // 目标值字段
  showPercentage?: boolean
  colorScheme?: 'gradient' | 'solid' | 'segmented'
  thresholds?: {             // 阈值颜色
    warning: number          // 警告阈值 (百分比)
    danger: number           // 危险阈值 (百分比)
  }
}
```

### 5. StatusIndicatorWidget.vue

```typescript
// Props 接口
interface StatusIndicatorProps {
  widgetId: string
  title?: string
  statusField: string        // 状态字段
  statusMapping: {           // 状态映射
    [value: string]: {
      label: string
      color: string
      icon?: string
    }
  }
  blinkOnWarning?: boolean   // 警告时闪烁
}
```

## 组件注册要求

每个组件必须在 `ComponentRegistry` 注册:

```typescript
// 注册示例
registry.register({
  type: 'chart',
  name: '图表',
  icon: 'TrendCharts',
  category: 'visualization',
  defaultSize: { width: 4, height: 3 },
  minSize: { width: 2, height: 2 },
  maxSize: { width: 12, height: 8 },
  configSchema: chartConfigSchema,
  component: () => import('./ChartWidget.vue')
})
```

## 配置面板要求

每个组件需要对应的配置面板:
- 数据源选择（报表/SQL）
- 字段映射（X轴、Y轴等）
- 样式配置（颜色、字体）
- 交互配置（刷新间隔）

## 验收标准

1. ✅ 5 种组件正确渲染
2. ✅ 配置面板功能完整
3. ✅ 数据绑定正常工作
4. ✅ 自动刷新功能正常
5. ✅ 响应式布局适配
6. ✅ 组件注册到 Registry
7. ✅ 单元测试覆盖率 > 80%
8. ✅ 无控制台错误

## 完成标志

完成后提交:
1. Git 分支: `feature/dashboard-chart-widgets`
2. Pull Request 标题: `feat: dashboard chart widgets`
3. 确保 Agent 1 的 PR 已合并后再合并
