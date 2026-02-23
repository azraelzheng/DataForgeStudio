using Microsoft.Extensions.Configuration;

namespace DataForgeStudio.Api.Services;

/// <summary>
/// 数据库连接字符串提供者接口
/// </summary>
public interface IDbConnectionStringProvider
{
    string DefaultConnection { get; }
    string MasterConnection { get; }
}

/// <summary>
/// 数据库连接字符串提供者实现
/// </summary>
public class DbConnectionStringProvider : IDbConnectionStringProvider
{
    public string DefaultConnection { get; }
    public string MasterConnection { get; }

    public DbConnectionStringProvider(string defaultConnection, string masterConnection)
    {
        DefaultConnection = defaultConnection;
        MasterConnection = masterConnection;
    }
}

/// <summary>
/// 连接字符串加密/解密辅助类
/// </summary>
public static class ConnectionStringHelper
{
    /// <summary>
    /// 解密连接字符串（如果需要）
    /// 支持两种格式：
    /// 1. 标准格式：Password=xxx（明文密码，向后兼容）
    /// 2. 加密格式：EncryptedPassword=xxx（加密密码）
    /// </summary>
    public static string DecryptIfNeeded(string? connectionString, IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return connectionString ?? string.Empty;
        }

        // 检查是否包含加密密码标记
        if (!connectionString.Contains("EncryptedPassword=", StringComparison.OrdinalIgnoreCase))
        {
            // 没有加密标记，直接返回（向后兼容明文密码）
            return connectionString;
        }

        try
        {
            // 获取加密密钥
            var aesKey = Environment.GetEnvironmentVariable("DFS_ENCRYPTION_AESKEY")
                ?? configuration["Security:Encryption:AesKey"]
                ?? throw new InvalidOperationException("加密密钥未配置。请设置环境变量 DFS_ENCRYPTION_AESKEY 或 Security:Encryption:AesKey");

            var aesIv = Environment.GetEnvironmentVariable("DFS_ENCRYPTION_AESIV")
                ?? configuration["Security:Encryption:AesIV"]
                ?? throw new InvalidOperationException("加密IV未配置。请设置环境变量 DFS_ENCRYPTION_AESIV 或 Security:Encryption:AesIV");

            // 提取加密密码
            var encryptedPassword = ExtractValue(connectionString, "EncryptedPassword");
            if (string.IsNullOrEmpty(encryptedPassword))
            {
                Console.WriteLine("⚠️ 连接字符串中 EncryptedPassword 为空");
                return connectionString;
            }

            // 解密密码
            var decryptedPassword = Shared.Utils.EncryptionHelper.AesDecrypt(encryptedPassword, aesKey, aesIv);

            // 替换 EncryptedPassword=xxx 为 Password=decrypted
            var result = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"EncryptedPassword=[^;]+",
                $"Password={decryptedPassword}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            Console.WriteLine("✅ 数据库连接字符串密码已解密");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 解密连接字符串失败: {ex.Message}");
            throw new InvalidOperationException($"解密数据库连接字符串失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从连接字符串中提取指定参数的值
    /// </summary>
    private static string? ExtractValue(string connectionString, string parameterName)
    {
        var pattern = $@"{parameterName}\s*=\s*([^;]+)";
        var match = System.Text.RegularExpressions.Regex.Match(connectionString, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
