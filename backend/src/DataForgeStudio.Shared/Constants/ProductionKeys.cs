// backend/src/DataForgeStudio.Shared/Constants/ProductionKeys.cs
namespace DataForgeStudio.Shared.Constants;

/// <summary>
/// 生产环境安全密钥
///
/// 重要说明：
/// 1. 此文件包含生产环境使用的所有安全密钥
/// 2. 发布新版本前，请更新这些密钥
/// 3. 备份 secrets.production.json 到安全位置
/// 4. 密钥长度要求：
///    - AES Key: 精确 32 字符
///    - AES IV: 精确 16 字符
///    - JWT Secret: 至少 64 字符
/// </summary>
public static class ProductionKeys
{
    /// <summary>
    /// 数据加密 AES 密钥（精确 32 字符）
    /// 用于：数据库连接字符串密码加密、数据源密码加密
    /// </summary>
    public const string AesKey = "DataForgeStudioV4AESKey32Bytes!!";

    /// <summary>
    /// 数据加密 AES IV（精确 16 字符）
    /// </summary>
    public const string AesIV = "DataForgeIV16Byt";

    /// <summary>
    /// JWT 签名密钥（至少 64 字符）
    /// 用于：用户登录 Token 签名
    /// </summary>
    public const string JwtSecret = "DataForgeStudioV4JWTSecretKey256BitsLongSecure2025ChangeThisInProduction";

    /// <summary>
    /// 许可证加密 AES 密钥（精确 32 字符）
    /// 用于：许可证文件加密
    /// </summary>
    public const string LicenseAesKey = "DataForgeStudioV4LicenseAES32!!!";

    /// <summary>
    /// 许可证加密 AES IV（精确 16 字符）
    /// </summary>
    public const string LicenseAesIV = "LicenseIV16Bytes";

    // ====== JWT 配置（通常不需要修改） ======

    public const string JwtIssuer = "DataForgeStudio";
    public const string JwtAudience = "DataForgeStudio.Client";
    public const int JwtExpirationMinutes = 15;
}
