using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Data.Data;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;
using DataForgeStudio.Shared.Utils;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 数据源服务实现
/// </summary>
public class DataSourceService : IDataSourceService
{
    private readonly DataForgeStudioDbContext _context;
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<DataSourceService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ILicenseService _licenseService;

    public DataSourceService(
        DataForgeStudioDbContext context,
        IDatabaseService databaseService,
        ILogger<DataSourceService> logger,
        IConfiguration configuration,
        ILicenseService licenseService)
    {
        _context = context;
        _databaseService = databaseService;
        _logger = logger;
        _configuration = configuration;
        _licenseService = licenseService;
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

    public async Task<ApiResponse<PagedResponse<DataSourceDto>>> GetDataSourcesAsync(PagedRequest request, string? dataSourceName = null, string? dbType = null, bool includeInactive = true)
    {
        var query = _context.DataSources.AsQueryable();

        // 如果不包含停用的数据源，只查询启用的
        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(dataSourceName))
        {
            query = query.Where(d => d.DataSourceName.Contains(dataSourceName));
        }

        if (!string.IsNullOrWhiteSpace(dbType))
        {
            query = query.Where(d => d.DbType == dbType);
        }

        var totalCount = await query.CountAsync();

        var dataSources = await query
            .OrderByDescending(d => d.CreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DataSourceDto
            {
                DataSourceId = d.DataSourceId,
                DataSourceName = d.DataSourceName,
                DbType = d.DbType,
                ServerAddress = d.ServerAddress,
                Port = d.Port,
                DatabaseName = d.DatabaseName,
                Username = d.Username,
                IsActive = d.IsActive,
                CreatedTime = d.CreatedTime
            })
            .ToListAsync();

        var pagedResponse = new PagedResponse<DataSourceDto>(dataSources, totalCount, request.PageIndex, request.PageSize);
        return ApiResponse<PagedResponse<DataSourceDto>>.Ok(pagedResponse);
    }

    public async Task<ApiResponse<DataSourceDto>> GetDataSourceByIdAsync(int dataSourceId)
    {
        var dataSource = await _context.DataSources
            .Where(d => d.DataSourceId == dataSourceId)
            .Select(d => new DataSourceDto
            {
                DataSourceId = d.DataSourceId,
                DataSourceName = d.DataSourceName,
                DbType = d.DbType,
                ServerAddress = d.ServerAddress,
                Port = d.Port,
                DatabaseName = d.DatabaseName,
                Username = d.Username,
                IsActive = d.IsActive,
                CreatedTime = d.CreatedTime
            })
            .FirstOrDefaultAsync();

        if (dataSource == null)
        {
            return ApiResponse<DataSourceDto>.Fail("数据源不存在", "NOT_FOUND");
        }

        return ApiResponse<DataSourceDto>.Ok(dataSource);
    }

    public async Task<ApiResponse<DataSourceDto>> CreateDataSourceAsync(CreateDataSourceRequest request, int createdBy)
    {
        // 检查许可证数据源数量限制
        var limitCheck = await _licenseService.CheckDataSourceLimitAsync();
        if (!limitCheck.Success)
        {
            return ApiResponse<DataSourceDto>.Fail(limitCheck.Message, limitCheck.ErrorCode);
        }

        // 生成数据源编码
        var code = $"DS_{DateTime.UtcNow:yyyyMMddHHmmss}";

        // 加密密码（使用配置的密钥）
        var (key, iv) = GetEncryptionKeys();
        var encryptedPassword = !string.IsNullOrEmpty(request.Password)
            ? EncryptionHelper.AesEncrypt(request.Password, key, iv)
            : null;

        var dataSource = new DataSource
        {
            DataSourceName = request.DataSourceName,
            DataSourceCode = code,
            DbType = request.DbType,
            ServerAddress = request.Server,
            Port = request.Port,
            DatabaseName = request.Database,
            Username = request.Username,
            Password = encryptedPassword,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedTime = DateTime.UtcNow
        };

        _context.DataSources.Add(dataSource);
        await _context.SaveChangesAsync();

        var dataSourceDto = new DataSourceDto
        {
            DataSourceId = dataSource.DataSourceId,
            DataSourceName = dataSource.DataSourceName,
            DbType = dataSource.DbType,
            ServerAddress = dataSource.ServerAddress,
            Port = dataSource.Port,
            DatabaseName = dataSource.DatabaseName,
            Username = dataSource.Username,
            IsActive = dataSource.IsActive,
            CreatedTime = dataSource.CreatedTime
        };

        return ApiResponse<DataSourceDto>.Ok(dataSourceDto, "数据源创建成功");
    }

