using System.Security.Cryptography;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 密钥管理服务接口 - 负责生成和管理 RSA 密钥对及 AES 密钥
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// 确保 RSA 密钥对存在，不存在则生成新的密钥对
    /// </summary>
    Task EnsureKeyPairExistsAsync();

    /// <summary>
    /// 确保 AES 密钥配置存在
    /// </summary>
    Task EnsureAesKeyExistsAsync();

    /// <summary>
    /// 获取公钥 (Base64 编码)
    /// </summary>
    Task<string> GetPublicKeyAsync();

    /// <summary>
    /// 获取 AES 密钥
    /// </summary>
    string GetAesKey();

    /// <summary>
    /// 获取 AES IV 向量
    /// </summary>
    string GetAesIv();

    /// <summary>
    /// 获取 RSA 实例并导入私钥 - 用于许可证签名等内部操作
    /// </summary>
    Task<RSA> GetRsaWithPrivateKeyAsync();

    /// <summary>
    /// 获取 RSA 实例并导入公钥 - 用于许可证验证
    /// </summary>
    Task<RSA> GetRsaWithPublicKeyAsync();
}
