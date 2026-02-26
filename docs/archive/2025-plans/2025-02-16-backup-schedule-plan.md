# 备份计划功能 实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在备份管理页面增加备份计划功能，支持重复计划和单次计划，自动执行备份并清理旧备份。

**Architecture:** 新增 BackupSchedule 实体存储计划配置，创建 BackupBackgroundService 后台服务每分钟检查并执行到期的计划，执行后自动清理超过保留数量的旧备份。

**Tech Stack:** ASP.NET Core 8.0 BackgroundService, Vue 3, Element Plus, SQL Server

---

## Task 1: 新增 BackupSchedule 实体

**Files:**
- Modify: `backend/src/DataForgeStudio.Domain/Entities/System.cs`

**Step 1: 在 System.cs 末尾添加 BackupSchedule 实体**

```csharp
/// <summary>
/// 备份计划表
/// </summary>
[Table("BackupSchedules")]
public class BackupSchedule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ScheduleId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ScheduleName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ScheduleType { get; set; } = "Recurring"; // "Recurring" or "Once"

    /// <summary>
    /// 重复计划的执行日期，逗号分隔的数字
    /// 0=周日, 1=周一, ..., 6=周六
    /// 例如: "1,3,5" 表示周一、周三、周五
    /// </summary>
    [MaxLength(50)]
    public string? RecurringDays { get; set; }

    /// <summary>
    /// 执行时间（时:分）
    /// </summary>
    [MaxLength(10)]
    public string? ScheduledTime { get; set; }

    /// <summary>
    /// 单次计划的执行日期时间
    /// </summary>
    public DateTime? OnceDate { get; set; }

    /// <summary>
    /// 保留备份数量
    /// </summary>
    public int RetentionCount { get; set; } = 10;

    public bool IsEnabled { get; set; } = true;

    public DateTime? LastRunTime { get; set; }

    public DateTime? NextRunTime { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedTime { get; set; }
}
```

**Step 2: Commit**

```bash
git add backend/src/DataForgeStudio.Domain/Entities/System.cs
git commit -m "feat: add BackupSchedule entity"
```

---

## Task 2: 更新 DbContext 添加 DbSet

**Files:**
- Modify: `backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs`

**Step 1: 添加 DbSet**

在 DbContext 类中添加:
```csharp
public DbSet<BackupSchedule> BackupSchedules { get; set; }
```

**Step 2: 添加实体配置**

