# Bug Fixes and Feature Improvements Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复 7 个测试发现的 bug 并实现 2 个新功能

**Architecture:** 前后端分离架构，前端 Vue 3 + Element Plus，后端 ASP.NET Core 8.0

**Tech Stack:** Vue 3, Pinia, Vue Router, CodeMirror 6, ASP.NET Core, EF Core, SQL Server

---

## 问题概览

| # | 问题描述 | 类型 | 优先级 |
|---|----------|------|--------|
| 1 | 访问首页路由跳转错误 | 前端 Bug | 高 |
| 2 | 新增权限组后角色选择不更新 | 前端状态管理 | 中 |
| 3 | 删除权限组时 500 错误 | 后端 Bug | 高 |
| 4 | 数据源窗口布局调整 | UI 调整 | 低 |
| 5 | 日志导出功能未实现 | 新功能 | 中 |
| 6 | 备份管理功能改进 | 功能改进 | 中 |
| 7 | 报表设计页面 searchKeymap 错误 | 前端 Bug | 高 |

---

## Task 1: 修复路由跳转问题（问题 1）

**描述:** 访问 `http://localhost:5173` 时，如果 token 有效应进入 `/home`，无效应进入 `/login`。当前会先进入 `/home` 再跳转到 `/login`。

**根本原因:** 路由守卫中，当访问根路径 `/` 时会重定向到 `/home`，但此时 token 检查尚未完成，导致先显示首页再跳转。

**Files:**
- Modify: `frontend/src/router/index.js:92-150`

**Step 1: 分析当前路由守卫逻辑**

当前 `beforeEach` 守卫的执行顺序：
1. 检查 `to.meta.requiresAuth`
2. 检查 `userStore.isLoggedIn`（仅检查 token 是否存在）
3. 如果未登录，跳转 `/login`
4. 如果已登录但无 userInfo，调用 `getCurrentUser()`
5. 如果访问 `/login` 且已登录，跳转 `/home`

问题：当访问 `/` 时，重定向到 `/home`，但此时 `userStore.isLoggedIn` 可能返回 true（token 存在于 localStorage），但 token 可能已过期。

**Step 2: 修改路由守卫逻辑**

在 `frontend/src/router/index.js` 中修改 `beforeEach` 守卫：

```javascript
// 修改后的路由守卫
router.beforeEach(async (to, from, next) => {
  // 设置页面标题
  document.title = to.meta.title ? `${to.meta.title} - DataForgeStudio V4` : 'DataForgeStudio V4'

  const userStore = useUserStore()

  // 避免重复导航
  if (to.path === from.path) {
    next()
    return
  }

  // 检查是否需要认证
  if (to.meta.requiresAuth) {
    // 首先检查 token 是否存在
    const hasToken = !!localStorage.getItem('token')

    if (!hasToken) {
      // 没有 token，直接跳转到登录页
      console.log('Route guard: No token found, redirecting to login')
      next('/login')
      return
    }

    // 有 token，验证用户信息
    if (!userStore.userInfo) {
      try {
        await userStore.getCurrentUser()
        // 如果获取用户信息失败（token 过期），getCurrentUser 会自动调用 logout()
        if (!userStore.userInfo) {
          console.log('Route guard: Failed to get user info, redirecting to login')
          next('/login')
          return
        }
      } catch (error) {
        // token 无效，跳转到登录页
        console.log('Route guard: Token invalid, redirecting to login')
        next('/login')
        return
      }
    }
  }

  // 如果访问登录页且已经登录，跳转到首页
  if (to.path === '/login' && userStore.isLoggedIn) {
    console.log('Route guard: Already logged in, redirecting to home')
    next('/home')
    return
  }

  // 检查权限
  if (to.meta.permission) {
    if (!userStore.hasPermission(to.meta.permission)) {
      ElMessage.error(`您没有访问该页面的权限，需要权限：${to.meta.permission}`)
      next(from.path || '/')
      return
    }
  }

  next()
})
```

**Step 3: 测试路由跳转**

1. 清空 localStorage: `localStorage.clear()`
2. 访问 `http://localhost:5173`
3. 预期：直接进入 `/login` 页面，不显示 `/home`

4. 使用有效 token 登录
5. 访问 `http://localhost:5173`
6. 预期：直接进入 `/home` 页面

**Step 4: 提交代码**

```bash
git add frontend/src/router/index.js
git commit -m "fix: correct route guard to prevent redirect loop when token expires"
```