    public async Task<ApiResponse> UpdateDataSourceAsync(int dataSourceId, CreateDataSourceRequest request)
    {
        var dataSource = await _context.DataSources.FindAsync(dataSourceId);
        if (dataSource == null)
        {
            return ApiResponse.Fail("数据源不存在", "NOT_FOUND");
        }

        dataSource.DataSourceName = request.DataSourceName;
        dataSource.DbType = request.DbType;
        dataSource.ServerAddress = request.Server;
        dataSource.Port = request.Port;
        dataSource.DatabaseName = request.Database;
        dataSource.Username = request.Username;

        // 只有提供了新密码才更新
        if (!string.IsNullOrEmpty(request.Password))
        {
            var (key, iv) = GetEncryptionKeys();
            dataSource.Password = EncryptionHelper.AesEncrypt(request.Password, key, iv);
        }

        dataSource.UpdatedTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("数据源更新成功");
    }

    public async Task<ApiResponse> DeleteDataSourceAsync(int dataSourceId)
    {
        var dataSource = await _context.DataSources.FindAsync(dataSourceId);
        if (dataSource == null)
        {
            return ApiResponse.Fail("数据源不存在", "NOT_FOUND");
        }

        // 检查是否有报表使用此数据源
        var hasReports = await _context.Reports.AnyAsync(r => r.DataSourceId == dataSourceId);
        if (hasReports)
        {
            return ApiResponse.Fail("该数据源下还有报表，无法删除", "DATASOURCE_IN_USE");
        }

        _context.DataSources.Remove(dataSource);
        await _context.SaveChangesAsync();
        return ApiResponse.Ok("数据源删除成功");
    }

    public async Task<ApiResponse> TestConnectionAsync(int dataSourceId)
    {
        var dataSource = await _context.DataSources.FindAsync(dataSourceId);
        if (dataSource == null)
        {
            return ApiResponse.Fail("数据源不存在", "NOT_FOUND");
        }

        // 验证必要字段
        if (string.IsNullOrWhiteSpace(dataSource.Username))
        {
            return ApiResponse.Fail("数据源配置不完整：用户名为空，请编辑数据源并填写正确的连接信息", "INVALID_CONFIG");
        }

        var result = await _databaseService.TestConnectionAsync(dataSource);
        if (!result.Success)
        {
            // 更新测试失败结果
            dataSource.LastTestTime = DateTime.UtcNow;
            dataSource.LastTestResult = false;
            dataSource.LastTestMessage = result.Message;
            await _context.SaveChangesAsync();

            return ApiResponse.Fail(result.Message, result.ErrorCode);
        }

        // 更新测试结果
        dataSource.LastTestTime = DateTime.UtcNow;
        dataSource.LastTestResult = true;
        dataSource.LastTestMessage = "连接成功";
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("连接测试成功");
    }

