# 备份计划功能设计

## 日期
2026-02-16

## 功能概述

在备份管理页面增加"备份计划"功能，支持：
- **重复计划**: 勾选周几（多选），设置执行时间
- **单次计划**: 指定日期和时间执行一次
- **保留策略**: 保留最近 N 条备份，超过自动删除最旧的

## 数据库设计

### 新增表 BackupSchedules

```sql
CREATE TABLE BackupSchedules (
    ScheduleId INT IDENTITY(1,1) PRIMARY KEY,
    ScheduleName NVARCHAR(100) NOT NULL,
    ScheduleType NVARCHAR(20) NOT NULL,           -- 'Recurring' 或 'Once'
    RecurringDays NVARCHAR(50) NULL,              -- 重复计划的执行日期，如 "0,1,2,3,4" 表示周一到周五
    ScheduledTime TIME NULL,                      -- 执行时间，如 02:00:00
    OnceDate DATETIME2 NULL,                      -- 单次计划的执行日期时间
    RetentionCount INT NOT NULL DEFAULT 10,       -- 保留备份数量
    IsEnabled BIT NOT NULL DEFAULT 1,             -- 是否启用
    LastRunTime DATETIME2 NULL,                   -- 最后执行时间
    NextRunTime DATETIME2 NULL,                   -- 下次执行时间
    CreatedTime DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedTime DATETIME2 NULL
);

CREATE INDEX IX_BackupSchedules_NextRunTime ON BackupSchedules(NextRunTime);
CREATE INDEX IX_BackupSchedules_IsEnabled ON BackupSchedules(IsEnabled);
```

### RecurringDays 格式说明
- 使用逗号分隔的数字表示星期几
- 0=周日, 1=周一, 2=周二, 3=周三, 4=周四, 5=周五, 6=周六
- 例如: "1,3,5" 表示周一、周三、周五执行

## 后端实现

### 1. 新增实体 BackupSchedule

文件: `backend/src/DataForgeStudio.Domain/Entities/System.cs`

```csharp
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

    [MaxLength(50)]
    public string? RecurringDays { get; set; } // "0,1,2,3,4,5,6"

    public TimeSpan? ScheduledTime { get; set; }

    public DateTime? OnceDate { get; set; }

    public int RetentionCount { get; set; } = 10;

    public bool IsEnabled { get; set; } = true;

    public DateTime? LastRunTime { get; set; }

    public DateTime? NextRunTime { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedTime { get; set; }
}
```

### 2. 新增 DTO

文件: `backend/src/DataForgeStudio.Shared/DTO/ApiResponse.cs`

```csharp
public class BackupScheduleDto
{
    public int ScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = string.Empty;
    public List<int> RecurringDays { get; set; } = new();
    public string? ScheduledTime { get; set; } // "HH:mm"
    public DateTime? OnceDate { get; set; }
    public int RetentionCount { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastRunTime { get; set; }
    public DateTime? NextRunTime { get; set; }
    public string CreatedTime { get; set; } = string.Empty;
}

public class CreateBackupScheduleRequest
{
    public string ScheduleName { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = "Recurring";
    public List<int>? RecurringDays { get; set; }
    public string? ScheduledTime { get; set; } // "HH:mm"
    public DateTime? OnceDate { get; set; }
    public int RetentionCount { get; set; } = 10;
    public bool IsEnabled { get; set; } = true;
}
```

### 3. 后台服务 BackupBackgroundService

文件: `backend/src/DataForgeStudio.Api/Services/BackupBackgroundService.cs`

```csharp
public class BackupBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
    }

    private async Task CheckAndExecuteSchedulesAsync()
    {
        var now = DateTime.UtcNow;

        // 查找需要执行的备份计划
        var schedules = await _context.BackupSchedules
            .Where(s => s.IsEnabled && s.NextRunTime <= now)
            .ToListAsync();

        foreach (var schedule in schedules)
        {
            await ExecuteScheduleAsync(schedule);
        }
    }

    private async Task ExecuteScheduleAsync(BackupSchedule schedule)
    {
        // 执行备份
        // ...

        // 清理旧备份
        await CleanupOldBackupsAsync(schedule.RetentionCount);

        // 更新下次执行时间
        schedule.LastRunTime = DateTime.UtcNow;
        schedule.NextRunTime = CalculateNextRunTime(schedule);
        await _context.SaveChangesAsync();
    }

    private DateTime CalculateNextRunTime(BackupSchedule schedule)
    {
        // 计算下次执行时间的逻辑
    }
}
```

### 4. API 端点

在 SystemController 中添加:

```csharp
// 获取备份计划列表
[HttpGet("backup-schedules")]
public async Task<ApiResponse<List<BackupScheduleDto>>> GetBackupSchedules()

// 创建备份计划
[HttpPost("backup-schedules")]
public async Task<ApiResponse<BackupScheduleDto>> CreateBackupSchedule([FromBody] CreateBackupScheduleRequest request)

// 更新备份计划
[HttpPut("backup-schedules/{id}")]
public async Task<ApiResponse<BackupScheduleDto>> UpdateBackupSchedule(int id, [FromBody] CreateBackupScheduleRequest request)

// 删除备份计划
[HttpDelete("backup-schedules/{id}")]
public async Task<ApiResponse> DeleteBackupSchedule(int id)

// 启用/禁用备份计划
[HttpPost("backup-schedules/{id}/toggle")]
public async Task<ApiResponse> ToggleBackupSchedule(int id)
```

## 前端实现

### 1. 新增 API 方法

文件: `frontend/src/api/request.js`

```javascript
// 备份计划 API
getBackupSchedules: () => request.get('/system/backup-schedules'),
createBackupSchedule: (data) => request.post('/system/backup-schedules', data),
updateBackupSchedule: (id, data) => request.put(`/system/backup-schedules/${id}`, data),
deleteBackupSchedule: (id) => request.delete(`/system/backup-schedules/${id}`),
toggleBackupSchedule: (id) => request.post(`/system/backup-schedules/${id}/toggle`),
```

### 2. 修改 BackupManagement.vue

在"创建备份"卡片和"备份列表"卡片之间，添加"备份计划"卡片:

- 显示现有备份计划列表
- 添加"新增计划"按钮
- 计划编辑对话框（计划名称、类型、执行日期、执行时间、保留数量）
- 启用/禁用切换
- 删除功能

### 3. UI 组件

```
┌─────────────────────────────────────────────────────┐
│ 备份计划                                    [新增]  │
├─────────────────────────────────────────────────────┤
│ 计划列表:                                           │
│ ┌─────────────────────────────────────────────────┐ │
│ │ 名称     类型   执行时间      保留数  状态  操作  │ │
│ │ 每日备份  重复   每天02:00     10     ✓    编辑删除│ │
│ │ 周末备份  重复   周六日03:00   5      ✓    编辑删除│ │
│ └─────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

## 实现步骤

1. 后端：新增 BackupSchedule 实体
2. 后端：创建数据库迁移
3. 后端：新增 DTO
4. 后端：扩展 ISystemService 接口
5. 后端：实现 SystemService 中的计划管理方法
6. 后端：创建 BackupBackgroundService 后台服务
7. 后端：在 Program.cs 注册后台服务
8. 后端：在 SystemController 添加 API 端点
9. 前端：添加 API 方法
10. 前端：修改 BackupManagement.vue
11. 测试验证

## 注意事项

- 后台服务每分钟检查一次是否有需要执行的计划
- 执行备份后自动清理超过保留数量的旧备份
- 单次计划执行后自动禁用
- 服务重启后会重新计算下次执行时间