---

## Task 2: 修复新增权限组后角色选择不更新（问题 2）

**描述:** 在权限组管理中新增角色后，去用户管理分配角色时，新角色不显示，需要刷新页面。

**根本原因:** UserManagement.vue 的 `allRoles` 只在 `onMounted` 时加载一次，新增角色后没有更新。

**Files:**
- Modify: `frontend/src/views/system/UserManagement.vue:286-296`

**Step 1: 分析角色加载逻辑**

当前 `loadRoles` 函数只在组件挂载时调用一次：
```javascript
onMounted(() => {
  loadData()
  loadRoles()  // 只在挂载时加载
})
```

**Step 2: 使用 provide/inject 或事件总线实现跨组件通信**

方案：在分配角色对话框打开时重新加载角色列表。

修改 `frontend/src/views/system/UserManagement.vue`:

```javascript
// 在 handleAssignRoles 函数中添加角色重新加载
const handleAssignRoles = async (row) => {
  if (row.username === 'root') {
    ElMessage.warning('root 用户拥有所有权限，无需分配角色')
    return
  }

  // 每次打开对话框时重新加载角色列表
  await loadRoles()

  currentUser.value = row
  selectedRoles.value = row.roles?.map(r => r.roleId) || []
  roleDialogVisible.value = true
}
```

**Step 3: 测试角色选择更新**

1. 在用户管理页面，记录当前角色数量
2. 打开权限组管理页面，新增一个角色
3. 返回用户管理页面，点击"分配角色"
4. 预期：新角色出现在角色选择列表中

**Step 4: 提交代码**

```bash
git add frontend/src/views/system/UserManagement.vue
git commit -m "fix: reload roles when opening assign roles dialog"
```

---

## Task 3: 修复删除权限组 500 错误（问题 3）

**描述:** 删除权限组时返回 500 错误。

**根本原因:** 根据 RoleService.cs:177-182，当角色下有用户时返回错误消息，但前端请求可能因为其他原因导致 500。需要检查后端日志。

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/RoleService.cs:167-187`
- Modify: `frontend/src/views/system/RoleManagement.vue:370-390`

**Step 1: 检查后端删除角色逻辑**

查看 `RoleService.DeleteRoleAsync`:
```csharp
public async Task<ApiResponse> DeleteRoleAsync(int roleId)
{
    var role = await _context.Roles
        .FirstOrDefaultAsync(r => r.RoleId == roleId && !r.IsSystem);

    if (role == null)
    {
        return ApiResponse.Fail("角色不存在或系统角色不可删除", "NOT_FOUND");
    }

    // 检查是否有用户使用此角色
    var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
    if (hasUsers)
    {
        return ApiResponse.Fail("该角色下还有用户，无法删除", "ROLE_IN_USE");
    }

    _context.Roles.Remove(role);
    await _context.SaveChangesAsync();
    return ApiResponse.Ok("角色删除成功");
}
```

问题：删除角色时没有处理 `RolePermissions` 关联数据。SQL Server 外键约束可能阻止删除。

**Step 2: 修改删除逻辑，先删除关联数据**

修改 `backend/src/DataForgeStudio.Core/Services/RoleService.cs`:

```csharp
public async Task<ApiResponse> DeleteRoleAsync(int roleId)
{
    var role = await _context.Roles
        .Include(r => r.RolePermissions)  // 加载权限关联
        .FirstOrDefaultAsync(r => r.RoleId == roleId && !r.IsSystem);

    if (role == null)
    {
        return ApiResponse.Fail("角色不存在或系统角色不可删除", "NOT_FOUND");
    }

    // 检查是否有用户使用此角色
    var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
    if (hasUsers)
    {
        return ApiResponse.Fail("该角色下还有用户，无法删除", "ROLE_IN_USE");
    }

    // 先删除角色权限关联
    if (role.RolePermissions.Any())
    {
        _context.RolePermissions.RemoveRange(role.RolePermissions);
    }

    // 删除角色
    _context.Roles.Remove(role);
    await _context.SaveChangesAsync();

    _logger.LogInformation($"角色删除成功: RoleId={roleId}, RoleName={role.RoleName}");
    return ApiResponse.Ok("角色删除成功");
}
```

**Step 3: 测试删除角色**

1. 创建一个测试角色
2. 确保没有用户使用该角色
3. 删除该角色
4. 预期：删除成功，返回 200

**Step 4: 提交代码**

```bash
git add backend/src/DataForgeStudio.Core/Services/RoleService.cs
git commit -m "fix: delete role permissions before deleting role to avoid foreign key constraint error"
```

---

## Task 4: 调整数据源窗口布局（问题 4）

**描述:** 新增数据源窗口中，将"选择数据库名"下移到"密码"下方。

**Files:**
- Modify: `frontend/src/views/system/DataSourceManagement.vue` (对话框部分)

**Step 1: 查找数据源对话框表单**

定位数据源编辑对话框，找到表单项顺序。

**Step 2: 调整表单项顺序**

将数据库选择项移到密码项之后。假设当前顺序是：服务器地址 -> 数据库名 -> 用户名 -> 密码

调整为：服务器地址 -> 用户名 -> 密码 -> 数据库名

```vue
<el-form-item label="服务器地址" prop="serverAddress">
  <el-input v-model="form.serverAddress" placeholder="请输入服务器地址" />
