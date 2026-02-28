# Agent 3 任务书: 看板视图开发

## 基本信息
- **Agent ID**: kanban-view-agent
- **模块**: Kanban View (Trello Style)
- **预计工时**: 6 天
- **优先级**: P1 (Agent 1 完成后开始)
- **依赖**: Agent 1 (核心引擎)

## 必须调用的 Skills（按顺序）

```
1. superpowers:using-git-worktrees  → 创建隔离工作区
2. superpowers:brainstorming        → 确认看板设计细节
3. superpowers:writing-plans        → 编写实施计划
4. kanban-view-builder              → 获取看板开发指南
5. superpowers:frontend-design      → UI/UX 设计
6. superpowers:test-driven-development → TDD 开发
7. superpowers:requesting-code-review → 代码审查
8. superpowers:verification-before-completion → 完成验证
```

## 前置条件

等待 Agent 1 完成以下组件:
- ✅ `StateStore.ts` - 状态管理
- ✅ `DataBinder.ts` - 数据绑定
- ✅ 数据库 `KanbanCards` 表

## 任务范围

### 输出文件
```
frontend/src/kanban/
├── components/
│   ├── KanbanBoard.vue           # 看板主体
│   ├── KanbanColumn.vue          # 状态列
│   ├── KanbanCard.vue            # 任务卡片
│   ├── CardDetail.vue            # 卡片详情弹窗
│   ├── CardForm.vue              # 卡片编辑表单
│   └── SwimLaneView.vue          # 泳道视图
├── composables/
│   ├── useDragDrop.ts            # 拖拽逻辑
│   ├── useKanbanFilter.ts        # 筛选/搜索
│   └── useKanbanState.ts         # 状态管理
├── types/
│   └── kanban.ts                 # TypeScript 接口
└── views/
    └── KanbanView.vue            # 看板视图页面

backend/src/DataForgeStudio.Core/Services/
└── KanbanService.cs

backend/src/DataForgeStudio.Api/Controllers/
└── KanbanController.cs
```

### 不包含
- 图表组件（Agent 2 负责）
- 核心引擎（Agent 1 负责）
- 车间大屏（Agent 4 负责）

## 核心功能要求

### 1. KanbanBoard.vue

```vue
<template>
  <div class="kanban-board">
    <!-- 工具栏 -->
    <div class="kanban-toolbar">
      <el-input v-model="search" placeholder="搜索卡片..." />
      <el-select v-model="filterPriority">优先级</el-select>
      <el-select v-model="filterAssignee">负责人</el-select>
      <el-button @click="addCard">添加卡片</el-button>
    </div>

    <!-- 状态列 -->
    <div class="kanban-columns" ref="columnsContainer">
      <KanbanColumn
        v-for="status in statuses"
        :key="status"
        :status="status"
        :cards="getCardsByStatus(status)"
        @card-drop="handleCardDrop"
      />
    </div>

    <!-- 卡片详情弹窗 -->
    <CardDetail v-model="showDetail" :card="selectedCard" />
  </div>
</template>
```

### 2. 拖拽功能 (useDragDrop.ts)

```typescript
export function useDragDrop() {
  // 必须实现:
  const draggedCard = ref<KanbanCard | null>(null)
  const sourceColumn = ref<string | null>(null)
  const dropTarget = ref<{ columnId: string; index: number } | null>(null)

  function startDrag(card: KanbanCard, columnId: string): void
  function endDrag(): void
  function onDrop(columnId: string, index: number): Promise<void>
  function moveCardLocally(cardId: string, from: string, to: string, index: number): void

  return { draggedCard, startDrag, endDrag, onDrop }
}
```

### 3. 状态列 (KanbanColumn.vue)

```typescript
// Props
interface ColumnProps {
  status: string
  title: string
  color?: string
  wipLimit?: number       // WIP 限制
  cards: KanbanCard[]
}

// 功能
- 卡片拖放目标区域
- WIP 限制指示器（超出时红色警告）
- 快速添加卡片按钮
- 卡片计数显示
```

### 4. 任务卡片 (KanbanCard.vue)

```typescript
// Props
interface CardProps {
  card: KanbanCard
  compact?: boolean       // 紧凑模式
}

// 显示内容
- 标题
- 优先级标签（颜色区分）
- 负责人头像
- 截止日期
- 标签
- 自定义字段
```

### 5. 卡片详情 (CardDetail.vue)

```typescript
// 功能
- 完整信息展示
- 编辑表单
- 活动历史
- 附件上传
- 评论功能（可选）
```

### 6. 泳道视图 (SwimLaneView.vue)

```typescript
// 用于车间生产任务
// 按车间/产线分组显示

interface SwimLaneConfig {
  groupBy: 'workshop' | 'production_line' | 'assignee'
  statuses: string[]
}
```

## API 端点要求

```
GET    /api/kanban/{dashboardId}/cards           # 获取卡片列表
POST   /api/kanban/{dashboardId}/cards           # 创建卡片
PUT    /api/kanban/{dashboardId}/cards/{cardId}  # 更新卡片
DELETE /api/kanban/{dashboardId}/cards/{cardId}  # 删除卡片
POST   /api/kanban/{dashboardId}/cards/move      # 移动卡片

Request Body for move:
{
  "cardId": "string",
  "fromStatus": "string",
  "toStatus": "string",
  "newOrder": number
}
```

## 数据绑定要求

看板视图必须支持从报表 SQL 获取数据:

```typescript
interface KanbanDataSource {
  type: 'report' | 'sql'
  reportId?: string
  sql?: string

  // 字段映射
  fieldMapping: {
    id: string              // 卡片ID字段
    title: string           // 标题字段
    status: string          // 状态字段
    priority?: string       // 优先级字段
    assignee?: string       // 负责人字段
    dueDate?: string        // 截止日期字段
  }
}
```

## 验收标准

1. ✅ 卡片拖拽功能正常
2. ✅ 状态列正确显示
3. ✅ WIP 限制警告功能
4. ✅ 筛选/搜索功能
5. ✅ 卡片详情弹窗
6. ✅ 新建/编辑卡片
7. ✅ 数据从 SQL 正确加载
8. ✅ API CRUD 功能完整
9. ✅ 响应式布局
10. ✅ 无控制台错误

## 完成标志

完成后提交:
1. Git 分支: `feature/kanban-view`
2. Pull Request 标题: `feat: kanban view with drag-drop`
3. 确保 Agent 1 的 PR 已合并后再合并
