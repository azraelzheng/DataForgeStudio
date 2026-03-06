// 快速生成测试许可证
// 运行: dotnet tool run dotnet-script quick-gen-license.csx

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// AES 密钥配置（与 ProductionKeys 一致）
var aesKey = "DataForgeStudioV4LicenseAES32!!!";
var aesIv = "LicenseIV16Bytes";

// RSA 私钥路径
var privateKeyPath = @"H:\DataForge\backend\src\DataForgeStudio.Api\keys\private_key.pem";

Console.WriteLine("=== 快速生成测试许可证 ===\n");

// 读取私钥
if (!File.Exists(privateKeyPath))
{
    Console.WriteLine($"错误: 私钥文件不存在: {privateKeyPath}");
    return;
}

var privateKeyBytes = File.ReadAllBytes(privateKeyPath);
Console.WriteLine($"私钥已加载 ({privateKeyBytes.Length} 字节)");

// 创建许可证数据
var licenseId = Guid.NewGuid().ToString();
var customerName = "测试客户";
var expiryDate = DateTime.UtcNow.AddYears(1);
var machineCode = ""; // 不绑定机器码

var licenseData = new
{
    LicenseId = licenseId,
    CustomerName = customerName,
    ExpiryDate = expiryDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
    MaxUsers = 10,
    MaxReports = 50,
    MaxDataSources = 5,
    MaxDashboards = 10,
    Features = new[] { "报表设计", "报表查询", "数据源管理", "大屏设计" },
    MachineCode = machineCode,
    IssuedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
    LicenseType = "Standard"
};

// 序列化（用于签名）
var jsonForSigning = JsonSerializer.Serialize(licenseData, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
});

Console.WriteLine($"许可证数据已准备 (ID: {licenseId})");

// 签名
string signature;
using (var rsa = RSA.Create())
{
    rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
    var signatureBytes = rsa.SignData(
        Encoding.UTF8.GetBytes(jsonForSigning),
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1
    );
    signature = Convert.ToBase64String(signatureBytes);
}

Console.WriteLine("签名已完成");

// 完整许可证（包含签名）
var fullLicense = new
{
    licenseData.LicenseId,
    licenseData.CustomerName,
    licenseData.ExpiryDate,
    licenseData.MaxUsers,
    licenseData.MaxReports,
    licenseData.MaxDataSources,
    licenseData.MaxDashboards,
    licenseData.Features,
    licenseData.MachineCode,
    licenseData.IssuedDate,
    licenseData.LicenseType,
    Signature = signature
};

var fullJson = JsonSerializer.Serialize(fullLicense, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
});

// 加密
var encrypted = Encrypt(fullJson, aesKey, aesIv);

// 保存
var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
var fileName = $"quick_test_Standard_{timestamp}.lic";
var outputDir = @"H:\DataForge\backend\tools\LicenseGenerator\licenses";
if (!Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
}
var outputPath = Path.Combine(outputDir, fileName);

File.WriteAllText(outputPath, encrypted);

Console.WriteLine($"\n✅ 许可证已生成: {outputPath}");
Console.WriteLine($"\n客户名称: {customerName}");
Console.WriteLine($"许可证类型: Standard");
Console.WriteLine($"过期日期: {expiryDate:yyyy-MM-dd}");
Console.WriteLine($"最大用户: 10");
Console.WriteLine($"最大报表: 50");
Console.WriteLine($"最大数据源: 5");
Console.WriteLine($"最大大屏: 10");

// 验证解密
Console.WriteLine("\n=== 验证解密 ===");
try
{
    var fileContent = File.ReadAllText(outputPath).Trim();
    var decrypted = Decrypt(fileContent, aesKey, aesIv);
    Console.WriteLine("✅ 解密验证通过！");

    var parsed = JsonSerializer.Deserialize<JsonDocument>(decrypted);
    Console.WriteLine($"许可证ID: {parsed?.RootElement.GetProperty("licenseId").GetString()}");
    Console.WriteLine($"客户名称: {parsed?.RootElement.GetProperty("customerName").GetString()}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 解密验证失败: {ex.Message}");
}

// 加密函数
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

// 解密函数
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
