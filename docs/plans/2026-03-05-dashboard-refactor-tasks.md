# 大屏管理模块重构 - 任务分解

> **Dashboard Module Refactor - Task Breakdown**
>
> 本文档是 `2026-03-05-dashboard-refactor-design.md` 的详细任务分解，供执行参考。

---

## 任务概览

| 阶段 | 任务数 | 预计时间 |
|------|--------|---------|
| Phase 1: 状态管理层 | 5 | 2-3 天 |
| Phase 2: 设计器增强 | 4 | 2-3 天 |
| Phase 3: 组件系统 | 6 | 3-4 天 |
| Phase 4: 公开访问 | 3 | 1-2 天 |
| **总计** | **18** | **8-12 天** |

---

## Phase 1: 状态管理层

### Task 1.1: 创建 Pinia Store
**预计时间**: 2-3 小时

**文件**: `frontend/src/store/dashboard.ts`

**实现步骤**:
1. 创建 `frontend/src/store/` 目录（如不存在）
2. 创建 `dashboard.ts` 文件
3. 定义 `DashboardState` 接口
4. 实现 `useDashboardStore` defineStore
5. 实现所有 getters 和 actions

**代码骨架**:
```typescript
import { defineStore } from 'pinia'
import type { DashboardDetailDto, DashboardWidgetDto } from '@/api/types'
import request from '@/api/request'

interface DashboardState {
  currentDashboard: DashboardDetailDto | null
  selectedWidgetId: number | null
  history: DashboardWidgetDto[][]
  historyIndex: number
  isDirty: boolean
  scale: number
  showGrid: boolean
  clipboard: DashboardWidgetDto | null
}

export const useDashboardStore = defineStore('dashboard', {
  state: (): DashboardState => ({
    currentDashboard: null,
    selectedWidgetId: null,
    history: [],
    historyIndex: -1,
    isDirty: false,
    scale: 1,
    showGrid: true,
    clipboard: null
  }),

  getters: {
    widgets: (state) => state.currentDashboard?.widgets ?? [],
    selectedWidget: (state) => {
      if (!state.currentDashboard || !state.selectedWidgetId) return null
      return state.currentDashboard.widgets.find(w => w.widgetId === state.selectedWidgetId)
    },
    canUndo: (state) => state.historyIndex > 0,
    canRedo: (state) => state.historyIndex < state.history.length - 1
  },

  actions: {
    async loadDashboard(id: number) {
      const res = await request.get(`/api/dashboards/${id}`)
      if (res.success) {
        this.currentDashboard = res.data
        this.saveHistory()
      }
      return res
    },

    async saveDashboard() {
      if (!this.currentDashboard) return
      const res = await request.put(`/api/dashboards/${this.currentDashboard.dashboardId}`, {
        name: this.currentDashboard.name,
        description: this.currentDashboard.description,
        layoutConfig: this.currentDashboard.layoutConfig,
        themeConfig: this.currentDashboard.themeConfig
      })
      if (res.success) {
        this.isDirty = false
      }
      return res
    },

    setDirty(dirty: boolean) {
      this.isDirty = dirty
    },

    addWidget(widget: DashboardWidgetDto) {
      if (!this.currentDashboard) return
      this.currentDashboard.widgets.push(widget)
      this.saveHistory()
    },

    updateWidget(id: number, updates: Partial<DashboardWidgetDto>) {
      if (!this.currentDashboard) return
      const index = this.currentDashboard.widgets.findIndex(w => w.widgetId === id)
      if (index !== -1) {
        Object.assign(this.currentDashboard.widgets[index], updates)
        this.saveHistory()
      }
    },

    deleteWidget(id: number) {
      if (!this.currentDashboard) return
      const index = this.currentDashboard.widgets.findIndex(w => w.widgetId === id)
      if (index !== -1) {
        this.currentDashboard.widgets.splice(index, 1)
        if (this.selectedWidgetId === id) {
          this.selectedWidgetId = null
        }
        this.saveHistory()
      }
    },

    selectWidget(id: number | null) {
      this.selectedWidgetId = id
    },

    updateWidgetPositions(positions: Array<{widgetId: number, x: number, y: number, w: number, h: number}>) {
      if (!this.currentDashboard) return
      positions.forEach(pos => {
        const widget = this.currentDashboard!.widgets.find(w => w.widgetId === pos.widgetId)
        if (widget) {
          widget.positionX = pos.x
          widget.positionY = pos.y
          widget.width = pos.w
          widget.height = pos.h
        }
      })
      this.saveHistory()
    },

    saveHistory() {
      if (!this.currentDashboard) return
      // Truncate future history
      this.history = this.history.slice(0, this.historyIndex + 1)
      // Add current snapshot
      this.history.push(JSON.parse(JSON.stringify(this.currentDashboard.widgets)))
      // Limit history size
      if (this.history.length > 50) {
        this.history.shift()
      }
      this.historyIndex = this.history.length - 1
      this.isDirty = true
    },

    undo() {
      if (!this.canUndo) return
      this.historyIndex--
      const snapshot = this.history[this.historyIndex]
      if (this.currentDashboard) {
        this.currentDashboard.widgets = JSON.parse(JSON.stringify(snapshot))
      }
    },

    redo() {
      if (!this.canRedo) return
      this.historyIndex++
      const snapshot = this.history[this.historyIndex]
      if (this.currentDashboard) {
        this.currentDashboard.widgets = JSON.parse(JSON.stringify(snapshot))
      }
    },

    copyWidget() {
      if (!this.selectedWidget) return
      this.clipboard = JSON.parse(JSON.stringify(this.selectedWidget))
    },

    pasteWidget() {
      if (!this.clipboard || !this.currentDashboard) return
      const newWidget = {
        ...JSON.parse(JSON.stringify(this.clipboard)),
        widgetId: Date.now(), // Temporary ID
        positionX: this.clipboard.positionX + 20,
        positionY: this.clipboard.positionY + 20
      }
      this.currentDashboard.widgets.push(newWidget)
      this.selectWidget(newWidget.widgetId)
      this.saveHistory()
    },

    setScale(scale: number) {
      this.scale = Math.max(0.25, Math.min(2, scale))
    },

    toggleGrid() {
      this.showGrid = !this.showGrid
    },

    reset() {
      this.currentDashboard = null
      this.selectedWidgetId = null
      this.history = []
      this.historyIndex = -1
      this.isDirty = false
      this.scale = 1
      this.showGrid = true
      this.clipboard = null
    }
  }
})
```

