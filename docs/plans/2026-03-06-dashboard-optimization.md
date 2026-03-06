# 大屏管理模块优化实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 优化 DataForgeStudio 大屏管理模块的 UI/UX 设计、渲染性能和数据流处理，打造高性能大屏可视化系统。

**Architecture:** 采用渐进式优化策略，从 P0 核心性能问题开始，逐步完善 UI/UX 设计。使用 composables 封装可复用逻辑，遵循 Vue 3 最佳实践。

**Tech Stack:** Vue 3 + TypeScript + ECharts + Element Plus + Canvas API

**NotebookLM 同步:** 每完成一项任务后，将进度同步到 NotebookLM 笔记本 `dataforgestudio-大屏可视化开发指南`

---

## Phase 1: P0 高优先级任务

### Task 1: setInterval → requestAnimationFrame 动画优化

**问题：** `useAutoRefresh.ts:125` 和 `DataBinder.ts:244` 使用 `setInterval` 刷新数据，不符合高性能大屏规范。

**Files:**
- Modify: `frontend/src/display/composables/useAutoRefresh.ts:62-128`
- Modify: `frontend/src/dashboard/core/DataBinder.ts:66-68,244,267`
- Modify: `frontend/src/views/dashboard/PublicDashboard.vue:960`
- Modify: `frontend/src/views/dashboard/DashboardView.vue:1050`
- Test: `frontend/src/display/composables/__tests__/useAutoRefresh.test.ts`

**Step 1: 创建 useAnimationFrame composable**

```typescript
// frontend/src/display/composables/useAnimationFrame.ts
import { ref, onUnmounted } from 'vue'

export function useAnimationFrame() {
  const frameId = ref<number | null>(null)
  const isRunning = ref(false)

  function start(callback: (deltaTime: number) => void) {
    if (isRunning.value) return

    isRunning.value = true
    let lastTime = performance.now()

    function tick(currentTime: number) {
      if (!isRunning.value) return

      const deltaTime = currentTime - lastTime
      lastTime = currentTime

      callback(deltaTime)
      frameId.value = requestAnimationFrame(tick)
    }

    frameId.value = requestAnimationFrame(tick)
  }

  function stop() {
    isRunning.value = false
    if (frameId.value !== null) {
      cancelAnimationFrame(frameId.value)
      frameId.value = null
    }
  }

  onUnmounted(() => {
    stop()
  })

  return {
    start,
    stop,
    isRunning,
    frameId
  }
}

/**
 * 基于 rAF 的定时刷新 Hook
 * 替代 setInterval，与浏览器刷新率同步
 */
export function useRAFTimer(callback: () => void, interval: Ref<number>) {
  const { start: startRAF, stop: stopRAF, isRunning } = useAnimationFrame()
  let elapsed = 0

  function start() {
    stopRAF()
    if (interval.value <= 0) return

    elapsed = 0
    startRAF((deltaTime) => {
      elapsed += deltaTime
      if (elapsed >= interval.value * 1000) {
        callback()
        elapsed = 0
      }
    })
  }

  function stop() {
    stopRAF()
    elapsed = 0
  }

  return { start, stop, isRunning }
}
```

**Step 2: 修改 useAutoRefresh.ts 使用 rAF**

在 `useAutoRefresh.ts` 中替换 `setInterval`:

```typescript
// 替换原来的 timer: ReturnType<typeof setInterval>
import { useRAFTimer } from './useAnimationFrame'

// 在 useAutoRefresh 函数中:
const { start: startTimer, stop: stopTimer } = useRAFTimer(refresh, interval)

// 修改 start 函数:
function start(): void {
  stop()
  if (interval.value <= 0) {
    console.warn('[useAutoRefresh] 刷新间隔必须大于 0')
    return
  }
  isActive = true
  if (opts.immediate) {
    refresh()
  }
  startTimer() // 使用 rAF timer
}

// 修改 stop 函数:
function stop(): void {
  isActive = false
  stopTimer() // 停止 rAF timer
}
```

**Step 3: 修改 DataBinder.ts 使用 rAF**

```typescript
// 在 DataBinder 类中添加:
import { useRAFTimer } from '../composables/useAnimationFrame'

// 替换 globalRefreshTimer 和 refreshTimers 的实现
// 使用 Map 存储 rAF 控制器
private refreshControllers: Map<string, { start: () => void, stop: () => void }> = new Map()
```

**Step 4: 运行测试验证**

Run: `cd frontend && npm run test -- useAutoRefresh.test.ts`
Expected: PASS (需要先编写测试)

**Step 5: 提交更改**

