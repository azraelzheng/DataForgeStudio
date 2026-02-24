namespace DataForgeStudio.Shared.DTO;

/// <summary>
/// API 响应基类
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// 成功响应
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 失败响应
    /// </summary>
    public static ApiResponse<T> Fail(string message, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode ?? "ERROR"
        };
    }
}

/// <summary>
/// 无数据的 API 响应
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// 成功响应
    /// </summary>
    public static ApiResponse Ok(string message = "操作成功")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Data = null
        };
    }

    /// <summary>
    /// 失败响应
    /// </summary>
    public static new ApiResponse Fail(string message, string? errorCode = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode ?? "ERROR"
        };
    }
}

/// <summary>
/// 分页请求
/// </summary>
public class PagedRequest
{
    private int _pageIndex = 1;
    private int _pageSize = 20;

    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = value < 1 ? 1 : value;
    }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 100);
    }

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? SortField { get; set; }

    /// <summary>
    /// 排序方向 (asc/desc)
    /// </summary>
    public string SortOrder { get; set; } = "desc";

    /// <summary>
    /// 关键词
    /// </summary>
    public string? Keyword { get; set; }
}

/// <summary>
/// 分页响应
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public List<T> Items { get; set; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;

    public PagedResponse()
    {
        Items = new List<T>();
    }

    public PagedResponse(List<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}

/// <summary>
/// 登录请求
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// 验证码
    /// </summary>
    public string? CaptchaCode { get; set; }

    /// <summary>
    /// 记住我
    /// </summary>
    public bool RememberMe { get; set; }
}

/// <summary>
/// 登录响应
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT Token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token 类型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfoDto? UserInfo { get; set; }

    /// <summary>
    /// 是否需要修改密码
    /// </summary>
    public bool RequiresPasswordChange { get; set; }

    /// <summary>
    /// 用户ID（用于强制修改密码场景）
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 登录是否成功
    /// </summary>
    public bool Success { get; set; } = true;
}

/// <summary>
/// 用户信息 DTO
/// </summary>
public class UserInfoDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// 权限列表
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// 修改密码请求
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// 旧密码
    /// </summary>
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码（至少 8 个字符，必须包含大小写字母和数字）
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 确认密码
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// 强制修改密码请求（首次登录）
/// </summary>
public class ForcePasswordChangeRequest
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 临时密码
    /// </summary>
    public string TemporaryPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码（至少 8 个字符，必须包含大小写字母和数字）
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 确认密码
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// 用户 DTO
/// </summary>
public class UserDto
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public string? RealName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public DateTime CreatedTime { get; set; }
    public List<RoleDto> Roles { get; set; } = new();
    /// <summary>
    /// 是否有操作日志记录（有记录的用户只能停用不能删除）
    /// </summary>
    public bool HasOperationLogs { get; set; }
}

/// <summary>
/// 创建/更新用户请求
/// </summary>
public class CreateUserRequest
{
    public required string Username { get; set; }

    /// <summary>
    /// 密码（至少 8 个字符，必须包含大小写字母和数字）
    /// </summary>
    public string? Password { get; set; }
    public string? RealName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 重置密码请求
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// 新密码（至少 8 个字符，必须包含大小写字母和数字）
    /// </summary>
    public required string NewPassword { get; set; }
}

/// <summary>
/// 分配角色请求
/// </summary>
public class AssignRolesRequest
{
    public List<int> RoleIds { get; set; } = new();
}

