namespace DataForgeStudio.Core.Configuration;

/// <summary>
/// 安全配置选项（从环境变量读取）
/// </summary>
public class SecurityOptions
{
    public const string SectionName = "Security";

    public JwtOptions Jwt { get; set; } = new();
    public EncryptionOptions Encryption { get; set; } = new();
    public LicenseOptions License { get; set; } = new();

    /// <summary>
    /// JWT 配置选项
    /// </summary>
    public class JwtOptions
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = "DataForgeStudio";
        public string Audience { get; set; } = "DataForgeStudio.Client";
        public int ExpirationMinutes { get; set; } = 60;
    }

    /// <summary>
    /// 加密配置选项
    /// </summary>
    public class EncryptionOptions
    {
        public string AesKey { get; set; } = string.Empty;
        public string AesIV { get; set; } = string.Empty;
    }

    /// <summary>
    /// 许可证配置选项
    /// </summary>
    public class LicenseOptions
    {
        public string AesKey { get; set; } = string.Empty;
        public string AesIv { get; set; } = string.Empty;
    }
}
