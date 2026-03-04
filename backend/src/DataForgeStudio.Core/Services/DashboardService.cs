using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 大屏服务实现
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly IReportService _reportService;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        DataForgeStudioDbContext context,
        IReportService reportService,
        ILicenseService licenseService,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _reportService = reportService;
        _licenseService = licenseService;
        _logger = logger;
    }

    #region 大屏 CRUD

    /// <summary>
    /// 获取大屏分页列表
    /// 注意：此分页方法使用 OFFSET/FETCH 语法，需要 SQL Server 2012+
    /// 如果需要支持 SQL Server 2005，请使用原始 SQL 查询配合 ROW_NUMBER()
    /// </summary>
    public async Task<ApiResponse<PagedResponse<DashboardDto>>> GetDashboardsAsync(PagedRequest request, string? name = null)
    {
        var query = _context.Dashboards
            .Include(d => d.Creator)
            .AsQueryable();

        // 名称过滤
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(d => d.Name.Contains(name));
        }

        var totalCount = await query.CountAsync();

        var dashboards = await query
            .OrderByDescending(d => d.CreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DashboardDto
            {
                DashboardId = d.DashboardId,
                Name = d.Name,
                Description = d.Description,
                Theme = d.Theme,
                RefreshInterval = d.RefreshInterval,
                IsPublic = d.IsPublic,
                Status = d.Status,
                PublicUrl = d.PublicUrl,
                Width = d.Width,
                Height = d.Height,
                BackgroundColor = d.BackgroundColor,
                BackgroundImage = d.BackgroundImage,
                CreatedTime = d.CreatedTime,
                UpdatedTime = d.UpdatedTime,
                CreatorName = d.Creator != null ? d.Creator.Username : null,
                WidgetCount = d.Widgets.Count
            })
            .ToListAsync();

        var pagedResponse = new PagedResponse<DashboardDto>(dashboards, totalCount, request.PageIndex, request.PageSize);
        return ApiResponse<PagedResponse<DashboardDto>>.Ok(pagedResponse);
    }

    /// <summary>
    /// 获取大屏详情
    /// </summary>
    public async Task<ApiResponse<DashboardDetailDto>> GetDashboardByIdAsync(int dashboardId)
    {
        var dashboard = await _context.Dashboards
            .Include(d => d.Creator)
            .Include(d => d.Widgets)
                .ThenInclude(w => w.Report)
            .Include(d => d.Widgets)
                .ThenInclude(w => w.Rules)
            .Where(d => d.DashboardId == dashboardId)
            .FirstOrDefaultAsync();

        if (dashboard == null)
        {
            return ApiResponse<DashboardDetailDto>.Fail("大屏不存在", "NOT_FOUND");
        }

        var dashboardDetail = MapToDetailDto(dashboard);
        return ApiResponse<DashboardDetailDto>.Ok(dashboardDetail);
    }

    /// <summary>
    /// 创建大屏
    /// </summary>
    public async Task<ApiResponse<DashboardDto>> CreateDashboardAsync(CreateDashboardRequest request, int createdBy)
    {
        // 检查许可证限制
        var limitCheck = await _licenseService.CheckDashboardLimitAsync();
        if (!limitCheck.Success)
        {
            return ApiResponse<DashboardDto>.Fail(
                limitCheck.Message,
                limitCheck.ErrorCode ?? "DASHBOARD_LIMIT_EXCEEDED"
            );
        }

        var dashboard = new Dashboard
        {
            Name = request.Name,
            Description = request.Description,
            Theme = request.Theme,
            RefreshInterval = request.RefreshInterval,
            IsPublic = request.IsPublic,
            LayoutConfig = request.LayoutConfig,
            ThemeConfig = request.ThemeConfig,
            Width = request.Width,
            Height = request.Height,
            BackgroundColor = request.BackgroundColor,
            BackgroundImage = request.BackgroundImage,
            CreatedBy = createdBy,
            CreatedTime = DateTime.UtcNow
        };

        _context.Dashboards.Add(dashboard);
        await _context.SaveChangesAsync();

        // 重新查询以获取创建人信息
        var createdDashboard = await _context.Dashboards
            .Include(d => d.Creator)
            .FirstOrDefaultAsync(d => d.DashboardId == dashboard.DashboardId);

        var dashboardDto = new DashboardDto
        {
            DashboardId = dashboard.DashboardId,
            Name = dashboard.Name,
            Description = dashboard.Description,
            Theme = dashboard.Theme,
            RefreshInterval = dashboard.RefreshInterval,
            IsPublic = dashboard.IsPublic,
            Status = dashboard.Status,
            PublicUrl = dashboard.PublicUrl,
            Width = dashboard.Width,
            Height = dashboard.Height,
            BackgroundColor = dashboard.BackgroundColor,
            BackgroundImage = dashboard.BackgroundImage,
            CreatedTime = dashboard.CreatedTime,
            CreatorName = createdDashboard?.Creator?.Username,
            WidgetCount = 0
        };

        return ApiResponse<DashboardDto>.Ok(dashboardDto, "大屏创建成功");
    }

    /// <summary>
    /// 更新大屏
    /// </summary>
    public async Task<ApiResponse> UpdateDashboardAsync(int dashboardId, CreateDashboardRequest request)
    {
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard == null)
        {
            return ApiResponse.Fail("大屏不存在", "NOT_FOUND");
        }

        dashboard.Name = request.Name;
        dashboard.Description = request.Description;
        dashboard.Theme = request.Theme;
        dashboard.RefreshInterval = request.RefreshInterval;
        dashboard.IsPublic = request.IsPublic;
        dashboard.LayoutConfig = request.LayoutConfig;
        dashboard.ThemeConfig = request.ThemeConfig;
        dashboard.Width = request.Width;
        dashboard.Height = request.Height;
        dashboard.BackgroundColor = request.BackgroundColor;
        dashboard.BackgroundImage = request.BackgroundImage;
        dashboard.UpdatedTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("大屏更新成功");
    }

    /// <summary>
    /// 删除大屏
    /// </summary>
    public async Task<ApiResponse> DeleteDashboardAsync(int dashboardId)
    {
        var dashboard = await _context.Dashboards
            .Include(d => d.Widgets)
                .ThenInclude(w => w.Rules)
            .FirstOrDefaultAsync(d => d.DashboardId == dashboardId);

        if (dashboard == null)
        {
            return ApiResponse.Fail("大屏不存在", "NOT_FOUND");
        }

        // 删除关联的规则和组件（级联删除）
        foreach (var widget in dashboard.Widgets)
        {
            _context.WidgetRules.RemoveRange(widget.Rules);
        }
        _context.DashboardWidgets.RemoveRange(dashboard.Widgets);
        _context.Dashboards.Remove(dashboard);

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("大屏删除成功");
    }

    #endregion

    #region 组件管理

    /// <summary>
    /// 添加组件
    /// </summary>
    public async Task<ApiResponse<DashboardWidgetDto>> AddWidgetAsync(int dashboardId, CreateWidgetRequest request)
    {
        // 验证大屏是否存在
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard == null)
        {
            return ApiResponse<DashboardWidgetDto>.Fail("大屏不存在", "NOT_FOUND");
        }

        // 验证报表是否存在（仅当 ReportId > 0 时验证）
        string? reportName = null;
        if (request.ReportId > 0)
        {
            var report = await _context.Reports.FindAsync(request.ReportId);
            if (report == null)
            {
                return ApiResponse<DashboardWidgetDto>.Fail("关联的报表不存在", "REPORT_NOT_FOUND");
            }
            reportName = report.ReportName;
        }

        var widget = new DashboardWidget
        {
            DashboardId = dashboardId,
            ReportId = request.ReportId,
            WidgetType = request.WidgetType,
            Title = request.Title,
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            Width = request.Width,
            Height = request.Height,
            DataConfig = request.DataConfig,
            StyleConfig = request.StyleConfig,
            CreatedTime = DateTime.UtcNow
        };

        _context.DashboardWidgets.Add(widget);
        await _context.SaveChangesAsync();

        // 更新大屏的更新时间
        dashboard.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var widgetDto = MapToWidgetDto(widget, reportName);
        return ApiResponse<DashboardWidgetDto>.Ok(widgetDto, "组件添加成功");
    }

    /// <summary>
    /// 更新组件
    /// </summary>
    public async Task<ApiResponse> UpdateWidgetAsync(int dashboardId, int widgetId, CreateWidgetRequest request)
    {
        var widget = await _context.DashboardWidgets
            .Include(w => w.Dashboard)
            .FirstOrDefaultAsync(w => w.WidgetId == widgetId && w.DashboardId == dashboardId);

        if (widget == null)
        {
            return ApiResponse.Fail("组件不存在", "NOT_FOUND");
        }

        // 验证报表是否存在（仅当 ReportId > 0 时验证）
        if (request.ReportId > 0)
        {
            var report = await _context.Reports.FindAsync(request.ReportId);
            if (report == null)
            {
                return ApiResponse.Fail("关联的报表不存在", "REPORT_NOT_FOUND");
            }
        }

        widget.ReportId = request.ReportId;
        widget.WidgetType = request.WidgetType;
        widget.Title = request.Title;
        widget.PositionX = request.PositionX;
        widget.PositionY = request.PositionY;
        widget.Width = request.Width;
        widget.Height = request.Height;
        widget.DataConfig = request.DataConfig;
        widget.StyleConfig = request.StyleConfig;

        // 更新大屏的更新时间
        widget.Dashboard.UpdatedTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("组件更新成功");
    }

    /// <summary>
    /// 删除组件
    /// </summary>
    public async Task<ApiResponse> DeleteWidgetAsync(int dashboardId, int widgetId)
    {
        var widget = await _context.DashboardWidgets
            .Include(w => w.Dashboard)
            .Include(w => w.Rules)
            .FirstOrDefaultAsync(w => w.WidgetId == widgetId && w.DashboardId == dashboardId);

        if (widget == null)
        {
            return ApiResponse.Fail("组件不存在", "NOT_FOUND");
        }

        // 删除关联规则
        _context.WidgetRules.RemoveRange(widget.Rules);
        _context.DashboardWidgets.Remove(widget);

        // 更新大屏的更新时间
        widget.Dashboard.UpdatedTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("组件删除成功");
    }

    /// <summary>
    /// 批量更新组件位置
    /// </summary>
    public async Task<ApiResponse> UpdateWidgetPositionsAsync(int dashboardId, List<WidgetPositionRequest> positions)
    {
        var dashboard = await _context.Dashboards
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.DashboardId == dashboardId);

        if (dashboard == null)
        {
            return ApiResponse.Fail("大屏不存在", "NOT_FOUND");
        }

        foreach (var position in positions)
        {
            var widget = dashboard.Widgets.FirstOrDefault(w => w.WidgetId == position.WidgetId);
            if (widget != null)
            {
                widget.PositionX = position.PositionX;
                widget.PositionY = position.PositionY;
                widget.Width = position.Width;
                widget.Height = position.Height;
            }
        }

        dashboard.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("组件位置更新成功");
    }

    #endregion

    #region 一键转换

    /// <summary>
    /// 从报表一键转换为大屏
    /// </summary>
    public async Task<ApiResponse<DashboardDetailDto>> ConvertFromReportAsync(int reportId, string? dashboardName, int createdBy)
    {
        // 获取报表详情
        var report = await _context.Reports
            .Include(r => r.Fields)
            .Include(r => r.Parameters)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);

        if (report == null)
        {
            return ApiResponse<DashboardDetailDto>.Fail("报表不存在", "REPORT_NOT_FOUND");
        }

        // 创建大屏
        var dashboard = new Dashboard
        {
            Name = dashboardName ?? $"{report.ReportName} - 大屏",
            Description = report.Description,
            Theme = "dark",
            RefreshInterval = 30,
            IsPublic = false,
            CreatedBy = createdBy,
            CreatedTime = DateTime.UtcNow
        };

        _context.Dashboards.Add(dashboard);
        await _context.SaveChangesAsync();

        // 智能生成组件布局
        var widgets = GenerateWidgetsFromReport(dashboard.DashboardId, report);
        _context.DashboardWidgets.AddRange(widgets);
        await _context.SaveChangesAsync();

        // 返回创建的大屏详情
        return await GetDashboardByIdAsync(dashboard.DashboardId);
    }

    /// <summary>
    /// 根据报表字段智能生成组件布局
    /// </summary>
    private List<DashboardWidget> GenerateWidgetsFromReport(int dashboardId, Report report)
    {
        var widgets = new List<DashboardWidget>();
        var fields = report.Fields.Where(f => f.IsVisible).OrderBy(f => f.SortOrder).ToList();

        if (fields.Count == 0)
        {
            return widgets;
        }

        // 根据字段数量和类型智能布局
        int currentX = 0;
        int currentY = 0;
        int gridColumns = 12; // 假设 12 列网格
        int rowHeight = 4; // 每行高度单位

        // 分析字段类型
        var numericFields = fields.Where(f => IsNumericType(f.DataType)).ToList();
        var dateFields = fields.Where(f => IsDateType(f.DataType)).ToList();
        var stringFields = fields.Where(f => IsStringType(f.DataType)).ToList();

        // 1. 如果有数值字段，创建统计卡片（最多 4 个）
        var statsToShow = numericFields.Take(4).ToList();
        int statsWidth = statsToShow.Count > 0 ? Math.Max(3, gridColumns / statsToShow.Count) : 0;

        foreach (var field in statsToShow)
        {
            widgets.Add(new DashboardWidget
            {
                DashboardId = dashboardId,
                ReportId = report.ReportId,
                WidgetType = "statistics",
                Title = field.DisplayName ?? field.FieldName,
                PositionX = currentX,
                PositionY = currentY,
                Width = statsWidth,
                Height = 3,
                DataConfig = System.Text.Json.JsonSerializer.Serialize(new
                {
                    field = field.FieldName,
                    aggregate = "sum"
                }),
                CreatedTime = DateTime.UtcNow
            });

            currentX += statsWidth;
        }

        // 换行
        if (statsToShow.Count > 0)
        {
            currentX = 0;
            currentY += 3;
        }

        // 2. 如果有日期字段和数值字段，创建折线图
        if (dateFields.Count > 0 && numericFields.Count > 0)
        {
            widgets.Add(new DashboardWidget
            {
                DashboardId = dashboardId,
                ReportId = report.ReportId,
                WidgetType = "chart",
                Title = "趋势图",
                PositionX = 0,
                PositionY = currentY,
                Width = gridColumns / 2,
                Height = rowHeight * 2,
                DataConfig = System.Text.Json.JsonSerializer.Serialize(new
                {
                    chartType = "line",
                    xAxis = dateFields[0].FieldName,
                    yAxis = numericFields.Take(2).Select(f => f.FieldName).ToList()
                }),
                CreatedTime = DateTime.UtcNow
            });

            currentX = gridColumns / 2;
        }

        // 3. 如果有多个数值字段，创建柱状图或饼图
        if (numericFields.Count > 1)
        {
            widgets.Add(new DashboardWidget
            {
                DashboardId = dashboardId,
                ReportId = report.ReportId,
                WidgetType = "chart",
                Title = "数据对比",
                PositionX = currentX,
                PositionY = currentY,
                Width = gridColumns / 2,
                Height = rowHeight * 2,
                DataConfig = System.Text.Json.JsonSerializer.Serialize(new
                {
                    chartType = "bar",
                    xAxis = stringFields.FirstOrDefault()?.FieldName ?? "Category",
                    yAxis = numericFields.Take(3).Select(f => f.FieldName).ToList()
                }),
                CreatedTime = DateTime.UtcNow
            });

            currentX = 0;
            currentY += rowHeight * 2;
        }

        // 4. 创建数据表格
        widgets.Add(new DashboardWidget
        {
            DashboardId = dashboardId,
            ReportId = report.ReportId,
            WidgetType = "table",
            Title = "数据明细",
            PositionX = 0,
            PositionY = currentY,
            Width = gridColumns,
            Height = rowHeight * 3,
            DataConfig = System.Text.Json.JsonSerializer.Serialize(new
            {
                columns = fields.Select(f => new
                {
                    field = f.FieldName,
                    title = f.DisplayName ?? f.FieldName,
                    width = f.Width > 0 ? f.Width : 100
                }).ToList()
            }),
            CreatedTime = DateTime.UtcNow
        });

        return widgets;
    }

    /// <summary>
    /// 判断是否为数值类型
    /// </summary>
    private bool IsNumericType(string? dataType)
    {
        if (string.IsNullOrEmpty(dataType)) return false;
        var type = dataType.ToLower();
        return type.Contains("int") || type.Contains("decimal") || type.Contains("double") ||
               type.Contains("float") || type.Contains("number") || type.Contains("numeric") ||
               type.Contains("money") || type.Contains("real");
    }

    /// <summary>
    /// 判断是否为日期类型
    /// </summary>
    private bool IsDateType(string? dataType)
    {
        if (string.IsNullOrEmpty(dataType)) return false;
        var type = dataType.ToLower();
        return type.Contains("date") || type.Contains("time");
    }

    /// <summary>
    /// 判断是否为字符串类型
    /// </summary>
    private bool IsStringType(string? dataType)
    {
        if (string.IsNullOrEmpty(dataType)) return true; // 默认为字符串
        var type = dataType.ToLower();
        return type.Contains("char") || type.Contains("text") || type.Contains("string") || type.Contains("nvarchar");
    }

    #endregion

    #region 数据获取

    /// <summary>
    /// 获取大屏数据（包含所有组件的数据）
    /// </summary>
    public async Task<ApiResponse<DashboardDataDto>> GetDashboardDataAsync(int dashboardId)
    {
        var dashboard = await _context.Dashboards
            .Include(d => d.Creator)
            .Include(d => d.Widgets)
                .ThenInclude(w => w.Report)
            .Include(d => d.Widgets)
                .ThenInclude(w => w.Rules)
            .FirstOrDefaultAsync(d => d.DashboardId == dashboardId);

        if (dashboard == null)
        {
            return ApiResponse<DashboardDataDto>.Fail("大屏不存在", "NOT_FOUND");
        }

        var result = new DashboardDataDto
        {
            Dashboard = new DashboardDto
            {
                DashboardId = dashboard.DashboardId,
                Name = dashboard.Name,
                Description = dashboard.Description,
                Theme = dashboard.Theme,
                RefreshInterval = dashboard.RefreshInterval,
                IsPublic = dashboard.IsPublic,
                Status = dashboard.Status,
                PublicUrl = dashboard.PublicUrl,
                Width = dashboard.Width,
                Height = dashboard.Height,
                BackgroundColor = dashboard.BackgroundColor,
                BackgroundImage = dashboard.BackgroundImage,
                CreatedTime = dashboard.CreatedTime,
                UpdatedTime = dashboard.UpdatedTime,
                CreatorName = dashboard.Creator?.Username,
                WidgetCount = dashboard.Widgets.Count
            },
            WidgetData = new Dictionary<int, WidgetDataResult>()
        };

        // 并行获取每个组件的数据
        var tasks = dashboard.Widgets.Select(async widget =>
        {
            var dataResult = await GetWidgetDataAsync(widget);
            return (widget.WidgetId, dataResult);
        });

        var results = await Task.WhenAll(tasks);
        foreach (var (widgetId, dataResult) in results)
        {
            result.WidgetData[widgetId] = dataResult;
        }

        return ApiResponse<DashboardDataDto>.Ok(result);
    }

    /// <summary>
    /// 获取单个组件的数据
    /// </summary>
    private async Task<WidgetDataResult> GetWidgetDataAsync(DashboardWidget widget)
    {
        try
        {
            // 解析查询条件值
            Dictionary<string, object> conditionValues = new();
            if (!string.IsNullOrEmpty(widget.DataConfig))
            {
                try
                {
                    var dataConfig = JsonSerializer.Deserialize<WidgetDataConfig>(widget.DataConfig, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (dataConfig?.QueryConditionValues != null)
                    {
                        conditionValues = dataConfig.QueryConditionValues;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "解析组件 DataConfig 失败: WidgetId={WidgetId}", widget.WidgetId);
                }
            }

            var executeRequest = new ExecuteReportRequest
            {
                Parameters = conditionValues
            };
            var reportResult = await _reportService.ExecuteReportAsync(widget.ReportId, executeRequest);

            return new WidgetDataResult
            {
                WidgetId = widget.WidgetId,
                Success = reportResult.Success,
                ErrorMessage = reportResult.Success ? null : reportResult.Message,
                Data = reportResult.Data?.Select(d => d.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value == null ? null : kvp.Value
                )).ToList(),
                FetchTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组件数据失败: WidgetId={WidgetId}, ReportId={ReportId}",
                widget.WidgetId, widget.ReportId);

            return new WidgetDataResult
            {
                WidgetId = widget.WidgetId,
                Success = false,
                ErrorMessage = $"获取数据失败: {ex.Message}",
                FetchTime = DateTime.UtcNow
            };
        }
    }

    #endregion

    #region 公开访问

    /// <summary>
    /// 获取公开大屏详情（无需登录）
    /// </summary>
    public async Task<ApiResponse<DashboardDetailDto>> GetPublicDashboardAsync(int dashboardId)
    {
        var dashboard = await _context.Dashboards
            .Include(d => d.Widgets)
                .ThenInclude(w => w.Report)
            .Include(d => d.Widgets)
                .ThenInclude(w => w.Rules)
            .FirstOrDefaultAsync(d => d.DashboardId == dashboardId && d.IsPublic);

        if (dashboard == null)
        {
            return ApiResponse<DashboardDetailDto>.Fail("大屏不存在或未公开", "NOT_FOUND");
        }

        var dashboardDetail = MapToDetailDto(dashboard);
        return ApiResponse<DashboardDetailDto>.Ok(dashboardDetail);
    }

    /// <summary>
    /// 获取公开大屏数据（无需登录）
    /// </summary>
    public async Task<ApiResponse<DashboardDataDto>> GetPublicDashboardDataAsync(int dashboardId)
    {
        // 首先验证是否为公开大屏
        var dashboard = await _context.Dashboards
            .FirstOrDefaultAsync(d => d.DashboardId == dashboardId && d.IsPublic);

        if (dashboard == null)
        {
            return ApiResponse<DashboardDataDto>.Fail("大屏不存在或未公开", "NOT_FOUND");
        }

        // 调用现有的数据获取逻辑
        return await GetDashboardDataAsync(dashboardId);
    }

    /// <summary>
    /// 发布大屏
    /// </summary>
    public async Task<ApiResponse<DashboardDto>> PublishDashboardAsync(int dashboardId, int userId)
    {
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard == null)
            return ApiResponse<DashboardDto>.Fail("大屏不存在", "NOT_FOUND");

        dashboard.Status = "published";
        dashboard.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // 返回更新后的大屏信息
        var updatedDashboard = await _context.Dashboards
            .Include(d => d.Creator)
            .FirstOrDefaultAsync(d => d.DashboardId == dashboardId);

        if (updatedDashboard == null)
            return ApiResponse<DashboardDto>.Fail("大屏数据获取失败", "FETCH_ERROR");
        return ApiResponse<DashboardDto>.Ok(MapToDto(updatedDashboard));
    }

    /// <summary>
    /// 取消发布大屏
    /// </summary>
    public async Task<ApiResponse<DashboardDto>> UnpublishDashboardAsync(int dashboardId, int userId)
    {
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard == null)
            return ApiResponse<DashboardDto>.Fail("大屏不存在", "NOT_FOUND");

        dashboard.Status = "draft";
        dashboard.IsPublic = false;
        dashboard.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // 返回更新后的大屏信息
        var updatedDashboard = await _context.Dashboards
            .Include(d => d.Creator)
            .FirstOrDefaultAsync(d => d.DashboardId == dashboardId);

        if (updatedDashboard == null)
            return ApiResponse<DashboardDto>.Fail("大屏数据获取失败", "FETCH_ERROR");
        return ApiResponse<DashboardDto>.Ok(MapToDto(updatedDashboard));
    }

    /// <summary>
    /// 更新大屏访问设置
    /// </summary>
    public async Task<ApiResponse<DashboardAccessDto>> UpdateDashboardAccessAsync(
        int dashboardId, UpdateDashboardAccessRequest request, int userId)
    {
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard == null)
            return ApiResponse<DashboardAccessDto>.Fail("大屏不存在", "NOT_FOUND");

        dashboard.IsPublic = request.IsPublic;

        // 如果设置为公开且没有 PublicUrl，生成一个
        if (request.IsPublic && string.IsNullOrEmpty(dashboard.PublicUrl))
        {
            dashboard.PublicUrl = Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        // 序列化授权用户ID列表
        dashboard.AuthorizedUserIds = request.AuthorizedUserIds != null
            ? JsonSerializer.Serialize(request.AuthorizedUserIds)
            : null;

        dashboard.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetDashboardAccessAsync(dashboardId);
    }

    /// <summary>
    /// 获取大屏访问设置
    /// </summary>
    public async Task<ApiResponse<DashboardAccessDto>> GetDashboardAccessAsync(int dashboardId)
    {
        var dashboard = await _context.Dashboards.FindAsync(dashboardId);
        if (dashboard == null)
            return ApiResponse<DashboardAccessDto>.Fail("大屏不存在", "NOT_FOUND");

        var accessDto = new DashboardAccessDto
        {
            DashboardId = dashboard.DashboardId,
            IsPublic = dashboard.IsPublic,
            PublicUrl = dashboard.IsPublic ? $"/public/d/{dashboard.PublicUrl}" : null,
            Status = dashboard.Status,
            AuthorizedUserIds = DeserializeUserIds(dashboard.AuthorizedUserIds)
        };

        return ApiResponse<DashboardAccessDto>.Ok(accessDto);
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 反序列化授权用户ID列表
    /// </summary>
    private List<int> DeserializeUserIds(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<int>();

        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch (JsonException)
        {
            return new List<int>();
        }
    }

    /// <summary>
    /// 将 Dashboard 实体映射为 DashboardDto
    /// </summary>
    private DashboardDto MapToDto(Dashboard dashboard)
    {
        return new DashboardDto
        {
            DashboardId = dashboard.DashboardId,
            Name = dashboard.Name,
            Description = dashboard.Description,
            Theme = dashboard.Theme,
            RefreshInterval = dashboard.RefreshInterval,
            IsPublic = dashboard.IsPublic,
            Status = dashboard.Status,
            PublicUrl = dashboard.PublicUrl,
            Width = dashboard.Width,
            Height = dashboard.Height,
            BackgroundColor = dashboard.BackgroundColor,
            BackgroundImage = dashboard.BackgroundImage,
            CreatedTime = dashboard.CreatedTime,
            UpdatedTime = dashboard.UpdatedTime,
            CreatorName = dashboard.Creator?.Username,
            WidgetCount = dashboard.Widgets?.Count ?? 0
        };
    }

    /// <summary>
    /// 将 Dashboard 实体映射为 DashboardDetailDto
    /// </summary>
    private DashboardDetailDto MapToDetailDto(Dashboard dashboard)
    {
        return new DashboardDetailDto
        {
            DashboardId = dashboard.DashboardId,
            Name = dashboard.Name,
            Description = dashboard.Description,
            Theme = dashboard.Theme,
            RefreshInterval = dashboard.RefreshInterval,
            IsPublic = dashboard.IsPublic,
            Status = dashboard.Status,
            PublicUrl = dashboard.PublicUrl,
            Width = dashboard.Width,
            Height = dashboard.Height,
            BackgroundColor = dashboard.BackgroundColor,
            BackgroundImage = dashboard.BackgroundImage,
            LayoutConfig = dashboard.LayoutConfig,
            ThemeConfig = dashboard.ThemeConfig,
            CreatedTime = dashboard.CreatedTime,
            UpdatedTime = dashboard.UpdatedTime,
            CreatorName = dashboard.Creator?.Username,
            WidgetCount = dashboard.Widgets.Count,
            Widgets = dashboard.Widgets.OrderBy(w => w.PositionY).ThenBy(w => w.PositionX).Select(w => MapToWidgetDto(w)).ToList()
        };
    }

    /// <summary>
    /// 将 DashboardWidget 实体映射为 DashboardWidgetDto
    /// </summary>
    private DashboardWidgetDto MapToWidgetDto(DashboardWidget widget, string? reportName = null)
    {
        return new DashboardWidgetDto
        {
            WidgetId = widget.WidgetId,
            DashboardId = widget.DashboardId,
            ReportId = widget.ReportId,
            ReportName = reportName ?? widget.Report?.ReportName,
            WidgetType = widget.WidgetType,
            Title = widget.Title,
            PositionX = widget.PositionX,
            PositionY = widget.PositionY,
            Width = widget.Width,
            Height = widget.Height,
            DataConfig = widget.DataConfig,
            StyleConfig = widget.StyleConfig,
            CreatedTime = widget.CreatedTime,
            Rules = widget.Rules?.OrderBy(r => r.Priority).Select(r => new WidgetRuleDto
            {
                RuleId = r.RuleId,
                WidgetId = r.WidgetId,
                RuleName = r.RuleName,
                Field = r.Field,
                Operator = r.Operator,
                Value = r.Value,
                ActionType = r.ActionType,
                ActionValue = r.ActionValue,
                Priority = r.Priority,
                CreatedTime = r.CreatedTime
            }).ToList() ?? new List<WidgetRuleDto>()
        };
    }

    #endregion

    #region 辅助类

    /// <summary>
    /// 组件数据配置辅助类
    /// </summary>
    private class WidgetDataConfig
    {
        public int? ReportId { get; set; }
        public Dictionary<string, object>? QueryConditionValues { get; set; }
    }

    #endregion
}
