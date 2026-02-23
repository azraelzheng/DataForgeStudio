using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 系统服务实现
/// </summary>
public class SystemService : ISystemService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly ILogger<SystemService> _logger;

    public SystemService(DataForgeStudioDbContext context, ILogger<SystemService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResponse<SystemConfigDto>>> GetConfigsAsync(PagedRequest request)
    {
        var query = _context.SystemConfigs;

        var totalCount = await query.CountAsync();

        var configs = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.ConfigKey)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new SystemConfigDto
            {
                ConfigId = c.ConfigId,
                ConfigKey = c.ConfigKey,
                ConfigValue = c.ConfigValue,
                ConfigType = c.ConfigType,
                Description = c.Description
            })
            .ToListAsync();

        var pagedResponse = new PagedResponse<SystemConfigDto>(configs, totalCount, request.PageIndex, request.PageSize);
        return ApiResponse<PagedResponse<SystemConfigDto>>.Ok(pagedResponse);
    }

    public async Task<ApiResponse<PagedResponse<OperationLogDto>>> GetLogsAsync(
        PagedRequest request,
        string? username = null,
        string? action = null,
        string? module = null,
        string? startTime = null,
        string? endTime = null)
    {
        var query = _context.OperationLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(l => l.Username != null && l.Username.Contains(username));
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

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.CreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new OperationLogDto
            {
                LogId = l.LogId,
                Username = l.Username ?? string.Empty,
                Action = l.Action,
                Module = l.Module,
                Description = l.Description ?? string.Empty,
                Ip = l.IpAddress ?? string.Empty,
                CreatedTime = l.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToListAsync();

        var pagedResponse = new PagedResponse<OperationLogDto>(logs, totalCount, request.PageIndex, request.PageSize);
        return ApiResponse<PagedResponse<OperationLogDto>>.Ok(pagedResponse);
    }

    public async Task<ApiResponse> ClearLogsAsync()
    {
        var logCount = await _context.OperationLogs.CountAsync();
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM OperationLogs");

        _logger.LogInformation("已清空 {Count} 条操作日志", logCount);
        return ApiResponse.Ok($"已清空 {logCount} 条日志");
    }

    public async Task<ApiResponse> DeleteLogsByQueryAsync(
        string? username = null,
        string? action = null,
        string? module = null,
        string? startTime = null,
        string? endTime = null)
    {
        var query = _context.OperationLogs.AsQueryable();

        // 应用查询条件
        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(l => l.Username != null && l.Username.Contains(username));
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

        // 获取符合条件的日志数量
        var count = await query.CountAsync();

        if (count == 0)
        {
            return ApiResponse.Ok("没有符合条件的日志");
        }

        // 删除符合条件的日志
        _context.OperationLogs.RemoveRange(query);
        await _context.SaveChangesAsync();

        _logger.LogInformation("根据查询条件删除了 {Count} 条操作日志", count);
        return ApiResponse.Ok($"已删除 {count} 条日志");
    }

    public async Task<ApiResponse> DeleteLogsByIdsAsync(List<int> logIds)
    {
        if (logIds == null || logIds.Count == 0)
        {
            return ApiResponse.Fail("没有选择要删除的日志", "NO_LOGS_SELECTED");
        }

        // 查找要删除的日志
        var logsToDelete = await _context.OperationLogs
            .Where(l => logIds.Contains(l.LogId))
            .ToListAsync();

        if (logsToDelete.Count == 0)
        {
            return ApiResponse.Fail("未找到要删除的日志", "LOGS_NOT_FOUND");
        }

        // 删除日志
        _context.OperationLogs.RemoveRange(logsToDelete);
        await _context.SaveChangesAsync();

        _logger.LogInformation("删除了 {Count} 条操作日志", logsToDelete.Count);
        return ApiResponse.Ok($"已删除 {logsToDelete.Count} 条日志");
    }

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
            query = query.Where(l => l.Username != null && l.Username.Contains(username));
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
            worksheet.Cell(row, 1).Value = log.Username ?? "";
            worksheet.Cell(row, 2).Value = log.Action ?? "";
            worksheet.Cell(row, 3).Value = log.Module ?? "";
            worksheet.Cell(row, 4).Value = log.Description ?? "";
            worksheet.Cell(row, 5).Value = log.IpAddress ?? "";
            worksheet.Cell(row, 6).Value = log.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss");
            row++;
        }

        // 设置表格边框样式
        var range = worksheet.Range(1, 1, row - 1, 6);
        range.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportSelectedLogsToExcelAsync(List<int> logIds)
    {
        var query = _context.OperationLogs.Where(l => logIds.Contains(l.LogId));

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
            worksheet.Cell(row, 1).Value = log.Username ?? "";
            worksheet.Cell(row, 2).Value = log.Action ?? "";
            worksheet.Cell(row, 3).Value = log.Module ?? "";
            worksheet.Cell(row, 4).Value = log.Description ?? "";
            worksheet.Cell(row, 5).Value = log.IpAddress ?? "";
            worksheet.Cell(row, 6).Value = log.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss");
            row++;
        }

        // 设置表格边框样式
        var range = worksheet.Range(1, 1, row - 1, 6);
        range.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

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

            _logger.LogInformation("数据库备份成功: {Path}, 大小: {Size} 字节", backupPath, fileSize);
            return ApiResponse<BackupRecordDto>.Ok(backupDto, "备份创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建数据库备份失败");
            return ApiResponse<BackupRecordDto>.Fail($"创建备份失败: {ex.Message}", "BACKUP_ERROR");
        }
    }

    private string ExtractDatabaseName(string connectionString)
    {
        // 从连接字符串中提取数据库名称
        // 支持 "Database" 和 "Initial Catalog" 两种格式
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

    private async Task<bool> ExecuteBackupCommand(string connectionString, string databaseName, string backupPath)
    {
        try
        {
            // 构建备份 SQL 命令
            var backupSql = $@"
                USE [master];
                BACKUP DATABASE [{databaseName}]
                TO DISK = N'{backupPath.Replace("\\", "\\\\")}'
                WITH FORMAT,
                     MEDIANAME = N'{databaseName}_Backup',
                     NAME = N'Full Backup of {databaseName}',
                     STATS = 10;
            ";

            // 使用 SqlConnection 执行备份
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new Microsoft.Data.SqlClient.SqlCommand(backupSql, connection)
            {
                CommandTimeout = 300  // 5分钟超时
            };

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行备份命令失败: {Database}", databaseName);
            return false;
        }
    }

    public async Task<ApiResponse<PagedResponse<BackupRecordDto>>> GetBackupsAsync(PagedRequest request, string? backupName = null)
    {
        var query = _context.BackupRecords.AsQueryable();

        // 按备份名称搜索
        if (!string.IsNullOrWhiteSpace(backupName))
        {
            query = query.Where(b => b.BackupName.Contains(backupName));
        }

        var totalCount = await query.CountAsync();

        var backupEntities = await query
            .OrderByDescending(b => b.CreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var backups = backupEntities.Select(b => new BackupRecordDto
        {
            BackupId = b.BackupId,
            BackupName = b.BackupName,
            FileName = System.IO.Path.GetFileName(b.BackupPath),
            FileSize = b.FileSize,
            Description = b.Description,
            CreatedBy = b.CreatedBy?.ToString() ?? "System",
            CreatedTime = b.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();

        var pagedResponse = new PagedResponse<BackupRecordDto>(backups, totalCount, request.PageIndex, request.PageSize);
        return ApiResponse<PagedResponse<BackupRecordDto>>.Ok(pagedResponse);
    }

    public async Task<ApiResponse> DeleteBackupAsync(int backupId)
    {
        var backup = await _context.BackupRecords.FindAsync(backupId);
        if (backup == null)
        {
            return ApiResponse.Fail("备份记录不存在", "NOT_FOUND");
        }

        // TODO: 删除实际文件
        if (System.IO.File.Exists(backup.BackupPath))
        {
            System.IO.File.Delete(backup.BackupPath);
        }

        _context.BackupRecords.Remove(backup);
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("备份删除成功");
    }

    public async Task<ApiResponse> RestoreBackupAsync(int backupId)
    {
        try
        {
            var backup = await _context.BackupRecords.FindAsync(backupId);
            if (backup == null)
            {
                return ApiResponse.Fail("备份记录不存在", "NOT_FOUND");
            }

            if (!System.IO.File.Exists(backup.BackupPath))
            {
                return ApiResponse.Fail("备份文件不存在", "FILE_NOT_FOUND");
            }

            // 获取数据库连接字符串
            var connectionString = _context.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return ApiResponse.Fail("无法获取数据库连接字符串", "CONNECTION_STRING_ERROR");
            }

            // 解析数据库名称
            var databaseName = ExtractDatabaseName(connectionString);
            if (string.IsNullOrEmpty(databaseName))
            {
                return ApiResponse.Fail("无法获取数据库名称", "DATABASE_NAME_ERROR");
            }

            // 执行恢复命令
            var restoreSuccess = await ExecuteRestoreCommand(connectionString, databaseName, backup.BackupPath);

            if (!restoreSuccess)
            {
                _logger.LogError($"数据库恢复失败: {backup.BackupPath}");
                return ApiResponse.Fail("数据库恢复失败", "RESTORE_FAILED");
            }

            _logger.LogInformation($"数据库恢复成功: {backup.BackupPath}");
            return ApiResponse.Ok("备份恢复成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复数据库备份失败");
            return ApiResponse.Fail($"恢复备份失败: {ex.Message}", "RESTORE_ERROR");
        }
    }

    private async Task<bool> ExecuteRestoreCommand(string connectionString, string databaseName, string backupPath)
    {
        try
        {
            // 构建恢复 SQL 命令
            // 需要先设置为单用户模式，杀掉所有连接，然后恢复，最后恢复多用户模式
            var restoreSql = $@"
                USE [master];
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                RESTORE DATABASE [{databaseName}]
                FROM DISK = N'{backupPath.Replace("\\", "\\\\")}'
                WITH REPLACE,
                     STATS = 10;
                ALTER DATABASE [{databaseName}] SET MULTI_USER;
            ";

            // 使用 SqlConnection 执行恢复
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new Microsoft.Data.SqlClient.SqlCommand(restoreSql, connection)
            {
                CommandTimeout = 600  // 10分钟超时
            };

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行恢复命令失败: {Database}", databaseName);

            // 尝试恢复多用户模式
            try
            {
                var resetSql = $"USE [master]; ALTER DATABASE [{databaseName}] SET MULTI_USER;";
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();
                using var command = new Microsoft.Data.SqlClient.SqlCommand(resetSql, connection);
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // 忽略恢复模式的错误
            }

            return false;
        }
    }

    #region 备份计划管理

    public async Task<ApiResponse<List<BackupScheduleDto>>> GetBackupSchedulesAsync()
    {
        var schedules = await _context.BackupSchedules
            .OrderByDescending(s => s.CreatedTime)
            .ToListAsync();

        var result = schedules.Select(s => new BackupScheduleDto
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
        }).ToList();

        return ApiResponse<List<BackupScheduleDto>>.Ok(result);
    }

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

    #endregion
}
