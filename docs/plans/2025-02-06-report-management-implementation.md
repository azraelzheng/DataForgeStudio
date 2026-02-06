# 报表管理系统重新设计实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 重构报表管理系统，分离报表查询和报表设计功能，实现字段类型自动映射和查询条件配置

**Architecture:** 前端 Vue 3 + Element Plus 左右分栏布局，后端 ASP.NET Core 8.0 + EF Core，数据使用 JSON 字段存储配置

**Tech Stack:** Vue 3, Element Plus, ECharts, ASP.NET Core 8.0, Entity Framework Core, SQL Server 2005+

---

## 目录

- [阶段一：基础架构重构](#阶段一基础架构重构)
- [阶段二：核心功能实现](#阶段二核心功能实现)
- [阶段三：增强功能](#阶段三增强功能)
- [阶段四：权限和测试](#阶段四权限和测试)

---

## 阶段一：基础架构重构

### Task 1: 修改前端路由配置

**Files:**
- Modify: `frontend/src/router/index.js`

**Step 1: 修改路由配置**

打开 `frontend/src/router/index.js`，找到报表相关路由，修改为：

```javascript
// 报表查询 - 纯查询页面
{
  path: '/report',
  name: 'ReportQuery',
  component: () => import('../views/report/ReportQuery.vue'),
  meta: { title: '报表查询', requiresAuth: true, permission: 'report:query' }
},
// 报表设计列表 - 设计管理页面
{
  path: '/report/design',
  name: 'ReportDesignList',
  component: () => import('../views/report/ReportDesignList.vue'),
  meta: { title: '报表设计', requiresAuth: true, permission: 'report:design' }
},
// 报表设计器 - 实际设计页面
{
  path: '/report/designer',
  name: 'ReportDesigner',
  component: () => import('../views/report/ReportDesigner.vue'),
  meta: { title: '报表设计器', requiresAuth: true, permission: 'report:design' }
},
```

**Step 2: 提交修改**

```bash
git add frontend/src/router/index.js
git commit -m "refactor: 分离报表查询和报表设计路由

- 报表查询: /report (ReportQuery.vue)
- 报表设计列表: /report/design (ReportDesignList.vue)
- 报表设计器: /report/designer (ReportDesigner.vue)
"
```

---

### Task 2: 重命名现有文件

**Files:**
- Rename: `frontend/src/views/report/ReportList.vue` → `frontend/src/views/report/ReportQuery.vue`
- Rename: `frontend/src/views/report/ReportDesign.vue` → `frontend/src/views/report/ReportDesigner.vue`

**Step 1: 重命名文件**

```bash
cd frontend/src/views/report
mv ReportList.vue ReportQuery.vue
mv ReportDesign.vue ReportDesigner.vue
```

**Step 2: 更新 ReportDesigner.vue 中的组件名称**

打开 `frontend/src/views/report/ReportDesigner.vue`，修改组件名称（如果需要）：

```vue
<script setup>
// 组件名称不需要特别修改，使用 <script setup> 语法糖即可
</script>
```

**Step 3: 提交重命名**

```bash
git add frontend/src/views/report/
git commit -m "refactor: 重命名报表相关组件

- ReportList.vue → ReportQuery.vue
- ReportDesign.vue → ReportDesigner.vue
"
```

---

### Task 3: 创建报表设计列表页面

**Files:**
- Create: `frontend/src/views/report/ReportDesignList.vue`

**Step 1: 创建报表设计列表页面**

创建 `frontend/src/views/report/ReportDesignList.vue`:

```vue
<template>
  <div class="report-design-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>报表设计管理</span>
          <div>
            <el-button type="primary" @click="handleExportConfig">
              <el-icon><Download /></el-icon>
              导出配置
            </el-button>
            <el-button type="primary" @click="handleCreate">
              <el-icon><Plus /></el-icon>
              新建报表
            </el-button>
          </div>
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
        <el-table-column prop="lastViewTime" label="最后查看" width="180">
          <template #default="{ row }">
            {{ row.lastViewTime ? formatDate(row.lastViewTime) : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="createdTime" label="创建时间" width="180" />
        <el-table-column label="操作" width="320" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button type="info" link size="small" @click="handlePreview(row)">
              <el-icon><View /></el-icon>
              预览
            </el-button>
            <el-button type="success" link size="small" @click="handleCopy(row)">
              <el-icon><DocumentCopy /></el-icon>
              复制
            </el-button>
            <el-button
              :type="row.isEnabled ? 'warning' : 'success'"
              link
              size="small"
              @click="handleToggleStatus(row)"
            >
              <el-icon><Switch /></el-icon>
              {{ row.isEnabled ? '停用' : '启用' }}
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

    <!-- 预览对话框 -->
    <el-dialog v-model="previewVisible" title="报表预览" width="90%" top="5vh">
      <div v-if="previewReport">
        <div class="report-info">
          <el-descriptions :column="2" border>
            <el-descriptions-item label="报表名称">{{ previewReport.reportName }}</el-descriptions-item>
            <el-descriptions-item label="分类">{{ previewReport.reportCategory }}</el-descriptions-item>
            <el-descriptions-item label="数据源">{{ previewReport.dataSourceName }}</el-descriptions-item>
            <el-descriptions-item label="描述">{{ previewReport.description || '-' }}</el-descriptions-item>
          </el-descriptions>
        </div>
        <el-divider>字段配置</el-divider>
        <el-table :data="previewReport.columns || []" border>
          <el-table-column prop="fieldName" label="字段名" width="150" />
          <el-table-column prop="displayName" label="显示名称" width="150" />
          <el-table-column prop="dataType" label="数据类型" width="100" />
          <el-table-column prop="width" label="宽度" width="80" />
        </el-table>
      </div>
    </el-dialog>
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
const previewVisible = ref(false)
const previewReport = ref(null)

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
      includeDisabled: true,
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
  router.push('/report/designer')
}

const handleEdit = (row) => {
  router.push(`/report/designer?id=${row.reportId}`)
}

const handlePreview = async (row) => {
  try {
    const res = await reportApi.getReport(row.reportId)
    if (res.success) {
      previewReport.value = res.data
      previewVisible.value = true
    }
  } catch (error) {
    console.error('加载报表失败:', error)
  }
}

const handleCopy = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要复制报表"${row.reportName}"吗？`, '确认复制', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'info'
    })

    const res = await reportApi.copyReport(row.reportId)
    if (res.success) {
      ElMessage.success('复制成功，请在编辑页面完善副本报表')
      loadData()
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('复制失败:', error)
    }
  }
}

