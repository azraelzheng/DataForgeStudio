# 报表管理系统重构实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 重构报表管理系统，分离报表查询和报表设计功能，增强字段类型自动匹配和查询条件配置

**Architecture:** 前端使用 Vue 3 + Element Plus，后端使用 ASP.NET Core 8.0 + EF Core，数据使用 SQL Server

**Tech Stack:** Vue 3, Element Plus, ASP.NET Core 8.0, Entity Framework Core, SQL Server, ClosedXML, QuestPDF

---

## 需求概述

### 1. 报表查询页面
- **功能定位**: 纯查询页面，只能调用已设计好的报表
- **核心功能**: 选择报表、输入查询条件、查询数据、导出数据
- **无权限**: 编辑、删除、停用报表功能

### 2. 报表设计页面
- **功能定位**: 报表设计管理页面
- **核心功能**: 新建/编辑报表、字段配置、参数配置、图表配置、报表列表管理
- **权限**: 编辑、删除、停用报表

### 3. 字段类型自动匹配
- **数据库字段类型 → 系统字段类型映射**:
  - varchar/nvarchar → 字符串
  - int/decimal/numeric → 数值
  - datetime/date → 日期
  - bit → 布尔
- **用途**: 用于查询条件设置时的控件类型

### 4. 查询条件配置
- **可选字段**: 选择哪些字段作为查询条件
- **条件关系**:
  - 等于 (=)
  - 不等于 (<> 或 !=)
  - 大于 (>)
  - 小于 (<)
  - 大于等于 (>=)
  - 小于等于 (<=)
  - 为空 (IS NULL)
  - 不为空 (IS NOT NULL)
  - 包含 (LIKE)
  - 在范围内 (IN)
- **输入控件**: 根据字段类型自动匹配（文本框、日期选择器、下拉框等）

---

## 任务分解

### Task 1: 重构前端路由和菜单结构

**目标**: 分离报表查询和报表设计为两个独立页面

**Files:**
- Modify: `frontend/src/router/index.js`
- Modify: `frontend/src/router/menu.js` (如果存在)
- Modify: `frontend/src/layout/components/Sidebar.vue` (如果存在)

**Step 1: 修改路由配置，分离报表查询和报表设计**

打开 `frontend/src/router/index.js`，找到报表相关路由，修改为：

```javascript
// 报表查询 - 纯查询页面
{
  path: '/report',
  name: 'ReportQuery',
  component: () => import('../views/report/ReportQuery.vue'),
  meta: { title: '报表查询', requiresAuth: true, permission: 'report:execute' }
},
// 报表设计 - 设计管理页面
{
  path: '/report/design',
  name: 'ReportDesignList',
  component: () => import('../views/report/ReportDesignList.vue'),
  meta: { title: '报表设计', requiresAuth: true, permission: 'report:design' }
},
// 报表设计器 - 实际设计页面
{
  path: '/report/design/:id?',
  name: 'ReportDesigner',
  component: () => import('../views/report/ReportDesigner.vue'),
  meta: { title: '报表设计器', requiresAuth: true, permission: 'report:design' }
},
```

**Step 2: 重命名现有文件**

```bash
# 在 frontend 目录执行
mv src/views/report/ReportList.vue src/views/report/ReportQuery.vue
mv src/views/report/ReportDesign.vue src/views/report/ReportDesigner.vue
```

**Step 3: 提交路由修改**

```bash
git add frontend/src/router/index.js
git commit -m "refactor: 分离报表查询和报表设计路由

- 报表查询: /report (ReportQuery.vue)
- 报表设计列表: /report/design (ReportDesignList.vue)
- 报表设计器: /report/design/:id (ReportDesigner.vue)
"
```

---

### Task 2: 创建报表设计列表页面 (ReportDesignList.vue)

**目标**: 创建报表设计管理页面，显示已设计报表列表，支持编辑、删除、停用

**Files:**
- Create: `frontend/src/views/report/ReportDesignList.vue`

**Step 1: 创建页面基本结构**

创建 `frontend/src/views/report/ReportDesignList.vue`:

```vue
<template>
  <div class="report-design-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>报表设计管理</span>
          <el-button type="primary" @click="handleCreate">
            <el-icon><Plus /></el-icon>
            新建报表
          </el-button>
        </div>
      </template>

      <!-- 搜索表单 -->
      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="报表名称">
          <el-input v-model="searchForm.reportName" placeholder="请输入报表名称" clearable />
        </el-form-item>
        <el-form-item label="分类">
          <el-select v-model="searchForm.category" placeholder="请选择分类" clearable>
            <el-option label="全部" value="" />
            <el-option label="销售" value="销售" />
            <el-option label="库存" value="库存" />
            <el-option label="财务" value="财务" />
            <el-option label="其他" value="其他" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.isEnabled" placeholder="请选择状态" clearable>
            <el-option label="全部" value="" />
            <el-option label="启用" :value="true" />
            <el-option label="停用" :value="false" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>
            查询
          </el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <!-- 报表表格 -->
      <el-table :data="tableData" v-loading="loading" border stripe>
        <el-table-column prop="reportName" label="报表名称" width="200" />
        <el-table-column prop="reportCategory" label="分类" width="100" />
        <el-table-column prop="dataSourceName" label="数据源" width="150" />
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isEnabled ? 'success' : 'danger'" size="small">
              {{ row.isEnabled ? '启用' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="viewCount" label="查看次数" width="100" align="center" />
        <el-table-column prop="lastViewTime" label="最后查看" width="180" />
        <el-table-column prop="createdTime" label="创建时间" width="180" />
        <el-table-column label="操作" width="280" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button
              type="warning"
              link
              size="small"
              @click="handleToggleStatus(row)"
            >
              <el-icon><Switch /></el-icon>
              {{ row.isEnabled ? '停用' : '启用' }}
            </el-button>
            <el-button type="success" link size="small" @click="handleCopy(row)">
              <el-icon><DocumentCopy /></el-icon>
              复制
            </el-button>
            <el-button type="danger" link size="small" @click="handleDelete(row)">
              <el-icon><Delete /></el-icon>
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <el-pagination
        v-model:current-page="pagination.page"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[10, 20, 50, 100]"
        :total="pagination.total"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="loadData"
        @current-change="loadData"
        style="margin-top: 20px; justify-content: flex-end;"
      />
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { reportApi } from '../../api/request'

const router = useRouter()
const loading = ref(false)
const tableData = ref([])

const searchForm = reactive({
  reportName: '',
  category: '',
  isEnabled: ''
})

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

onMounted(() => {
  loadData()
})

const loadData = async () => {
  loading.value = true
  try {
    const res = await reportApi.getReports({
      page: pagination.page,
      pageSize: pagination.pageSize,
      ...searchForm
    })
    if (res.success) {
      const data = res.data
      tableData.value = data.Items || data.items || []
      pagination.total = data.TotalCount || data.total || 0
    }
  } catch (error) {
    console.error('加载数据失败:', error)
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  pagination.page = 1
  loadData()
}

const handleReset = () => {
  searchForm.reportName = ''
  searchForm.category = ''
  searchForm.isEnabled = ''
  handleSearch()
}

const handleCreate = () => {
  router.push('/report/design')
}

const handleEdit = (row) => {
  router.push(`/report/design?id=${row.reportId}`)
}

const handleToggleStatus = async (row) => {
  try {
    await reportApi.updateReport(row.reportId, {
      isEnabled: !row.isEnabled
    })
    ElMessage.success('状态更新成功')
    loadData()
  } catch (error) {
    console.error('更新状态失败:', error)
  }
}

const handleCopy = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要复制报表"${row.reportName}"吗？`, '确认复制', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'info'
    })

    const res = await reportApi.createReport({
      reportName: `${row.reportName} (副本)`,
      reportCategory: row.reportCategory,
      dataSourceId: row.dataSourceId,
      sqlQuery: row.sqlQuery,
      description: row.description,
      isEnabled: false,
      // 复制字段和参数配置
      columns: row.columns || [],
      parameters: row.parameters || [],
      chartConfig: row.chartConfig,
      enableChart: row.enableChart
    })

    ElMessage.success('复制成功，请在编辑页面完善副本报表')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('复制失败:', error)
    }
  }
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要删除报表"${row.reportName}"吗？此操作不可恢复！`, '确认删除', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await reportApi.deleteReport(row.reportId)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}
</script>

<style scoped>
.report-design-list {
  height: 100%;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
}
</style>
```

**Step 2: 提交新文件**

```bash
git add frontend/src/views/report/ReportDesignList.vue
git commit -m "feat: 创建报表设计列表页面

- 显示已设计报表列表
- 支持编辑、删除、停用、复制报表
- 支持按名称、分类、状态筛选
"
```

---

### Task 3: 重构报表查询页面 (ReportQuery.vue)

**目标**: 简化报表列表页面为纯查询页面，移除编辑删除功能

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: 移除编辑和删除按钮**

在 `frontend/src/views/report/ReportQuery.vue` 中找到操作列，移除编辑和删除按钮，只保留查询和导出按钮：

