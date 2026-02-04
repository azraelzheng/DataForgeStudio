using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 密钥管理服务 - 负责生成和管理 RSA 密钥对及 AES 密钥
/// </summary>
public class KeyManagementService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeyManagementService> _logger;
    private readonly string _publicKeyPath;
    private readonly string _privateKeyPath;
    private readonly string _aesKey;
    private readonly string _aesIv;

    public KeyManagementService(
        IConfiguration configuration,
        ILogger<KeyManagementService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // 从配置读取密钥路径
        var licenseSection = _configuration.GetSection("License");
        _publicKeyPath = licenseSection["PublicKeyPath"] ?? "keys/public_key.pem";
        _privateKeyPath = licenseSection["PrivateKeyPath"] ?? "keys/private_key.pem";
        _aesKey = licenseSection["AesKey"] ?? "DataForgeStudioV4AESLicenseKey32Bytes!!";
        _aesIv = licenseSection["AesIv"] ?? "DataForgeI";
    }

    /// <summary>
    /// 确保 RSA 密钥对存在，不存在则生成新的密钥对
    /// </summary>
    public async Task EnsureKeyPairExistsAsync()
    {
        // 检查密钥文件是否已存在
        if (File.Exists(_publicKeyPath) && File.Exists(_privateKeyPath))
        {
            _logger.LogInformation("RSA 密钥对已存在，跳过生成");
            return;
        }

        _logger.LogInformation("开始生成 RSA 2048 位密钥对...");

        try
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(_publicKeyPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("创建密钥目录: {Directory}", directory);
            }

            // 生成 RSA 密钥对
            using var rsa = RSA.Create(2048);

            // 导出公钥 (二进制格式)
            var publicKeyBytes = rsa.ExportRSAPublicKey();
            await File.WriteAllBytesAsync(_publicKeyPath, publicKeyBytes);
            _logger.LogInformation("公钥已保存到: {PublicKeyPath}", _publicKeyPath);

            // 导出私钥 (二进制格式)
            var privateKeyBytes = rsa.ExportRSAPrivateKey();
            await File.WriteAllBytesAsync(_privateKeyPath, privateKeyBytes);
            _logger.LogInformation("私钥已保存到: {PrivateKeyPath}", _privateKeyPath);

            _logger.LogInformation("RSA 密钥对生成完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成 RSA 密钥对失败");
            throw;
        }
    }

    /// <summary>
    /// 确保 AES 密钥配置存在
    /// </summary>
    public async Task EnsureAesKeyExistsAsync()
    {
        // 验证 AES 密钥配置
        if (string.IsNullOrEmpty(_aesKey) || _aesKey.Length < 32)
        {
            _logger.LogWarning("AES 密钥配置无效或长度不足32字节");
        }

        if (string.IsNullOrEmpty(_aesIv) || _aesIv.Length < 16)
        {
            _logger.LogWarning("AES IV 配置无效或长度不足16字节");
        }

        await Task.CompletedTask;
        _logger.LogInformation("AES 密钥配置验证完成");
    }

    /// <summary>
    /// 获取公钥 (Base64 编码)
    /// </summary>
    public async Task<string> GetPublicKeyAsync()
    {
        if (!File.Exists(_publicKeyPath))
        {
            _logger.LogError("公钥文件不存在: {PublicKeyPath}", _publicKeyPath);
            throw new FileNotFoundException("公钥文件不存在，请先调用 EnsureKeyPairExistsAsync 生成密钥对", _publicKeyPath);
        }

        var publicKeyBytes = await File.ReadAllBytesAsync(_publicKeyPath);
        return Convert.ToBase64String(publicKeyBytes);
    }

    /// <summary>
    /// 获取私钥 (Base64 编码) - 仅限内部使用
    /// </summary>
    private async Task<string> GetPrivateKeyAsync()
    {
        if (!File.Exists(_privateKeyPath))
        {
            _logger.LogError("私钥文件不存在: {PrivateKeyPath}", _privateKeyPath);
            throw new FileNotFoundException("私钥文件不存在，请先调用 EnsureKeyPairExistsAsync 生成密钥对", _privateKeyPath);
        }

        var privateKeyBytes = await File.ReadAllBytesAsync(_privateKeyPath);
        return Convert.ToBase64String(privateKeyBytes);
    }

    /// <summary>
    /// 获取 AES 密钥
    /// </summary>
    public string GetAesKey()
    {
        return _aesKey;
    }

    /// <summary>
    /// 获取 AES IV 向量
    /// </summary>
    public string GetAesIv()
    {
        return _aesIv;
    }

    /// <summary>
    /// 获取 RSA 实例并导入私钥 - 用于许可证签名等内部操作
    /// </summary>
    public async Task<RSA> GetRsaWithPrivateKeyAsync()
    {
        var privateKeyBase64 = await GetPrivateKeyAsync();
        var privateKeyBytes = Convert.FromBase64String(privateKeyBase64);

        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        return rsa;
    }

    /// <summary>
    /// 获取 RSA 实例并导入公钥 - 用于许可证验证
    /// </summary>
    public async Task<RSA> GetRsaWithPublicKeyAsync()
    {
        var publicKeyBase64 = await GetPublicKeyAsync();
        var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKeyBytes, out _);
        return rsa;
    }
}
