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
    /// 获取加密密钥（优先从环境变量读取）
    /// </summary>
    private (string key, string iv) GetEncryptionKeys()
    {
        var key = Environment.GetEnvironmentVariable("DFS_ENCRYPTION_AESKEY")
            ?? _configuration["Security:Encryption:AesKey"]
            ?? throw new InvalidOperationException("加密密钥未配置。请设置环境变量 DFS_ENCRYPTION_AESKEY");
        var iv = Environment.GetEnvironmentVariable("DFS_ENCRYPTION_AESIV")
            ?? _configuration["Security:Encryption:AesIV"]
            ?? throw new InvalidOperationException("加密IV未配置。请设置环境变量 DFS_ENCRYPTION_AESIV");
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
            "SqlServer" => ConnectionStringBuilder.BuildSqlServerConnectionString(
                dataSource.ServerAddress, dataSource.Port,
                dataSource.DatabaseName ?? "master",
                dataSource.Username ?? "", password,
                dataSource.ConnectionTimeout),
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
            "SqlServer" => ConnectionStringBuilder.BuildSqlServerConnectionString(
                dataSource.ServerAddress, dataSource.Port,
                dataSource.DatabaseName ?? "master",
                dataSource.Username ?? "", plainPassword,
                dataSource.ConnectionTimeout),
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

    /// <summary>
    /// 映射 SQL 数据类型到系统数据类型
    /// </summary>
    private string MapSystemDataType(string sqlDataType)
    {
        var type = sqlDataType?.ToLower() ?? "";

        // 字符串类型
        if (type.Contains("char") || type.Contains("text") || type.Contains("nvarchar"))
            return "String";

        // 数值类型
        if (type.Contains("int") || type.Contains("decimal") ||
            type.Contains("numeric") || type.Contains("float") ||
            type.Contains("real") || type.Contains("money") ||
            type.Contains("smallint") || type.Contains("bigint") ||
            type.Contains("tinyint"))
            return "Number";

        // 日期类型
        if (type.Contains("date") || type.Contains("time"))
            return "DateTime";

        // 布尔类型 (bit: SQL Server, bool/boolean: MySQL/PostgreSQL, yesno: Access)
        if (type.Contains("bit") || type.Equals("bool") || type.Equals("boolean") || type.Contains("yesno") || type.Contains("logical"))
            return "Boolean";

        return "String"; // 默认字符串
    }

    /// <summary>
    /// 检测字段是否可能为布尔类型（基于命名约定）
    /// 用于处理 tinyint/int 存储但逻辑上是布尔值的字段
    /// </summary>
    private bool IsLikelyBooleanField(string fieldName, string sqlDataType)
    {
        if (string.IsNullOrEmpty(fieldName))
            return false;

        var name = fieldName.ToLower();
        var type = sqlDataType?.ToLower() ?? "";

        // 只对 tinyint 或 smallint 类型进行布尔推断
        if (!type.Contains("tinyint") && !type.Contains("smallint"))
            return false;

        // 布尔字段常见命名模式
        var booleanPrefixes = new[] { "is", "has", "can", "should", "will", "was", "were", "does", "did", "allow", "enable", "support" };
        var booleanSuffixes = new[] { "flag", "enabled", "disabled", "active", "visible", "locked", "deleted", "published", "approved", "verified", "confirmed" };
        var booleanNames = new[] { "active", "enabled", "visible", "locked", "deleted", "published", "approved", "verified", "confirmed", "status" };

        // 检查前缀
        foreach (var prefix in booleanPrefixes)
        {
            if (name.StartsWith(prefix) && name.Length > prefix.Length)
                return true;
        }

        // 检查后缀
        foreach (var suffix in booleanSuffixes)
        {
            if (name.EndsWith(suffix) && name.Length > suffix.Length)
                return true;
        }

        // 检查完整名称
        foreach (var booleanName in booleanNames)
        {
            if (name.Equals(booleanName))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 获取表结构信息
    /// </summary>
    public async Task<ApiResponse<List<TableColumnDto>>> GetTableStructureAsync(DataSource dataSource, string tableName)
    {
        try
        {
            // 验证表名安全性（防止 SQL 注入）
            SqlTableNameValidator.ValidateAndThrow(tableName, _logger);

            _logger.LogInformation($"获取表结构: {dataSource.DbType} - {tableName}");

            using var connection = CreateConnection(dataSource);
            await connection.OpenAsync();

            // 根据数据库类型构建不同的查询
            var query = dataSource.DbType switch
            {
                "SqlServer" => @"
                    SELECT
                        c.name,
                        t.name,
                        c.max_length,
                        c.is_nullable,
                        CAST(
                            CASE WHEN pk.column_id IS NOT NULL THEN 'PK' ELSE '' END +
                            CASE WHEN ic.is_identity = 1 THEN ',IDENTITY' ELSE '' END
                        AS NVARCHAR(100)),
                        c.column_id
                    FROM sys.columns c
                    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                    LEFT JOIN (
                        SELECT i.column_id, i.object_id
                        FROM sys.indexes i
                        INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                        WHERE i.is_primary_key = 1
                    ) pk ON pk.column_id = c.column_id AND pk.object_id = c.object_id
                    LEFT JOIN sys.identity_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    WHERE c.object_id = OBJECT_ID(@tableName)
                    ORDER BY c.column_id",
                "MySql" => @"
                    SELECT
                        COLUMN_NAME,
                        DATA_TYPE,
                        CHARACTER_MAXIMUM_LENGTH,
                        IS_NULLABLE,
                        COLUMN_KEY,
                        ORDINAL_POSITION
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @tableName
                    ORDER BY ORDINAL_POSITION",
                "PostgreSQL" => @"
                    SELECT
                        a.attname as column_name,
                        pg_catalog.format_type(a.atttypid, a.atttypmod) as data_type,
                        CASE WHEN a.atttypmod > 0 THEN a.atttypmod - 4 ELSE NULL END as max_length,
                        NOT a.attnotnull as is_nullable,
                        CASE WHEN pk.contype = 'p' THEN 'PK' ELSE '' END as column_key,
                        a.attnum as position
                    FROM pg_attribute a
                    LEFT JOIN pg_constraint pk ON pk.conrelid = a.attrelid AND pk.conkey[1] = a.attnum AND pk.contype = 'p'
                    WHERE a.attrelid = @tableName::regclass AND a.attnum > 0 AND NOT a.attisdropped
                    ORDER BY a.attnum",
                "Oracle" => @"
                    SELECT
                        cols.column_name,
                        cols.data_type,
                        CASE
                            WHEN cols.char_length > 0 THEN cols.char_length
                            WHEN cols.data_precision IS NOT NULL THEN cols.data_precision
                            ELSE NULL
                        END as max_length,
                        CASE WHEN cols.nullable = 'Y' THEN 1 ELSE 0 END as is_nullable,
                        CASE WHEN pk.constraint_type = 'P' THEN 'PK' ELSE '' END as column_key,
                        cols.column_id as position
                    FROM all_tab_columns cols
                    LEFT JOIN all_cons_columns cc ON cc.owner = cols.owner AND cc.table_name = cols.table_name AND cc.column_name = cols.column_name
                    LEFT JOIN all_constraints pk ON pk.owner = cols.owner AND pk.constraint_name = cc.constraint_name AND pk.constraint_type = 'P'
                    WHERE cols.table_name = UPPER(@tableName)
                    ORDER BY cols.column_id",
                "SQLite" => @"
                    SELECT
                        name,
                        type,
                        0,
                        CASE WHEN [notnull] = 0 THEN 1 ELSE 0 END,
                        CASE WHEN pk = 1 THEN 'PK' ELSE '' END,
                        cid
                    FROM pragma_table_info(@tableName)
                    ORDER BY cid",
                _ => throw new NotSupportedException($"不支持的数据库类型: {dataSource.DbType}")
            };

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandTimeout = _options.DefaultCommandTimeout;

            // 添加表名参数
            var param = command.CreateParameter();
            param.ParameterName = "@tableName";
            param.Value = tableName;
            command.Parameters.Add(param);

            var columns = new List<TableColumnDto>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var column = new TableColumnDto
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    MaxLength = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                    IsNullable = dataSource.DbType == "Oracle" ? reader.GetBoolean(3) : reader.GetString(3) == "YES" || reader.GetBoolean(3),
                    ColumnProperty = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Position = reader.GetInt32(5),
                    SystemDataType = MapSystemDataType(reader.GetString(1))
                };
                columns.Add(column);
            }

            _logger.LogInformation($"获取表结构成功: {tableName}, 共 {columns.Count} 个字段");
            return ApiResponse<List<TableColumnDto>>.Ok(columns, "获取表结构成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取表结构失败: {ex.Message}");
            return ApiResponse<List<TableColumnDto>>.Fail($"获取表结构失败: {ex.Message}", "GET_TABLE_STRUCTURE_ERROR");
        }
    }

    /// <summary>
    /// 获取所有表及其列信息（用于SQL编辑器自动补全）
    /// 优化：使用单次查询获取所有表和列，避免 N+1 查询问题
    /// </summary>
    public async Task<ApiResponse<List<TableInfoDto>>> GetAllTablesAsync(DataSource dataSource)
    {
        try
        {
            _logger.LogInformation($"获取所有表: {dataSource.DbType}");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            using var connection = CreateConnection(dataSource);
            await connection.OpenAsync();

            // 单次查询获取所有表和列信息（避免 N+1 查询）
            var tablesMap = new Dictionary<string, List<TableColumnInfoDto>>();

            if (dataSource.DbType == "SQLite")
            {
                // SQLite 需要特殊处理：先获取表列表，再批量获取列
                var tableNames = new List<string>();

                using (var tableCommand = connection.CreateCommand())
                {
                    tableCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
                    using var reader = await tableCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                }

                // 批量获取所有表的列（仍然需要多次查询，但 SQLite 通常表较少）
                foreach (var tableName in tableNames)
                {
                    try
                    {
                        var columns = await GetTableColumnsForAutocompleteAsync(connection, dataSource.DbType, tableName);
                        tablesMap[tableName] = columns;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"获取表 {tableName} 的列信息失败，跳过");
                        tablesMap[tableName] = new List<TableColumnInfoDto>();
                    }
                }
            }
            else
            {
                // 其他数据库：使用单次查询获取所有表和列
                var columnsQuery = dataSource.DbType switch
                {
                    "SqlServer" => @"SELECT t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE
                        FROM INFORMATION_SCHEMA.TABLES t
                        LEFT JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME
                        WHERE t.TABLE_TYPE = 'BASE TABLE'
                        ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION",
                    "MySql" => @"SELECT t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE
                        FROM INFORMATION_SCHEMA.TABLES t
                        LEFT JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME AND c.TABLE_SCHEMA = DATABASE()
                        WHERE t.TABLE_SCHEMA = DATABASE() AND t.TABLE_TYPE = 'BASE TABLE'
                        ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION",
                    "PostgreSQL" => @"SELECT t.table_name, c.column_name, c.data_type
                        FROM information_schema.tables t
                        LEFT JOIN information_schema.columns c ON t.table_name = c.table_name AND c.table_schema = 'public'
                        WHERE t.table_schema = 'public' AND t.table_type = 'BASE TABLE'
                        ORDER BY t.table_name, c.ordinal_position",
                    "Oracle" => @"SELECT t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE
                        FROM USER_TABLES t
                        LEFT JOIN USER_TAB_COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME
                        ORDER BY t.TABLE_NAME, c.COLUMN_ID",
                    _ => throw new NotSupportedException($"不支持的数据库类型: {dataSource.DbType}")
                };

                using var command = connection.CreateCommand();
                command.CommandText = columnsQuery;
                command.CommandTimeout = _options.DefaultCommandTimeout;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var tableName = reader.GetString(0);
                    var columnName = reader.IsDBNull(1) ? null : reader.GetString(1);
                    var dataType = reader.IsDBNull(2) ? null : reader.GetString(2);

                    if (!tablesMap.ContainsKey(tableName))
                    {
                        tablesMap[tableName] = new List<TableColumnInfoDto>();
                    }

                    if (!string.IsNullOrEmpty(columnName))
                    {
                        tablesMap[tableName].Add(new TableColumnInfoDto
                        {
                            ColumnName = columnName,
                            DataType = dataType ?? "unknown"
                        });
                    }
                }
            }

            // 转换为结果列表
            var tables = tablesMap
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new TableInfoDto
                {
                    TableName = kvp.Key,
                    Columns = kvp.Value
                })
                .ToList();

            stopwatch.Stop();
            _logger.LogInformation($"获取所有表成功: 共 {tables.Count} 个表，耗时 {stopwatch.ElapsedMilliseconds}ms");
            return ApiResponse<List<TableInfoDto>>.Ok(tables, "获取表列表成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取所有表失败: {ex.Message}");
            return ApiResponse<List<TableInfoDto>>.Fail($"获取表列表失败: {ex.Message}", "GET_TABLES_ERROR");
        }
    }

    /// <summary>
    /// 获取查询的字段结构信息（不返回数据行）
    /// </summary>
    public async Task<ApiResponse<List<FieldSchemaDto>>> GetQuerySchemaAsync(
        DataSource dataSource,
        string sql,
        Dictionary<string, object>? parameters)
    {
        try
        {
            _logger.LogInformation($"获取查询结构: {dataSource.DbType}");

            using var connection = CreateConnection(dataSource);
            await connection.OpenAsync();

            // 根据数据库类型调整 SQL 以只获取元数据
            var schemaSql = dataSource.DbType switch
            {
                "SqlServer" => $"SET FMTONLY ON; {sql}; SET FMTONLY OFF;",
                "MySql" => sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase)
                    ? sql.Replace(" WHERE ", " WHERE 1=0 AND ", StringComparison.OrdinalIgnoreCase)
                    : sql + " WHERE 1=0",
                "PostgreSQL" => sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase)
                    ? sql.Replace(" WHERE ", " WHERE 1=0 AND ", StringComparison.OrdinalIgnoreCase)
                    : sql + " WHERE 1=0",
                "Oracle" => sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase)
                    ? sql.Replace(" WHERE ", " WHERE 1=0 AND ", StringComparison.OrdinalIgnoreCase)
                    : sql + " WHERE 1=0",
                "SQLite" => sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase)
                    ? sql.Replace(" WHERE ", " WHERE 1=0 AND ", StringComparison.OrdinalIgnoreCase)
                    : sql + " WHERE 1=0",
                _ => sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase)
                    ? sql.Replace(" WHERE ", " WHERE 1=0 AND ", StringComparison.OrdinalIgnoreCase)
                    : sql + " WHERE 1=0"
            };

            using var command = CreateCommand(schemaSql, connection, parameters);
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly);

            var fields = new List<FieldSchemaDto>();

            // 获取列结构
            var schemaTable = reader.GetColumnSchema();
            foreach (var column in schemaTable)
            {
                var sqlDataType = column.DataTypeName ?? "unknown";
                var systemDataType = MapSystemDataType(sqlDataType);

                // 如果 SQL 类型名检测不到布尔，使用 .NET 类型作为备用检测
                if (systemDataType == "String" && column.DataType == typeof(bool))
                {
                    systemDataType = "Boolean";
                }

                // 对于 tinyint/int 类型的字段，检查是否为布尔类型（基于命名约定）
                // 例如：isSale, hasPermission, canEdit, isActive, enabled, deleted 等
                if (systemDataType == "Number" && IsLikelyBooleanField(column.ColumnName, sqlDataType))
                {
                    systemDataType = "Boolean";
                }

                fields.Add(new FieldSchemaDto
                {
                    FieldName = column.ColumnName,
                    SqlDataType = sqlDataType,
                    SystemDataType = systemDataType
                });
            }

            _logger.LogInformation($"获取查询结构成功: 共 {fields.Count} 个字段");
            return ApiResponse<List<FieldSchemaDto>>.Ok(fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取查询结构失败: {ex.Message}");
            return ApiResponse<List<FieldSchemaDto>>.Fail($"获取查询结构失败: {ex.Message}", "SCHEMA_ERROR");
        }
    }

    /// <summary>
    /// 获取表的列信息（用于自动补全）
    /// </summary>
    private async Task<List<TableColumnInfoDto>> GetTableColumnsForAutocompleteAsync(DbConnection connection, string dbType, string tableName)
    {
        var columnQuery = dbType switch
        {
            "SqlServer" => "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName ORDER BY ORDINAL_POSITION",
            "MySql" => "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName ORDER BY ORDINAL_POSITION",
            "PostgreSQL" => "SELECT column_name, data_type FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @tableName ORDER BY ordinal_position",
            "Oracle" => "SELECT COLUMN_NAME, DATA_TYPE FROM USER_TAB_COLUMNS WHERE TABLE_NAME = UPPER(@tableName) ORDER BY COLUMN_ID",
            "SQLite" => "SELECT name, type FROM pragma_table_info(@tableName) ORDER BY cid",
            _ => throw new NotSupportedException($"不支持的数据库类型: {dbType}")
        };

        using var command = connection.CreateCommand();
        command.CommandText = columnQuery;

        var param = command.CreateParameter();
        param.ParameterName = "@tableName";
        param.Value = tableName;
        command.Parameters.Add(param);

        var columns = new List<TableColumnInfoDto>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new TableColumnInfoDto
            {
                ColumnName = reader.GetString(0),
                DataType = reader.GetString(1)
            });
        }

        return columns;
    }
}
