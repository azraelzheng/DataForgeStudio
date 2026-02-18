using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace DataForgeStudio.Shared.Utils;

/// <summary>
/// 加密辅助类
/// </summary>
public static class EncryptionHelper
{
    #region BCrypt 密码哈希

    /// <summary>
    /// 哈希密码
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <returns>哈希后的密码</returns>
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <param name="hash">哈希密码</param>
    /// <returns>是否匹配</returns>
    public static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    #endregion

    #region AES 加密/解密

    /// <summary>
    /// AES 加密
    /// </summary>
    /// <param name="plainText">明文</param>
    /// <param name="key">密钥</param>
    /// <param name="iv">IV向量</param>
    /// <returns>密文（Base64）</returns>
    public static string AesEncrypt(string plainText, string key, string iv)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        aes.IV = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }
        }
        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    /// <summary>
    /// AES 解密
    /// </summary>
    /// <param name="cipherText">密文（Base64）</param>
    /// <param name="key">密钥</param>
    /// <param name="iv">IV向量</param>
    /// <returns>明文</returns>
    public static string AesDecrypt(string cipherText, string key, string iv)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        aes.IV = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }

    /// <summary>
    /// 生成 AES 密钥
    /// </summary>
    /// <param name="keySize">密钥大小（128/192/256）</param>
    /// <returns>密钥</returns>
    public static string GenerateAesKey(int keySize = 256)
    {
        using var aes = Aes.Create();
        aes.KeySize = keySize;
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }

    /// <summary>
    /// 生成 AES IV 向量
    /// </summary>
    /// <returns>IV向量</returns>
    public static string GenerateAesIV()
    {
        using var aes = Aes.Create();
        aes.GenerateIV();
        return Convert.ToBase64String(aes.IV);
    }

    #endregion

    #region RSA 加密/解密/签名/验证

    /// <summary>
    /// 生成 RSA 密钥对
    /// </summary>
    /// <param name="keySize">密钥大小</param>
    /// <returns>(公钥, 私钥)</returns>
    public static (string publicKey, string privateKey) GenerateRsaKeyPair(int keySize = 2048)
    {
        using var rsa = RSA.Create(keySize);
        return (
            publicKey: Convert.ToBase64String(rsa.ExportRSAPublicKey()),
            privateKey: Convert.ToBase64String(rsa.ExportRSAPrivateKey())
        );
    }

    /// <summary>
    /// RSA 加密
    /// </summary>
    /// <param name="plainText">明文</param>
    /// <param name="publicKey">公钥（Base64）</param>
    /// <returns>密文（Base64）</returns>
    public static string RsaEncrypt(string plainText, string publicKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
        var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// RSA 解密
    /// </summary>
    /// <param name="cipherText">密文（Base64）</param>
    /// <param name="privateKey">私钥（Base64）</param>
    /// <returns>明文</returns>
    public static string RsaDecrypt(string cipherText, string privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        var decrypted = rsa.Decrypt(Convert.FromBase64String(cipherText), RSAEncryptionPadding.OaepSHA256);
        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    /// RSA 签名
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="privateKey">私钥（Base64）</param>
    /// <returns>签名（Base64）</returns>
    public static string RsaSignData(string data, string privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    /// <summary>
    /// RSA 验证签名
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="signature">签名（Base64）</param>
    /// <param name="publicKey">公钥（Base64）</param>
    /// <returns>是否验证通过</returns>
    public static bool RsaVerifyData(string data, string signature, string publicKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
        return rsa.VerifyData(Encoding.UTF8.GetBytes(data), Convert.FromBase64String(signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    #endregion

    #region 哈希

    /// <summary>
    /// 计算 SHA256 哈希
    /// </summary>
    public static string ComputeSha256Hash(string input) => ComputeHash(SHA256.Create(), input);

    /// <summary>
    /// 计算 SHA512 哈希
    /// </summary>
    public static string ComputeSha512Hash(string input) => ComputeHash(SHA512.Create(), input);

    /// <summary>
    /// 计算 MD5 哈希
    /// </summary>
    public static string ComputeMd5Hash(string input) => ComputeHash(MD5.Create(), input);

    private static string ComputeHash(HashAlgorithm algorithm, string input)
    {
        using var hashAlg = algorithm;
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = hashAlg.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLower();
    }

    #endregion

    #region 便捷方法（已弃用）

    /// <summary>
    /// AES 加密（使用默认密钥和IV）- 已弃用
    /// </summary>
    [Obsolete("请使用 AesEncrypt(plainText, key, iv) 并从配置读取密钥。")]
    public static string EncryptAES(string plainText)
        => throw new InvalidOperationException("EncryptAES 方法已弃用。请使用 AesEncrypt(plainText, key, iv) 并从配置读取密钥。");

    /// <summary>
    /// AES 解密（使用默认密钥和IV）- 已弃用
    /// </summary>
    [Obsolete("请使用 AesDecrypt(cipherText, key, iv) 并从配置读取密钥。")]
    public static string DecryptAES(string cipherText)
        => throw new InvalidOperationException("DecryptAES 方法已弃用。请使用 AesDecrypt(cipherText, key, iv) 并从配置读取密钥。");

    /// <summary>
    /// RSA 加密（使用默认公钥）- 已弃用，请使用 RsaEncrypt
    /// </summary>
    [Obsolete("请使用 RsaEncrypt(plainText, publicKey) 并传入公钥。")]
    public static string EncryptRSA(string plainText)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));

    #endregion

    #region 获取机器码

    /// <summary>
    /// 获取机器码（用于许可证绑定）
    /// </summary>
    public static string GetMachineCode()
    {
        try
        {
            var cpuId = GetCpuId();
            var diskId = GetDiskSerialNumber();
            return ComputeSha256Hash($"{cpuId}-{diskId}").Substring(0, 32);
        }
        catch
        {
            return "UNKNOWN_MACHINE";
        }
    }

    private static string GetCpuId()
    {
        try
        {
            var cpuInfo = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            return ComputeMd5Hash(cpuInfo).Substring(0, 8);
        }
        catch
        {
            return "CPU_UNKNOWN";
        }
    }

    private static string GetDiskSerialNumber()
    {
        try
        {
            var drive = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var driveInfo = new DriveInfo(Path.GetPathRoot(drive) ?? "C:");
            return ComputeMd5Hash(driveInfo.VolumeLabel).Substring(0, 8);
        }
        catch
        {
            return "DISK_UNKNOWN";
        }
    }

    #endregion
}
