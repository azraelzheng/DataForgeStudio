# Dashboard Query Conditions Reuse Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable dashboard widgets to use report query conditions, storing condition values in widget config and passing them when fetching data.

**Architecture:** Backend parses widget DataConfig JSON to extract queryConditionValues, passes them to ExecuteReportAsync. Frontend fetches report query conditions when selecting a report, displays input controls, and stores values in widget config.

**Tech Stack:** ASP.NET Core 8.0, Vue 3 + Composition API, Element Plus, System.Text.Json

---

## Task 1: Backend - Parse DataConfig and Pass Query Conditions

**Files:**
- Modify: `H:\DataForge\backend\src\DataForgeStudio.Core\Services\DashboardService.cs:625-657`

**Step 1: Add WidgetDataConfig helper class**

Add this private class inside DashboardService (after the existing helper classes region or at the end of the file before the closing brace):

```csharp
/// <summary>
/// 组件数据配置辅助类
/// </summary>
private class WidgetDataConfig
{
    public int? ReportId { get; set; }
    public Dictionary<string, object>? QueryConditionValues { get; set; }
}
```

**Step 2: Modify GetWidgetDataAsync to parse and pass query conditions**

Replace the existing `GetWidgetDataAsync` method (lines 625-657) with:

```csharp
/// <summary>
/// 获取单个组件的数据
/// </summary>
private async Task<WidgetDataResult> GetWidgetDataAsync(DashboardWidget widget)
{
    try
    {
        // 解析查询条件值
        Dictionary<string, object> conditionValues = new();
        if (!string.IsNullOrEmpty(widget.DataConfig))
        {
            try
            {
                var dataConfig = JsonSerializer.Deserialize<WidgetDataConfig>(widget.DataConfig, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (dataConfig?.QueryConditionValues != null)
                {
                    conditionValues = dataConfig.QueryConditionValues;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "解析组件 DataConfig 失败: WidgetId={WidgetId}", widget.WidgetId);
            }
        }

        var executeRequest = new ExecuteReportRequest
        {
            Parameters = conditionValues
        };
        var reportResult = await _reportService.ExecuteReportAsync(widget.ReportId, executeRequest);

        return new WidgetDataResult
        {
            WidgetId = widget.WidgetId,
            Success = reportResult.Success,
            ErrorMessage = reportResult.Success ? null : reportResult.Message,
            Data = reportResult.Data?.Select(d => d.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value == null ? null : kvp.Value
            )).ToList(),
            FetchTime = DateTime.UtcNow
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取组件数据失败: WidgetId={WidgetId}, ReportId={ReportId}",
            widget.WidgetId, widget.ReportId);

        return new WidgetDataResult
        {
            WidgetId = widget.WidgetId,
            Success = false,
            ErrorMessage = $"获取数据失败: {ex.Message}",
            FetchTime = DateTime.UtcNow
        };
    }
}
```

**Step 3: Verify the required using statement exists**

Ensure `System.Text.Json` is imported at the top of DashboardService.cs. If not present, add:

```csharp
using System.Text.Json;
```

**Step 4: Build and verify**

Run: `cd H:/DataForge && dotnet build backend/DataForgeStudio.sln`
Expected: Build succeeded with no errors

**Step 5: Commit**

```bash
cd H:/DataForge
git add backend/src/DataForgeStudio.Core/Services/DashboardService.cs
git commit -m "$(cat <<'EOF'
feat(backend): pass query condition values in GetWidgetDataAsync

- Add WidgetDataConfig helper class to parse DataConfig JSON
- Extract queryConditionValues and pass to ExecuteReportRequest
- Add error handling for JSON parsing

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Frontend - Add Query Conditions State and Fetch Logic

**Files:**
- Modify: `H:\DataForge\frontend\src\views\dashboard\DashboardDesigner.vue`

**Step 1: Add state variables for query conditions**

In the script section, after `const availableFields = ref([])` (around line 444), add:

```javascript
const widgetQueryConditions = ref([])  // 当前选中组件的查询条件定义
const queryConditionValues = ref({})   // 当前选中组件的查询条件值
```

**Step 2: Add helper functions for query conditions**

After the `handleReportChange` function, add these helper functions:

```javascript
// 获取字段键名（用于表单绑定）
const getFieldKey = (qc) => {
  return `${qc.fieldName}_${qc.operator}`
}

// 获取操作符标签
const getOperatorLabel = (operator) => {
  const labels = {
    'eq': '等于',
    'ne': '不等于',
    'gt': '大于',
    'lt': '小于',
    'ge': '大于等于',
    'le': '小于等于',
    'like': '包含',
    'between': '介于',
    'null': '为空',
    'notnull': '不为空',
    'true': '为真',
    'false': '为假'
  }
  return labels[operator] || operator
}

