# Dashboard-Report 集成修复实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复 V1.1.0 标准版看板组件只能引用报表的架构要求

**Architecture:** 后端添加 enableChart 过滤参数和组件管理 API；前端移除 SQL 选项，添加组件管理 UI

**Tech Stack:** ASP.NET Core 8.0, Vue 3, Element Plus, TypeScript

---

## Task 1: ReportsController 添加 enableChart 过滤参数

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Controllers/ReportsController.cs`

**Step 1: 定位 GetReports 方法**

找到 `GetReports` 方法的参数定义部分。

**Step 2: 添加 enableChart 参数**

在方法参数中添加：

```csharp
[HttpGet]
public async Task<ActionResult<ApiResponse<PagedResult<ReportDto>>>> GetReports(
    [FromQuery] int? page = null,
    [FromQuery] int? pageSize = null,
    [FromQuery] string? keyword = null,
    [FromQuery] string? category = null,
    [FromQuery] bool? enableChart = null)  // 新增此参数
```

**Step 3: 添加过滤逻辑**

在查询构建部分添加：

```csharp
// 在 var query = _context.Reports 之后添加
if (enableChart.HasValue)
{
    query = query.Where(r => r.EnableChart == enableChart.Value);
}
```

**Step 4: 验证修改**

```bash
cd /h/DataForge/backend && dotnet build
```

Expected: Build succeeded

**Step 5: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Controllers/ReportsController.cs
git commit -m "feat(api): add enableChart filter to GetReports endpoint

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 2: DashboardController 添加组件管理 API

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Controllers/DisplayController.cs` (DashboardController 部分)

**Step 1: 定位 DashboardController 类**

在 `DisplayController.cs` 中找到 `DashboardController` 类（约第 182 行）。

**Step 2: 添加 CreateWidgetRequest DTO**

在 `DashboardDto.cs` 文件中添加：

```csharp
// backend/src/DataForgeStudio.Domain/DTOs/DashboardDto.cs

public class CreateWidgetRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;  // chart, number-card, table
    public string? ReportId { get; set; }
    public string? Config { get; set; }  // JSON 配置
}

public class UpdateWidgetRequest
{
    public string? Name { get; set; }
    public string? Config { get; set; }
}
```

**Step 3: 添加 AddWidget API**

在 DashboardController 中添加：

```csharp
/// <summary>
/// 添加看板组件
/// </summary>
[HttpPost("{id}/widgets")]
public async Task<ActionResult<ApiResponse<DashboardWidgetDto>>> AddWidget(
    string id,
    [FromBody] CreateWidgetRequest request)
{
    var dashboard = await _context.Dashboards
        .Include(d => d.Widgets)
        .FirstOrDefaultAsync(d => d.Id == id);

    if (dashboard == null)
    {
        return NotFound(ApiResponse<DashboardWidgetDto>.ErrorResponse("看板不存在"));
    }

    var widget = new DashboardWidget
    {
        Id = Guid.NewGuid().ToString("N"),
        DashboardId = id,
        Name = request.Name,
        Type = request.Type,
        Config = request.Config ?? "{}",
        Position = "{}",
        DisplayOrder = dashboard.Widgets.Count
    };

    _context.DashboardWidgets.Add(widget);
    await _context.SaveChangesAsync();

    var dto = new DashboardWidgetDto
    {
        Id = widget.Id,
        Name = widget.Name,
        Type = widget.Type,
        Config = widget.Config,
        Position = widget.Position,
        DisplayOrder = widget.DisplayOrder
    };

    return Ok(ApiResponse<DashboardWidgetDto>.SuccessResponse(dto));
}
```

**Step 4: 添加 UpdateWidget API**

```csharp
/// <summary>
/// 更新看板组件
/// </summary>
[HttpPut("{id}/widgets/{widgetId}")]
public async Task<ActionResult<ApiResponse<DashboardWidgetDto>>> UpdateWidget(
    string id,
    string widgetId,
    [FromBody] UpdateWidgetRequest request)
{
    var widget = await _context.DashboardWidgets
        .FirstOrDefaultAsync(w => w.Id == widgetId && w.DashboardId == id);

    if (widget == null)
    {
        return NotFound(ApiResponse<DashboardWidgetDto>.ErrorResponse("组件不存在"));
    }

    if (request.Name != null)
        widget.Name = request.Name;

    if (request.Config != null)
        widget.Config = request.Config;

    await _context.SaveChangesAsync();

    var dto = new DashboardWidgetDto
    {
        Id = widget.Id,
        Name = widget.Name,
        Type = widget.Type,
        Config = widget.Config,
        Position = widget.Position,
        DisplayOrder = widget.DisplayOrder
    };

    return Ok(ApiResponse<DashboardWidgetDto>.SuccessResponse(dto));
}
```

