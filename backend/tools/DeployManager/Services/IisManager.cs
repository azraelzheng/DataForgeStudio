using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Web.Administration;

namespace DeployManager.Services;

/// <summary>
/// IIS 管理器实现
/// 使用 Microsoft.Web.Administration 管理 IIS 站点
/// </summary>
public class IisManager : IIisManager
{
    /// <summary>
    /// 检查 IIS 是否已安装
    /// </summary>
    /// <returns>如果 IIS 已安装返回 true，否则返回 false</returns>
    public bool IsIisInstalled()
    {
        try
        {
            // 尝试创建 ServerManager 来检查 IIS 是否可用
            using var serverManager = new ServerManager();
            Debug.WriteLine("[IisManager] IIS 已安装且可用");
            return true;
        }
        catch (FileNotFoundException ex)
        {
            // Microsoft.Web.Administration 找不到 IIS 配置文件
            Debug.WriteLine($"[IisManager] IIS 未安装或配置文件不存在: {ex.Message}");
            return false;
        }
        catch (COMException ex)
        {
            // COM 组件异常，通常表示 IIS 管理服务未运行
            Debug.WriteLine($"[IisManager] IIS 管理服务未运行: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IisManager] 检查 IIS 安装状态时发生异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 检查指定站点是否存在
    /// </summary>
    /// <param name="siteName">站点名称</param>
    /// <returns>如果站点存在返回 true，否则返回 false</returns>
    public bool IsSiteExists(string siteName)
    {
        if (string.IsNullOrWhiteSpace(siteName))
        {
            throw new ArgumentNullException(nameof(siteName));
        }

        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites[siteName];
            var exists = site != null;

            Debug.WriteLine($"[IisManager] 检查站点 '{siteName}' 是否存在: {exists}");
            return exists;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IisManager] 检查站点存在性时发生异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 配置 IIS 站点（创建或更新）
    /// </summary>
    /// <param name="siteName">站点名称</param>
    /// <param name="port">监听端口</param>
    /// <param name="physicalPath">物理路径</param>
    /// <exception cref="ArgumentNullException">参数为空</exception>
    /// <exception cref="InvalidOperationException">IIS 未安装或配置失败</exception>
    public void ConfigureSite(string siteName, int port, string physicalPath)
    {
        if (string.IsNullOrWhiteSpace(siteName))
        {
            throw new ArgumentNullException(nameof(siteName));
        }

        if (port <= 0 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "端口号必须在 1-65535 之间");
        }

        if (string.IsNullOrWhiteSpace(physicalPath))
        {
            throw new ArgumentNullException(nameof(physicalPath));
        }

        try
        {
            using var serverManager = new ServerManager();

            // 确保物理路径存在
            if (!Directory.Exists(physicalPath))
            {
                Directory.CreateDirectory(physicalPath);
                Debug.WriteLine($"[IisManager] 创建物理路径: {physicalPath}");
            }

            // 查找或创建应用程序池
            var appPoolName = $"{siteName}_AppPool";
            var appPool = serverManager.ApplicationPools[appPoolName];

            if (appPool == null)
            {
                appPool = serverManager.ApplicationPools.Add(appPoolName);
                appPool.ManagedRuntimeVersion = ""; // 无托管代码（用于 .NET Core）
                appPool.AutoStart = true;
                Debug.WriteLine($"[IisManager] 创建应用程序池: {appPoolName}");
            }

            // 查找或创建站点
            var site = serverManager.Sites[siteName];

            if (site == null)
            {
                // 创建新站点
                site = serverManager.Sites.Add(siteName, physicalPath, port);
                site.ApplicationDefaults.ApplicationPoolName = appPoolName;
                Debug.WriteLine($"[IisManager] 创建站点: {siteName}, 端口: {port}");
            }
            else
            {
                // 更新现有站点
                site.Applications[0].VirtualDirectories[0].PhysicalPath = physicalPath;
                site.Applications[0].ApplicationPoolName = appPoolName;

                // 更新绑定（移除旧的 HTTP 绑定，添加新的）
                var httpBinding = site.Bindings.FirstOrDefault(b =>
                    b.Protocol.Equals("http", StringComparison.OrdinalIgnoreCase));

                if (httpBinding != null)
                {
                    site.Bindings.Remove(httpBinding);
                }

                site.Bindings.Add($":{port}:", "http");
                Debug.WriteLine($"[IisManager] 更新站点: {siteName}, 端口: {port}");
            }

            // 提交更改
            serverManager.CommitChanges();
            Debug.WriteLine($"[IisManager] 站点配置已保存: {siteName}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IisManager] 配置站点失败: {ex.Message}");
            throw new InvalidOperationException($"配置 IIS 站点失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 启动 IIS 站点
    /// </summary>
    /// <param name="siteName">站点名称</param>
    /// <exception cref="InvalidOperationException">站点不存在或启动失败</exception>
    public void StartSite(string siteName)
    {
        if (string.IsNullOrWhiteSpace(siteName))
        {
            throw new ArgumentNullException(nameof(siteName));
        }

        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites[siteName];

            if (site == null)
            {
                throw new InvalidOperationException($"站点 '{siteName}' 不存在");
            }

            if (site.State == ObjectState.Started)
            {
                Debug.WriteLine($"[IisManager] 站点 '{siteName}' 已在运行中");
                return;
            }

            Debug.WriteLine($"[IisManager] 正在启动站点 '{siteName}'...");
            var state = site.Start();
            serverManager.CommitChanges();

            Debug.WriteLine($"[IisManager] 站点 '{siteName}' 启动成功，状态: {state}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IisManager] 启动站点失败: {ex.Message}");
            throw new InvalidOperationException($"启动 IIS 站点失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 停止 IIS 站点
    /// </summary>
    /// <param name="siteName">站点名称</param>
    /// <exception cref="InvalidOperationException">站点不存在或停止失败</exception>
    public void StopSite(string siteName)
    {
        if (string.IsNullOrWhiteSpace(siteName))
        {
            throw new ArgumentNullException(nameof(siteName));
        }

        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites[siteName];

            if (site == null)
            {
                throw new InvalidOperationException($"站点 '{siteName}' 不存在");
            }

            if (site.State == ObjectState.Stopped)
            {
                Debug.WriteLine($"[IisManager] 站点 '{siteName}' 已停止");
                return;
            }

            Debug.WriteLine($"[IisManager] 正在停止站点 '{siteName}'...");
            var state = site.Stop();
            serverManager.CommitChanges();

            Debug.WriteLine($"[IisManager] 站点 '{siteName}' 停止成功，状态: {state}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IisManager] 停止站点失败: {ex.Message}");
            throw new InvalidOperationException($"停止 IIS 站点失败: {ex.Message}", ex);
        }
    }
}
