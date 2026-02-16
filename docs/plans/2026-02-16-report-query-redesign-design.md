# 报表查询界面重设计

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:writing-plans to create implementation plan.

**日期:** 2026-02-16

**目标:** 优化报表查询界面，使查询结果占据更大比例，支持列筛选、列排序，同时保持美观实用。

**技术栈:** Vue 3 Composition API, Element Plus, CSS3

---

## 1. 整体布局结构

```
┌────────────────────────────────────────────────────────────────────┐
│ ┌──────────┐ ┌──────────────────────────────────────────────────┐ │
│ │          │ │  ┌────────────────────────────────────────────┐  │ │
│ │  Report  │ │  │ Report Title + Category Tag                │  │ │
│ │  List    │ │  └────────────────────────────────────────────┘  │ │
│ │          │ │  ┌────────────────────────────────────────────┐  │ │
│ │ Sidebar  │ │  │ Query Conditions (collapsible)             │  │ │
│ │          │ │  │ [Condition inputs...] [Query] [Export]      │  │ │
│ │  [◄]     │ │  └────────────────────────────────────────────┘  │ │
│ │          │ │  ┌────────────────────────────────────────────┐  │ │
│ │          │ │  │                                            │  │ │
│ │          │ │  │         Results Table                       │  │ │
│ │          │ │  │   (expands to fill remaining space)        │  │ │
│ │          │ │  │                                            │  │ │
│ │          │ │  │   - Column header filters                  │  │ │
│ │          │ │  │   - Click to sort                          │  │ │
│ │          │ │  │   - Striped rows                           │  │ │
│ │          │ │  │                                            │  │ │
│ └──────────┘ │  └────────────────────────────────────────────┘  │ │
│              └──────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────┘
```

**关键改动:**
- 左侧边栏: 280px 宽，可收起到 48px 图标模式，带平滑动画
- 右侧区域: 使用 `flex: 1` 填充剩余空间
- 查询条件: 可折叠卡片（查询后可最小化）
- 结果表格: 占据所有剩余垂直空间 (`flex: 1` + `overflow: auto`)

---

## 2. 可折叠侧边栏设计

### 展开状态 (280px)
- 搜索框：快速过滤报表
- 报表列表：显示报表名称、分类标签、查看次数
- 收起按钮：底部 ◄ 按钮

### 收起状态 (48px)
- 只显示报表图标
- 鼠标悬停显示 tooltip（报表名称）
- 展开按钮：► 按钮

### 功能特点
- 动画效果：300ms ease-in-out 过渡
- 状态记忆：localStorage 保存用户偏好
- 选中高亮：收起状态下保持蓝色边框高亮

---

## 3. 查询结果区域

### 3.1 列头筛选

每列标题下方添加筛选输入框：

| 列类型 | 筛选控件 |
|--------|----------|
| 文本 (String) | 文本输入框，支持模糊匹配 |
| 数字 (Number) | 范围选择器（最小值 - 最大值） |
| 日期 (DateTime) | 日期范围选择器 |
| 布尔 (Boolean) | 下拉选择（全部/是/否） |

筛选逻辑：前端实时筛选，输入即生效

### 3.2 点击排序

- 点击列标题循环切换：无序 → 升序 ▲ → 降序 ▼ → 无序
- 当前排序列显示排序图标（蓝色）
- 排序时表格内容即时更新
- 支持单列排序

### 3.3 工具栏

- **列设置按钮**: 弹出对话框设置列可见性、顺序
- **记录统计**: 显示 "共 X 条" 或 "筛选结果 X / Y 条"

### 3.4 分页

- 底部分页器
- 显示 "显示 1-20 / 共 X 条"
- 支持页码跳转

---

## 4. 视觉美化

### 4.1 配色方案

```css
/* 主色调 */
--primary-color: #409eff;      /* Element Plus 蓝 */
--primary-light: #ecf5ff;      /* 浅蓝背景 */
--success-color: #67c23a;      /* 成功绿 */

/* 背景色 */
--bg-page: #f5f7fa;            /* 页面背景 */
--bg-card: #ffffff;            /* 卡片背景 */
--bg-hover: #f0f7ff;           /* 悬停背景 */

/* 边框 */
--border-light: #e4e7ed;       /* 浅边框 */
--border-active: #409eff;      /* 激活边框 */
```

### 4.2 卡片样式

- 圆角: 8px
- 阴影: `0 2px 12px rgba(0, 0, 0, 0.08)`
- 悬停时阴影加深
- 选中报表左侧蓝色指示条 (4px)

### 4.3 表格样式

- 表头: 深色背景 (#fafafa)，加粗字体
- 斑马纹: 奇偶行交替背景色
- 行悬停: 浅蓝色高亮
- 筛选框: 淡灰色背景，聚焦时蓝色边框
- 排序图标: 蓝色高亮当前排序列

### 4.4 动画效果

- 侧边栏收起/展开: 300ms ease-in-out
- 报表卡片悬停: scale(1.02) 微放大
- 表格行悬停: 背景色 200ms 渐变过渡
- 按钮点击: 微小下压效果 (transform: translateY(1px))

### 4.5 响应式设计

- 小屏幕 (< 768px): 侧边栏自动收起
- 表格支持水平滚动

---

## 5. 组件结构

```
ReportQuery.vue
├── Sidebar (可折叠)
│   ├── SearchInput
│   └── ReportList
│       └── ReportItem (多个)
├── MainContent
│   ├── ReportHeader
│   ├── QueryConditions (可折叠)
│   │   └── ConditionInputs
│   ├── ActionBar
│   │   ├── QueryButton
│   │   └── ExportButton
│   └── ResultsTable
│       ├── TableToolbar
│       │   ├── ResultCount
│       │   └── ColumnSettings
│       ├── ElTable (增强版)
│       │   ├── FilterableColumn (多个)
│       │   │   ├── Header (带排序图标)
│       │   │   └── FilterInput
│       │   └── TableBody
│       └── Pagination
```

---

## 6. 状态管理

```javascript
// 侧边栏状态
const sidebarCollapsed = ref(false)

// 表格状态
const currentSort = reactive({ field: null, order: null })
const columnFilters = reactive({})
const filteredData = computed(() => {
  // 应用筛选和排序逻辑
})

// 列设置
const visibleColumns = ref([])  // 可见列
const columnOrder = ref([])     // 列顺序

// 条件面板
const conditionsCollapsed = ref(false)
```

---

## 7. 实现优先级

1. **P0 - 核心功能**
   - 可折叠侧边栏
   - 表格自适应高度
   - 点击排序

2. **P1 - 增强功能**
   - 列头筛选
   - 列设置对话框
   - 条件面板折叠

3. **P2 - 美化优化**
   - 动画效果
   - 响应式设计
   - 状态持久化

---

## 批准记录

- [x] Section 1: 整体布局结构 - 已批准
- [x] Section 2: 可折叠侧边栏设计 - 已批准
- [x] Section 3: 查询结果区域 - 已批准
- [x] Section 4: 视觉美化 - 已批准
