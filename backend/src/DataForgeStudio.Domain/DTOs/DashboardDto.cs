using Newtonsoft.Json;

namespace DataForgeStudio.Domain.DTOs;

/// <summary>
/// 看板位置配置 DTO
/// </summary>
public class DashboardPositionDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// 看板布局配置 DTO
/// </summary>
public class DashboardLayoutDto
{
    public int Columns { get; set; } = 12;
    public int RowHeight { get; set; } = 60;
    public int Gap { get; set; } = 16;
    public int? MaxRows { get; set; }
}

/// <summary>
/// 看板组件 DTO
/// </summary>
public class DashboardWidgetDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DashboardPositionDto Position { get; set; } = new();
    public Dictionary<string, object>? Config { get; set; }
    public Dictionary<string, object>? DataBinding { get; set; }
    public int DisplayOrder { get; set; }
    public string? DashboardId { get; set; }
}

/// <summary>
/// 看板配置 DTO
/// </summary>
public class DashboardDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DashboardLayoutDto Layout { get; set; } = new();
    public int RefreshInterval { get; set; } = 60;
    public bool IsPublished { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
    public List<DashboardWidgetDto> Widgets { get; set; } = new();
}

/// <summary>
/// 创建看板 DTO
/// </summary>
public class DashboardCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DashboardLayoutDto? Layout { get; set; }
    public int RefreshInterval { get; set; } = 60;
    public bool IsPublished { get; set; } = false;
}

/// <summary>
/// 更新看板 DTO
/// </summary>
public class DashboardUpdateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DashboardLayoutDto? Layout { get; set; }
    public int? RefreshInterval { get; set; }
    public bool? IsPublished { get; set; }
}

/// <summary>
/// 车间大屏配置 DTO
/// </summary>
public class DisplayConfigDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> DashboardIds { get; set; } = new();
    public int Interval { get; set; } = 30;
    public int AutoRefresh { get; set; } = 60;
    public string Transition { get; set; } = "fade";
    public bool ShowClock { get; set; } = true;
    public bool ShowDashboardName { get; set; } = true;
    public bool Loop { get; set; } = true;
    public bool PauseOnHover { get; set; } = true;
    public int CreatedBy { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}

/// <summary>
/// 创建车间大屏配置 DTO
/// </summary>
public class DisplayConfigCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> DashboardIds { get; set; } = new();
    public int Interval { get; set; } = 30;
    public int AutoRefresh { get; set; } = 60;
    public string Transition { get; set; } = "fade";
    public bool ShowClock { get; set; } = true;
    public bool ShowDashboardName { get; set; } = true;
    public bool Loop { get; set; } = true;
    public bool PauseOnHover { get; set; } = true;
}

/// <summary>
/// 更新车间大屏配置 DTO
/// </summary>
public class DisplayConfigUpdateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? DashboardIds { get; set; }
    public int? Interval { get; set; }
    public int? AutoRefresh { get; set; }
    public string? Transition { get; set; }
    public bool? ShowClock { get; set; }
    public bool? ShowDashboardName { get; set; }
    public bool? Loop { get; set; }
    public bool? PauseOnHover { get; set; }
}

/// <summary>
/// 车间大屏数据响应 DTO
/// </summary>
public class DisplayDataResponseDto
{
    public Dictionary<string, DisplayDashboardDataDto> Dashboards { get; set; } = new();
    public long Timestamp { get; set; }
}

/// <summary>
/// 单个看板数据 DTO
/// </summary>
public class DisplayDashboardDataDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public object? Data { get; set; }
}
