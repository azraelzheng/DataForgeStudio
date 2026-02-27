using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 系统服务接口
/// </summary>
public interface ISystemService
{
    /// <summary>
    /// 获取系统配置列表
    /// </summary>
    Task<ApiResponse<PagedResponse<SystemConfigDto>>> GetConfigsAsync(PagedRequest request);

    /// <summary>
    /// 获取操作日志
    /// </summary>
    Task<ApiResponse<PagedResponse<OperationLogDto>>> GetLogsAsync(PagedRequest request, string? username = null, string? action = null, string? module = null, string? startTime = null, string? endTime = null);

    /// <summary>
    /// 清空日志
    /// </summary>
    Task<ApiResponse> ClearLogsAsync();

    /// <summary>
    /// 根据查询条件删除日志
    /// </summary>
    Task<ApiResponse> DeleteLogsByQueryAsync(string? username = null, string? action = null, string? module = null, string? startTime = null, string? endTime = null);

    /// <summary>
    /// 根据ID列表删除日志
    /// </summary>
    Task<ApiResponse> DeleteLogsByIdsAsync(List<int> logIds);

    /// <summary>
    /// 导出操作日志到 Excel
    /// </summary>
    Task<byte[]> ExportLogsToExcelAsync(string? username = null, string? action = null, string? module = null, string? startTime = null, string? endTime = null);

    /// <summary>
    /// 导出选中的操作日志到 Excel
    /// </summary>
    Task<byte[]> ExportSelectedLogsToExcelAsync(List<int> logIds);

    /// <summary>
    /// 创建备份
    /// </summary>
    Task<ApiResponse<BackupRecordDto>> CreateBackupAsync(CreateBackupRequest request, int createdBy);

    /// <summary>
    /// 获取备份列表
    /// </summary>
    Task<ApiResponse<PagedResponse<BackupRecordDto>>> GetBackupsAsync(PagedRequest request, string? backupName = null);

    /// <summary>
    /// 删除备份
    /// </summary>
    Task<ApiResponse> DeleteBackupAsync(int backupId);

    /// <summary>
    /// 恢复备份
    /// </summary>
    Task<ApiResponse> RestoreBackupAsync(int backupId);

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

    /// <summary>
    /// 获取系统信息（版本号等）
    /// </summary>
    ApiResponse<SystemInfoDto> GetSystemInfo();

    /// <summary>
    /// 获取文档内容
    /// </summary>
    /// <param name="type">文档类型: eula, privacy, manual</param>
    ApiResponse<DocumentDto> GetDocument(string type);
}