在 `OnModelCreating` 方法中添加:
```csharp
// 配置 BackupSchedule
modelBuilder.Entity<BackupSchedule>(entity =>
{
    entity.HasIndex(e => e.NextRunTime);
    entity.HasIndex(e => e.IsEnabled);
});
```

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs
git commit -m "feat: add BackupSchedules DbSet"
```

---

## Task 3: 创建数据库迁移

**Files:**
- Create: `backend/src/DataForgeStudio.Data/Migrations/xxx_AddBackupSchedules.cs`

**Step 1: 停止后端服务（如果在运行）**

**Step 2: 创建迁移**

```bash
cd backend
dotnet ef migrations add AddBackupSchedules --project src/DataForgeStudio.Data --startup-project src/DataForgeStudio.Api
```

**Step 3: 检查迁移文件**

确认生成的迁移文件正确创建了 BackupSchedules 表。

**Step 4: Commit**

```bash
git add backend/src/DataForgeStudio.Data/Migrations/
git commit -m "feat: add BackupSchedules migration"
```

---

## Task 4: 新增 DTO

**Files:**
- Modify: `backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs`

**Step 1: 在文件末尾添加 DTO**

```csharp
/// <summary>
/// 备份计划 DTO
/// </summary>
public class BackupScheduleDto
{
    public int ScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = string.Empty;
    public List<int> RecurringDays { get; set; } = new();
    public string? ScheduledTime { get; set; }
    public DateTime? OnceDate { get; set; }
    public int RetentionCount { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastRunTime { get; set; }
    public DateTime? NextRunTime { get; set; }
    public string CreatedTime { get; set; } = string.Empty;
}

/// <summary>
/// 创建/更新备份计划请求
/// </summary>
public class CreateBackupScheduleRequest
{
    public string ScheduleName { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = "Recurring";
    public List<int>? RecurringDays { get; set; }
    public string? ScheduledTime { get; set; }
    public DateTime? OnceDate { get; set; }
    public int RetentionCount { get; set; } = 10;
    public bool IsEnabled { get; set; } = true;
}
```

**Step 2: Commit**

```bash
git add backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs
git commit -m "feat: add BackupSchedule DTOs"
```

---

## Task 5: 扩展 ISystemService 接口

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Interfaces/ISystemService.cs`

**Step 1: 添加备份计划相关方法**

```csharp
/// <summary>
/// 获取备份计划列表
/// </summary>
Task<ApiResponse<List<BackupScheduleDto>>> GetBackupSchedulesAsync();

/// <summary>
/// 创建备份计划
/// </summary>
Task<ApiResponse<BackupScheduleDto>> CreateBackupScheduleAsync(CreateBackupScheduleRequest request);

/// <summary>
/// 更新备份计划
/// </summary>
Task<ApiResponse<BackupScheduleDto>> UpdateBackupScheduleAsync(int scheduleId, CreateBackupScheduleRequest request);

/// <summary>
/// 删除备份计划
/// </summary>
Task<ApiResponse> DeleteBackupScheduleAsync(int scheduleId);

/// <summary>
/// 切换备份计划启用状态
/// </summary>
Task<ApiResponse> ToggleBackupScheduleAsync(int scheduleId);
```

**Step 2: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Interfaces/ISystemService.cs
git commit -m "feat: add backup schedule methods to ISystemService"
```

---

## Task 6: 实现 SystemService 中的备份计划方法

**Files:**
- Modify: `backend/src/DataForgeStudio.Core/Services/SystemService.cs`

**Step 1: 添加 using 语句**

```csharp
using System.Globalization;
```

**Step 2: 实现 GetBackupSchedulesAsync**

```csharp
public async Task<ApiResponse<List<BackupScheduleDto>>> GetBackupSchedulesAsync()
{
    var schedules = await _context.BackupSchedules
        .OrderByDescending(s => s.CreatedTime)
        .Select(s => new BackupScheduleDto
        {
            ScheduleId = s.ScheduleId,
            ScheduleName = s.ScheduleName,
            ScheduleType = s.ScheduleType,
            RecurringDays = string.IsNullOrEmpty(s.RecurringDays)
                ? new List<int>()
                : s.RecurringDays.Split(',').Select(int.Parse).ToList(),
            ScheduledTime = s.ScheduledTime,
            OnceDate = s.OnceDate,
            RetentionCount = s.RetentionCount,
            IsEnabled = s.IsEnabled,
            LastRunTime = s.LastRunTime,
            NextRunTime = s.NextRunTime,
            CreatedTime = s.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss")
        })
        .ToListAsync();

    return ApiResponse<List<BackupScheduleDto>>.Ok(schedules);
}
```

**Step 3: 实现 CreateBackupScheduleAsync**

```csharp
public async Task<ApiResponse<BackupScheduleDto>> CreateBackupScheduleAsync(CreateBackupScheduleRequest request)
{
    var schedule = new BackupSchedule
    {
        ScheduleName = request.ScheduleName,
        ScheduleType = request.ScheduleType,
        RecurringDays = request.RecurringDays != null ? string.Join(",", request.RecurringDays) : null,
        ScheduledTime = request.ScheduledTime,
        OnceDate = request.OnceDate,
        RetentionCount = request.RetentionCount,
        IsEnabled = request.IsEnabled,
        CreatedTime = DateTime.UtcNow,
        NextRunTime = CalculateNextRunTime(request.ScheduleType,
            request.RecurringDays, request.ScheduledTime, request.OnceDate)
    };

    _context.BackupSchedules.Add(schedule);
    await _context.SaveChangesAsync();

    return await GetBackupScheduleDtoAsync(schedule.ScheduleId);
}
```

**Step 4: 实现 UpdateBackupScheduleAsync**

```csharp
public async Task<ApiResponse<BackupScheduleDto>> UpdateBackupScheduleAsync(int scheduleId, CreateBackupScheduleRequest request)
{
    var schedule = await _context.BackupSchedules.FindAsync(scheduleId);
    if (schedule == null)
    {
        return ApiResponse<BackupScheduleDto>.Fail("备份计划不存在", "NOT_FOUND");
    }

    schedule.ScheduleName = request.ScheduleName;
    schedule.ScheduleType = request.ScheduleType;
    schedule.RecurringDays = request.RecurringDays != null ? string.Join(",", request.RecurringDays) : null;
    schedule.ScheduledTime = request.ScheduledTime;
    schedule.OnceDate = request.OnceDate;
    schedule.RetentionCount = request.RetentionCount;
    schedule.IsEnabled = request.IsEnabled;
    schedule.UpdatedTime = DateTime.UtcNow;
    schedule.NextRunTime = CalculateNextRunTime(request.ScheduleType,
        request.RecurringDays, request.ScheduledTime, request.OnceDate);

    await _context.SaveChangesAsync();

    return await GetBackupScheduleDtoAsync(schedule.ScheduleId);
}
```

**Step 5: 实现 DeleteBackupScheduleAsync**

```csharp
public async Task<ApiResponse> DeleteBackupScheduleAsync(int scheduleId)
{
    var schedule = await _context.BackupSchedules.FindAsync(scheduleId);
    if (schedule == null)
    {
        return ApiResponse.Fail("备份计划不存在", "NOT_FOUND");
    }

    _context.BackupSchedules.Remove(schedule);
    await _context.SaveChangesAsync();

    return ApiResponse.Ok("删除成功");
}
```

**Step 6: 实现 ToggleBackupScheduleAsync**

```csharp
public async Task<ApiResponse> ToggleBackupScheduleAsync(int scheduleId)
{
    var schedule = await _context.BackupSchedules.FindAsync(scheduleId);
    if (schedule == null)
    {
        return ApiResponse.Fail("备份计划不存在", "NOT_FOUND");
    }

    schedule.IsEnabled = !schedule.IsEnabled;
    schedule.UpdatedTime = DateTime.UtcNow;

    if (schedule.IsEnabled)
    {
        var days = string.IsNullOrEmpty(schedule.RecurringDays)
            ? null
            : schedule.RecurringDays.Split(',').Select(int.Parse).ToList();
        schedule.NextRunTime = CalculateNextRunTime(schedule.ScheduleType,
            days, schedule.ScheduledTime, schedule.OnceDate);
    }

    await _context.SaveChangesAsync();

    return ApiResponse.Ok(schedule.IsEnabled ? "已启用" : "已禁用");
}
```

**Step 7: 添加辅助方法**

```csharp
private async Task<ApiResponse<BackupScheduleDto>> GetBackupScheduleDtoAsync(int scheduleId)
{
    var schedule = await _context.BackupSchedules.FindAsync(scheduleId);
    if (schedule == null)
    {
        return ApiResponse<BackupScheduleDto>.Fail("备份计划不存在", "NOT_FOUND");
    }

    var dto = new BackupScheduleDto
    {
        ScheduleId = schedule.ScheduleId,
        ScheduleName = schedule.ScheduleName,
        ScheduleType = schedule.ScheduleType,
        RecurringDays = string.IsNullOrEmpty(schedule.RecurringDays)
            ? new List<int>()
            : schedule.RecurringDays.Split(',').Select(int.Parse).ToList(),
        ScheduledTime = schedule.ScheduledTime,
        OnceDate = schedule.OnceDate,
        RetentionCount = schedule.RetentionCount,
        IsEnabled = schedule.IsEnabled,
        LastRunTime = schedule.LastRunTime,
        NextRunTime = schedule.NextRunTime,
        CreatedTime = schedule.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss")
    };

    return ApiResponse<BackupScheduleDto>.Ok(dto);
}

private DateTime? CalculateNextRunTime(string scheduleType, List<int>? recurringDays, string? scheduledTime, DateTime? onceDate)
{
    var now = DateTime.UtcNow;

    if (scheduleType == "Once" && onceDate.HasValue)
    {
        return onceDate.Value;
    }

    if (scheduleType == "Recurring" && recurringDays != null && recurringDays.Any() && !string.IsNullOrEmpty(scheduledTime))
    {
        // 解析执行时间
        var timeParts = scheduledTime.Split(':');
        var hour = int.Parse(timeParts[0]);
        var minute = timeParts.Length > 1 ? int.Parse(timeParts[1]) : 0;

        // 找到下一个执行日期
        for (int i = 0; i <= 7; i++)
        {
            var checkDate = now.Date.AddDays(i);
            var dayOfWeek = (int)checkDate.DayOfWeek;

            if (recurringDays.Contains(dayOfWeek))
            {
                var runTime = checkDate.AddHours(hour).AddMinutes(minute);
                if (runTime > now)
                {
                    return runTime;
                }
            }
        }
    }

    return null;
}
```

**Step 8: 验证编译**

```bash
dotnet build backend/src/DataForgeStudio.Core/DataForgeStudio.Core.csproj
```

**Step 9: Commit**

```bash
git add backend/src/DataForgeStudio.Core/Services/SystemService.cs
git commit -m "feat: implement backup schedule methods in SystemService"
```

---

## Task 7: 创建 BackupBackgroundService 后台服务

**Files:**
- Create: `backend/src/DataForgeStudio.Api/Services/BackupBackgroundService.cs`

**Step 1: 创建后台服务文件**

```csharp
using Microsoft.EntityFrameworkCore;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;

namespace DataForgeStudio.Api.Services;

/// <summary>
/// 备份计划后台服务
/// </summary>
public class BackupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackupBackgroundService> _logger;