```bash
git add frontend/src/display/composables/useAnimationFrame.ts
git add frontend/src/display/composables/useAutoRefresh.ts
git add frontend/src/dashboard/core/DataBinder.ts
git commit -m "perf(display): replace setInterval with requestAnimationFrame for smoother animations

- Add useAnimationFrame composable for rAF-based timing
- Update useAutoRefresh to use rAF instead of setInterval
- Update DataBinder to use rAF for data refresh scheduling

Refs: Dashboard Optimization Plan Task 1"
```

**Step 6: 同步进度到 NotebookLM**

```bash
# 向 NotebookLM 报告 Task 1 完成状态
```

---

### Task 2: 添加局部重绘支持

**问题：** 数据更新时重新渲染整个图表/组件，造成性能浪费。

**Files:**
- Create: `frontend/src/dashboard/composables/usePartialUpdate.ts`
- Modify: `frontend/src/dashboard/composables/useECharts.ts`
- Modify: `frontend/src/dashboard/core/DataBinder.ts:196-206`
- Test: `frontend/src/dashboard/composables/__tests__/usePartialUpdate.test.ts`

**Step 1: 创建 usePartialUpdate composable**

```typescript
// frontend/src/dashboard/composables/usePartialUpdate.ts
import { ref, type Ref } from 'vue'
import type { ECharts } from 'echarts'

export interface PartialUpdateOptions {
  /** 是否启用局部更新 */
  enabled: boolean
  /** 更新模式: 'replace' | 'append' | 'merge' */
  mode: 'replace' | 'append' | 'merge'
  /** 最大数据点数（超过则裁剪） */
  maxDataPoints?: number
}

const DEFAULT_OPTIONS: PartialUpdateOptions = {
  enabled: true,
  mode: 'merge'
}

/**
 * 图表局部更新 Hook
 * 只更新数据部分，避免完整重绘
 */
export function usePartialUpdate(
  chartInstance: Ref<ECharts | undefined>,
  options: PartialUpdateOptions = DEFAULT_OPTIONS
) {
  const isUpdating = ref(false)

  /**
   * 更新单个系列的数据
   */
  function updateSeriesData(seriesIndex: number, data: unknown[]) {
    if (!chartInstance.value || isUpdating.value) return

    isUpdating.value = true

    try {
      const option = chartInstance.value.getOption() as Record<string, unknown>
      const series = option.series as Record<string, unknown>[]

      if (series && series[seriesIndex]) {
        // 只更新数据，不改变其他配置
        series[seriesIndex] = {
          ...series[seriesIndex],
          data
        }

        chartInstance.value.setOption(
          { series },
          { notMerge: false, replaceMerge: ['series'] }
        )
      }
    } finally {
      isUpdating.value = false
    }
  }

  /**
   * 追加数据到系列末尾
   */
  function appendSeriesData(seriesIndex: number, newData: unknown | unknown[]) {
    if (!chartInstance.value) return

    const option = chartInstance.value.getOption() as Record<string, unknown>
    const series = option.series as Record<string, unknown>[]

    if (series && series[seriesIndex]) {
      const currentData = (series[seriesIndex].data as unknown[]) || []
      const dataToAdd = Array.isArray(newData) ? newData : [newData]

      let updatedData = [...currentData, ...dataToAdd]

      // 裁剪超出最大数据点的数据
      if (options.maxDataPoints && updatedData.length > options.maxDataPoints) {
        updatedData = updatedData.slice(-options.maxDataPoints)
      }

      updateSeriesData(seriesIndex, updatedData)
    }
  }

  /**
   * 增量更新（只更新变化的数据点）
   */
  function deltaUpdate(changes: Array<{ index: number; value: unknown }>) {
    if (!chartInstance.value) return

    const option = chartInstance.value.getOption() as Record<string, unknown>
    const series = option.series as Record<string, unknown>[]

    if (!series) return

    changes.forEach(({ index, value }) => {
      if (series[0] && (series[0].data as unknown[])) {
        (series[0].data as unknown[])[index] = value
      }
    })

    chartInstance.value.setOption({ series }, { notMerge: false })
  }

  return {
    isUpdating,
    updateSeriesData,
    appendSeriesData,
    deltaUpdate
  }
}
```

**Step 2: 修改 useECharts.ts 添加局部更新支持**

在 useECharts 返回值中添加 partialUpdate 方法:

```typescript
// 在 useECharts.ts 中添加:
import { usePartialUpdate } from './usePartialUpdate'

// 在返回值中:
const { updateSeriesData, appendSeriesData, deltaUpdate } = usePartialUpdate(chartInstance)

return {
  // ...existing returns
  updateSeriesData,
  appendSeriesData,
  deltaUpdate
}
```

**Step 3: 修改 DataBinder.ts 使用局部更新**