// 获取操作符占位符
const getOperatorPlaceholder = (operator) => {
  const labels = {
    'eq': '请输入等于的值',
    'ne': '请输入不等于的值',
    'gt': '请输入最小值（不含）',
    'lt': '请输入最大值（不含）',
    'ge': '请输入最小值',
    'le': '请输入最大值',
    'like': '请输入包含的关键字',
    'between': '请输入范围值'
  }
  return labels[operator] || '请输入值'
}
```

**Step 3: Modify handleReportChange to fetch query conditions**

Replace the existing `handleReportChange` function with:

```javascript
// 报表变更时加载字段、查询条件和预览数据
const handleReportChange = async (reportId) => {
  if (!reportId) {
    availableFields.value = []
    widgetQueryConditions.value = []
    queryConditionValues.value = {}
    return
  }

  try {
    // 获取报表详情
    const res = await reportApi.getReport(reportId)
    if (res.success) {
      const reportData = res.data
      const columns = reportData.Columns || reportData.columns || []
      availableFields.value = columns.map(col => col.FieldName || col.fieldName)

      // 获取查询条件
      const conditions = reportData.QueryConditions || reportData.queryConditions || []
      widgetQueryConditions.value = conditions.map(qc => ({
        fieldName: qc.FieldName || qc.fieldName,
        displayName: qc.DisplayName || qc.displayName || qc.FieldName || qc.fieldName,
        dataType: qc.DataType || qc.dataType || 'String',
        operator: qc.Operator || qc.operator || 'eq',
        defaultValue: qc.DefaultValue || qc.defaultValue
      }))

      // 初始化条件值（使用默认值）
      queryConditionValues.value = {}
      widgetQueryConditions.value.forEach(qc => {
        if (!['null', 'notnull', 'true', 'false'].includes(qc.operator)) {
          if (qc.defaultValue) {
            queryConditionValues.value[getFieldKey(qc)] = qc.defaultValue
          }
        }
      })

      // 存储列信息到组件配置中
      if (selectedWidget.value) {
        selectedWidget.value.config = selectedWidget.value.config || {}
        selectedWidget.value.config.columns = columns.map(col => ({
          field: col.FieldName || col.fieldName,
          label: col.DisplayName || col.displayName || col.FieldName || col.fieldName,
          width: col.Width || col.width || 100
        }))
      }
    }

    // 获取报表预览数据（最多显示 10 条）
    const dataRes = await reportApi.executeReport(reportId, { pageSize: 10 })
    if (dataRes.success && dataRes.data) {
      widgetDataMap.value[reportId] = dataRes.data.slice(0, 10)
    }
  } catch (error) {
    console.error('加载报表信息失败:', error)
    ElMessage.error('加载报表信息失败')
  }
}
```

**Step 4: Verify no syntax errors**

Run: `cd H:/DataForge/frontend && npm run build`
Expected: Build completes without errors (warnings are acceptable)

**Step 5: Commit**

```bash
cd H:/DataForge
git add frontend/src/views/dashboard/DashboardDesigner.vue
git commit -m "$(cat <<'EOF'
feat(frontend): add query conditions state and fetch logic

- Add widgetQueryConditions and queryConditionValues state
- Add helper functions: getFieldKey, getOperatorLabel, getOperatorPlaceholder
- Modify handleReportChange to fetch and initialize query conditions

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Frontend - Add Query Conditions UI

**Files:**
- Modify: `H:\DataForge\frontend\src\views\dashboard\DashboardDesigner.vue`

**Step 1: Add query conditions UI after report selection**

Find the line `</el-form-item>` after the report selection (around line 280-281), and add the following after it:

