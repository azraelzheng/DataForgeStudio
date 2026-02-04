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
    private const string CACHE_KEY = "LicenseValidation";
    private const int CACHE_DURATION_MINUTES = 30;
    private const string LICENSE_TYPE_TRIAL = "Trial";

    public LicenseService(
        DataForgeStudioDbContext context,
        ILogger<LicenseService> logger,
        IKeyManagementService keyManagementService,
        IMemoryCache memoryCache)
    {
        _context = context;
        _logger = logger;
        _keyManagementService = keyManagementService;
        _memoryCache = memoryCache;
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

            // 2. 解析 JSON
            LicenseData licenseData;
            try
            {
                licenseData = JsonSerializer.Deserialize<LicenseData>(licenseJson);
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
            var publicKey = await _keyManagementService.GetPublicKeyAsync();
            bool isValid = EncryptionHelper.RsaVerifyData(
                licenseJson,
                licenseData.Signature,
                publicKey
            );

            if (!isValid)
            {
                return ApiResponse<LicenseInfoDto>.Fail("许可证已被篡改，无法激活。请联系供应商重新获取许可证。", "TAMPERED");
            }

            // 4. 获取当前服务器机器码
            var currentMachineCode = EncryptionHelper.GetMachineCode();

            // 5. 验证机器码是否匹配
            if (licenseData.MachineCode != currentMachineCode)
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

        // 验证签名
        var publicKey = await _keyManagementService.GetPublicKeyAsync();
        bool isValid = EncryptionHelper.RsaVerifyData(
            JsonSerializer.Serialize(licenseData),
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
            return JsonSerializer.Deserialize<LicenseData>(licenseJson);
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
    private async Task<ApiResponse<LicenseInfoDto>> GenerateTrialLicenseAsync()
    {
        try
        {
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
            var licenseData = new LicenseData
            {
                LicenseId = Guid.NewGuid().ToString(),
                CustomerName = "试用用户",
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                MaxUsers = 5,
                MaxReports = 10,
                MaxDataSources = 2,
                Features = new List<string> { "报表设计", "报表查询", "数据源管理" },
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
}
