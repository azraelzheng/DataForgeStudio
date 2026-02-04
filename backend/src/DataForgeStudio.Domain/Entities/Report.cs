using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataForgeStudio.Domain.Entities;

/// <summary>
/// 数据源表
/// </summary>
[Table("DataSources")]
public class DataSource
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DataSourceId { get; set; }

    [Required]
    [MaxLength(100)]
    public string DataSourceName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DataSourceCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string DbType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ServerAddress { get; set; } = string.Empty;

    public int? Port { get; set; }

    [MaxLength(100)]
    public string? DatabaseName { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    /// <summary>
    /// AES 加密存储的密码
    /// </summary>
    [MaxLength(500)]
    public string? Password { get; set; }

    public bool IsIntegratedSecurity { get; set; } = false;

    public int ConnectionTimeout { get; set; } = 30;

    public int CommandTimeout { get; set; } = 60;

    public bool IsDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? TestSql { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public DateTime? LastTestTime { get; set; }

    public bool? LastTestResult { get; set; }

    [MaxLength(500)]
    public string? LastTestMessage { get; set; }

    /// <summary>
    /// 导航属性 - 报表
    /// </summary>
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}

/// <summary>
/// 报表定义表
/// </summary>
[Table("Reports")]
public class Report
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReportId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ReportName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ReportCode { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ReportCategory { get; set; }

    public int DataSourceId { get; set; }

    [Required]
    public string SqlStatement { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsPaged { get; set; } = true;

    public int PageSize { get; set; } = 50;

    public int CacheDuration { get; set; } = 0;

    public bool IsEnabled { get; set; } = true;

    public bool IsSystem { get; set; } = false;

    public int ViewCount { get; set; } = 0;

    public DateTime? LastViewTime { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 导航属性 - 数据源
    /// </summary>
    [ForeignKey(nameof(DataSourceId))]
    public virtual DataSource DataSource { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 报表字段
    /// </summary>
    public virtual ICollection<ReportField> Fields { get; set; } = new List<ReportField>();

    /// <summary>
    /// 导航属性 - 报表参数
    /// </summary>
    public virtual ICollection<ReportParameter> Parameters { get; set; } = new List<ReportParameter>();
}

/// <summary>
/// 报表字段配置表
/// </summary>
[Table("ReportFields")]
public class ReportField
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int FieldId { get; set; }

    public int ReportId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string DataType { get; set; } = string.Empty;

    public int Width { get; set; } = 100;

    public bool IsVisible { get; set; } = true;

    public bool IsSortable { get; set; } = true;

    public bool IsFilterable { get; set; } = false;

    public bool IsGroupable { get; set; } = false;

    public int SortOrder { get; set; } = 0;

    [MaxLength(10)]
    public string Align { get; set; } = "left";

    [MaxLength(50)]
    public string? FormatString { get; set; }

    [MaxLength(20)]
    public string? AggregateFunction { get; set; }

    [MaxLength(100)]
    public string? CssClass { get; set; }

    [MaxLength(200)]
    public string? Remark { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 报表
    /// </summary>
    [ForeignKey(nameof(ReportId))]
    public virtual Report Report { get; set; } = null!;
}

/// <summary>
/// 报表参数配置表
/// </summary>
[Table("ReportParameters")]
public class ReportParameter
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ParameterId { get; set; }

    public int ReportId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ParameterName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string DataType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string InputType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? DefaultValue { get; set; }

    public bool IsRequired { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Dropdown 选项配置 (JSON)
    /// </summary>
    public string? Options { get; set; }

    /// <summary>
    /// SQL 查询选项配置 (JSON)
    /// </summary>
    public string? QueryOptions { get; set; }

    [MaxLength(200)]
    public string? Remark { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 报表
    /// </summary>
    [ForeignKey(nameof(ReportId))]
    public virtual Report Report { get; set; } = null!;
}
