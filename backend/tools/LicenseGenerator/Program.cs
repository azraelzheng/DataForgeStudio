using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DataForgeStudio.Shared.Utils;

namespace DataForgeStudio.LicenseGenerator;

/// <summary>
/// 许可证生成工具 - DataForgeStudio V4
/// 用于生成加密的许可证文件（.lic）
/// </summary>
class Program
{
    // 许可证类型枚举
    private static readonly string[] LicenseTypes = { "Trial", "Standard", "Professional", "Enterprise" };

    // 可用功能列表
    private static readonly string[] AvailableFeatures =
    {
        "报表设计", "报表查询", "图表展示", "Excel导出", "PDF导出",
        "数据源管理", "用户管理", "角色管理"
    };

    // 密钥配置（从外部文件读取）
    private static LicenseKeyConfig? _keyConfig;

    /// <summary>
    /// 加载密钥配置
    /// </summary>
    private static void LoadKeyConfig()
    {
        var configPath = "license-keys.json";

        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            _keyConfig = JsonSerializer.Deserialize<LicenseKeyConfig>(json);
            Console.WriteLine($"已加载密钥配置: {Path.GetFullPath(configPath)}");
        }
        else
        {
            // 使用 ProductionKeys 默认值
            _keyConfig = new LicenseKeyConfig
            {
                AesKey = "DataForgeStudioV4LicenseAES32!!!",
                AesIV = "LicenseIV16Bytes"
            };
            Console.WriteLine("使用内置默认密钥配置");
            Console.WriteLine($"提示: 可创建 {Path.GetFullPath(configPath)} 文件自定义密钥");
        }
    }

    // 私钥路径（优先从命令行参数读取，否则使用默认路径）
    private static string GetPrivateKeyPath(string[] args)
    {
        // 如果提供了命令行参数，使用参数指定的路径
        if (args.Length > 0 && File.Exists(args[0]))
        {
            return args[0];
        }

        // 1. 优先：exe 同目录下的 private_key.pem
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var sameDirPath = Path.Combine(exeDir, "private_key.pem");
        if (File.Exists(sameDirPath))
        {
            return sameDirPath;
        }

        // 2. 当前工作目录下的 keys 文件夹（与 API 服务共用）
        var keysPath = Path.Combine("keys", "private_key.pem");
        if (File.Exists(keysPath))
        {
            return keysPath;
        }

        // 3. 开发环境路径：相对于工具项目目录
        var devPath = Path.Combine("..", "..", "..", "..", "..", "src", "DataForgeStudio.Api", "keys", "private_key.pem");
        if (File.Exists(devPath))
        {
            return devPath;
        }

        // 返回默认路径（会在后续报错）
        return sameDirPath;
    }

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("DataForgeStudio V4 - 许可证生成工具");
        Console.WriteLine("====================================================");
        Console.WriteLine();

        // 加载密钥配置
        LoadKeyConfig();
        Console.WriteLine();

        try
        {
            // 1. 读取私钥
            var privateKeyPath = GetPrivateKeyPath(args);
            var privateKey = await ReadPrivateKeyAsync(privateKeyPath);
            Console.WriteLine($"已成功加载私钥文件: {Path.GetFullPath(privateKeyPath)}");
            Console.WriteLine();

            // 2. 收集许可证信息
            var licenseData = CollectLicenseInfo();

            // 3. 生成许可证
            var licenseFile = await GenerateLicenseAsync(licenseData, privateKey);

            Console.WriteLine();
            Console.WriteLine("====================================================");
            Console.WriteLine("许可证生成成功！");
            Console.WriteLine($"文件路径: {Path.GetFullPath(licenseFile)}");
            Console.WriteLine("====================================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"错误: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// 读取私钥文件
    /// </summary>
    private static async Task<string> ReadPrivateKeyAsync(string keyPath)
    {
        if (!File.Exists(keyPath))
        {
            Console.WriteLine($"错误: 私钥文件不存在");
            Console.WriteLine($"期望路径: {Path.GetFullPath(keyPath)}");
            Console.WriteLine();
            Console.WriteLine("提示: 请先运行 DataForgeStudio.Api 项目，系统会自动生成密钥对");
            Console.WriteLine("     或者通过命令行参数指定私钥路径: LicenseGenerator.exe <private_key_path>");
            throw new FileNotFoundException("私钥文件不存在", keyPath);
        }

        var privateKeyBytes = await File.ReadAllBytesAsync(keyPath);
        return Convert.ToBase64String(privateKeyBytes);
    }

    /// <summary>
    /// 收集许可证信息（交互式输入）
    /// </summary>
    private static LicenseInfo CollectLicenseInfo()
    {
        var info = new LicenseInfo();

        // 客户名称
        Console.Write("请输入客户名称: ");
        info.CustomerName = Console.ReadLine()?.Trim() ?? "UnknownCustomer";
        if (string.IsNullOrWhiteSpace(info.CustomerName))
        {
            info.CustomerName = "UnknownCustomer";
        }

        // 许可证类型
        Console.WriteLine();
        Console.WriteLine("许可证类型:");
        for (int i = 0; i < LicenseTypes.Length; i++)
        {
            Console.WriteLine($"  {i + 1}. {LicenseTypes[i]}");
        }
        Console.Write("请选择许可证类型 (1-4, 默认: 2): ");
        var typeInput = Console.ReadLine()?.Trim();
        int typeIndex = 2; // 默认 Standard
        if (int.TryParse(typeInput, out int typeChoice) && typeChoice >= 1 && typeChoice <= LicenseTypes.Length)
        {
            typeIndex = typeChoice;
        }
        info.LicenseType = LicenseTypes[typeIndex - 1];

        // 根据许可证类型设置默认值
        SetDefaultsForLicenseType(info, info.LicenseType);

        // 过期日期
        Console.WriteLine();
        Console.WriteLine($"过期日期 (默认: {info.ExpiryDate:yyyy-MM-dd})");
        Console.Write("请输入过期日期 (格式: yyyy-MM-dd, 直接回车使用默认值): ");
        var dateInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(dateInput) && DateTime.TryParse(dateInput, out var expiryDate))
        {
            info.ExpiryDate = expiryDate;
        }

        // 最大用户数
        Console.WriteLine();
        Console.Write($"最大用户数 (默认: {info.MaxUsers}): ");
        var usersInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(usersInput) && int.TryParse(usersInput, out var maxUsers) && maxUsers > 0)
        {
            info.MaxUsers = maxUsers;
        }

        // 最大报表数
        Console.Write($"最大报表数 (默认: {info.MaxReports}, 输入 0 表示无限制): ");
        var reportsInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(reportsInput) && int.TryParse(reportsInput, out var maxReports) && maxReports >= 0)
        {
            info.MaxReports = maxReports;
        }

        // 最大数据源数
        Console.Write($"最大数据源数 (默认: {info.MaxDataSources}, 输入 0 表示无限制): ");
        var datasourcesInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(datasourcesInput) && int.TryParse(datasourcesInput, out var maxDataSources) && maxDataSources >= 0)
        {
            info.MaxDataSources = maxDataSources;
        }

        // 功能选择
        Console.WriteLine();
        Console.WriteLine("可用功能:");
        for (int i = 0; i < AvailableFeatures.Length; i++)
        {
            Console.WriteLine($"  {i + 1}. {AvailableFeatures[i]}");
        }
        Console.WriteLine();
        Console.Write("请输入启用的功能编号 (多个用逗号分隔, 如: 1,2,3,5): ");
        var featuresInput = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(featuresInput))
        {
            // 默认启用所有功能
            info.Features = AvailableFeatures.ToList();
        }
        else
        {
            info.Features = new List<string>();
            var indices = featuresInput.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var indexStr in indices)
            {
                if (int.TryParse(indexStr, out int idx) && idx >= 1 && idx <= AvailableFeatures.Length)
                {
                    info.Features.Add(AvailableFeatures[idx - 1]);
                }
            }

            if (info.Features.Count == 0)
            {
                Console.WriteLine("警告: 未选择任何功能，将启用所有功能");
                info.Features = AvailableFeatures.ToList();
            }
        }

        // 机器码绑定
        Console.WriteLine();
        Console.WriteLine("机器码绑定 (可选)");
        Console.WriteLine("如需绑定到特定服务器，请输入机器码；留空则不绑定");
        Console.Write("机器码 (留空则不绑定): ");
        info.MachineCode = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.WriteLine();
        Console.WriteLine("====================================================");
        Console.WriteLine("许可证信息预览:");
        Console.WriteLine($"  客户名称: {info.CustomerName}");
        Console.WriteLine($"  许可证类型: {info.LicenseType}");
        Console.WriteLine($"  过期日期: {info.ExpiryDate:yyyy-MM-dd}");
        Console.WriteLine($"  最大用户数: {info.MaxUsers}");
        Console.WriteLine($"  最大报表数: {(info.MaxReports == 0 ? "无限制" : info.MaxReports.ToString())}");
        Console.WriteLine($"  最大数据源数: {(info.MaxDataSources == 0 ? "无限制" : info.MaxDataSources.ToString())}");
        Console.WriteLine($"  启用功能: {string.Join(", ", info.Features)}");
        Console.WriteLine($"  绑定机器码: {(string.IsNullOrEmpty(info.MachineCode) ? "不绑定" : info.MachineCode)}");
        Console.WriteLine("====================================================");
        Console.WriteLine();

        // 4. 确认生成
        Console.Write("确认生成许可证? (Y/N): ");
        var confirm = Console.ReadLine()?.Trim().ToUpper();
        if (confirm != "Y")
        {
            Console.WriteLine("已取消生成许可证");
            Environment.Exit(0);
        }

        return info;
    }

    /// <summary>
    /// 根据许可证类型设置默认值
    /// </summary>
    private static void SetDefaultsForLicenseType(LicenseInfo info, string licenseType)
    {
        var now = DateTime.UtcNow;

        switch (licenseType)
        {
            case "Trial":
                info.ExpiryDate = now.AddDays(30);
                info.MaxUsers = 5;
                info.MaxReports = 10;
                info.MaxDataSources = 2;
                info.Features = AvailableFeatures.Take(4).ToList(); // 基础功能
                break;

            case "Standard":
                info.ExpiryDate = now.AddYears(1);
                info.MaxUsers = 20;
                info.MaxReports = 50;
                info.MaxDataSources = 5;
                info.Features = AvailableFeatures.Take(6).ToList(); // 标准功能
                break;

            case "Professional":
                info.ExpiryDate = now.AddYears(1);
                info.MaxUsers = 100;
                info.MaxReports = 0; // 无限制
                info.MaxDataSources = 0; // 无限制
                info.Features = AvailableFeatures.ToList(); // 所有功能
                break;

            case "Enterprise":
                info.ExpiryDate = now.AddYears(2);
                info.MaxUsers = 0; // 无限制
                info.MaxReports = 0; // 无限制
                info.MaxDataSources = 0; // 无限制
                info.Features = AvailableFeatures.ToList(); // 所有功能
                break;
        }
    }

    /// <summary>
    /// 生成许可证文件
    /// </summary>
    private static async Task<string> GenerateLicenseAsync(LicenseInfo info, string privateKey)
    {
        Console.WriteLine("正在生成许可证...");

        // 1. 生成许可证唯一标识 (使用 "N" 格式: 无连字符的紧凑格式)
        var licenseId = Guid.NewGuid().ToString("N").ToUpper();

        // 2. 构建许可证数据 JSON
        var licenseData = new
        {
            LicenseId = licenseId,
            CustomerName = info.CustomerName,
            ExpiryDate = info.ExpiryDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MaxUsers = info.MaxUsers,
            MaxReports = info.MaxReports,
            MaxDataSources = info.MaxDataSources,
            Features = info.Features,
            MachineCode = info.MachineCode,
            IssuedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            LicenseType = info.LicenseType,
            Signature = "" // 稍后填充
        };

        // 3. 序列化为 JSON（不包含 Signature）
        var licenseJson = JsonSerializer.Serialize(licenseData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        // 重新构建不含 Signature 的 JSON
        var jsonForSigning = JsonSerializer.Serialize(new
        {
            LicenseId = licenseId,
            CustomerName = info.CustomerName,
            ExpiryDate = info.ExpiryDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MaxUsers = info.MaxUsers,
            MaxReports = info.MaxReports,
            MaxDataSources = info.MaxDataSources,
            Features = info.Features,
            MachineCode = info.MachineCode,
            IssuedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            LicenseType = info.LicenseType
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        // 4. 使用 RSA 私钥签名
        Console.WriteLine("正在签名许可证...");
        var signature = EncryptionHelper.RsaSignData(jsonForSigning, privateKey);

        // 5. 将签名添加到 JSON
        var finalLicenseData = new
        {
            LicenseId = licenseId,
            CustomerName = info.CustomerName,
            ExpiryDate = info.ExpiryDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MaxUsers = info.MaxUsers,
            MaxReports = info.MaxReports,
            MaxDataSources = info.MaxDataSources,
            Features = info.Features,
            MachineCode = info.MachineCode,
            IssuedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            LicenseType = info.LicenseType,
            Signature = signature
        };

        var finalJson = JsonSerializer.Serialize(finalLicenseData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        // 6. 使用 AES 加密（使用配置文件或内置密钥）
        Console.WriteLine("正在加密许可证...");
        var encryptedData = EncryptionHelper.AesEncrypt(finalJson, _keyConfig!.AesKey, _keyConfig.AesIV);

        // 7. 生成输出文件名
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var safeCustomerName = string.Join("_", info.CustomerName.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{safeCustomerName}_{info.LicenseType}_{timestamp}.lic";

        // 确保输出目录存在
        var outputDir = Path.Combine(".", "licenses");
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var outputPath = Path.Combine(outputDir, fileName);

        // 8. 写入文件
        await File.WriteAllTextAsync(outputPath, encryptedData);

        Console.WriteLine("许可证加密完成");

        return outputPath;
    }

    /// <summary>
    /// 许可证信息类
    /// </summary>
    private class LicenseInfo
    {
        public string CustomerName { get; set; } = string.Empty;
        public string LicenseType { get; set; } = "Standard";
        public DateTime ExpiryDate { get; set; }
        public int MaxUsers { get; set; }
        public int MaxReports { get; set; }
        public int MaxDataSources { get; set; }
        public List<string> Features { get; set; } = new();
        public string MachineCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// 密钥配置类
    /// </summary>
    private class LicenseKeyConfig
    {
        public string AesKey { get; set; } = string.Empty;
        public string AesIV { get; set; } = string.Empty;
    }
}