```typescript
// 在 DataBinder.refresh 方法中添加局部更新逻辑:
async refresh(widgetId: string, usePartialUpdate: boolean = true): Promise<void> {
  const binding = this.bindings.get(widgetId)
  if (!binding) return

  const source = this.sources.get(binding.sourceId)
  if (!source) return

  // 如果启用局部更新且有缓存数据，尝试增量更新
  if (usePartialUpdate && binding.state.value.data) {
    // 执行增量更新逻辑
    const newData = await source.fetcher()
    this.applyDeltaUpdate(widgetId, binding.state.value.data, newData)
  } else {
    // 完整更新
    binding.state.value.isLoading = true
    try {
      const data = await source.fetcher()
      binding.state.value.data = this.applyFieldMapping(data, binding.fieldMapping)
      binding.state.value.lastRefresh = new Date()
    } finally {
      binding.state.value.isLoading = false
    }
  }
}
```

**Step 4: 提交更改**

```bash
git add frontend/src/dashboard/composables/usePartialUpdate.ts
git add frontend/src/dashboard/composables/useECharts.ts
git add frontend/src/dashboard/core/DataBinder.ts
git commit -m "feat(dashboard): add partial repaint support for better performance

- Add usePartialUpdate composable for incremental chart updates
- Support append, replace, and delta update modes
- Update DataBinder to use partial updates when possible

Refs: Dashboard Optimization Plan Task 2"
```

**Step 5: 同步进度到 NotebookLM**

---

## Phase 2: P1 中优先级任务

### Task 3: 玻璃拟态样式实现

**Files:**
- Create: `frontend/src/styles/glassmorphism.scss`
- Modify: `frontend/src/views/dashboard/PublicDashboard.vue` (样式部分)
- Modify: `frontend/src/views/dashboard/DashboardView.vue` (样式部分)

**Step 1: 创建玻璃拟态样式文件**

```scss
// frontend/src/styles/glassmorphism.scss

// 玻璃拟态基础样式
.glass-panel {
  background: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 12px;
  box-shadow:
    0 4px 30px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.1);
}

// 玻璃拟态变体 - 深色
.glass-panel--dark {
  background: rgba(10, 15, 30, 0.7);
  backdrop-filter: blur(16px);
  border: 1px solid rgba(255, 255, 255, 0.05);
}

// 玻璃拟态变体 - 浅色（用于亮色主题）
.glass-panel--light {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(12px);
  border: 1px solid rgba(0, 0, 0, 0.1);
  box-shadow:
    0 4px 30px rgba(0, 0, 0, 0.1),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
}

// 高亮边框效果
.glass-panel--highlight {
  border: 1px solid rgba(0, 217, 255, 0.3);
  box-shadow:
    0 4px 30px rgba(0, 217, 255, 0.15),
    inset 0 1px 0 rgba(255, 255, 255, 0.1);
}

// 动态发光效果（用于重要指标）
.glass-panel--glow {
  animation: glass-glow 3s ease-in-out infinite;
}

@keyframes glass-glow {
  0%, 100% {
    box-shadow:
      0 4px 30px rgba(0, 217, 255, 0.15),
      inset 0 1px 0 rgba(255, 255, 255, 0.1);
  }
  50% {
    box-shadow:
      0 4px 40px rgba(0, 217, 255, 0.3),
      inset 0 1px 0 rgba(255, 255, 255, 0.15);
  }
}

// 响应式模糊强度（高分辨率屏幕使用更强的模糊）
@media (min-width: 2560px) {
  .glass-panel {
    backdrop-filter: blur(16px);
  }
}

@media (min-width: 3840px) {
  .glass-panel {
    backdrop-filter: blur(20px);
  }
}
```

**Step 2: 在组件中应用玻璃拟态样式**

修改 `PublicDashboard.vue` 和 `DashboardView.vue`:

```vue
<style>
@import '@/styles/glassmorphism.scss';

.widget-container {
  /* 替换原有样式 */
  box-sizing: border-box;

  /* 应用玻璃拟态 */
  @extend .glass-panel;
}
</style>
```

**Step 3: 提交更改**

```bash
git add frontend/src/styles/glassmorphism.scss
git add frontend/src/views/dashboard/PublicDashboard.vue
git add frontend/src/views/dashboard/DashboardView.vue
git commit -m "style(dashboard): add glassmorphism styles for modern UI

- Add glassmorphism.scss with blur, transparency, and glow effects
- Apply to widget containers in dashboard views
- Support dark/light theme variants

Refs: Dashboard Optimization Plan Task 3"
```

**Step 4: 同步进度到 NotebookLM**

---

### Task 4: 语义化色彩字典

**Files:**
- Create: `frontend/src/styles/colors/_semantic.scss`
- Create: `frontend/src/styles/colors/_chart.scss`
- Modify: `frontend/src/components/dashboard/DashboardChart.vue:212-223`

**Step 1: 创建语义化色彩文件**

