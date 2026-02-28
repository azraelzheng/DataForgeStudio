using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Domain.DTOs;
using DataForgeStudio.Shared.DTO;
using DataForgeStudio.Shared.Utils;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 看板服务实现
/// </summary>
public class KanbanService : IKanbanService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly ILogger<KanbanService> _logger;

    public KanbanService(
        DataForgeStudioDbContext context,
        ILogger<KanbanService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取看板的所有卡片
    /// </summary>
    public async Task<ApiResponse<List<KanbanCardDto>>> GetCardsAsync(int dashboardId)
    {
        try
        {
            // 验证看板是否存在
            var board = await _context.KanbanBoards
                .FirstOrDefaultAsync(b => b.BoardId == dashboardId);

            if (board == null)
            {
                return ApiResponse<List<KanbanCardDto>>.Fail("看板不存在", "NOT_FOUND");
            }

            var cards = await _context.KanbanCards
                .Where(c => c.BoardId == dashboardId)
                .OrderBy(c => c.Status)
                .ThenBy(c => c.SortOrder)
                .Select(c => new KanbanCardDto
                {
                    Id = c.CardId.ToString(),
                    Title = c.Title,
                    Description = c.Description,
                    Status = c.Status,
                    Priority = c.Priority,
                    AssigneeId = c.AssigneeId,
                    AssigneeName = c.AssigneeName,
                    AssigneeAvatar = c.AssigneeAvatar,
                    DueDate = c.DueDate,
                    Tags = string.IsNullOrEmpty(c.Tags) ? null : JsonConvert.DeserializeObject<List<string>>(c.Tags),
                    CustomFields = string.IsNullOrEmpty(c.CustomFields) ? null : JsonConvert.DeserializeObject<Dictionary<string, object>>(c.CustomFields),
                    Order = c.SortOrder,
                    AttachmentCount = c.AttachmentCount,
                    CommentCount = c.CommentCount,
                    CreatedBy = c.CreatedBy,
                    CreatedTime = c.CreatedTime,
                    UpdatedTime = c.UpdatedTime
                })
                .ToListAsync();

            return ApiResponse<List<KanbanCardDto>>.Ok(cards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看板卡片失败: DashboardId={DashboardId}", dashboardId);
            return ApiResponse<List<KanbanCardDto>>.Fail("获取卡片失败");
        }
    }

    /// <summary>
    /// 获取单个卡片详情
    /// </summary>
    public async Task<ApiResponse<KanbanCardDto>> GetCardByIdAsync(int dashboardId, string cardId)
    {
        try
        {
            var card = await _context.KanbanCards
                .Include(c => c.Activities.OrderByDescending(a => a.CreatedTime).Take(10))
                .FirstOrDefaultAsync(c => c.BoardId == dashboardId && c.CardId.ToString() == cardId);

            if (card == null)
            {
                return ApiResponse<KanbanCardDto>.Fail("卡片不存在", "NOT_FOUND");
            }

            var dto = MapToDto(card);
            return ApiResponse<KanbanCardDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取卡片详情失败: CardId={CardId}", cardId);
            return ApiResponse<KanbanCardDto>.Fail("获取卡片详情失败");
        }
    }

    /// <summary>
    /// 创建新卡片
    /// </summary>
    public async Task<ApiResponse<KanbanCardDto>> CreateCardAsync(int dashboardId, KanbanCardCreateDto dto)
    {
        try
        {
            // 验证看板是否存在
            var board = await _context.KanbanBoards
                .FirstOrDefaultAsync(b => b.BoardId == dashboardId);

            if (board == null)
            {
                return ApiResponse<KanbanCardDto>.Fail("看板不存在", "NOT_FOUND");
            }

            // 获取当前状态下的最大排序号
            var maxOrder = await _context.KanbanCards
                .Where(c => c.BoardId == dashboardId && c.Status == dto.Status)
                .OrderByDescending(c => c.SortOrder)
                .Select(c => c.SortOrder)
                .FirstOrDefaultAsync();

            var card = new KanbanCard
            {
                BoardId = dashboardId,
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status,
                Priority = dto.Priority,
                AssigneeId = dto.AssigneeId,
                DueDate = dto.DueDate,
                Tags = dto.Tags != null ? JsonConvert.SerializeObject(dto.Tags) : null,
                CustomFields = dto.CustomFields != null ? JsonConvert.SerializeObject(dto.CustomFields) : null,
                SortOrder = maxOrder + 1,
                CreatedTime = DateTime.UtcNow
            };

            _context.KanbanCards.Add(card);
            await _context.SaveChangesAsync();

            // 记录活动
            await AddActivityAsync(card.CardId, "created", "创建了此卡片");

            var result = MapToDto(card);
            return ApiResponse<KanbanCardDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建卡片失败: DashboardId={DashboardId}", dashboardId);
            return ApiResponse<KanbanCardDto>.Fail("创建卡片失败");
        }
    }

    /// <summary>
    /// 更新卡片
    /// </summary>
    public async Task<ApiResponse<bool>> UpdateCardAsync(int dashboardId, string cardId, KanbanCardCreateDto dto)
    {
        try
        {
            var card = await _context.KanbanCards
                .FirstOrDefaultAsync(c => c.BoardId == dashboardId && c.CardId.ToString() == cardId);

            if (card == null)
            {
                return ApiResponse<bool>.Fail("卡片不存在", "NOT_FOUND");
            }

            // 记录变更
            var changes = new List<string>();
            if (card.Status != dto.Status) changes.Add($"状态: {card.Status} -> {dto.Status}");
            if (card.Priority != dto.Priority) changes.Add($"优先级: {card.Priority} -> {dto.Priority}");
            if (card.AssigneeId != dto.AssigneeId) changes.Add("负责人已变更");
            if (card.Title != dto.Title) changes.Add("标题已变更");

            card.Title = dto.Title;
            card.Description = dto.Description;
            card.Status = dto.Status;
            card.Priority = dto.Priority;
            card.AssigneeId = dto.AssigneeId;
            card.DueDate = dto.DueDate;
            card.Tags = dto.Tags != null ? JsonConvert.SerializeObject(dto.Tags) : null;
            card.CustomFields = dto.CustomFields != null ? JsonConvert.SerializeObject(dto.CustomFields) : null;
            card.UpdatedTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 记录活动
            if (changes.Count > 0)
            {
                await AddActivityAsync(card.CardId, "updated", string.Join("; ", changes));
            }

            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新卡片失败: CardId={CardId}", cardId);
            return ApiResponse<bool>.Fail("更新卡片失败");
        }
    }

    /// <summary>
    /// 删除卡片
    /// </summary>
    public async Task<ApiResponse<bool>> DeleteCardAsync(int dashboardId, string cardId)
    {
        try
        {
            var card = await _context.KanbanCards
                .FirstOrDefaultAsync(c => c.BoardId == dashboardId && c.CardId.ToString() == cardId);

            if (card == null)
            {
                return ApiResponse<bool>.Fail("卡片不存在", "NOT_FOUND");
            }

            _context.KanbanCards.Remove(card);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除卡片失败: CardId={CardId}", cardId);
            return ApiResponse<bool>.Fail("删除卡片失败");
        }
    }

    /// <summary>
    /// 移动卡片到新位置
    /// </summary>
    public async Task<ApiResponse<KanbanMoveCardResponseDto>> MoveCardAsync(int dashboardId, KanbanMoveCardDto dto)
    {
        try
        {
            var card = await _context.KanbanCards
                .FirstOrDefaultAsync(c => c.BoardId == dashboardId && c.CardId.ToString() == dto.CardId);

            if (card == null)
            {
                return ApiResponse<KanbanMoveCardResponseDto>.Fail("卡片不存在", "NOT_FOUND");
            }

            var oldStatus = card.Status;
            var oldOrder = card.SortOrder;

            // 更新卡片状态和顺序
            card.Status = dto.ToStatus;
            card.SortOrder = dto.NewOrder;
            card.UpdatedTime = DateTime.UtcNow;

            // 重新排序目标列中的其他卡片
            var targetColumnCards = await _context.KanbanCards
                .Where(c => c.BoardId == dashboardId && c.Status == dto.ToStatus && c.CardId.ToString() != dto.CardId)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            var affectedCardIds = new List<string>();
            var order = 0;

            foreach (var c in targetColumnCards)
            {
                if (order == dto.NewOrder)
                {
                    order++; // 为移动的卡片腾出位置
                }
                c.SortOrder = order;
                affectedCardIds.Add(c.CardId.ToString());
                order++;
            }

            await _context.SaveChangesAsync();

            // 记录活动
            if (oldStatus != dto.ToStatus)
            {
                await AddActivityAsync(card.CardId, "moved", $"从 {oldStatus} 移动到 {dto.ToStatus}");
            }

            var response = new KanbanMoveCardResponseDto
            {
                Success = true,
                NewOrder = dto.NewOrder,
                AffectedCardIds = affectedCardIds
            };

            return ApiResponse<KanbanMoveCardResponseDto>.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移动卡片失败: CardId={CardId}", dto.CardId);
            return ApiResponse<KanbanMoveCardResponseDto>.Fail("移动卡片失败");
        }
    }

    /// <summary>
    /// 获取看板配置
    /// </summary>
    public async Task<ApiResponse<KanbanBoardDto>> GetBoardAsync(int dashboardId)
    {
        try
        {
            var board = await _context.KanbanBoards
                .FirstOrDefaultAsync(b => b.BoardId == dashboardId);

            if (board == null)
            {
                return ApiResponse<KanbanBoardDto>.Fail("看板不存在", "NOT_FOUND");
            }

            var columns = string.IsNullOrEmpty(board.ColumnsConfig)
                ? new List<KanbanColumnDto>()
                : JsonConvert.DeserializeObject<List<KanbanColumnDto>>(board.ColumnsConfig);

            var dto = new KanbanBoardDto
            {
                Id = board.BoardId,
                Name = board.BoardName,
                Description = board.Description,
                Columns = columns,
                EnableSwimLanes = board.EnableSwimLanes,
                SwimLaneBy = board.SwimLaneBy,
                CustomSwimLaneField = board.CustomSwimLaneField,
                IsPublished = board.IsPublished
            };

            return ApiResponse<KanbanBoardDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看板配置失败: DashboardId={DashboardId}", dashboardId);
            return ApiResponse<KanbanBoardDto>.Fail("获取看板配置失败");
        }
    }

    /// <summary>
    /// 添加活动记录
    /// </summary>
    private async Task AddActivityAsync(int cardId, string activityType, string? description = null)
    {
        try
        {
            var activity = new KanbanActivity
            {
                CardId = cardId,
                ActivityType = activityType,
                Description = description,
                CreatedTime = DateTime.UtcNow
            };

            _context.KanbanActivities.Add(activity);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "记录活动失败: CardId={CardId}", cardId);
        }
    }

    /// <summary>
    /// 映射实体到 DTO
    /// </summary>
    private KanbanCardDto MapToDto(KanbanCard card)
    {
        return new KanbanCardDto
        {
            Id = card.CardId.ToString(),
            Title = card.Title,
            Description = card.Description,
            Status = card.Status,
            Priority = card.Priority,
            AssigneeId = card.AssigneeId,
            AssigneeName = card.AssigneeName,
            AssigneeAvatar = card.AssigneeAvatar,
            DueDate = card.DueDate,
            Tags = string.IsNullOrEmpty(card.Tags) ? null : JsonConvert.DeserializeObject<List<string>>(card.Tags),
            CustomFields = string.IsNullOrEmpty(card.CustomFields) ? null : JsonConvert.DeserializeObject<Dictionary<string, object>>(card.CustomFields),
            Order = card.SortOrder,
            AttachmentCount = card.AttachmentCount,
            CommentCount = card.CommentCount,
            CreatedBy = card.CreatedBy,
            CreatedTime = card.CreatedTime,
            UpdatedTime = card.UpdatedTime
        };
    }
}
