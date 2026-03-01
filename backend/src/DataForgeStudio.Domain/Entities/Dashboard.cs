using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 大屏配置表
/// </summary>
[Table("Dashboards")]
public class Dashboard
{
    /// <summary>
    /// 大屏ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DashboardId { get; set; }

    /// <summary>
    /// 大屏名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 大屏描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 主题：dark（深色）, light（浅色）
    /// </summary>
    [MaxLength(20)]
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// 自动刷新间隔（秒）
    /// </summary>
    public int RefreshInterval { get; set; } = 30;

    /// <summary>
    /// 是否公开（无需登录即可访问）
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 布局配置 (JSON)
    /// </summary>
    public string? LayoutConfig { get; set; }

    /// <summary>
    /// 主题配置 (JSON)
    /// </summary>
    public string? ThemeConfig { get; set; }

    /// <summary>
    /// 创建人ID
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 导航属性 - 大屏组件
    /// </summary>
    public virtual ICollection<DashboardWidget> Widgets { get; set; } = new List<DashboardWidget>();

    /// <summary>
    /// 导航属性 - 创建人
    /// </summary>
    [ForeignKey(nameof(CreatedBy))]
    public virtual User? Creator { get; set; }
}

/// <summary>
/// 大屏组件配置表
/// </summary>
[Table("DashboardWidgets")]
public class DashboardWidget
{
    /// <summary>
    /// 组件ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int WidgetId { get; set; }

    /// <summary>
    /// 所属大屏ID
    /// </summary>
    public int DashboardId { get; set; }

    /// <summary>
    /// 组件名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 组件类型：chart（图表）, table（表格）, statistics（统计卡片）, text（文本）等
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string WidgetType { get; set; } = string.Empty;

    /// <summary>
    /// 关联的报表ID（可选）
    /// </summary>
    public int? ReportId { get; set; }

    /// <summary>
    /// 组件位置X坐标（网格单位）
    /// </summary>
    public int PositionX { get; set; } = 0;

    /// <summary>
    /// 组件位置Y坐标（网格单位）
    /// </summary>
    public int PositionY { get; set; } = 0;

    /// <summary>
    /// 组件宽度（网格单位）
    /// </summary>
    public int Width { get; set; } = 1;

    /// <summary>
    /// 组件高度（网格单位）
    /// </summary>
    public int Height { get; set; } = 1;

    /// <summary>
    /// 组件配置 (JSON)
    /// </summary>
    public string? Config { get; set; }

    /// <summary>
    /// 数据源配置 (JSON)
    /// </summary>
    public string? DataSourceConfig { get; set; }

    /// <summary>
    /// 排序序号
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 导航属性 - 所属大屏
    /// </summary>
    [ForeignKey(nameof(DashboardId))]
    public virtual Dashboard Dashboard { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 关联报表
    /// </summary>
    [ForeignKey(nameof(ReportId))]
    public virtual Report? Report { get; set; }
}