```scss
// frontend/src/styles/colors/_semantic.scss

// 状态语义色
$color-success: #22c55e;    // 健康/达成/正常
$color-warning: #f59e0b;    // 待观察/风险/注意
$color-danger: #ef4444;     // 警告/异常/错误
$color-info: #3b82f6;       // 信息/中性

// 状态色映射
$semantic-colors: (
  'success': $color-success,
  'warning': $color-warning,
  'danger': $color-danger,
  'info': $color-info,
  'primary': #3b82f6,
  'secondary': #6b7280
);

// 辅助函数：获取语义色
@function semantic-color($name) {
  @return map-get($semantic-colors, $name);
}
```

```scss
// frontend/src/styles/colors/_chart.scss

// 图表专用色板（限制为6色，符合大屏规范）
$chart-colors-primary: (
  #3b82f6,  // 蓝色 - 主数据
  #22c55e,  // 绿色 - 正向数据
  #f59e0b,  // 橙色 - 警告数据
  #ef4444,  // 红色 - 负向数据
  #8b5cf6,  // 紫色 - 辅助数据
  #06b6d4   // 青色 - 对比数据
);

// 扩展色板（需要更多颜色时使用，最多8色）
$chart-colors-extended: (
  #3b82f6,
  #22c55e,
  #f59e0b,
  #ef4444,
  #8b5cf6,
  #06b6d4,
  #ec4899,
  #14b8a6
);

// 冷暖对比色板
$chart-colors-warm: (#ff6b6b, #ff8e72, #ffb347, #ffcc5c);
$chart-colors-cool: (#4ecdc4, #45b7d1, #5dade2, #3498db);

// CSS 变量导出
:root {
  --chart-color-1: #{nth($chart-colors-primary, 1)};
  --chart-color-2: #{nth($chart-colors-primary, 2)};
  --chart-color-3: #{nth($chart-colors-primary, 3)};
  --chart-color-4: #{nth($chart-colors-primary, 4)};
  --chart-color-5: #{nth($chart-colors-primary, 5)};
  --chart-color-6: #{nth($chart-colors-primary, 6)};
}
```

**Step 2: 创建 TypeScript 版本的色彩字典**

```typescript
// frontend/src/styles/colors/index.ts

export const semanticColors = {
  success: '#22c55e',
  warning: '#f59e0b',
  danger: '#ef4444',
  info: '#3b82f6'
} as const

export const chartColors = {
  // 主色板（6色，符合大屏规范）
  primary: ['#3b82f6', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4'],

  // 扩展色板（最多8色）
  extended: ['#3b82f6', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4', '#ec4899', '#14b8a6'],

  // 冷暖色板
  warm: ['#ff6b6b', '#ff8e72', '#ffb347', '#ffcc5c'],
  cool: ['#4ecdc4', '#45b7d1', '#5dade2', '#3498db']
} as const

/**
 * 根据值获取状态色
 */
export function getStatusColor(value: number, thresholds: { warning: number; danger: number }): string {
  if (value >= thresholds.danger) return semanticColors.danger
  if (value >= thresholds.warning) return semanticColors.warning
  return semanticColors.success
}
```

**Step 3: 修改 DashboardChart.vue 使用语义化色彩**

```typescript
// 替换 DashboardChart.vue:212-223
import { chartColors } from '@/styles/colors'

const defaultColorScheme = chartColors.primary // 使用统一的色彩字典
```

**Step 4: 提交更改**

```bash
git add frontend/src/styles/colors/
git add frontend/src/components/dashboard/DashboardChart.vue
git commit -m "style(dashboard): add semantic color dictionary

- Add semantic colors for status (success/warning/danger/info)
- Limit chart palette to 6 colors per large screen standards
- Add TypeScript color utilities with getStatusColor helper

Refs: Dashboard Optimization Plan Task 4"
```

**Step 5: 同步进度到 NotebookLM**

---

### Task 5: 响应式字体（1-3-7米规则）

**Files:**
- Create: `frontend/src/display/composables/useResponsiveFont.ts`
- Modify: `frontend/src/views/dashboard/PublicDashboard.vue` (应用响应式字体)

**Step 1: 创建响应式字体 Hook**

