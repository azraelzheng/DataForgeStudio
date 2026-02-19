using Installer.Models;
using Microsoft.Data.SqlClient;

namespace Installer.Services;

public interface IDatabaseTestService
{
    Task<(bool Success, string Message)> TestConnectionAsync(DatabaseConfig config);
}

public class DatabaseTestService : IDatabaseTestService
{
    public async Task<(bool Success, string Message)> TestConnectionAsync(DatabaseConfig config)
    {
        try
        {
            using var connection = new SqlConnection(config.GetMasterConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{config.Database}'";
            var result = await command.ExecuteScalarAsync();
            var dbExists = Convert.ToInt32(result) > 0;

            if (dbExists)
                return (true, "连接成功！数据库已存在（现有数据将保留）");
            else
                return (true, "连接成功！数据库将在首次启动时创建");
        }
        catch (SqlException ex)
        {
            var message = ex.Number switch
            {
                2 or 53 => "无法连接到服务器，请检查服务器地址和端口",
                18456 => "登录失败，请检查用户名和密码",
                18452 => "登录失败，服务器不允许远程连接",
                4060 => "无法打开数据库，请检查数据库名称",
                _ => $"连接失败: {ex.Message}"
            };
            return (false, message);
        }
        catch (Exception ex)
        {
            return (false, $"连接失败: {ex.Message}");
        }
    }
}
