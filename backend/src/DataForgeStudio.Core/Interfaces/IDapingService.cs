using DataForgeStudio.Domain.DTOs;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 高级大屏服务接口
/// </summary>
public interface IDapingService
{
    /// <summary>
    /// 获取项目列表
    /// </summary>
    Task<ApiResponse<PagedResponse<DapingProjectDto>>> GetProjectsAsync(DapingProjectListRequest request, int userId);

    /// <summary>
    /// 获取项目详情
    /// </summary>
    Task<ApiResponse<DapingProjectDetailDto>> GetProjectByIdAsync(int projectId, int userId);

    /// <summary>
    /// 创建项目
    /// </summary>
    Task<ApiResponse<DapingProjectDto>> CreateProjectAsync(CreateDapingProjectRequest request, int userId);

    /// <summary>
    /// 更新项目
    /// </summary>
    Task<ApiResponse> UpdateProjectAsync(int projectId, UpdateDapingProjectRequest request, int userId);

    /// <summary>
    /// 删除项目
    /// </summary>
    Task<ApiResponse> DeleteProjectAsync(int projectId, int userId);

    /// <summary>
    /// 发布项目
    /// </summary>
    Task<ApiResponse<DapingProjectDto>> PublishProjectAsync(int projectId, int userId);

    /// <summary>
    /// 取消发布
    /// </summary>
    Task<ApiResponse<DapingProjectDto>> UnpublishProjectAsync(int projectId, int userId);

    /// <summary>
    /// 获取公开项目（无需认证）
    /// </summary>
    Task<ApiResponse<DapingProjectDetailDto>> GetPublicProjectAsync(string publicUrl);
}