**Step 5: 添加 DeleteWidget API**

```csharp
/// <summary>
/// 删除看板组件
/// </summary>
[HttpDelete("{id}/widgets/{widgetId}")]
public async Task<ActionResult<ApiResponse>> DeleteWidget(string id, string widgetId)
{
    var widget = await _context.DashboardWidgets
        .FirstOrDefaultAsync(w => w.Id == widgetId && w.DashboardId == id);

    if (widget == null)
    {
        return NotFound(ApiResponse.ErrorResponse("组件不存在"));
    }

    _context.DashboardWidgets.Remove(widget);
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.SuccessResponse("删除成功"));
}
```

**Step 6: 验证修改**

```bash
cd /h/DataForge/backend && dotnet build
```

Expected: Build succeeded

**Step 7: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Controllers/DisplayController.cs
git add backend/src/DataForgeStudio.Domain/DTOs/DashboardDto.cs
git commit -m "feat(api): add widget management APIs to DashboardController

- POST /api/dashboard/{id}/widgets - add widget
- PUT /api/dashboard/{id}/widgets/{widgetId} - update widget
- DELETE /api/dashboard/{id}/widgets/{widgetId} - delete widget

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 3: ChartConfigPanel.vue 移除 SQL 选项

**Files:**
- Modify: `frontend/src/dashboard/widgets/config/ChartConfigPanel.vue`

**Step 1: 找到数据源类型选择代码**

定位到 `dataSourceType` 相关的 radio-group 代码。

**Step 2: 移除 SQL 选项**

将：
```vue
<el-form-item label="数据源类型" prop="dataSourceType">
  <el-radio-group v-model="formData.dataSourceType">
    <el-radio value="report">报表</el-radio>
    <el-radio value="sql">SQL</el-radio>
  </el-radio-group>
</el-form-item>
```

改为：
```vue
<!-- 标准版只支持报表数据源 -->
```
（删除整个 el-form-item）

**Step 3: 修改报表选择条件**

将：
```vue
<el-form-item v-if="formData.dataSourceType === 'report'" label="选择报表" prop="reportId">
```

改为：
```vue
<el-form-item label="选择报表" prop="reportId">
```

**Step 4: 添加报表过滤逻辑**

在 `loadReports` 函数中添加过滤：

```typescript
async function loadReports(): Promise<void> {
  try {
    const response = await request.get('/reports', {
      params: {
        enableChart: true,
        pageSize: 100
      }
    })
    reportList.value = response.data?.items || response.data || []
  } catch (error) {
    console.error('[ChartConfigPanel] 加载报表列表失败:', error)
  }
}
```

**Step 5: 固定 dataSourceType**

在 formData 初始化时固定为 'report'：

```typescript
const formData = ref<ChartConfigData>({
  // ...
  dataSourceType: 'report',  // 标准版固定为报表
  // ...
})
```

**Step 6: Commit**

```bash
git add frontend/src/dashboard/widgets/config/ChartConfigPanel.vue
git commit -m "fix(frontend): remove SQL option from ChartConfigPanel

Standard version only supports report data source.
Add enableChart=true filter when loading reports.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 4: NumberCardConfig.vue 移除 SQL 选项

**Files:**
- Modify: `frontend/src/dashboard/widgets/config/NumberCardConfig.vue`

**Step 1-6: 同 Task 3**

执行与 ChartConfigPanel.vue 相同的修改：
1. 移除数据源类型选择
2. 移除 SQL 相关表单项
3. 添加 enableChart=true 过滤
4. 固定 dataSourceType 为 'report'

**Step 7: Commit**

```bash
git add frontend/src/dashboard/widgets/config/NumberCardConfig.vue
git commit -m "fix(frontend): remove SQL option from NumberCardConfig

