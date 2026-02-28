using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Domain.DTOs;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 车间大屏控制器
/// </summary>
[ApiController]
[Route("api/display")]
[Authorize]
public class DisplayController : ControllerBase
{
    private readonly IDisplayService _displayService;
    private readonly ILogger<DisplayController> _logger;

    public DisplayController(IDisplayService displayService, ILogger<DisplayController> logger)
    {
        _displayService = displayService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户 ID
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// 获取大屏配置列表
    /// </summary>
    /// <returns>配置列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DisplayConfigDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDisplayConfigs()
    {
        var result = await _displayService.GetDisplayConfigsAsync();

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 获取大屏配置详情
    /// </summary>
    /// <param name="id">配置 ID</param>
    /// <returns>配置详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DisplayConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDisplayConfig(string id)
    {
        var result = await _displayService.GetDisplayConfigByIdAsync(id);

        if (!result.Success)
        {
            if (result.ErrorCode == "NOT_FOUND")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 创建大屏配置
    /// </summary>
    /// <param name="dto">配置数据</param>
    /// <returns>创建的配置</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DisplayConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateDisplayConfig([FromBody] DisplayConfigCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<DisplayConfigDto>.Fail("请求参数错误", "INVALID_PARAMS"));
        }

        var userId = GetCurrentUserId();
        var result = await _displayService.CreateDisplayConfigAsync(dto, userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 更新大屏配置
    /// </summary>
    /// <param name="id">配置 ID</param>
    /// <param name="dto">配置数据</param>
    /// <returns>更新后的配置</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DisplayConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDisplayConfig(string id, [FromBody] DisplayConfigUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<DisplayConfigDto>.Fail("请求参数错误", "INVALID_PARAMS"));
        }

        var userId = GetCurrentUserId();
        var result = await _displayService.UpdateDisplayConfigAsync(id, dto, userId);

        if (!result.Success)
        {
            if (result.ErrorCode == "NOT_FOUND")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 删除大屏配置
    /// </summary>
    /// <param name="id">配置 ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteDisplayConfig(string id)
    {
        var result = await _displayService.DeleteDisplayConfigAsync(id);

        if (!result.Success)
        {
            if (result.ErrorCode == "NOT_FOUND")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 获取大屏聚合数据
    /// </summary>
    /// <param name="id">配置 ID</param>
    /// <returns>聚合数据</returns>
    [HttpGet("{id}/data")]
    [ProducesResponseType(typeof(ApiResponse<DisplayDataResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDisplayData(string id)
    {
        var result = await _displayService.GetDisplayDataAsync(id);

        if (!result.Success)
        {
            if (result.ErrorCode == "NOT_FOUND")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}

/// <summary>
/// 看板控制器
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户 ID
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// 获取看板列表
    /// </summary>
    /// <returns>看板列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DashboardDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboards()
    {
        var result = await _dashboardService.GetDashboardsAsync();

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 获取看板详情
    /// </summary>
    /// <param name="id">看板 ID</param>
    /// <returns>看板详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(string id)
    {
        var result = await _dashboardService.GetDashboardByIdAsync(id);

        if (!result.Success)
        {
            if (result.ErrorCode == "NOT_FOUND")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 创建看板
    /// </summary>
    /// <param name="dto">看板数据</param>
    /// <returns>创建的看板</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateDashboard([FromBody] DashboardCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<DashboardDto>.Fail("请求参数错误", "INVALID_PARAMS"));
        }

        var userId = GetCurrentUserId();
        var result = await _dashboardService.CreateDashboardAsync(dto, userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 更新看板
    /// </summary>
    /// <param name="id">看板 ID</param>
    /// <param name="dto">看板数据</param>
    /// <returns>更新后的看板</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDashboard(string id, [FromBody] DashboardUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<DashboardDto>.Fail("请求参数错误", "INVALID_PARAMS"));
        }

        var userId = GetCurrentUserId();
        var result = await _dashboardService.UpdateDashboardAsync(id, dto, userId);

        if (!result.Success)
        {
            if (result.ErrorCode == "NOT_FOUND")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 删除看板
    /// </summary>
    /// <param name="id">看板 ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteDashboard(string id)
    {
        var result = await _dashboardService.DeleteDashboardAsync(id);

        if (!result.Success)
        {
            if (result.ErrorCode == "NOT_FOUND")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}
