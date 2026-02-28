# Agent 4 任务书: 车间大屏显示

## 基本信息
- **Agent ID**: display-mode-agent
- **模块**: Workshop Display Mode
- **预计工时**: 4 天
- **优先级**: P1 (Agent 1, 2 完成后开始)
- **依赖**: Agent 1 (核心引擎), Agent 2 (图表组件)

## 必须调用的 Skills（按顺序）

```
1. superpowers:using-git-worktrees  → 创建隔离工作区
2. superpowers:brainstorming        → 确认大屏设计细节
3. superpowers:writing-plans        → 编写实施计划
4. fullscreen-display-builder       → 获取大屏开发指南
5. superpowers:frontend-design      → UI/UX 设计
6. superpowers:test-driven-development → TDD 开发
7. superpowers:requesting-code-review → 代码审查
8. superpowers:verification-before-completion → 完成验证
```

## 前置条件

等待 Agent 1, 2 完成以下组件:
- ✅ `DataBinder.ts` - 数据绑定（含自动刷新）
- ✅ `ChartWidget.vue` - 图表组件
- ✅ `NumberCardWidget.vue` - 数字卡片
- ✅ 数据库 `DisplayConfigs` 表

## 任务范围

### 输出文件
```
frontend/src/display/
├── components/
│   ├── DisplayMode.vue           # 全屏模式入口
│   ├── CarouselPlayer.vue        # 轮播播放器
│   ├── DisplayDashboard.vue      # 单看板展示
│   ├── ClockWidget.vue           # 时钟组件
│   └── SystemStatusWidget.vue    # 系统状态指示
├── composables/
│   ├── useFullscreen.ts          # 全屏 API 封装
│   ├── useCarousel.ts            # 轮播逻辑
│   └── useAutoRefresh.ts         # 自动刷新
├── utils/
│   └── licenseCheck.ts           # 许可证验证
├── views/
│   ├── DisplayConfig.vue         # 大屏配置页面
│   └── FullscreenView.vue        # 全屏展示视图
└── types/
    └── display.ts                # TypeScript 接口

backend/src/DataForgeStudio.Core/Services/
└── DisplayService.cs

backend/src/DataForgeStudio.Api/Controllers/
└── DisplayController.cs
```

### 不包含
- 看板引擎（Agent 1 负责）
- 图表组件（Agent 2 负责）
- 看板视图（Agent 3 负责）

## 核心功能要求

### 1. DisplayMode.vue - 全屏模式入口

```vue
<template>
  <div class="display-mode" :class="{ fullscreen: isFullscreen }">
    <!-- 配置面板（非全屏时显示） -->
    <div v-if="!isFullscreen" class="display-config">
      <el-form :model="config">
        <el-form-item label="选择看板">
          <el-select v-model="config.dashboardIds" multiple>
            <el-option v-for="d in dashboards" :key="d.id" :label="d.name" :value="d.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="轮播间隔">
          <el-input-number v-model="config.interval" :min="5" :max="300" :step="5" />
          <span>秒</span>
        </el-form-item>
        <el-form-item label="数据刷新">
          <el-input-number v-model="config.autoRefresh" :min="10" :max="600" :step="10" />
          <span>秒</span>
        </el-form-item>
        <el-form-item label="转场效果">
          <el-select v-model="config.transition">
            <el-option label="淡入淡出" value="fade" />
            <el-option label="滑动" value="slide" />
            <el-option label="无" value="none" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-checkbox v-model="config.showClock">显示时钟</el-checkbox>
          <el-checkbox v-model="config.showDashboardName">显示看板名称</el-checkbox>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="enterDisplayMode">
            进入大屏模式
          </el-button>
        </el-form-item>
      </el-form>
    </div>

    <!-- 轮播内容 -->
    <CarouselPlayer
      v-if="config.dashboardIds.length > 0"
      :dashboard-ids="config.dashboardIds"
      :interval="config.interval"
      :transition="config.transition"
      :auto-refresh="config.autoRefresh"
      :show-clock="config.showClock"
      :show-name="config.showDashboardName"
    />

    <!-- 退出按钮 -->
    <transition name="fade">
      <div v-if="showExitButton" class="exit-button">
        <el-button @click="exitFullscreen">退出大屏</el-button>
      </div>
    </transition>
  </div>
</template>
```

### 2. useFullscreen.ts - 全屏 API

```typescript
export function useFullscreen() {
  const isFullscreen = ref(false)

  // 必须实现:
  async function enterFullscreen(element?: HTMLElement): Promise<void>
  async function exitFullscreen(): Promise<void>
  function toggleFullscreen(element?: HTMLElement): void

  // 监听 fullscreenchange 事件
  // 处理 ESC 键退出
  // 处理浏览器兼容性

  return { isFullscreen, enterFullscreen, exitFullscreen, toggleFullscreen }
}
```