```typescript
// frontend/src/display/composables/useResponsiveFont.ts
import { computed, type Ref } from 'vue'

export type ViewingDistance = 'near' | 'medium' | 'far'

export interface ResponsiveFontOptions {
  /** 观看距离 */
  distance?: ViewingDistance
  /** 基准字号（近距时的字号） */
  baseFontSize?: number
  /** 设计稿宽度 */
  designWidth?: number
}

/**
 * 响应式字体 Hook
 * 基于人机工程学 1-3-7 米规则计算字体大小
 *
 * 规则说明：
 * - 近距离 (1-3米): 个人控制台，最小字符高度 4-6mm
 * - 中距离 (3-7米): 会议室看板，最小字符高度 6-12mm
 * - 远距离 (7米+): 展厅/指挥中心，最小字符高度 12mm+
 *
 * 经验公式：每1米观看距离对应1厘米字体高度
 */
export function useResponsiveFont(
  options: ResponsiveFontOptions = {},
  viewportWidth?: Ref<number>
) {
  const {
    distance = 'medium',
    baseFontSize = 16,
    designWidth = 1920
  } = options

  // 距离对应的字号缩放因子
  const distanceScale: Record<ViewingDistance, number> = {
    near: 1,      // 基准
    medium: 1.5,  // 1.5倍
    far: 2.25     // 2.25倍
  }

  // 计算视口缩放比例
  const viewportScale = computed(() => {
    if (!viewportWidth?.value) return 1
    return Math.min(viewportWidth.value / designWidth, 1)
  })

  // 计算最终字号
  const scale = computed(() => {
    return distanceScale[distance] * viewportScale.value
  })

  // 字体大小预设
  const fontSize = computed(() => ({
    // 基础文本
    xs: `${Math.round(baseFontSize * 0.75 * scale.value)}px`,
    sm: `${Math.round(baseFontSize * 0.875 * scale.value)}px`,
    base: `${Math.round(baseFontSize * scale.value)}px`,
    lg: `${Math.round(baseFontSize * 1.125 * scale.value)}px`,
    xl: `${Math.round(baseFontSize * 1.25 * scale.value)}px`,

    // 标题
    '2xl': `${Math.round(baseFontSize * 1.5 * scale.value)}px`,
    '3xl': `${Math.round(baseFontSize * 1.875 * scale.value)}px`,
    '4xl': `${Math.round(baseFontSize * 2.25 * scale.value)}px`,
    '5xl': `${Math.round(baseFontSize * 3 * scale.value)}px`,

    // 大屏专用
    kpi: `${Math.round(baseFontSize * 3.5 * scale.value)}px`,      // KPI 数字
    kpiLabel: `${Math.round(baseFontSize * 1.25 * scale.value)}px`, // KPI 标签
    title: `${Math.round(baseFontSize * 2 * scale.value)}px`,       // 组件标题
    subtitle: `${Math.round(baseFontSize * 1.5 * scale.value)}px`   // 副标题
  }))

  // 行高预设
  const lineHeight = computed(() => ({
    tight: 1.25 * scale.value,
    normal: 1.5 * scale.value,
    relaxed: 1.75 * scale.value
  }))

  /**
   * 获取特定用途的字体样式
   */
  function getFontStyle(purpose: 'kpi' | 'label' | 'title' | 'table' | 'chart') {
    const styles: Record<string, Record<string, string>> = {
      kpi: {
        fontSize: fontSize.value.kpi,
        fontWeight: 'bold',
        lineHeight: String(lineHeight.value.tight)
      },
      label: {
        fontSize: fontSize.value.kpiLabel,
        fontWeight: '500',
        lineHeight: String(lineHeight.value.normal)
      },
      title: {
        fontSize: fontSize.value.title,
        fontWeight: '600',
        lineHeight: String(lineHeight.value.tight)
      },
      table: {
        fontSize: fontSize.value.sm,
        fontWeight: 'normal',
        lineHeight: String(lineHeight.value.normal)
      },
      chart: {
        fontSize: fontSize.value.base,
        fontWeight: 'normal',
        lineHeight: String(lineHeight.value.normal)
      }
    }

    return styles[purpose]
  }

  return {
    fontSize,
    lineHeight,
    scale,
    getFontStyle
  }
}

/**
 * 基于物理距离自动选择字体大小的 Hook
 */
export function useAutoDistanceFont(
  containerRef: Ref<HTMLElement | undefined>,
  options: Omit<ResponsiveFontOptions, 'distance'> = {}
) {
  const distance = ref<ViewingDistance>('medium')

  // 简单的距离估算（基于容器宽度）
  watch(containerRef, (el) => {
    if (!el) return

    const width = el.clientWidth
    // 基于容器宽度估算观看距离
    if (width >= 3840) {
      distance.value = 'far'
    } else if (width >= 2560) {
      distance.value = 'medium'
    } else {
      distance.value = 'near'
    }
  })

  const responsiveFont = useResponsiveFont({ ...options, distance: distance.value })

  return {
    ...responsiveFont,
    distance
  }
}
```

**Step 2: 在大屏组件中应用**

```vue
<!-- 在 PublicDashboard.vue 中 -->
<script setup>
import { useResponsiveFont } from '@/display/composables/useResponsiveFont'

// 根据大屏类型选择观看距离
const viewingDistance = computed(() => {
  // 可以从配置中读取
  return dashboardInfo.settings?.viewingDistance || 'medium'
})

const { fontSize, getFontStyle } = useResponsiveFont({
  distance: viewingDistance.value
})
</script>

<template>
  <div class="card-value" :style="getFontStyle('kpi')">
    {{ formatCardValue(widget) }}
  </div>
  <div class="card-label" :style="getFontStyle('label')">
    {{ widget.title || '数据' }}
  </div>
</template>
```

