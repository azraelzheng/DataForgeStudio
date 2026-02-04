using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 角色表
/// </summary>
[Table("Roles")]
public class Role
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RoleId { get; set; }

    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string RoleCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 权限列表 (JSON)
    /// </summary>
    public string? Permissions { get; set; }

    public bool IsSystem { get; set; } = false;

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public int? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 导航属性 - 用户角色关联
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// 导航属性 - 角色权限关联
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
