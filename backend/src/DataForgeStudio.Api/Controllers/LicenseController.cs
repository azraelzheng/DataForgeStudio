using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 许可证管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LicenseController : ControllerBase
{
    private readonly ILicenseService _licenseService;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(ILicenseService licenseService, ILogger<LicenseController> logger)
    {
        _licenseService = licenseService;
        _logger = logger;
    }

    /// <summary>
    /// 获取许可证信息
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ApiResponse<LicenseInfoDto>> GetLicense()
    {
        return await _licenseService.GetLicenseAsync();
    }

    /// <summary>
    /// 激活许可证
    /// </summary>
    [HttpPost("activate")]
    public async Task<ApiResponse<LicenseInfoDto>> ActivateLicense([FromBody] ActivateLicenseRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        return await _licenseService.ActivateLicenseAsync(request, ipAddress);
    }

    /// <summary>
    /// 验证许可证
    /// </summary>
    [HttpPost("validate")]
    public async Task<ApiResponse<LicenseValidationResponse>> ValidateLicense([FromQuery] bool forceRefresh = false)
    {
        return await _licenseService.ValidateLicenseAsync(forceRefresh);
    }
}
