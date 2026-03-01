using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Core.Services;
using DataForgeStudio.Domain.DTOs;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 看板视图控制器
/// </summary>
[ApiController]
[Route("api/kanban")]
[Authorize]
public class KanbanController : ControllerBase
{
    private readonly IKanbanService _kanbanService;
    private readonly ILogger<KanbanController> _logger;

    public KanbanController(IKanbanService kanbanService, ILogger<KanbanController> logger)
    {
        _kanbanService = kanbanService;
        _logger = logger;
    }

    /// <summary>
    /// 获取看板的所有卡片
    /// </summary>
    /// <param name="dashboardId">看板 ID</param>
    /// <returns>卡片列表</returns>
    [HttpGet("{dashboardId}/cards")]
    [ProducesResponseType(typeof(ApiResponse<List<KanbanCardDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCards(int dashboardId)
    {
        var result = await _kanbanService.GetCardsAsync(dashboardId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 获取单个卡片详情
    /// </summary>
    /// <param name="dashboardId">看板 ID</param>
    /// <param name="cardId">卡片 ID</param>
    /// <returns>卡片详情</returns>
    [HttpGet("{dashboardId}/cards/{cardId}")]
    [ProducesResponseType(typeof(ApiResponse<KanbanCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCard(int dashboardId, string cardId)
    {
        var result = await _kanbanService.GetCardByIdAsync(dashboardId, cardId);

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
    /// 创建新卡片
    /// </summary>
    /// <param name="dashboardId">看板 ID</param>
    /// <param name="dto">卡片数据</param>
    /// <returns>创建的卡片</returns>
    [HttpPost("{dashboardId}/cards")]
    [ProducesResponseType(typeof(ApiResponse<KanbanCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateCard(int dashboardId, [FromBody] KanbanCardCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<KanbanCardDto>.Fail("请求参数错误", "INVALID_PARAMS"));
        }

        var result = await _kanbanService.CreateCardAsync(dashboardId, dto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 更新卡片
    /// </summary>
    /// <param name="dashboardId">看板 ID</param>
    /// <param name="cardId">卡片 ID</param>
    /// <param name="dto">卡片数据</param>
    /// <returns>操作结果</returns>
    [HttpPut("{dashboardId}/cards/{cardId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCard(int dashboardId, string cardId, [FromBody] KanbanCardCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<bool>.Fail("请求参数错误", "INVALID_PARAMS"));
        }

        var result = await _kanbanService.UpdateCardAsync(dashboardId, cardId, dto);

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
    /// 删除卡片
    /// </summary>
    /// <param name="dashboardId">看板 ID</param>
    /// <param name="cardId">卡片 ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{dashboardId}/cards/{cardId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCard(int dashboardId, string cardId)
    {
        var result = await _kanbanService.DeleteCardAsync(dashboardId, cardId);

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
    /// 移动卡片到新位置
    /// </summary>
    /// <param name="dashboardId">看板 ID</param>
    /// <param name="dto">移动请求</param>
    /// <returns>移动结果</returns>
    [HttpPost("{dashboardId}/cards/move")]
    [ProducesResponseType(typeof(ApiResponse<KanbanMoveCardResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveCard(int dashboardId, [FromBody] KanbanMoveCardDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<KanbanMoveCardResponseDto>.Fail("请求参数错误", "INVALID_PARAMS"));
        }

        var result = await _kanbanService.MoveCardAsync(dashboardId, dto);

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
    /// 获取看板配置
    /// </summary>
    /// <param name="dashboardId">看板 ID</param>
    /// <returns>看板配置</returns>
    [HttpGet("{dashboardId}/config")]
    [ProducesResponseType(typeof(ApiResponse<KanbanBoardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBoardConfig(int dashboardId)
    {
        var result = await _kanbanService.GetBoardAsync(dashboardId);

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
