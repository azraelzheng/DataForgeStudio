using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;
using DataForgeStudio.Shared.Utils;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 系统管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemController : ControllerBase
{
    private readonly ISystemService _systemService;
    private readonly ILogger<SystemController> _logger;

    public SystemController(ISystemService systemService, ILogger<SystemController> logger)
    {
        _systemService = systemService;
        _logger = logger;
    }

    /// <summary>
    /// 获取机器码（无需认证，用于许可证激活）
    /// </summary>
    [HttpGet("machine-code")]
    [AllowAnonymous]
    public ApiResponse<string> GetMachineCode()
    {
        try
        {
            var machineCode = EncryptionHelper.GetMachineCode();
            return ApiResponse<string>.Ok(machineCode, "获取机器码成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取机器码失败");
            return ApiResponse<string>.Fail("获取机器码失败", "GET_MACHINE_CODE_ERROR");
        }
    }

    /// <summary>
    /// 获取系统配置列表
    /// </summary>
    [HttpGet("configs")]
    public async Task<ApiResponse<PagedResponse<SystemConfigDto>>> GetConfigs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var request = new PagedRequest { PageIndex = page, PageSize = pageSize };
        return await _systemService.GetConfigsAsync(request);
    }

    /// <summary>
    /// 获取操作日志
    /// </summary>
    [HttpGet("logs")]
    public async Task<ApiResponse<PagedResponse<OperationLogDto>>> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? username = null,
        [FromQuery] string? action = null,
        [FromQuery] string? module = null,
        [FromQuery] string? startTime = null,
        [FromQuery] string? endTime = null)
    {
        var request = new PagedRequest { PageIndex = page, PageSize = pageSize };
        return await _systemService.GetLogsAsync(request, username, action, module, startTime, endTime);
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    [HttpDelete("logs")]
    public async Task<ApiResponse> ClearLogs()
    {
        return await _systemService.ClearLogsAsync();
    }

    /// <summary>
    /// 根据查询条件删除日志
    /// </summary>
    [HttpDelete("logs/delete-by-query")]
    public async Task<ApiResponse> DeleteLogsByQuery(
        [FromQuery] string? username = null,
        [FromQuery] string? action = null,
        [FromQuery] string? module = null,
        [FromQuery] string? startTime = null,
        [FromQuery] string? endTime = null)
    {
        return await _systemService.DeleteLogsByQueryAsync(username, action, module, startTime, endTime);
    }

    /// <summary>
    /// 根据ID列表删除日志
    /// </summary>
    [HttpDelete("logs/delete-by-ids")]
    public async Task<ApiResponse> DeleteLogsByIds([FromBody] List<int> logIds)
    {
        return await _systemService.DeleteLogsByIdsAsync(logIds);
    }

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

    /// <summary>
    /// 导出选中的操作日志到 Excel
    /// </summary>
    [HttpPost("logs/export-selected")]
    public async Task<IActionResult> ExportSelectedLogs([FromBody] List<int> logIds)
    {
        if (logIds == null || logIds.Count == 0)
        {
            return BadRequest(ApiResponse.Fail("请选择要导出的日志", "NO_SELECTION"));
        }

        var excelData = await _systemService.ExportSelectedLogsToExcelAsync(logIds);

        return File(
            excelData,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"操作日志_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }

    /// <summary>
    /// 创建备份
    /// </summary>
    [HttpPost("backup")]
    public async Task<ApiResponse<BackupRecordDto>> CreateBackup([FromBody] CreateBackupRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return ApiResponse<BackupRecordDto>.Fail("无效的用户信息", "UNAUTHORIZED");
        }

        return await _systemService.CreateBackupAsync(request, userId);
    }

    /// <summary>
    /// 获取备份列表
    /// </summary>
    [HttpGet("backups")]
    public async Task<ApiResponse<PagedResponse<BackupRecordDto>>> GetBackups(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? backupName = null)
    {
        var request = new PagedRequest { PageIndex = page, PageSize = pageSize };
        return await _systemService.GetBackupsAsync(request, backupName);
    }

    /// <summary>
    /// 删除备份
    /// </summary>
    [HttpDelete("backups/{id}")]
    public async Task<ApiResponse> DeleteBackup(int id)
    {
        return await _systemService.DeleteBackupAsync(id);
    }

    /// <summary>
    /// 恢复备份
    /// </summary>
    [HttpPost("backups/{id}/restore")]
    public async Task<ApiResponse> RestoreBackup(int id)
    {
        return await _systemService.RestoreBackupAsync(id);
    }

    #region 备份计划管理

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

    #endregion
}
