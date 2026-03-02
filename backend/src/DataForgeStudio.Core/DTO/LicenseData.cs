using System.Text.Json.Serialization;

namespace DataForgeStudio.Core.DTO;

/// <summary>
/// 许可证数据（用于序列化）
/// 包含所有许可证信息，将被加密后存储
/// </summary>
public class LicenseData
{
    /// <summary>
    /// 许可证唯一标识
    /// </summary>
    public string LicenseId { get; set; } = string.Empty;

    /// <summary>
    /// 客户名称
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 过期日期
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// 最大用户数
    /// </summary>
    public int MaxUsers { get; set; }

    /// <summary>
    /// 最大报表数
    /// </summary>
    public int MaxReports { get; set; }

    /// <summary>
    /// 最大数据源数
    /// </summary>
    public int MaxDataSources { get; set; }

    /// <summary>
    /// 最大大屏数量（0 表示无限制）
    /// </summary>
    public int MaxDashboards { get; set; }

    /// <summary>
    /// 功能列表
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// 绑定的机器码
    /// </summary>
    public string MachineCode { get; set; } = string.Empty;

    /// <summary>
    /// 签发日期
    /// </summary>
    public string IssuedDate { get; set; } = string.Empty;

    /// <summary>
    /// 许可证类型 (Trial, Standard, Professional, Enterprise)
    /// </summary>
    public string LicenseType { get; set; } = string.Empty;

    /// <summary>
    /// RSA 签名（Base64）
    /// </summary>
    public string Signature { get; set; } = string.Empty;
}
