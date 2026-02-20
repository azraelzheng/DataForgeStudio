using System.Diagnostics;
using DeployManager.Models;
using Microsoft.Data.SqlClient;

namespace DeployManager.Services;

/// <summary>
/// 数据库连接服务实现
/// 使用 Microsoft.Data.SqlClient 测试数据库连接
/// </summary>
public class DatabaseConnectionService : IDatabaseConnectionService
{
    /// <summary>
    /// 测试数据库连接
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns>包含成功标志和消息的元组</returns>
    public async Task<(bool Success, string Message)> TestConnectionAsync(DatabaseConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            // 首先连接到 master 数据库检查服务器连接
            var masterConnectionString = GetMasterConnectionString(config);
            Debug.WriteLine($"[DatabaseConnectionService] 正在连接到 master 数据库...");

            using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync();

            Debug.WriteLine("[DatabaseConnectionService] 成功连接到服务器");

            // 检查目标数据库是否存在
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM sys.databases WHERE name = @databaseName";
            command.Parameters.AddWithValue("@databaseName", config.Database);

            var result = await command.ExecuteScalarAsync();
            var dbExists = Convert.ToInt32(result) > 0;

            if (dbExists)
            {
                // 尝试连接到目标数据库
                var targetConnectionString = config.GetConnectionString();
                using var targetConnection = new SqlConnection(targetConnectionString);
                await targetConnection.OpenAsync();

                Debug.WriteLine($"[DatabaseConnectionService] 成功连接到目标数据库: {config.Database}");
                return (true, "连接成功！数据库已存在（现有数据将保留）");
            }
            else
            {
                Debug.WriteLine($"[DatabaseConnectionService] 数据库 '{config.Database}' 不存在，将在首次启动时创建");
                return (true, "连接成功！数据库将在首次启动时创建");
            }
        }
        catch (SqlException ex)
        {
            var message = GetSqlErrorMessage(ex);
            Debug.WriteLine($"[DatabaseConnectionService] SQL 错误 (Code: {ex.Number}): {ex.Message}");
            return (false, message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseConnectionService] 连接失败: {ex.Message}");
            return (false, $"连接失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取 master 数据库的连接字符串
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns>master 数据库连接字符串</returns>
    private static string GetMasterConnectionString(DatabaseConfig config)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = config.Port == 1433
                ? $"tcp:{config.Server}"
                : $"tcp:{config.Server},{config.Port}",
            InitialCatalog = "master",
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };

        if (config.UseWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = config.Username;
            builder.Password = config.Password;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// 根据 SQL 错误代码获取友好的错误消息
    /// </summary>
    /// <param name="ex">SQL 异常</param>
    /// <returns>友好的错误消息</returns>
    private static string GetSqlErrorMessage(SqlException ex)
    {
        return ex.Number switch
        {
            // 连接相关错误
            2 => "无法连接到服务器，请检查服务器地址是否正确",
            53 => "无法连接到服务器，请检查服务器地址和端口",
            258 => "连接超时，请检查服务器是否可达",

            // 认证相关错误
            18456 => "登录失败，请检查用户名和密码",
            18452 => "登录失败，服务器不允许远程连接",
            18450 => "登录失败，用户没有访问权限",

            // 数据库相关错误
            4060 => "无法打开数据库，请检查数据库名称",
            911 => "数据库不存在或用户没有访问权限",

            // 网络相关错误
            10060 => "网络连接超时，请检查防火墙设置",
            10061 => "连接被拒绝，请检查 SQL Server 是否正在运行",

            // SSL/证书相关错误
            -2146893019 => "SSL 连接失败，请检查证书配置",

            // 默认错误消息
            _ => $"连接失败 (错误代码: {ex.Number}): {ex.Message}"
        };
    }

    /// <summary>
    /// 检查 SQL Server 服务是否可用
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns>如果服务可用返回 true，否则返回 false</returns>
    public async Task<(bool Success, string Message)> CheckServerAvailableAsync(DatabaseConfig config)
    {
        try
        {
            var connectionString = GetMasterConnectionString(config);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // 执行简单查询确认服务器响应
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT @@VERSION";
            var version = await command.ExecuteScalarAsync();

            Debug.WriteLine($"[DatabaseConnectionService] SQL Server 版本: {version}");
            return (true, $"服务器可用\n版本: {version}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseConnectionService] 服务器不可用: {ex.Message}");
            return (false, $"服务器不可用: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取数据库大小信息
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns>数据库大小信息（MB）</returns>
    public async Task<long?> GetDatabaseSizeMbAsync(DatabaseConfig config)
    {
        try
        {
            var connectionString = config.GetConnectionString();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT SUM(size * 8 / 1024) as SizeMB
                FROM sys.master_files
                WHERE database_id = DB_ID(@databaseName)";
            command.Parameters.AddWithValue("@databaseName", config.Database);

            var result = await command.ExecuteScalarAsync();
            return result != DBNull.Value ? Convert.ToInt64(result) : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseConnectionService] 获取数据库大小失败: {ex.Message}");
            return null;
        }
    }
}