Standard version only supports report data source.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 5: TableConfig.vue 移除 SQL 选项

**Files:**
- Modify: `frontend/src/dashboard/widgets/config/TableConfig.vue`

**Step 1-6: 同 Task 3**

执行与 ChartConfigPanel.vue 相同的修改。

**Step 7: Commit**

```bash
git add frontend/src/dashboard/widgets/config/TableConfig.vue
git commit -m "fix(frontend): remove SQL option from TableConfig

Standard version only supports report data source.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 6: DashboardManagement.vue 添加组件管理 UI

**Files:**
- Modify: `frontend/src/views/dashboard/DashboardManagement.vue`

**Step 1: 添加状态变量**

在 script setup 中添加：

```typescript
// 设计器状态
const designerVisible = ref(false)
const currentDashboard = ref<Dashboard | null>(null)
const widgetDialogVisible = ref(false)
const widgetForm = ref({
  name: '',
  type: 'chart',
  reportId: ''
})
const availableReports = ref<Array<{ id: string; name: string }>>([])
```

**Step 2: 添加"设计"按钮**

在操作列添加：

```vue
<el-button
  type="primary"
  size="small"
  :icon="Setting"
  @click="openDesigner(row)"
>
  设计
</el-button>
```

**Step 3: 添加导入图标**

```typescript
import { Plus, Edit, Delete, Setting } from '@element-plus/icons-vue'
```

**Step 4: 添加 openDesigner 函数**

```typescript
async function openDesigner(dashboard: Dashboard): Promise<void> {
  currentDashboard.value = dashboard
  designerVisible.value = true
  await loadDashboardWidgets()
  await loadAvailableReports()
}

async function loadDashboardWidgets(): Promise<void> {
  if (!currentDashboard.value) return
  // 加载看板详情包含组件
  const response = await request.get(`/dashboard/${currentDashboard.value.id}`)
  if (response.success) {
    currentDashboard.value = response.data
  }
}

async function loadAvailableReports(): Promise<void> {
  const response = await request.get('/reports', {
    params: { enableChart: true, pageSize: 100 }
  })
  availableReports.value = response.data?.items || response.data || []
}
```

**Step 5: 添加组件管理对话框**

```vue
<!-- 组件管理对话框 -->
<el-dialog
  v-model="designerVisible"
  :title="`看板设计 - ${currentDashboard?.name || ''}`"
  width="90%"
  destroy-on-close