const handleToggleStatus = async (row) => {
  try {
    await reportApi.toggleReport(row.reportId)
    ElMessage.success(row.isEnabled ? '已停用' : '已启用')
    loadData()
  } catch (error) {
    console.error('更新状态失败:', error)
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

const handleExportConfig = async () => {
  try {
    const res = await reportApi.exportAllConfigs()
    if (res.success) {
      const blob = new Blob([JSON.stringify(res.data, null, 2)], { type: 'application/json' })
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `report-configs-${Date.now()}.json`
      a.click()
      window.URL.revokeObjectURL(url)
      ElMessage.success('配置导出成功')
    }
  } catch (error) {
    console.error('导出配置失败:', error)
  }
}

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN')
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

.report-info {
  margin-bottom: 20px;
}
</style>
```

**Step 2: 提交新文件**

```bash
git add frontend/src/views/report/ReportDesignList.vue
git commit -m "feat: 创建报表设计列表页面

- 显示所有报表（包括停用的）
- 支持编辑、预览、复制、停用、删除操作
- 支持按名称、分类、状态筛选
- 添加导出配置功能
"
```

---

### Task 4: 数据库迁移 - 添加图表和查询条件字段

**Files:**
- Modify: `backend/src/DataForgeStudio.Domain/Entities/Report.cs`
- Create: `backend/src/DataForgeStudio.Data/Migrations/<timestamp>_AddReportEnhancements.cs`

**Step 1: 修改 Report 实体**

打开 `backend/src/DataForgeStudio.Domain/Entities/Report.cs`，添加以下字段：

```csharp
/// <summary>
/// 图表配置 (JSON)
/// </summary>
[MaxLength(2000)]
public string? ChartConfig { get; set; }

/// <summary>
/// 是否启用图表
/// </summary>
public bool EnableChart { get; set; } = false;

/// <summary>
/// 查询条件配置 (JSON)
/// </summary>
[MaxLength(2000)]
public string? QueryConditions { get; set; }
```

**Step 2: 创建数据库迁移**

```bash
cd backend/src/DataForgeStudio.Api
dotnet ef migrations add AddReportEnhancements --project ../DataForgeStudio.Data
```

**Step 3: 应用迁移**

```bash
dotnet ef database update --project ../DataForgeStudio.Data
```

**Step 4: 提交修改**

```bash
git add backend/src/DataForgeStudio.Domain/Entities/Report.cs
git add backend/src/DataForgeStudio.Data/Migrations/
git commit -m "feat: 添加报表增强字段

- 添加 ChartConfig 字段（图表配置JSON）
- 添加 EnableChart 字段（是否启用图表）
- 添加 QueryConditions 字段（查询条件配置JSON）
- 添加数据库迁移
"
```

---

## 阶段二：核心功能实现

### Task 5: 创建查询条件配置组件

**Files:**
- Create: `frontend/src/components/QueryConditions.vue`

**Step 1: 创建查询条件组件**

创建 `frontend/src/components/QueryConditions.vue`:

```vue
<template>
  <div class="query-conditions">
    <div class="conditions-header">
      <span>查询条件配置</span>
      <div>
        <el-button type="primary" link size="small" @click="handleAutoGenerate">
          <el-icon><MagicStick /></el-icon>
          自动生成
        </el-button>
        <el-button type="primary" link size="small" @click="showFieldSelector">
          <el-icon><Plus /></el-icon>
          添加条件
        </el-button>
      </div>
    </div>

    <el-table :data="modelValue" border max-height="300">
      <el-table-column label="字段" width="150">
        <template #default="{ row }">
          <span>{{ row.displayName }}</span>
        </template>
      </el-table-column>
      <el-table-column label="系统类型" width="100">
        <template #default="{ row }">
          <el-tag size="small">{{ getDataTypeLabel(row.dataType) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="条件关系" width="120">
        <template #default="{ row }">
          <el-select v-model="row.operator" size="small">
            <el-option
              v-for="op in getOperators(row.dataType)"
              :key="op.value"
              :label="op.label"
              :value="op.value"
            />
          </el-select>
        </template>
      </el-table-column>
      <el-table-column label="默认值">
        <template #default="{ row }">
          <template v-if="row.operator !== 'null' && row.operator !== 'notnull'">
            <el-input
              v-if="row.dataType === 'String'"
              v-model="row.defaultValue"
              size="small"
              placeholder="默认值"
            />
            <el-input-number
              v-else-if="row.dataType === 'Number'"
              v-model="row.defaultValue"
              size="small"
              :controls-position="'right'"
            />
            <el-select
              v-else-if="row.dataType === 'DateTime'"
              v-model="row.defaultValue"
              size="small"
            >
              <el-option label="今天" value="today" />
              <el-option label="本周" value="thisWeek" />
              <el-option label="本月" value="thisMonth" />
              <el-option label="最近7天" value="last7Days" />
              <el-option label="最近30天" value="last30Days" />
            </el-select>
            <el-select
              v-else-if="row.dataType === 'Boolean'"
              v-model="row.defaultValue"
              size="small"
            >
              <el-option label="是" :value="true" />
              <el-option label="否" :value="false" />
            </el-select>
          </template>
          <span v-else style="color: #909399;">无需默认值</span>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="80" align="center">
        <template #default="{ $index }">
          <el-button type="danger" link size="small" @click="removeCondition($index)">
            <el-icon><Delete /></el-icon>
          </el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 字段选择器对话框 -->
    <el-dialog v-model="fieldSelectorVisible" title="选择查询字段" width="500px">
      <el-checkbox-group v-model="selectedFieldNames">
        <el-checkbox
          v-for="field in availableFields"
          :key="field.fieldName"
          :label="field.fieldName"
        >
          {{ field.displayName }} ({{ getDataTypeLabel(field.dataType) }})
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
import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'

const props = defineProps({
  // 所有可用的字段
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

const fieldSelectorVisible = ref(false)
const selectedFieldNames = ref([])

// 可用的字段（未添加为查询条件的字段）
const availableFields = computed(() => {
  const selectedNames = props.modelValue.map(c => c.fieldName)
  return props.fields.filter(f => !selectedNames.includes(f.fieldName))
})

// 操作符映射
const operatorMap = {
  String: [
    { label: '等于', value: 'eq' },
    { label: '不等于', value: 'ne' },
    { label: '包含', value: 'like' },
    { label: '为空', value: 'null' },
    { label: '不为空', value: 'notnull' }
  ],
  Number: [
    { label: '等于', value: 'eq' },
    { label: '不等于', value: 'ne' },
    { label: '大于', value: 'gt' },
    { label: '小于', value: 'lt' },
    { label: '大于等于', value: 'ge' },
    { label: '小于等于', value: 'le' },
    { label: '为空', value: 'null' },
    { label: '不为空', value: 'notnull' }
  ],
  DateTime: [
    { label: '等于', value: 'eq' },
    { label: '大于', value: 'gt' },
    { label: '小于', value: 'lt' },
    { label: '大于等于', value: 'ge' },
    { label: '小于等于', value: 'le' },
    { label: '为空', value: 'null' },
    { label: '不为空', value: 'notnull' }
  ],
  Boolean: [
    { label: '等于', value: 'eq' }
  ]
}

const getDataTypeLabel = (type) => {
  const map = {
    'String': '字符串',
    'Number': '数值',
    'DateTime': '日期',
    'Boolean': '布尔'
  }
  return map[type] || type
}

const getOperators = (dataType) => {
  return operatorMap[dataType] || operatorMap.String
}

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
        dataType: field.dataType,
        operator: getOperators(field.dataType)[0].value,
        defaultValue: ''
      })
    }
  })
  fieldSelectorVisible.value = false
}