**验证方法**:
- [ ] Store 文件创建成功
- [ ] TypeScript 编译无错误
- [ ] 所有 actions 可调用

---

### Task 1.2: 创建 useHistory Composable
**预计时间**: 1 小时

**文件**: `frontend/src/composables/useHistory.ts`

**实现步骤**:
1. 创建 `frontend/src/composables/` 目录
2. 创建 `useHistory.ts` 文件
3. 封装历史记录操作逻辑

**代码**:
```typescript
import { useDashboardStore } from '@/store/dashboard'

export function useHistory() {
  const store = useDashboardStore()

  const pushHistory = () => {
    store.saveHistory()
  }

  const undo = () => {
    store.undo()
  }

  const redo = () => {
    store.redo()
  }

  const clearHistory = () => {
    store.history = []
    store.historyIndex = -1
  }

  return {
    pushHistory,
    undo,
    redo,
    clearHistory,
    canUndo: computed(() => store.canUndo),
    canRedo: computed(() => store.canRedo)
  }
}
```

**验证方法**:
- [ ] Composable 导出正常
- [ ] undo/redo 调用 store 方法

---

### Task 1.3: 创建 useSelection Composable
**预计时间**: 30 分钟

**文件**: `frontend/src/composables/useSelection.ts`

**代码**:
```typescript
import { computed } from 'vue'
import { useDashboardStore } from '@/store/dashboard'

export function useSelection() {
  const store = useDashboardStore()

  const selectedWidget = computed(() => store.selectedWidget)
  const selectedWidgetId = computed(() => store.selectedWidgetId)

  const selectWidget = (id: number | null) => {
    store.selectWidget(id)
  }

  const clearSelection = () => {
    store.selectWidget(null)
  }

  const isSelected = (id: number) => {
    return store.selectedWidgetId === id
  }

  return {
    selectedWidget,
    selectedWidgetId,
    selectWidget,
    clearSelection,
    isSelected
  }
}
```

---

### Task 1.4: 创建 useDragDrop Composable
**预计时间**: 1 小时

