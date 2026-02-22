using System.Text.Json.Serialization;

namespace DeployManager.Models;

/// <summary>
/// 数据库配置
/// </summary>
public class DatabaseConfig
{
    [JsonPropertyName("server")]
    public string Server { get; set; } = "localhost";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 1433;

    [JsonPropertyName("database")]
    public string Database { get; set; } = "DataForgeStudio";

    [JsonPropertyName("username")]
    public string Username { get; set; } = "sa";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("useWindowsAuth")]
    public bool UseWindowsAuth { get; set; } = true;

    /// <summary>
    /// 获取数据库连接字符串
    /// </summary>
    public string GetConnectionString()
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = Port == 1433 ? $"tcp:{Server}" : $"tcp:{Server},{Port}",
            InitialCatalog = Database,
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };
        if (UseWindowsAuth)
            builder.IntegratedSecurity = true;
        else
        {
            builder.UserID = Username;
            builder.Password = Password;
        }
        return builder.ConnectionString;
    }
}

/// <summary>
/// 服务状态枚举
/// </summary>
public enum ServiceStatus
{
    Running,
    Stopped,
    Unknown
}
