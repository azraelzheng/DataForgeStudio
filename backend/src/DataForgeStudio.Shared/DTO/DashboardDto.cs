namespace DataForgeStudio.Shared.DTO;

#region 大屏 DTO

/// <summary>
/// 大屏基本信息 DTO
/// </summary>
public class DashboardDto
{
    /// <summary>
    /// 大屏ID
    /// </summary>
    public int DashboardId { get; set; }

    /// <summary>
    /// 大屏名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 大屏描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 主题：dark（深色）, light（浅色）
    /// </summary>
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// 自动刷新间隔（秒）
    /// </summary>
    public int RefreshInterval { get; set; } = 30;

    /// <summary>
    /// 是否公开（无需登录即可访问）
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 大屏状态：draft（草稿）/ published（已发布）
    /// </summary>
    public string Status { get; set; } = "draft";

    /// <summary>
    /// 公开访问URL标识
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>
    /// 画布宽度
    /// </summary>
    public int Width { get; set; } = 1920;

    /// <summary>
    /// 画布高度
    /// </summary>
    public int Height { get; set; } = 1080;

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string BackgroundColor { get; set; } = "#0a1628";

    /// <summary>
    /// 背景图片URL
    /// </summary>
    public string? BackgroundImage { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 创建人用户名
    /// </summary>
    public string? CreatorName { get; set; }

    /// <summary>
    /// 组件数量
    /// </summary>
    public int WidgetCount { get; set; }
}

/// <summary>
/// 大屏详情 DTO（包含组件列表）
/// </summary>
public class DashboardDetailDto : DashboardDto
{
    /// <summary>
    /// 布局配置 (JSON)
    /// </summary>
    public string? LayoutConfig { get; set; }

    /// <summary>
    /// 主题配置 (JSON)
    /// </summary>
    public string? ThemeConfig { get; set; }

    /// <summary>
    /// 组件列表
    /// </summary>
    public List<DashboardWidgetDto> Widgets { get; set; } = new();
}

/// <summary>
/// 创建/更新大屏请求
/// </summary>
public class CreateDashboardRequest
{
    /// <summary>
    /// 大屏ID（更新时必填）
    /// </summary>
    public int? DashboardId { get; set; }

    /// <summary>
    /// 大屏名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 大屏描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 主题：dark（深色）, light（浅色）
    /// </summary>
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// 自动刷新间隔（秒）
    /// </summary>
    public int RefreshInterval { get; set; } = 30;

    /// <summary>
    /// 是否公开（无需登录即可访问）
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 布局配置 (JSON)
    /// </summary>
    public string? LayoutConfig { get; set; }

    /// <summary>
    /// 主题配置 (JSON)
    /// </summary>
    public string? ThemeConfig { get; set; }

    /// <summary>
    /// 画布宽度
    /// </summary>
    public int Width { get; set; } = 1920;

    /// <summary>
    /// 画布高度
    /// </summary>
    public int Height { get; set; } = 1080;

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string BackgroundColor { get; set; } = "#0a1628";

    /// <summary>
    /// 背景图片URL
    /// </summary>
    public string? BackgroundImage { get; set; }

    /// <summary>
    /// 模板ID（可选，用于从模板创建）
    /// </summary>
    public string? TemplateId { get; set; }
}

/// <summary>
/// 更新大屏访问设置请求
/// </summary>
public class UpdateDashboardAccessRequest
{
    /// <summary>
    /// 是否公开访问
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 授权用户ID列表
    /// </summary>
    public List<int>? AuthorizedUserIds { get; set; }
}

/// <summary>
/// 大屏访问设置DTO
/// </summary>
public class DashboardAccessDto
{
    /// <summary>
    /// 大屏ID
    /// </summary>
    public int DashboardId { get; set; }

    /// <summary>
    /// 是否公开访问
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 公开访问URL标识
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>
    /// 授权用户ID列表
    /// </summary>
    public List<int>? AuthorizedUserIds { get; set; }

    /// <summary>
    /// 大屏状态
    /// </summary>
    public string Status { get; set; } = "draft";
}

#endregion

#region 组件 DTO

/// <summary>
/// 大屏组件 DTO
/// </summary>
public class DashboardWidgetDto
{
    /// <summary>
    /// 组件ID
    /// </summary>
    public int WidgetId { get; set; }

    /// <summary>
    /// 所属大屏ID
    /// </summary>
    public int DashboardId { get; set; }

    /// <summary>
    /// 关联的报表ID
    /// </summary>
    public int ReportId { get; set; }

