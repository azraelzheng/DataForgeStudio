# 报表查询表格动态高度实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让报表查询结果表格根据屏幕高度自动适配，显示尽可能多的数据行

**Architecture:** 修改 CSS 布局使用 flex 收缩，添加 resize 监听器动态计算表格高度

**Tech Stack:** Vue 3 Composition API, Element Plus, CSS Flexbox

---

## Task 1: 修改 CSS 布局支持动态高度

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (CSS section)

**Step 1: 修改 results-container 样式**

找到 `.results-container` 样式，添加 `min-height: 0`：

```css
.results-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: var(--bg-card);
  border-radius: 8px;
  box-shadow: var(--shadow-card);
  overflow: hidden;
  min-height: 0;  /* 关键：允许 flex 收缩 */
}
```

**Step 2: 修改 table-wrapper 样式**

找到 `.table-wrapper` 样式，修改为：

```css
.table-wrapper {
  flex: 1;
  min-height: 0;  /* 关键：允许 flex 收缩 */
  overflow: hidden;
  padding: 0;
}
```

**Step 3: 验证前端编译**

检查 Vite HMR 输出，确保无编译错误。

**Step 4: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "style: update table container CSS for dynamic height"
```

---

## Task 2: 实现动态高度计算逻辑

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue` (script section)

**Step 1: 添加 updateTableHeight 函数**

在 `tableMaxHeight` 定义附近添加更新函数：

```javascript
const tableMaxHeight = ref(400)  // 表格最大高度

// 动态计算表格高度
const updateTableHeight = () => {
  nextTick(() => {
    if (tableWrapperRef.value) {
      // 使用容器高度，减去预留空间（分页栏、合计行等）
      const wrapperHeight = tableWrapperRef.value.clientHeight
      if (wrapperHeight > 0) {
        // 预留 60px 给分页栏和合计行
        tableMaxHeight.value = Math.max(wrapperHeight - 60, 200)
      }
    }
  })
}
```

**Step 2: 添加生命周期钩子**

在 `onMounted` 和 `onUnmounted` 中添加 resize 监听：

```javascript
onMounted(() => {
  // 初始计算
  updateTableHeight()
  // 监听窗口大小变化
  window.addEventListener('resize', updateTableHeight)
})

onUnmounted(() => {
  window.removeEventListener('resize', updateTableHeight)
})
```

**Step 3: 监听数据变化时重新计算**

找到 `reportData` 的 watch 或相关逻辑，确保数据加载后更新高度：

```javascript
// 在 watch reportData 或 handleQuery 成功后调用
watch(reportData, () => {
  if (reportData.value?.length > 0) {
    nextTick(updateTableHeight)
  }
})
```

或者在 `handleQuery` 函数成功后添加：

```javascript
// 在 handleQuery 的成功回调中
if (res.success) {
  reportData.value = res.data
  // 数据加载后更新表格高度
  nextTick(updateTableHeight)
}
```

**Step 4: 验证功能**

1. 打开报表查询页面
2. 选择报表并执行查询
3. 调整浏览器窗口大小
4. 验证表格高度自动调整

**Step 5: Commit**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: implement dynamic table height based on screen size"
```

---

## Task 3: 测试与优化

**Step 1: 测试不同屏幕尺寸**

- 全屏窗口
- 半屏窗口
- 小窗口
- 调整窗口大小时表格是否响应

**Step 2: 测试不同数据量**

- 少量数据（1-5行）
- 中等数据（10-50行）
- 大量数据（100+行）

**Step 3: 测试分页**

- 切换分页时表格高度是否稳定
- 分页栏是否正确显示

**Step 4: 最终 Commit（如有修复）**

```bash
git add -A
git commit -m "fix: dynamic table height edge cases"
```

---

## 完成检查清单

- [ ] CSS 布局支持 flex 收缩
- [ ] 添加 updateTableHeight 函数
- [ ] 添加 resize 事件监听
- [ ] 数据加载后更新高度
- [ ] 不同屏幕尺寸测试通过
- [ ] 不同数据量测试通过
- [ ] 分页功能正常
