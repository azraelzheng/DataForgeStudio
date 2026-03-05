# 大屏管理模块重构实现计划

> **Dashboard Module Refactor Implementation Plan**
>
> 本文档用于指导大屏管理模块的分阶段迭代重构，确保 100% 对齐设计文档 `大屏设计器分析文档.md`

---

## 一、项目概述

### 1.1 目标
将当前大屏管理实现完全对齐 `docs/design/大屏设计器分析文档.md` 设计规范。

### 1.2 约束
- 单人开发
- 追求完美质量
- 分阶段迭代交付

### 1.3 总时间估算
约 10-14 个工作日

---

## 二、阶段划分

### 第一阶段：状态管理层 (2-3 天)

#### 2.1.1 目标
建立完整的状态管理架构，将分散在组件中的状态集中管理。

#### 2.1.2 交付物
```
frontend/src/
├── store/
│   └── dashboard.ts           # Pinia 状态管理
└── composables/
    ├── useDesigner.ts         # 设计器核心逻辑
    ├── useDragDrop.ts         # 拖拽逻辑
    ├── useSelection.ts        # 选择逻辑
    └── useHistory.ts          # 撤销/重做
```

#### 2.1.3 Pinia Store 设计

```typescript
// store/dashboard.ts
import { defineStore } from 'pinia'
import type { DashboardDetailDto, DashboardWidgetDto } from '@/api/types'

interface DashboardState {
  // 当前编辑的大屏
  currentDashboard: DashboardDetailDto | null

  // 选中的组件 ID
  selectedWidgetId: number | null

  // 历史记录（撤销/重做）
  history: DashboardWidgetDto[][]
  historyIndex: number

  // 是否已修改
  isDirty: boolean

  // 画布缩放比例
  scale: number

  // 是否显示网格
  showGrid: boolean

  // 剪贴板
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
    // 大屏管理
    async loadDashboard(id: number) { /* ... */ },
    async saveDashboard() { /* ... */ },
    setDirty(dirty: boolean) { this.isDirty = dirty },

    // 组件管理
    addWidget(widget: DashboardWidgetDto) { /* ... */ },
    updateWidget(id: number, updates: Partial<DashboardWidgetDto>) { /* ... */ },
    deleteWidget(id: number) { /* ... */ },
    selectWidget(id: number | null) { this.selectedWidgetId = id },

    // 位置管理
    updateWidgetPositions(positions: Array<{widgetId: number, x: number, y: number, w: number, h: number}>) { /* ... */ },

    // 历史管理
    saveHistory() { /* 保存当前状态到历史 */ },
    undo() { /* 撤销 */ },
    redo() { /* 重做 */ },

    // 剪贴板
    copyWidget() { /* 复制选中组件到剪贴板 */ },
    pasteWidget() { /* 粘贴剪贴板内容 */ },

    // 视图控制
    setScale(scale: number) { this.scale = scale },
    toggleGrid() { this.showGrid = !this.showGrid },

    // 重置
    reset() { /* 重置所有状态 */ }
  }
})
```

#### 2.1.4 useHistory Composable

```typescript
// composables/useHistory.ts
import { useDashboardStore } from '@/store/dashboard'

export function useHistory() {
  const store = useDashboardStore()

  const MAX_HISTORY = 50

  const pushHistory = () => {
    if (!store.currentDashboard) return

    // 截断后面的历史
    store.history = store.history.slice(0, store.historyIndex + 1)

    // 添加当前状态
    const snapshot = JSON.parse(JSON.stringify(store.currentDashboard.widgets))
    store.history.push(snapshot)

    // 限制历史记录数量
    if (store.history.length > MAX_HISTORY) {
      store.history.shift()
    }

    store.historyIndex = store.history.length - 1
    store.isDirty = true
  }

  const undo = () => {
    if (!store.canUndo) return

    store.historyIndex--
    const snapshot = store.history[store.historyIndex]
    if (store.currentDashboard) {
      store.currentDashboard.widgets = JSON.parse(JSON.stringify(snapshot))
    }
  }

  const redo = () => {
    if (!store.canRedo) return

    store.historyIndex++
    const snapshot = store.history[store.historyIndex]
    if (store.currentDashboard) {
      store.currentDashboard.widgets = JSON.parse(JSON.stringify(snapshot))
    }
  }

  return { pushHistory, undo, redo }
}
```

#### 2.1.5 验收标准
- [ ] Pinia store 创建完成，包含所有状态
- [ ] useHistory 实现撤销/重做功能
- [ ] DashboardDesigner 迁移到使用 Pinia store
- [ ] 现有功能不受影响

