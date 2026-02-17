# 报表查询表格动态高度优化设计

**日期**: 2026-02-18
**状态**: 已批准

---

## 需求

报表查询页面的查询结果表格应该：
1. 根据屏幕高度自动适配，显示尽可能多的数据行
2. 合计行保持在表格底部
3. 水平滚动条在合计行下方（视口底部）

---

## 当前问题

`tableMaxHeight` 硬编码为 400px，不会响应屏幕尺寸变化：

```javascript
const tableMaxHeight = ref(400)  // 固定值，不响应屏幕大小
```

---

## 设计方案

### 动态高度计算

使用容器引用和响应式计算：

```javascript
// 使用 ResizeObserver 监听容器大小变化
const updateTableHeight = () => {
  if (tableWrapperRef.value) {
    // 获取容器可用高度
    const rect = tableWrapperRef.value.getBoundingClientRect()
    // 减去分页栏和合计行的高度
    tableMaxHeight.value = rect.height - 60  // 预留分页和合计行空间
  }
}
```

### 布局结构

```
┌─────────────────────────────────────┐ ← 视口顶部
│ 顶部导航栏 (固定)                    │
├─────────────────────────────────────┤
│ 报表标题                             │
├─────────────────────────────────────┤
│ 查询条件区 (可折叠)                  │
├─────────────────────────────────────┤
│ 工具栏 (记录数 + 导出按钮)           │
├─────────────────────────────────────┤
│ ┌─────────────────────────────────┐ │
│ │                                 │ │
│ │   数据行 (自动填充可用空间)      │ │
│ │                                 │ │
│ ├─────────────────────────────────┤ │
│ │   合计行                         │ │
│ ├─────────────────────────────────┤ │
│ │ ▼ 水平滚动条                     │ │
│ └─────────────────────────────────┘ │
├─────────────────────────────────────┤
│ 分页栏                               │
└─────────────────────────────────────┘ ← 视口底部
```

### 实现要点

1. **results-container**: `flex: 1` 填充剩余空间
2. **table-wrapper**: `flex: 1` + `overflow: hidden`
3. **el-table**: 使用 `max-height` 动态计算
4. **监听 resize**: 窗口大小变化时重新计算

---

## 影响范围

| 文件 | 改动 |
|------|------|
| `ReportQuery.vue` | 添加动态高度计算逻辑 |

---

## 改动细节

### 1. 修改 results-container 样式

```css
.results-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;  /* 关键：允许 flex 收缩 */
  overflow: hidden;
}
```

### 2. 修改 table-wrapper 样式

```css
.table-wrapper {
  flex: 1;
  min-height: 0;  /* 关键：允许 flex 收缩 */
  overflow: hidden;
}
```

### 3. 添加动态高度计算

```javascript
import { useResizeObserver } from '@vueuse/core'

// 或者手动实现
const updateTableHeight = () => {
  nextTick(() => {
    if (tableWrapperRef.value) {
      const wrapper = tableWrapperRef.value
      tableMaxHeight.value = wrapper.clientHeight - 10  // 预留空间
    }
  })
}

onMounted(() => {
  updateTableHeight()
  window.addEventListener('resize', updateTableHeight)
})

onUnmounted(() => {
  window.removeEventListener('resize', updateTableHeight)
})

// 监听 results-container 显示/隐藏
watch(() => reportData.value, () => {
  if (reportData.value?.length > 0) {
    nextTick(updateTableHeight)
  }
})
```
