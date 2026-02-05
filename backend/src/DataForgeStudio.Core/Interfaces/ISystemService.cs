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
    /// 创建备份
    /// </summary>
    Task<ApiResponse<BackupRecordDto>> CreateBackupAsync(CreateBackupRequest request, int createdBy);

    /// <summary>
    /// 获取备份列表
    /// </summary>
    Task<ApiResponse<PagedResponse<BackupRecordDto>>> GetBackupsAsync(PagedRequest request);

    /// <summary>
    /// 删除备份
    /// </summary>
    Task<ApiResponse> DeleteBackupAsync(int backupId);

    /// <summary>
    /// 恢复备份
    /// </summary>
    Task<ApiResponse> RestoreBackupAsync(int backupId);
}