const removeCondition = (index) => {
  props.modelValue.splice(index, 1)
}

const handleAutoGenerate = () => {
  if (!props.fields || props.fields.length === 0) {
    ElMessage.warning('请先配置字段信息')
    return
  }

  const commonKeywords = ['名称', '姓名', '代码', '编号', '日期', '时间', '状态', '金额', '数量']
  const newConditions = []

  props.fields.forEach(field => {
    const displayName = field.displayName || field.fieldName
    const isCommonField = commonKeywords.some(kw => displayName.includes(kw))

    if (isCommonField && !props.modelValue.find(c => c.fieldName === field.fieldName)) {
      let defaultOperator = 'eq'
      if (field.dataType === 'String') {
        defaultOperator = 'like'
      } else if (field.dataType === 'DateTime') {
        defaultOperator = 'ge'
      } else if (field.dataType === 'Number') {
        defaultOperator = 'ge'
      }

      newConditions.push({
        fieldName: field.fieldName,
        displayName: displayName,
        dataType: field.dataType,
        operator: defaultOperator,
        defaultValue: field.dataType === 'DateTime' ? 'thisMonth' : ''
      })
    }
  })

  newConditions.forEach(c => props.modelValue.push(c))
  ElMessage.success(`已自动生成 ${newConditions.length} 个查询条件`)
}
</script>

<style scoped>
.query-conditions {
  margin-bottom: 20px;
}

.conditions-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
  font-weight: 500;
}
</style>
```

**Step 2: 提交新组件**

```bash
git add frontend/src/components/QueryConditions.vue
git commit -m "feat: 创建查询条件配置组件

- 支持选择字段作为查询条件
- 根据字段类型显示对应操作符
- 支持设置默认值
- 支持自动生成查询条件
- 支持添加/删除查询条件
"
```

---

### Task 6: 实现字段类型自动映射 - 后端

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/DatabaseService.cs`
- Modify: `backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs`

**Step 1: 扩展 TableColumnDto**

打开 `backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs`，修改 TableColumnDto：

```csharp
public class TableColumnDto
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public string? ColumnProperty { get; set; }
    public int Position { get; set; }

    /// <summary>
    /// 系统数据类型（用于前端查询条件）
    /// </summary>
    public string SystemDataType { get; set; } = "String";
}
```

**Step 2: 在 DatabaseService 中添加类型映射方法**

打开 `backend/src/DataForgeStudio.Core/Services/DatabaseService.cs`，添加映射方法和修改 GetTableStructureAsync：

```csharp
private string MapSystemDataType(string sqlDataType)
{
    var type = sqlDataType?.ToLower() ?? "";

    // 字符串类型
    if (type.Contains("char") || type.Contains("text") || type.Contains("nvarchar"))
        return "String";

    // 数值类型
    if (type.Contains("int") || type.Contains("decimal") ||
        type.Contains("numeric") || type.Contains("float") ||
        type.Contains("real") || type.Contains("money") || type.Contains("smallint") || type.Contains("bigint"))
        return "Number";

    // 日期类型
    if (type.Contains("date") || type.Contains("time"))
        return "DateTime";

    // 布尔类型
    if (type.Contains("bit"))
        return "Boolean";

    return "String"; // 默认字符串
}
```

**Step 3: 修改 GetTableStructureAsync 方法**

在 `GetTableStructureAsync` 方法中，创建 TableColumnDto 时添加 SystemDataType：

```csharp
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
```

**Step 4: 提交修改**

```bash
git add backend/src/DataForgeStudio.Core/Services/DatabaseService.cs
git add backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs
git commit -m "feat: 实现数据库字段类型自动映射

- TableColumnDto 添加 SystemDataType 属性
- 添加 MapSystemDataType 辅助方法
- 支持自动映射 SQL Server 类型到系统类型（String/Number/DateTime/Boolean）
"
```

---

### Task 7: 更新 ReportDesigner.vue 集成新功能

**Files:**
- Modify: `frontend/src/views/report/ReportDesigner.vue`

**Step 1: 集成 QueryConditions 组件**

打开 `frontend/src/views/report/ReportDesigner.vue`，在 script 部分添加导入：

```javascript
import QueryConditions from '../../components/QueryConditions.vue'
```

**Step 2: 在 form 中添加 queryConditions 字段**

找到 `const form = reactive({...})`，添加：

