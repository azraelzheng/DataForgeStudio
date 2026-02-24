using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Core.Configuration;
using DataForgeStudio.Shared.Constants;

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
        var configPublicKeyPath = licenseSection["PublicKeyPath"] ?? "keys/public_key.pem";
        var configPrivateKeyPath = licenseSection["PrivateKeyPath"] ?? "keys/private_key.pem";

        // 解析路径：如果是相对路径，则基于应用程序目录解析
        // 这样无论从命令行运行还是作为 Windows 服务运行都能正确找到密钥文件
        _publicKeyPath = ResolvePath(configPublicKeyPath);
        _privateKeyPath = ResolvePath(configPrivateKeyPath);

        _logger.LogInformation("密钥文件路径: 公钥={PublicKeyPath}, 私钥={PrivateKeyPath}",
            _publicKeyPath, _privateKeyPath);

        // 优先从 Security:License 读取（Program.cs 可能已设置默认值）
        var securityLicenseSection = _configuration.GetSection("Security:License");
        _aesKey = securityLicenseSection["AesKey"] ?? licenseSection["AesKey"] ?? string.Empty;
        _aesIv = securityLicenseSection["AesIv"] ?? licenseSection["AesIv"] ?? string.Empty;

        // 如果仍然为空，使用 ProductionKeys 中的默认值（后备方案）
        if (string.IsNullOrEmpty(_aesKey))
        {
            _aesKey = ProductionKeys.LicenseAesKey;
            _logger.LogWarning("License AES Key 未配置，使用 ProductionKeys 默认值");
        }
        if (string.IsNullOrEmpty(_aesIv))
        {
            _aesIv = ProductionKeys.LicenseAesIV;
            _logger.LogWarning("License AES IV 未配置，使用 ProductionKeys 默认值");
        }
    }

    /// <summary>
    /// 解析路径：如果是相对路径，则基于应用程序目录解析
    /// 这样无论从命令行运行还是作为 Windows 服务运行都能正确找到文件
    /// </summary>
    private static string ResolvePath(string path)
    {
        // 如果是绝对路径，直接返回
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        // 获取应用程序基目录（对于 Windows 服务，这是服务可执行文件所在目录）
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 组合路径
        var fullPath = Path.Combine(baseDir, path);

        // 规范化路径
        return Path.GetFullPath(fullPath);
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