    public BackupBackgroundService(IServiceProvider serviceProvider, ILogger<BackupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("备份计划后台服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndExecuteSchedulesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查备份计划时出错");
            }

            // 每分钟检查一次
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("备份计划后台服务已停止");
    }

    private async Task CheckAndExecuteSchedulesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataForgeStudioDbContext>();

        var now = DateTime.UtcNow;

        // 查找需要执行的备份计划
        var schedules = await context.BackupSchedules
            .Where(s => s.IsEnabled && s.NextRunTime.HasValue && s.NextRunTime.Value <= now)
            .ToListAsync();

        foreach (var schedule in schedules)
        {
            try
            {
                await ExecuteScheduleAsync(context, schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行备份计划 {ScheduleId} 失败", schedule.ScheduleId);
            }
        }
    }

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

            // 创建备份
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupName = $"{schedule.ScheduleName}_{timestamp}";
            var fileName = $"{databaseName}_{timestamp}.bak";
            var backupPath = Path.Combine(backupDir, fileName);

            var backupSuccess = await ExecuteBackupCommandAsync(connectionString, databaseName, backupPath);

            // 获取文件大小
            long? fileSize = null;
            if (backupSuccess && File.Exists(backupPath))
            {
                fileSize = new FileInfo(backupPath).Length;
            }