```vue
<!-- 修改前 -->
<el-table-column label="操作" width="250" fixed="right">
  <template #default="{ row }">
    <el-button type="primary" link size="small" @click="handleQuery(row)">
      <el-icon><Search /></el-icon>
      查询
    </el-button>
    <el-button type="primary" link size="small" @click="handleEdit(row)">
      <el-icon><Edit /></el-icon>
      编辑
    </el-button>
    <el-button type="success" link size="small" @click="handleExport(row)">
      <el-icon><Download /></el-icon>
      导出
    </el-button>
    <el-button type="danger" link size="small" @click="handleDelete(row)">
      <el-icon><Delete /></el-icon>
      删除
    </el-button>
  </template>
</el-table-column>

<!-- 修改后 -->
<el-table-column label="操作" width="180" fixed="right">
  <template #default="{ row }">
    <el-button type="primary" link size="small" @click="handleQuery(row)">
      <el-icon><Search /></el-icon>
      查询
    </el-button>
    <el-button type="success" link size="small" @click="handleExport(row)">
      <el-icon><Download /></el-icon>
      导出
    </el-button>
  </template>
</el-table-column>
```

**Step 2: 移除编辑和删除方法**

删除 `handleEdit` 和 `handleDelete` 方法：

```javascript
// 删除这些方法
const handleEdit = (row) => { ... }  // 删除
const handleDelete = async (row) => { ... }  // 删除
```

**Step 3: 更新页面标题和描述**

修改页面标题为"报表查询"：

```vue
<template #header>
  <div class="card-header">
    <span>报表查询</span>
    <!-- 移除任何新增按钮 -->
  </div>
</template>
```

**Step 4: 提交修改**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "refactor: 简化报表查询页面为纯查询功能

- 移除编辑和删除按钮，只保留查询和导出
- 更新页面标题为'报表查询'
- 移除 handleEdit 和 handleDelete 方法
"
```

---

### Task 4: 扩展后端 Report 实体，支持图表配置

**目标**: 确保图表配置能正确保存和加载

**Files:**
- Modify: `backend/src/DataForgeStudio.Domain/Entities/Report.cs`
- Modify: `backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs`

**Step 1: 在 Report 实体中添加图表字段**

打开 `backend/src/DataForgeStudio.Domain/Entities/Report.cs`，添加以下属性：

```csharp
/// <summary>
/// 图表配置
/// </summary>
public string? ChartConfig { get; set; }

