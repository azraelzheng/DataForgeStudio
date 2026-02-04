using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 用户表
/// </summary>
[Table("Users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? RealName { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    [MaxLength(50)]
    public string? Position { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 系统内置用户 (root)
    /// </summary>
    public bool IsSystem { get; set; } = false;

    public bool IsLocked { get; set; } = false;

    public DateTime? LastLoginTime { get; set; }

    [MaxLength(50)]
    public string? LastLoginIP { get; set; }

    public int PasswordFailCount { get; set; } = 0;

    public bool MustChangePassword { get; set; } = false;

    [MaxLength(500)]
    public string? Remark { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 导航属性 - 用户角色关联
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// 导航属性 - 操作日志
    /// </summary>
    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();

    /// <summary>
    /// 导航属性 - 登录日志
    /// </summary>
    public virtual ICollection<LoginLog> LoginLogs { get; set; } = new List<LoginLog>();
}
