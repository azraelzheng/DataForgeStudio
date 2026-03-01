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
/// 车间大屏服务实现
/// </summary>
public class DisplayService : IDisplayService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly ILogger<DisplayService> _logger;

    public DisplayService(
        DataForgeStudioDbContext context,
        ILogger<DisplayService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取大屏配置列表
    /// </summary>
    public async Task<ApiResponse<List<DisplayConfigDto>>> GetDisplayConfigsAsync()
    {
        try
        {
            var configs = await _context.DisplayConfigs
                .OrderByDescending(c => c.CreatedTime)
                .Select(c => new DisplayConfigDto
                {
                    Id = c.ConfigGuid ?? c.ConfigId.ToString(),
                    Name = c.Name,
                    Description = c.Description,
                    DashboardIds = string.IsNullOrEmpty(c.DashboardIds)
                        ? new List<string>()
                        : JsonConvert.DeserializeObject<List<string>>(c.DashboardIds) ?? new(),
                    Interval = c.Interval,
                    AutoRefresh = c.AutoRefresh,
                    Transition = c.Transition,
                    ShowClock = c.ShowClock,
                    ShowDashboardName = c.ShowDashboardName,
                    Loop = c.Loop,
                    PauseOnHover = c.PauseOnHover,
                    CreatedBy = c.CreatedBy,
                    CreatedTime = c.CreatedTime,
                    UpdatedTime = c.UpdatedTime
                })
                .ToListAsync();

            return ApiResponse<List<DisplayConfigDto>>.Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取大屏配置列表失败");
            return ApiResponse<List<DisplayConfigDto>>.Fail("获取配置列表失败");
        }
    }

    /// <summary>
    /// 获取大屏配置详情
    /// </summary>
    public async Task<ApiResponse<DisplayConfigDto>> GetDisplayConfigByIdAsync(string id)
    {
        try
        {
            var config = await FindConfigByIdAsync(id);

            if (config == null)
            {
                return ApiResponse<DisplayConfigDto>.Fail("配置不存在", "NOT_FOUND");
            }

            var dto = MapToDisplayConfigDto(config);
            return ApiResponse<DisplayConfigDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取大屏配置失败: ConfigId={ConfigId}", id);
            return ApiResponse<DisplayConfigDto>.Fail("获取配置失败");
        }
    }

    /// <summary>
    /// 创建大屏配置
    /// </summary>
    public async Task<ApiResponse<DisplayConfigDto>> CreateDisplayConfigAsync(DisplayConfigCreateDto dto, int userId)
    {
        try
        {
            // 验证看板是否存在
            foreach (var dashboardId in dto.DashboardIds)
            {
                var dashboard = await FindDashboardByGuidAsync(dashboardId);
                if (dashboard == null)
                {
                    return ApiResponse<DisplayConfigDto>.Fail($"看板 {dashboardId} 不存在", "INVALID_DASHBOARD");
                }
            }

            var config = new DisplayConfig
            {
                ConfigGuid = Guid.NewGuid().ToString("N"),
                Name = dto.Name,
                Description = dto.Description,
                DashboardIds = JsonConvert.SerializeObject(dto.DashboardIds),
                Interval = dto.Interval,
                AutoRefresh = dto.AutoRefresh,
                Transition = dto.Transition,
                ShowClock = dto.ShowClock,
                ShowDashboardName = dto.ShowDashboardName,
                Loop = dto.Loop,
                PauseOnHover = dto.PauseOnHover,
                CreatedBy = userId,
                CreatedTime = DateTime.UtcNow
            };

            _context.DisplayConfigs.Add(config);
            await _context.SaveChangesAsync();

            var result = MapToDisplayConfigDto(config);
            return ApiResponse<DisplayConfigDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建大屏配置失败");
            return ApiResponse<DisplayConfigDto>.Fail("创建配置失败");
        }
    }

    /// <summary>
    /// 更新大屏配置
    /// </summary>
    public async Task<ApiResponse<DisplayConfigDto>> UpdateDisplayConfigAsync(string id, DisplayConfigUpdateDto dto, int userId)
    {
        try
        {
            var config = await FindConfigByIdAsync(id);

            if (config == null)
            {
                return ApiResponse<DisplayConfigDto>.Fail("配置不存在", "NOT_FOUND");
            }

            // 如果更新了看板列表，验证看板是否存在
            if (dto.DashboardIds != null)
            {
                foreach (var dashboardId in dto.DashboardIds)
                {
                    var dashboard = await FindDashboardByGuidAsync(dashboardId);
                    if (dashboard == null)
                    {
                        return ApiResponse<DisplayConfigDto>.Fail($"看板 {dashboardId} 不存在", "INVALID_DASHBOARD");
                    }
                }
            }

            // 更新字段
            if (dto.Name != null) config.Name = dto.Name;
            if (dto.Description != null) config.Description = dto.Description;
            if (dto.DashboardIds != null) config.DashboardIds = JsonConvert.SerializeObject(dto.DashboardIds);
            if (dto.Interval.HasValue) config.Interval = dto.Interval.Value;
            if (dto.AutoRefresh.HasValue) config.AutoRefresh = dto.AutoRefresh.Value;
            if (dto.Transition != null) config.Transition = dto.Transition;
            if (dto.ShowClock.HasValue) config.ShowClock = dto.ShowClock.Value;
            if (dto.ShowDashboardName.HasValue) config.ShowDashboardName = dto.ShowDashboardName.Value;
            if (dto.Loop.HasValue) config.Loop = dto.Loop.Value;
            if (dto.PauseOnHover.HasValue) config.PauseOnHover = dto.PauseOnHover.Value;

            config.UpdatedBy = userId;
            config.UpdatedTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = MapToDisplayConfigDto(config);
            return ApiResponse<DisplayConfigDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新大屏配置失败: ConfigId={ConfigId}", id);
            return ApiResponse<DisplayConfigDto>.Fail("更新配置失败");
        }
    }

    /// <summary>
    /// 删除大屏配置
    /// </summary>
    public async Task<ApiResponse<bool>> DeleteDisplayConfigAsync(string id)
    {
        try
        {
            var config = await FindConfigByIdAsync(id);

            if (config == null)
            {
                return ApiResponse<bool>.Fail("配置不存在", "NOT_FOUND");
            }

            _context.DisplayConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除大屏配置失败: ConfigId={ConfigId}", id);
            return ApiResponse<bool>.Fail("删除配置失败");
        }
    }

    /// <summary>
    /// 获取大屏聚合数据
    /// </summary>
    public async Task<ApiResponse<DisplayDataResponseDto>> GetDisplayDataAsync(string id)
    {
        try
        {
            var config = await FindConfigByIdAsync(id);

            if (config == null)
            {
                return ApiResponse<DisplayDataResponseDto>.Fail("配置不存在", "NOT_FOUND");
            }

            var dashboardIds = JsonConvert.DeserializeObject<List<string>>(config.DashboardIds)
                ?? new List<string>();

            var dashboards = new Dictionary<string, DisplayDashboardDataDto>();

            foreach (var dashboardId in dashboardIds)
            {
                var dashboard = await FindDashboardByGuidAsync(dashboardId);
                if (dashboard != null)
                {
                    // 获取看板数据（这里可以调用 DashboardService 或直接查询）
                    var data = await GetDashboardDataAsync(dashboard);

                    dashboards[dashboardId] = new DisplayDashboardDataDto
                    {
                        Id = dashboardId,
                        Name = dashboard.Name,
                        Data = data
                    };
                }
            }

            var response = new DisplayDataResponseDto
            {
                Dashboards = dashboards,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            return ApiResponse<DisplayDataResponseDto>.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取大屏数据失败: ConfigId={ConfigId}", id);
            return ApiResponse<DisplayDataResponseDto>.Fail("获取数据失败");
        }
    }

    /// <summary>
    /// 查找配置（通过 ID 或 GUID）
    /// </summary>
    private async Task<DisplayConfig?> FindConfigByIdAsync(string id)
    {
        // 尝试按 GUID 查找
        var config = await _context.DisplayConfigs
            .FirstOrDefaultAsync(c => c.ConfigGuid == id);

        // 如果没找到，尝试按 ID 查找
        if (config == null && int.TryParse(id, out var configId))
        {
            config = await _context.DisplayConfigs
                .FirstOrDefaultAsync(c => c.ConfigId == configId);
        }

        return config;
    }

    /// <summary>
    /// 查找看板（通过 GUID）
    /// </summary>
    private async Task<Dashboard?> FindDashboardByGuidAsync(string guid)
    {
        return await _context.Dashboards
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.DashboardGuid == guid);
    }

    /// <summary>
    /// 获取看板数据
    /// </summary>
    private async Task<object?> GetDashboardDataAsync(Dashboard dashboard)
    {
        // 这里可以根据需要获取看板的实际数据
        // 简化版本：返回看板的基本信息和组件列表
        return new
        {
            id = dashboard.DashboardGuid ?? dashboard.DashboardId.ToString(),
            name = dashboard.Name,
            description = dashboard.Description,
            layout = string.IsNullOrEmpty(dashboard.LayoutConfig)
                ? null
                : JsonConvert.DeserializeObject<object>(dashboard.LayoutConfig),
            widgets = dashboard.Widgets.Select(w => new
            {
                id = w.WidgetGuid ?? w.WidgetId.ToString(),
                type = w.Type,
                name = w.Name,
                position = string.IsNullOrEmpty(w.Position)
                    ? null
                    : JsonConvert.DeserializeObject<object>(w.Position),
                config = string.IsNullOrEmpty(w.Config)
                    ? null
                    : JsonConvert.DeserializeObject<object>(w.Config),
                dataBinding = string.IsNullOrEmpty(w.DataBinding)
                    ? null
                    : JsonConvert.DeserializeObject<object>(w.DataBinding)
            }).ToList()
        };
    }

    /// <summary>
    /// 映射实体到 DTO
    /// </summary>
    private DisplayConfigDto MapToDisplayConfigDto(DisplayConfig config)
    {
        return new DisplayConfigDto
        {
            Id = config.ConfigGuid ?? config.ConfigId.ToString(),
            Name = config.Name,
            Description = config.Description,
            DashboardIds = string.IsNullOrEmpty(config.DashboardIds)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(config.DashboardIds) ?? new(),
            Interval = config.Interval,
            AutoRefresh = config.AutoRefresh,
            Transition = config.Transition,
            ShowClock = config.ShowClock,
            ShowDashboardName = config.ShowDashboardName,
            Loop = config.Loop,
            PauseOnHover = config.PauseOnHover,
            CreatedBy = config.CreatedBy,
            CreatedTime = config.CreatedTime,
            UpdatedTime = config.UpdatedTime
        };
    }
}
