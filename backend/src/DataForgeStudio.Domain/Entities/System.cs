using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 系统配置表
/// </summary>
[Table("SystemConfigs")]
public class SystemConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ConfigId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ConfigKey { get; set; } = string.Empty;

    public string? ConfigValue { get; set; }

    [Required]
    [MaxLength(20)]
    public string ConfigType { get; set; } = "String";

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsSystem { get; set; } = false;

    public int SortOrder { get; set; } = 0;

    public int? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedTime { get; set; }
}

/// <summary>
/// 备份记录表
/// </summary>
[Table("BackupRecords")]
public class BackupRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BackupId { get; set; }

    [Required]
    [MaxLength(200)]
    public string BackupName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string BackupType { get; set; } = "Manual";

    [Required]
    [MaxLength(500)]
    public string BackupPath { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DatabaseName { get; set; }

    public long? FileSize { get; set; }

    public DateTime BackupTime { get; set; } = DateTime.UtcNow;

    public bool IsSuccess { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 许可证表
/// </summary>
[Table("Licenses")]
public class License
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LicenseId { get; set; }

    /// <summary>
    /// RSA 加密的许可证密钥
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string LicenseKey { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(50)]
    public string? ContactPerson { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public int? MaxUsers { get; set; }

    public int? MaxReports { get; set; }

    public int? MaxDataSources { get; set; }

    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// 功能列表 (JSON)
    /// </summary>
    public string? Features { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? ActivatedTime { get; set; }

    [MaxLength(50)]
    public string? ActivatedIP { get; set; }

    [MaxLength(200)]
    public string? MachineCode { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedTime { get; set; }
}
