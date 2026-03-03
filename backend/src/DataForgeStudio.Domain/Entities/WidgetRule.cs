using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 组件规则配置表
/// 用于定义组件数据的条件格式化规则（如阈值变色、图标显示等）
/// </summary>
[Table("WidgetRules")]
public class WidgetRule
{
    /// <summary>
    /// 规则ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RuleId { get; set; }

    /// <summary>
    /// 所属组件ID
    /// </summary>
    public int WidgetId { get; set; }

    /// <summary>
    /// 规则名称
    /// </summary>
    [MaxLength(50)]
    public string? RuleName { get; set; }

    /// <summary>
    /// 要比较的字段名
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 比较操作符：lt（小于）, lte（小于等于）, gt（大于）, gte（大于等于）,
    /// eq（等于）, neq（不等于）, between（区间）, contains（包含）
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// 比较值（对于 between 操作符，使用逗号分隔两个值）
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 动作类型：setColor（设置颜色）, setIcon（设置图标）, showText（显示文本）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// 动作值（如颜色值、图标名称、文本内容等）
    /// </summary>
    [MaxLength(100)]
    public string? ActionValue { get; set; }

    /// <summary>
    /// 规则优先级（数值越小优先级越高）
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 所属组件
    /// </summary>
    [ForeignKey(nameof(WidgetId))]
    public virtual DashboardWidget Widget { get; set; } = null!;
}