    /// <summary>
    /// 关联的报表名称
    /// </summary>
    public string? ReportName { get; set; }

    /// <summary>
    /// 组件类型：chart（图表）, table（表格）, statistics（统计卡片）, text（文本）等
    /// </summary>
    public required string WidgetType { get; set; }

    /// <summary>
    /// 组件标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 组件位置X坐标（网格单位）
    /// </summary>
    public int PositionX { get; set; }

    /// <summary>
    /// 组件位置Y坐标（网格单位）
    /// </summary>
    public int PositionY { get; set; }

    /// <summary>
    /// 组件宽度（网格单位）
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 组件高度（网格单位）
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 数据配置 (JSON)
    /// </summary>
    public string? DataConfig { get; set; }

    /// <summary>
    /// 样式配置 (JSON)
    /// </summary>
    public string? StyleConfig { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 规则列表
    /// </summary>
    public List<WidgetRuleDto> Rules { get; set; } = new();
}

/// <summary>
/// 创建/更新组件请求
/// </summary>
public class CreateWidgetRequest
{
    /// <summary>
    /// 组件ID（更新时必填）
    /// </summary>
    public int? WidgetId { get; set; }

    /// <summary>
    /// 所属大屏ID
    /// </summary>
    public int DashboardId { get; set; }

    /// <summary>
    /// 关联的报表ID
    /// </summary>
    public int ReportId { get; set; }

    /// <summary>
    /// 组件类型：chart（图表）, table（表格）, statistics（统计卡片）, text（文本）等
    /// </summary>
    public required string WidgetType { get; set; }

    /// <summary>
    /// 组件标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 组件位置X坐标（网格单位）
    /// </summary>
    public int PositionX { get; set; }

    /// <summary>
    /// 组件位置Y坐标（网格单位）
    /// </summary>
    public int PositionY { get; set; }

    /// <summary>
    /// 组件宽度（网格单位）
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 组件高度（网格单位）
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 数据配置 (JSON)
    /// </summary>
    public string? DataConfig { get; set; }

    /// <summary>
    /// 样式配置 (JSON)
    /// </summary>
    public string? StyleConfig { get; set; }
}

/// <summary>
/// 组件位置更新请求
/// </summary>
public class WidgetPositionRequest
{
    /// <summary>
    /// 组件ID
    /// </summary>
    public int WidgetId { get; set; }

    /// <summary>
    /// 组件位置X坐标（网格单位）
    /// </summary>
    public int PositionX { get; set; }

    /// <summary>
    /// 组件位置Y坐标（网格单位）
    /// </summary>
    public int PositionY { get; set; }

    /// <summary>
    /// 组件宽度（网格单位）
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 组件高度（网格单位）
    /// </summary>
    public int Height { get; set; }
}

/// <summary>
/// 批量更新组件位置请求
/// </summary>
public class BatchWidgetPositionRequest
{
    /// <summary>
    /// 组件位置列表
    /// </summary>
    public List<WidgetPositionRequest> Widgets { get; set; } = new();
}

#endregion

#region 规则 DTO

/// <summary>
/// 组件规则 DTO
/// </summary>
public class WidgetRuleDto
{
    /// <summary>
    /// 规则ID
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// 所属组件ID
    /// </summary>
    public int WidgetId { get; set; }

    /// <summary>
    /// 规则名称
    /// </summary>
    public string? RuleName { get; set; }

    /// <summary>
    /// 要比较的字段名
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// 比较操作符：lt（小于）, lte（小于等于）, gt（大于）, gte（大于等于）,
    /// eq（等于）, neq（不等于）, between（区间）, contains（包含）
    /// </summary>
    public required string Operator { get; set; }

    /// <summary>
    /// 比较值（对于 between 操作符，使用逗号分隔两个值）
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// 动作类型：setColor（设置颜色）, setIcon（设置图标）, showText（显示文本）
    /// </summary>
    public required string ActionType { get; set; }

    /// <summary>
    /// 动作值（如颜色值、图标名称、文本内容等）
    /// </summary>
    public string? ActionValue { get; set; }

    /// <summary>
    /// 规则优先级（数值越小优先级越高）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }
}

/// <summary>
/// 创建/更新规则请求
/// </summary>
public class CreateWidgetRuleRequest
{
    /// <summary>
    /// 规则ID（更新时必填）
    /// </summary>
    public int? RuleId { get; set; }

    /// <summary>
    /// 所属组件ID
    /// </summary>
    public int WidgetId { get; set; }

    /// <summary>
    /// 规则名称
    /// </summary>
    public string? RuleName { get; set; }

