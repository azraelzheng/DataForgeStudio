// 快速测试许可证解密
// 运行: dotnet script test-decrypt.csx

#r "nuget: System.Security.Cryptography.ProtectedData, 7.0.0"

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// 读取许可证文件
var licensePath = Args.Count > 0 ? Args[0] : @"H:\DataForge\backend\tools\LicenseGenerator\bin\Release\net8.0\win-x64\publish\licenses\test_Standard_20260304015237.lic";

Console.WriteLine($"测试文件: {licensePath}");
Console.WriteLine($"文件存在: {File.Exists(licensePath)}");

if (!File.Exists(licensePath))
{
    Console.WriteLine("文件不存在!");
    return;
}

var licenseContent = File.ReadAllText(licensePath).Trim();
Console.WriteLine($"内容长度: {licenseContent.Length}");
Console.WriteLine($"前50字符: {licenseContent.Substring(0, Math.Min(50, licenseContent.Length))}");

// AES 密钥配置（与 ProductionKeys 一致）
var aesKey = "DataForgeStudioV4LicenseAES32!!!";
var aesIv = "LicenseIV16Bytes";

Console.WriteLine($"\nAES Key: {aesKey} (长度: {aesKey.Length})");
Console.WriteLine($"AES IV: {aesIv} (长度: {aesIv.Length})");

try
{
    // 模拟 EncryptionHelper.AesDecrypt
    var keyBytes = Encoding.UTF8.GetBytes(aesKey.PadRight(32).Substring(0, 32));
    var ivBytes = Encoding.UTF8.GetBytes(aesIv.PadRight(16).Substring(0, 16));

    Console.WriteLine($"\n实际 Key 字节: {BitConverter.ToString(keyBytes).Replace("-", "")}");
    Console.WriteLine($"实际 IV 字节: {BitConverter.ToString(ivBytes).Replace("-", "")}");

    var cipherBytes = Convert.FromBase64String(licenseContent);
    Console.WriteLine($"\n密文字节长度: {cipherBytes.Length}");

    using var aes = Aes.Create();
    aes.Key = keyBytes;
    aes.IV = ivBytes;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    using var decryptor = aes.CreateDecryptor();
    using var msDecrypt = new MemoryStream(cipherBytes);
    using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
    using var srDecrypt = new StreamReader(csDecrypt);

    var plainText = srDecrypt.ReadToEnd();

    Console.WriteLine("\n=== 解密成功! ===");
    Console.WriteLine("解密后内容:");
    Console.WriteLine(plainText);
}
catch (FormatException fe)
{
    Console.WriteLine($"\nBase64 格式错误: {fe.Message}");
}
catch (CryptographicException ce)
{
    Console.WriteLine($"\n解密失败 (密钥不匹配或数据损坏): {ce.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"\n解密失败: {ex.Message}");
    Console.WriteLine($"异常类型: {ex.GetType().Name}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"内部异常: {ex.InnerException.Message}");
    }
}