/// <summary>
/// 是否启用图表
/// </summary>
public bool EnableChart { get; set; }
```

**Step 2: 更新 DbContext 配置**

在 `backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs` 的 OnModelCreating 方法中配置（如果需要）：

```csharp
model.Entity<Report>(entity =>
{
    entity.Property(e => e.ChartConfig).HasMaxLength(2000);
    entity.Property(e => e.EnableChart).IsRequired();
});
```

**Step 3: 创建数据库迁移**

```bash
cd backend/src/DataForgeStudio.Api
dotnet ef migrations add AddChartConfigToReport --project ../DataForgeStudio.Data
dotnet ef database update --project ../DataForgeStudio.Data
```

**Step 4: 提交修改**

```bash
git add backend/src/DataForgeStudio.Domain/Entities/Report.cs
git add backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs
git add backend/src/DataForgeStudio.Api/Migrations/*.cs
git commit -m "feat: 添加报表图表配置支持

- Report 实体添加 ChartConfig 和 EnableChart 字段
- 更新 DbContext 配置
- 添加数据库迁移
"
```

---

### Task 5: 创建查询条件配置组件

**目标**: 创建可配置的查询条件组件，支持多种条件关系

**Files:**
- Create: `frontend/src/components/QueryConditions.vue`

**Step 1: 创建查询条件组件**

创建 `frontend/src/components/QueryConditions.vue`:

```vue
<template>
  <div class="query-conditions">
    <el-form :model="form" label-width="100px">
      <el-row :gutter="20">
        <el-col :span="8" v-for="(field, index) in fields" :key="index">
          <el-form-item :label="field.displayName">
            <!-- 条件关系选择器 -->
            <el-select
              v-model="field.operator"
              placeholder="条件"
              style="width: 100px; margin-right: 8px;"
            >
              <el-option label="等于" value="eq" />
              <el-option label="不等于" value="ne" />
              <el-option label="大于" value="gt" />
              <el-option label="小于" value="lt" />
              <el-option label="大于等于" value="ge" />
              <el-option label="小于等于" value="le" />
              <el-option label="为空" value="null" />
              <el-option label="不为空" value="notnull" />
              <el-option label="包含" value="like" />
            </el-select>

            <!-- 值输入控件 -->
            <template v-if="field.operator !== 'null' && field.operator !== 'notnull'">
              <!-- 字符串类型：文本输入框 -->
              <el-input
                v-if="field.dataType === 'String'"
                v-model="field.value"
                :placeholder="`请输入${field.displayName}`"
                clearable
                style="flex: 1;"
              />

              <!-- 数值类型：数字输入框 -->
              <el-input-number
                v-else-if="field.dataType === 'Number'"
                v-model="field.value"
                :placeholder="`请输入${field.displayName}`"
                :controls-position="'right'"
                style="flex: 1;"
              />

              <!-- 日期类型：日期选择器 -->
              <el-date-picker
                v-else-if="field.dataType === 'DateTime'"
                v-model="field.value"
                type="date"
                :placeholder="`请选择${field.displayName}`"
                value-format="YYYY-MM-DD"
                style="flex: 1;"
              />

              <!-- 布尔类型：下拉选择 -->
              <el-select
                v-else-if="field.dataType === 'Boolean'"
                v-model="field.value"
                :placeholder="`请选择${field.displayName}`"
                clearable
                style="flex: 1;"
              >
                <el-option label="是" :value="true" />
                <el-option label="否" :value="false" />
              </el-select>
            </template>

            <!-- 删除条件按钮 -->
            <el-button
              type="danger"
              link
              size="small"
              @click="removeField(index)"
              style="margin-left: 8px;"
            >
              <el-icon><Delete /></el-icon>
            </el-button>
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>

    <!-- 添加条件按钮 -->
    <el-button type="primary" link @click="showFieldSelector">
      <el-icon><Plus /></el-icon>
      添加查询条件
    </el-button>

    <!-- 字段选择器对话框 -->
    <el-dialog v-model="fieldSelectorVisible" title="选择查询字段" width="500px">
      <el-checkbox-group v-model="selectedFieldNames">
        <el-checkbox
          v-for="field in availableFields"
          :key="field.fieldName"
          :label="field.fieldName"
        >
          {{ field.displayName }}
        </el-checkbox>
      </el-checkbox-group>
      <template #footer>
        <el-button @click="fieldSelectorVisible = false">取消</el-button>
        <el-button type="primary" @click="addSelectedFields">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'

const props = defineProps({
  // 所有可用的字段（从报表字段配置中获取）
  fields: {
    type: Array,
    default: () => []
  },
  // 模型值（v-model）
  modelValue: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['update:modelValue'])

// 当前已选择的查询条件
const form = reactive({
  conditions: []
})

const fieldSelectorVisible = ref(false)
const selectedFieldNames = ref([])

// 可用的字段（未添加为查询条件的字段）
const availableFields = computed(() => {
  const selectedNames = props.modelValue.map(c => c.fieldName)
  return props.fields.filter(f => !selectedNames.includes(f.fieldName))
})

const showFieldSelector = () => {
  selectedFieldNames.value = []
  fieldSelectorVisible.value = true
}

const addSelectedFields = () => {
  selectedFieldNames.value.forEach(fieldName => {
    const field = props.fields.find(f => f.fieldName === fieldName)
    if (field && !props.modelValue.find(c => c.fieldName === fieldName)) {
      props.modelValue.push({
        fieldName: field.fieldName,
        displayName: field.displayName,
        dataType: mapDataType(field.dataType),
        operator: 'eq',
        value: ''
      })
    }
  })
  fieldSelectorVisible.value = false
}

const removeField = (index) => {
  props.modelValue.splice(index, 1)
}

// 数据库类型到系统类型的映射
const mapDataType = (dbType) => {
  // 映射 SQL Server 类型到系统类型
  const typeMap = {
    // 字符串类型
    'varchar': 'String',
    'nvarchar': 'String',
    'char': 'String',
    'nchar': 'String',
    'text': 'String',
    'ntext': 'String',

    // 数值类型
    'int': 'Number',
    'bigint': 'Number',
    'smallint': 'Number',
    'tinyint': 'Number',
    'decimal': 'Number',
    'numeric': 'Number',
    'float': 'Number',
    'real': 'Number',

    // 日期类型
    'datetime': 'DateTime',
    'date': 'DateTime',
    'datetime2': 'DateTime',
    'smalldatetime': 'DateTime',
    'datetimeoffset': 'DateTime',

    // 布尔类型
    'bit': 'Boolean'
  }

  const type = dbType?.toLowerCase() || ''
  return typeMap[type] || 'String'
}

// 暴露给父组件的方法
defineExpose({
  getConditions: () => form.conditions,
  reset: () => {
    props.modelValue.splice(0)
  }
})
</script>

<style scoped>
.query-conditions {
  margin-bottom: 20px;
}
</style>
```

**Step 2: 提交新组件**

```bash
git add frontend/src/components/QueryConditions.vue
git commit -m "feat: 创建查询条件配置组件

- 支持多种条件关系（等于、不等于、大于、小于、为空等）
- 根据字段类型自动匹配输入控件
- 支持动态添加/删除查询条件
"
```

---

### Task 6: 实现字段类型自动匹配服务

**目标**: 后端提供 API 获取表结构时自动映射字段类型

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/DatabaseService.cs`
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs`

**Step 1: 扩展 GetTableStructureAsync 方法返回字段类型**

修改 `backend/src/DataForgeStudio.Core/Services/DatabaseService.cs` 中的 GetTableStructureAsync 方法，确保返回完整的字段类型信息：

```csharp
public async Task<ApiResponse<List<TableColumnDto>>> GetTableStructureAsync(int dataSourceId, string tableName)
{
    var dataSource = await _context.DataSources.FindAsync(dataSourceId);
    if (dataSource == null)
    {
        return ApiResponse.Fail<List<TableColumnDto>>("数据源不存在", "DATASOURCE_NOT_FOUND");
    }

    var tableColumns = new List<TableColumnDto>();

    // 连接到数据库并获取表结构
    var connectionString = BuildConnectionString(dataSource);
    using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
    await connection.OpenAsync();

    var query = $@"
        SELECT
            c.COLUMN_NAME,
            c.DATA_TYPE,
            c.CHARACTER_MAXIMUM_LENGTH,
            c.IS_NULLABLE,
            c.COLUMNPROPERTY,
            c.ORDINAL_POSITION
        FROM INFORMATION_SCHEMA.COLUMNS c
        WHERE c.TABLE_NAME = @TableName
        ORDER BY c.ORDINAL_POSITION";

    using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
    command.Parameters.AddWithValue("@TableName", tableName);

    using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var column = new TableColumnDto
        {
            ColumnName = reader.GetString(0),
            DataType = reader.GetString(1),
            MaxLength = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
            IsNullable = reader.GetBoolean(3),
            ColumnProperty = reader.GetString(4),
            Position = reader.GetInt32(5)
        };
        tableColumns.Add(column);
    }

    return ApiResponse<List<TableColumnDto>>.Ok(tableColumns);
}
```

**Step 2: 在 TableColumnDto 中添加 SystemDataType 属性**

修改 `backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs` 中的 TableColumnDto 定义：

```csharp
public class TableColumnDto
{
    public string ColumnName { get; set; }
    public string DataType { get; set; }
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public string? ColumnProperty { get; set; }
    public int Position { get; set; }

    // 系统数据类型（用于前端查询条件）
    public string SystemDataType { get; set; } = "String";
}
```

**Step 3: 在 DatabaseService 中添加类型映射逻辑**

在 GetTableStructureAsync 方法中添加类型映射：

```csharp
while (await reader.ReadAsync())
{
    var column = new TableColumnDto
    {
        ColumnName = reader.GetString(0),
        DataType = reader.GetString(1),
        MaxLength = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
        IsNullable = reader.GetBoolean(3),
        ColumnProperty = reader.GetString(4),
        Position = reader.GetInt32(5),
        SystemDataType = MapSystemDataType(reader.GetString(1)) // 添加系统类型映射
    };
    tableColumns.Add(column);
}

// 添加辅助方法
private string MapSystemDataType(string sqlDataType)
{
    var type = sqlDataType?.ToLower() ?? "";

    if (type.Contains("char") || type.Contains("text"))
        return "String";
    if (type.Contains("int") || type.Contains("decimal") || type.Contains("numeric") || type.Contains("float") || type.Contains("real"))
        return "Number";
    if (type.Contains("date") || type.Contains("time"))
        return "DateTime";
    if (type.Contains("bit"))
        return "Boolean";

    return "String"; // 默认为字符串
}
```

**Step 4: 提交修改**

```bash
git add backend/src/DataForgeStudio.Core/Services/DatabaseService.cs
git add backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs
git commit -m "feat: 增强表结构查询返回系统数据类型

- TableColumnDto 添加 SystemDataType 属性
- 添加 MapSystemDataType 辅助方法
- 支持自动映射 SQL Server 类型到系统类型（String/Number/DateTime/Boolean）
"
```

---

### Task 7: 更新 ReportDesigner.vue 集成新功能

**目标**: 更新报表设计器页面，集成字段类型自动匹配和查询条件配置

**Files:**
- Modify: `frontend/src/views/report/ReportDesigner.vue`
- Modify: `frontend/src/components/SqlEditor.vue`

**Step 1: 在字段配置中显示系统数据类型**

修改 ReportDesigner.vue 的字段配置表格，添加系统数据类型列：

```vue
<el-table-column prop="dataType" label="系统类型" width="100">
  <template #default="{ row }">
    <el-tag size="small">{{ getSystemDataTypeLabel(row.dataType) }}</el-tag>
  </template>
</el-table-column>
```

添加辅助方法：

```javascript
const getSystemDataTypeLabel = (type) => {
  const map = {
    'String': '字符串',
    'Number': '数值',
    'DateTime': '日期',
    'Boolean': '布尔'
  }
  return map[type] || type
}
```

**Step 2: 集成查询条件配置组件**

在 ReportDesigner.vue 中导入并使用 QueryConditions 组件：

```vue
<template>
  <!-- 在参数配置卡片后添加查询条件配置卡片 -->
  <el-card class="design-card" style="margin-top: 20px;">
    <template #header>
      <div style="display: flex; justify-content: space-between; align-items: center;">
        <span>查询条件配置</span>
        <el-button type="primary" link size="small" @click="handleAutoGenerateConditions">
          <el-icon><MagicStick /></el-icon>
          自动生成
        </el-button>
      </div>
    </template>

    <QueryConditions
      ref="queryConditionsRef"
      v-model="form.queryConditions"
      :fields="form.columns"
    />
  </el-card>
</template>

<script setup>
import QueryConditions from '../../components/QueryConditions.vue'

const queryConditionsRef = ref()

const handleAutoGenerateConditions = () => {
  // 自动根据字段类型生成推荐的查询条件
  if (!form.columns || form.columns.length === 0) {
    ElMessage.warning('请先配置字段信息')
    return
  }

  // 默认为常用字段添加查询条件
  const commonFields = ['名称', '名称', '日期', '时间', '状态']
  form.queryConditions = []

  form.columns.forEach(col => {
    // 简单的启发式规则：名称、日期、状态类字段添加为查询条件
    if (commonFields.some(f => col.displayName.includes(f))) {
      form.queryConditions.push({
        fieldName: col.fieldName,
        displayName: col.displayName,
        dataType: col.dataType,
        operator: 'eq',
        value: ''
      })
    }
  })

  ElMessage.success(`已自动生成 ${form.queryConditions.length} 个查询条件`)
}
</script>
```

**Step 3: 提交修改**

```bash
git add frontend/src/views/report/ReportDesigner.vue
git commit -m "feat: 报表设计器集成查询条件配置

- 添加查询条件配置卡片
- 支持自动生成查询条件
- 字段配置显示系统数据类型标签
- 集成 QueryConditions 组件
"
```

---

### Task 8: 更新 ReportService 支持查询条件持久化

**目标**: 后端保存和加载查询条件配置

**Files:**
- Modify: `backend/src/DataForgeStudio.Domain/Entities/Report.cs`
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs`

**Step 1: 在 Report 实体中添加查询条件字段**

修改 `backend/src/DataForgeStudio.Domain/Entities/Report.cs`：

```csharp
/// <summary>
/// 查询条件配置（JSON）
/// </summary>
public string? QueryConditions { get; set; }
```

**Step 2: 更新 ReportDetailDto**

修改 `backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs` 中的 ReportDetailDto：

```csharp
public class ReportDetailDto : ReportDto
{
    public string SqlQuery { get; set; }
    public List<ReportFieldDto> Columns { get; set; }
    public List<ReportParameterDto> Parameters { get; set; }
    public bool EnableChart { get; set; }
    public ChartConfigDto? ChartConfig { get; set; }
    public List<QueryConditionDto>? QueryConditions { get; set; }  // 新增
}

// 新增查询条件 DTO
public class QueryConditionDto
{
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
    public string Operator { get; set; } = "eq";  // eq, ne, gt, lt, ge, le, null, notnull, like
    public string? Value { get; set; }
}
```

**Step 3: 更新 ReportService 保存逻辑**

在 CreateReportAsync 和 UpdateReportAsync 方法中保存查询条件：

```csharp
// 保存查询条件
if (request.QueryConditions != null && request.QueryConditions.Count > 0)
{
    report.QueryConditions = JsonSerializer.Serialize(request.QueryConditions);
}
```

**Step 4: 提交修改**

```bash
git add backend/src/DataForgeStudio.Domain/Entities/Report.cs
git add backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs
git add backend/src/DataForgeStudio.Core/Services/ReportService.cs
git commit -m "feat: 支持查询条件持久化

- Report 实体添加 QueryConditions 字段
- 添加 QueryConditionDto 定义
- 在 ReportService 中保存查询条件
"
```

---

### Task 9: 更新 ReportQuery.vue 集成查询条件

**目标**: 报表查询页面使用动态查询条件

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: 集成 QueryConditions 组件**

在 ReportQuery.vue 中添加查询条件部分：

```vue
<template>
  <div class="report-query">
    <el-card>
      <template #header>
        <span>报表查询</span>
      </template>

      <!-- 报表选择 -->
      <el-form :inline="true" @submit.prevent="handleSelectReport">
        <el-form-item label="选择报表">
          <el-select
            v-model="selectedReportId"
            placeholder="请选择报表"
            style="width: 300px;"
            @change="handleReportChange"
          >
            <el-option
              v-for="report in reports"
              :key="report.reportId"
              :label="report.reportName"
              :value="report.reportId"
            />
          </el-select>
        </el-form-item>
      </el-form>

      <!-- 查询条件（从报表配置中自动加载） -->
      <div v-if="selectedReport && queryConditions.length > 0" class="query-conditions-wrapper">
        <el-divider content-position="left">查询条件</el-divider>
        <QueryConditions
          ref="queryConditionsRef"
          v-model="queryConditions"
          :fields="queryConditionFields"
        />
      </div>

      <!-- 查询和导出按钮 -->
      <div style="margin-top: 20px;">
        <el-button
          type="primary"
          @click="handleQuery"
          :loading="querying"
          :disabled="!selectedReport"
        >
          <el-icon><Search /></el-icon>
          查询
        </el-button>
        <el-button
          type="success"
          @click="handleExport"
          :loading="exporting"
          :disabled="!selectedReport || !reportData || reportData.length === 0"
        >
          <el-icon><Download /></el-icon>
          导出
        </el-button>
      </div>

      <!-- 查询结果 -->
      <div v-if="reportData && reportData.length > 0" class="result-wrapper">
        <el-divider content-position="left">查询结果</el-divider>

        <!-- 数据表格 -->
        <el-table
          :data="reportData"
          border
          stripe
          :data="tableData"
          :max-height="500"
        >
          <el-table-column
            v-for="col in reportColumns"
            :key="col.fieldName"
            :prop="col.fieldName"
            :label="col.displayName"
            :width="col.width"
            :align="col.align"
          />
        </el-table>

        <!-- 图表 -->
        <div v-if="currentReport.enableChart && chartData" class="chart-wrapper">
          <el-divider content-position="left">图表</el-divider>
          <div ref="chartRef" style="height: 400px;"></div>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import * as echarts from 'echarts'
import { reportApi } from '../../api/request'
import QueryConditions from '../../components/QueryConditions.vue'

const selectedReportId = ref(null)
const reports = ref([])
const currentReport = ref(null)
const queryConditions = ref([])
const queryConditionFields = ref([])
const queryConditionsRef = ref()

const reportData = ref([])
const reportColumns = ref([])
const querying = ref(false)
const exporting = ref(false)
const tableData = ref([])

// 加载报表列表
onMounted(async () => {
  const res = await reportApi.getReports({ page: 1, pageSize: 1000 })
  if (res.success) {
    reports.value = res.data.Items || res.data.items || []
  }
})

const handleReportChange = async (reportId) => {
  const report = reports.value.find(r => r.reportId === reportId)
  if (!report) return

  // 加载报表详情，包括查询条件
  const res = await reportApi.getReport(reportId)
  if (res.success) {
    currentReport.value = res.data
    // 解析查询条件
    queryConditions.value = res.data.queryConditions || []
    // 设置查询条件字段
    queryConditionFields.value = queryConditions.value.map(qc => ({
      fieldName: qc.fieldName,
      displayName: qc.displayName,
      dataType: qc.dataType
    }))
  }
}

const handleQuery = async () => {
  // 构建查询参数
  const params = buildQueryParams()
  querying.value = true

  try {
    const res = await reportApi.executeReport(selectedReportId.value, params)
    if (res.success) {
      reportData.value = res.data
      reportColumns.value = currentReport.value.columns
      tableData.value = res.data

      // 渲染图表
      if (currentReport.value.enableChart) {
        await nextTick()
        renderChart()
      }
    }
  } catch (error) {
    console.error('查询失败:', error)
  } finally {
    querying.value = false
  }
}

const buildQueryParams = () => {
  const params = {}

  queryConditions.value.forEach(qc => {
    if (qc.value === '' || qc.value === null || qc.value === undefined) {
      return // 空值不添加到查询条件
    }

    // 根据操作符设置参数
    switch (qc.operator) {
      case 'eq':
        params[`${qc.fieldName}_eq`] = qc.value
        break
      case 'ne':
        params[`${qc.fieldName}_ne`] = qc.value
        break
      case 'gt':
        params[`${qc.fieldName}_gt`] = qc.value
        break
      case 'lt':
        params[`${qc.fieldName}_lt`] = qc.value
        break
      case 'ge':
        params[`${qc.fieldName}_ge`] = qc.value
        break
      case 'le':
        params[`${qc.fieldName}_le`] = qc.value
        break
      case 'null':
        params[`${qc.fieldName}_null`] = true
        break
      case 'notnull':
        params[`${qc.fieldName}_notnull`] = true
        break
      case 'like':
        params[`${qc.fieldName}_like`] = qc.value
        break
    }
  })

  return params
}
</script>
```

**Step 2: 提交修改**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: 报表查询页面集成动态查询条件

- 从报表详情加载查询条件配置
- 支持多种查询条件关系
- 根据字段类型自动匹配输入控件
- 条件参数构建逻辑
"
```

---

### Task 10: 创建数据库迁移脚本

**目标**: 添加查询条件相关字段的数据库迁移

**Files:**
- Create: `backend/src/DataForgeStudio.Api/Migrations/Report_QueryConditions_Migration.cs`

**Step 1: 生成迁移**

```bash
cd backend/src/DataForgeStudio.Api
dotnet ef migrations add AddQueryConditionsToReport --project ../DataForgeStudio.Data
```

**Step 2: 检查生成的迁移文件，确保字段定义正确**

**Step 3: 应用迁移**

```bash
dotnet ef database update --project ../DataForgeStudio.Data
```

**Step 4: 提交迁移文件**

```bash
git add backend/src/DataForgeStudio.Api/Migrations/
git commit -m "feat: 添加报表查询条件数据库迁移

- 添加 QueryConditions 字段到 Reports 表
- 更新 Report 实体和 DbContext
"
```

---

### Task 11: 权限定义更新

**目标**: 更新权限定义，分离报表查询和报表设计权限

**Files:**
- Modify: `backend/src/DataForgeStudio.Data/Data/DbInitializer.cs`

**Step 1: 在 DbInitializer.cs 中更新权限定义**

找到权限定义部分，添加新的权限：

```csharp
// 在 CreateAllPermissionsAsync 方法中添加：
new Permission { PermissionCode = "report:query", PermissionName = "查询报表", Module = "Report", Action = "Query", Description = "执行已创建的报表查询" },
new Permission { PermissionCode = "report:configCondition", PermissionName = "配置查询条件", Module = "Report", Action = "ConfigCondition", Description = "配置报表查询条件" },
```

**Step 2: 提交修改**

```bash
git add backend/src/DataForgeStudio.Data/Data/DbInitializer.cs
git commit -m "feat: 添加报表查询相关权限

- 添加 report:query 权限（查询报表）
- 添加 report:configCondition 权限（配置查询条件）
"
```

---

### Task 12: 测试和验证

**目标:** 测试整个重构后的报表管理系统功能

**Files:**
- Test: 手动测试所有功能

**Step 1: 启动服务并测试**

```bash
# 启动后端
cd backend/src/DataForgeStudio.Api && dotnet run

# 启动前端
cd frontend && npm run dev
```

**Step 2: 功能测试清单**

- [ ] 报表查询页面只显示查询和导出按钮
- [ ] 报表设计列表页面显示所有报表
- [ ] 新建报表后正确保存字段类型
- [ ] 编辑报表时正确加载所有配置
- [ ] 查询条件正确保存和加载
- [ ] 字段类型自动匹配正确
- [ ] 查询条件参数正确构建
- [ ] 图表配置正确保存和加载

**Step 3: 提交最终修改**

```bash
git add .
git commit -m "test: 完成报表管理系统重构

- 分离报表查询和报表设计功能
- 实现字段类型自动匹配
- 实现查询条件配置
- 所有功能测试通过
"
```

---

## 执行顺序

按顺序执行上述 12 个任务，每个任务执行完毕后应确保：
1. 代码编译通过
2. 功能正常工作
3. 提交代码到 git

完成所有任务后，刷新浏览器测试新功能。
