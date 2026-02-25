# fix3.md 六个问题修复实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复 fix3.md 中报告的 6 个功能问题

**Architecture:** 分别修复后端服务、前端组件和安装脚本中的 bug，确保数据流、状态管理和服务启动正确

**Tech Stack:** ASP.NET Core 8.0, Vue 3, Element Plus, Inno Setup 6

---

## Task 1: 修复安装脚本 - 添加 Web 服务启动命令

**Files:**
- Modify: `installer/setup.iss:100-106`

**Step 1: 添加 DFWebService 启动命令**

在 `setup.iss` 的 `[Run]` 部分，在启动 DFAppService 之后添加启动 DFWebService 的命令：

```inno
[Run]
; 安装完成后运行配置器
Filename: "{tmp}\configurator\Configurator.exe"; Parameters: "install --install-path ""{app}"" --db-server ""{code:GetDbServer}"" --db-port {code:GetDbPort} --db-auth ""{code:GetDbAuth}"" --db-user ""{code:GetDbUser}"" --db-password ""{code:GetDbPassword}"" --backend-port {code:GetBackendPort} --frontend-port {code:GetFrontendPort}"; Flags: waituntilterminated; StatusMsg: "正在配置系统..."
; 安装完成后自动启动 API 服务
Filename: "{app}\Manager\nssm.exe"; Parameters: "start DFAppService"; Flags: runhidden waituntilterminated; StatusMsg: "正在启动 DataForgeStudio API 服务..."
; 安装完成后自动启动 Web 服务
Filename: "{app}\Manager\nssm.exe"; Parameters: "start DFWebService"; Flags: runhidden waituntilterminated; StatusMsg: "正在启动 DataForgeStudio Web 服务..."
; 可选：打开管理界面
Filename: "http://localhost:{code:GetFrontendPort}"; Flags: shellexec postinstall skipifsilent nowait; Description: "打开 DataForgeStudio 管理界面"
```

**Step 2: 提交**

```bash
git add installer/setup.iss
git commit -m "fix: add DFWebService startup command in installer"
```

---

## Task 2: 修复 Configurator - 更新 NSSM 路径

**Files:**
- Modify: `backend/tools/Configurator/Program.cs:1248-1260`

**Step 1: 更新 RegisterWebService 方法中的 NSSM 路径**

找到 `RegisterWebService` 方法（约第 1248 行），修改 NSSM 路径：

```csharp
static void RegisterWebService(Configuration config)
{
    var serviceName = "DFWebService";
    var nginxExePath = Path.Combine(config.InstallPath, "WebServer", "nginx.exe");
    // 更新 NSSM 路径：从 tools\nssm 改为 manager
    var nssmPath = Path.Combine(config.InstallPath, "Manager", "nssm.exe");

    // 检查 NSSM 是否存在
    if (!File.Exists(nssmPath))
    {
        Console.WriteLine("  警告: NSSM 未找到，跳过 Web 服务注册");
        return;
    }
    // ... 其余代码保持不变
}
```

**Step 2: 提交**

```bash
git add backend/tools/Configurator/Program.cs
git commit -m "fix: update NSSM path in Configurator (tools -> manager)"
```

---

## Task 3: 修复备份服务 - 使用计划的备份路径

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Services/BackupBackgroundService.cs:88-100`

**Step 1: 修改 ExecuteScheduleAsync 方法使用 schedule.BackupPath**

找到 `ExecuteScheduleAsync` 方法中创建备份目录的部分（约第 88-100 行），修改为：

```csharp
private async Task ExecuteScheduleAsync(DataForgeStudioDbContext context, BackupSchedule schedule)
{
    _logger.LogInformation("开始执行备份计划: {ScheduleName}", schedule.ScheduleName);

    try
    {
        // 获取数据库连接字符串
        var connectionString = context.Database.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("无法获取数据库连接字符串");
            return;
        }

        var databaseName = ExtractDatabaseName(connectionString);
        if (string.IsNullOrEmpty(databaseName))
        {
            _logger.LogError("无法获取数据库名称");
            return;
        }

        // 使用计划配置的备份路径，若未配置则使用默认路径
        string backupDir;
        if (!string.IsNullOrWhiteSpace(schedule.BackupPath))
        {
            backupDir = schedule.BackupPath;
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }
        }
        else
        {
            backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupName = $"{schedule.ScheduleName}_{timestamp}";
        var fileName = $"{databaseName}_{timestamp}.bak";
        var backupPath = Path.Combine(backupDir, fileName);

        // ... 其余代码保持不变
    }
}
```

**Step 2: 提交**

```bash
git add backend/src/DataForgeStudio.Api/Services/BackupBackgroundService.cs
git commit -m "fix: use schedule.BackupPath in BackupBackgroundService"
```

---

## Task 4: 修复备份备注不保存

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/SystemService.cs:385-398`

