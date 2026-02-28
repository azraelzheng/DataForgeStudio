# Agent Teams 配置说明

## 配置位置

Agent Teams 配置存储在用户目录：

| 配置项 | 位置 |
|--------|------|
| 主配置 | `C:\Users\azrae\.claude\settings.json` |
| 团队配置 | `C:\Users\azrae\.claude\teams\dashboard-kanban-team\` |
| 任务列表 | `C:\Users\azrae\.claude\tasks\dashboard-kanban-team\` |
| Hooks | `C:\Users\azrae\.claude\hooks\` |

## 已启用的功能

### 环境变量
- `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS`: "1" (已启用)
- `teammateMode`: "in-process" (Windows 不支持 tmux)

### Hooks

#### TeammateIdle
- 文件: `teammate_idle_check.py`
- 作用: 队友空闲前检查代码质量
- 退出码 2: 发送反馈并保持工作

#### TaskCompleted
- 文件: `task_completed_check.py`
- 作用: 任务完成前强制质量门
- 检查项:
  - Git 未提交更改
  - TypeScript 编译错误
  - ESLint 错误
  - 后端编译错误
- 退出码 2: 阻止完成并发送反馈

## 团队成员

| Agent | Skill | 任务书 |
|-------|-------|--------|
| dashboard-engine-agent | dashboard-engine-builder | AGENT-1-TASK.md |
| chart-component-agent | chart-component-builder | AGENT-2-TASK.md |
| kanban-view-agent | kanban-view-builder | AGENT-3-TASK.md |
| display-mode-agent | fullscreen-display-builder | AGENT-4-TASK.md |
| coordination-review-agent | code-review-coordinator | AGENT-5-TASK.md |

## 启动开发

### 方式一：使用 Agent Teams

```
创建一个 agent team 来开发 DataForgeStudio V1.1.0 看板系统。
团队成员: dashboard-engine-agent, chart-component-agent, kanban-view-agent, display-mode-agent, coordination-review-agent
按照 Phase 1 → Phase 2 → Phase 3 顺序执行。
```

### 方式二：手动分阶段

1. Phase 1: 启动 dashboard-engine-agent 完成核心引擎
2. Phase 2: 启动 chart-component-agent, kanban-view-agent, display-mode-agent 并行
3. Phase 2-3: coordination-review-agent 持续审核
4. Phase 3: coordination-review-agent 集成测试

## 质量标准

- TypeScript 类型覆盖: 100% (禁止 any)
- 单元测试覆盖: > 80%
- ESLint: 无错误
- 后端编译: 无错误
- 代码提交: 任务完成前必须提交

## 相关文档

- 设计文档: `docs/plans/2026-02-28-dashboard-kanban-design.md`
- Agent 任务书: `docs/plans/agent-tasks/AGENT-{1-5}-TASK.md`
- 官方文档: https://code.claude.com/docs/zh-CN/agent-teams
