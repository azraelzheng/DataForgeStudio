// backend/src/DataForgeStudio.Domain/DTOs/DirectoryInfoDto.cs
namespace DataForgeStudio.Domain.DTOs;

/// <summary>
/// 目录信息 DTO（用于备份路径选择）
/// </summary>
public class DirectoryInfoDto
{
    /// <summary>
    /// 目录名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 完整路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 是否有子目录
    /// </summary>
    public bool HasChildren { get; set; }

    /// <summary>
    /// 是否为驱动器根目录
    /// </summary>
    public bool IsDrive { get; set; }
}
