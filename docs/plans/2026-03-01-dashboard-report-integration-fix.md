# Dashboard-Report 集成修复设计文档

**日期**: 2026-03-01
**版本**: V1.1.0 标准版
**状态**: 已批准

## 背景

V1.1.0 Dashboard/Kanban 功能需要修复以符合标准版架构要求。标准版看板组件只能引用报表，不能独立配置 SQL。

## 架构

```
报表 (Report)                    - 核心数据源
  ↓ 引用
看板组件 (DashboardWidget)        - 只能引用报表
  ↓ 包含
看板 (Dashboard)                  - 布局容器
  ↓ 引用
车间大屏 (Display)                - 轮播展示
```

## 需求确认

| 需求 | 决定 |
|------|------|
| 版本类型 | 标准版（组件只能引用报表） |
| 看板设计器 | 阶段1实现简单列表管理，阶段2实现可视化设计器 |
| 报表过滤 | 只显示 EnableChart=true 的报表 |
| 车间大屏 | 保持现状 |

## 修改范围

### 第一部分：后端 API 修改

#### 1.1 ReportsController - 添加过滤参数

**文件**: `backend/src/DataForgeStudio.Api/Controllers/ReportsController.cs`

```csharp
// GET /api/reports?enableChart=true
[HttpGet]
public async Task<ActionResult<ApiResponse<PagedResult<ReportDto>>>> GetReports(
    [FromQuery] int? page = null,
    [FromQuery] int? pageSize = null,
    [FromQuery] string? keyword = null,
    [FromQuery] bool? enableChart = null)  // 新增参数
{
    var query = _context.Reports
        .Include(r => r.DataSource)
        .Where(r => !r.IsDeleted);

    if (enableChart.HasValue)
    {
        query = query.Where(r => r.EnableChart == enableChart.Value);
    }
    // ... 其余逻辑保持不变
}
```

#### 1.2 DashboardController - 添加组件管理 API

**文件**: `backend/src/DataForgeStudio.Api/Controllers/DisplayController.cs` (DashboardController 部分)

```csharp
// POST /api/dashboard/{id}/widgets - 添加组件
[HttpPost("{id}/widgets")]
public async Task<ActionResult<ApiResponse<DashboardWidgetDto>>> AddWidget(
    string id, [FromBody] CreateWidgetRequest request)

// PUT /api/dashboard/{id}/widgets/{widgetId} - 更新组件
[HttpPut("{id}/widgets/{widgetId}")]
public async Task<ActionResult<ApiResponse<DashboardWidgetDto>>> UpdateWidget(
    string id, string widgetId, [FromBody] UpdateWidgetRequest request)

// DELETE /api/dashboard/{id}/widgets/{widgetId} - 删除组件
[HttpDelete("{id}/widgets/{widgetId}")]
public async Task<ActionResult<ApiResponse>> DeleteWidget(string id, string widgetId)
```

### 第二部分：前端配置面板修改

#### 2.1 ChartConfigPanel.vue - 移除 SQL 选项

**文件**: `frontend/src/dashboard/widgets/config/ChartConfigPanel.vue`

修改内容：
- 移除 `dataSourceType` 单选组（report/sql 选择）
- 固定为报表类型
- 加载报表时使用 `?enableChart=true` 过滤

```vue
<!-- 修改后 -->
<el-form-item label="选择报表" prop="reportId">
  <el-select v-model="formData.reportId" placeholder="选择报表" filterable>
    <el-option
      v-for="report in reportList"
      :key="report.id"
      :label="report.name"
      :value="report.id"
    />
  </el-select>
</el-form-item>
```

```typescript
// 加载报表列表
async function loadReports() {
  const response = await request.get('/reports', {
    params: { enableChart: true, pageSize: 100 }
  })
  reportList.value = response.data?.items || response.data || []
}
```

#### 2.2 NumberCardConfig.vue - 同样修改

**文件**: `frontend/src/dashboard/widgets/config/NumberCardConfig.vue`

- 移除 SQL 选项
- 添加报表过滤

#### 2.3 TableConfig.vue - 同样修改

**文件**: `frontend/src/dashboard/widgets/config/TableConfig.vue`

- 移除 SQL 选项
- 添加报表过滤

### 第三部分：看板管理页面组件管理

#### 3.1 DashboardManagement.vue - 添加设计按钮

**文件**: `frontend/src/views/dashboard/DashboardManagement.vue`

在操作列添加"设计"按钮：
```vue
<el-button type="primary" size="small" @click="openDesigner(row)">
  设计
</el-button>
```

#### 3.2 组件管理对话框

新增以下功能：
- 组件列表显示（名称、类型、关联报表）
- 添加组件按钮
- 编辑/删除组件操作

#### 3.3 添加组件对话框

表单字段：
- 组件类型（图表/数据卡片/数据表格）
- 选择报表（只显示 EnableChart=true 的报表）
- 组件名称

### 第四部分：数据流

```
用户操作                          系统响应
─────────────────────────────────────────────────────────
1. 打开看板设计器        → 加载看板详情和已有组件
2. 点击"添加组件"         → 打开组件配置对话框
3. 选择组件类型           → 加载对应配置面板
4. 选择报表               → 只显示 EnableChart=true 的报表
5. 保存组件               → 调用 POST /api/dashboard/{id}/widgets
6. 组件显示在看板列表      → 刷新组件列表

车间大屏轮播时：
Display → 加载 DashboardIds → 加载每个 Dashboard → 加载每个 Widget → 调用报表 API 获取数据
```

## 实现顺序

| 步骤 | 文件 | 修改内容 | 预计工作量 |
|------|------|----------|------------|
| 1 | ReportsController.cs | 添加 `enableChart` 过滤参数 | 0.5h |
| 2 | DisplayController.cs (DashboardController) | 添加组件管理 API | 1h |
| 3 | ChartConfigPanel.vue | 移除 SQL 选项，加载过滤后的报表 | 0.5h |
| 4 | NumberCardConfig.vue | 同上 | 0.5h |
| 5 | TableConfig.vue | 同上 | 0.5h |
| 6 | DashboardManagement.vue | 添加组件管理 UI | 2h |

**总预计工作量**: 约 5 小时

## 测试计划

1. **API 测试**
   - 验证 `/api/reports?enableChart=true` 只返回启用图表的报表
   - 验证组件 CRUD API 正常工作

2. **UI 测试**
   - 验证配置面板不显示 SQL 选项
   - 验证报表下拉框只显示 EnableChart=true 的报表
   - 验证看板管理页面的组件管理功能

3. **集成测试**
   - 创建报表 → 启用图表 → 在看板中引用 → 在车间大屏展示

## 后续阶段（V1.2.0）

- 可视化网格设计器
- 组件拖拽定位和大小调整
- 实时预览功能

---

*设计文档由 Claude Code 生成*