**文件**: `frontend/src/composables/useDragDrop.ts`

**代码**:
```typescript
import { ref } from 'vue'
import { useDashboardStore } from '@/store/dashboard'

export function useDragDrop() {
  const store = useDashboardStore()
  const isDragging = ref(false)
  const dragType = ref<string | null>(null)

  const handleDragStart = (type: string) => {
    isDragging.value = true
    dragType.value = type
  }

  const handleDragEnd = () => {
    isDragging.value = false
    dragType.value = null
  }

  const handleDrop = (event: DragEvent, canvasRect: DOMRect, scale: number) => {
    if (!dragType.value) return

    const x = Math.round((event.clientX - canvasRect.left) / scale)
    const y = Math.round((event.clientY - canvasRect.top) / scale)

    // Return position for parent to handle widget creation
    return { type: dragType.value, x, y }
  }

  return {
    isDragging,
    dragType,
    handleDragStart,
    handleDragEnd,
    handleDrop
  }
}
```

---

### Task 1.5: 迁移 DashboardDesigner 到 Pinia
**预计时间**: 3-4 小时

**文件**: `frontend/src/views/dashboard/DashboardDesigner.vue`

**实现步骤**:
1. 引入 `useDashboardStore`
2. 替换所有 `ref` 状态为 store 状态
3. 替换所有本地方法为 store actions
4. 保持现有功能不变

**关键修改点**:
```typescript
// Before
const widgets = ref<DashboardWidgetDto[]>([])
const selectedWidgetId = ref<number | null>(null)

// After
import { useDashboardStore } from '@/store/dashboard'
const store = useDashboardStore()
// Use store.widgets, store.selectedWidgetId, etc.
```

**验证方法**:
- [ ] 打开设计器无报错
- [ ] 加载大屏数据正常
- [ ] 选中组件功能正常
- [ ] 保存大屏功能正常

---

## Phase 2: 设计器增强

### Task 2.1: 创建键盘快捷键系统
**预计时间**: 2 小时

**文件**: `frontend/src/composables/useKeyboard.ts`

**代码**:
```typescript
import { onMounted, onUnmounted } from 'vue'
import { useDashboardStore } from '@/store/dashboard'

export function useKeyboard() {
  const store = useDashboardStore()

  const handleKeyDown = (e: KeyboardEvent) => {
    // Skip if input is focused
    if (isInputFocused()) return

    switch (e.key) {
      case 'Delete':
      case 'Backspace':
        if (store.selectedWidgetId) {
          e.preventDefault()
          store.deleteWidget(store.selectedWidgetId)
        }
        break

      case 'c':
      case 'C':
        if (e.ctrlKey || e.metaKey) {
          e.preventDefault()
          store.copyWidget()
        }
        break

      case 'v':
      case 'V':
        if (e.ctrlKey || e.metaKey) {
          e.preventDefault()
          store.pasteWidget()
        }
        break

      case 'z':
      case 'Z':
        if (e.ctrlKey || e.metaKey) {
          e.preventDefault()
          if (e.shiftKey) {
            store.redo()
          } else {
            store.undo()
          }
        }
        break

      case 'y':
      case 'Y':
        if (e.ctrlKey || e.metaKey) {
          e.preventDefault()
          store.redo()
        }
        break

      case 'Escape':
        store.selectWidget(null)
        break
    }
  }

  onMounted(() => {
    window.addEventListener('keydown', handleKeyDown)
  })

  onUnmounted(() => {
    window.removeEventListener('keydown', handleKeyDown)
  })
}

function isInputFocused(): boolean {
  const el = document.activeElement
  return el?.tagName === 'INPUT' || el?.tagName === 'TEXTAREA' || el?.isContentEditable
}
```

**在 DashboardDesigner.vue 中使用**:
```typescript
import { useKeyboard } from '@/composables/useKeyboard'
// In setup:
useKeyboard()
```

**验证方法**:
- [ ] Delete 删除选中组件
- [ ] Ctrl+C 复制组件
- [ ] Ctrl+V 粘贴组件
- [ ] Ctrl+Z 撤销
- [ ] Ctrl+Y 重做
- [ ] Escape 取消选择

---

### Task 2.2: 创建缩放控制组件
**预计时间**: 1.5 小时

**文件**: `frontend/src/components/designer/ZoomControl.vue`

