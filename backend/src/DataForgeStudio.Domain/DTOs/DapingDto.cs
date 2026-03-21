using System.ComponentModel.DataAnnotations;

namespace DataForgeStudio.Domain.DTOs;

/// <summary>
/// 大屏项目 DTO
/// </summary>
public class DapingProjectDto
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int State { get; set; }
    public string? PublicUrl { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}

/// <summary>
/// 大屏项目详情 DTO（包含完整配置）
/// </summary>
public class DapingProjectDetailDto : DapingProjectDto
{
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 创建大屏项目请求
/// </summary>
public class CreateDapingProjectRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 更新大屏项目请求
/// </summary>
public class UpdateDapingProjectRequest
{
    public string? Name { get; set; }

    public string? Content { get; set; }
}

/// <summary>
/// 大屏项目列表请求
/// </summary>
public class DapingProjectListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Name { get; set; }
    public int? State { get; set; }
}