>
  <div class="designer-content">
    <!-- 组件列表 -->
    <el-card header="组件列表">
      <el-table :data="currentDashboard?.widgets || []" stripe>
        <el-table-column prop="name" label="组件名称" width="200" />
        <el-table-column prop="type" label="类型" width="120">
          <template #default="{ row }">
            <el-tag>{{ getWidgetTypeName(row.type) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150">
          <template #default="{ row }">
            <el-button type="danger" size="small" link @click="deleteWidget(row.id)">
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-button type="primary" :icon="Plus" style="margin-top: 16px" @click="showAddWidgetDialog">
        添加组件
      </el-button>
    </el-card>
  </div>
</el-dialog>
```

**Step 6: 添加组件对话框**

```vue
<!-- 添加组件对话框 -->
<el-dialog
  v-model="widgetDialogVisible"
  title="添加组件"
  width="500px"
  @close="resetWidgetForm"
>
  <el-form :model="widgetForm" label-width="100px">
    <el-form-item label="组件名称" required>
      <el-input v-model="widgetForm.name" placeholder="请输入组件名称" />
    </el-form-item>

    <el-form-item label="组件类型" required>
      <el-select v-model="widgetForm.type" placeholder="选择组件类型">
        <el-option label="图表" value="chart" />
        <el-option label="数据卡片" value="number-card" />
        <el-option label="数据表格" value="table" />
      </el-select>
    </el-form-item>

    <el-form-item label="选择报表" required>
      <el-select v-model="widgetForm.reportId" placeholder="选择报表" filterable>
        <el-option
          v-for="report in availableReports"
          :key="report.id"
          :label="report.name"
          :value="report.id"
        />
      </el-select>
    </el-form-item>
  </el-form>

  <template #footer>
    <el-button @click="widgetDialogVisible = false">取消</el-button>
    <el-button type="primary" @click="addWidget">确定</el-button>
  </template>
</el-dialog>
```

**Step 7: 添加操作函数**

```typescript
function showAddWidgetDialog(): void {
  widgetForm.value = {
    name: '',
    type: 'chart',
    reportId: ''
  }
  widgetDialogVisible.value = true
}

function resetWidgetForm(): void {
  widgetForm.value = {
    name: '',
    type: 'chart',
    reportId: ''
  }
}

function getWidgetTypeName(type: string): string {
  const names: Record<string, string> = {
    'chart': '图表',
    'number-card': '数据卡片',
    'table': '数据表格'
  }
  return names[type] || type
}

async function addWidget(): Promise<void> {
  if (!currentDashboard.value) return
  if (!widgetForm.value.name || !widgetForm.value.reportId) {
    ElMessage.warning('请填写完整信息')
    return
  }

  try {
    const response = await request.post(
      `/dashboard/${currentDashboard.value.id}/widgets`,
      {
        name: widgetForm.value.name,
        type: widgetForm.value.type,
        reportId: widgetForm.value.reportId,
        config: JSON.stringify({ reportId: widgetForm.value.reportId })
      }
    )

    if (response.success) {
      ElMessage.success('添加成功')
      widgetDialogVisible.value = false
      await loadDashboardWidgets()
    } else {
      ElMessage.error(response.message || '添加失败')
    }
  } catch (error) {
    console.error('[DashboardManagement] 添加组件失败:', error)
    ElMessage.error('添加失败')
  }
}

async function deleteWidget(widgetId: string): Promise<void> {
  if (!currentDashboard.value) return

  try {
    await ElMessageBox.confirm('确定要删除该组件吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    const response = await request.delete(
      `/dashboard/${currentDashboard.value.id}/widgets/${widgetId}`
    )

    if (response.success) {
      ElMessage.success('删除成功')
      await loadDashboardWidgets()
    } else {
      ElMessage.error(response.message || '删除失败')
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('[DashboardManagement] 删除组件失败:', error)
      ElMessage.error('删除失败')
    }
  }
}
```

**Step 8: 添加样式**

```css
.designer-content {
  min-height: 400px;
}
```

**Step 9: 添加 Dashboard 类型**

确保导入正确的类型：

```typescript
interface Dashboard {
  id: string
  name: string
  description?: string
  widgets?: Widget[]
}

interface Widget {
  id: string
  name: string
  type: string
  config?: string
}
```

**Step 10: Commit**

```bash
git add frontend/src/views/dashboard/DashboardManagement.vue
git commit -m "feat(frontend): add widget management UI to DashboardManagement

- Add 'Design' button to open designer dialog
- Add widget list with delete function
- Add widget creation dialog with report selection
- Filter reports by enableChart=true

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Task 7: 集成测试

**Step 1: 启动后端服务**

```bash
cd /h/DataForge/backend/src/DataForgeStudio.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run &
```

**Step 2: 启动前端服务**

```bash
cd /h/DataForge/frontend && npm run dev &
```

**Step 3: 测试 API**

```bash
# 获取启用图表的报表
curl -s "http://localhost:5000/api/reports?enableChart=true" | head -100

# 登录获取 token
curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"root","password":"Admin@123"}'

# 测试组件 API (需要替换 token 和 dashboard id)
curl -s -X POST "http://localhost:5000/api/dashboard/{id}/widgets" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"name":"测试组件","type":"chart","reportId":"xxx"}'
```

**Step 4: 手动测试流程**

1. 登录系统 (root / Admin@123)
2. 进入"报表管理"，创建报表并启用图表
3. 进入"看板管理"，创建看板
4. 点击"设计"按钮，添加组件
5. 选择报表（只显示启用了图表的报表）
6. 保存组件
7. 进入"车间大屏"，创建配置并选择看板

---

## 完成标志

- [ ] ReportsController 支持 enableChart 过滤
- [ ] DashboardController 支持组件 CRUD
- [ ] ChartConfigPanel 只显示报表选项
- [ ] NumberCardConfig 只显示报表选项
- [ ] TableConfig 只显示报表选项
- [ ] DashboardManagement 支持组件管理

---

*实施计划由 Claude Code 生成*
