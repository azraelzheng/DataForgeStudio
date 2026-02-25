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