**Step 3: 提交更改**

```bash
git add frontend/src/display/composables/useResponsiveFont.ts
git add frontend/src/views/dashboard/PublicDashboard.vue
git commit -m "feat(display): add responsive font based on 1-3-7 meter rule

- Add useResponsiveFont composable with distance-based scaling
- Support near/medium/far viewing distances
- Provide predefined font sizes for KPI, label, title, etc.

Refs: Dashboard Optimization Plan Task 5"
```

**Step 4: 同步进度到 NotebookLM**

---

## Phase 3: P2 中优先级任务

### Task 6: 数据缓存和请求去重

**Files:**
- Create: `frontend/src/dashboard/core/DataCache.ts`
- Modify: `frontend/src/dashboard/core/DataBinder.ts`

**Step 1: 创建 DataCache 类**

```typescript
// frontend/src/dashboard/core/DataCache.ts

interface CacheEntry<T> {
  data: T
  timestamp: number
  ttl: number
}

interface PendingRequest<T> {
  promise: Promise<T>
  timestamp: number
}

/**
 * 数据缓存管理器
 * 支持 TTL、请求去重、LRU 淘汰
 */
export class DataCache<T = unknown> {
  private cache = new Map<string, CacheEntry<T>>()
  private pending = new Map<string, PendingRequest<T>>()
  private accessOrder: string[] = []
  private maxSize: number

  constructor(maxSize: number = 100) {
    this.maxSize = maxSize
  }

  /**
   * 获取缓存数据或执行请求
   * 自动处理缓存命中、请求去重
   */
  async fetch(
    key: string,
    fetcher: () => Promise<T>,
    ttl: number = 60000 // 默认 1 分钟
  ): Promise<T> {
    // 1. 检查缓存
    const cached = this.cache.get(key)
    if (cached && Date.now() - cached.timestamp < cached.ttl) {
      this.updateAccessOrder(key)
      return cached.data
    }

    // 2. 检查是否有相同的请求正在进行（去重）
    const pending = this.pending.get(key)
    if (pending && Date.now() - pending.timestamp < 30000) {
      return pending.promise
    }

    // 3. 发起新请求
    const promise = fetcher().then(data => {
      this.set(key, data, ttl)
      this.pending.delete(key)
      return data
    }).catch(err => {
      this.pending.delete(key)
      throw err
    })

    this.pending.set(key, { promise, timestamp: Date.now() })
    return promise
  }

  /**
   * 设置缓存
   */
  set(key: string, data: T, ttl: number = 60000): void {
    // LRU 淘汰
    if (this.cache.size >= this.maxSize && !this.cache.has(key)) {
      const oldest = this.accessOrder.shift()
      if (oldest) {
        this.cache.delete(oldest)
      }
    }

    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl
    })
    this.updateAccessOrder(key)
  }

  /**
   * 获取缓存（不触发请求）
   */
  get(key: string): T | undefined {
    const entry = this.cache.get(key)
    if (!entry) return undefined

    if (Date.now() - entry.timestamp >= entry.ttl) {
      this.cache.delete(key)
      return undefined
    }

    this.updateAccessOrder(key)
    return entry.data
  }

  /**
   * 清除缓存
   */
  clear(key?: string): void {
    if (key) {
      this.cache.delete(key)
      this.accessOrder = this.accessOrder.filter(k => k !== key)
    } else {
      this.cache.clear()
      this.accessOrder = []
    }
  }

  /**
   * 获取缓存统计
   */
  getStats() {
    return {
      size: this.cache.size,
      pendingCount: this.pending.size,
      keys: Array.from(this.cache.keys())
    }
  }

  private updateAccessOrder(key: string): void {
    const index = this.accessOrder.indexOf(key)
    if (index !== -1) {
      this.accessOrder.splice(index, 1)
    }
    this.accessOrder.push(key)
  }
}

// 全局缓存实例
export const globalDataCache = new DataCache()
```

**Step 2: 在 DataBinder 中集成缓存**

```typescript
// 在 DataBinder.ts 中:
import { globalDataCache } from './DataCache'

// 修改 refresh 方法:
async refresh(widgetId: string): Promise<void> {
  const binding = this.bindings.get(widgetId)
  if (!binding) return

  const source = this.sources.get(binding.sourceId)
  if (!source) return

  binding.state.value.isLoading = true
  binding.state.value.error = null

  try {
    // 使用缓存
    const cacheKey = `${binding.sourceId}:${JSON.stringify(binding.fieldMapping)}`
    const data = await globalDataCache.fetch(
      cacheKey,
      source.fetcher,
      60000 // 1 分钟 TTL
    )

    binding.state.value.data = this.applyFieldMapping(data, binding.fieldMapping)
    binding.state.value.lastRefresh = new Date()
  } catch (error) {
    binding.state.value.error = error instanceof Error ? error : new Error(String(error))
    console.error(`[DataBinder] 刷新组件 "${widgetId}" 数据失败:`, error)
  } finally {
    binding.state.value.isLoading = false
  }
}
```

