using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Shared.Utils;
using Microsoft.Win32;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 试用期许可证追踪器接口
/// </summary>
public interface ITrialLicenseTracker
{
    /// <summary>
    /// 检查试用期状态
    /// </summary>
    /// <returns>试用期状态</returns>
    TrialLicenseStatus CheckTrialStatus();

    /// <summary>
    /// 记录首次运行时间
    /// </summary>
    void RecordFirstRun();
}

/// <summary>
/// 试用期许可证状态
/// </summary>
public class TrialLicenseStatus
{
    /// <summary>
    /// 是否为首次运行
    /// </summary>
    public bool IsFirstRun { get; set; }

    /// <summary>
    /// 试用期是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 首次运行时间
    /// </summary>
    public DateTime? FirstRunTime { get; set; }

    /// <summary>
    /// 到期时间
    /// </summary>
    public DateTime? ExpiryTime { get; set; }

    /// <summary>
    /// 剩余天数
    /// </summary>
    public int DaysRemaining { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 试用期许可证追踪器 - 使用 DPAPI 多位置存储防止重置
/// </summary>
public class TrialLicenseTracker : ITrialLicenseTracker
{
    private readonly ILogger<TrialLicenseTracker> _logger;

    /// <summary>
    /// 试用期天数
    /// </summary>
    private const int TRIAL_DAYS = 15;

    /// <summary>
    /// 数据版本
    /// </summary>
    private const int DATA_VERSION = 1;

    /// <summary>
    /// 注册表路径
    /// </summary>
    private const string REGISTRY_PATH = @"SOFTWARE\Microsoft\CryptoAPI\v2\machine";

    /// <summary>
    /// 注册表值名
    /// </summary>
    private const string REGISTRY_VALUE_NAME = "CacheData";

    /// <summary>
    /// ProgramData 目录下的文件路径模板
    /// </summary>
    private const string PROGRAM_DATA_PATH_TEMPLATE = @"Microsoft\Crypto\RSA\MachineKeys\{0}.dat";

    /// <summary>
    /// 应用目录下的缓存文件名
    /// </summary>
    private const string APP_CACHE_FILENAME = ".runtime_cache";

    /// <summary>
    /// 机器码（缓存）
    /// </summary>
    private readonly string _machineCode;

    /// <summary>
    /// 稳定 GUID（基于机器码生成）
    /// </summary>
    private readonly string _stableGuid;

    public TrialLicenseTracker(ILogger<TrialLicenseTracker> logger)
    {
        _logger = logger;
        _machineCode = EncryptionHelper.GetMachineCode();
        _stableGuid = GetStableGuid();
    }

    /// <summary>
    /// 基于机器码生成稳定的 GUID
    /// </summary>
    private string GetStableGuid()
    {
        // 使用 SHA256 生成确定性 GUID
        var hash = EncryptionHelper.ComputeSha256Hash(_machineCode + "TrialLicenseTracker");
        return new Guid(hash.Substring(0, 32).ToUpper()).ToString("D");
    }

    /// <summary>
    /// 获取注册表完整路径
    /// </summary>
    private string GetRegistryPath()
    {
        return REGISTRY_PATH;
    }

    /// <summary>
    /// 获取 ProgramData 文件完整路径
    /// </summary>
    private string GetProgramDataPath()
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var filename = string.Format(PROGRAM_DATA_PATH_TEMPLATE, _stableGuid);
        return Path.Combine(programData, filename);
    }

    /// <summary>
    /// 获取应用目录缓存文件完整路径
    /// </summary>
    private string GetAppCachePath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var configDir = Path.Combine(appDir, "config");
        return Path.Combine(configDir, APP_CACHE_FILENAME);
    }

