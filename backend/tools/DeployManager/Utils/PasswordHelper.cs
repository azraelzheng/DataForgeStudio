namespace DeployManager.Utils;

/// <summary>
/// 密码加密辅助类
/// </summary>
public static class PasswordHelper
{
    // 注意：这里需要引用 DataForgeStudio.Shared 项目，或者复制 EncryptionHelper 的 AES 方法
    // 由于 DeployManager 是独立项目，这里提供简化实现

    private static readonly string AesKey = "DeployManager2024Key32Bytes!!"; // 32 bytes
    private static readonly string AesIV = "DeployMgr16Bytes!"; // 16 bytes

    /// <summary>
    /// 加密密码
    /// </summary>
    public static string EncryptPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            return string.Empty;

        return AesEncrypt(plainPassword);
    }

    /// <summary>
    /// 解密密码
    /// </summary>
    public static string DecryptPassword(string encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return string.Empty;

        return AesDecrypt(encryptedPassword);
    }

    private static string AesEncrypt(string plainText)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Text.Encoding.UTF8.GetBytes(AesKey);
        aes.IV = System.Text.Encoding.UTF8.GetBytes(AesIV);
        aes.Mode = System.Security.Cryptography.CipherMode.CBC;
        aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new System.IO.MemoryStream();
        using (var csEncrypt = new System.Security.Cryptography.CryptoStream(msEncrypt, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
        using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }
        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    private static string AesDecrypt(string cipherText)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Text.Encoding.UTF8.GetBytes(AesKey);
        aes.IV = System.Text.Encoding.UTF8.GetBytes(AesIV);
        aes.Mode = System.Security.Cryptography.CipherMode.CBC;
        aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new System.IO.MemoryStream(Convert.FromBase64String(cipherText));
        using var csDecrypt = new System.Security.Cryptography.CryptoStream(msDecrypt, decryptor, System.Security.Cryptography.CryptoStreamMode.Read);
        using var srDecrypt = new System.IO.StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }
}