```vue
                <!-- 查询条件配置 (绑定报表后显示) -->
                <template v-if="widgetQueryConditions.length > 0">
                  <el-divider>查询条件</el-divider>
                  <div class="query-conditions-config">
                    <div v-for="qc in widgetQueryConditions" :key="qc.fieldName + qc.operator" class="condition-item">
                      <el-form-item :label="qc.displayName">
                        <!-- 不需要输入值的操作符 -->
                        <template v-if="['null', 'notnull', 'true', 'false'].includes(qc.operator)">
                          <span class="operator-label">{{ getOperatorLabel(qc.operator) }}</span>
                        </template>

                        <!-- DateTime between: 日期范围选择器 -->
                        <template v-else-if="qc.operator === 'between' && qc.dataType === 'DateTime'">
                          <el-date-picker
                            v-model="queryConditionValues[getFieldKey(qc)]"
                            type="daterange"
                            range-separator="至"
                            start-placeholder="开始日期"
                            end-placeholder="结束日期"
                            value-format="YYYY-MM-DD"
                            style="width: 100%;"
                          />
                        </template>

                        <!-- Number between: 两个数字输入框 -->
                        <template v-else-if="qc.operator === 'between' && qc.dataType === 'Number'">
                          <div class="number-range-input">
                            <el-input-number
                              v-model="queryConditionValues[getFieldKey(qc) + '_start']"
                              placeholder="最小值"
                              :controls-position="'right'"
                              style="flex: 1;"
                            />
                            <span style="margin: 0 8px;">~</span>
                            <el-input-number
                              v-model="queryConditionValues[getFieldKey(qc) + '_end']"
                              placeholder="最大值"
                              :controls-position="'right'"
                              style="flex: 1;"
                            />
                          </div>
                        </template>

                        <!-- String 类型 -->
                        <template v-else-if="qc.dataType === 'String'">
                          <el-input
                            v-model="queryConditionValues[getFieldKey(qc)]"
                            :placeholder="getOperatorPlaceholder(qc.operator)"
                            clearable
                          />
                        </template>

                        <!-- Number 类型 -->
                        <template v-else-if="qc.dataType === 'Number'">
                          <el-input-number
                            v-model="queryConditionValues[getFieldKey(qc)]"
                            :placeholder="getOperatorPlaceholder(qc.operator)"
                            :controls-position="'right'"
                            style="width: 100%;"
                          />
                        </template>

                        <!-- DateTime 类型 -->
                        <template v-else-if="qc.dataType === 'DateTime'">
                          <el-date-picker
                            v-model="queryConditionValues[getFieldKey(qc)]"
                            type="date"
                            :placeholder="getOperatorPlaceholder(qc.operator)"
                            value-format="YYYY-MM-DD"
                            style="width: 100%;"
                          />
                        </template>

                        <!-- Boolean 类型 -->
                        <template v-else-if="qc.dataType === 'Boolean'">
                          <el-select
                            v-model="queryConditionValues[getFieldKey(qc)]"
                            placeholder="请选择"
                            clearable
                            style="width: 100%;"
                          >
                            <el-option label="是" :value="true" />
                            <el-option label="否" :value="false" />
                          </el-select>
                        </template>
                      </el-form-item>
                    </div>
                  </div>
                </template>
```

**Step 2: Add styles for query conditions**

In the `<style scoped>` section, add:

```css
/* 查询条件配置样式 */
.query-conditions-config {
  max-height: 300px;
  overflow-y: auto;
}

.query-conditions-config .condition-item {
  margin-bottom: 8px;
}

.query-conditions-config .operator-label {
  color: #909399;
  font-size: 12px;
}

.query-conditions-config .number-range-input {
  display: flex;
  align-items: center;
}
```

**Step 3: Verify build**

Run: `cd H:/DataForge/frontend && npm run build`
Expected: Build completes without errors

**Step 4: Commit**

```bash
cd H:/DataForge
git add frontend/src/views/dashboard/DashboardDesigner.vue
git commit -m "$(cat <<'EOF'
feat(frontend): add query conditions UI in widget config

- Add query condition input controls for all data types
- Support operators: eq, between, like, null, etc.
- Add styles for query conditions config panel

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Frontend - Store Query Conditions in Widget Config

**Files:**
- Modify: `H:\DataForge\frontend\src\views\dashboard\DashboardDesigner.vue`

**Step 1: Add watch to sync queryConditionValues to selectedWidget**

Add a watcher to sync query condition values to the widget's config. Add this after the existing watchers:

```javascript
// 同步查询条件值到组件配置
watch(queryConditionValues, (newValues) => {
  if (selectedWidget.value) {
    selectedWidget.value.config = selectedWidget.value.config || {}
    selectedWidget.value.config.queryConditionValues = { ...newValues }
  }
}, { deep: true })
```

**Step 2: Load existing query conditions when selecting a widget**

Modify the `handleSelectWidget` function to load existing query conditions. Find the existing function and update it:

```javascript
// 选择组件
const handleSelectWidget = (item) => {
  selectedWidgetId.value = item.i
  selectedWidget.value = layout.value.find(w => w.i === item.i)
  activeTab.value = 'widget'

  // 加载已保存的查询条件值
  if (selectedWidget.value?.config?.queryConditionValues) {
    queryConditionValues.value = { ...selectedWidget.value.config.queryConditionValues }
  } else {
    queryConditionValues.value = {}
  }

  // 如果有报表ID，加载查询条件定义
  if (selectedWidget.value?.reportId) {
    loadQueryConditionsForWidget(selectedWidget.value.reportId)
  }
}