</el-form-item>
<el-form-item label="端口号" prop="port">
  <el-input-number v-model="form.port" :min="1" :max="65535" />
</el-form-item>
<el-form-item label="用户名" prop="username">
  <el-input v-model="form.username" placeholder="请输入用户名" />
</el-form-item>
<el-form-item label="密码" prop="password">
  <el-input v-model="form.password" type="password" placeholder="请输入密码" show-password />
</el-form-item>
<el-form-item label="数据库名" prop="databaseName">
  <el-select v-model="form.databaseName" placeholder="请选择数据库" filterable>
    <el-option
      v-for="db in databaseList"
      :key="db"
      :label="db"
      :value="db"
    />
  </el-select>
</el-form-item>
```

**Step 3: 测试表单布局**

1. 点击"新增数据源"
2. 检查表单项顺序是否正确

**Step 4: 提交代码**

```bash
git add frontend/src/views/system/DataSourceManagement.vue
git commit -m "ui: reorder datasource form fields, move database name after password"
```

---

## Task 5: 实现日志导出功能（问题 5）

**描述:** 日志管理页面需要支持导出为 Excel。

**Files:**
- Modify: `frontend/src/views/system/LogManagement.vue`
- Modify: `frontend/src/api/request.js` (添加导出 API)
- Modify: `backend/src/DataForgeStudio.Api/Controllers/SystemController.cs`
- Modify: `backend/src/DataForgeStudio.Core/Services/SystemService.cs`

**Step 1: 后端添加导出 API**

在 `backend/src/DataForgeStudio.Core/Interfaces/ISystemService.cs` 添加接口：

```csharp
Task<byte[]> ExportLogsToExcelAsync(
    string? username = null,
    string? action = null,
    string? module = null,
    string? startTime = null,
    string? endTime = null);