            // 记录备份
            var backupRecord = new BackupRecord
            {
                BackupName = backupName,
                BackupType = "Scheduled",
                BackupPath = backupPath,
                DatabaseName = databaseName,
                Description = $"计划备份: {schedule.ScheduleName}",
                FileSize = fileSize,
                BackupTime = DateTime.UtcNow,
                IsSuccess = backupSuccess,
                CreatedTime = DateTime.UtcNow
            };

            context.BackupRecords.Add(backupRecord);

            // 更新计划状态
            schedule.LastRunTime = DateTime.UtcNow;

            if (schedule.ScheduleType == "Once")
            {
                // 单次计划执行后禁用
                schedule.IsEnabled = false;
                schedule.NextRunTime = null;
            }
            else
            {
                // 重复计划计算下次执行时间
                var days = string.IsNullOrEmpty(schedule.RecurringDays)
                    ? null
                    : schedule.RecurringDays.Split(',').Select(int.Parse).ToList();
                schedule.NextRunTime = CalculateNextRunTime(schedule.ScheduleType, days, schedule.ScheduledTime, null);
            }

            await context.SaveChangesAsync();

            // 清理旧备份
            await CleanupOldBackupsAsync(context, schedule.RetentionCount);

            _logger.LogInformation("备份计划执行完成: {ScheduleName}, 成功: {Success}",
                schedule.ScheduleName, backupSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行备份计划失败: {ScheduleName}", schedule.ScheduleName);
        }
    }

    private async Task<bool> ExecuteBackupCommandAsync(string connectionString, string databaseName, string backupPath)
    {
        try
        {
            var backupSql = $@"
                USE [master];
                BACKUP DATABASE [{databaseName}]
                TO DISK = N'{backupPath.Replace("\\", "\\\\")}'
                WITH FORMAT,
                     MEDIANAME = N'{databaseName}_Backup',
                     NAME = N'Scheduled Backup of {databaseName}',
                     STATS = 10;
            ";

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new Microsoft.Data.SqlClient.SqlCommand(backupSql, connection)
            {
                CommandTimeout = 300
            };

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行备份命令失败");
            return false;
        }
    }

    private async Task CleanupOldBackupsAsync(DataForgeStudioDbContext context, int retentionCount)
    {
        // 获取所有成功的备份记录，按时间降序
        var backups = await context.BackupRecords
            .Where(b => b.IsSuccess)
            .OrderByDescending(b => b.BackupTime)
            .ToListAsync();

        // 删除超过保留数量的备份
        var toDelete = backups.Skip(retentionCount).ToList();

        foreach (var backup in toDelete)
        {
            try
            {
                // 删除文件
                if (File.Exists(backup.BackupPath))
                {
                    File.Delete(backup.BackupPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "删除备份文件失败: {Path}", backup.BackupPath);
            }

            context.BackupRecords.Remove(backup);
        }

        if (toDelete.Any())
        {
            await context.SaveChangesAsync();
            _logger.LogInformation("清理了 {Count} 个旧备份", toDelete.Count);
        }
    }

    private string ExtractDatabaseName(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].Trim();
                if (key.Equals("Database", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase))
                {
                    return keyValue[1].Trim();
                }
            }
        }
        return string.Empty;
    }

    private DateTime? CalculateNextRunTime(string scheduleType, List<int>? recurringDays, string? scheduledTime, DateTime? onceDate)
    {
        var now = DateTime.UtcNow;

        if (scheduleType == "Once" && onceDate.HasValue)
        {
            return onceDate.Value;
        }

        if (scheduleType == "Recurring" && recurringDays != null && recurringDays.Any() && !string.IsNullOrEmpty(scheduledTime))
        {
            var timeParts = scheduledTime.Split(':');
            var hour = int.Parse(timeParts[0]);
            var minute = timeParts.Length > 1 ? int.Parse(timeParts[1]) : 0;

            for (int i = 0; i <= 7; i++)
            {
                var checkDate = now.Date.AddDays(i);
                var dayOfWeek = (int)checkDate.DayOfWeek;

                if (recurringDays.Contains(dayOfWeek))
                {
                    var runTime = checkDate.AddHours(hour).AddMinutes(minute);
                    if (runTime > now)
                    {
                        return runTime;
                    }
                }
            }
        }

        return null;
    }
}
```

**Step 2: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Services/BackupBackgroundService.cs
git commit -m "feat: create BackupBackgroundService for scheduled backups"
```