// 加载组件的查询条件定义
const loadQueryConditionsForWidget = async (reportId) => {
  try {
    const res = await reportApi.getReport(reportId)
    if (res.success) {
      const reportData = res.data
      const conditions = reportData.QueryConditions || reportData.queryConditions || []
      widgetQueryConditions.value = conditions.map(qc => ({
        fieldName: qc.FieldName || qc.fieldName,
        displayName: qc.DisplayName || qc.displayName || qc.FieldName || qc.fieldName,
        dataType: qc.DataType || qc.dataType || 'String',
        operator: qc.Operator || qc.operator || 'eq',
        defaultValue: qc.DefaultValue || qc.defaultValue
      }))
    }
  } catch (error) {
    console.error('加载查询条件失败:', error)
  }
}
```

**Step 3: Reset query conditions when deselecting**

Add logic to reset query conditions when no widget is selected. Modify the watch for `selectedWidgetId` if it exists, or add:

```javascript
// 组件选择变化时重置查询条件
watch(selectedWidgetId, (newId) => {
  if (!newId) {
    widgetQueryConditions.value = []
    queryConditionValues.value = {}
  }
})
```

**Step 4: Verify build**

Run: `cd H:/DataForge/frontend && npm run build`
Expected: Build completes without errors

**Step 5: Commit**

```bash
cd H:/DataForge
git add frontend/src/views/dashboard/DashboardDesigner.vue
git commit -m "$(cat <<'EOF'
feat(frontend): store and load query conditions in widget config

- Add watcher to sync queryConditionValues to selectedWidget.config
- Load existing conditions when selecting a widget
- Reset conditions when deselecting widget

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Frontend - Update Save Logic to Include Query Conditions

**Files:**
- Modify: `H:\DataForge\frontend\src\views\dashboard\DashboardDesigner.vue`

**Step 1: Ensure queryConditionValues is saved in DataConfig**

Find the save widget function (likely `handleSaveWidget` or similar code within `handleSave`). Ensure the widget's DataConfig includes queryConditionValues. Look for where the widget data is prepared for saving:

```javascript
// 在保存组件时，确保 queryConditionValues 被包含在 config 中
const prepareWidgetForSave = (widget) => {
  return {
    ...widget,
    config: {
      ...widget.config,
      queryConditionValues: widget.config?.queryConditionValues || {}
    }
  }
}
```

If the save logic directly uses the widget object, verify that `queryConditionValues` in `config` is preserved when saving. The watcher from Task 4 should already handle this, but double-check the save function.

**Step 2: Verify DataConfig is serialized correctly**

Ensure that when the widget is saved, the `config` object (including `queryConditionValues`) is properly serialized. Check the API call that saves widgets and ensure the config is included.

**Step 3: Verify build**

Run: `cd H:/DataForge/frontend && npm run build`
Expected: Build completes without errors

**Step 4: Commit**

```bash
cd H:/DataForge
git add frontend/src/views/dashboard/DashboardDesigner.vue
git commit -m "$(cat <<'EOF'
feat(frontend): ensure queryConditionValues saved in widget DataConfig

- Verify queryConditionValues is included when saving widget
- DataConfig JSON will contain queryConditionValues for backend

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Integration Testing

**Files:**
- Test: Manual testing in browser

**Step 1: Start development servers**

Run backend:
```bash
cd H:/DataForge && dotnet run --project backend/src/DataForgeStudio.Api
```

Run frontend (in new terminal):
```bash
cd H:/DataForge/frontend && npm run dev
```

**Step 2: Test query conditions flow**

1. Create a new dashboard or open existing one
2. Add a widget and select a report that has query conditions
3. Verify query conditions appear in the config panel
4. Fill in query condition values
5. Save the dashboard
6. Refresh the page
7. Select the widget again - verify condition values are preserved
8. Preview the dashboard - verify data is filtered correctly

**Step 3: Test different condition types**

Test each condition type:
- [ ] String with eq operator
- [ ] Number with between operator
- [ ] DateTime with between operator
- [ ] Boolean select

**Step 4: Commit test results**

```bash
cd H:/DataForge
git add -A
git commit -m "$(cat <<'EOF'
test: verify query conditions flow works end-to-end

Manual testing completed:
- Query conditions display correctly
- Values save and load properly
- Backend receives and applies conditions

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## Summary

This implementation enables dashboard widgets to:

1. **Display query conditions** - When a report is selected, its query conditions appear in the widget config panel
2. **Accept condition values** - Users can fill in condition values using appropriate input controls
3. **Store condition values** - Values are saved in the widget's `config.queryConditionValues`
4. **Apply conditions** - Backend parses `DataConfig` and passes conditions to `ExecuteReportAsync`

**Data flow:**
```
Design mode:
1. Select report → fetch queryConditions
2. Render condition inputs → user fills values
3. Watch syncs values to selectedWidget.config.queryConditionValues
4. Save → DataConfig JSON includes queryConditionValues

View mode:
1. Load dashboard → read widget DataConfig
2. GetWidgetDataAsync → parse DataConfig.queryConditionValues
3. ExecuteReportAsync(reportId, { Parameters: conditionValues })
4. Return filtered data
```
