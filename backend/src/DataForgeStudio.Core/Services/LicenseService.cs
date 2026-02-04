using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Core.DTO;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;
using DataForgeStudio.Shared.Utils;
using System.Text.Json;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 许可证服务实现 - 零信任架构，仅处理加密数据
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LicenseService> _logger;

    public LicenseService(
        DataForgeStudioDbContext context,
        IConfiguration configuration,
        ILogger<LicenseService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
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

        var licenseInfo = new LicenseInfoDto
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

        return ApiResponse<LicenseInfoDto>.Ok(licenseInfo);
    }

    public async Task<ApiResponse<LicenseInfoDto>> ActivateLicenseAsync(ActivateLicenseRequest request, string? ipAddress)
    {
        try
        {
            // 1. 使用 AES 解密许可证
            string licenseJson;
            try
            {
                var aesKey = _configuration["License:AesKey"];
                var aesIv = _configuration["License:AesIv"];
                licenseJson = EncryptionHelper.AesDecrypt(request.LicenseKey, aesKey, aesIv);
            }
            catch
            {
                return ApiResponse<LicenseInfoDto>.Fail("许可证文件格式错误或已损坏", "INVALID_FORMAT");
            }

            // 2. 解析 JSON
            LicenseData licenseData;
            try
            {
                licenseData = JsonSerializer.Deserialize<LicenseData>(licenseJson);
            }
            catch
            {
                return ApiResponse<LicenseInfoDto>.Fail("许可证内容格式错误", "INVALID_CONTENT");
            }

            // 3. 读取公钥验证签名
            var publicKey = await GetPublicKeyAsync();
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

            // 9. 返回许可证信息
            var licenseInfo = new LicenseInfoDto
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

            return ApiResponse<LicenseInfoDto>.Ok(licenseInfo, "许可证激活成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活许可证失败");
            return ApiResponse<LicenseInfoDto>.Fail($"许可证激活失败: {ex.Message}", "ACTIVATION_FAILED");
        }
    }

    public async Task<ApiResponse<LicenseValidationResponse>> ValidateLicenseAsync()
    {
        var machineCode = EncryptionHelper.GetMachineCode();
        var license = await _context.Licenses
            .Where(l => l.MachineCode == machineCode)
            .OrderByDescending(l => l.ActivatedTime)
            .FirstOrDefaultAsync();

        if (license == null)
        {
            return ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
            {
                Valid = false,
                Message = "未激活许可证"
            });
        }

        // 解密并验证许可证
        var licenseData = await DecryptLicenseAsync(license.LicenseKey);
        if (licenseData == null)
        {
            return ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
            {
                Valid = false,
                Message = "许可证数据无效"
            });
        }

        // 验证签名
        var publicKey = await GetPublicKeyAsync();
        bool isValid = EncryptionHelper.RsaVerifyData(
            JsonSerializer.Serialize(licenseData),
            license.Signature,
            publicKey
        );

        if (!isValid)
        {
            return ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
            {
                Valid = false,
                Message = "许可证已被篡改"
            });
        }

        // 检查过期
        if (licenseData.ExpiryDate < DateTime.UtcNow)
        {
            return ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
            {
                Valid = false,
                Message = $"许可证已过期（过期日期: {licenseData.ExpiryDate:yyyy-MM-dd}）"
            });
        }

        return ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
        {
            Valid = true,
            Message = "许可证有效",
            LicenseInfo = new LicenseInfoDto
            {
                LicenseId = license.LicenseId,
                LicenseType = licenseData.LicenseType,
                CustomerName = licenseData.CustomerName,
                ExpiryDate = licenseData.ExpiryDate,
                MaxUsers = licenseData.MaxUsers,
                MaxReports = licenseData.MaxReports,
                MaxDataSources = licenseData.MaxDataSources,
                Features = licenseData.Features
            }
        });
    }

    /// <summary>
    /// 解密许可证数据
    /// </summary>
    private async Task<LicenseData?> DecryptLicenseAsync(string encryptedLicense)
    {
        try
        {
            var aesKey = _configuration["License:AesKey"];
            var aesIv = _configuration["License:AesIv"];
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
    /// 读取公钥
    /// </summary>
    private async Task<string> GetPublicKeyAsync()
    {
        var publicKeyPath = _configuration["License:PublicKeyPath"];
        return await File.ReadAllTextAsync(publicKeyPath);
    }
}
