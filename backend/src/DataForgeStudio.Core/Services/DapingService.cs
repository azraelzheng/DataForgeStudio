using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.DTOs;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 高级大屏服务实现
/// </summary>
public class DapingService : IDapingService
{
    private readonly DataForgeStudioDbContext _context;

    public DapingService(DataForgeStudioDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PagedResponse<DapingProjectDto>>> GetProjectsAsync(
        DapingProjectListRequest request, int userId)
    {
        var query = _context.DapingProjects
            .Include(p => p.Creator)
            .Where(p => p.CreatedBy == userId);

        if (!string.IsNullOrEmpty(request.Name))
        {
            query = query.Where(p => p.Name.Contains(request.Name));
        }

        if (request.State.HasValue)
        {
            query = query.Where(p => p.State == request.State.Value);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.UpdatedTime ?? p.CreatedTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new DapingProjectDto
            {
                ProjectId = p.ProjectId,
                Name = p.Name,
                State = p.State,
                PublicUrl = p.PublicUrl,
                CreatedBy = p.CreatedBy,
                CreatorName = p.Creator != null ? p.Creator.Username : null,
                CreatedTime = p.CreatedTime,
                UpdatedTime = p.UpdatedTime
            })
            .ToListAsync();

        return ApiResponse<PagedResponse<DapingProjectDto>>.Ok(
            new PagedResponse<DapingProjectDto>(items, total, request.Page, request.PageSize));
    }

    public async Task<ApiResponse<DapingProjectDetailDto>> GetProjectByIdAsync(int projectId, int userId)
    {
        var project = await _context.DapingProjects
            .Include(p => p.Creator)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CreatedBy == userId);

        if (project == null)
        {
            return ApiResponse<DapingProjectDetailDto>.Fail("项目不存在或无权访问");
        }

        return ApiResponse<DapingProjectDetailDto>.Ok(MapToDetailDto(project));
    }

    public async Task<ApiResponse<DapingProjectDto>> CreateProjectAsync(
        CreateDapingProjectRequest request, int userId)
    {
        var project = new DapingProject
        {
            Name = request.Name,
            Content = request.Content,
            State = 1,
            CreatedBy = userId,
            CreatedTime = DateTime.UtcNow
        };

        _context.DapingProjects.Add(project);
        await _context.SaveChangesAsync();

        return ApiResponse<DapingProjectDto>.Ok(MapToDto(project));
    }

    public async Task<ApiResponse> UpdateProjectAsync(
        int projectId, UpdateDapingProjectRequest request, int userId)
    {
        var project = await _context.DapingProjects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CreatedBy == userId);

        if (project == null)
        {
            return ApiResponse.Fail("项目不存在或无权访问");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            project.Name = request.Name;
        }

        if (!string.IsNullOrEmpty(request.Content))
        {
            project.Content = request.Content;
        }

        project.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("更新成功");
    }

    public async Task<ApiResponse> DeleteProjectAsync(int projectId, int userId)
    {
        var project = await _context.DapingProjects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CreatedBy == userId);

        if (project == null)
        {
            return ApiResponse.Fail("项目不存在或无权访问");
        }

        _context.DapingProjects.Remove(project);
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("删除成功");
    }

    public async Task<ApiResponse<DapingProjectDto>> PublishProjectAsync(int projectId, int userId)
    {
        var project = await _context.DapingProjects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CreatedBy == userId);

        if (project == null)
        {
            return ApiResponse<DapingProjectDto>.Fail("项目不存在或无权访问");
        }

        project.State = 2;
        project.PublicUrl ??= Guid.NewGuid().ToString("N")[..8].ToUpper();
        project.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<DapingProjectDto>.Ok(MapToDto(project));
    }

    public async Task<ApiResponse<DapingProjectDto>> UnpublishProjectAsync(int projectId, int userId)
    {
        var project = await _context.DapingProjects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CreatedBy == userId);

        if (project == null)
        {
            return ApiResponse<DapingProjectDto>.Fail("项目不存在或无权访问");
        }

        project.State = 1;
        project.UpdatedTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<DapingProjectDto>.Ok(MapToDto(project));
    }

    public async Task<ApiResponse<DapingProjectDetailDto>> GetPublicProjectAsync(string publicUrl)
    {
        var project = await _context.DapingProjects
            .FirstOrDefaultAsync(p => p.PublicUrl == publicUrl && p.State == 2);

        if (project == null)
        {
            return ApiResponse<DapingProjectDetailDto>.Fail("项目不存在或未发布");
        }

        return ApiResponse<DapingProjectDetailDto>.Ok(MapToDetailDto(project));
    }

    private static DapingProjectDto MapToDto(DapingProject p) => new()
    {
        ProjectId = p.ProjectId,
        Name = p.Name,
        State = p.State,
        PublicUrl = p.PublicUrl,
        CreatedBy = p.CreatedBy,
        CreatedTime = p.CreatedTime,
        UpdatedTime = p.UpdatedTime
    };

    private static DapingProjectDetailDto MapToDetailDto(DapingProject p) => new()
    {
        ProjectId = p.ProjectId,
        Name = p.Name,
        State = p.State,
        Content = p.Content,
        PublicUrl = p.PublicUrl,
        CreatedBy = p.CreatedBy,
        CreatedTime = p.CreatedTime,
        UpdatedTime = p.UpdatedTime
    };
}
