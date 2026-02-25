// backend/src/DataForgeStudio.Core/Interfaces/IDirectoryService.cs
using DataForgeStudio.Domain.DTOs;

namespace DataForgeStudio.Core.Interfaces;

public interface IDirectoryService
{
    /// <summary>
    /// 获取目录列表
    /// </summary>
    /// <param name="path">父目录路径，为空则返回驱动器列表</param>
    /// <returns>目录信息列表</returns>
    Task<List<DirectoryInfoDto>> GetDirectoriesAsync(string? path = null);
}