---

## Task 8: 注册后台服务

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Program.cs`

**Step 1: 在服务注册区域添加后台服务**

在 `builder.Services` 配置区域添加:

```csharp
// 注册备份计划后台服务
builder.Services.AddHostedService<DataForgeStudio.Api.Services.BackupBackgroundService>();
```

**Step 2: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Program.cs
git commit -m "feat: register BackupBackgroundService"
```

---

## Task 9: 在 SystemController 添加 API 端点

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Controllers/SystemController.cs`

**Step 1: 添加备份计划 API 端点**

```csharp
/// <summary>
/// 获取备份计划列表
/// </summary>
[HttpGet("backup-schedules")]
public async Task<ApiResponse<List<BackupScheduleDto>>> GetBackupSchedules()
{
    return await _systemService.GetBackupSchedulesAsync();
}

/// <summary>
/// 创建备份计划
/// </summary>
[HttpPost("backup-schedules")]
public async Task<ApiResponse<BackupScheduleDto>> CreateBackupSchedule([FromBody] CreateBackupScheduleRequest request)
{
    return await _systemService.CreateBackupScheduleAsync(request);
}

/// <summary>
/// 更新备份计划
/// </summary>
[HttpPut("backup-schedules/{id}")]
public async Task<ApiResponse<BackupScheduleDto>> UpdateBackupSchedule(int id, [FromBody] CreateBackupScheduleRequest request)
{
    return await _systemService.UpdateBackupScheduleAsync(id, request);
}