**Step 3: 提交更改**

```bash
git add frontend/src/dashboard/core/DataCache.ts
git add frontend/src/dashboard/core/DataBinder.ts
git commit -m "feat(dashboard): add data cache with deduplication and LRU eviction

- Add DataCache class with TTL support
- Implement request deduplication for concurrent requests
- Integrate cache into DataBinder for automatic caching

Refs: Dashboard Optimization Plan Task 6"
```

**Step 4: 同步进度到 NotebookLM**

---

### Task 7: 虚拟化渲染（大表格）

**Files:**
- Modify: `frontend/src/components/dashboard/DashboardTable.vue`
- Add: Element Plus virtual table dependency

**Step 1: 安装 Element Plus 虚拟表格**

```bash
cd frontend && npm install @element-plus/components-virtual-table
```

**Step 2: 修改 DashboardTable.vue 使用虚拟滚动**

```vue
<!-- 使用 el-table-v2 替代 el-table -->
<template>
  <div class="dashboard-table" ref="containerRef">
    <el-table-v2
      v-if="useVirtual"
      :columns="columns"
      :data="tableData"
      :width="tableWidth"
      :height="tableHeight"
      :row-height="48"
      :header-height="48"
      fixed
    />
    <el-table
      v-else
      :data="tableData"
      :style="{ width: '100%', height: '100%' }"
      size="small"
      border
    >
      <!-- 原有表格实现 -->
    </el-table>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { ElTableV2 } from '@element-plus/components-virtual-table'

const VIRTUAL_THRESHOLD = 100 // 超过 100 行启用虚拟滚动

const useVirtual = computed(() => {
  return tableData.value.length > VIRTUAL_THRESHOLD
})

// 转换列格式为 el-table-v2 所需格式
const columns = computed(() => {
  return props.columnConfig.map(col => ({
    key: col.field,
    title: col.label,
    width: col.width || 150,
    align: col.align || 'left'
  }))
})
</script>
```

**Step 3: 提交更改**

```bash
git add frontend/src/components/dashboard/DashboardTable.vue
git add frontend/package.json
git commit -m "perf(dashboard): add virtual scrolling for large tables

- Use el-table-v2 for tables with >100 rows
- Auto-detect and switch between virtual/normal mode
- Maintain styling consistency with glass-panel theme

Refs: Dashboard Optimization Plan Task 7"
```

**Step 4: 同步进度到 NotebookLM**

---

### Task 8: 性能监控和降级策略

**Files:**
- Create: `frontend/src/display/composables/usePerformanceMonitor.ts`
- Create: `frontend/src/display/composables/usePerformanceDegradation.ts`

**Step 1: 创建性能监控 Hook**

```typescript
// frontend/src/display/composables/usePerformanceMonitor.ts
import { ref, onMounted, onUnmounted } from 'vue'

export interface PerformanceStats {
  fps: number
  memoryUsage: number | null
  shouldReduceMotion: boolean
  isLowPerformance: boolean
}

/**
 * 性能监控 Hook
 * 实时监测 FPS 和内存使用情况
 */
export function usePerformanceMonitor(options: {
  /** 触发降级的 FPS 阈值 */
  lowFpsThreshold?: number
  /** 监测间隔（毫秒） */
  sampleInterval?: number
} = {}) {
  const {
    lowFpsThreshold = 30,
    sampleInterval = 1000
  } = options

  const fps = ref(60)
  const memoryUsage = ref<number | null>(null)
  const shouldReduceMotion = ref(false)
  const isLowPerformance = ref(false)

  let frameCount = 0
  let lastTime = performance.now()
  let animationId: number | null = null

  function measureFrame() {
    frameCount++
    animationId = requestAnimationFrame(measureFrame)
  }

  function sample() {
    const now = performance.now()
    const elapsed = now - lastTime

    if (elapsed >= sampleInterval) {
      fps.value = Math.round((frameCount * 1000) / elapsed)
      frameCount = 0
      lastTime = now

      // 检测低性能
      isLowPerformance.value = fps.value < lowFpsThreshold
      shouldReduceMotion.value = fps.value < lowFpsThreshold

      // 获取内存使用（如果可用）
      if ((performance as any).memory) {
        memoryUsage.value = (performance as any).memory.usedJSHeapSize / (1024 * 1024)
      }
    }
  }

  onMounted(() => {
    measureFrame()
    setInterval(sample, sampleInterval)
  })

  onUnmounted(() => {
    if (animationId !== null) {
      cancelAnimationFrame(animationId)
    }
  })

  const stats: PerformanceStats = {
    get fps() { return fps.value },
    get memoryUsage() { return memoryUsage.value },
    get shouldReduceMotion() { return shouldReduceMotion.value },
    get isLowPerformance() { return isLowPerformance.value }
  }

  return {
    fps,
    memoryUsage,
    shouldReduceMotion,
    isLowPerformance,
    stats
  }
}
```