/// <summary>
/// 角色 DTO
/// </summary>
public class RoleDto
{
    public int RoleId { get; set; }
    public required string RoleName { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedTime { get; set; }
    public int UserCount { get; set; }
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// 创建/更新角色请求
/// </summary>
public class CreateRoleRequest
{
    public required string RoleName { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// 分配权限请求
/// </summary>
public class AssignPermissionsRequest
{
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// 数据源 DTO
/// </summary>
public class DataSourceDto
{
    public int DataSourceId { get; set; }
    public required string DataSourceName { get; set; }
    public required string DbType { get; set; }
    public required string ServerAddress { get; set; }
    public int? Port { get; set; }
    public string? DatabaseName { get; set; }
    public string? Username { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedTime { get; set; }
}

/// <summary>
/// 创建/更新数据源请求
/// </summary>
public class CreateDataSourceRequest
{
    public required string DataSourceName { get; set; }
    public required string DbType { get; set; }
    public required string Server { get; set; }
    public int Port { get; set; }
    public required string Database { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// 报表 DTO
/// </summary>
public class ReportDto
{
    public int ReportId { get; set; }
    public required string ReportName { get; set; }
    public string? ReportCategory { get; set; }
    public int DataSourceId { get; set; }
    public string? Description { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewTime { get; set; }
    public DateTime CreatedTime { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 报表详情 DTO
/// </summary>
public class ReportDetailDto : ReportDto
{
    public required string SqlQuery { get; set; }
    public List<ReportFieldDto> Columns { get; set; } = new();
    public List<ReportParameterDto> Parameters { get; set; } = new();
    public bool EnableChart { get; set; }
    public ChartConfigDto? ChartConfig { get; set; }
    public List<QueryConditionDto>? QueryConditions { get; set; }
}

/// <summary>
/// 报表字段 DTO
/// </summary>
public class ReportFieldDto
{
    public required string FieldName { get; set; }
    public required string DisplayName { get; set; }
    public required string DataType { get; set; }
    public int Width { get; set; }
    public string Align { get; set; } = "left";
    public bool IsVisible { get; set; }
    public bool IsSortable { get; set; }
    /// <summary>
    /// 汇总类型：none, sum, avg
    /// </summary>
    public string? SummaryType { get; set; }
    /// <summary>
    /// 汇总值小数位数，null 表示自动检测
    /// </summary>
    public int? SummaryDecimals { get; set; }
}

/// <summary>
/// 报表参数 DTO
/// </summary>
public class ReportParameterDto
{
    public required string Name { get; set; }
    public required string Label { get; set; }
    public required string DataType { get; set; }
    public string? DefaultValue { get; set; }
}

/// <summary>
/// 查询条件 DTO
/// </summary>
public class QueryConditionDto
{
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
    public string Operator { get; set; } = "eq";
    public string? DefaultValue { get; set; }
}

/// <summary>
/// 图表配置 DTO
/// </summary>
public class ChartConfigDto
{
    public string ChartType { get; set; } = "bar";
    public string XField { get; set; } = string.Empty;
    public List<string> YFields { get; set; } = new();
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// 创建/更新报表请求
/// </summary>
public class CreateReportRequest
{
    public int? ReportId { get; set; }
    public required string ReportName { get; set; }
    public required string ReportCategory { get; set; }
    public int DataSourceId { get; set; }
    public required string SqlQuery { get; set; }
    public string? Description { get; set; }
    public List<ReportFieldDto> Columns { get; set; } = new();
    public List<ReportParameterDto> Parameters { get; set; } = new();
    public bool EnableChart { get; set; }
    public ChartConfigDto? ChartConfig { get; set; }
    public List<QueryConditionDto>? QueryConditions { get; set; }
}

/// <summary>
/// 执行报表请求
/// </summary>
public class ExecuteReportRequest
{
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// 测试查询请求（用于报表设计器）
/// </summary>
public class TestQueryRequest
{
    public int DataSourceId { get; set; }
    public string Sql { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// 导出格式枚举
/// </summary>
public enum ExportFormat
{
    Excel = 1,
    Csv = 2,
    Pdf = 3
}

/// <summary>
/// 导出报表请求
/// </summary>
public class ExportReportRequest : ExecuteReportRequest
{
    public ExportFormat Format { get; set; } = ExportFormat.Excel;
    public string? FileName { get; set; }
}

/// <summary>
/// 许可证信息 DTO
/// </summary>
public class LicenseInfoDto
{
    public int LicenseId { get; set; }
    public required string LicenseType { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? MaxUsers { get; set; }
    public int? MaxReports { get; set; }
    public int? MaxDataSources { get; set; }
    public List<string>? Features { get; set; }
}

/// <summary>
/// 激活许可证请求
/// </summary>
public class ActivateLicenseRequest
{
    public required string LicenseKey { get; set; }
}

/// <summary>
/// 许可证验证响应
/// </summary>
public class LicenseValidationResponse
{
    public bool Valid { get; set; }
    public string? Message { get; set; }
    public LicenseInfoDto? LicenseInfo { get; set; }
}

/// <summary>
/// 操作日志 DTO
/// </summary>
public class OperationLogDto
{
    public int LogId { get; set; }
    public required string Username { get; set; }
    public required string Action { get; set; }
    public required string Module { get; set; }
    public required string Description { get; set; }
    public required string Ip { get; set; }
    public required string CreatedTime { get; set; }
}

/// <summary>
/// 系统配置 DTO
/// </summary>
public class SystemConfigDto
{
    public int ConfigId { get; set; }
    public required string ConfigKey { get; set; }
    public string? ConfigValue { get; set; }
    public required string ConfigType { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// 备份记录 DTO
/// </summary>
public class BackupRecordDto
{
    public int BackupId { get; set; }
    public required string BackupName { get; set; }
    public required string FileName { get; set; }
    public long? FileSize { get; set; }
    public string? Description { get; set; }
    public required string CreatedBy { get; set; }
    public required string CreatedTime { get; set; }
}

/// <summary>
/// 创建备份请求
/// </summary>
public class CreateBackupRequest
{
    public string? BackupName { get; set; }
    public string? Description { get; set; }
    /// <summary>
    /// 备份文件存放路径（为空则使用默认路径）
    /// </summary>
    public string? BackupPath { get; set; }
}

/// <summary>
/// 表字段 DTO
/// </summary>
public class TableColumnDto
{
    /// <summary>
    /// 字段名称
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 数据类型
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// 最大长度
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// 是否可为空
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 字段属性（如主键、自增等）
    /// </summary>
    public string? ColumnProperty { get; set; }

    /// <summary>
    /// 字段位置
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// 系统数据类型（用于前端查询条件）
    /// </summary>
    public string SystemDataType { get; set; } = "String";
}

/// <summary>
/// 表信息 DTO（用于SQL编辑器自动补全）
/// </summary>
public class TableInfoDto
{
    /// <summary>
    /// 表名称
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 列信息
    /// </summary>
    public List<TableColumnInfoDto> Columns { get; set; } = new();
}

/// <summary>
/// 表列信息 DTO（用于SQL编辑器自动补全）
/// </summary>
public class TableColumnInfoDto
{
    /// <summary>
    /// 列名称
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 数据类型
    /// </summary>
    public string DataType { get; set; } = string.Empty;
}

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
    /// <summary>
    /// 备份文件存放路径
    /// </summary>
    public string? BackupPath { get; set; }
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
    /// <summary>
    /// 备份文件存放路径（为空则使用默认路径）
    /// </summary>
    public string? BackupPath { get; set; }
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 许可证使用统计 DTO
/// </summary>
public class LicenseUsageStatsDto
{
    /// <summary>
    /// 当前用户数量
    /// </summary>
    public int CurrentUsers { get; set; }

    /// <summary>
    /// 当前报表数量
    /// </summary>
    public int CurrentReports { get; set; }

    /// <summary>
    /// 当前数据源数量
    /// </summary>
    public int CurrentDataSources { get; set; }
}
