using DataForgeStudio.Domain.DTOs;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 看板服务接口
/// </summary>
public interface IKanbanService
{
    /// <summary>
    /// 获取看板的所有卡片
    /// </summary>
    Task<ApiResponse<List<KanbanCardDto>>> GetCardsAsync(int dashboardId);

    /// <summary>
    /// 获取单个卡片详情
    /// </summary>
    Task<ApiResponse<KanbanCardDto>> GetCardByIdAsync(int dashboardId, string cardId);

    /// <summary>
    /// 创建新卡片
    /// </summary>
    Task<ApiResponse<KanbanCardDto>> CreateCardAsync(int dashboardId, KanbanCardCreateDto dto);

    /// <summary>
    /// 更新卡片
    /// </summary>
    Task<ApiResponse<bool>> UpdateCardAsync(int dashboardId, string cardId, KanbanCardCreateDto dto);

    /// <summary>
    /// 删除卡片
    /// </summary>
    Task<ApiResponse<bool>> DeleteCardAsync(int dashboardId, string cardId);

    /// <summary>
    /// 移动卡片到新位置
    /// </summary>
    Task<ApiResponse<KanbanMoveCardResponseDto>> MoveCardAsync(int dashboardId, KanbanMoveCardDto dto);

    /// <summary>
    /// 获取看板配置
    /// </summary>
    Task<ApiResponse<KanbanBoardDto>> GetBoardAsync(int dashboardId);
}
