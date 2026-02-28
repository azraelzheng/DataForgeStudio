using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Domain.DTOs;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 看板服务实现
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        DataForgeStudioDbContext context,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取看板列表
    /// </summary>
    public async Task<ApiResponse<List<DashboardDto>>> GetDashboardsAsync()
    {
        try
        {
            var dashboards = await _context.Dashboards
                .Include(d => d.Widgets)
                .OrderByDescending(d => d.CreatedTime)
                .Select(d => new DashboardDto
                {
                    Id = d.DashboardGuid ?? d.DashboardId.ToString(),
                    Name = d.Name,
                    Description = d.Description,
                    Category = d.Category,
                    Layout = string.IsNullOrEmpty(d.LayoutConfig)
                        ? new DashboardLayoutDto()
                        : JsonConvert.DeserializeObject<DashboardLayoutDto>(d.LayoutConfig) ?? new(),
                    RefreshInterval = d.RefreshInterval,
                    IsPublished = d.IsPublished,
                    CreatedBy = d.CreatedBy,
                    CreatedTime = d.CreatedTime,
                    UpdatedTime = d.UpdatedTime,
                    Widgets = d.Widgets.Select(w => new DashboardWidgetDto
                    {
                        Id = w.WidgetGuid ?? w.WidgetId.ToString(),
                        Type = w.Type,
                        Name = w.Name,
                        Position = string.IsNullOrEmpty(w.Position)
                            ? new DashboardPositionDto()
                            : JsonConvert.DeserializeObject<DashboardPositionDto>(w.Position) ?? new(),
                        Config = string.IsNullOrEmpty(w.Config)
                            ? null
                            : JsonConvert.DeserializeObject<Dictionary<string, object>>(w.Config),
                        DataBinding = string.IsNullOrEmpty(w.DataBinding)
                            ? null
                            : JsonConvert.DeserializeObject<Dictionary<string, object>>(w.DataBinding),
                        DisplayOrder = w.DisplayOrder
                    }).ToList()
                })
                .ToListAsync();

            return ApiResponse<List<DashboardDto>>.Ok(dashboards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看板列表失败");
            return ApiResponse<List<DashboardDto>>.Fail("获取看板列表失败");
        }
    }

    /// <summary>
    /// 获取看板详情
    /// </summary>
    public async Task<ApiResponse<DashboardDto>> GetDashboardByIdAsync(string id)
    {
        try
        {
            var dashboard = await FindDashboardByIdAsync(id);

            if (dashboard == null)
            {
                return ApiResponse<DashboardDto>.Fail("看板不存在", "NOT_FOUND");
            }

            var dto = MapToDashboardDto(dashboard);
            return ApiResponse<DashboardDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看板失败: DashboardId={DashboardId}", id);
            return ApiResponse<DashboardDto>.Fail("获取看板失败");
        }
    }

    /// <summary>
    /// 创建看板
    /// </summary>
    public async Task<ApiResponse<DashboardDto>> CreateDashboardAsync(DashboardCreateDto dto, int userId)
    {
        try
        {
            var dashboard = new Dashboard
            {
                DashboardGuid = Guid.NewGuid().ToString("N"),
                Name = dto.Name,
                Description = dto.Description,
                Category = dto.Category,
                LayoutConfig = dto.Layout != null
                    ? JsonConvert.SerializeObject(dto.Layout)
                    : JsonConvert.SerializeObject(new DashboardLayoutDto()),
                RefreshInterval = dto.RefreshInterval,
                IsPublished = dto.IsPublished,
                CreatedBy = userId,
                CreatedTime = DateTime.UtcNow
            };

            _context.Dashboards.Add(dashboard);
            await _context.SaveChangesAsync();

            var result = MapToDashboardDto(dashboard);
            return ApiResponse<DashboardDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建看板失败");
            return ApiResponse<DashboardDto>.Fail("创建看板失败");
        }
    }

    /// <summary>
    /// 更新看板
    /// </summary>
    public async Task<ApiResponse<DashboardDto>> UpdateDashboardAsync(string id, DashboardUpdateDto dto, int userId)
    {
        try
        {
            var dashboard = await FindDashboardByIdAsync(id);

            if (dashboard == null)
            {
                return ApiResponse<DashboardDto>.Fail("看板不存在", "NOT_FOUND");
            }

            // 更新字段
            if (dto.Name != null) dashboard.Name = dto.Name;
            if (dto.Description != null) dashboard.Description = dto.Description;
            if (dto.Category != null) dashboard.Category = dto.Category;
            if (dto.Layout != null) dashboard.LayoutConfig = JsonConvert.SerializeObject(dto.Layout);
            if (dto.RefreshInterval.HasValue) dashboard.RefreshInterval = dto.RefreshInterval.Value;
            if (dto.IsPublished.HasValue) dashboard.IsPublished = dto.IsPublished.Value;

            dashboard.UpdatedBy = userId;
            dashboard.UpdatedTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = MapToDashboardDto(dashboard);
            return ApiResponse<DashboardDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新看板失败: DashboardId={DashboardId}", id);
            return ApiResponse<DashboardDto>.Fail("更新看板失败");
        }
    }

    /// <summary>
    /// 删除看板
    /// </summary>
    public async Task<ApiResponse<bool>> DeleteDashboardAsync(string id)
    {
        try
        {
            var dashboard = await FindDashboardByIdAsync(id);

            if (dashboard == null)
            {
                return ApiResponse<bool>.Fail("看板不存在", "NOT_FOUND");
            }

            _context.Dashboards.Remove(dashboard);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除看板失败: DashboardId={DashboardId}", id);
            return ApiResponse<bool>.Fail("删除看板失败");
        }
    }

    /// <summary>
    /// 添加组件到看板
    /// </summary>
    public async Task<ApiResponse<DashboardWidgetDto>> AddWidgetAsync(string dashboardId, DashboardWidgetDto dto, int userId)
    {
        try
        {
            var dashboard = await FindDashboardByIdAsync(dashboardId);

            if (dashboard == null)
            {
                return ApiResponse<DashboardWidgetDto>.Fail("看板不存在", "NOT_FOUND");
            }

            var widget = new DashboardWidget
            {
                WidgetGuid = Guid.NewGuid().ToString("N"),
                DashboardId = dashboard.DashboardId,
                Type = dto.Type,
                Name = dto.Name,
                Position = JsonConvert.SerializeObject(dto.Position),
                Config = dto.Config != null
                    ? JsonConvert.SerializeObject(dto.Config)
                    : null,
                DataBinding = dto.DataBinding != null
                    ? JsonConvert.SerializeObject(dto.DataBinding)
                    : null,
                DisplayOrder = dto.DisplayOrder,
                CreatedTime = DateTime.UtcNow
            };

            _context.DashboardWidgets.Add(widget);
            await _context.SaveChangesAsync();

            var result = MapToWidgetDto(widget);
            return ApiResponse<DashboardWidgetDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加组件失败: DashboardId={DashboardId}", dashboardId);
            return ApiResponse<DashboardWidgetDto>.Fail("添加组件失败");
        }
    }

    /// <summary>
    /// 更新看板组件
    /// </summary>
    public async Task<ApiResponse<DashboardWidgetDto>> UpdateWidgetAsync(string dashboardId, string widgetId, DashboardWidgetDto dto, int userId)
    {
        try
        {
            var widget = await FindWidgetByGuidAsync(widgetId);

            if (widget == null)
            {
                return ApiResponse<DashboardWidgetDto>.Fail("组件不存在", "NOT_FOUND");
            }

            // 更新字段
            if (dto.Type != null) widget.Type = dto.Type;
            if (dto.Name != null) widget.Name = dto.Name;
            if (dto.Position != null) widget.Position = JsonConvert.SerializeObject(dto.Position);
            if (dto.Config != null) widget.Config = JsonConvert.SerializeObject(dto.Config);
            if (dto.DataBinding != null) widget.DataBinding = JsonConvert.SerializeObject(dto.DataBinding);
            if (dto.DisplayOrder > 0) widget.DisplayOrder = dto.DisplayOrder;

            widget.UpdatedTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = MapToWidgetDto(widget);
            return ApiResponse<DashboardWidgetDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新组件失败: WidgetId={WidgetId}", widgetId);
            return ApiResponse<DashboardWidgetDto>.Fail("更新组件失败");
        }
    }

    /// <summary>
    /// 删除看板组件
    /// </summary>
    public async Task<ApiResponse<bool>> DeleteWidgetAsync(string dashboardId, string widgetId)
    {
        try
        {
            var widget = await FindWidgetByGuidAsync(widgetId);

            if (widget == null)
            {
                return ApiResponse<bool>.Fail("组件不存在", "NOT_FOUND");
            }

            _context.DashboardWidgets.Remove(widget);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除组件失败: WidgetId={WidgetId}", widgetId);
            return ApiResponse<bool>.Fail("删除组件失败");
        }
    }

    /// <summary>
    /// 查找看板（通过 ID 或 GUID）
    /// </summary>
    private async Task<Dashboard?> FindDashboardByIdAsync(string id)
    {
        // 尝试按 GUID 查找
        var dashboard = await _context.Dashboards
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.DashboardGuid == id);

        // 如果没找到，尝试按 ID 查找
        if (dashboard == null && int.TryParse(id, out var dashboardId))
        {
            dashboard = await _context.Dashboards
                .Include(d => d.Widgets)
                .FirstOrDefaultAsync(d => d.DashboardId == dashboardId);
        }

        return dashboard;
    }

    /// <summary>
    /// 查找组件（通过 GUID）
    /// </summary>
    private async Task<DashboardWidget?> FindWidgetByGuidAsync(string guid)
    {
        return await _context.DashboardWidgets
            .FirstOrDefaultAsync(w => w.WidgetGuid == guid);
    }

    /// <summary>
    /// 映射实体到 DTO
    /// </summary>
    private DashboardDto MapToDashboardDto(Dashboard dashboard)
    {
        return new DashboardDto
        {
            Id = dashboard.DashboardGuid ?? dashboard.DashboardId.ToString(),
            Name = dashboard.Name,
            Description = dashboard.Description,
            Category = dashboard.Category,
            Layout = string.IsNullOrEmpty(dashboard.LayoutConfig)
                ? new DashboardLayoutDto()
                : JsonConvert.DeserializeObject<DashboardLayoutDto>(dashboard.LayoutConfig) ?? new(),
            RefreshInterval = dashboard.RefreshInterval,
            IsPublished = dashboard.IsPublished,
            CreatedBy = dashboard.CreatedBy,
            CreatedTime = dashboard.CreatedTime,
            UpdatedTime = dashboard.UpdatedTime,
            Widgets = dashboard.Widgets.Select(w => MapToWidgetDto(w)).ToList()
        };
    }

    /// <summary>
    /// 映射组件实体到 DTO
    /// </summary>
    private DashboardWidgetDto MapToWidgetDto(DashboardWidget widget)
    {
        return new DashboardWidgetDto
        {
            Id = widget.WidgetGuid ?? widget.WidgetId.ToString(),
            Type = widget.Type,
            Name = widget.Name,
            Position = string.IsNullOrEmpty(widget.Position)
                ? new DashboardPositionDto()
                : JsonConvert.DeserializeObject<DashboardPositionDto>(widget.Position) ?? new(),
            Config = string.IsNullOrEmpty(widget.Config)
                ? null
                : JsonConvert.DeserializeObject<Dictionary<string, object>>(widget.Config),
            DataBinding = string.IsNullOrEmpty(widget.DataBinding)
                ? null
                : JsonConvert.DeserializeObject<Dictionary<string, object>>(widget.DataBinding),
            DisplayOrder = widget.DisplayOrder,
            DashboardId = widget.DashboardId.ToString()
        };
    }
}
