using Microsoft.Extensions.Configuration;

namespace DataForgeStudio.Core.Configuration;

/// <summary>
/// 安全配置选项（从环境变量或配置文件读取）
/// 环境变量优先于配置文件
/// </summary>
public class SecurityOptions
{
    public const string SectionName = "Security";

    private JwtOptions? _jwt;
    private EncryptionOptions? _encryption;
    private LicenseOptions? _license;

    public JwtOptions Jwt => _jwt ??= new JwtOptions();
    public EncryptionOptions Encryption => _encryption ??= new EncryptionOptions();
    public LicenseOptions License => _license ??= new LicenseOptions();

    /// <summary>
    /// 获取实际使用的 JWT 配置（优先使用环境变量）
    /// </summary>
    public JwtOptions GetJwtOptions(IConfiguration configuration)
    {
        var options = new JwtOptions();

        // 从配置文件读取（作为默认值）
        var configSection = configuration.GetSection(SectionName);
        var jwtConfig = configSection.GetSection("Jwt");
        options.Secret = Environment.GetEnvironmentVariable("DFS_JWT_SECRET") ?? jwtConfig["Secret"] ?? string.Empty;
        options.Issuer = jwtConfig["Issuer"] ?? "DataForgeStudio";
        options.Audience = jwtConfig["Audience"] ?? "DataForgeStudio.Client";
        options.ExpirationMinutes = int.Parse(jwtConfig["ExpirationMinutes"] ?? "15");

        return options;
    }

    /// <summary>
    /// 获取实际使用的加密配置（优先使用环境变量）
    /// </summary>
    public EncryptionOptions GetEncryptionOptions(IConfiguration configuration)
    {
        var options = new EncryptionOptions();

        // 从配置文件读取（作为默认值）
        var configSection = configuration.GetSection(SectionName);
        var encryptionConfig = configSection.GetSection("Encryption");
        options.AesKey = Environment.GetEnvironmentVariable("DFS_ENCRYPTION_AESKEY") ?? encryptionConfig["AesKey"] ?? string.Empty;
        options.AesIV = Environment.GetEnvironmentVariable("DFS_ENCRYPTION_AESIV") ?? encryptionConfig["AesIV"] ?? string.Empty;

        return options;
    }

    /// <summary>
    /// 获取实际使用的许可证配置（优先使用环境变量）
    /// </summary>
    public LicenseOptions GetLicenseOptions(IConfiguration configuration)
    {
        var options = new LicenseOptions();

        // 从配置文件读取（作为默认值）
        var configSection = configuration.GetSection(SectionName);
        var licenseConfig = configSection.GetSection("License");
        options.AesKey = Environment.GetEnvironmentVariable("DFS_LICENSE_AESKEY") ?? licenseConfig["AesKey"] ?? string.Empty;
        options.AesIv = Environment.GetEnvironmentVariable("DFS_LICENSE_AESIV") ?? licenseConfig["AesIv"] ?? string.Empty;

        return options;
    }

    /// <summary>
    /// JWT 配置选项
    /// </summary>
    public class JwtOptions
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = "DataForgeStudio";
        public string Audience { get; set; } = "DataForgeStudio.Client";
        public int ExpirationMinutes { get; set; } = 15;
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
