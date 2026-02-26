# 报表查询页面布局优化实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 优化报表查询页面布局，减少留白，增加查询结果显示区域，实现查询条件智能折叠

**Architecture:** 通过 CSS 变量调整侧边栏和条件区域样式，使用 Vue 响应式状态控制条件折叠

**Tech Stack:** Vue 3, Element Plus, CSS

---

## Task 1: 优化侧边栏 CSS 样式

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (CSS section)

**Step 1: 修改 CSS 变量和侧边栏宽度**

找到 `.report-query` 中的 CSS 变量定义，修改侧边栏宽度：

```css
.report-query {
  height: 100%;
  --sidebar-width: 220px;  /* 从 280px 改为 220px */
  --sidebar-collapsed-width: 48px;
  /* ... 其他变量保持不变 */
}
```

**Step 2: 优化侧边栏搜索框样式**

找到 `.sidebar-search` 样式，修改：

```css
.sidebar-search {
  padding: 8px;  /* 从 12px 改为 8px */
  border-bottom: 1px solid var(--border-light);
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 48px;  /* 从 56px 改为 48px */
}
```

**Step 3: 优化列表区域和报表项样式**

找到 `.sidebar-list` 和 `.report-item` 样式，修改：

```css
.sidebar-list {
  flex: 1;
  overflow-y: auto;
  padding: 4px;  /* 从 8px 改为 4px */
}

.report-list {
  display: flex;
  flex-direction: column;
  gap: 2px;  /* 从 4px 改为 2px */
}

.report-item {
  display: flex;
  align-items: center;
  padding: 8px;  /* 从 12px 改为 8px */
  border-radius: 6px;  /* 从 8px 改为 6px */
  cursor: pointer;
  margin-bottom: 0;  /* 移除 margin-bottom */
  border-left: 3px solid transparent;
}
```

**Step 4: 验证前端编译**

检查 Vite HMR 输出，确保无编译错误。

**Step 5: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "style: optimize sidebar layout for more compact display"
```

---

## Task 2: 优化查询条件区域样式

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (CSS section)

**Step 1: 修改条件折叠面板样式**

找到 `.conditions-collapse` 相关样式，修改：

```css
.conditions-collapse {
  border: none;
  background: transparent;  /* 从 var(--bg-card) 改为透明 */
  border-radius: 0;  /* 从 8px 改为 0 */
  box-shadow: none;  /* 移除阴影 */
  margin-bottom: 12px;  /* 从 16px 改为 12px */
  border-bottom: 1px solid var(--border-light);  /* 添加底部分隔线 */
}

.conditions-collapse :deep(.el-collapse-item__header) {
  border-bottom: none;
  height: 36px;  /* 从 44px 改为 36px */
  font-size: 14px;
  background: transparent;
}

.conditions-collapse :deep(.el-collapse-item__wrap) {
  border-bottom: none;
  background: transparent;
}

.conditions-collapse :deep(.el-collapse-item__content) {
  padding: 0;
}
```

**Step 2: 修改条件内容区域样式**

找到 `.conditions-content` 样式，修改：

```css
.conditions-content {
  padding: 10px 0;  /* 从 16px 改为 10px，左右改为 0 */
}

.conditions-actions {
  margin-top: 12px;  /* 从 16px 改为 12px */
  padding-top: 12px;  /* 从 16px 改为 12px */
  border-top: 1px solid var(--border-light);
  display: flex;
  gap: 12px;
}
```

**Step 3: 验证前端编译**

检查 Vite HMR 输出。

**Step 4: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "style: flatten conditions area and reduce padding"
```

---

## Task 3: 实现查询条件智能折叠功能

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (template and script)

**Step 1: 添加折叠状态变量**

在 script setup 中添加新的状态变量：

```javascript
// 查询条件折叠状态
const conditionsCollapsed = ref(false)  // 是否折叠到首行

// 计算第一行能显示的条件数量（每行2个条件，el-col :span="12"）
const firstRowConditionsCount = computed(() => {
  // 每个 el-col :span="12"，所以一行显示 2 个条件
  return 2
})

// 首行条件（折叠时显示）
const firstRowConditions = computed(() => {
  return queryConditions.value.slice(0, firstRowConditionsCount.value)
})

// 剩余条件（折叠时隐藏）
const remainingConditions = computed(() => {
  return queryConditions.value.slice(firstRowConditionsCount.value)
})

// 是否有超过一行的条件
const hasMoreConditions = computed(() => {
  return queryConditions.value.length > firstRowConditionsCount.value
})
```

**Step 2: 修改模板，支持条件折叠显示**

修改查询条件区域的模板：