---

### 第二阶段：设计器增强 (2-3 天)

#### 2.2.1 目标
完善设计器交互体验，添加快捷键和控制按钮。

#### 2.2.2 交付物
- 键盘快捷键系统
- 缩放控制 UI
- 网格切换 UI
- 右键菜单

#### 2.2.3 键盘快捷键设计

```typescript
// composables/useKeyboard.ts
import { onMounted, onUnmounted } from 'vue'
import { useDashboardStore } from '@/store/dashboard'

export function useKeyboard() {
  const store = useDashboardStore()

  const handleKeyDown = (e: KeyboardEvent) => {
    // 忽略输入框内的快捷键
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
  const activeElement = document.activeElement
  return activeElement?.tagName === 'INPUT' ||
         activeElement?.tagName === 'TEXTAREA' ||
         activeElement?.isContentEditable
}
```

#### 2.2.4 缩放控制设计

```vue
<!-- components/designer/ZoomControl.vue -->
<template>
  <div class="zoom-control">
    <el-button-group>
      <el-button :icon="ZoomOut" @click="zoomOut" :disabled="scale <= 0.25" />
      <el-dropdown trigger="click">
        <span class="zoom-text">{{ scalePercent }}%</span>
                <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item v-for="z in zoomLevels" :key="z" @click="setScale(z)">
              {{ z * 100 }}%
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
      <el-button :icon="ZoomIn" @click="zoomIn" :disabled="scale >= 2" />
      <el-button :icon="Aim" @click="fitToScreen" title="适应屏幕" />
    </el-button-group>
  </div>
</template>

<script setup>
const store = useDashboardStore()
const scale = computed(() => store.scale)
const scalePercent = computed(() => Math.round(scale.value * 100))
const zoomLevels = [0.25, 0.5, 0.75, 1, 1.25, 1.5, 2]

const zoomIn = () => store.setScale(Math.min(2, scale.value + 0.25))
const zoomOut = () => store.setScale(Math.max(0.25, scale.value - 0.25))
const setScale = (s: number) => store.setScale(s)
const fitToScreen = () => { /* 计算适应屏幕的缩放比例 */ }
</script>
```

#### 2.2.5 验收标准
- [ ] Delete 键删除选中组件
- [ ] Ctrl+C/V 复制粘贴组件
- [ ] Ctrl+Z/Y 撤销重做
- [ ] 缩放控制正常工作 (25%~200%)
- [ ] 网格显示/隐藏切换

---

### 第三阶段：组件系统完善 (3-4 天)

#### 2.3.1 目标
完善所有组件类型和配置面板，设计器中实时预览图表。

#### 2.3.2 交付物
```
frontend/src/dashboard/widgets/
├── ChartWidget.vue          # ECharts 图表 (设计器预览)
├── TableWidget.vue          # 表格
├── NumberCardWidget.vue     # 数字卡片
├── TextWidget.vue           # 文本组件 (新增)
├── ImageWidget.vue          # 图片组件 (新增)
└── config/
    ├── ChartConfigPanel.vue    # 图表配置
    ├── TableConfig.vue         # 表格配置
    └── NumberCardConfig.vue    # 数字卡片配置
```

#### 2.3.3 图表组件设计器预览

```vue
<!-- widgets/ChartWidget.vue -->
<template>
  <div ref="chartRef" class="chart-widget">
    <div v-if="loading" class="loading-overlay">
      <el-icon class="is-loading"><Loading /></el-icon>
    </div>
    <div v-else-if="error" class="error-overlay">
      <el-icon><Warning /></el-icon>
      <span>{{ error }}</span>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, watch, computed } from 'vue'
import * as echarts from 'echarts'

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

  return generateEChartsOption(styleConfig, dataConfig, props.data)
})

onMounted(() => {
  if (chartRef.value) {
    chartInstance = echarts.init(chartRef.value, 'dark')
    chartInstance.setOption(chartOption.value)
  }
})

watch([() => props.data, chartOption], () => {
  chartInstance?.setOption(chartOption.value, true)
}, { deep: true })

onUnmounted(() => {
  chartInstance?.dispose()
})
</script>
```

#### 2.3.4 配置面板组件

