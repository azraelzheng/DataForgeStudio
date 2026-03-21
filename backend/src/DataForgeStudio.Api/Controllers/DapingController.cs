using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Domain.DTOs;
using DataForgeStudio.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DataForgeStudio.Api.Controllers;

/// <summary>
/// 高级大屏 API 控制器
/// </summary>
[ApiController]
[Route("api/daping")]
[Authorize]
public class DapingController : ControllerBase
{
    private readonly IDapingService _dapingService;

    public DapingController(IDapingService dapingService)
    {
        _dapingService = dapingService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// 获取项目列表
    /// </summary>
    [HttpPost("projects/list")]
    public async Task<ApiResponse<PagedResponse<DapingProjectDto>>> GetProjects(
        [FromBody] DapingProjectListRequest request)
    {
        return await _dapingService.GetProjectsAsync(request, GetCurrentUserId());
    }

    /// <summary>
    /// 获取项目详情
    /// </summary>
    [HttpGet("projects/{id}")]
    public async Task<ApiResponse<DapingProjectDetailDto>> GetProject(int id)
    {
        return await _dapingService.GetProjectByIdAsync(id, GetCurrentUserId());
    }

    /// <summary>
    /// 创建项目
    /// </summary>
    [HttpPost("projects")]
    public async Task<ApiResponse<DapingProjectDto>> CreateProject(
        [FromBody] CreateDapingProjectRequest request)
    {
        return await _dapingService.CreateProjectAsync(request, GetCurrentUserId());
    }

    /// <summary>
    /// 更新项目
    /// </summary>
    [HttpPut("projects/{id}")]
    public async Task<ApiResponse> UpdateProject(int id, [FromBody] UpdateDapingProjectRequest request)
    {
        return await _dapingService.UpdateProjectAsync(id, request, GetCurrentUserId());
    }

    /// <summary>
    /// 删除项目
    /// </summary>
    [HttpDelete("projects/{id}")]
    public async Task<ApiResponse> DeleteProject(int id)
    {
        return await _dapingService.DeleteProjectAsync(id, GetCurrentUserId());
    }

    /// <summary>
    /// 发布项目
    /// </summary>
    [HttpPost("projects/{id}/publish")]
    public async Task<ApiResponse<DapingProjectDto>> PublishProject(int id)
    {
        return await _dapingService.PublishProjectAsync(id, GetCurrentUserId());
    }

    /// <summary>
    /// 取消发布
    /// </summary>
    [HttpPost("projects/{id}/unpublish")]
    public async Task<ApiResponse<DapingProjectDto>> UnpublishProject(int id)
    {
        return await _dapingService.UnpublishProjectAsync(id, GetCurrentUserId());
    }

    /// <summary>
    /// 获取公开项目详情（无需认证）
    /// </summary>
    [HttpGet("/api/public/daping/{publicUrl}")]
    [AllowAnonymous]
    public async Task<ApiResponse<DapingProjectDetailDto>> GetPublicProject(string publicUrl)
    {
        return await _dapingService.GetPublicProjectAsync(publicUrl);
    }
}