```html
<!-- 查询条件 (可折叠) -->
<div v-if="queryConditions.length > 0" class="conditions-panel">
  <!-- 标题栏 -->
  <div class="conditions-header" @click="toggleConditionsCollapse">
    <span class="collapse-title">
      <el-icon><Filter /></el-icon>
      查询条件
    </span>
    <el-icon v-if="hasMoreConditions" class="collapse-icon" :class="{ rotated: conditionsCollapsed }">
      <ArrowDown />
    </el-icon>
  </div>

  <!-- 条件内容 -->
  <div class="conditions-content" v-show="!conditionsCollapsed || !hasQueried">
    <el-form :inline="true" :model="conditionForm" label-width="100px">
      <el-row :gutter="16">
        <!-- 所有条件 -->
        <el-col :span="12" v-for="qc in queryConditions" :key="qc.fieldName + qc.operator">
          <!-- 条件输入控件保持不变 -->
        </el-col>
      </el-row>
    </el-form>
    <div class="conditions-actions">
      <el-button type="primary" @click="handleQuery" :loading="querying">
        <el-icon><Search /></el-icon>
        查询
      </el-button>
      <el-button @click="resetConditions">重置条件</el-button>
    </div>
  </div>

  <!-- 折叠时显示的条件摘要 -->
  <div v-if="conditionsCollapsed && hasQueried && queryConditions.length > 0" class="conditions-summary">
    <div class="summary-row">
      <el-form :inline="true" :model="conditionForm" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="12" v-for="qc in firstRowConditions" :key="qc.fieldName + qc.operator">
            <!-- 首行条件输入控件 -->
          </el-col>
        </el-row>
      </el-form>
      <el-button link type="primary" @click="conditionsCollapsed = false" v-if="hasMoreConditions">
        展开全部
      </el-button>
    </div>
  </div>
</div>
```

**Step 3: 添加折叠切换函数**

```javascript
// 切换条件折叠状态
const toggleConditionsCollapse = () => {
  if (hasMoreConditions.value) {
    conditionsCollapsed.value = !conditionsCollapsed.value
  }
}
```

**Step 4: 修改 handleQuery 函数，查询后自动折叠**

在 `handleQuery` 函数成功后添加折叠逻辑：

```javascript
const handleQuery = async () => {
  hasQueried.value = true
  querying.value = true
  try {
    const params = buildQueryParams()
    const res = await reportApi.executeReport(selectedReport.value.reportId, { parameters: params })
    if (res.success) {
      reportData.value = res.data
      resetColumnFilters()
      // 查询成功后折叠条件
      if (hasMoreConditions.value) {
        conditionsCollapsed.value = true
      }
      updateTableHeight()
    } else {
      ElMessage.error(res.message || '查询失败')
    }
  } catch (error) {
    console.error('查询失败:', error)
    ElMessage.error('查询失败：网络错误')
  } finally {
    querying.value = false
  }
}
```

**Step 5: 添加 ArrowDown 图标导入**

在图标导入中添加 ArrowDown：

```javascript
import { Search, Document, Download, ArrowLeft, ArrowRight, Filter, ArrowDown } from '@element-plus/icons-vue'
```

**Step 6: 添加折叠相关 CSS 样式**

```css
/* 条件面板样式 */
.conditions-panel {
  background: transparent;
  border-bottom: 1px solid var(--border-light);
  margin-bottom: 12px;
}

.conditions-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 0;
  cursor: pointer;
  user-select: none;
}

.conditions-header:hover {
  background-color: rgba(0, 0, 0, 0.02);
}

.collapse-icon {
  transition: transform 0.3s ease;
  color: #909399;
}

.collapse-icon.rotated {
  transform: rotate(180deg);
}

.conditions-summary {
  padding: 8px 0;
}

.summary-row {
  display: flex;
  align-items: center;
  gap: 16px;
}

.summary-row .el-form {
  flex: 1;
}
```

**Step 7: 验证前端编译**

检查 Vite HMR 输出。

**Step 8: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: implement smart collapse for query conditions"
```

---

## Task 4: 测试与验证

**Step 1: 测试侧边栏优化**

- 侧边栏宽度是否变窄
- 报表列表是否更紧凑
- 选中报表是否正常显示

**Step 2: 测试条件区域优化**

- 条件区域是否扁平化
- 内边距是否减少

**Step 3: 测试智能折叠功能**

- 初始状态是否展开所有条件
- 点击查询后是否自动折叠
- 点击标题栏是否能展开/收起
- 只有一行条件时不显示折叠图标

**Step 4: 测试结果区域自适应**

- 条件折叠时结果区域是否变大
- 条件展开时结果区域是否变小

**Step 5: 最终 Commit（如有修复）**

```bash
git add -A
git commit -m "fix: layout optimization adjustments"
```

---

## 完成检查清单

- [ ] 侧边栏宽度 280px → 220px
- [ ] 报表项内边距 12px → 8px
- [ ] 搜索框高度 56px → 48px
- [ ] 条件区域扁平化
- [ ] 条件区域内边距减少
- [ ] 智能折叠功能实现
- [ ] 查询后自动折叠
- [ ] 结果区域自适应
- [ ] 测试通过
