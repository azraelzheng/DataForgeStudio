using DeployManager.Models;

namespace DeployManager.Services;

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// 加载配置
    /// </summary>
    DeployConfig Load();

    /// <summary>
    /// 保存配置
    /// </summary>
    void Save(DeployConfig config);

    /// <summary>
    /// 配置文件路径
    /// </summary>
    string ConfigPath { get; }
}
