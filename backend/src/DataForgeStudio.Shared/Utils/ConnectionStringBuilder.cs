namespace DataForgeStudio.Shared.Utils;

/// <summary>
/// 数据库连接字符串构建器
/// 处理 SQL Server 命名实例等特殊情况
/// </summary>
public static class ConnectionStringBuilder
{
    /// <summary>
    /// 检测是否为 SQL Server 命名实例
    /// </summary>
    /// <param name="serverAddress">服务器地址</param>
    /// <returns>如果是命名实例返回 true，否则返回 false</returns>
    public static bool IsNamedInstance(string serverAddress)
    {
        return !string.IsNullOrEmpty(serverAddress) && serverAddress.Contains('\\');
    }

    /// <summary>
    /// 构建 SQL Server 连接字符串
    /// </summary>
    /// <param name="serverAddress">服务器地址（可以是 hostname 或 hostname\instanceName）</param>
    /// <param name="port">端口号（仅对默认实例有效）</param>
    /// <param name="database">数据库名称</param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="connectionTimeout">连接超时（秒）</param>
    /// <returns>SQL Server 连接字符串</returns>
    public static string BuildSqlServerConnectionString(
        string serverAddress,
        int? port,
        string database,
        string username,
        string password,
        int connectionTimeout = 30)
    {
        // 命名实例：Server=hostname\instanceName（不指定端口，由 SQL Server Browser 解析）
        // 默认实例：Server=hostname,port
        var serverPart = IsNamedInstance(serverAddress)
            ? serverAddress
            : $"{serverAddress},{port ?? 1433}";

        return $"Server={serverPart};Database={database};User Id={username};Password={password};Connection Timeout={connectionTimeout};TrustServerCertificate=True;";
    }
}
