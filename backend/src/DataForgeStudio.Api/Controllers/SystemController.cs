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
    /// 获取机器码
    /// </summary>
    [HttpGet("machine-code")]
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
        [FromQuery] int pageSize = 20)
    {
        var request = new PagedRequest { PageIndex = page, PageSize = pageSize };
        return await _systemService.GetBackupsAsync(request);
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
}
