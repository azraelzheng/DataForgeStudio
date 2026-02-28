# Agent 5 任务书: 协调与代码审核

## 基本信息
- **Agent ID**: coordination-review-agent
- **模块**: Code Quality & Integration
- **预计工时**: 3 天
- **优先级**: P2 (Agent 1-4 并行开发期间持续审核)
- **依赖**: Agent 1, 2, 3, 4 (审核各 Agent 产出)

## 必须调用的 Skills（按顺序）

```
1. superpowers:using-git-worktrees  → 创建隔离工作区
2. superpowers:requesting-code-review → 代码审核标准
3. superpowers:receiving-code-review → 处理审核反馈
4. code-review-coordinator          → 获取协调审核指南
5. superpowers:verification-before-completion → 完成验证
```

## 前置条件

等待 Agent 1-4 完成以下里程碑:
- ✅ Agent 1: 核心引擎完成 (LayoutEngine, ComponentRegistry, DataBinder, StateStore)
- ✅ Agent 2: 图表组件完成 (ChartWidget, NumberCardWidget, DataTableWidget)
- ✅ Agent 3: 看板视图完成 (KanbanBoard, KanbanColumn, KanbanCard)
- ✅ Agent 4: 车间大屏完成 (DisplayMode, CarouselPlayer, useFullscreen)

## 任务范围

### 审核范围
```
frontend/src/
├── dashboard/
│   ├── core/                    # Agent 1 审核
│   │   ├── LayoutEngine.ts
│   │   ├── ComponentRegistry.ts
│   │   ├── DataBinder.ts
│   │   └── StateStore.ts
│   ├── widgets/                 # Agent 2 审核
│   │   ├── ChartWidget.vue
│   │   ├── NumberCardWidget.vue
│   │   └── DataTableWidget.vue
│   └── composables/
│       └── useECharts.ts
├── kanban/                      # Agent 3 审核
│   ├── components/
│   │   ├── KanbanBoard.vue
│   │   ├── KanbanColumn.vue
│   │   └── KanbanCard.vue
│   └── composables/
│       └── useDragDrop.ts
└── display/                     # Agent 4 审核
    ├── components/
    │   ├── DisplayMode.vue
    │   └── CarouselPlayer.vue
    └── composables/
        ├── useFullscreen.ts
        └── useCarousel.ts

backend/src/DataForgeStudio.Core/Services/
├── DashboardService.cs          # Agent 1
├── KanbanService.cs             # Agent 3
└── DisplayService.cs            # Agent 4
```

### 不包含
- 功能实现（由 Agent 1-4 负责）
- 单元测试编写（由各 Agent 自行完成）

## 核心功能要求

### 1. 代码一致性审核

```typescript
// 审核检查清单
interface CodeConsistencyCheck {
  // 命名规范
  naming: {
    components: 'PascalCase'      // Vue 组件
    composables: 'camelCase'      // useXxx
    types: 'PascalCase'           // TypeScript 接口
    props: 'camelCase'            // Props 属性
    events: 'kebab-case'          // emit 事件
  }

  // TypeScript 类型
  types: {
    noAny: true                   // 禁止 any
    explicitReturn: true          // 明确返回类型
    interfaceOverType: true       // 优先 interface
  }

  // Vue 组件规范
  vue: {
    scriptSetup: true             // 使用 <script setup>
    defineProps: true             // 使用 defineProps
    defineEmits: true             // 使用 defineEmits
    compositionApi: true          // Composition API
  }

  // 样式规范
  style: {
    scoped: true                  // 必须使用 scoped
    cssVariables: true            // 使用 CSS 变量
    noDeep: false                 // 允许 :deep()
  }
}
```

### 2. 架构合规性审核

```typescript
// 架构检查清单
interface ArchitectureCheck {
  // 模块边界
  boundaries: {
    dashboardCore: ['LayoutEngine', 'ComponentRegistry', 'DataBinder', 'StateStore']
    dashboardWidgets: ['ChartWidget', 'NumberCardWidget', 'DataTableWidget']
    kanbanView: ['KanbanBoard', 'KanbanColumn', 'KanbanCard']
    displayMode: ['DisplayMode', 'CarouselPlayer', 'useFullscreen']
  }

  // 依赖方向
  dependencies: {
    widgets: ['dashboard/core']   // 组件依赖核心
    kanban: ['dashboard/core']    // 看板依赖核心
    display: ['dashboard/core', 'dashboard/widgets']  // 大屏依赖核心+组件
  }

  // 共享代码位置
  sharedCode: {
    types: 'frontend/src/shared/types/'
    utils: 'frontend/src/shared/utils/'
    api: 'frontend/src/api/'
  }
}
```

