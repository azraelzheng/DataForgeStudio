using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 用户角色关联表
/// </summary>
[Table("UserRoles")]
public class UserRole
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserRoleId { get; set; }

    public int UserId { get; set; }

    public int RoleId { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 角色
    /// </summary>
    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;
}

/// <summary>
/// 权限表
/// </summary>
[Table("Permissions")]
public class Permission
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PermissionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PermissionCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PermissionName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Module { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public int? ParentId { get; set; }

    public int SortOrder { get; set; } = 0;

    public bool IsSystem { get; set; } = false;

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 角色权限关联
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    /// <summary>
    /// 导航属性 - 父权限
    /// </summary>
    [ForeignKey(nameof(ParentId))]
    public virtual Permission? Parent { get; set; }

    /// <summary>
    /// 导航属性 - 子权限
    /// </summary>
    public virtual ICollection<Permission> Children { get; set; } = new List<Permission>();
}

/// <summary>
/// 角色权限关联表
/// </summary>
[Table("RolePermissions")]
public class RolePermission
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RolePermissionId { get; set; }

    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 角色
    /// </summary>
    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 权限
    /// </summary>
    [ForeignKey(nameof(PermissionId))]
    public virtual Permission Permission { get; set; } = null!;
}
