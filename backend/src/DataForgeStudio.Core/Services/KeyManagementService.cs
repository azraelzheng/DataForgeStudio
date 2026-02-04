using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 密钥管理服务 - 负责生成和管理 RSA 密钥对及 AES 密钥
/// </summary>
public class KeyManagementService : IKeyManagementService
{
    private const int DEFAULT_RSA_KEY_SIZE = 2048;

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

        _logger.LogInformation("开始生成 RSA {KeySize} 位密钥对...", DEFAULT_RSA_KEY_SIZE);

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
            using var rsa = RSA.Create(DEFAULT_RSA_KEY_SIZE);

            // 导出公钥 (二进制格式)
            var publicKeyBytes = rsa.ExportRSAPublicKey();
            await File.WriteAllBytesAsync(_publicKeyPath, publicKeyBytes);
            SetKeyFilePermissions(_publicKeyPath);
            _logger.LogInformation("公钥已保存到: {PublicKeyPath}", _publicKeyPath);

            // 导出私钥 (二进制格式)
            var privateKeyBytes = rsa.ExportRSAPrivateKey();
            await File.WriteAllBytesAsync(_privateKeyPath, privateKeyBytes);
            SetKeyFilePermissions(_privateKeyPath);
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
        // 验证 AES 密钥配置 - AES-256 需要 32 字节密钥
        if (string.IsNullOrEmpty(_aesKey) || _aesKey.Length < 32)
        {
            throw new InvalidOperationException(
                $"AES 密钥配置无效或长度不足32字节。当前长度: {_aesKey?.Length ?? 0}。请检查配置文件 License:AesKey。");
        }

        // AES-CBC 需要 16 字节 IV
        if (string.IsNullOrEmpty(_aesIv) || _aesIv.Length < 16)
        {
            throw new InvalidOperationException(
                $"AES IV 配置无效或长度不足16字节。当前长度: {_aesIv?.Length ?? 0}。请检查配置文件 License:AesIv。");
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
        try
        {
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "导入 RSA 私钥失败，密钥文件可能已损坏");
            throw new InvalidOperationException("无法导入 RSA 私钥，密钥文件可能已损坏。请重新生成密钥对。", ex);
        }

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
        try
        {
            rsa.ImportRSAPublicKey(publicKeyBytes, out _);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "导入 RSA 公钥失败，密钥文件可能已损坏");
            throw new InvalidOperationException("无法导入 RSA 公钥，密钥文件可能已损坏。请重新生成密钥对。", ex);
        }

        return rsa;
    }

    /// <summary>
    /// 设置密钥文件权限（Windows 平台）
    /// </summary>
    private void SetKeyFilePermissions(string filePath)
    {
        try
        {
            // 在 Windows 上设置文件为隐藏和系统文件，提供基础保护
            if (OperatingSystem.IsWindows())
            {
                var attributes = File.GetAttributes(filePath);
                File.SetAttributes(filePath, attributes | FileAttributes.System | FileAttributes.Hidden);
                _logger.LogDebug("已设置密钥文件权限: {FilePath}", filePath);
            }
            // Linux/Mac 平台跳过 chmod 设置，因为需要 P/Invoke 调用原生 API
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "设置密钥文件权限失败: {FilePath}", filePath);
            // 不抛出异常，权限设置失败不影响密钥生成
        }
    }
}
