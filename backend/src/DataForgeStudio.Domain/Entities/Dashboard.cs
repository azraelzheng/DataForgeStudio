using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 看板配置表
/// </summary>
[Table("Dashboards")]
public class Dashboard
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DashboardId { get; set; }

    /// <summary>
    /// 看板 GUID (用于前端引用)
    /// </summary>
    [MaxLength(50)]
    public string? DashboardGuid { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 分类
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// 布局配置 (JSON)
    /// </summary>
    [Column(TypeName = "nvarchar(MAX)")]
    public string? LayoutConfig { get; set; }

    /// <summary>
    /// 数据刷新间隔（秒）
    /// </summary>
    public int RefreshInterval { get; set; } = 60;

    /// <summary>
    /// 是否发布
    /// </summary>
    public bool IsPublished { get; set; } = false;

    public int CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 导航属性 - 看板组件
    /// </summary>
    public virtual ICollection<DashboardWidget> Widgets { get; set; } = new List<DashboardWidget>();
}

/// <summary>
/// 看板组件表
/// </summary>
[Table("DashboardWidgets")]
public class DashboardWidget
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int WidgetId { get; set; }

    /// <summary>
    /// 组件 GUID
    /// </summary>
    [MaxLength(50)]
    public string? WidgetGuid { get; set; }

    [Required]
    public int DashboardId { get; set; }

    /// <summary>
    /// 组件类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 组件名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 位置配置 (JSON) - 包含 x, y, width, height
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(MAX)")]
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// 组件配置 (JSON)
    /// </summary>
    [Column(TypeName = "nvarchar(MAX)")]
    public string? Config { get; set; }

    /// <summary>
    /// 数据绑定配置 (JSON)
    /// </summary>
    [Column(TypeName = "nvarchar(MAX)")]
    public string? DataBinding { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 导航属性 - 看板
    /// </summary>
    [ForeignKey(nameof(DashboardId))]
    public virtual Dashboard Dashboard { get; set; } = null!;
}

/// <summary>
/// 车间大屏配置表
/// </summary>
[Table("DisplayConfigs")]
public class DisplayConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ConfigId { get; set; }

    /// <summary>
    /// 配置 GUID (用于前端引用)
    /// </summary>
    [MaxLength(50)]
    public string? ConfigGuid { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 看板 ID 列表 (JSON 数组，存储 GUID)
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(MAX)")]
    public string DashboardIds { get; set; } = "[]";

    /// <summary>
    /// 轮播间隔（秒）
    /// </summary>
    public int Interval { get; set; } = 30;

    /// <summary>
    /// 数据刷新间隔（秒）
    /// </summary>
    public int AutoRefresh { get; set; } = 60;

    /// <summary>
    /// 转场效果：fade, slide, none
    /// </summary>
    [MaxLength(20)]
    public string Transition { get; set; } = "fade";

    /// <summary>
    /// 是否显示时钟
    /// </summary>
    public bool ShowClock { get; set; } = true;

    /// <summary>
    /// 是否显示看板名称
    /// </summary>
    public bool ShowDashboardName { get; set; } = true;

    /// <summary>
    /// 是否循环播放
    /// </summary>
    public bool Loop { get; set; } = true;

    /// <summary>
    /// 是否悬停暂停
    /// </summary>
    public bool PauseOnHover { get; set; } = true;

    public int CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedTime { get; set; }
}
