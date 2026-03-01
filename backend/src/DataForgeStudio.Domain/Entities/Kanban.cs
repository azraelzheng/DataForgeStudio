using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 看板配置表
/// </summary>
[Table("KanbanBoards")]
public class KanbanBoard
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BoardId { get; set; }

    [Required]
    [MaxLength(100)]
    public string BoardName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 列配置 (JSON)
    /// </summary>
    [Column(TypeName = "nvarchar(MAX)")]
    public string ColumnsConfig { get; set; } = "[]";

    /// <summary>
    /// 数据源配置 (JSON)
    /// </summary>
    [Column(TypeName = "nvarchar(MAX)")]
    public string? DataSourceConfig { get; set; }

    /// <summary>
    /// 是否启用泳道视图
    /// </summary>
    public bool EnableSwimLanes { get; set; } = false;

    /// <summary>
    /// 泳道分组字段
    /// </summary>
    [MaxLength(50)]
    public string? SwimLaneBy { get; set; }

    /// <summary>
    /// 自定义泳道字段名
    /// </summary>
    [MaxLength(100)]
    public string? CustomSwimLaneField { get; set; }

    public bool IsPublished { get; set; } = false;

    public int? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 导航属性 - 卡片
    /// </summary>
    public virtual ICollection<KanbanCard> Cards { get; set; } = new List<KanbanCard>();
}

/// <summary>
/// 看板卡片表
/// </summary>
[Table("KanbanCards")]
public class KanbanCard
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CardId { get; set; }

    [Required]
    public int BoardId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 卡片描述
    /// </summary>
    [Column(TypeName = "nvarchar(MAX)")]
    public string? Description { get; set; }

    /// <summary>
    /// 卡片状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 优先级：low, medium, high, urgent
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "medium";

    /// <summary>
    /// 负责人 ID (关联用户表或自定义)
    /// </summary>
    [MaxLength(100)]
    public string? AssigneeId { get; set; }

    /// <summary>
    /// 负责人名称 (冗余字段)
    /// </summary>
    [MaxLength(100)]
    public string? AssigneeName { get; set; }

    /// <summary>
    /// 负责人头像 URL
    /// </summary>
    [MaxLength(500)]
    public string? AssigneeAvatar { get; set; }

    /// <summary>
    /// 截止日期
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// 标签 (JSON 数组)
    /// </summary>
    [Column(TypeName = "nvarchar(MAX)")]
    public string? Tags { get; set; }

    /// <summary>
    /// 自定义字段 (JSON 对象)
    /// </summary>
    [Column(TypeName = "nvarchar(MAX)")]
    public string? CustomFields { get; set; }

    /// <summary>
    /// 在列中的顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 附件数量
    /// </summary>
    public int AttachmentCount { get; set; } = 0;

    /// <summary>
    /// 评论数量
    /// </summary>
    public int CommentCount { get; set; } = 0;

    /// <summary>
    /// 创建者 ID
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 导航属性 - 看板
    /// </summary>
    [ForeignKey(nameof(BoardId))]
    public virtual KanbanBoard Board { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 活动记录
    /// </summary>
    public virtual ICollection<KanbanActivity> Activities { get; set; } = new List<KanbanActivity>();
}

/// <summary>
/// 看板活动记录表
/// </summary>
[Table("KanbanActivities")]
public class KanbanActivity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ActivityId { get; set; }

    [Required]
    public int CardId { get; set; }

    /// <summary>
    /// 用户 ID
    /// </summary>
    [MaxLength(100)]
    public string? UserId { get; set; }

    /// <summary>
    /// 用户名称
    /// </summary>
    [MaxLength(100)]
    public string? UserName { get; set; }

    /// <summary>
    /// 活动类型：created, updated, moved, commented, assigned
    /// </summary>
    [MaxLength(50)]
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// 活动描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 变更前的值 (JSON)
    /// </summary>
    [Column(TypeName = "nvarchar(MAX)")]
    public string? OldValue { get; set; }

    /// <summary>
    /// 变更后的值 (JSON)
    /// </summary>
    [Column(TypeName = "nvarchar(MAX)")]
    public string? NewValue { get; set; }

    /// <summary>
    /// IP 地址
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 卡片
    /// </summary>
    [ForeignKey(nameof(CardId))]
    public virtual KanbanCard Card { get; set; } = null!;
}

/// <summary>
/// 看板附件表
/// </summary>
[Table("KanbanAttachments")]
public class KanbanAttachment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AttachmentId { get; set; }

    [Required]
    public int CardId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FileType { get; set; }

    public long FileSize { get; set; }

    /// <summary>
    /// 上传者 ID
    /// </summary>
    [MaxLength(100)]
    public string? UploadedBy { get; set; }

    public DateTime UploadedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 卡片
    /// </summary>
    [ForeignKey(nameof(CardId))]
    public virtual KanbanCard Card { get; set; } = null!;
}

/// <summary>
/// 看板评论表
/// </summary>
[Table("KanbanComments")]
public class KanbanComment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CommentId { get; set; }

    [Required]
    public int CardId { get; set; }

    /// <summary>
    /// 评论者 ID
    /// </summary>
    [MaxLength(100)]
    public string? UserId { get; set; }

    /// <summary>
    /// 评论者名称
    /// </summary>
    [MaxLength(100)]
    public string? UserName { get; set; }

    /// <summary>
    /// 评论内容
    /// </summary>
    [Column(TypeName = "nvarchar(MAX)")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// IP 地址
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 卡片
    /// </summary>
    [ForeignKey(nameof(CardId))]
    public virtual KanbanCard Card { get; set; } = null!;
}