    public async Task<ApiResponse> TestConnectionAsync(CreateDataSourceRequest request)
    {
        // 如果数据库名称为空，使用 master 数据库进行测试
        var databaseName = !string.IsNullOrEmpty(request.Database) ? request.Database : "master";

        // 直接构建连接字符串进行测试（密码是明文的）
        var connectionString = request.DbType switch
        {
            "SqlServer" => $"Server={request.Server},{request.Port};Database={databaseName};User Id={request.Username};Password={request.Password};Connection Timeout=30;TrustServerCertificate=True;",
            "MySql" => $"Server={request.Server};Port={request.Port};Database={databaseName};Uid={request.Username};Pwd={request.Password};Connection Timeout=30;AllowUserVariables=True;SslMode=None;",
            "PostgreSQL" => $"Host={request.Server};Port={request.Port};Database={databaseName};Username={request.Username};Password={request.Password};Timeout=30;",
            "Oracle" => $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={request.Server})(PORT={request.Port}))(CONNECT_DATA=(SERVICE_NAME={databaseName})));User Id={request.Username};Password={request.Password};",
            "SQLite" => $"Data Source={databaseName};",
            _ => throw new NotSupportedException($"不支持的数据库类型: {request.DbType}")
        };

        // 创建临时数据源对象用于 DatabaseService
        var tempDataSource = new DataSource
        {
            DbType = request.DbType,
            ServerAddress = request.Server,
            Port = request.Port,
            DatabaseName = databaseName,
            Username = request.Username,
            // 不设置密码，因为我们会使用自定义连接字符串
            Password = null,
            ConnectionTimeout = 30
        };

        // 使用自定义方法测试连接（传入明文密码）
        var result = await _databaseService.TestConnectionWithCredentialsAsync(tempDataSource, request.Password);
        if (!result.Success)
        {
            return ApiResponse.Fail(result.Message, result.ErrorCode);
        }
        return ApiResponse.Ok("连接测试成功");
    }

    public async Task<ApiResponse<List<string>>> GetDatabasesAsync(CreateDataSourceRequest request)
    {
        try
        {
            // 构建连接字符串（不指定数据库）
            var connectionString = request.DbType switch
            {
                "SqlServer" => $"Server={request.Server},{request.Port};Database=master;User Id={request.Username};Password={request.Password};Connection Timeout=30;TrustServerCertificate=True;",
                "MySql" => $"Server={request.Server};Port={request.Port};Database=information_schema;Uid={request.Username};Pwd={request.Password};Connection Timeout=30;AllowUserVariables=True;SslMode=None;",
                "PostgreSQL" => $"Host={request.Server};Port={request.Port};Database=postgres;Username={request.Username};Password={request.Password};Timeout=30;",
                "Oracle" => $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={request.Server})(PORT={request.Port}))(CONNECT_DATA=(SERVICE_NAME=oracl)));User Id={request.Username};Password={request.Password};",
                _ => throw new NotSupportedException($"不支持的数据库类型: {request.DbType}")
            };

            var result = await _databaseService.GetDatabasesAsync(request.DbType, connectionString);
            if (!result.Success)
            {
                return ApiResponse<List<string>>.Fail(result.Message, result.ErrorCode);
            }

            return ApiResponse<List<string>>.Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取数据库列表失败: {ex.Message}");
            return ApiResponse<List<string>>.Fail($"获取数据库列表失败: {ex.Message}", "FETCH_DATABASES_ERROR");
        }
    }

    public async Task<ApiResponse> ToggleActiveAsync(int dataSourceId)
    {
        var dataSource = await _context.DataSources.FindAsync(dataSourceId);
        if (dataSource == null)
        {
            return ApiResponse.Fail("数据源不存在", "NOT_FOUND");
        }

        dataSource.IsActive = !dataSource.IsActive;
        dataSource.UpdatedTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ApiResponse.Ok(dataSource.IsActive ? "数据源已启用" : "数据源已停用");
    }

    public async Task<ApiResponse<List<TableInfoDto>>> GetTableStructureAsync(int dataSourceId)
    {
        var dataSource = await _context.DataSources.FindAsync(dataSourceId);
        if (dataSource == null)
        {
            return ApiResponse<List<TableInfoDto>>.Fail("数据源不存在", "NOT_FOUND");
        }

        if (!dataSource.IsActive)
        {
            return ApiResponse<List<TableInfoDto>>.Fail("数据源已停用", "DATASOURCE_INACTIVE");
        }

        return await _databaseService.GetAllTablesAsync(dataSource);
    }

    private string BuildConnectionString(DataSource dataSource)
    {
        // 解密密码（使用配置的密钥）
        var (key, iv) = GetEncryptionKeys();
        var password = !string.IsNullOrEmpty(dataSource.Password)
            ? EncryptionHelper.AesDecrypt(dataSource.Password, key, iv)
            : "";

        return dataSource.DbType switch
        {
            "SqlServer" => $"Server={dataSource.ServerAddress},{dataSource.Port};Database={dataSource.DatabaseName};User Id={dataSource.Username};Password={password};",
            "MySql" => $"Server={dataSource.ServerAddress};Port={dataSource.Port};Database={dataSource.DatabaseName};Uid={dataSource.Username};Pwd={password};",
            "PostgreSQL" => $"Host={dataSource.ServerAddress};Port={dataSource.Port};Database={dataSource.DatabaseName};Username={dataSource.Username};Password={password};",
            "Oracle" => $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={dataSource.ServerAddress})(PORT={dataSource.Port}))(CONNECT_DATA=(SERVICE_NAME={dataSource.DatabaseName})));User Id={dataSource.Username};Password={password};",
            _ => throw new NotSupportedException($"不支持的数据库类型: {dataSource.DbType}")
        };
    }
}
