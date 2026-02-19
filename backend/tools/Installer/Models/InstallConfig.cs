using System.Text.Json.Serialization;

namespace Installer.Models;

public class InstallConfig
{
    [JsonPropertyName("installPath")]
    public string InstallPath { get; set; } = @"C:\Program Files\DataForgeStudio";

    [JsonPropertyName("database")]
    public DatabaseConfig Database { get; set; } = new();

    [JsonPropertyName("frontend")]
    public FrontendConfig Frontend { get; set; } = new();

    [JsonPropertyName("backend")]
    public BackendConfig Backend { get; set; } = new();
}

public class DatabaseConfig
{
    [JsonPropertyName("server")]
    public string Server { get; set; } = "localhost";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 1433;

    [JsonPropertyName("database")]
    public string Database { get; set; } = "DataForgeStudio_V4";

    [JsonPropertyName("username")]
    public string Username { get; set; } = "sa";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("useWindowsAuth")]
    public bool UseWindowsAuth { get; set; } = true;

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

    public string GetMasterConnectionString()
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = Port == 1433 ? $"tcp:{Server}" : $"tcp:{Server},{Port}",
            InitialCatalog = "master",
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

public class FrontendConfig
{
    [JsonPropertyName("port")]
    public int Port { get; set; } = 80;
}

public class BackendConfig
{
    [JsonPropertyName("port")]
    public int Port { get; set; } = 5000;

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = "DataForgeStudio API";
}