```javascript
const form = reactive({
  // ... 现有字段
  queryConditions: [],
  chartConfig: {
    chartType: 'bar',
    xField: '',
    yFields: [],
    title: ''
  }
})
```

**Step 3: 在模板中添加查询条件配置卡片**

在参数配置卡片后添加：

```vue
<!-- 查询条件配置 -->
<el-card class="design-card" style="margin-top: 20px;">
  <template #header>
    <div style="display: flex; justify-content: space-between; align-items: center;">
      <span>查询条件配置</span>
    </div>
  </template>

  <QueryConditions
    v-model="form.queryConditions"
    :fields="form.columns"
  />
</el-card>
```

**Step 4: 修改 handleAutoDetectFields 方法**

确保自动识别字段时使用 SystemDataType：

```javascript
const detectedFields = []

for (const [fieldName, value] of Object.entries(firstRow)) {
  let dataType = 'String'
  if (typeof value === 'number') {
    dataType = Number.isInteger(value) ? 'Number' : 'Number'
  } else if (value instanceof Date) {
    dataType = 'DateTime'
  } else if (typeof value === 'boolean') {
    dataType = 'Boolean'
  }

  detectedFields.push({
    fieldName: fieldName,
    displayName: fieldName,
    dataType: dataType,
    width: 120,
    align: dataType === 'Number' ? 'right' : 'left',
    isVisible: true,
    isSortable: true
  })
}
```

**Step 5: 修改 handleSave 方法**

确保保存时包含 queryConditions 和 chartConfig：

```javascript
const handleSave = async () => {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  saving.value = true
  try {
    const saveData = {
      ...form,
      chartConfig: form.enableChart ? JSON.stringify(form.chartConfig) : null,
      queryConditions: form.queryConditions.length > 0 ? JSON.stringify(form.queryConditions) : null
    }

    if (form.reportId) {
      await reportApi.updateReport(form.reportId, saveData)
    } else {
      await reportApi.createReport(saveData)
    }
    ElMessage.success('保存成功')
    router.push('/report/design')
  } catch (error) {
    console.error('保存失败:', error)
  } finally {
    saving.value = false
  }
}
```

**Step 6: 提交修改**

```bash
git add frontend/src/views/report/ReportDesigner.vue
git commit -m "feat: 报表设计器集成查询条件配置

- 集成 QueryConditions 组件
- 添加查询条件配置卡片
- 保存时序列化查询条件和图表配置
- 字段配置显示系统数据类型
"
```

---

### Task 8: 更新 ReportService 支持查询条件持久化

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs`
- Modify: `backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs`

**Step 1: 添加查询条件 DTO**

打开 `backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs`，添加：

```csharp
public class QueryConditionDto
{
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
    public string Operator { get; set; } = "eq";
    public string? DefaultValue { get; set; }
}

public class ChartConfigDto
{
    public string ChartType { get; set; } = "bar";
    public string XField { get; set; } = string.Empty;
    public List<string> YFields { get; set; } = new();
    public string Title { get; set; } = string.Empty;
}
```

**Step 2: 更新 ReportDetailDto**

```csharp
public class ReportDetailDto : ReportDto
{
    public string SqlStatement { get; set; } = string.Empty;
    public List<ReportFieldDto> Fields { get; set; } = new();
    public List<ReportParameterDto> Parameters { get; set; } = new();
    public bool EnableChart { get; set; }
    public ChartConfigDto? ChartConfig { get; set; }
    public List<QueryConditionDto>? QueryConditions { get; set; }
}
```

**Step 3: 更新 ReportService 的映射逻辑**

打开 `backend/src/DataForgeStudio.Core/Services/ReportService.cs`，在 GetReportByIdAsync 中添加反序列化：

```csharp
// 解析查询条件
if (!string.IsNullOrEmpty(report.QueryConditions))
{
    try
    {
        reportDto.QueryConditions = JsonSerializer.Deserialize<List<QueryConditionDto>>(report.QueryConditions);
    }
    catch
    {
        reportDto.QueryConditions = new List<QueryConditionDto>();
    }
}

// 解析图表配置
if (report.EnableChart && !string.IsNullOrEmpty(report.ChartConfig))
{
    try
    {
        reportDto.ChartConfig = JsonSerializer.Deserialize<ChartConfigDto>(report.ChartConfig);
    }
    catch
    {
        reportDto.ChartConfig = null;
    }
}
```

**Step 4: 更新 CreateReportAsync 和 UpdateReportAsync**

在保存时序列化查询条件和图表配置：

```csharp
// 保存查询条件
if (request.QueryConditions != null && request.QueryConditions.Count > 0)
{
    report.QueryConditions = JsonSerializer.Serialize(request.QueryConditions);
}
else
{
    report.QueryConditions = null;
}

// 保存图表配置
if (request.EnableChart && request.ChartConfig != null)
{
    report.ChartConfig = JsonSerializer.Serialize(request.ChartConfig);
}
else
{
    report.ChartConfig = null;
}
```

**Step 5: 提交修改**

```bash
git add backend/src/DataForgeStudio.Core/Services/ReportService.cs
git add backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs
git commit -m "feat: 支持查询条件和图表配置持久化

- 添加 QueryConditionDto 和 ChartConfigDto
- ReportService 支持序列化/反序列化查询条件和图表配置
- 更新 ReportDetailDto 包含新字段
"
```

---

### Task 9: 重构 ReportQuery.vue 为左右分栏布局

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`

**Step 1: 完全重写 ReportQuery.vue**

创建新的左右分栏布局：