**Step 1: 在创建 BackupRecord 时添加 Description 字段**

找到 `CreateBackupAsync` 方法中创建 `BackupRecord` 的部分（约第 385-398 行），添加 Description：

```csharp
// 记录备份信息
var backup = new BackupRecord
{
    BackupName = backupName,
    BackupType = "Manual",
    BackupPath = backupPath,
    FileSize = (int)fileSize,
    BackupTime = DateTime.UtcNow,
    IsSuccess = true,
    Description = request.Description,  // 添加这行：保存备注
    CreatedBy = createdBy,
    CreatedTime = DateTime.UtcNow
};

_context.BackupRecords.Add(backup);
await _context.SaveChangesAsync();
```

**Step 2: 提交**

```bash
git add backend/src/DataForgeStudio.Core/Services/SystemService.cs
git commit -m "fix: save backup description in CreateBackupAsync"
```

---

## Task 5: 修复报表列表数据源不显示 - 后端

**Files:**
- Modify: `backend/src/DataForgeStudio.Domain/DTOs/ReportDto.cs`
- Modify: `backend/src/DataForgeStudio.Core/Services/ReportService.cs:60-77`

**Step 1: 在 ReportDto 中添加 DataSourceName 属性**

```csharp
// backend/src/DataForgeStudio.Domain/DTOs/ReportDto.cs
namespace DataForgeStudio.Domain.DTOs;

public class ReportDto
{
    public int ReportId { get; set; }
    public string ReportName { get; set; } = string.Empty;
    public string? ReportCategory { get; set; }
    public int? DataSourceId { get; set; }
    public string? DataSourceName { get; set; }  // 添加这行
    public string? Description { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewTime { get; set; }
    public DateTime CreatedTime { get; set; }
    public bool IsEnabled { get; set; }
}
```

**Step 2: 修改 GetReportsAsync 方法返回 DataSourceName**

```csharp
// backend/src/DataForgeStudio.Core/Services/ReportService.cs
public async Task<ApiResponse<PagedResponse<ReportDto>>> GetReportsAsync(PagedRequest request, string? reportName = null, string? category = null)
{
    var query = _context.Reports.AsQueryable();

    if (!string.IsNullOrWhiteSpace(reportName))
    {
        query = query.Where(r => r.ReportName.Contains(reportName));
    }

    if (!string.IsNullOrWhiteSpace(category))
    {
        query = query.Where(r => r.ReportCategory == category);
    }

    var totalCount = await query.CountAsync();

    var reports = await query
        .Include(r => r.DataSource)  // 确保 Include DataSource
        .OrderByDescending(r => r.CreatedTime)
        .Skip((request.PageIndex - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(r => new ReportDto
        {
            ReportId = r.ReportId,
            ReportName = r.ReportName,
            ReportCategory = r.ReportCategory,
            DataSourceId = r.DataSourceId,
            DataSourceName = r.DataSource != null ? r.DataSource.Name : null,  // 添加这行
            Description = r.Description,
            ViewCount = r.ViewCount,
            LastViewTime = r.LastViewTime,
            CreatedTime = r.CreatedTime,
            IsEnabled = r.IsEnabled
        })
        .ToListAsync();

    var pagedResponse = new PagedResponse<ReportDto>(reports, totalCount, request.PageIndex, request.PageSize);
    return ApiResponse<PagedResponse<ReportDto>>.Ok(pagedResponse);
}
```

**Step 3: 提交**

```bash
git add backend/src/DataForgeStudio.Domain/DTOs/ReportDto.cs backend/src/DataForgeStudio.Core/Services/ReportService.cs
git commit -m "fix: add DataSourceName to ReportDto and GetReportsAsync"
```

