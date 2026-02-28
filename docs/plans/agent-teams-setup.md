# Agent Teams Superpowers 工作流程

> **重要**: 所有 Agent 必须严格按照此流程执行，不得跳过任何步骤！

## 强制执行规则

1. **顺序执行**: Skills 必须按指定顺序调用
2. **输出验证**: 每个 Skill 必须产生预期输出才能继续
3. **禁止跳过**: 任何步骤不得跳过或合并
4. **记录追踪**: 每个 Skill 执行结果必须记录

---

## Agent 1: dashboard-engine-agent

### Superpowers 流程（严格顺序）

```
┌─────────────────────────────────────────────────────────────┐
│ Step 1: superpowers:using-git-worktrees                     │
│ ─────────────────────────────────────────────────────────── │
│ 目的: 创建隔离工作区                                         │
│ 输出: git worktree 路径                                      │
│ 验证: git worktree list 显示新工作区                         │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 2: superpowers:brainstorming                           │
│ ─────────────────────────────────────────────────────────── │
│ 目的: 确认引擎设计细节                                       │
│ 输入: 设计文档 docs/plans/2026-02-28-dashboard-kanban-design.md │
│ 输出: 确认的设计决策文档                                     │
│ 验证: 设计文档已更新并提交                                   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 3: superpowers:writing-plans                           │
│ ─────────────────────────────────────────────────────────── │
│ 目的: 编写详细实施计划                                       │
│ 输出: docs/plans/phase-1-engine-implementation.md           │
│ 验证: 计划包含所有任务、文件路径、测试策略                   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 4: dashboard-engine-builder (专用 Skill)               │
│ ─────────────────────────────────────────────────────────── │
│ 目的: 获取引擎开发指南                                       │
│ 参考: LayoutEngine, ComponentRegistry, DataBinder 模式      │
│ 验证: 理解所有核心类的设计模式                               │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 5: superpowers:test-driven-development                 │
│ ─────────────────────────────────────────────────────────── │
│ 目的: TDD 开发                                              │
│ 流程:                                                       │
│   1. 写失败测试 → 2. 运行测试 → 3. 写最小实现 → 4. 运行测试  │
│   5. 重构 → 6. 提交                                         │
│ 验证: 所有测试通过，覆盖率 > 80%                            │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 6: superpowers:requesting-code-review                  │
│ ─────────────────────────────────────────────────────────── │
│ 目的: 代码审查                                               │
│ 输出: 代码审查报告                                           │
│ 验证: 审查通过或问题已修复                                   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 7: superpowers:verification-before-completion          │
│ ─────────────────────────────────────────────────────────── │
│ 目的: 完成验证                                               │
│ 检查:                                                       │
│   - 所有测试通过                                             │
│   - TypeScript 无错误                                        │
│   - ESLint 无错误                                            │
│   - 代码已提交                                               │
│   - 文档已更新                                               │
│ 输出: 验证报告                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## Agent 2: chart-component-agent

### Superpowers 流程（严格顺序）

```
Step 1: superpowers:using-git-worktrees
    → 创建隔离工作区
    ↓
Step 2: superpowers:brainstorming
    → 确认图表组件设计（5种组件）
    ↓
Step 3: superpowers:writing-plans
    → 编写图表组件实施计划
    ↓
Step 4: chart-component-builder (专用 Skill)
    → 获取图表开发指南
    ↓
Step 5: superpowers:frontend-design
    → UI/UX 设计（Element Plus + ECharts）
    ↓
Step 6: superpowers:test-driven-development
    → TDD 开发（每个组件独立测试）
    ↓
Step 7: superpowers:requesting-code-review
    → 代码审查
    ↓
Step 8: superpowers:verification-before-completion
    → 完成验证
```

**依赖**: Agent 1 完成 WidgetWrapper 和 ComponentRegistry

---

## Agent 3: kanban-view-agent

### Superpowers 流程（严格顺序）

```
Step 1: superpowers:using-git-worktrees
    → 创建隔离工作区
    ↓
Step 2: superpowers:brainstorming
    → 确认看板设计细节（拖拽、状态列、卡片）
    ↓
Step 3: superpowers:writing-plans
    → 编写看板视图实施计划
    ↓
Step 4: kanban-view-builder (专用 Skill)
    → 获取看板开发指南
    ↓
Step 5: superpowers:frontend-design
    → UI/UX 设计（Trello 风格）
    ↓
Step 6: superpowers:test-driven-development
    → TDD 开发（拖拽逻辑、卡片管理）
    ↓
Step 7: superpowers:requesting-code-review
    → 代码审查
    ↓
Step 8: superpowers:verification-before-completion
    → 完成验证
