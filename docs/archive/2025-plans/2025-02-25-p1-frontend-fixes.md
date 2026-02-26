# P1: 前端修复与改进实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复前端表单显示问题和改进备份路径选择交互

**Architecture:** 修改现有 Vue 组件，解决数据绑定和表单重置问题，添加文件夹选择对话框功能

**Tech Stack:** Vue 3, Element Plus, Composition API

**Related Tasks:** fix2.md 任务 7, 8, 9, 11, 12

---

## Task 1: 修复数据源编辑弹窗信息显示不全 (fix2.md #7)

**问题描述:** 编辑数据源时弹窗未显示所有字段（端口、描述等）

**Files:**
- Modify: `frontend/src/views/system/DataSourceManagement.vue:281-285`

**Step 1: 定位问题**

当前 `handleEdit` 函数使用 `Object.assign(form, row)` 直接赋值，但 row 中的字段名可能与 form 定义不匹配。

**Step 2: 修复字段映射**

在 `handleEdit` 函数中添加正确的字段映射：

```javascript
const handleEdit = (row) => {
  isEdit.value = true
  // 正确映射所有字段
  Object.assign(form, {
    dataSourceId: row.dataSourceId,
    dataSourceName: row.dataSourceName || '',
    dbType: row.dbType || 'SqlServer',
    server: row.serverAddress || row.server || 'localhost',
    port: row.port || 1433,
    database: row.databaseName || row.database || '',
    username: row.username || '',
    password: '', // 密码不回显，需要重新输入
    description: row.description || ''
  })
  dialogVisible.value = true
}
```

**Step 3: 验证修复**

1. 启动前端：`cd frontend && npm run dev`
2. 打开数据源管理页面
3. 点击"编辑"按钮，确认所有字段正确回显

---

## Task 2: 修复报表编辑弹窗信息显示不全 (fix2.md #8)

**问题描述:** 编辑报表时弹窗缺失部分字段

**Files:**
- Modify: `frontend/src/views/report/ReportDesigner.vue:519-557`

**Step 1: 检查 loadReport 函数**

当前 `loadReport` 函数需要确保加载所有字段。

**Step 2: 完善字段映射**

修改 `loadReport` 函数，确保所有字段正确加载：

```javascript
const loadReport = async (id) => {
  try {
    const res = await reportApi.getReport(id)
    if (res.success) {
      const data = res.data

      form.reportId = data.reportId
      form.reportName = data.reportName || ''
      form.reportCategory = data.reportCategory || ''
      form.description = data.description || ''
      form.dataSourceId = data.dataSourceId
      form.sqlQuery = data.sqlStatement || data.sqlQuery || ''
      form.parameters = data.parameters || []
      form.enableChart = data.enableChart || false

      // 确保 chartConfig 完整加载
      form.chartConfig = data.chartConfig || {
        chartType: 'bar',
        xField: '',
        yFields: [],
        title: ''
      }

      // 确保 queryConditions 加载
      form.queryConditions = data.queryConditions || data.queryFields || []

      // 确保字段配置加载（支持 columns 和 fields 两种命名）
      const columns = data.columns || data.fields || []
      form.columns = columns

      if (columns.length > 0) {
        availableFields.value = columns.map(f => ({
          fieldName: f.fieldName,
          displayName: f.displayName || f.fieldName,
          dataType: f.dataType
        }))
      }
    }
  } catch {
    // 加载失败
  }
}
```

**Step 3: 验证修复**

1. 打开报表设计页面，编辑一个已存在的报表
2. 确认报表名称、分类、描述、数据源、SQL、字段配置、查询条件、图表配置都正确显示

---

## Task 3: 修复新增报表时表单残留数据 (fix2.md #9)

**问题描述:** 点击"新增报表"后表单显示之前编辑的数据

**Files:**
- Modify: `frontend/src/views/report/ReportDesigner.vue`

**Step 1: 添加表单重置函数**

在 `ReportDesigner.vue` 中添加 `resetForm` 函数：

```javascript
// 重置表单到初始状态
const resetForm = () => {
  form.reportId = null
  form.reportName = ''
  form.reportCategory = ''
  form.description = ''
  form.dataSourceId = null
  form.sqlQuery = ''
  form.columns = []
  form.parameters = []
  form.queryConditions = []
  form.enableChart = false
  form.chartConfig = {
    chartType: 'bar',
    xField: '',
    yFields: [],
    title: ''
  }
  availableFields.value = []

  // 重置表单验证状态
  formRef.value?.resetFields()
}
```

**Step 2: 在 onMounted 中判断新建还是编辑**

修改 `onMounted` 逻辑：

```javascript
onMounted(async () => {
  await loadDataSources()

  const reportId = route.query.id
  if (reportId) {
    // 编辑模式：加载报表数据
    loadReport(reportId)
  } else {
    // 新建模式：重置表单
    resetForm()
  }
})
```

**Step 3: 添加路由监听**

添加 `watch` 监听路由变化，处理从编辑页面跳转到新建页面的情况：

