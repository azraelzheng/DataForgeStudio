using DeployManager.Models;

namespace DeployManager.Services;

/// <summary>
/// 数据库连接服务接口
/// </summary>
public interface IDatabaseConnectionService
{
    /// <summary>
    /// 测试数据库连接
    /// </summary>
    Task<(bool Success, string Message)> TestConnectionAsync(DatabaseConfig config);
}