/// <summary>
/// 删除备份计划
/// </summary>
[HttpDelete("backup-schedules/{id}")]
public async Task<ApiResponse> DeleteBackupSchedule(int id)
{
    return await _systemService.DeleteBackupScheduleAsync(id);
}

/// <summary>
/// 切换备份计划启用状态
/// </summary>
[HttpPost("backup-schedules/{id}/toggle")]
public async Task<ApiResponse> ToggleBackupSchedule(int id)
{
    return await _systemService.ToggleBackupScheduleAsync(id);
}
```

**Step 2: 验证编译**

```bash
dotnet build backend/src/DataForgeStudio.Api/DataForgeStudio.Api.csproj
```

**Step 3: Commit**

```bash
git add backend/src/DataForgeStudio.Api/Controllers/SystemController.cs
git commit -m "feat: add backup schedule API endpoints"
```

---

## Task 10: 前端添加 API 方法

**Files:**
- Modify: `frontend/src/api/request.js`

**Step 1: 在 systemApi 对象中添加备份计划 API**

```javascript
// 备份计划 API
getBackupSchedules: () => request.get('/system/backup-schedules'),
createBackupSchedule: (data) => request.post('/system/backup-schedules', data),
updateBackupSchedule: (id, data) => request.put(`/system/backup-schedules/${id}`, data),
deleteBackupSchedule: (id) => request.delete(`/system/backup-schedules/${id}`),
toggleBackupSchedule: (id) => request.post(`/system/backup-schedules/${id}/toggle`),
```

**Step 2: Commit**

```bash
git add frontend/src/api/request.js
git commit -m "feat: add backup schedule API methods"
```

---

## Task 11: 修改 BackupManagement.vue 添加备份计划 UI

**Files:**
- Modify: `frontend/src/views/system/BackupManagement.vue`

**Step 1: 在模板中添加备份计划卡片**

在"创建备份"卡片和"备份列表"卡片之间添加:

```vue
<!-- 备份计划 -->
<el-col :span="24" style="margin-top: 20px;">
  <el-card>
    <template #header>
      <div class="card-header">
        <span>备份计划</span>
        <el-button type="primary" @click="handleAddSchedule">
          <el-icon><Plus /></el-icon>
          新增计划
        </el-button>
      </div>
    </template>

    <el-table :data="schedules" v-loading="schedulesLoading" border stripe>
      <el-table-column prop="scheduleName" label="计划名称" width="150" />
      <el-table-column prop="scheduleType" label="类型" width="100">
        <template #default="{ row }">
          <el-tag :type="row.scheduleType === 'Recurring' ? 'primary' : 'warning'">
            {{ row.scheduleType === 'Recurring' ? '重复' : '单次' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="执行时间" min-width="200">
        <template #default="{ row }">
          <span v-if="row.scheduleType === 'Recurring'">
            {{ formatRecurringDays(row.recurringDays) }} {{ row.scheduledTime }}
          </span>
          <span v-else>
            {{ formatOnceDate(row.onceDate) }}
          </span>
        </template>
      </el-table-column>
      <el-table-column prop="retentionCount" label="保留数量" width="100" />
      <el-table-column prop="nextRunTime" label="下次执行" width="180">
        <template #default="{ row }">
          {{ formatDateTime(row.nextRunTime) }}
        </template>
      </el-table-column>
      <el-table-column prop="isEnabled" label="状态" width="80">
        <template #default="{ row }">
          <el-switch v-model="row.isEnabled" @change="handleToggleSchedule(row)" />
        </template>
      </el-table-column>
      <el-table-column label="操作" width="120" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" link size="small" @click="handleEditSchedule(row)">编辑</el-button>
          <el-button type="danger" link size="small" @click="handleDeleteSchedule(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>
  </el-card>
</el-col>
```

**Step 2: 添加计划编辑对话框**

```vue
<!-- 计划编辑对话框 -->
<el-dialog v-model="scheduleDialogVisible" :title="editingSchedule ? '编辑计划' : '新增计划'" width="500px">
  <el-form :model="scheduleForm" label-width="100px">
    <el-form-item label="计划名称" required>
      <el-input v-model="scheduleForm.scheduleName" placeholder="请输入计划名称" />
    </el-form-item>
    <el-form-item label="计划类型" required>
      <el-radio-group v-model="scheduleForm.scheduleType">
        <el-radio value="Recurring">重复计划</el-radio>
        <el-radio value="Once">单次计划</el-radio>
      </el-radio-group>
    </el-form-item>

    <!-- 重复计划设置 -->
    <template v-if="scheduleForm.scheduleType === 'Recurring'">
      <el-form-item label="执行日期" required>
        <el-checkbox-group v-model="scheduleForm.recurringDays">
          <el-checkbox :value="0">周日</el-checkbox>
          <el-checkbox :value="1">周一</el-checkbox>
          <el-checkbox :value="2">周二</el-checkbox>
          <el-checkbox :value="3">周三</el-checkbox>
          <el-checkbox :value="4">周四</el-checkbox>
          <el-checkbox :value="5">周五</el-checkbox>
          <el-checkbox :value="6">周六</el-checkbox>
        </el-checkbox-group>
      </el-form-item>
      <el-form-item label="执行时间" required>
        <el-time-select
          v-model="scheduleForm.scheduledTime"
          start="00:00"
          end="23:59"
          step="00:30"
          placeholder="选择时间"
        />
      </el-form-item>
    </template>

    <!-- 单次计划设置 -->
    <template v-if="scheduleForm.scheduleType === 'Once'">
      <el-form-item label="执行时间" required>
        <el-date-picker
          v-model="scheduleForm.onceDate"
          type="datetime"
          placeholder="选择日期时间"
          value-format="YYYY-MM-DDTHH:mm:ss"
        />
      </el-form-item>
    </template>

    <el-form-item label="保留数量">
      <el-input-number v-model="scheduleForm.retentionCount" :min="1" :max="100" />
      <span style="margin-left: 10px; color: #909399;">个备份</span>
    </el-form-item>
  </el-form>
  <template #footer>
    <el-button @click="scheduleDialogVisible = false">取消</el-button>
    <el-button type="primary" @click="handleSaveSchedule" :loading="savingSchedule">保存</el-button>
  </template>
</el-dialog>
```

**Step 3: 在 script 中添加相关变量和方法**

```javascript
// 备份计划相关
const schedules = ref([])
const schedulesLoading = ref(false)
const scheduleDialogVisible = ref(false)
const editingSchedule = ref(null)
const savingSchedule = ref(false)

const scheduleForm = reactive({
  scheduleName: '',
  scheduleType: 'Recurring',
  recurringDays: [1, 2, 3, 4, 5],
  scheduledTime: '02:00',
  onceDate: null,
  retentionCount: 10
})

const loadSchedules = async () => {
  schedulesLoading.value = true
  try {
    const res = await systemApi.getBackupSchedules()
    if (res.success) {
      schedules.value = res.data || []
    }
  } catch (error) {
    console.error('加载备份计划失败:', error)
  } finally {
    schedulesLoading.value = false
  }
}

const handleAddSchedule = () => {
  editingSchedule.value = null
  Object.assign(scheduleForm, {
    scheduleName: '',
    scheduleType: 'Recurring',
    recurringDays: [1, 2, 3, 4, 5],
    scheduledTime: '02:00',
    onceDate: null,
    retentionCount: 10
  })
  scheduleDialogVisible.value = true
}

const handleEditSchedule = (row) => {
  editingSchedule.value = row
  Object.assign(scheduleForm, {
    scheduleName: row.scheduleName,
    scheduleType: row.scheduleType,
    recurringDays: row.recurringDays || [],
    scheduledTime: row.scheduledTime,
    onceDate: row.onceDate,
    retentionCount: row.retentionCount
  })
  scheduleDialogVisible.value = true
}

const handleSaveSchedule = async () => {
  if (!scheduleForm.scheduleName) {
    ElMessage.warning('请输入计划名称')
    return
  }

  savingSchedule.value = true
  try {
    if (editingSchedule.value) {
      await systemApi.updateBackupSchedule(editingSchedule.value.scheduleId, scheduleForm)
      ElMessage.success('更新成功')
    } else {
      await systemApi.createBackupSchedule(scheduleForm)
      ElMessage.success('创建成功')
    }
    scheduleDialogVisible.value = false
    loadSchedules()
  } catch (error) {
    console.error('保存失败:', error)
  } finally {
    savingSchedule.value = false
  }
}

const handleToggleSchedule = async (row) => {
  try {
    await systemApi.toggleBackupSchedule(row.scheduleId)
    ElMessage.success(row.isEnabled ? '已启用' : '已禁用')
    loadSchedules()
  } catch (error) {
    console.error('切换状态失败:', error)
    row.isEnabled = !row.isEnabled
  }
}

const handleDeleteSchedule = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要删除计划"${row.scheduleName}"吗？`, '提示', {
      type: 'warning'
    })
    await systemApi.deleteBackupSchedule(row.scheduleId)
    ElMessage.success('删除成功')
    loadSchedules()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}

const formatRecurringDays = (days) => {
  if (!days || days.length === 0) return '-'
  const dayNames = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']
  return days.map(d => dayNames[d]).join('、')
}

const formatOnceDate = (date) => {
  if (!date) return '-'
  return new Date(date).toLocaleString('zh-CN')
}

const formatDateTime = (date) => {
  if (!date) return '-'
  return new Date(date).toLocaleString('zh-CN')
}
```

**Step 4: 在 onMounted 中加载计划数据**

```javascript
onMounted(() => {
  loadData()
  loadSchedules()  // 添加这行
})
```

**Step 5: 验证前端构建**

```bash
cd frontend && npm run build
```

**Step 6: Commit**

```bash
git add frontend/src/views/system/BackupManagement.vue
git commit -m "feat: add backup schedule UI to BackupManagement"
```

---

## Task 12: 测试验证

**Step 1: 停止后端服务（如果在运行）**

**Step 2: 执行数据库迁移**

```bash
cd backend
dotnet ef database update --project src/DataForgeStudio.Data --startup-project src/DataForgeStudio.Api
```

**Step 3: 手动执行 SQL 添加 Description 列（如果尚未执行）**

```sql
ALTER TABLE BackupRecords ADD Description NVARCHAR(500) NULL;
```

**Step 4: 启动后端服务**

```bash
dotnet run --project backend/src/DataForgeStudio.Api
```

**Step 5: 启动前端服务**

```bash
cd frontend && npm run dev
```

**Step 6: 功能测试**

1. 进入备份管理页面
2. 测试创建重复计划（选择周几、时间、保留数量）
3. 测试创建单次计划
4. 测试编辑计划
5. 测试启用/禁用计划
6. 测试删除计划
7. 验证后台服务是否正确执行计划

**Step 7: 最终 Commit**

```bash
git add -A
git commit -m "feat: complete backup schedule feature"
```

---

## 完成标准

- [ ] 数据库 BackupSchedules 表创建成功
- [ ] 后端 API 端点正常工作
- [ ] 后台服务正确检查和执行备份计划
- [ ] 前端 UI 正确显示和管理计划
- [ ] 重复计划按选定日期和时间执行
- [ ] 单次计划执行后自动禁用
- [ ] 超过保留数量的旧备份自动清理