```vue
<template>
  <div class="report-query">
    <el-row :gutter="20" style="height: 100%;">
      <!-- 左侧：报表列表 -->
      <el-col :span="8" style="height: 100%;">
        <el-card style="height: 100%; overflow-y: auto;">
          <template #header>
            <span>报表列表</span>
          </template>

          <!-- 搜索 -->
          <el-input
            v-model="searchKeyword"
            placeholder="搜索报表..."
            clearable
            style="margin-bottom: 15px;"
          >
            <template #prefix>
              <el-icon><Search /></el-icon>
            </template>
          </el-input>

          <!-- 报表列表 -->
          <div class="report-list">
            <div
              v-for="report in filteredReports"
              :key="report.reportId"
              :class="['report-item', { active: selectedReportId === report.reportId }]"
              @click="selectReport(report)"
            >
              <div class="report-icon">
                <el-icon><Document /></el-icon>
              </div>
              <div class="report-info">
                <div class="report-name">{{ report.reportName }}</div>
                <div class="report-meta">
                  <el-tag size="small" type="info">{{ report.reportCategory || '未分类' }}</el-tag>
                  <span class="view-count">{{ report.viewCount || 0 }} 次查看</span>
                </div>
              </div>
              <el-icon class="report-arrow" @click.stop="selectReport(report)">
                <ArrowRight />
              </el-icon>
            </div>
          </div>
        </el-card>
      </el-col>

      <!-- 右侧：查询区域 -->
      <el-col :span="16" style="height: 100%;">
        <el-card style="height: 100%; overflow-y: auto;">
          <!-- 未选择报表 -->
          <div v-if="!selectedReport" class="empty-state">
            <el-empty description="请选择左侧报表进行查询">
              <el-icon size="60" color="#909399"><Document /></el-icon>
            </el-empty>
          </div>

          <!-- 已选择报表 -->
          <div v-else class="query-area">
            <!-- 报表标题 -->
            <div class="report-header">
              <h2>{{ selectedReport.reportName }}</h2>
              <el-tag v-if="selectedReport.reportCategory" type="info">{{ selectedReport.reportCategory }}</el-tag>
            </div>

            <!-- 查询条件 -->
            <div v-if="queryConditions.length > 0" class="conditions-section">
              <div class="section-header">
                <span>查询条件</span>
                <el-button link size="small" @click="resetConditions">重置条件</el-button>
              </div>
              <el-form :inline="true" :model="queryForm" label-width="100px">
                <el-row :gutter="20">
                  <el-col :span="12" v-for="qc in queryConditions" :key="qc.fieldName">
                    <el-form-item :label="qc.displayName">
                      <!-- 字符串类型 -->
                      <el-input
                        v-if="qc.dataType === 'String' && qc.operator !== 'null' && qc.operator !== 'notnull'"
                        v-model="queryForm[qc.fieldName]"
                        :placeholder="getOperatorLabel(qc.operator)"
                        clearable
                      />
                      <!-- 数值类型 -->
                      <el-input-number
                        v-else-if="qc.dataType === 'Number' && qc.operator !== 'null' && qc.operator !== 'notnull'"
                        v-model="queryForm[qc.fieldName]"
                        :placeholder="getOperatorLabel(qc.operator)"
                        :controls-position="'right'"
                        style="width: 100%;"
                      />
                      <!-- 日期类型 -->
                      <el-date-picker
                        v-else-if="qc.dataType === 'DateTime' && qc.operator !== 'null' && qc.operator !== 'notnull'"
                        v-model="queryForm[qc.fieldName]"
                        type="date"
                        :placeholder="getOperatorLabel(qc.operator)"
                        value-format="YYYY-MM-DD"
                        style="width: 100%;"
                      />
                      <!-- 布尔类型 -->
                      <el-select
                        v-else-if="qc.dataType === 'Boolean'"
                        v-model="queryForm[qc.fieldName]"
                        placeholder="请选择"
                        clearable
                        style="width: 100%;"
                      >
                        <el-option label="是" :value="true" />
                        <el-option label="否" :value="false" />
                      </el-select>
                      <!-- 为空/不为空 -->
                      <span v-else class="condition-note">{{ getOperatorLabel(qc.operator) }}</span>
                    </el-form-item>
                  </el-col>
                </el-row>
              </el-form>
            </div>

            <!-- 操作按钮 -->
            <div class="action-buttons">
              <el-button type="primary" @click="handleQuery" :loading="querying">
                <el-icon><Search /></el-icon>
                查询
              </el-button>
              <el-button type="success" @click="handleExportExcel" :loading="exporting" :disabled="!reportData || reportData.length === 0">
                <el-icon><Download /></el-icon>
                导出 Excel
              </el-button>
            </div>

            <!-- 查询结果 -->
            <div v-if="reportData && reportData.length > 0" class="results-section">
              <div class="section-header">
                <span>查询结果 (共 {{ reportData.length }} 条记录)</span>
                <el-radio-group v-model="resultView" size="small">
                  <el-radio-button label="table">表格</el-radio-button>
                  <el-radio-button label="chart" v-if="selectedReport.enableChart">图表</el-radio-button>
                </el-radio-group>
              </div>

              <!-- 表格视图 -->
              <el-table v-if="resultView === 'table'" :data="reportData" border max-height="400">
                <el-table-column
                  v-for="col in displayColumns"
                  :key="col.fieldName"
                  :prop="col.fieldName"
                  :label="col.displayName"
                  :width="col.width"
                  :align="col.align || 'left'"
                />
              </el-table>

              <!-- 图表视图 -->
              <div v-if="resultView === 'chart' && selectedReport.enableChart" ref="chartRef" style="height: 400px;"></div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, nextTick, onUnmounted } from 'vue'
import { ElMessage } from 'element-plus'
import * as echarts from 'echarts'
import { reportApi } from '../../api/request'

const searchKeyword = ref('')
const reports = ref([])
const selectedReportId = ref(null)
const selectedReport = ref(null)
const queryConditions = ref([])
const queryForm = reactive({})
const reportData = ref([])
const querying = ref(false)
const exporting = ref(false)
const resultView = ref('table')
const chartRef = ref(null)
let chartInstance = null

// 获取报表列表
onMounted(async () => {
  await loadReports()
})

const loadReports = async () => {
  try {
    const res = await reportApi.getReports({ page: 1, pageSize: 1000 })
    if (res.success) {
      reports.value = (res.data.Items || res.data.items || []).filter(r => r.isEnabled)
    }
  } catch (error) {
    console.error('加载报表失败:', error)
  }
}

// 过滤报表
const filteredReports = computed(() => {
  if (!searchKeyword.value) return reports.value
  return reports.value.filter(r =>
    r.reportName.toLowerCase().includes(searchKeyword.value.toLowerCase())
  )
})

// 选择报表
const selectReport = async (report) => {
  selectedReportId.value = report.reportId
  try {
    const res = await reportApi.getReport(report.reportId)
    if (res.success) {
      selectedReport.value = res.data
      queryConditions.value = res.data.queryConditions || []
      resetConditions()
      reportData.value = []
    }
  } catch (error) {
    console.error('加载报表详情失败:', error)
  }
}

// 获取显示列
const displayColumns = computed(() => {
  return selectedReport.value?.fields || []
})

// 重置条件
const resetConditions = () => {
  Object.keys(queryForm).forEach(key => delete queryForm[key])
  queryConditions.value.forEach(qc => {
    if (qc.defaultValue) {
      queryForm[qc.fieldName] = qc.defaultValue
    }
  })
}

// 获取操作符标签
const getOperatorLabel = (operator) => {
  const map = {
    'eq': '等于',
    'ne': '不等于',
    'gt': '大于',
    'lt': '小于',
    'ge': '大于等于',
    'le': '小于等于',
    'like': '包含',
    'null': '为空',
    'notnull': '不为空'
  }
  return map[operator] || operator
}

// 执行查询
const handleQuery = async () => {
  querying.value = true
  try {
    const params = buildQueryParams()
    const res = await reportApi.executeReport(selectedReport.value.reportId, params)
    if (res.success) {
      reportData.value = res.data

      if (selectedReport.value.enableChart) {
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

// 构建查询参数
const buildQueryParams = () => {
  const params = {}
  queryConditions.value.forEach(qc => {
    const value = queryForm[qc.fieldName]
    if (value === '' || value === null || value === undefined) {
      return
    }
    params[`${qc.fieldName}_${qc.operator}`] = value
  })
  return params
}

// 渲染图表
const renderChart = () => {
  if (!chartRef.value) return

  if (chartInstance) {
    chartInstance.dispose()
  }

  chartInstance = echarts.init(chartRef.value)

  const columns = displayColumns.value || []
  const data = reportData.value || []

  if (data.length === 0) return

  const chartConfig = selectedReport.value.chartConfig || {}
  const xAxisData = data.map(row => row[chartConfig.xField || columns[0]?.fieldName] || '')
  const seriesData = chartConfig.yFields?.map(yField => ({
    name: yField,
    type: chartConfig.chartType || 'bar',
    data: data.map(row => row[yField] || 0)
  })) || []

  const option = {
    title: { text: chartConfig.title || selectedReport.value.reportName },
    tooltip: { trigger: 'axis' },
    legend: { data: seriesData.map(s => s.name) },
    xAxis: { type: 'category', data: xAxisData },
    yAxis: { type: 'value' },
    series: seriesData
  }

  if (chartConfig.chartType === 'pie') {
    option.xAxis = undefined
    option.yAxis = undefined
    option.series = [{
      type: 'pie',
      data: xAxisData.map((x, i) => ({ name: x, value: seriesData[0]?.data[i] || 0 }))
    }]
  }

  chartInstance.setOption(option)
}

// 导出 Excel
const handleExportExcel = async () => {
  exporting.value = true
  try {
    const params = buildQueryParams()
    const res = await reportApi.exportReport(selectedReport.value.reportId, params)
    const blob = new Blob([res], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${selectedReport.value.reportName}_${Date.now()}.xlsx`
    a.click()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    console.error('导出失败:', error)
  } finally {
    exporting.value = false
  }
}

