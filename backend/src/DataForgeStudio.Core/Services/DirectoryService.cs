// backend/src/DataForgeStudio.Core/Services/DirectoryService.cs
using System.Runtime.InteropServices;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Domain.DTOs;
using Microsoft.Extensions.Logging;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 目录服务 - 提供服务器目录浏览功能
/// </summary>
public class DirectoryService : IDirectoryService
{
    private readonly ILogger<DirectoryService> _logger;

    // 敏感目录黑名单（不显示）
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Windows",
        "Program Files",
        "Program Files (x86)",
        "System Volume Information",
        "$RECYCLE.BIN",
        "ProgramData",
        "Users",
        "PerfLogs",
        "Recovery",
        "Boot",
        "System Reserved"
    };

    public DirectoryService(ILogger<DirectoryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取目录列表
    /// </summary>
    public async Task<List<DirectoryInfoDto>> GetDirectoriesAsync(string? path = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                // 返回可用驱动器列表
                return await GetDrivesAsync();
            }
            else
            {
                // 返回指定路径下的子目录
                return await GetSubDirectoriesAsync(path);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "访问目录被拒绝: {Path}", path);
            return new List<DirectoryInfoDto>();
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "目录不存在: {Path}", path);
            return new List<DirectoryInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取目录列表失败: {Path}", path);
            throw;
        }
    }

    /// <summary>
    /// 获取可用驱动器列表
    /// </summary>
    private Task<List<DirectoryInfoDto>> GetDrivesAsync()
    {
        var result = new List<DirectoryInfoDto>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    result.Add(new DirectoryInfoDto
                    {
                        Name = $"{drive.Name} ({GetDriveLabel(drive)})",
                        Path = drive.RootDirectory.FullName.TrimEnd('\\'),
                        HasChildren = true,
                        IsDrive = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "无法访问驱动器: {Drive}", drive.Name);
            }
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// 获取驱动器标签
    /// </summary>
    private static string GetDriveLabel(DriveInfo drive)
    {
        try
        {
            return string.IsNullOrWhiteSpace(drive.VolumeLabel)
                ? "本地磁盘"
                : drive.VolumeLabel;
        }
        catch
        {
            return "本地磁盘";
        }
    }

    /// <summary>
    /// 获取子目录列表
    /// </summary>
    private Task<List<DirectoryInfoDto>> GetSubDirectoriesAsync(string path)
    {
        var result = new List<DirectoryInfoDto>();

        // 验证路径格式
        if (!IsValidPath(path))
        {
            _logger.LogWarning("无效的路径格式: {Path}", path);
            return Task.FromResult(result);
        }

        var directoryInfo = new DirectoryInfo(path);

        if (!directoryInfo.Exists)
        {
            _logger.LogWarning("目录不存在: {Path}", path);
            return Task.FromResult(result);
        }

        foreach (var dir in directoryInfo.GetDirectories())
        {
            try
            {
                // 过滤敏感目录
                if (ShouldExclude(dir.Name))
                {
                    continue;
                }

                // 检查是否可访问
                bool hasChildren = false;
                try
                {
                    hasChildren = dir.GetDirectories().Length > 0;
                }
                catch (UnauthorizedAccessException)
                {
                    // 无权限访问子目录，跳过
                    continue;
                }

                result.Add(new DirectoryInfoDto
                {
                    Name = dir.Name,
                    Path = dir.FullName,
                    HasChildren = hasChildren,
                    IsDrive = false
                });
            }
            catch (UnauthorizedAccessException)
            {
                // 无权限访问此目录，跳过
                continue;
            }
        }

        // 按名称排序
        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(result);
    }

    /// <summary>
    /// 检查目录是否应该被排除
    /// </summary>
    private static bool ShouldExclude(string directoryName)
    {
        return ExcludedDirectories.Contains(directoryName);
    }

    /// <summary>
    /// 验证路径格式
    /// </summary>
    private static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // 检查是否为合法的 Windows 路径
        try
        {
            // 检查路径中是否包含非法字符
            char[] invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
                return false;

            // 检查路径格式（如 C:\ 或 \\server\share）
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true; // 非 Windows 系统跳过格式检查

            // Windows 路径格式检查
            return path.Length >= 2 && path[1] == ':';
        }
        catch
        {
            return false;
        }
    }
}