### 3. API 接口一致性

```typescript
// API 审核清单
interface APIConsistencyCheck {
  // 响应格式
  responseFormat: {
    success: boolean
    message: string
    data: any
    errorCode: string | null
    timestamp: number
  }

  // 命名规范
  endpoints: {
    getAll: 'GET /api/{resource}'
    getById: 'GET /api/{resource}/{id}'
    create: 'POST /api/{resource}'
    update: 'PUT /api/{resource}/{id}'
    delete: 'DELETE /api/{resource}/{id}'
  }

  // 错误处理
  errors: {
    400: '请求参数错误'
    401: '未授权'
    403: '权限不足'
    404: '资源不存在'
    500: '服务器内部错误'
  }
}
```

### 4. 代码冲突解决

```typescript
// 冲突检测
interface ConflictResolution {
  // 文件冲突
  fileConflicts: {
    type: 'same_file'
    agents: string[]
    resolution: 'merge' | 'coordinate' | 'escalate'
  }

  // 接口冲突
  interfaceConflicts: {
    type: 'interface_mismatch'
    file1: string
    file2: string
    resolution: 'unify' | 'adapter' | 'escalate'
  }

  // 依赖冲突
  dependencyConflicts: {
    type: 'version_mismatch'
    package: string
    version1: string
    version2: string
    resolution: 'upgrade' | 'downgrade' | 'split'
  }
}
```

## 审核流程

### Phase 1: 并行审核 (Agent 1-4 开发期间)

```
每完成一个功能模块:
1. 读取该模块代码
2. 检查代码一致性
3. 检查架构合规性
4. 记录问题到共享任务列表
5. 通知相关 Agent 修复
```

### Phase 2: 集成审核 (所有 Agent 完成后)

```
1. 合并所有分支到集成分支
2. 解决代码冲突
3. 检查接口一致性
4. 运行集成测试
5. 生成审核报告
```

### Phase 3: 最终审核 (发布前)

```
1. 完整代码审查
2. 性能检查
3. 安全检查
4. 文档完整性检查
5. 验收测试
```

## 审核报告模板

```markdown
# 代码审核报告

## 审核信息
- **审核日期**: YYYY-MM-DD
- **审核范围**: Agent X - 模块名
- **审核人**: coordination-review-agent

## 审核结果

### 通过项 ✅
- [ ] 命名规范符合要求
- [ ] TypeScript 类型完整
- [ ] 组件结构规范
- [ ] API 接口一致

### 问题项 ❌
| 文件 | 问题 | 严重程度 | 建议 |
|------|------|----------|------|
| file.ts:123 | 使用了 any 类型 | 中 | 定义明确类型 |

### 改进建议 💡
- 建议1
- 建议2

## 结论
- [ ] 通过
- [ ] 需修改后通过
- [ ] 需重新审核
```

## 验收标准

1. ✅ 所有代码符合命名规范
2. ✅ 无 TypeScript 编译错误
3. ✅ 无 ESLint 警告
4. ✅ 组件使用 Composition API
5. ✅ API 响应格式一致
6. ✅ 无代码冲突
7. ✅ 集成测试通过
8. ✅ 生成完整审核报告

## 完成标志

完成后提交:
1. Git 分支: `integration/dashboard-kanban`
2. Pull Request 标题: `chore: code review and integration for dashboard system`
3. 附带完整审核报告
4. 确保所有 Agent 的问题已修复

## 与其他 Agent 协作

### 通信机制
- 使用共享任务列表 (`TaskWrite`) 记录问题
- 使用 Git 分支隔离代码
- 使用 Pull Request 进行代码合并

### 问题升级
- 轻微问题: 直接记录，通知 Agent 修复
- 中等问题: 记录并等待 Agent 响应
- 严重问题: 停止集成，升级到主会话处理

### 审核优先级
1. Agent 1 核心引擎 (最高 - 其他 Agent 依赖)
2. Agent 2 图表组件
3. Agent 3 看板视图
4. Agent 4 车间大屏