// 清理
onUnmounted(() => {
  if (chartInstance) {
    chartInstance.dispose()
  }
})
</script>

<style scoped>
.report-query {
  height: 100%;
}

.report-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.report-item {
  display: flex;
  align-items: center;
  padding: 12px;
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
}

.report-item:hover {
  background-color: #f5f7fa;
  border-color: #409eff;
}

.report-item.active {
  background-color: #ecf5ff;
  border-color: #409eff;
}

.report-icon {
  font-size: 24px;
  color: #409eff;
  margin-right: 12px;
}

.report-info {
  flex: 1;
}

.report-name {
  font-weight: 500;
  margin-bottom: 4px;
}

.report-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: #909399;
}

.report-arrow {
  color: #c0c4cc;
}

.empty-state {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
}

.query-area {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.report-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-bottom: 16px;
  border-bottom: 1px solid #e4e7ed;
}

.report-header h2 {
  margin: 0;
  font-size: 20px;
}

.conditions-section,
.results-section {
  padding: 16px;
  background-color: #f5f7fa;
  border-radius: 6px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
  font-weight: 500;
}

.action-buttons {
  display: flex;
  gap: 12px;
}

.condition-note {
  color: #909399;
  font-size: 14px;
}
</style>
```

**Step 2: 提交修改**

```bash
git add frontend/src/views/report/ReportQuery.vue
git commit -m "feat: 重构报表查询页面为左右分栏布局

- 左侧显示已启用报表列表
- 右侧显示查询区域
- 根据报表配置动态生成查询条件
- 支持表格和图表切换
- 移除编辑和删除功能
"
```

---

## 阶段三：增强功能

### Task 10: 添加报表统计功能 API

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs`

**Step 1: 添加获取统计信息方法**

在 `ReportService.cs` 中添加：

```csharp
public async Task<ApiResponse<ReportStatisticsDto>> GetReportStatisticsAsync(int reportId)
{
    var report = await _context.Reports.FindAsync(reportId);
    if (report == null)
    {
        return ApiResponse.Fail<ReportStatisticsDto>("报表不存在", "REPORT_NOT_FOUND");
    }

    var statistics = new ReportStatisticsDto
    {
        ReportId = report.ReportId,
        ReportName = report.ReportName,
        ViewCount = report.ViewCount,
        LastViewTime = report.LastViewTime,
        CreatedTime = report.CreatedTime,
        UpdatedTime = report.UpdatedTime
    };

    return ApiResponse.Ok(statistics);
}
```

**Step 2: 添加 ReportStatisticsDto**

在 `ApiResponse.cs` 中添加：

```csharp
public class ReportStatisticsDto
{
    public int ReportId { get; set; }
    public string ReportName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public DateTime? LastViewTime { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}
```

**Step 3: 添加控制器方法**

在 `ReportController.cs` 中添加：

```csharp
[HttpGet("{id}/statistics")]
[RequirePermission("report:view")]
public async Task<IActionResult> GetStatistics(int id)
{
    var result = await _reportService.GetReportStatisticsAsync(id);
    return Ok(result);
}
```

