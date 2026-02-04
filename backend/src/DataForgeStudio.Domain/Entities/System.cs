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
/// 许可证表 - 零信任架构，仅存储加密数据
/// </summary>
[Table("Licenses")]
public class License
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LicenseId { get; set; }

    /// <summary>
    /// AES 加密的完整许可证 JSON
    /// 包含所有许可证信息（客户名称、过期日期、功能列表等）
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string LicenseKey { get; set; } = string.Empty;

    /// <summary>
    /// RSA 签名（Base64）
    /// 用于验证许可证完整性和真实性
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// 绑定的机器码
    /// 许可证与特定服务器绑定，防止迁移使用
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string MachineCode { get; set; } = string.Empty;

    /// <summary>
    /// 激活时间
    /// </summary>
    public DateTime ActivatedTime { get; set; }

    /// <summary>
    /// 激活时的 IP 地址
    /// </summary>
    [MaxLength(50)]
    public string? ActivatedIP { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