---

## Task 6: 修复报表页面数据不刷新

**Files:**
- Modify: `frontend/src/views/report/ReportQuery.vue`
- Modify: `frontend/src/views/report/ReportDesignList.vue`

**Step 1: 在 ReportQuery.vue 中添加 onActivated 刷新**

在 `<script setup>` 部分添加 `onActivated` 钩子：

```vue
<script setup>
import { ref, reactive, onMounted, onActivated, computed, nextTick } from 'vue'
// ... 其他导入

// 现有的 onMounted
onMounted(async () => {
  await loadReports()
  adjustLayout()
  window.addEventListener('resize', adjustLayout)
})

// 添加 onActivated - 每次组件激活时刷新数据
onActivated(async () => {
  await loadReports()
})

// ... 其余代码
</script>
```

**Step 2: 在 ReportDesignList.vue 中添加 onActivated 刷新**

```vue
<script setup>
import { ref, reactive, onMounted, onActivated, onUnmounted, nextTick } from 'vue'
// ... 其他导入

// 现有的 onMounted
onMounted(async () => {
  await loadCategories()
  await loadData()
  await nextTick()
  calculateTableHeight()
  window.addEventListener('resize', calculateTableHeight)
})

// 添加 onActivated - 每次组件激活时刷新数据
onActivated(async () => {
  await loadData()
})

// ... 其余代码
</script>
```

**Step 3: 提交**

```bash
git add frontend/src/views/report/ReportQuery.vue frontend/src/views/report/ReportDesignList.vue
git commit -m "fix: refresh data on page activation (keep-alive fix)"
```

---

## Task 7: 修复密码验证问题

**Files:**
- Modify: `frontend/src/App.vue:248-268`

**Step 1: 修改密码验证规则**

将 `passwordRules` 改为在 `reactive` 对象外部定义验证函数，确保获取最新值：

```vue
<script setup>
// ... 其他代码

// 修改密码相关
const passwordDialogVisible = ref(false)
const passwordSubmitting = ref(false)
const passwordFormRef = ref()
const passwordForm = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

// 验证确认密码 - 使用函数确保获取最新值
const validateConfirmPassword = (rule, value, callback) => {
  if (value !== passwordForm.newPassword) {
    callback(new Error('两次输入的密码不一致'))
  } else {
    callback()
  }
}

const passwordRules = {
  oldPassword: [{ required: true, message: '请输入旧密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 8, message: '密码长度至少8位', trigger: 'blur' },
    { pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/, message: '密码必须包含大小写字母和数字', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: ['blur', 'change'] }  // 添加 change 触发器
  ]
}

// ... 其余代码
</script>
```

**Step 2: 提交**

```bash
git add frontend/src/App.vue
git commit -m "fix: improve password validation reliability"
```

---

## Task 8: 构建并测试

**Step 1: 构建后端**

```bash
cd H:/DataForge
dotnet build backend/DataForgeStudio.sln
```

**Step 2: 构建前端**

```bash
cd H:/DataForge/frontend
npm run build
```

**Step 3: 构建安装包**

```bash
cd H:/DataForge/scripts
./build-installer.ps1
```

**Step 4: 最终提交（如有遗漏）**

```bash
git add -A
git commit -m "fix: complete fix3.md bug fixes"
git push origin master
```

---

## 文件变更摘要

| 文件 | 操作 | 任务 |
|------|------|------|
| `installer/setup.iss` | 修改 | Task 1 |
| `backend/tools/Configurator/Program.cs` | 修改 | Task 2 |
| `backend/src/DataForgeStudio.Api/Services/BackupBackgroundService.cs` | 修改 | Task 3 |
| `backend/src/DataForgeStudio.Core/Services/SystemService.cs` | 修改 | Task 4 |
| `backend/src/DataForgeStudio.Domain/DTOs/ReportDto.cs` | 修改 | Task 5 |
| `backend/src/DataForgeStudio.Core/Services/ReportService.cs` | 修改 | Task 5 |
| `frontend/src/views/report/ReportQuery.vue` | 修改 | Task 6 |
| `frontend/src/views/report/ReportDesignList.vue` | 修改 | Task 6 |
| `frontend/src/App.vue` | 修改 | Task 7 |