**Step 4: 提交修改**

```bash
git add backend/src/DataForgeStudio.Core/Services/ReportService.cs
git add backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs
git add backend/src/DataForgeStudio.Api/Controllers/ReportController.cs
git commit -m "feat: 添加报表统计信息 API

- 添加 GetReportStatisticsAsync 方法
- 添加 ReportStatisticsDto
- 添加 GET /api/reports/{id}/statistics 接口
"
```

---

### Task 11: 添加报表复制功能 API

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs`

**Step 1: 添加复制报表方法**

在 `ReportService.cs` 中添加：

```csharp
public async Task<ApiResponse<ReportDetailDto>> CopyReportAsync(int reportId, int? userId)
{
    var originalReport = await _context.Reports
        .Include(r => r.Fields)
        .Include(r => r.Parameters)
        .FirstOrDefaultAsync(r => r.ReportId == reportId);

    if (originalReport == null)
    {
        return ApiResponse.Fail<ReportDetailDto>("报表不存在", "REPORT_NOT_FOUND");
    }

    // 创建新报表
    var newReport = new Report
    {
        ReportName = $"{originalReport.ReportName} (副本)",
        ReportCode = $"{originalReport.ReportCode}_COPY_{DateTime.UtcNow:yyyyMMddHHmmss}",
        ReportCategory = originalReport.ReportCategory,
        DataSourceId = originalReport.DataSourceId,
        SqlStatement = originalReport.SqlStatement,
        Description = originalReport.Description,
        IsPaged = originalReport.IsPaged,
        PageSize = originalReport.PageSize,
        CacheDuration = originalReport.CacheDuration,
        IsEnabled = false, // 副本默认停用
        IsSystem = false,
        ViewCount = 0,
        CreatedBy = userId,
        CreatedTime = DateTime.UtcNow,
        EnableChart = originalReport.EnableChart,
        ChartConfig = originalReport.ChartConfig,
        QueryConditions = originalReport.QueryConditions
    };

    _context.Reports.Add(newReport);
    await _context.SaveChangesAsync();

    // 复制字段配置
    foreach (var field in originalReport.Fields)
    {
        var newField = new ReportField
        {
            ReportId = newReport.ReportId,
            FieldName = field.FieldName,
            DisplayName = field.DisplayName,
            DataType = field.DataType,
            Width = field.Width,
            IsVisible = field.IsVisible,
            IsSortable = field.IsSortable,
            IsFilterable = field.IsFilterable,
            IsGroupable = field.IsGroupable,
            SortOrder = field.SortOrder,
            Align = field.Align,
            FormatString = field.FormatString,
            AggregateFunction = field.AggregateFunction,
            CssClass = field.CssClass,
            Remark = field.Remark,
            CreatedTime = DateTime.UtcNow
        };
        _context.ReportFields.Add(newField);
    }

    // 复制参数配置
    foreach (var param in originalReport.Parameters)
    {
        var newParam = new ReportParameter
        {
            ReportId = newReport.ReportId,
            ParameterName = param.ParameterName,
            DisplayName = param.DisplayName,
            DataType = param.DataType,
            InputType = param.InputType,
            DefaultValue = param.DefaultValue,
            IsRequired = param.IsRequired,
            SortOrder = param.SortOrder,
            Options = param.Options,
            QueryOptions = param.QueryOptions,
            Remark = param.Remark,
            CreatedTime = DateTime.UtcNow
        };
        _context.ReportParameters.Add(newParam);
    }

    await _context.SaveChangesAsync();

    // 返回新报表详情
    return await GetReportByIdAsync(newReport.ReportId);
}
```

**Step 2: 添加控制器方法**

在 `ReportController.cs` 中添加：

```csharp
[HttpPost("{id}/copy")]
[RequirePermission("report:create")]
public async Task<IActionResult> CopyReport(int id)
{
    var userId = User.GetUserId();
    var result = await _reportService.CopyReportAsync(id, userId);
    return Ok(result);
}
```

**Step 3: 提交修改**

```bash
git add backend/src/DataForgeStudio.Core/Services/ReportService.cs
git add backend/src/DataForgeStudio.Api/Controllers/ReportController.cs
git commit -m "feat: 添加报表复制功能

- 添加 CopyReportAsync 方法
- 复制报表基本信息、字段配置、参数配置
- 副本默认停用状态
- 添加 POST /api/reports/{id}/copy 接口
"
```

---

### Task 12: 添加报表配置导出功能 API

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs`

**Step 1: 添加导出配置方法**

在 `ReportService.cs` 中添加：

```csharp
public async Task<ApiResponse<List<ReportConfigExportDto>>> ExportAllReportConfigsAsync()
{
    var reports = await _context.Reports
        .Include(r => r.Fields)
        .Include(r => r.Parameters)
        .Where(r => !r.IsSystem)
        .OrderBy(r => r.ReportCategory)
        .ThenBy(r => r.ReportName)
        .ToListAsync();

    var configs = reports.Select(r => new ReportConfigExportDto
    {
        ReportName = r.ReportName,
        ReportCode = r.ReportCode,
        ReportCategory = r.ReportCategory,
        DataSourceId = r.DataSourceId,
        SqlStatement = r.SqlStatement,
        Description = r.Description,
        EnableChart = r.EnableChart,
        ChartConfig = r.ChartConfig,
        QueryConditions = r.QueryConditions,
        Fields = r.Fields.Select(f => new FieldConfigExportDto
        {
            FieldName = f.FieldName,
            DisplayName = f.DisplayName,
            DataType = f.DataType,
            Width = f.Width,
            IsVisible = f.IsVisible,
            IsSortable = f.IsSortable,
            Align = f.Align
        }).ToList(),
        Parameters = r.Parameters.Select(p => new ParameterConfigExportDto
        {
            ParameterName = p.ParameterName,
            DisplayName = p.DisplayName,
            DataType = p.DataType,
            InputType = p.InputType,
            DefaultValue = p.DefaultValue,
            IsRequired = p.IsRequired
        }).ToList()
    }).ToList();

    return ApiResponse.Ok(configs);
}
```

**Step 2: 添加导出 DTO**

在 `ApiResponse.cs` 中添加：

