using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.Utils;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 数据库连接选项
/// </summary>
public class DatabaseOptions
{
    public int DefaultCommandTimeout { get; set; } = 60;
    public int DefaultConnectionTimeout { get; set; } = 30;
}

/// <summary>
/// 数据库服务实现 - 使用 ADO.NET
/// </summary>
public class DatabaseService : IDatabaseService
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly DatabaseOptions _options;
    private readonly IConfiguration _configuration;

    public DatabaseService(ILogger<DatabaseService> logger, IOptions<DatabaseOptions>? options = null, IConfiguration? configuration = null)
    {
        _logger = logger;
        _options = options?.Value ?? new DatabaseOptions();
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// 获取加密密钥
    /// </summary>
    private (string key, string iv) GetEncryptionKeys()
    {
        var key = _configuration["Security:Encryption:AesKey"]
            ?? throw new InvalidOperationException("加密密钥未配置。请设置环境变量 DFS_ENCRYPTION_AES_KEY");
        var iv = _configuration["Security:Encryption:AesIV"]
            ?? throw new InvalidOperationException("加密IV未配置。请设置环境变量 DFS_ENCRYPTION_AES_IV");
        return (key, iv);
    }

    /// <summary>
    /// 构建数据库连接字符串
    /// </summary>
    private string BuildConnectionString(DataSource dataSource)
    {
        // 解密密码（使用配置的密钥）
        var (key, iv) = GetEncryptionKeys();
        var password = !string.IsNullOrEmpty(dataSource.Password)
            ? EncryptionHelper.AesDecrypt(dataSource.Password, key, iv)
            : "";

        return dataSource.DbType switch
        {
            "SqlServer" => $"Server={dataSource.ServerAddress},{dataSource.Port};Database={dataSource.DatabaseName};User Id={dataSource.Username};Password={password};Connection Timeout={dataSource.ConnectionTimeout};TrustServerCertificate=True;",
            "MySql" => $"Server={dataSource.ServerAddress};Port={dataSource.Port};Database={dataSource.DatabaseName};Uid={dataSource.Username};Pwd={password};Connection Timeout={dataSource.ConnectionTimeout};AllowUserVariables=True;SslMode=None;",
            "PostgreSQL" => $"Host={dataSource.ServerAddress};Port={dataSource.Port};Database={dataSource.DatabaseName};Username={dataSource.Username};Password={password};Timeout={dataSource.ConnectionTimeout};",
            "Oracle" => $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={dataSource.ServerAddress})(PORT={dataSource.Port}))(CONNECT_DATA=(SERVICE_NAME={dataSource.DatabaseName})));User Id={dataSource.Username};Password={password};",
            "SQLite" => $"Data Source={dataSource.DatabaseName};",
            _ => throw new NotSupportedException($"不支持的数据库类型: {dataSource.DbType}")
        };
    }

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    private DbConnection CreateConnection(DataSource dataSource)
    {
        var connectionString = BuildConnectionString(dataSource);

        return dataSource.DbType switch
        {
            "SqlServer" => new Microsoft.Data.SqlClient.SqlConnection(connectionString),
            "MySql" => new MySqlConnector.MySqlConnection(connectionString),
            "PostgreSQL" => new Npgsql.NpgsqlConnection(connectionString),
            "Oracle" => new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString),
            "SQLite" => new Microsoft.Data.Sqlite.SqliteConnection(connectionString),
            _ => throw new NotSupportedException($"不支持的数据库类型: {dataSource.DbType}")
        };
    }

    /// <summary>
    /// 创建数据库命令
    /// </summary>
    private DbCommand CreateCommand(string sql, DbConnection connection, Dictionary<string, object>? parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.DefaultCommandTimeout;

        // 添加参数
        if (parameters != null && parameters.Count > 0)
        {
            foreach (var param in parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }
        }

        return command;
    }

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    public async Task<ApiResponse<bool>> TestConnectionAsync(DataSource dataSource)
    {
        try
        {
            _logger.LogInformation($"测试连接: {dataSource.DbType} - {dataSource.ServerAddress}:{dataSource.Port}/{dataSource.DatabaseName}");

            using var connection = CreateConnection(dataSource);
            await connection.OpenAsync();

            // 简单查询测试连接
            using var command = connection.CreateCommand();
            command.CommandText = dataSource.DbType switch
            {
                "SqlServer" => "SELECT 1",
                "MySql" => "SELECT 1",
                "PostgreSQL" => "SELECT 1",
                "Oracle" => "SELECT 1 FROM DUAL",
                "SQLite" => "SELECT 1",
                _ => "SELECT 1"
            };

            var result = await command.ExecuteScalarAsync();
            var success = result != null && Convert.ToInt32(result) == 1;

            if (success)
            {
                _logger.LogInformation($"连接测试成功: {dataSource.DbType} - {dataSource.ServerAddress}");
                return ApiResponse<bool>.Ok(true, "连接测试成功");
            }
            else
            {
                return ApiResponse<bool>.Fail("连接测试失败", "CONNECTION_FAILED");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"连接测试失败: {ex.Message}");
            return ApiResponse<bool>.Fail($"连接测试失败: {ex.Message}", "CONNECTION_ERROR");
        }
    }

    /// <summary>
    /// 测试数据库连接（使用明文密码，用于创建前测试）
    /// </summary>
    public async Task<ApiResponse<bool>> TestConnectionWithCredentialsAsync(DataSource dataSource, string plainPassword)
    {
        // 构建连接字符串（使用明文密码）
        var connectionString = BuildConnectionStringWithPassword(dataSource, plainPassword);

        // 创建连接
        using var connection = CreateConnectionByConnectionString(dataSource.DbType, connectionString);

        try
        {
            _logger.LogInformation($"测试连接: {dataSource.DbType} - {dataSource.ServerAddress}:{dataSource.Port}/{dataSource.DatabaseName}");

            await connection.OpenAsync();

            // 简单查询测试连接
            using var command = connection.CreateCommand();
            command.CommandText = dataSource.DbType switch
            {
                "SqlServer" => "SELECT 1",
                "MySql" => "SELECT 1",
                "PostgreSQL" => "SELECT 1",
                "Oracle" => "SELECT 1 FROM DUAL",
                "SQLite" => "SELECT 1",
                _ => "SELECT 1"
            };

            var result = await command.ExecuteScalarAsync();
            var success = result != null && Convert.ToInt32(result) == 1;

            if (success)
            {
                _logger.LogInformation($"连接测试成功: {dataSource.DbType} - {dataSource.ServerAddress}");
                return ApiResponse<bool>.Ok(true, "连接测试成功");
            }
            else
            {
                return ApiResponse<bool>.Fail("连接测试失败", "CONNECTION_FAILED");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"连接测试失败: {ex.Message}");
            return ApiResponse<bool>.Fail($"连接测试失败: {ex.Message}", "CONNECTION_ERROR");
        }
    }

    /// <summary>
    /// 执行 SQL 查询并返回字典列表
    /// </summary>
    public async Task<ApiResponse<List<Dictionary<string, object>>>> ExecuteQueryAsync(
        DataSource dataSource,
        string sql,
        Dictionary<string, object>? parameters)
    {
        try
        {
            _logger.LogInformation($"执行查询: {dataSource.DbType} - {sql.Substring(0, Math.Min(sql.Length, 100))}...");

            using var connection = CreateConnection(dataSource);
            await connection.OpenAsync();

            using var command = CreateCommand(sql, connection, parameters);
            using var reader = await command.ExecuteReaderAsync();

            var result = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var fieldName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[fieldName] = value!;
                }
                result.Add(row);
            }

            _logger.LogInformation($"查询执行成功: 返回 {result.Count} 行");
            return ApiResponse<List<Dictionary<string, object>>>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"查询执行失败: {ex.Message}");
            return ApiResponse<List<Dictionary<string, object>>>.Fail($"查询执行失败: {ex.Message}", "QUERY_ERROR");
        }
    }

    /// <summary>
    /// 执行 SQL 查询并返回 DataTable
    /// </summary>
    public async Task<ApiResponse<DataTable>> ExecuteQueryDataTableAsync(
        DataSource dataSource,
        string sql,
        Dictionary<string, object>? parameters)
    {
        try
        {
            _logger.LogInformation($"执行查询: {dataSource.DbType} - {sql.Substring(0, Math.Min(sql.Length, 100))}...");

            using var connection = CreateConnection(dataSource);
            await connection.OpenAsync();

            using var command = CreateCommand(sql, connection, parameters);

            var dataTable = new DataTable();

            // SQLite 需要特殊处理（没有 SqlDataAdapter）
            if (dataSource.DbType == "SQLite")
            {
                using var reader = await command.ExecuteReaderAsync();
                // 构建列
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dataTable.Columns.Add(reader.GetName(i), GetSqliteType(reader.GetFieldType(i).Name));
                }

                // 填充数据
                while (await reader.ReadAsync())
                {
                    var row = dataTable.NewRow();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                    }
                    dataTable.Rows.Add(row);
                }
            }
            else
            {
                using var adapter = CreateDataAdapter(connection, dataSource.DbType);
                adapter.SelectCommand = command;
                adapter.Fill(dataTable);
            }

            _logger.LogInformation($"查询执行成功: 返回 {dataTable.Rows.Count} 行, {dataTable.Columns.Count} 列");
            return ApiResponse<DataTable>.Ok(dataTable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"查询执行失败: {ex.Message}");
            return ApiResponse<DataTable>.Fail($"查询执行失败: {ex.Message}", "QUERY_ERROR");
        }
    }

    /// <summary>
    /// 获取 SQLite 数据类型
    /// </summary>
    private Type GetSqliteType(string typeName)
    {
        return typeName switch
        {
            "INTEGER" => typeof(long),
            "REAL" => typeof(double),
            "TEXT" => typeof(string),
            "BLOB" => typeof(byte[]),
            _ => typeof(object)
        };
    }

    /// <summary>
    /// 创建 DataAdapter
    /// </summary>
    private DbDataAdapter CreateDataAdapter(DbConnection connection, string dbType)
    {
        return dbType switch
        {
            "SqlServer" => new Microsoft.Data.SqlClient.SqlDataAdapter(),
            "MySql" => new MySqlConnector.MySqlDataAdapter(),
            "PostgreSQL" => new Npgsql.NpgsqlDataAdapter(),
            "Oracle" => new Oracle.ManagedDataAccess.Client.OracleDataAdapter(),
            _ => throw new NotSupportedException($"不支持的数据库类型: {dbType}")
        };
    }

    /// <summary>
    /// 使用明文密码构建连接字符串
    /// </summary>
    private string BuildConnectionStringWithPassword(DataSource dataSource, string plainPassword)
    {
        return dataSource.DbType switch
        {
            "SqlServer" => $"Server={dataSource.ServerAddress},{dataSource.Port};Database={dataSource.DatabaseName};User Id={dataSource.Username};Password={plainPassword};Connection Timeout={dataSource.ConnectionTimeout};TrustServerCertificate=True;",
            "MySql" => $"Server={dataSource.ServerAddress};Port={dataSource.Port};Database={dataSource.DatabaseName};Uid={dataSource.Username};Pwd={plainPassword};Connection Timeout={dataSource.ConnectionTimeout};AllowUserVariables=True;SslMode=None;",
            "PostgreSQL" => $"Host={dataSource.ServerAddress};Port={dataSource.Port};Database={dataSource.DatabaseName};Username={dataSource.Username};Password={plainPassword};Timeout={dataSource.ConnectionTimeout};",
            "Oracle" => $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={dataSource.ServerAddress})(PORT={dataSource.Port}))(CONNECT_DATA=(SERVICE_NAME={dataSource.DatabaseName})));User Id={dataSource.Username};Password={plainPassword};",
            "SQLite" => $"Data Source={dataSource.DatabaseName};",
            _ => throw new NotSupportedException($"不支持的数据库类型: {dataSource.DbType}")
        };
    }

    /// <summary>
    /// 根据连接字符串创建数据库连接
    /// </summary>
    private DbConnection CreateConnectionByConnectionString(string dbType, string connectionString)
    {
        return dbType switch
        {
            "SqlServer" => new Microsoft.Data.SqlClient.SqlConnection(connectionString),
            "MySql" => new MySqlConnector.MySqlConnection(connectionString),
            "PostgreSQL" => new Npgsql.NpgsqlConnection(connectionString),
            "Oracle" => new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString),
            "SQLite" => new Microsoft.Data.Sqlite.SqliteConnection(connectionString),
            _ => throw new NotSupportedException($"不支持的数据库类型: {dbType}")
        };
    }

    /// <summary>
    /// 获取数据库列表
    /// </summary>
    public async Task<ApiResponse<List<string>>> GetDatabasesAsync(string dbType, string connectionString)
    {
        try
        {
            _logger.LogInformation($"获取数据库列表: {dbType}");

            using var connection = CreateConnectionByConnectionString(dbType, connectionString);
            await connection.OpenAsync();

            // 根据数据库类型构建不同的查询
            var query = dbType switch
            {
                "SqlServer" => "SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name",  // 排除系统数据库
                "MySql" => "SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys') ORDER BY schema_name",
                "PostgreSQL" => "SELECT datname FROM pg_database WHERE datistemplate = false ORDER BY datname",
                "Oracle" => "SELECT username FROM all_users WHERE username NOT IN ('SYS', 'SYSTEM', 'DBSNMP', 'SYSMAN', 'OUTLN') ORDER BY username",
                _ => throw new NotSupportedException($"不支持的数据库类型: {dbType}")
            };

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandTimeout = _options.DefaultCommandTimeout;

            var databases = new List<string>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                databases.Add(reader.GetString(0));
            }

            _logger.LogInformation($"获取数据库列表成功: 共 {databases.Count} 个数据库");
            return ApiResponse<List<string>>.Ok(databases, "获取数据库列表成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取数据库列表失败: {ex.Message}");
            return ApiResponse<List<string>>.Fail($"获取数据库列表失败: {ex.Message}", "GET_DATABASES_ERROR");
        }
    }
}
