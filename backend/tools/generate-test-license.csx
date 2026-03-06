// 简单测试：生成并解密许可证
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// AES 密钥配置
var aesKey = "DataForgeStudioV4LicenseAES32!!!";
var aesIv = "LicenseIV16Bytes";

Console.WriteLine("=== 许可证加密/解密测试 ===\n");
Console.WriteLine($"AES Key: {aesKey}");
Console.WriteLine($"AES IV: {aesIv}");

// 创建测试许可证数据
var testData = new
{
    LicenseId = Guid.NewGuid().ToString(),
    CustomerName = "测试客户",
    ExpiryDate = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
    MaxUsers = 10,
    MaxReports = 50,
    MaxDataSources = 5,
    MaxDashboards = 10,
    Features = new[] { "报表设计", "报表查询" },
    MachineCode = "",
    IssuedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
    LicenseType = "Standard",
    Signature = "test-signature"
};

var json = JsonSerializer.Serialize(testData, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});

Console.WriteLine($"\n原始 JSON (长度 {json.Length}):");
Console.WriteLine(json.Substring(0, Math.Min(200, json.Length)) + "...\n");

// 加密
Console.WriteLine("正在加密...");
var encrypted = Encrypt(json, aesKey, aesIv);
Console.WriteLine($"加密后 (长度 {encrypted.Length}):");
Console.WriteLine(encrypted.Substring(0, Math.Min(50, encrypted.Length)) + "...\n");

// 解密
Console.WriteLine("正在解密...");
try
{
    var decrypted = Decrypt(encrypted, aesKey, aesIv);
    Console.WriteLine("解密成功!");
    Console.WriteLine($"解密后 JSON (长度 {decrypted.Length}):");
    Console.WriteLine(decrypted.Substring(0, Math.Min(200, decrypted.Length)) + "...\n");

    // 验证
    if (json == decrypted)
    {
        Console.WriteLine("✅ 加密/解密测试通过！原始数据与解密数据一致。");
    }
    else
    {
        Console.WriteLine("❌ 警告：原始数据与解密数据不一致！");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 解密失败: {ex.Message}");
}

// 测试文件中的许可证
Console.WriteLine("\n\n=== 测试现有许可证文件 ===\n");
var licensePath = @"H:\DataForge\backend\tools\LicenseGenerator\bin\Release\net8.0\win-x64\publish\licenses\test_Standard_20260304015237.lic";
if (File.Exists(licensePath))
{
    var fileContent = File.ReadAllText(licensePath).Trim();
    Console.WriteLine($"许可证文件: {licensePath}");
    Console.WriteLine($"文件内容长度: {fileContent.Length}");

    try
    {
        var decryptedLicense = Decrypt(fileContent, aesKey, aesIv);
        Console.WriteLine("✅ 许可证文件解密成功!");
        Console.WriteLine($"解密后内容:\n{decryptedLicense}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 许可证文件解密失败: {ex.Message}");
        Console.WriteLine("\n可能原因:");
        Console.WriteLine("1. 许可证文件是用不同的 AES 密钥加密的");
        Console.WriteLine("2. 许可证文件已损坏");
        Console.WriteLine("3. 许可证文件格式不正确");
    }
}
else
{
    Console.WriteLine($"许可证文件不存在: {licensePath}");
}

// 加密函数（与 EncryptionHelper.AesEncrypt 一致）
static string Encrypt(string plainText, string key, string iv)
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
        using var swEncrypt = new StreamWriter(csEncrypt);
        swEncrypt.Write(plainText);
    }
    return Convert.ToBase64String(msEncrypt.ToArray());
}

// 解密函数（与 EncryptionHelper.AesDecrypt 一致）
static string Decrypt(string cipherText, string key, string iv)
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
