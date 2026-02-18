# 管理页面动态表格高度设计

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:writing-plans to create implementation plan.

**日期**: 2026-02-18
**状态**: 已批准

---

## 需求

为管理页面实现动态表格滚动功能：
1. 表格自动填满可用空间
2. 数据多时表格内部滚动，而不是整个页面滚动
3. 分页栏固定在表格下方

---

## 需要修改的页面

| 页面 | 文件路径 | 表格数量 |
|------|----------|----------|
| 用户管理 | `frontend/src/views/system/UserManagement.vue` | 1 |
| 权限组管理 | `frontend/src/views/system/RoleManagement.vue` | 1 |
| 数据源管理 | `frontend/src/views/system/DataSourceManagement.vue` | 1 |
| 日志管理 | `frontend/src/views/system/LogManagement.vue` | 1 |
| 报表设计列表 | `frontend/src/views/report/ReportDesignList.vue` | 1 |
| 备份管理 | `frontend/src/views/system/BackupManagement.vue` | 2 |

---

## 设计方案

### 布局结构

```
┌─────────────────────────────────────┐ ← 页面顶部
│ el-card                             │
│ ┌─────────────────────────────────┐ │
│ │ Header (卡片标题 + 按钮)         │ │
│ ├─────────────────────────────────┤ │
│ │ 搜索表单 (flex-shrink: 0)       │ │
│ ├─────────────────────────────────┤ │
│ │ ┌─────────────────────────────┐ │ │
│ │ │                             │ │ │
│ │ │   表格 (flex: 1)            │ │ │
│ │ │   内部滚动                   │ │ │
│ │ │                             │ │ │
│ │ └─────────────────────────────┘ │ │
│ ├─────────────────────────────────┤ │
│ │ 分页栏 (flex-shrink: 0)         │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘ ← 页面底部
```

### 模板结构

```vue
<template>
  <div class="xxx-management">
    <el-card class="flex-card">
      <template #header>
        <div class="card-header">...</div>
      </template>

      <!-- 搜索表单 -->
      <div class="search-grid">...</div>

      <!-- 表格容器 -->
      <div class="table-wrapper" ref="tableWrapper">
        <el-table :data="tableData" :height="tableHeight" v-loading="loading" border stripe>
          ...
        </el-table>
      </div>

      <!-- 分页 -->
      <el-pagination ... />
    </el-card>
  </div>
</template>
```

### Script 逻辑

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

onMounted(() => {
  loadData()
  nextTick(updateTableHeight)
  window.addEventListener('resize', updateTableHeight)
})

onUnmounted(() => {
  window.removeEventListener('resize', updateTableHeight)
})
```

### CSS 样式

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
  /* 现有样式保持不变 */
}

.table-wrapper {
  flex: 1;
  overflow: hidden;
  min-height: 100px;
}

.el-pagination {
  flex-shrink: 0;
  margin-top: 16px;
}
```

---

## BackupManagement 特殊处理

BackupManagement.vue 有 2 个表格（计划表格和备份表格），使用 el-row/el-col 布局：
- 左侧计划表格
- 右侧备份表格

处理方式：
- 为每个表格容器单独计算高度
- 保持现有的左右布局结构

---

## 实现要点

1. **Flex 布局**：使用 `display: flex` + `flex-direction: column`
2. **表格容器**：`.table-wrapper` 使用 `flex: 1` 填满剩余空间
3. **动态高度**：通过 `:height="tableHeight"` 绑定
4. **响应式**：监听 resize 事件
5. **防止溢出**：设置 `overflow: hidden`

---

## 参考

- 已实现示例：`frontend/src/views/report/ReportQuery.vue`