    /// <summary>
    /// 检查试用期状态
    /// </summary>
    public TrialLicenseStatus CheckTrialStatus()
    {
        try
        {
            var records = new List<(TrialData? data, string location)>();

            // 从三个位置读取数据
            var (registryData, registryError) = ReadFromRegistry();
            if (registryData != null)
            {
                records.Add((registryData, "Registry"));
            }
            else if (!string.IsNullOrEmpty(registryError))
            {
                _logger.LogDebug("注册表读取失败: {Error}", registryError);
            }

            var (programData, programDataError) = ReadFromFile(GetProgramDataPath());
            if (programData != null)
            {
                records.Add((programData, "ProgramData"));
            }
            else if (!string.IsNullOrEmpty(programDataError))
            {
                _logger.LogDebug("ProgramData 读取失败: {Error}", programDataError);
            }

            var (appCacheData, appCacheError) = ReadFromFile(GetAppCachePath());
            if (appCacheData != null)
            {
                records.Add((appCacheData, "AppCache"));
            }
            else if (!string.IsNullOrEmpty(appCacheError))
            {
                _logger.LogDebug("AppCache 读取失败: {Error}", appCacheError);
            }

            // 如果没有任何记录，说明是首次运行
            if (records.Count == 0)
            {
                _logger.LogInformation("未找到试用期记录，判定为首次运行");
                return new TrialLicenseStatus
                {
                    IsFirstRun = true,
                    IsValid = true,
                    DaysRemaining = TRIAL_DAYS
                };
            }

            // 验证所有记录并找出最早的首次运行时间
            DateTime? earliestFirstRun = null;
            var validRecordCount = 0;

            foreach (var (data, location) in records)
            {
                if (data == null) continue;

                var (isValid, validationError) = ValidateRecord(data, location);
                if (isValid)
                {
                    validRecordCount++;
                    if (data.FirstRun.HasValue && (!earliestFirstRun.HasValue || data.FirstRun.Value < earliestFirstRun.Value))
                    {
                        earliestFirstRun = data.FirstRun.Value;
                    }
                }
                else
                {
                    _logger.LogWarning("位置 {Location} 的记录验证失败: {Error}", location, validationError);
                }
            }

            // 如果没有有效记录，可能是数据被篡改
            if (validRecordCount == 0)
            {
                _logger.LogWarning("所有试用期记录均无效，可能存在篡改行为");
                return new TrialLicenseStatus
                {
                    IsFirstRun = false,
                    IsValid = false,
                    ErrorMessage = "试用期数据异常，请联系技术支持"
                };
            }

            // 使用最早的首次运行时间计算到期时间
            if (!earliestFirstRun.HasValue)
            {
                _logger.LogWarning("无法确定首次运行时间");
                return new TrialLicenseStatus
                {
                    IsFirstRun = false,
                    IsValid = false,
                    ErrorMessage = "试用期数据不完整"
                };
            }

            var expiryTime = earliestFirstRun.Value.AddDays(TRIAL_DAYS);
            var now = DateTime.UtcNow;
            var isTrialValid = now < expiryTime;
            var daysRemaining = isTrialValid ? (int)Math.Ceiling((expiryTime - now).TotalDays) : 0;

            _logger.LogInformation("试用期状态: 首次运行={FirstRun}, 到期={Expiry}, 剩余={Days}天",
                earliestFirstRun.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                expiryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                daysRemaining);

            return new TrialLicenseStatus
            {
                IsFirstRun = false,
                IsValid = isTrialValid,
                FirstRunTime = earliestFirstRun,
                ExpiryTime = expiryTime,
                DaysRemaining = daysRemaining,
                ErrorMessage = isTrialValid ? null : $"试用期已过期（过期日期: {expiryTime:yyyy-MM-dd}）"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查试用期状态时发生错误");
            return new TrialLicenseStatus
            {
                IsFirstRun = false,
                IsValid = false,
                ErrorMessage = $"检查试用期状态失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 记录首次运行时间
    /// </summary>
    public void RecordFirstRun()
    {
        try
        {
            var now = DateTime.UtcNow;
            var trialData = TrialData.Create(_machineCode, now, now.AddDays(TRIAL_DAYS), DATA_VERSION);

            // 写入所有位置
            WriteToRegistry(trialData);
            WriteToFile(GetProgramDataPath(), trialData);
            WriteToFile(GetAppCachePath(), trialData);

            _logger.LogInformation("试用期首次运行时间已记录: {FirstRun}, 到期时间: {Expiry}",
                now.ToString("yyyy-MM-dd HH:mm:ss"),
                trialData.Expiry?.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录首次运行时间失败");
        }
    }

    /// <summary>
    /// 验证单条记录
    /// </summary>
    private (bool isValid, string? error) ValidateRecord(TrialData data, string location)
    {
        // 验证版本
        if (data.Version != DATA_VERSION)
        {
            return (false, $"版本不匹配: 期望 {DATA_VERSION}, 实际 {data.Version}");
        }

        // 验证机器码
        if (!data.ValidateMachineCode(_machineCode))
        {
            return (false, "机器码不匹配");
        }

        // 验证校验和
        if (!data.ValidateChecksum())
        {
            return (false, "校验和不匹配，数据可能被篡改");
        }

        // 验证时间合理性
        if (data.FirstRun.HasValue)
        {
            if (data.FirstRun.Value > DateTime.UtcNow.AddMinutes(5))
            {
                return (false, "首次运行时间在未来");
            }

            if (data.FirstRun.Value < new DateTime(2024, 1, 1))
            {
                return (false, "首次运行时间过早，数据异常");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// 从注册表读取数据
    /// </summary>
    private (TrialData? data, string? error) ReadFromRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(GetRegistryPath());
            if (key == null)
            {
                return (null, "注册表键不存在");
            }

            var value = key.GetValue(REGISTRY_VALUE_NAME) as byte[];
            if (value == null || value.Length == 0)
            {
                return (null, "注册表值不存在");
            }

            return DecryptData(value);
        }
        catch (UnauthorizedAccessException ex)
        {
            return (null, $"注册表访问被拒绝: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"读取注册表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 写入注册表
    /// </summary>
    private void WriteToRegistry(TrialData data)
    {
        try
        {
            var encryptedData = EncryptData(data);
            if (encryptedData == null)
            {
                _logger.LogWarning("加密数据失败，跳过注册表写入");
                return;
            }

            // 确保注册表路径存在
            var path = GetRegistryPath();
            var currentKey = Registry.LocalMachine;

            foreach (var segment in path.Split('\\'))
            {
                var subKey = currentKey.OpenSubKey(segment, true);
                if (subKey == null)
                {
                    subKey = currentKey.CreateSubKey(segment);
                }
                currentKey.Close();
                currentKey = subKey;
            }

            currentKey.SetValue(REGISTRY_VALUE_NAME, encryptedData, RegistryValueKind.Binary);
            currentKey.Close();

            _logger.LogDebug("试用期数据已写入注册表");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "无权限写入注册表，可能需要管理员权限");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入注册表失败");
        }
    }

    /// <summary>
    /// 从文件读取数据
    /// </summary>
    private (TrialData? data, string? error) ReadFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return (null, "文件不存在");
            }

            var encryptedData = File.ReadAllBytes(filePath);
            if (encryptedData.Length == 0)
            {
                return (null, "文件为空");
            }

            return DecryptData(encryptedData);
        }
        catch (UnauthorizedAccessException ex)
        {
            return (null, $"文件访问被拒绝: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"读取文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    private void WriteToFile(string filePath, TrialData data)
    {
        try
        {
            var encryptedData = EncryptData(data);
            if (encryptedData == null)
            {
                _logger.LogWarning("加密数据失败，跳过文件写入: {Path}", filePath);
                return;
            }

            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(filePath, encryptedData);

            // 设置文件属性为隐藏和系统文件
            var fileInfo = new FileInfo(filePath);
            fileInfo.Attributes |= FileAttributes.Hidden | FileAttributes.System;

            _logger.LogDebug("试用期数据已写入文件: {Path}", filePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "无权限写入文件: {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入文件失败: {Path}", filePath);
        }
    }

    /// <summary>
    /// 使用 DPAPI 加密数据
    /// </summary>
    private byte[]? EncryptData(TrialData data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var plainBytes = Encoding.UTF8.GetBytes(json);

            // 使用 DPAPI 的 CurrentUser 范围加密（与用户账户绑定）
            // 注意：这在不同用户账户下会有不同的加密结果
            var encryptedData = ProtectedData.Protect(plainBytes, null, DataProtectionScope.LocalMachine);

            return encryptedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DPAPI 加密失败");
            return null;
        }
    }

    /// <summary>
    /// 使用 DPAPI 解密数据
    /// </summary>
    private (TrialData? data, string? error) DecryptData(byte[] encryptedData)
    {
        try
        {
            var plainBytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.LocalMachine);
            var json = Encoding.UTF8.GetString(plainBytes);

            var data = JsonSerializer.Deserialize<TrialData>(json);
            if (data == null)
            {
                return (null, "反序列化失败");
            }

            return (data, null);
        }
        catch (CryptographicException ex)
        {
            return (null, $"DPAPI 解密失败（可能是不同机器或密钥损坏）: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"解密失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 写入所有位置
    /// </summary>
    private void WriteToAllLocations(TrialData data)
    {
        WriteToRegistry(data);
        WriteToFile(GetProgramDataPath(), data);
        WriteToFile(GetAppCachePath(), data);
    }

    #region 内部数据模型

    /// <summary>
    /// 试用期数据模型（内部使用）
    /// </summary>
    internal class TrialData
    {
        /// <summary>
        /// 数据版本
        /// </summary>
        public int V { get; set; }

        /// <summary>
        /// 机器码
        /// </summary>
        public string? Mc { get; set; }

        /// <summary>
        /// 首次运行时间（Unix 时间戳毫秒）
        /// </summary>
        public long? Fr { get; set; }

        /// <summary>
        /// 到期时间（Unix 时间戳毫秒）
        /// </summary>
        public long? Te { get; set; }

        /// <summary>
        /// 校验和
        /// </summary>
        public string? Cs { get; set; }

        /// <summary>
        /// 随机数（增加熵值）
        /// </summary>
        public string? Nk { get; set; }

        // JSON 序列化需要的属性别名
        public int Version { get => V; set => V = value; }
        public string? MachineCode { get => Mc; set => Mc = value; }
        public DateTime? FirstRun
        {
            get => Fr.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(Fr.Value).UtcDateTime : null;
            set => Fr = value.HasValue ? new DateTimeOffset(value.Value).ToUnixTimeMilliseconds() : null;
        }
        public DateTime? Expiry
        {
            get => Te.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(Te.Value).UtcDateTime : null;
            set => Te = value.HasValue ? new DateTimeOffset(value.Value).ToUnixTimeMilliseconds() : null;
        }
        public string? Checksum { get => Cs; set => Cs = value; }
        public string? Nonce { get => Nk; set => Nk = value; }

        /// <summary>
        /// 创建新的试用期数据
        /// </summary>
        public static TrialData Create(string machineCode, DateTime firstRun, DateTime expiry, int version)
        {
            var nonce = GenerateNonce();
            var data = new TrialData
            {
                V = version,
                Mc = machineCode,
                Fr = new DateTimeOffset(firstRun).ToUnixTimeMilliseconds(),
                Te = new DateTimeOffset(expiry).ToUnixTimeMilliseconds(),
                Nk = nonce
            };

            data.Cs = data.ComputeChecksum();
            return data;
        }

        /// <summary>
        /// 验证校验和
        /// </summary>
        public bool ValidateChecksum()
        {
            if (string.IsNullOrEmpty(Cs)) return false;
            var expected = ComputeChecksum();
            return Cs == expected;
        }

        /// <summary>
        /// 验证机器码
        /// </summary>
        public bool ValidateMachineCode(string currentMachineCode)
        {
            return !string.IsNullOrEmpty(Mc) && Mc == currentMachineCode;
        }

        /// <summary>
        /// 计算校验和
        /// </summary>
        private string ComputeChecksum()
        {
            // 使用关键字段计算校验和
            var dataToHash = $"{V}|{Mc}|{Fr}|{Te}|{Nk}";
            return EncryptionHelper.ComputeSha256Hash(dataToHash).Substring(0, 16);
        }

        /// <summary>
        /// 生成随机数
        /// </summary>
        private static string GenerateNonce()
        {
            var bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    #endregion
}