    /// <summary>
    /// 要比较的字段名
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// 比较操作符：lt（小于）, lte（小于等于）, gt（大于）, gte（大于等于）,
    /// eq（等于）, neq（不等于）, between（区间）, contains（包含）
    /// </summary>
    public required string Operator { get; set; }

    /// <summary>
    /// 比较值（对于 between 操作符，使用逗号分隔两个值）
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// 动作类型：setColor（设置颜色）, setIcon（设置图标）, showText（显示文本）
    /// </summary>
    public required string ActionType { get; set; }

    /// <summary>
    /// 动作值（如颜色值、图标名称、文本内容等）
    /// </summary>
    public string? ActionValue { get; set; }

    /// <summary>
    /// 规则优先级（数值越小优先级越高）
    /// </summary>
    public int Priority { get; set; }
}

#endregion

#region 大屏数据响应 DTO

/// <summary>
/// 大屏数据响应 DTO
/// </summary>
public class DashboardDataDto
{
    /// <summary>
    /// 大屏基本信息
    /// </summary>
    public DashboardDto Dashboard { get; set; } = null!;

    /// <summary>
    /// 组件数据字典（WidgetId -> 数据）
    /// </summary>
    public Dictionary<int, WidgetDataResult> WidgetData { get; set; } = new();
}

/// <summary>
/// 组件数据结果
/// </summary>
public class WidgetDataResult
{
    /// <summary>
    /// 组件ID
    /// </summary>
    public int WidgetId { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 数据列表
    /// </summary>
    public List<Dictionary<string, object?>>? Data { get; set; }

    /// <summary>
    /// 数据获取时间
    /// </summary>
    public DateTime FetchTime { get; set; }
}

#endregion

#region 一键转换 DTO

/// <summary>
/// 一键转换报表为大屏请求
/// </summary>
public class ConvertReportToDashboardRequest
{
    /// <summary>
    /// 源报表ID
    /// </summary>
    public int ReportId { get; set; }

    /// <summary>
    /// 大屏名称（可选，默认使用报表名称）
    /// </summary>
    public string? DashboardName { get; set; }

    /// <summary>
    /// 大屏描述（可选，默认使用报表描述）
    /// </summary>
    public string? DashboardDescription { get; set; }

    /// <summary>
    /// 主题：dark（深色）, light（浅色）
    /// </summary>
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// 自动刷新间隔（秒）
    /// </summary>
    public int RefreshInterval { get; set; } = 30;
}

/// <summary>
/// 一键转换响应
/// </summary>
public class ConvertReportToDashboardResponse
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
    /// 创建的大屏ID
    /// </summary>
    public int? DashboardId { get; set; }

    /// <summary>
    /// 创建的大屏详情
    /// </summary>
    public DashboardDetailDto? Dashboard { get; set; }
}

#endregion

#region 公开访问 DTO

/// <summary>
/// 公开大屏访问响应
/// </summary>
public class PublicDashboardDto
{
    /// <summary>
    /// 大屏名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 大屏描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 主题
    /// </summary>
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// 主题配置 (JSON)
    /// </summary>
    public string? ThemeConfig { get; set; }

    /// <summary>
    /// 自动刷新间隔（秒）
    /// </summary>
    public int RefreshInterval { get; set; }

    /// <summary>
    /// 布局配置 (JSON)
    /// </summary>
    public string? LayoutConfig { get; set; }

    /// <summary>
    /// 组件列表（包含数据）
    /// </summary>
    public List<PublicWidgetDto> Widgets { get; set; } = new();
}

/// <summary>
/// 公开组件 DTO（包含数据）
/// </summary>
public class PublicWidgetDto
{
    /// <summary>
    /// 组件ID
    /// </summary>
    public int WidgetId { get; set; }

    /// <summary>
    /// 组件类型
    /// </summary>
    public string WidgetType { get; set; } = string.Empty;

    /// <summary>
    /// 组件标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 组件位置X坐标
    /// </summary>
    public int PositionX { get; set; }

    /// <summary>
    /// 组件位置Y坐标
    /// </summary>
    public int PositionY { get; set; }

    /// <summary>
    /// 组件宽度
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 组件高度
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 样式配置 (JSON)
    /// </summary>
    public string? StyleConfig { get; set; }

    /// <summary>
    /// 规则列表
    /// </summary>
    public List<WidgetRuleDto> Rules { get; set; } = new();

    /// <summary>
    /// 组件数据
    /// </summary>
    public WidgetDataResult? Data { get; set; }
}

#endregion