```

在 `backend/src/DataForgeStudio.Core/Services/SystemService.cs` 实现导出：

```csharp
public async Task<byte[]> ExportLogsToExcelAsync(
    string? username = null,
    string? action = null,
    string? module = null,
    string? startTime = null,
    string? endTime = null)
{
    var query = _context.OperationLogs.AsQueryable();

    // 应用相同的过滤条件
    if (!string.IsNullOrWhiteSpace(username))
    {
        query = query.Where(l => l.Username.Contains(username));
    }

    if (!string.IsNullOrWhiteSpace(action))
    {
        query = query.Where(l => l.Action == action);
    }

    if (!string.IsNullOrWhiteSpace(module))
    {
        query = query.Where(l => l.Module == module);
    }

    if (DateTime.TryParse(startTime, out var start))
    {
        query = query.Where(l => l.CreatedTime >= start);
    }

    if (DateTime.TryParse(endTime, out var end))
    {
        query = query.Where(l => l.CreatedTime <= end);
    }

    var logs = await query
        .OrderByDescending(l => l.CreatedTime)
        .ToListAsync();

    // 使用 ClosedXML 导出 Excel
    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var worksheet = workbook.Worksheets.Add("操作日志");

    // 设置表头
    worksheet.Cell("A1").Value = "用户名";
    worksheet.Cell("B1").Value = "操作";
    worksheet.Cell("C1").Value = "模块";
    worksheet.Cell("D1").Value = "描述";
    worksheet.Cell("E1").Value = "IP地址";
    worksheet.Cell("F1").Value = "操作时间";

    // 填充数据
    int row = 2;
    foreach (var log in logs)
    {
        worksheet.Cell(row, 1).Value = log.Username;
        worksheet.Cell(row, 2).Value = log.Action;
        worksheet.Cell(row, 3).Value = log.Module;
        worksheet.Cell(row, 4).Value = log.Description;
        worksheet.Cell(row, 5).Value = log.IpAddress;
        worksheet.Cell(row, 6).Value = log.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss");
        row++;
    }

    // 设置表格样式
    var range = worksheet.Range(1, 1, row - 1, 6);
    range.CreateTable().Theme = ClosedXML.Excel.Table.TableThemes.Medium2;

    worksheet.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    return stream.ToArray();
}
```

**Step 2: 添加控制器端点**

在 `backend/src/DataForgeStudio.Api/Controllers/SystemController.cs` 添加：

```csharp
/// <summary>
/// 导出操作日志到 Excel
/// </summary>
[HttpGet("logs/export")]
public async Task<IActionResult> ExportLogs(
    [FromQuery] string? username = null,
    [FromQuery] string? action = null,
    [FromQuery] string? module = null,
    [FromQuery] string? startTime = null,
    [FromQuery] string? endTime = null)
{
    var excelData = await _systemService.ExportLogsToExcelAsync(
        username, action, module, startTime, endTime);

    return File(
        excelData,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"操作日志_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
}
```

**Step 3: 前端添加导出 API**

在 `frontend/src/api/request.js` 添加：

```javascript
// 导出日志
export const exportLogs = (params) => {
  return request({
    url: '/api/system/logs/export',
    method: 'GET',
    params,
    responseType: 'blob'
  })
}
```

**Step 4: 前端页面添加导出按钮**

在 `frontend/src/views/system/LogManagement.vue` 添加导出功能：

```vue
<template>
  <!-- 在搜索表单区域添加导出按钮 -->
  <el-form :inline="true" :model="searchForm" class="search-form">
    <!-- ... 现有搜索项 ... -->
    <el-form-item>
      <el-button type="primary" @click="handleSearch">查询</el-button>
      <el-button @click="handleReset">重置</el-button>
      <el-button type="success" @click="handleExport" :loading="exporting">
        <el-icon><Download /></el-icon>
        导出Excel
      </el-button>
    </el-form-item>
  </el-form>
</template>

<script setup>
const exporting = ref(false)

const handleExport = async () => {
  exporting.value = true
  try {
    const blob = await systemApi.exportLogs(searchForm)

    // 创建下载链接
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `操作日志_${new Date().getTime()}.xlsx`
    link.click()
    window.URL.revokeObjectURL(url)

    ElMessage.success('导出成功')
  } catch (error) {
    console.error('导出失败:', error)
    ElMessage.error('导出失败')
  } finally {
    exporting.value = false
  }
}
</script>
```

**Step 5: 测试导出功能**

1. 在日志管理页面添加一些测试日志
2. 点击"导出 Excel"按钮
3. 验证下载的文件可以正常打开

**Step 6: 提交代码**

```bash
git add backend/src/DataForgeStudio.Core/Services/SystemService.cs
git add backend/src/DataForgeStudio.Core/Interfaces/ISystemService.cs
git add backend/src/DataForgeStudio.Api/Controllers/SystemController.cs
git add frontend/src/api/request.js
git add frontend/src/views/system/LogManagement.vue
git commit -m "feat: add operation log export to Excel functionality"
```

---

## Task 6: 改进备份管理功能（问题 6）

**描述:** 备份管理不需要输入备份名称，默认使用 `DataForge_日期时间.bak` 格式。

**Files:**
- Modify: `frontend/src/views/system/BackupManagement.vue:10-23`
- Modify: `backend/src/DataForgeStudio.Core/Services/SystemService.cs:197-288`

**Step 1: 简化前端表单**

修改 `frontend/src/views/system/BackupManagement.vue`，移除备份名称输入框：

```vue
<el-form :model="backupForm" ref="backupFormRef" :inline="true">
  <el-form-item label="备注">
    <el-input v-model="backupForm.description" placeholder="请输入备注（可选）" style="width: 300px;" />
  </el-form-item>
  <el-form-item>
    <el-button type="primary" @click="handleCreateBackup" :loading="creating">
      <el-icon><Plus /></el-icon>
      创建备份
    </el-button>
  </el-form-item>
</el-form>
```

同时修改表单验证规则，移除 backupName 的必填验证。

**Step 2: 修改后端自动生成备份名称**

修改 `backend/src/DataForgeStudio.Core/Services/SystemService.cs`:

```csharp
public async Task<ApiResponse<BackupRecordDto>> CreateBackupAsync(CreateBackupRequest request, int createdBy)
{
    try
    {
        // 获取数据库连接字符串
        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            return ApiResponse<BackupRecordDto>.Fail("无法获取数据库连接字符串", "CONNECTION_STRING_ERROR");
        }

        // 解析连接字符串获取数据库名称
        var databaseName = ExtractDatabaseName(connectionString);
        if (string.IsNullOrEmpty(databaseName))
        {
            return ApiResponse<BackupRecordDto>.Fail("无法获取数据库名称", "DATABASE_NAME_ERROR");
        }

        // 创建备份目录
        var backupDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
        if (!System.IO.Directory.Exists(backupDir))
        {
            System.IO.Directory.CreateDirectory(backupDir);
        }

        // 自动生成备份名称和文件名
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupName = $"DataForge_{timestamp}";
        var fileName = $"{databaseName}_{timestamp}.bak";
        var backupPath = System.IO.Path.Combine(backupDir, fileName);

        // 执行备份命令
        var backupSuccess = await ExecuteBackupCommand(connectionString, databaseName, backupPath);

        if (!backupSuccess)
        {
            var failedBackup = new BackupRecord
            {
                BackupName = backupName,
                BackupType = "Manual",
                BackupPath = backupPath,
                FileSize = 0,
                BackupTime = DateTime.UtcNow,
                IsSuccess = false,
                CreatedBy = createdBy,
                CreatedTime = DateTime.UtcNow
            };

            _context.BackupRecords.Add(failedBackup);
            await _context.SaveChangesAsync();

            return ApiResponse<BackupRecordDto>.Fail("数据库备份失败", "BACKUP_FAILED");
        }

        // 获取文件大小
        var fileInfo = new System.IO.FileInfo(backupPath);
        var fileSize = fileInfo.Length;

        // 记录备份信息
        var backup = new BackupRecord
        {
            BackupName = backupName,
            BackupType = "Manual",
            BackupPath = backupPath,
            FileSize = (int)fileSize,
            BackupTime = DateTime.UtcNow,
            IsSuccess = true,
            CreatedBy = createdBy,
            CreatedTime = DateTime.UtcNow
        };

        _context.BackupRecords.Add(backup);
        await _context.SaveChangesAsync();

        var backupDto = new BackupRecordDto
        {
            BackupId = backup.BackupId,
            BackupName = backup.BackupName,
            FileName = fileName,
            FileSize = backup.FileSize,
            Description = request.Description,
            CreatedBy = createdBy.ToString(),
            CreatedTime = backup.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss")
        };

        _logger.LogInformation($"数据库备份成功: {backupPath}, 大小: {fileSize} 字节");
        return ApiResponse<BackupRecordDto>.Ok(backupDto, "备份创建成功");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建数据库备份失败");
        return ApiResponse<BackupRecordDto>.Fail($"创建备份失败: {ex.Message}", "BACKUP_ERROR");
    }
}
```

**Step 3: 修改 CreateBackupRequest DTO**

确保 `BackupName` 属性不是必填的：

```csharp
public class CreateBackupRequest
{
    public string? BackupName { get; set; }  // 改为可空
    public string? Description { get; set; }
}
```

**Step 4: 测试备份创建**

1. 在备份管理页面，不输入任何名称，直接点击"创建备份"
2. 预期：成功创建备份，文件名格式为 `DataForge_20250205_143000.bak`

**Step 5: 提交代码**

```bash
git add backend/src/DataForgeStudio.Core/Services/SystemService.cs
git add backend/src/DataForgeStudio.Shared/DTO/CreateBackupRequest.cs
git add frontend/src/views/system/BackupManagement.vue
git commit -m "feat: auto-generate backup name with timestamp, simplify backup creation form"
```

---

## Task 7: 修复报表设计页面 searchKeymap 错误（问题 7）

**描述:** 进入报表设计页面报错：`searchKeymap.of is not a function`

**根本原因:** CodeMirror 6 的 `@codemirror/search` 包版本 6.6.0 中，`searchKeymap` 的导出方式发生了变化。`searchKeymap` 不再是一个函数，而是一个配置对象或被移除了 `of` 方法。

**Files:**
- Modify: `frontend/src/components/SqlEditor.vue:85-129`

**Step 1: 检查 searchKeymap 的正确用法**

查看 `@codemirror/search` 包的源码或文档。在新版本中，`searchKeymap` 可能是一个 `keymap` 数组而不是需要用 `.of()` 包装的对象。

**Step 2: 修复 SqlEditor.vue 中的扩展配置**

修改 `frontend/src/components/SqlEditor.vue`:

```javascript
// 创建编辑器扩展
const getExtensions = () => {
  const extensions = [
    lineNumbers(),
    highlightActiveLineGutter(),
    highlightSpecialChars(),
    highlightActiveLine(),
    drawSelection(),
    dropCursor(),
    bracketMatching(),
    rectangularSelection(),
    sql(),
    keymap.of([
      ...defaultKeymap,
      indentWithTab,
      { key: 'Shift-Alt-f', run: formatSQL }
    ]),
    // 修复：直接使用 searchKeymap，不用 .of()
    ...(Array.isArray(searchKeymap) ? searchKeymap : [searchKeymap]),
    // 或者完全移除 searchKeymap，使用默认的搜索功能
    // searchKeymap 已经包含在默认配置中
    highlightSelectionMatches(),
    // 使用自定义 SQL 自动补全
    createSqlAutocomplete(fetchTablesFromBackend, props.dataSourceId),
    sqlLinter,
    EditorView.updateListener.of((update) => {
      if (update.docChanged) {
        const content = update.state.doc.toString()
        emit('update:modelValue', content)
        emit('change', content)
      }
    }),
    EditorView.theme({
      '&': {
        height: '100%',
        fontSize: '14px'
      },
      '.cm-scroller': {
        overflow: 'auto',
        height: '100%'
      },
      '.cm-content': {
        padding: '10px'
      },
      '.cm-focused': {
        outline: 'none'
      }
    })
  ]

  // 添加主题
  if (props.theme === 'dark') {
    extensions.push(oneDark)
  }

  // 添加只读模式
  if (props.readOnly) {
    extensions.push(EditorState.readOnly.of(true))
  }

  return extensions
}
```

或者更简单的方式，直接移除 `searchKeymap.of(searchKeymap)` 这一行，因为搜索功能已经默认包含在 CodeMirror 中：

```javascript
const getExtensions = () => {
  const extensions = [
    lineNumbers(),
    highlightActiveLineGutter(),
    highlightSpecialChars(),
    highlightActiveLine(),
    drawSelection(),
    dropCursor(),
    bracketMatching(),
    rectangularSelection(),
    sql(),
    keymap.of([
      ...defaultKeymap,
      indentWithTab,
      { key: 'Shift-Alt-f', run: formatSQL }
    ]),
    // 移除有问题的 searchKeymap
    highlightSelectionMatches(),
    createSqlAutocomplete(fetchTablesFromBackend, props.dataSourceId),
    sqlLinter,
    // ... 其余配置
  ]

  return extensions
}
```

**Step 3: 更新导入语句**

同时检查并更新导入：

```javascript
// 移除 searchKeymap 的导入，如果不再使用
// import { searchKeymap, highlightSelectionMatches } from '@codemirror/search'
import { highlightSelectionMatches } from '@codemirror/search'
```

**Step 4: 测试 SQL 编辑器**

1. 访问报表设计页面
2. 验证 SQL 编辑器正常加载
3. 测试搜索功能（Ctrl+F 或 Cmd+F）

**Step 5: 提交代码**

```bash
git add frontend/src/components/SqlEditor.vue
git commit -m "fix: remove searchKeymap.of() call causing 'is not a function' error"
```

---

## 执行顺序建议

1. **高优先级 Bug 先修复**: Task 1 → Task 3 → Task 7
2. **中等优先级**: Task 2 → Task 5 → Task 6
3. **低优先级**: Task 4

## 测试检查清单

- [ ] 访问根路径，无 token 时直接进入登录页
- [ ] 访问根路径，有有效 token 时直接进入首页
- [ ] 新增权限组后，用户管理的角色选择立即显示新角色
- [ ] 删除无用户的角色成功
- [ ] 删除有用户的角色返回友好错误提示
- [ ] 数据源表单字段顺序正确
- [ ] 日志可以成功导出为 Excel
- [ ] 备份创建不需要输入名称，自动生成
- [ ] 报表设计页面 SQL 编辑器正常加载

---

## 备注

- 所有提交使用中文 commit message
- 每完成一个 Task 后提交一次代码
- 遇到问题先查看控制台错误日志和后端日志