```javascript
import { watch } from 'vue'

// 监听路由变化
watch(() => route.query.id, (newId) => {
  if (newId) {
    loadReport(newId)
  } else {
    resetForm()
  }
})
```

**Step 4: 验证修复**

1. 编辑一个报表
2. 点击"新增报表"（或通过导航进入）
3. 确认表单为空白状态

---

## Task 4: 改进备份创建交互 - 添加文件夹选择 (fix2.md #11)

**问题描述:** 备份路径需要手动输入，不直观

**Files:**
- Modify: `frontend/src/views/system/BackupManagement.vue`

**Step 1: 使用 Element Plus 的 el-upload 或自定义按钮**

由于浏览器安全限制，Web 应用无法直接访问文件系统。需要使用后端 API 来处理路径选择。

**方案：添加后端 API 获取可用目录列表**

修改前端，添加路径选择按钮和弹出框：

```vue
<!-- 在 toolbar-left 中修改备份路径输入框 -->
<el-input
  v-model="backupForm.backupPath"
  placeholder="备份路径，如 D:\Backups"
  style="width: 280px;"
  clearable
>
  <template #append>
    <el-button :icon="FolderOpened" @click="handleSelectBackupPath" />
  </template>
</el-input>
```

**Step 2: 添加路径选择对话框**

```vue
<!-- 路径选择对话框 -->
<el-dialog v-model="pathDialogVisible" title="选择备份路径" width="500px">
  <el-input
    v-model="selectedPath"
    placeholder="当前路径"
    readonly
    style="margin-bottom: 10px;"
  >
    <template #prepend>当前路径</template>
  </el-input>

  <div class="path-list">
    <div
      v-for="dir in directoryList"
      :key="dir.path"
      class="path-item"
      @click="handleNavigateDirectory(dir.path)"
    >
      <el-icon><Folder /></el-icon>
      <span>{{ dir.name }}</span>
    </div>
    <el-empty v-if="directoryList.length === 0" description="无法读取目录" />
  </div>

  <template #footer>
    <el-button @click="pathDialogVisible = false">取消</el-button>
    <el-button type="primary" @click="confirmPath">确认选择</el-button>
  </template>
</el-dialog>
```

**Step 3: 添加后端 API（需要后端支持）**

在 `SystemController.cs` 添加获取目录列表的 API：

```csharp
[HttpGet("directories")]
public async Task<ApiResponse<List<DirectoryInfoDto>>> GetDirectories([FromQuery] string? path)
{
    // 实现获取目录列表的逻辑
}
```

**Step 4: 如果无法添加后端 API，使用简化方案**

如果后端暂时无法修改，可以使用提示文字替代：

```vue
<el-input
  v-model="backupForm.backupPath"
  placeholder="备份路径，如 D:\Backups"
  style="width: 280px;"
  clearable
>
  <template #append>
    <el-tooltip content="请输入 SQL Server 服务账户有写入权限的路径" placement="top">
      <el-button :icon="QuestionFilled" />
    </el-tooltip>
  </template>
</el-input>
```

**验证:** 当前已有提示，如果需要文件夹选择对话框，需要后端配合添加 API

---

## Task 5: 改进备份计划路径选择交互 (fix2.md #12)

**问题描述:** 备份计划路径选择不够便捷

**Files:**
- Modify: `frontend/src/views/system/BackupManagement.vue:223-231`

**Step 1: 修改计划编辑对话框中的路径输入**

将当前的提示文字改为按钮：

```vue
<el-form-item label="备份路径" required>
  <el-input v-model="scheduleForm.backupPath" placeholder="如 D:\Backups" clearable>
    <template #append>
      <el-button @click="handleSelectSchedulePath">
        <el-icon><FolderOpened /></el-icon>
        选择
      </el-button>
    </template>
  </el-input>
</el-form-item>
```

**Step 2: 复用路径选择逻辑**

```javascript
const handleSelectSchedulePath = () => {
  // 复用 Task 4 中实现的路径选择逻辑
  pathDialogVisible.value = true
  currentPathTarget.value = 'schedule' // 标记当前是选择计划路径
}

const confirmPath = () => {
  if (currentPathTarget.value === 'backup') {
    backupForm.backupPath = selectedPath.value
  } else if (currentPathTarget.value === 'schedule') {
    scheduleForm.backupPath = selectedPath.value
  }
  pathDialogVisible.value = false
}
```

**Step 3: 验证修复**

1. 打开备份管理页面
2. 点击"新增计划"
3. 确认备份路径输入框右侧有"选择"按钮
4. 点击按钮可以打开路径选择对话框

---

## 执行顺序

1. Task 1 → Task 2 → Task 3 (表单修复，互不依赖)
2. Task 4 → Task 5 (路径选择改进，Task 5 依赖 Task 4)

## 注意事项

- Task 4 和 Task 5 的文件夹选择功能需要后端 API 支持
- 如果后端暂时无法修改，可以先保留当前的输入框+提示方式
- 所有修改后需要测试表单的提交功能是否正常
