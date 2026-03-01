using Newtonsoft.Json;

namespace DataForgeStudio.Domain.DTOs;

/// <summary>
/// 看板列配置 DTO
/// </summary>
public class KanbanColumnDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int? WipLimit { get; set; }
    public bool AllowDrop { get; set; } = true;
    public int Order { get; set; }
}

/// <summary>
/// 看板配置 DTO
/// </summary>
public class KanbanBoardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<KanbanColumnDto> Columns { get; set; } = new();
    public bool EnableSwimLanes { get; set; }
    public string? SwimLaneBy { get; set; }
    public string? CustomSwimLaneField { get; set; }
    public bool IsPublished { get; set; }
}

/// <summary>
/// 看板卡片 DTO
/// </summary>
public class KanbanCardDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public string? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public string? AssigneeAvatar { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
    public int Order { get; set; }
    public int AttachmentCount { get; set; }
    public int CommentCount { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}

/// <summary>
/// 创建/更新卡片 DTO
/// </summary>
public class KanbanCardCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public string? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
}

/// <summary>
/// 移动卡片 DTO
/// </summary>
public class KanbanMoveCardDto
{
    public string CardId { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public int NewOrder { get; set; }
}

/// <summary>
/// 移动卡片响应 DTO
/// </summary>
public class KanbanMoveCardResponseDto
{
    public bool Success { get; set; }
    public int NewOrder { get; set; }
    public List<string> AffectedCardIds { get; set; } = new();
}

/// <summary>
/// 看板活动记录 DTO
/// </summary>
public class KanbanActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedTime { get; set; }
}

/// <summary>
/// 看板筛选条件 DTO
/// </summary>
public class KanbanFilterDto
{
    public string? Search { get; set; }
    public List<string>? Priorities { get; set; }
    public List<string>? Assignees { get; set; }
    public List<string>? Tags { get; set; }
    public string? DueDateFilter { get; set; }
}

/// <summary>
/// 数据源字段映射 DTO
/// </summary>
public class KanbanFieldMappingDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Priority { get; set; }
    public string? Assignee { get; set; }
    public string? DueDate { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// 看板数据源配置 DTO
/// </summary>
public class KanbanDataSourceDto
{
    public string Type { get; set; } = "report"; // report, sql
    public int? ReportId { get; set; }
    public string? Sql { get; set; }
    public int? ConnectionId { get; set; }
    public KanbanFieldMappingDto FieldMapping { get; set; } = new();
}
