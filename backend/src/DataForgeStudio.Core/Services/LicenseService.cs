using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;
using DataForgeStudio.Shared.Utils;
using System.Text.Json;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 许可证服务实现
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
        var license = await _context.Licenses
            .Where(l => l.IsActive)
            .OrderByDescending(l => l.ActivatedTime)
            .FirstOrDefaultAsync();

        if (license == null)
        {
            return ApiResponse<LicenseInfoDto>.Fail("未激活许可证", "NO_LICENSE");
        }

        var licenseInfo = new LicenseInfoDto
        {
            LicenseId = license.LicenseId,
            LicenseType = DetermineLicenseType(license),
            CustomerName = license.CompanyName,
            ExpiryDate = license.ExpiryDate,
            MaxUsers = license.MaxUsers,
            MaxReports = license.MaxReports,
            MaxDataSources = license.MaxDataSources,
            Features = !string.IsNullOrEmpty(license.Features)
                ? JsonSerializer.Deserialize<List<string>>(license.Features)
                : new List<string>()
        };

        return ApiResponse<LicenseInfoDto>.Ok(licenseInfo);
    }

    public async Task<ApiResponse<LicenseInfoDto>> ActivateLicenseAsync(ActivateLicenseRequest request, string? ipAddress)
    {
        try
        {
            // TODO: 实现实际的许可证验证逻辑
            // 1. 使用RSA解密许可证密钥
            // 2. 验证许可证签名
            // 3. 提取许可证信息
            // 4. 检查许可证是否已使用
            // 5. 保存许可证信息

            // 简单示例：创建试用许可证
            var trialExpiry = DateTime.UtcNow.AddDays(30);
            var features = new List<string> { "报表设计", "报表查询", "图表展示", "Excel导出", "数据源管理" };

            var license = new License
            {
                LicenseKey = EncryptionHelper.EncryptRSA(request.LicenseKey),
                CompanyName = "试用客户",
                MaxUsers = 10,
                MaxReports = 100,
                MaxDataSources = 5,
                ExpiryDate = trialExpiry,
                Features = JsonSerializer.Serialize(features),
                IsActive = true,
                ActivatedTime = DateTime.UtcNow,
                ActivatedIP = ipAddress,
                MachineCode = GenerateMachineCode(),
                CreatedTime = DateTime.UtcNow
            };

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync();

            var licenseInfo = new LicenseInfoDto
            {
                LicenseId = license.LicenseId,
                LicenseType = "Trial",
                CustomerName = license.CompanyName,
                ExpiryDate = license.ExpiryDate,
                MaxUsers = license.MaxUsers,
                MaxReports = license.MaxReports,
                MaxDataSources = license.MaxDataSources,
                Features = features
            };

            return ApiResponse<LicenseInfoDto>.Ok(licenseInfo, "许可证激活成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活许可证失败");
            return ApiResponse<LicenseInfoDto>.Fail("许可证激活失败: " + ex.Message, "ACTIVATION_FAILED");
        }
    }

    public async Task<ApiResponse<LicenseValidationResponse>> ValidateLicenseAsync()
    {
        var license = await _context.Licenses
            .Where(l => l.IsActive)
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

        if (license.ExpiryDate.HasValue && license.ExpiryDate.Value < DateTime.UtcNow)
        {
            return ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
            {
                Valid = false,
                Message = "许可证已过期"
            });
        }

        return ApiResponse<LicenseValidationResponse>.Ok(new LicenseValidationResponse
        {
            Valid = true,
            Message = "许可证有效"
        });
    }

    private string DetermineLicenseType(License license)
    {
        if (!license.ExpiryDate.HasValue || license.ExpiryDate.Value > DateTime.UtcNow.AddYears(1))
        {
            return "Enterprise";
        }
        else if (license.MaxUsers.HasValue && license.MaxUsers.Value > 50)
        {
            return "Professional";
        }
        else if (license.MaxUsers.HasValue && license.MaxUsers.Value > 10)
        {
            return "Standard";
        }
        return "Trial";
    }

    private string GenerateMachineCode()
    {
        // TODO: 实现基于机器硬件的唯一标识
        return Environment.MachineName + "_" + Environment.UserName;
    }
}
