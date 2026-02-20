namespace DeployManager.Services;

/// <summary>
/// IIS 管理接口
/// </summary>
public interface IIisManager
{
    /// <summary>
    /// 检查 IIS 是否已安装
    /// </summary>
    bool IsIisInstalled();

    /// <summary>
    /// 检查站点是否存在
    /// </summary>
    bool IsSiteExists(string siteName);

    /// <summary>
    /// 配置站点
    /// </summary>
    void ConfigureSite(string siteName, int port, string physicalPath);

    /// <summary>
    /// 启动站点
    /// </summary>
    void StartSite(string siteName);

    /// <summary>
    /// 停止站点
    /// </summary>
    void StopSite(string siteName);
}