**代码**:
```vue
<template>
  <div class="zoom-control">
    <el-button-group size="small">
      <el-button @click="zoomOut" :disabled="scale <= 0.25" title="缩小">
        <el-icon><ZoomOut /></el-icon>
      </el-button>
      <el-dropdown trigger="click" @command="handleScaleSelect">
        <el-button>
          {{ scalePercent }}%
        </el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item v-for="z in zoomLevels" :key="z" :command="z">
              {{ z * 100 }}%
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
      <el-button @click="zoomIn" :disabled="scale >= 2" title="放大">
        <el-icon><ZoomIn /></el-icon>
      </el-button>
      <el-button @click="fitToScreen" title="适应屏幕">
        <el-icon><FullScreen /></el-icon>
      </el-button>
    </el-button-group>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useDashboardStore } from '@/store/dashboard'
import { ZoomIn, ZoomOut, FullScreen } from '@element-plus/icons-vue'

const store = useDashboardStore()
const props = defineProps<{
  containerWidth: number
  containerHeight: number
  canvasWidth: number
  canvasHeight: number
}>()

const emit = defineEmits<{
  (e: 'fit', scale: number): void
}>()

const scale = computed(() => store.scale)
const scalePercent = computed(() => Math.round(scale.value * 100))
const zoomLevels = [0.25, 0.5, 0.75, 1, 1.25, 1.5, 2]

const zoomIn = () => store.setScale(Math.min(2, scale.value + 0.25))
const zoomOut = () => store.setScale(Math.max(0.25, scale.value - 0.25))
const handleScaleSelect = (s: number) => store.setScale(s)

const fitToScreen = () => {
  const scaleX = props.containerWidth / props.canvasWidth
  const scaleY = props.containerHeight / props.canvasHeight
  const fitScale = Math.min(scaleX, scaleY, 1)
  store.setScale(Math.round(fitScale * 100) / 100)
}
</script>

<style scoped>
.zoom-control {
  display: inline-flex;
  align-items: center;
}
</style>
```

---

### Task 2.3: 创建网格控制组件
**预计时间**: 30 分钟

**文件**: `frontend/src/components/designer/ToolbarControls.vue`

**代码**:
```vue
<template>
  <div class="toolbar-controls">
    <el-button-group size="small">
      <el-button
        :type="showGrid ? 'primary' : 'default'"
        @click="toggleGrid"
        title="显示/隐藏网格"
      >
        <el-icon><Grid /></el-icon>
      </el-button>
    </el-button-group>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useDashboardStore } from '@/store/dashboard'
import { Grid } from '@element-plus/icons-vue'

const store = useDashboardStore()
const showGrid = computed(() => store.showGrid)
const toggleGrid = () => store.toggleGrid()
</script>
```

---

### Task 2.4: 更新设计器工具栏
**预计时间**: 1 小时

**文件**: `frontend/src/views/dashboard/DashboardDesigner.vue`

**修改工具栏区域，添加新控制组件**:
```vue
<template>
  <div class="designer-toolbar">
    <div class="toolbar-left">
      <!-- 现有按钮... -->
    </div>
    <div class="toolbar-center">
      <ZoomControl
        :container-width="containerWidth"
        :container-height="containerHeight"
        :canvas-width="dashboardForm.width"
        :canvas-height="dashboardForm.height"
      />
      <ToolbarControls />
    </div>
    <div class="toolbar-right">
      <!-- 撤销/重做按钮 -->
      <el-button-group>
        <el-button
          :disabled="!store.canUndo"
          @click="store.undo"
          title="撤销 (Ctrl+Z)"
        >
          <el-icon><Back /></el-icon>
        </el-button>
        <el-button
          :disabled="!store.canRedo"
          @click="store.redo"
          title="重做 (Ctrl+Y)"
        >
          <el-icon><Right /></el-icon>
        </el-button>
      </el-button-group>
      <!-- 现有按钮... -->
    </div>
  </div>
</template>
```

**验证方法**:
- [ ] 缩放按钮工作正常
- [ ] 网格切换正常
- [ ] 撤销/重做按钮状态正确

---

## Phase 3: 组件系统完善

### Task 3.1: 创建 ECharts 图表组件
**预计时间**: 3 小时

**文件**: `frontend/src/dashboard/widgets/ChartWidget.vue`