**Step 2: 创建性能降级 Hook**

```typescript
// frontend/src/display/composables/usePerformanceDegradation.ts
import { computed, type Ref } from 'vue'

export interface DegradationConfig {
  /** 禁用粒子效果 */
  disableParticles: boolean
  /** 禁用发光边框 */
  disableGlow: boolean
  /** 禁用模糊背景 */
  disableBlur: boolean
  /** 禁用动画 */
  disableAnimations: boolean
  /** 降低图表细节 */
  reduceChartDetail: boolean
}

/**
 * 性能降级 Hook
 * 根据性能指标自动调整视觉效果
 */
export function usePerformanceDegradation(
  isLowPerformance: Ref<boolean>,
  shouldReduceMotion: Ref<boolean>
) {
  const config = computed<DegradationConfig>(() => {
    if (!isLowPerformance.value && !shouldReduceMotion.value) {
      return {
        disableParticles: false,
        disableGlow: false,
        disableBlur: false,
        disableAnimations: false,
        reduceChartDetail: false
      }
    }

    // 低性能模式
    return {
      disableParticles: true,
      disableGlow: true,
      disableBlur: shouldReduceMotion.value,
      disableAnimations: shouldReduceMotion.value,
      reduceChartDetail: isLowPerformance.value
    }
  })

  /**
   * 获取降级后的样式
   */
  function getDegradedStyle(baseStyle: Record<string, string>) {
    if (!shouldReduceMotion.value) return baseStyle

    const result = { ...baseStyle }

    if (config.value.disableGlow) {
      delete result.boxShadow
      delete result.textShadow
    }

    if (config.value.disableBlur) {
      delete result.backdropFilter
    }

    return result
  }

  /**
   * 获取降级后的 ECharts 配置
   */
  function getDegradedChartOption(baseOption: Record<string, unknown>) {
    if (!config.value.reduceChartDetail) return baseOption

    return {
      ...baseOption,
      animation: !config.value.disableAnimations,
      // 减少图表细节
      series: (baseOption.series as any[])?.map(s => ({
        ...s,
        animation: !config.value.disableAnimations,
        // 对于折线图，禁用平滑以提升性能
        smooth: s.type === 'line' ? false : undefined
      }))
    }
  }

  return {
    config,
    getDegradedStyle,
    getDegradedChartOption
  }
}
```

**Step 3: 提交更改**

```bash
git add frontend/src/display/composables/usePerformanceMonitor.ts
git add frontend/src/display/composables/usePerformanceDegradation.ts
git commit -m "feat(display): add performance monitoring and degradation

- Add FPS and memory monitoring
- Auto-enable degradation when FPS < 30
- Support disabling particles, glow, blur, animations
- Provide chart option degradation helper

Refs: Dashboard Optimization Plan Task 8"
```

**Step 4: 同步进度到 NotebookLM**

---

## Phase 4: P3 低优先级任务（可选）

> 这些任务为可选实现，根据实际需求和时间安排决定是否执行。

### Task 9: WebSocket 实时推送
### Task 10: WebGL 渲染器支持
### Task 11: 视频墙多屏支持

> 详细实现方案已在优化分析报告中提供，此处略过具体步骤。

---

## 执行进度跟踪

| 任务 | 状态 | 完成时间 | NotebookLM 同步 |
|------|------|----------|-----------------|
| Task 1 | ✅ 已完成 | 2026-03-06 | ✅ |
| Task 2 | ✅ 已完成 | 2026-03-06 | ✅ |
| Task 3 | ✅ 已完成 | 2026-03-06 | ✅ |
| Task 4 | ✅ 已完成 | 2026-03-06 | ✅ |
| Task 5 | ✅ 已完成 | 2026-03-06 | ✅ |
| Task 6 | ✅ 已完成 | 2026-03-06 | ✅ |
| Task 7 | ✅ 已完成 | 2026-03-06 | ✅ |
| Task 8 | ✅ 已完成 | 2026-03-06 | ✅ |

---

## 约束和规范

1. **每个任务完成后必须:**
   - 运行相关测试确保通过
   - 提交 Git 更改
   - 同步进度到 NotebookLM

2. **代码规范:**
   - 使用 TypeScript
   - 遵循 Vue 3 Composition API 最佳实践
   - 添加必要的类型注释

3. **测试要求:**
   - 为新的 composables 编写单元测试
   - 测试覆盖率目标: 80%
