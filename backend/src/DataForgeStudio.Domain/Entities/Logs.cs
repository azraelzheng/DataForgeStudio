using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 操作日志表
/// </summary>
[Table("OperationLogs")]
public class OperationLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogId { get; set; }

    public int? UserId { get; set; }

    [MaxLength(50)]
    public string? Username { get; set; }

    [Required]
    [MaxLength(50)]
    public string Module { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ActionType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(500)]
    public string? RequestUrl { get; set; }

    [MaxLength(10)]
    public string? RequestMethod { get; set; }

    public string? RequestData { get; set; }

    public string? ResponseData { get; set; }

    public int? Duration { get; set; }

    public bool IsSuccess { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}

/// <summary>
/// 登录日志表
/// </summary>
[Table("LoginLogs")]
public class LoginLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogId { get; set; }

    public int? UserId { get; set; }

    [MaxLength(50)]
    public string? Username { get; set; }

    public DateTime LoginTime { get; set; } = DateTime.UtcNow;

    public DateTime? LogoutTime { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(20)]
    public string? LoginStatus { get; set; }

    [MaxLength(200)]
    public string? FailureReason { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; }

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}