**实现步骤**:
1. 安装 echarts 依赖（如未安装）
2. 创建图表组件
3. 支持多种图表类型
4. 响应式更新

**代码骨架**:
```vue
<template>
  <div ref="chartRef" class="chart-widget"></div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, computed, nextTick } from 'vue'
import * as echarts from 'echarts'
import type { DashboardWidgetDto } from '@/api/types'

const props = defineProps<{
  widget: DashboardWidgetDto
  data: any[]
  mode: 'design' | 'preview'
}>()

const chartRef = ref<HTMLElement>()
let chartInstance: echarts.ECharts | null = null

const chartOption = computed(() => {
  const styleConfig = JSON.parse(props.widget.styleConfig || '{}')
  const dataConfig = JSON.parse(props.widget.dataConfig || '{}')
  return generateOption(styleConfig, dataConfig, props.data)
})

function generateOption(style: any, dataConfig: any, data: any[]) {
  const chartType = style.chartType || 'bar'
  const xField = dataConfig.xField
  const yField = dataConfig.yField

  if (!xField || !yField || !data.length) {
    return getDefaultOption(chartType)
  }

  const xAxisData = data.map(d => d[xField])
  const seriesData = data.map(d => d[yField])

  return {
    backgroundColor: 'transparent',
    tooltip: { trigger: 'axis' },
    grid: { left: '10%', right: '10%', top: '10%', bottom: '10%' },
    xAxis: {
      type: 'category',
      data: xAxisData,
      axisLine: { lineStyle: { color: '#3a5a8c' } },
      axisLabel: { color: '#8ab4f8' }
    },
    yAxis: {
      type: 'value',
      axisLine: { lineStyle: { color: '#3a5a8c' } },
      axisLabel: { color: '#8ab4f8' },
      splitLine: { lineStyle: { color: '#1a3a5c' } }
    },
    series: [{
      type: chartType,
      data: seriesData,
      itemStyle: { color: '#409eff' }
    }]
  }
}

function getDefaultOption(type: string) {
  return {
    backgroundColor: 'transparent',
    title: {
      text: '请配置数据源',
      left: 'center',
      top: 'center',
      textStyle: { color: '#666', fontSize: 14 }
    }
  }
}

onMounted(async () => {
  await nextTick()
  if (chartRef.value) {
    chartInstance = echarts.init(chartRef.value)
    chartInstance.setOption(chartOption.value)
  }
})

watch(chartOption, (option) => {
  if (chartInstance) {
    chartInstance.setOption(option, true)
  }
}, { deep: true })

onUnmounted(() => {
  chartInstance?.dispose()
})

defineExpose({
  resize: () => chartInstance?.resize()
})
</script>

<style scoped>
.chart-widget {
  width: 100%;
  height: 100%;
}
</style>
```

---

### Task 3.2: 创建图表配置面板
**预计时间**: 2 小时

**文件**: `frontend/src/dashboard/widgets/config/ChartConfigPanel.vue`

**代码骨架**:
```vue
<template>
  <div class="chart-config-panel">
    <el-form label-width="80px" size="small">
      <el-form-item label="图表类型">
        <el-select v-model="localStyle.chartType" @change="emitChange">
          <el-option label="折线图" value="line" />
          <el-option label="柱状图" value="bar" />
          <el-option label="饼图" value="pie" />
          <el-option label="仪表盘" value="gauge" />
          <el-option label="雷达图" value="radar" />
        </el-select>
      </el-form-item>

      <el-divider>数据映射</el-divider>

      <el-form-item label="X轴字段">
        <el-select v-model="localDataConfig.xField" @change="emitChange">
          <el-option v-for="f in fields" :key="f" :value="f" />
        </el-select>
      </el-form-item>

      <el-form-item label="Y轴字段">
        <el-select v-model="localDataConfig.yField" @change="emitChange">
          <el-option v-for="f in fields" :key="f" :value="f" />
        </el-select>
      </el-form-item>

      <el-divider>样式配置</el-divider>

      <el-form-item label="显示图例">
        <el-switch v-model="localStyle.showLegend" @change="emitChange" />
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'

const props = defineProps<{
  styleConfig: string
  dataConfig: string
  fields: string[]
}>()

const emit = defineEmits<{
  (e: 'update:styleConfig', value: string): void
  (e: 'update:dataConfig', value: string): void
}>()

const localStyle = ref(JSON.parse(props.styleConfig || '{}'))
const localDataConfig = ref(JSON.parse(props.dataConfig || '{}'))

const emitChange = () => {
  emit('update:styleConfig', JSON.stringify(localStyle.value))
  emit('update:dataConfig', JSON.stringify(localDataConfig.value))
}

watch(() => props.styleConfig, (v) => {
  localStyle.value = JSON.parse(v || '{}')
})

watch(() => props.dataConfig, (v) => {
  localDataConfig.value = JSON.parse(v || '{}')
})
</script>
```