```csharp
public class ReportConfigExportDto
{
    public string ReportName { get; set; } = string.Empty;
    public string ReportCode { get; set; } = string.Empty;
    public string? ReportCategory { get; set; }
    public int DataSourceId { get; set; }
    public string SqlStatement { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool EnableChart { get; set; }
    public string? ChartConfig { get; set; }
    public string? QueryConditions { get; set; }
    public List<FieldConfigExportDto> Fields { get; set; } = new();
    public List<ParameterConfigExportDto> Parameters { get; set; } = new();
}

public class FieldConfigExportDto
{
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int Width { get; set; }
    public bool IsVisible { get; set; }
    public bool IsSortable { get; set; }
    public string Align { get; set; } = "left";
}

public class ParameterConfigExportDto
{
    public string ParameterName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string InputType { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
}
```

**Step 3: 添加控制器方法**

在 `ReportController.cs` 中添加：

```csharp
[HttpGet("export-config")]
[RequirePermission("report:design")]
public async Task<IActionResult> ExportAllConfigs()
{
    var result = await _reportService.ExportAllReportConfigsAsync();
    return Ok(result);
}
```

**Step 4: 提交修改**

```bash
git add backend/src/DataForgeStudio.Core/Services/ReportService.cs
git add backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs
git add backend/src/DataForgeStudio.Api/Controllers/ReportController.cs
git commit -m "feat: 添加报表配置导出功能

- 添加 ExportAllReportConfigsAsync 方法
- 导出所有非系统报表的配置
- 添加 GET /api/reports/export-config 接口
"
```

---

## 阶段四：权限和测试

### Task 13: 更新权限定义

**Files:**
- Modify: `backend/src/DataForgeStudio.Data/Data/DbInitializer.cs`

**Step 1: 添加报表相关权限**

打开 `backend/src/DataForgeStudio.Data/Data/DbInitializer.cs`，在 CreateAllPermissionsAsync 方法中添加：

```csharp
// 报表查询相关
new Permission { PermissionCode = "report:query", PermissionName = "访问报表查询", Module = "Report", Action = "Query", Description = "访问报表查询页面" },
new Permission { PermissionCode = "report:execute", PermissionName = "执行报表查询", Module = "Report", Action = "Execute", Description = "执行报表查询并查看结果" },

// 报表设计相关
new Permission { PermissionCode = "report:design", PermissionName = "访问报表设计", Module = "Report", Action = "Design", Description = "访问报表设计管理页面" },
new Permission { PermissionCode = "report:create", PermissionName = "创建报表", Module = "Report", Action = "Create", Description = "创建新报表" },
new Permission { PermissionCode = "report:edit", PermissionName = "编辑报表", Module = "Report", Action = "Edit", Description = "编辑报表配置" },
new Permission { PermissionCode = "report:delete", PermissionName = "删除报表", Module = "Report", Action = "Delete", Description = "删除报表" },
new Permission { PermissionCode = "report:toggle", PermissionName = "停用启用报表", Module = "Report", Action = "Toggle", Description = "停用或启用报表" },
```

**Step 2: 更新角色权限分配**

修改 CreateDefaultRolesAsync 方法，更新角色权限：

```csharp
// 报表查询组
var viewerPermissions = new List<string>
{
    "report:query",
    "report:execute"
};

// 报表设计组
var designerPermissions = new List<string>
{
    "report:query",
    "report:execute",
    "report:design",
    "report:create",
    "report:edit",
    "report:toggle"
};

// 管理员拥有所有权限
```

**Step 3: 提交修改**

```bash
git add backend/src/DataForgeStudio.Data/Data/DbInitializer.cs
git commit -m "feat: 更新报表权限定义

- 添加 report:query 权限
- 添加 report:execute 权限
- 添加 report:design 权限
- 添加 report:create 权限
- 添加 report:edit 权限
- 添加 report:delete 权限
- 添加 report:toggle 权限
- 更新角色权限分配
"
```

---

### Task 14: 端到端测试和验证

**Files:**
- Test: 手动测试所有功能

**Step 1: 启动服务**

```bash
# 启动后端
cd backend/src/DataForgeStudio.Api && dotnet run

# 启动前端（新终端）
cd frontend && npm run dev
```

**Step 2: 功能测试清单**

**报表查询页面测试：**
- [ ] 页面显示左右分栏布局
- [ ] 左侧显示已启用的报表列表
- [ ] 点击报表后右侧显示查询界面
- [ ] 查询条件根据配置动态生成
- [ ] 执行查询后显示结果
- [ ] 导出 Excel 功能正常

**报表设计列表页面测试：**
- [ ] 显示所有报表（包括停用的）
- [ ] 新建报表按钮跳转到设计器
- [ ] 编辑按钮正常工作
- [ ] 预览功能正常
- [ ] 复制功能创建副本
- [ ] 停用/启用功能正常
- [ ] 删除功能有二次确认
- [ ] 导出配置功能正常

**报表设计器测试：**
- [ ] 字段配置显示系统类型
- [ ] 测试查询后自动映射字段类型
- [ ] 查询条件配置组件正常
- [ ] 自动生成查询条件功能
- [ ] 保存报表成功

**权限测试：**
- [ ] 报表查询组只能访问查询页面
- [ ] 报表设计组可以访问设计页面
- [ ] 未授权页面显示权限错误

**Step 3: 验证通过后提交**

```bash
git add .
git commit -m "test: 完成报表管理系统重构测试

- 所有功能测试通过
- 权限控制正常
- 左右分栏布局正常
- 查询条件配置正常
- 字段类型自动映射正常
"
```

---

## 执行顺序

按顺序执行上述 14 个任务，每个任务执行完毕后应确保：
1. 代码编译通过
2. 功能正常工作
3. 提交代码到 git

完成所有任务后，系统应该具备以下功能：
- 报表查询和报表设计完全分离
- 左右分栏的查询页面布局
- 根据配置动态生成查询条件
- 字段类型自动映射
- 完善的权限控制

---

## 技能参考

实施过程中，每个任务应使用对应的 skill：

- 前端组件开发: `vue-best-practices`
- 后端服务开发: 常规编码
- 测试相关: `superpowers:test-driven-development`
- 代码审查: `superpowers:requesting-code-review`