```

**依赖**: Agent 1 完成 LayoutEngine 和 StateStore

---

## Agent 4: display-mode-agent

### Superpowers 流程（严格顺序）

```
Step 1: superpowers:using-git-worktrees
    → 创建隔离工作区
    ↓
Step 2: superpowers:brainstorming
    → 确认大屏设计细节（全屏、轮播、刷新）
    ↓
Step 3: superpowers:writing-plans
    → 编写大屏实施计划
    ↓
Step 4: fullscreen-display-builder (专用 Skill)
    → 获取大屏开发指南
    ↓
Step 5: superpowers:frontend-design
    → UI/UX 设计（高分辨率适配）
    ↓
Step 6: superpowers:test-driven-development
    → TDD 开发（全屏API、轮播逻辑）
    ↓
Step 7: superpowers:requesting-code-review
    → 代码审查
    ↓
Step 8: superpowers:verification-before-completion
    → 完成验证
```

**依赖**: Agent 1 完成 DataBinder，Agent 2 完成图表组件

---

## Agent 5: coordination-review-agent

### Superpowers 流程（严格顺序）

```
Step 1: superpowers:using-git-worktrees
    → 创建隔离工作区
    ↓
Step 2: superpowers:requesting-code-review
    → 代码审查标准
    ↓
Step 3: superpowers:receiving-code-review
    → 处理审查反馈流程
    ↓
Step 4: code-review-coordinator (专用 Skill)
    → 获取协调审核指南
    ↓
Step 5: superpowers:verification-before-completion
    → 最终验证（集成测试）
```

**职责**:
- Phase 2 期间持续审核 Agent 2, 3, 4 的代码
- Phase 3 进行集成测试
- 生成审核报告

---

## 流程验证清单

### 每个 Agent 启动前检查

- [ ] 是否已调用 Skill tool 加载第一个 Skill?
- [ ] 是否在 Skill 指导下执行?
- [ ] 是否记录了 Skill 执行结果?

### 每个 Skill 完成后检查

- [ ] Skill 是否产生预期输出?
- [ ] 输出是否符合验收标准?
- [ ] 是否已提交相关更改?

### Phase 转换检查

- [ ] 前置 Agent 是否已完成所有 Skills?
- [ ] 验证报告是否通过?
- [ ] 依赖的代码是否已合并?

---

## 违规处理

### 跳过 Skill
- **后果**: 任务标记为失败
- **处理**: 必须从跳过的 Skill 重新开始

### 顺序错误
- **后果**: 当前工作无效
- **处理**: 返回上一个正确完成的 Skill 继续

### 输出不符合
- **后果**: Skill 标记为未完成
- **处理**: 必须修正输出直到符合标准

---

## 启动命令

### 启动 Agent 1 (Phase 1)

```
创建 teammate dashboard-engine-agent 开发看板核心引擎。

你必须严格按照以下 Superpowers 流程执行，不得跳过任何步骤：

1. 首先调用 superpowers:using-git-worktrees 创建隔离工作区
2. 然后调用 superpowers:brainstorming 确认设计细节
3. 调用 superpowers:writing-plans 编写实施计划
4. 调用 dashboard-engine-builder skill 获取开发指南
5. 使用 superpowers:test-driven-development 进行 TDD 开发
6. 完成后调用 superpowers:requesting-code-review 请求代码审查
7. 最后调用 superpowers:verification-before-completion 进行完成验证

任务书位置: docs/plans/agent-tasks/AGENT-1-TASK.md
设计文档: docs/plans/2026-02-28-dashboard-kanban-design.md
```

### 启动 Agent 2-4 (Phase 2 并行)

```
创建 3 个 teammates 并行开发:

1. chart-component-agent - 图表组件
2. kanban-view-agent - 看板视图
3. display-mode-agent - 车间大屏

每个 teammate 必须严格按照 Superpowers 流程执行:
using-git-worktrees → brainstorming → writing-plans → [专用skill] → frontend-design → test-driven-development → requesting-code-review → verification-before-completion

依赖: Agent 1 已完成核心引擎

任务书:
- Agent 2: docs/plans/agent-tasks/AGENT-2-TASK.md
- Agent 3: docs/plans/agent-tasks/AGENT-3-TASK.md
- Agent 4: docs/plans/agent-tasks/AGENT-4-TASK.md
```

### 启动 Agent 5 (Phase 2-3 持续)

```
创建 coordination-review-agent 负责协调和代码审核。

流程:
1. superpowers:using-git-worktrees
2. superpowers:requesting-code-review
3. superpowers:receiving-code-review
4. code-review-coordinator skill
5. superpowers:verification-before-completion

在 Phase 2 期间持续审核其他 Agent 的代码。
在 Phase 3 进行集成测试。

任务书: docs/plans/agent-tasks/AGENT-5-TASK.md
```
