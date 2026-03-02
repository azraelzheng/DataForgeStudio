using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Core.DTO;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;
using DataForgeStudio.Shared.Utils;
using System.Text.Json;
using System.Security.Cryptography;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 许可证服务实现 - 零信任架构，仅处理加密数据
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly ILogger<LicenseService> _logger;
    private readonly IKeyManagementService _keyManagementService;
    private readonly IMemoryCache _memoryCache;
    private readonly ITrialLicenseTracker _trialTracker;
    private const string CACHE_KEY = "LicenseValidation";
    private const int CACHE_DURATION_MINUTES = 30;
    private const string LICENSE_TYPE_TRIAL = "Trial";

    public LicenseService(
        DataForgeStudioDbContext context,
        ILogger<LicenseService> logger,
        IKeyManagementService keyManagementService,
        IMemoryCache memoryCache,
        ITrialLicenseTracker trialTracker)
    {
        _context = context;
        _logger = logger;
        _keyManagementService = keyManagementService;
        _memoryCache = memoryCache;
        _trialTracker = trialTracker;
    }

    public async Task<ApiResponse<LicenseInfoDto>> GetLicenseAsync()
    {
        var machineCode = EncryptionHelper.GetMachineCode();
        var license = await _context.Licenses
            .Where(l => l.MachineCode == machineCode)
            .OrderByDescending(l => l.ActivatedTime)
            .FirstOrDefaultAsync();

        if (license == null)
        {
            return ApiResponse<LicenseInfoDto>.Fail("未激活许可证", "NO_LICENSE");
        }

        // 解密许可证数据
        var licenseData = await DecryptLicenseAsync(license.LicenseKey);
        if (licenseData == null)
        {
            return ApiResponse<LicenseInfoDto>.Fail("许可证数据解密失败", "DECRYPT_FAILED");
        }

        var licenseInfo = MapToLicenseInfoDto(license, licenseData);

        return ApiResponse<LicenseInfoDto>.Ok(licenseInfo);
    }

    /// <summary>
    /// 将许可证实体和数据映射到 DTO
    /// </summary>
    private LicenseInfoDto MapToLicenseInfoDto(License license, LicenseData licenseData)
    {
        return new LicenseInfoDto
        {
            LicenseId = license.LicenseId,
            LicenseType = licenseData.LicenseType,
            CustomerName = licenseData.CustomerName,
            ExpiryDate = licenseData.ExpiryDate,
            MaxUsers = licenseData.MaxUsers,
            MaxReports = licenseData.MaxReports,
            MaxDataSources = licenseData.MaxDataSources,
            MaxDashboards = licenseData.MaxDashboards,
            Features = licenseData.Features
        };
    }

    public async Task<ApiResponse<LicenseInfoDto>> ActivateLicenseAsync(ActivateLicenseRequest request, string? ipAddress)
    {
        try
        {
            // 1. 使用 AES 解密许可证
            string licenseJson;
            try
            {
                var aesKey = _keyManagementService.GetAesKey();
                var aesIv = _keyManagementService.GetAesIv();
                licenseJson = EncryptionHelper.AesDecrypt(request.LicenseKey, aesKey, aesIv);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "许可证 AES 解密失败");
                return ApiResponse<LicenseInfoDto>.Fail("许可证文件格式错误或已损坏", "INVALID_FORMAT");
            }

            // 2. 解析 JSON（使用 camelCase 命名策略，与 LicenseGenerator 保持一致）
            LicenseData licenseData;
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
                licenseData = JsonSerializer.Deserialize<LicenseData>(licenseJson, jsonOptions);
                if (licenseData == null)
                {
                    _logger.LogError("许可证数据反序列化后为 null");
                    return ApiResponse<LicenseInfoDto>.Fail("许可证内容格式错误", "INVALID_CONTENT");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "许可证数据反序列化失败");
                return ApiResponse<LicenseInfoDto>.Fail("许可证内容格式错误", "INVALID_CONTENT");
            }

            // 3. 使用 KeyManagementService 获取公钥验证签名
            // 重新构建不含 Signature 的 JSON（与 LicenseGenerator 签名时的格式一致）
            var jsonForVerification = JsonSerializer.Serialize(new
            {
                LicenseId = licenseData.LicenseId,
                CustomerName = licenseData.CustomerName,
                ExpiryDate = licenseData.ExpiryDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                MaxUsers = licenseData.MaxUsers,
                MaxReports = licenseData.MaxReports,
                MaxDataSources = licenseData.MaxDataSources,
                MaxDashboards = licenseData.MaxDashboards,
                Features = licenseData.Features,
                MachineCode = licenseData.MachineCode,
                IssuedDate = licenseData.IssuedDate,
                LicenseType = licenseData.LicenseType
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var publicKey = await _keyManagementService.GetPublicKeyAsync();
            bool isValid = EncryptionHelper.RsaVerifyData(
                jsonForVerification,
                licenseData.Signature,
                publicKey
            );

            if (!isValid)
            {
                return ApiResponse<LicenseInfoDto>.Fail("许可证已被篡改，无法激活。请联系供应商重新获取许可证。", "TAMPERED");
            }

            // 4. 获取当前服务器机器码
            var currentMachineCode = EncryptionHelper.GetMachineCode();

            // 5. 验证机器码是否匹配（如果许可证绑定了机器码）
            if (!string.IsNullOrEmpty(licenseData.MachineCode) && licenseData.MachineCode != currentMachineCode)
            {
                return ApiResponse<LicenseInfoDto>.Fail(
                    $"许可证与当前服务器不匹配\n许可证绑定机器码: {licenseData.MachineCode}\n当前服务器机器码: {currentMachineCode}\n\n请联系供应商，提供当前服务器机器码以重新生成许可证。",
                    "MACHINE_MISMATCH"
                );
            }

            // 6. 检查是否过期
            if (licenseData.ExpiryDate < DateTime.UtcNow)
            {
                return ApiResponse<LicenseInfoDto>.Fail(
                    $"许可证已过期（过期日期: {licenseData.ExpiryDate:yyyy-MM-dd}），请联系供应商续费。",
                    "EXPIRED"
                );
            }

            // 7. 检查该机器码是否已激活过其他许可证
            var existingLicense = await _context.Licenses
                .Where(l => l.MachineCode == currentMachineCode)
                .FirstOrDefaultAsync();

            if (existingLicense != null)
            {
                // 替换为新的许可证
                _context.Licenses.Remove(existingLicense);
            }

            // 8. 保存许可证到数据库
            var license = new Domain.Entities.License
            {
                LicenseKey = request.LicenseKey, // 加密的完整 JSON
                Signature = licenseData.Signature,
                MachineCode = currentMachineCode,
                ActivatedTime = DateTime.UtcNow,
                ActivatedIP = ipAddress,
                CreatedTime = DateTime.UtcNow
            };

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync();

            // 清除缓存，以便下次验证时使用新的许可证
            _memoryCache.Remove(CACHE_KEY);

            // 9. 返回许可证信息
            var licenseInfo = MapToLicenseInfoDto(license, licenseData);

            return ApiResponse<LicenseInfoDto>.Ok(licenseInfo, "许可证激活成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活许可证失败");
            return ApiResponse<LicenseInfoDto>.Fail($"许可证激活失败: {ex.Message}", "ACTIVATION_FAILED");
        }
    }

    /// <summary>
    /// 验证许可证（带缓存，支持自动生成试用许可证）
    /// </summary>
    /// <param name="forceRefresh">是否强制刷新缓存</param>
    public async Task<ApiResponse<LicenseValidationResponse>> ValidateLicenseAsync(bool forceRefresh = false)
    {
        // 检查缓存
        if (!forceRefresh && _memoryCache.TryGetValue<ApiResponse<LicenseValidationResponse>>(CACHE_KEY, out var cachedResponse))
        {
            _logger.LogDebug("从缓存返回许可证验证结果");
            return cachedResponse;
        }

        var machineCode = EncryptionHelper.GetMachineCode();
        var license = await _context.Licenses
            .Where(l => l.MachineCode == machineCode)
            .OrderByDescending(l => l.ActivatedTime)
            .FirstOrDefaultAsync();

        if (license == null)
        {
            // 自动生成试用许可证
            _logger.LogInformation("未找到许可证，自动生成试用许可证");
            var trialResult = await GenerateTrialLicenseAsync();

            if (!trialResult.Success)
            {
                var response = ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
                {
                    Valid = false,
                    Message = $"未激活许可证，且试用许可证生成失败: {trialResult.Message}"
                });
                _memoryCache.Set(CACHE_KEY, response, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return response;
            }

            var trialValidationResponse = ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
            {
                Valid = true,
                Message = "试用许可证已自动生成",
                LicenseInfo = trialResult.Data
            });
            _memoryCache.Set(CACHE_KEY, trialValidationResponse, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return trialValidationResponse;
        }

        // 解密并验证许可证
        var licenseData = await DecryptLicenseAsync(license.LicenseKey);
        if (licenseData == null)
        {
            var response = ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
            {
                Valid = false,
                Message = "许可证数据无效"
            });
            _memoryCache.Set(CACHE_KEY, response, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return response;
        }

        // 验证签名 - 重新构建不含 Signature 的 JSON（与 LicenseGenerator 签名时的格式一致）
        var jsonForVerification = JsonSerializer.Serialize(new
        {
            LicenseId = licenseData.LicenseId,
            CustomerName = licenseData.CustomerName,
            ExpiryDate = licenseData.ExpiryDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MaxUsers = licenseData.MaxUsers,
            MaxReports = licenseData.MaxReports,
            MaxDataSources = licenseData.MaxDataSources,
            MaxDashboards = licenseData.MaxDashboards,
            Features = licenseData.Features,
            MachineCode = licenseData.MachineCode,
            IssuedDate = licenseData.IssuedDate,
            LicenseType = licenseData.LicenseType
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var publicKey = await _keyManagementService.GetPublicKeyAsync();
        bool isValid = EncryptionHelper.RsaVerifyData(
            jsonForVerification,
            licenseData.Signature,
            publicKey
        );

        if (!isValid)
        {
            var response = ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
            {
                Valid = false,
                Message = "许可证已被篡改"
            });
            _memoryCache.Set(CACHE_KEY, response, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return response;
        }

        // 检查过期
        if (licenseData.ExpiryDate < DateTime.UtcNow)
        {
            var response = ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
            {
                Valid = false,
                Message = $"许可证已过期（过期日期: {licenseData.ExpiryDate:yyyy-MM-dd}）"
            });
            _memoryCache.Set(CACHE_KEY, response, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return response;
        }

        var validationResponse = ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
        {
            Valid = true,
            Message = "许可证有效",
            LicenseInfo = MapToLicenseInfoDto(license, licenseData)
        });

        // 缓存结果
        _memoryCache.Set(CACHE_KEY, validationResponse, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

        return validationResponse;
    }

    /// <summary>
    /// 解密许可证数据
    /// </summary>
    private async Task<LicenseData?> DecryptLicenseAsync(string encryptedLicense)
    {
        try
        {
            var aesKey = _keyManagementService.GetAesKey();
            var aesIv = _keyManagementService.GetAesIv();
            var licenseJson = EncryptionHelper.AesDecrypt(encryptedLicense, aesKey, aesIv);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<LicenseData>(licenseJson, jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密许可证失败");
            return null;
        }
    }

    /// <summary>
    /// 生成试用许可证
    /// </summary>
    public async Task<ApiResponse<LicenseInfoDto>> GenerateTrialLicenseAsync()
    {
        try
        {
            // 使用 TrialLicenseTracker 检查试用期状态
            var trialStatus = _trialTracker.CheckTrialStatus();

            if (trialStatus.IsFirstRun)
            {
                // 首次运行，记录试用期起始时间
                _trialTracker.RecordFirstRun();
                _logger.LogInformation("首次运行，已记录试用期起始时间");
            }
            else if (!trialStatus.IsValid)
            {
                // 试用期已过期
                var trialExpiryDate = trialStatus.ExpiryTime?.ToString("yyyy-MM-dd") ?? "未知";
                _logger.LogWarning("试用期已过期，过期日期: {ExpiryDate}", trialExpiryDate);
                return ApiResponse<LicenseInfoDto>.Fail(
                    $"试用期已过期（过期日期: {trialExpiryDate}），请联系供应商购买正式许可证。",
                    "TRIAL_EXPIRED");
            }
            else
            {
                // 试用期有效
                _logger.LogInformation("试用期有效，剩余 {Days} 天", trialStatus.DaysRemaining);
            }

            var machineCode = EncryptionHelper.GetMachineCode();

            // 检查是否已有试用许可证
            var existingLicenses = await _context.Licenses
                .Where(l => l.MachineCode == machineCode)
                .ToListAsync();

            // 尝试解密现有许可证以检查是否有试用许可证
            foreach (var existingLicense in existingLicenses)
            {
                try
                {
                    var existingData = await DecryptLicenseAsync(existingLicense.LicenseKey);
                    if (existingData != null && existingData.LicenseType == LICENSE_TYPE_TRIAL)
                    {
                        return ApiResponse<LicenseInfoDto>.Fail("该机器已使用过试用许可证", "TRIAL_USED");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解密现有许可证失败，跳过此许可证");
                }
            }

            // 生成试用许可证数据
            // 使用 TrialLicenseTracker 计算的到期时间（15天试用期）
            var expiryDate = trialStatus.ExpiryTime ?? DateTime.UtcNow.AddDays(15);
            var licenseData = new LicenseData
            {
                LicenseId = Guid.NewGuid().ToString(),
                CustomerName = "试用用户",
                ExpiryDate = expiryDate,
                MaxUsers = 5,
                MaxReports = 10,
                MaxDataSources = 2,
                MaxDashboards = 3,
                Features = new List<string> { "报表设计", "报表查询", "数据源管理", "大屏设计" },
                MachineCode = machineCode,
                IssuedDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                LicenseType = LICENSE_TYPE_TRIAL,
                Signature = "" // 稍后生成签名
            };

            // 序列化为 JSON（不包含 Signature）
            var licenseJson = JsonSerializer.Serialize(licenseData);

            // 使用私钥签名
            using var rsa = await _keyManagementService.GetRsaWithPrivateKeyAsync();
            var signatureBytes = rsa.SignData(
                System.Text.Encoding.UTF8.GetBytes(licenseJson),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
            licenseData.Signature = Convert.ToBase64String(signatureBytes);

            // 重新序列化包含签名的完整数据
            var fullLicenseJson = JsonSerializer.Serialize(licenseData);

            // 使用 AES 加密
            var aesKey = _keyManagementService.GetAesKey();
            var aesIv = _keyManagementService.GetAesIv();
            var encryptedLicense = EncryptionHelper.AesEncrypt(fullLicenseJson, aesKey, aesIv);

            // 保存到数据库
            var license = new Domain.Entities.License
            {
                LicenseKey = encryptedLicense,
                Signature = licenseData.Signature,
                MachineCode = machineCode,
                ActivatedTime = DateTime.UtcNow,
                ActivatedIP = null,
                CreatedTime = DateTime.UtcNow
            };

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync();

            _logger.LogInformation("试用许可证已生成: {LicenseId}, 过期日期: {ExpiryDate}",
                licenseData.LicenseId, licenseData.ExpiryDate);

            // 返回许可证信息
            var licenseInfo = MapToLicenseInfoDto(license, licenseData);

            return ApiResponse<LicenseInfoDto>.Ok(licenseInfo, "试用许可证已自动生成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成试用许可证失败");
            return ApiResponse<LicenseInfoDto>.Fail($"生成试用许可证失败: {ex.Message}", "TRIAL_GENERATION_FAILED");
        }
    }

    /// <summary>
    /// 获取许可证使用统计
    /// </summary>
    public async Task<ApiResponse<LicenseUsageStatsDto>> GetUsageStatsAsync()
    {
        try
        {
            // 统计非系统用户数量
            var currentUsers = await _context.Users
                .Where(u => !u.IsSystem)
                .CountAsync();

            // 统计报表数量
            var currentReports = await _context.Reports
                .CountAsync();

            // 统计数据源数量
            var currentDataSources = await _context.DataSources
                .CountAsync();

            // 统计大屏数量
            var currentDashboards = await _context.Dashboards
                .CountAsync();

            var stats = new LicenseUsageStatsDto
            {
                CurrentUsers = currentUsers,
                CurrentReports = currentReports,
                CurrentDataSources = currentDataSources,
                CurrentDashboards = currentDashboards
            };

            return ApiResponse<LicenseUsageStatsDto>.Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取许可证使用统计失败");
            return ApiResponse<LicenseUsageStatsDto>.Fail($"获取统计数据失败: {ex.Message}", "GET_STATS_FAILED");
        }
    }

    /// <summary>
    /// 检查是否可以创建新的用户
    /// </summary>
    public async Task<ApiResponse> CheckUserLimitAsync()
    {
        try
        {
            // 验证许可证是否有效
            var validationResult = await ValidateLicenseAsync();
            if (!validationResult.Success || validationResult.Data == null || !validationResult.Data.Valid)
            {
                return ApiResponse.Fail(validationResult.Data?.Message ?? "许可证无效", "LICENSE_INVALID");
            }

            var licenseInfo = validationResult.Data.LicenseInfo;
            if (licenseInfo == null)
            {
                return ApiResponse.Fail("无法获取许可证信息", "LICENSE_INFO_MISSING");
            }

            // 如果 MaxUsers 为 0，表示无限制
            if (licenseInfo.MaxUsers == 0)
            {
                return ApiResponse.Ok();
            }

            // 统计当前用户数量
            var currentUsers = await _context.Users
                .Where(u => !u.IsSystem)
                .CountAsync();

            if (currentUsers >= licenseInfo.MaxUsers)
            {
                return ApiResponse.Fail(
                    $"已达到许可证用户数量限制（当前: {currentUsers}，最大: {licenseInfo.MaxUsers}），无法创建新用户",
                    "USER_LIMIT_EXCEEDED");
            }

            return ApiResponse.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用户数量限制失败");
            return ApiResponse.Fail($"检查用户限制失败: {ex.Message}", "CHECK_LIMIT_FAILED");
        }
    }

    /// <summary>
    /// 检查是否可以创建新的报表
    /// </summary>
    public async Task<ApiResponse> CheckReportLimitAsync()
    {
        try
        {
            // 验证许可证是否有效
            var validationResult = await ValidateLicenseAsync();
            if (!validationResult.Success || validationResult.Data == null || !validationResult.Data.Valid)
            {
                return ApiResponse.Fail(validationResult.Data?.Message ?? "许可证无效", "LICENSE_INVALID");
            }

            var licenseInfo = validationResult.Data.LicenseInfo;
            if (licenseInfo == null)
            {
                return ApiResponse.Fail("无法获取许可证信息", "LICENSE_INFO_MISSING");
            }

            // 如果 MaxReports 为 0，表示无限制
            if (licenseInfo.MaxReports == 0)
            {
                return ApiResponse.Ok();
            }

            // 统计当前报表数量
            var currentReports = await _context.Reports.CountAsync();

            if (currentReports >= licenseInfo.MaxReports)
            {
                return ApiResponse.Fail(
                    $"已达到许可证报表数量限制（当前: {currentReports}，最大: {licenseInfo.MaxReports}），无法创建新报表",
                    "REPORT_LIMIT_EXCEEDED");
            }

            return ApiResponse.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查报表数量限制失败");
            return ApiResponse.Fail($"检查报表限制失败: {ex.Message}", "CHECK_LIMIT_FAILED");
        }
    }

    /// <summary>
    /// 检查是否可以创建新的数据源
    /// </summary>
    public async Task<ApiResponse> CheckDataSourceLimitAsync()
    {
        try
        {
            // 验证许可证是否有效
            var validationResult = await ValidateLicenseAsync();
            if (!validationResult.Success || validationResult.Data == null || !validationResult.Data.Valid)
            {
                return ApiResponse.Fail(validationResult.Data?.Message ?? "许可证无效", "LICENSE_INVALID");
            }

            var licenseInfo = validationResult.Data.LicenseInfo;
            if (licenseInfo == null)
            {
                return ApiResponse.Fail("无法获取许可证信息", "LICENSE_INFO_MISSING");
            }

            // 如果 MaxDataSources 为 0，表示无限制
            if (licenseInfo.MaxDataSources == 0)
            {
                return ApiResponse.Ok();
            }

            // 统计当前数据源数量
            var currentDataSources = await _context.DataSources.CountAsync();

            if (currentDataSources >= licenseInfo.MaxDataSources)
            {
                return ApiResponse.Fail(
                    $"已达到许可证数据源数量限制（当前: {currentDataSources}，最大: {licenseInfo.MaxDataSources}），无法创建新数据源",
                    "DATASOURCE_LIMIT_EXCEEDED");
            }

            return ApiResponse.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查数据源数量限制失败");
            return ApiResponse.Fail($"检查数据源限制失败: {ex.Message}", "CHECK_LIMIT_FAILED");
        }
    }

    /// <summary>
    /// 检查是否可以创建新的大屏
    /// </summary>
    public async Task<ApiResponse> CheckDashboardLimitAsync()
    {
        try
        {
            // 验证许可证是否有效
            var validationResult = await ValidateLicenseAsync();
            if (!validationResult.Success || validationResult.Data == null || !validationResult.Data.Valid)
            {
                return ApiResponse.Fail(validationResult.Data?.Message ?? "许可证无效", "LICENSE_INVALID");
            }

            var licenseInfo = validationResult.Data.LicenseInfo;
            if (licenseInfo == null)
            {
                return ApiResponse.Fail("无法获取许可证信息", "LICENSE_INFO_MISSING");
            }

            // 如果 MaxDashboards 为 0 或 null，表示无限制
            if (licenseInfo.MaxDashboards == null || licenseInfo.MaxDashboards == 0)
            {
                return ApiResponse.Ok();
            }

            // 统计当前大屏数量
            var currentDashboards = await _context.Dashboards.CountAsync();

            if (currentDashboards >= licenseInfo.MaxDashboards)
            {
                return ApiResponse.Fail(
                    $"已达到许可证大屏数量限制（当前: {currentDashboards}，最大: {licenseInfo.MaxDashboards}），无法创建新大屏",
                    "DASHBOARD_LIMIT_EXCEEDED");
            }

            return ApiResponse.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查大屏数量限制失败");
            return ApiResponse.Fail($"检查大屏限制失败: {ex.Message}", "CHECK_LIMIT_FAILED");
        }
    }
}
