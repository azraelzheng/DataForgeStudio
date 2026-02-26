# 管理页面动态表格高度实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** 为 6 个管理页面实现动态表格滚动功能，让表格自动填满可用空间。

**Architecture:** 使用 Flex 布局让表格容器填满剩余空间，通过 `:height` 属性动态绑定表格高度，监听 resize 事件实现响应式。

**Tech Stack:** Vue 3 Composition API, Element Plus el-table, CSS Flexbox

---

## Task 1: UserManagement.vue

**Files:**
- Modify: `frontend/src/views/system/UserManagement.vue`

**Step 1: 修改模板 - 添加表格容器和高度绑定**

将表格部分改为：
```vue
<!-- 表格容器 -->
<div class="table-wrapper" ref="tableWrapper">
  <template v-if="tableData && tableData.length > 0">
    <el-table :data="tableData" :height="tableHeight" v-loading="loading" border stripe>
      <!-- 保持现有列不变 -->
    </el-table>
  </template>
  <el-empty v-else-if="!loading" description="暂无用户数据" />
</div>
```

**Step 2: 修改脚本 - 添加动态高度逻辑**

在 script setup 中添加：
```javascript
import { ref, onMounted, onUnmounted, nextTick } from 'vue'

const tableWrapper = ref(null)
const tableHeight = ref(null)

// 动态计算表格高度
const updateTableHeight = () => {
  nextTick(() => {
    if (tableWrapper.value) {
      tableHeight.value = tableWrapper.value.clientHeight
    }
  })
}

// 修改 onMounted
onMounted(() => {
  loadData()
  loadRoles()
  nextTick(updateTableHeight)
  window.addEventListener('resize', updateTableHeight)
})

// 添加 onUnmounted
onUnmounted(() => {
  window.removeEventListener('resize', updateTableHeight)
})
```

**Step 3: 修改样式 - 添加 Flex 布局**

```css
.user-management {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.flex-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.flex-card :deep(.el-card__header) {
  flex-shrink: 0;
}

.flex-card :deep(.el-card__body) {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.search-grid {
  flex-shrink: 0;
  /* 保持现有样式 */
}

.table-wrapper {
  flex: 1;
  overflow: hidden;
  min-height: 100px;
}

.el-pagination {
  flex-shrink: 0;
}
```

同时需要给 el-card 添加 class="flex-card"

**Step 4: 提交**

```bash
git add frontend/src/views/system/UserManagement.vue
git commit -m "feat: add dynamic table height to UserManagement"
```

---

## Task 2: RoleManagement.vue

**Files:**
- Modify: `frontend/src/views/system/RoleManagement.vue`

**Step 1: 修改模板**

与 Task 1 相同模式，添加 table-wrapper 和 :height 绑定。

**Step 2: 修改脚本**

添加 tableWrapper, tableHeight, updateTableHeight, resize 监听。

**Step 3: 修改样式**

添加 Flex 布局样式。

**Step 4: 提交**

```bash
git add frontend/src/views/system/RoleManagement.vue
git commit -m "feat: add dynamic table height to RoleManagement"
```

---

## Task 3: DataSourceManagement.vue

**Files:**
- Modify: `frontend/src/views/system/DataSourceManagement.vue`

**Step 1: 修改模板**

与 Task 1 相同模式。

**Step 2: 修改脚本**

添加动态高度逻辑。

**Step 3: 修改样式**

添加 Flex 布局样式。

**Step 4: 提交**

```bash
git add frontend/src/views/system/DataSourceManagement.vue
git commit -m "feat: add dynamic table height to DataSourceManagement"
```

---

## Task 4: LogManagement.vue

**Files:**
- Modify: `frontend/src/views/system/LogManagement.vue`

**Step 1: 修改模板**

与 Task 1 相同模式。

**Step 2: 修改脚本**

添加动态高度逻辑。

**Step 3: 修改样式**

添加 Flex 布局样式。

**Step 4: 提交**

```bash
git add frontend/src/views/system/LogManagement.vue
git commit -m "feat: add dynamic table height to LogManagement"
```

---

## Task 5: ReportDesignList.vue

**Files:**
- Modify: `frontend/src/views/report/ReportDesignList.vue`

**Step 1: 修改模板**

与 Task 1 相同模式。

**Step 2: 修改脚本**

添加动态高度逻辑。

**Step 3: 修改样式**

添加 Flex 布局样式。

**Step 4: 提交**

```bash
git add frontend/src/views/report/ReportDesignList.vue
git commit -m "feat: add dynamic table height to ReportDesignList"
```

---

## Task 6: BackupManagement.vue

**Files:**
- Modify: `frontend/src/views/system/BackupManagement.vue`

**Step 1: 修改模板**

BackupManagement 有 2 个表格（计划表格和备份表格），需要：
- 为每个表格添加独立的 wrapper 和 height ref
- scheduleTableWrapper + scheduleTableHeight
- backupTableWrapper + backupTableHeight

**Step 2: 修改脚本**

添加两组动态高度逻辑。

**Step 3: 修改样式**

添加 Flex 布局样式，保持左右布局。

**Step 4: 提交**

```bash
git add frontend/src/views/system/BackupManagement.vue
git commit -m "feat: add dynamic table height to BackupManagement"
```

---

## Task 7: 最终验证和推送

**Step 1: 验证所有页面**

访问每个管理页面，确认：
- 表格填满可用空间
- 数据多时表格内部滚动
- 分页栏固定在底部
- 窗口缩放时高度自适应

**Step 2: 推送到远程**

```bash
git push origin master
```

---

## 关键代码模板

### 通用 script 逻辑

```javascript
// 在 imports 中添加 onUnmounted, nextTick
import { ref, reactive, onMounted, onUnmounted, nextTick } from 'vue'

// 添加 ref
const tableWrapper = ref(null)
const tableHeight = ref(null)

// 添加高度计算函数
const updateTableHeight = () => {
  nextTick(() => {
    if (tableWrapper.value) {
      tableHeight.value = tableWrapper.value.clientHeight
    }
  })
}

// 修改 onMounted
onMounted(() => {
  loadData()  // 保持原有逻辑
  // ... 其他原有逻辑
  nextTick(updateTableHeight)
  window.addEventListener('resize', updateTableHeight)
})

// 添加 onUnmounted
onUnmounted(() => {
  window.removeEventListener('resize', updateTableHeight)
})
```

### 通用 CSS 样式

```css
.xxx-management {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.flex-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.flex-card :deep(.el-card__header) {
  flex-shrink: 0;
}

.flex-card :deep(.el-card__body) {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.search-grid {
  flex-shrink: 0;
}

.table-wrapper {
  flex: 1;
  overflow: hidden;
  min-height: 100px;
}

.el-pagination {
  flex-shrink: 0;
}
```