---

### Task 3.3: 创建数字卡片配置面板
**预计时间**: 1.5 小时

**文件**: `frontend/src/dashboard/widgets/config/NumberCardConfig.vue`

---

### Task 3.4: 创建表格配置面板
**预计时间**: 1.5 小时

**文件**: `frontend/src/dashboard/widgets/config/TableConfig.vue`

---

### Task 3.5: 创建文本组件
**预计时间**: 1.5 小时

**文件**: `frontend/src/dashboard/widgets/TextWidget.vue`

---

### Task 3.6: 创建图片组件
**预计时间**: 1 小时

**文件**: `frontend/src/dashboard/widgets/ImageWidget.vue`

---

## Phase 4: 公开访问修复

### Task 4.1: 修改前端路由
**预计时间**: 30 分钟

**文件**: `frontend/src/router/index.js`

**修改**:
```javascript
{
  path: '/public/d/:publicUrl',
  name: 'PublicDashboard',
  component: () => import('../views/dashboard/PublicDashboard.vue'),
  meta: { title: '大屏', requiresAuth: false }
}
```

---

### Task 4.2: 修改 PublicDashboard 组件
**预计时间**: 1 小时

**文件**: `frontend/src/views/dashboard/PublicDashboard.vue`

**修改**:
```typescript
// 使用 publicUrl 参数
const route = useRoute()
const publicUrl = route.params.publicUrl as string

// 调用新 API
const loadDashboard = async () => {
  const res = await request.get(`/api/public/dashboards/url/${publicUrl}`)
  // ...
}
```

---

### Task 4.3: 添加后端公开访问 API
**预计时间**: 1 小时

**文件**:
- `backend/src/DataForgeStudio.Api/Controllers/PublicController.cs`
- `backend/src/DataForgeStudio.Core/Services/DashboardService.cs`

**添加 API**:
```csharp
// PublicController.cs
[HttpGet("dashboards/url/{publicUrl}")]
[AllowAnonymous]
public async Task<ApiResponse<DashboardDetailDto>> GetPublicDashboardByUrl(string publicUrl)
{
    return await _dashboardService.GetPublicDashboardByUrlAsync(publicUrl);
}

// DashboardService.cs
public async Task<ApiResponse<DashboardDetailDto>> GetPublicDashboardByUrlAsync(string publicUrl)
{
    var dashboard = await _context.Dashboards
        .Include(d => d.Widgets)
            .ThenInclude(w => w.Report)
        .Include(d => d.Widgets)
            .ThenInclude(w => w.Rules)
        .FirstOrDefaultAsync(d => d.PublicUrl == publicUrl && d.IsPublic);

    if (dashboard == null)
    {
        return ApiResponse<DashboardDetailDto>.Fail("大屏不存在或未公开", "NOT_FOUND");
    }

    return await GetDashboardByIdAsync(dashboard.DashboardId);
}
```

---

## 验证清单

### Phase 1 完成验证
- [ ] `npm run dev` 无报错
- [ ] 打开设计器，加载现有大屏正常
- [ ] 所有现有功能正常工作

### Phase 2 完成验证
- [ ] Delete 键删除组件
- [ ] Ctrl+C/V 复制粘贴
- [ ] Ctrl+Z/Y 撤销重做
- [ ] 缩放控制工作正常
- [ ] 网格切换正常

### Phase 3 完成验证
- [ ] 图表在设计器中预览
- [ ] 配置面板修改生效
- [ ] 文本/图片组件可用

### Phase 4 完成验证
- [ ] 公开链接格式 `/public/d/{guid}`
- [ ] 无登录可访问
- [ ] 非公开返回 404

---

**创建时间**: 2026-03-05
**创建者**: Claude Code