### 3. useCarousel.ts - 轮播逻辑

```typescript
export function useCarousel(
  items: Ref<string[]>,        // Dashboard IDs
  interval: Ref<number>,       // Seconds
  options?: {
    loop?: boolean             // 是否循环
    pauseOnHover?: boolean     // 悬停暂停
    transitionDuration?: number // 转场时间(ms)
  }
) {
  const currentIndex = ref(0)
  const isTransitioning = ref(false)
  const isPaused = ref(false)

  // 必须实现:
  function start(): void
  function stop(): void
  function next(): void
  function prev(): void
  function goTo(index: number): void
  function pause(): void
  function resume(): void

  return { currentIndex, isTransitioning, isPaused, start, stop, next, prev, goTo, pause, resume }
}
```

### 4. useAutoRefresh.ts - 自动刷新

```typescript
export function useAutoRefresh(
  refreshFn: () => Promise<void>,
  interval: Ref<number>,       // Seconds
  options?: {
    immediate?: boolean        // 立即执行
    retryOnError?: boolean     // 错误重试
    maxRetries?: number        // 最大重试次数
  }
) {
  const isLoading = ref(false)
  const lastRefresh = ref<Date | null>(null)
  const error = ref<Error | null>(null)

  // 必须实现:
  async function refresh(): Promise<void>
  function start(): void
  function stop(): void

  return { isLoading, lastRefresh, error, refresh, start, stop }
}
```

### 5. licenseCheck.ts - 许可证验证

```typescript
export async function checkDisplayLicense(): Promise<LicenseStatus> {
  // 调用 /api/license/status
  // 返回许可证状态
}

export function canUseDisplayMode(status: LicenseStatus): boolean {
  // 检查许可证是否有效
  // 车间大屏功能需要许可证
  return status.isValid
}
```

### 6. CarouselPlayer.vue

```vue
<template>
  <div class="carousel-player">
    <transition :name="transition">
      <DisplayDashboard
        v-if="!isTransitioning"
        :key="currentIndex"
        :dashboard-id="currentDashboardId"
        :auto-refresh="autoRefresh"
      />
    </transition>

    <!-- 覆盖层信息 -->
    <div class="carousel-overlay">
      <ClockWidget v-if="showClock" />
      <div v-if="showName" class="dashboard-name">
        {{ currentDashboard?.name }}
      </div>
      <div class="page-indicator">
        {{ currentIndex + 1 }} / {{ dashboardIds.length }}
      </div>
    </div>
  </div>
</template>
```

## API 端点要求

```
GET    /api/display                    # 获取大屏配置列表
GET    /api/display/{id}               # 获取大屏详情
POST   /api/display                    # 创建大屏配置
PUT    /api/display/{id}               # 更新大屏配置
DELETE /api/display/{id}               # 删除大屏配置
GET    /api/display/{id}/data          # 获取聚合数据（所有看板）
```

## 显示模式要求

| 特性 | 要求 |
|------|------|
| 分辨率适配 | 支持 1920x1080, 2560x1440, 3840x2160 |
| 字体缩放 | 根据分辨率自动调整 |
| 时钟格式 | 24小时制，显示时分秒 |
| 日期格式 | YYYY年MM月DD日 星期X |
| 退出方式 | 鼠标移动显示退出按钮，ESC 键退出 |
| 错误处理 | 数据加载失败时显示错误提示，继续轮播 |

## 许可证检查

```typescript
// 在进入大屏模式前必须检查
async function enterDisplayMode() {
  const license = await checkDisplayLicense()
  if (!canUseDisplayMode(license)) {
    ElMessage.error('许可证无效或已过期，无法使用大屏模式')
    return
  }
  await enterFullscreen()
}
```

## 验收标准

1. ✅ 全屏模式正常进入/退出
2. ✅ 多看板轮播功能
3. ✅ 定时数据刷新
4. ✅ 许可证验证
5. ✅ 时钟显示正确
6. ✅ ESC 键退出
7. ✅ 鼠标移动显示退出按钮
8. ✅ 转场动画流畅
9. ✅ 高分辨率适配
10. ✅ API 端点完整
11. ✅ 无控制台错误

## 完成标志

完成后提交:
1. Git 分支: `feature/workshop-display-mode`
2. Pull Request 标题: `feat: workshop fullscreen display mode`
3. 确保 Agent 1, 2 的 PR 已合并后再合并