```vue
<!-- widgets/config/ChartConfigPanel.vue -->
<template>
  <div class="chart-config-panel">
    <el-form label-width="80px" size="small">
      <!-- 图表类型 -->
      <el-form-item label="图表类型">
        <el-select v-model="config.chartType">
          <el-option label="折线图" value="line" />
          <el-option label="柱状图" value="bar" />
          <el-option label="饼图" value="pie" />
          <el-option label="仪表盘" value="gauge" />
          <el-option label="雷达图" value="radar" />
        </el-select>
      </el-form-item>

      <!-- 数据映射 -->
      <el-divider>数据映射</el-divider>
      <el-form-item label="X轴字段">
        <el-select v-model="config.xField">
          <el-option v-for="f in fields" :key="f" :value="f" />
        </el-select>
      </el-form-item>
      <el-form-item label="Y轴字段">
        <el-select v-model="config.yField">
          <el-option v-for="f in fields" :key="f" :value="f" />
        </el-select>
      </el-form-item>
      <el-form-item label="系列字段">
        <el-select v-model="config.seriesField" clearable>
          <el-option v-for="f in fields" :key="f" :value="f" />
        </el-select>
      </el-form-item>

      <!-- 样式配置 -->
      <el-divider>样式配置</el-divider>
      <!-- 更多样式选项... -->
    </el-form>
  </div>
</template>
```

#### 2.3.5 验收标准
- [ ] 图表组件在设计器中显示 ECharts 预览
- [ ] 配置面板可以配置数据字段映射
- [ ] 文本组件可用（支持字体、大小、颜色）
- [ ] 图片组件可用（支持 URL 上传）

---

### 第四阶段：公开访问修复 (1-2 天)

#### 2.4.1 目标
修复公开访问功能，使用 GUID 而非数字 ID。

#### 2.4.2 问题分析
当前问题：
- `PublicDashboard.vue` 使用数字 dashboardId 作为路由参数
- 设计文档要求使用 GUID 格式的 publicUrl

#### 2.4.3 修复方案

**路由修改**:
```javascript
// router/index.js
{
  path: '/public/d/:publicUrl',
  name: 'PublicDashboard',
  component: () => import('../views/dashboard/PublicDashboard.vue'),
  meta: { title: '大屏', requiresAuth: false }
}
```

**PublicDashboard.vue 修改**:
```vue
<script setup>
const route = useRoute()
const publicUrl = route.params.publicUrl as string

// 使用 publicUrl 查询
const loadDashboard = async () => {
  try {
    const res = await request.get(`/api/public/dashboards/${publicUrl}`)
    // ...
  } catch {
    // 处理错误
  }
}
</script>
```

**后端 API 修改**:
```csharp
// PublicController.cs
[HttpGet("dashboards/{publicUrl}")]
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
        .FirstOrDefaultAsync(d => d.PublicUrl == publicUrl && d.IsPublic);

    if (dashboard == null)
    {
        return ApiResponse<DashboardDetailDto>.Fail("大屏不存在或未公开", "NOT_FOUND");
    }

    return await GetDashboardByIdAsync(dashboard.DashboardId);
}
```

#### 2.4.4 验收标准
- [ ] 公开链接格式为 `/public/d/{guid}`
- [ ] 无登录可访问公开大屏
- [ ] 非公开大屏返回 404

---

## 三、技术栈确认

| 层级 | 技术 | 版本 |
|------|------|------|
| 状态管理 | Pinia | 2.x |
| 图表库 | Apache ECharts | 5.x |
| UI 组件 | Element Plus | 2.x |
| 构建工具 | Vite | 5.x |
| 前端框架 | Vue 3 | 3.x |

---

## 四、风险与缓解措施

| 风险 | 缓解措施 |
|------|----------|
| 状态迁移导致功能回退 | 每个阶段完成后进行完整功能测试 |
| 撤销/重做性能问题 | 限制历史记录数量（最多 50 条） |
| ECharts 在设计器中性能问题 | 设计器使用简化配置，预览模式使用完整配置 |
| 公开访问 API 兼容性 | 保留数字 ID 访问方式作为备选 |

---

## 五、测试计划

### 单元测试
- Pinia store actions 测试
- useHistory composable 测试
- useKeyboard composable 测试

### 集成测试
- 设计器完整工作流测试
- 公开访问流程测试

### 手动测试清单
- [ ] 创建新大屏
- [ ] 添加/编辑/删除组件
- [ ] 撤销/重做操作
- [ ] 复制/粘贴组件
- [ ] 缩放画布
- [ ] 发布大屏
- [ ] 公开访问
- [ ] 数据刷新

---

## 六、文档更新

完成后需更新以下文档：
- [ ] `docs/PROJECT_STATUS.md` - 更新模块状态
- [ ] API 文档 - 添加新的公开访问 API
- [ ] 用户手册 - 添加键盘快捷键说明

---

**创建时间**: 2026-03-05
**创建者**: Claude Code
