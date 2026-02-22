using System.CommandLine;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Configurator;

class Program
{
    private static string? _logPath;
    private static string? _fallbackLogPath;
    private static bool _logEnabled = false;

    static async Task<int> Main(string[] args)
    {
        // 定义命令行参数（安装模式）
        var installPathOption = new Option<string>("--install-path") { Description = "安装路径" };
        var dbServerOption = new Option<string>("--db-server", () => "localhost") { Description = "数据库服务器" };
        var dbPortOption = new Option<int>("--db-port", () => 1433) { Description = "数据库端口" };
        var dbAuthOption = new Option<string>("--db-auth", () => "windows") { Description = "认证方式 (windows/sql)" };
        var dbUserOption = new Option<string>("--db-user", () => "") { Description = "数据库用户名" };
        var dbPasswordOption = new Option<string>("--db-password", () => "") { Description = "数据库密码" };
        var backendPortOption = new Option<int>("--backend-port", () => 5000) { Description = "后端API端口" };
        var frontendPortOption = new Option<int>("--frontend-port", () => 80) { Description = "前端Web端口" };

        // 安装命令
        var installCommand = new Command("install", "执行安装配置")
        {
            installPathOption,
            dbServerOption,
            dbPortOption,
            dbAuthOption,
            dbUserOption,
            dbPasswordOption,
            backendPortOption,
            frontendPortOption
        };

        installCommand.SetHandler(async (context) =>
        {
            var config = new Configuration
            {
                InstallPath = context.ParseResult.GetValueForOption(installPathOption)!,
                DbServer = context.ParseResult.GetValueForOption(dbServerOption),
                DbPort = context.ParseResult.GetValueForOption(dbPortOption),
                DbAuth = context.ParseResult.GetValueForOption(dbAuthOption),
                DbUser = context.ParseResult.GetValueForOption(dbUserOption) ?? "",
                DbPassword = context.ParseResult.GetValueForOption(dbPasswordOption) ?? "",
                BackendPort = context.ParseResult.GetValueForOption(backendPortOption),
                FrontendPort = context.ParseResult.GetValueForOption(frontendPortOption)
            };

            context.ExitCode = await RunConfiguration(config);
        });

        // 验证命令参数（JSON 格式）
        var configOption = new Option<string>("--config") { Description = "JSON 格式配置", IsRequired = true };

        var validateCommand = new Command("validate", "验证配置（返回 JSON 结果）")
        {
            configOption
        };

        validateCommand.SetHandler(async (context) =>
        {
            var configJson = context.ParseResult.GetValueForOption(configOption)!;
            context.ExitCode = await RunValidation(configJson);
        });

        // 兼容旧版：无子命令时使用安装模式
        var rootCommand = new RootCommand("DataForgeStudio 配置器")
        {
            installCommand,
            validateCommand
        };

        // 向后兼容：直接调用时（无子命令）使用安装模式
        rootCommand.SetHandler(async (context) =>
        {
            // 解析旧版参数
            var parseResult = context.ParseResult;

            var config = new Configuration
            {
                InstallPath = parseResult.GetValueForOption(installPathOption) ?? "",
                DbServer = parseResult.GetValueForOption(dbServerOption) ?? "localhost",
                DbPort = parseResult.GetValueForOption(dbPortOption),
                DbAuth = parseResult.GetValueForOption(dbAuthOption) ?? "windows",
                DbUser = parseResult.GetValueForOption(dbUserOption) ?? "",
                DbPassword = parseResult.GetValueForOption(dbPasswordOption) ?? "",
                BackendPort = parseResult.GetValueForOption(backendPortOption),
                FrontendPort = parseResult.GetValueForOption(frontendPortOption)
            };

            if (string.IsNullOrEmpty(config.InstallPath))
            {
                Console.WriteLine("错误: 必须指定 --install-path 参数");
                context.ExitCode = 1;
                return;
            }

            context.ExitCode = await RunConfiguration(config);
        });

        // 添加全局选项到 root
        rootCommand.AddOption(installPathOption);
        rootCommand.AddOption(dbServerOption);
        rootCommand.AddOption(dbPortOption);
        rootCommand.AddOption(dbAuthOption);
        rootCommand.AddOption(dbUserOption);
        rootCommand.AddOption(dbPasswordOption);
        rootCommand.AddOption(backendPortOption);
        rootCommand.AddOption(frontendPortOption);

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// 验证配置（返回 JSON 结果）
    /// </summary>
    static async Task<int> RunValidation(string configJson)
    {
        var result = new ValidationResult();

        try
        {
            var config = JsonSerializer.Deserialize<Configuration>(configJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (config == null)
            {
                result.Errors.Add(new ValidationError("INVALID_CONFIG", "无法解析配置 JSON"));
                OutputValidationResult(result);
                return 1;
            }

            // 1. 验证数据库连接
            await ValidateDatabaseConnection(config, result);

            // 2. 验证端口占用
            ValidatePortAvailability(config, result);

            // 输出结果
            OutputValidationResult(result);
            return result.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ValidationError("VALIDATION_ERROR", $"验证过程发生错误: {ex.Message}"));
            OutputValidationResult(result);
            return 1;
        }
    }

    /// <summary>
    /// 验证数据库连接
    /// </summary>
    static async Task ValidateDatabaseConnection(Configuration config, ValidationResult result)
    {
        try
        {
            var connectionString = GetConnectionString(config, "master");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // 测试查询
            using var cmd = new SqlCommand("SELECT 1", connection);
            await cmd.ExecuteScalarAsync();

            // 检查是否有创建数据库权限
            try
            {
                using var checkCmd = new SqlCommand(
                    "SELECT IS_SRVROLEMEMBER('sysadmin') + IS_MEMBER('dbcreator')",
                    connection);
                var hasPermission = await checkCmd.ExecuteScalarAsync();

                if (hasPermission != null && Convert.ToInt32(hasPermission) == 0)
                {
                    result.Warnings.Add(new ValidationWarning("DB_PERMISSION_WARNING",
                        "当前用户可能没有创建数据库的权限。如果数据库已存在，这不会影响安装。"));
                }
            }
            catch
            {
                // 权限检查失败，忽略
            }
        }
        catch (SqlException ex)
        {
            var errorMsg = ex.Number switch
            {
                2 => $"无法连接到数据库服务器 {config.DbServer}:{config.DbPort} - 网络连接失败",
                53 => $"无法连接到数据库服务器 {config.DbServer}:{config.DbPort} - 服务器不存在或无法访问",
                18456 => "登录失败 - 用户名或密码错误",
                18452 => "登录失败 - 不允许使用该账户进行 SQL Server 身份验证",
                18470 => "登录失败 - 该账户已被禁用",
                _ => $"数据库连接失败: {ex.Message}"
            };

            result.Errors.Add(new ValidationError("DB_CONNECT_FAILED", errorMsg));
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ValidationError("DB_CONNECT_FAILED", $"数据库连接失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 验证端口可用性
    /// </summary>
    static void ValidatePortAvailability(Configuration config, ValidationResult result)
    {
        // 检查后端端口
        var backendInUse = IsPortInUse(config.BackendPort, out var backendProcess);
        if (backendInUse)
        {
            result.Warnings.Add(new ValidationWarning("BACKEND_PORT_IN_USE",
                $"后端端口 {config.BackendPort} 已被占用{(backendProcess != null ? $" ({backendProcess})" : "")}"));
        }

        // 检查前端端口
        var frontendInUse = IsPortInUse(config.FrontendPort, out var frontendProcess);
        if (frontendInUse)
        {
            result.Warnings.Add(new ValidationWarning("FRONTEND_PORT_IN_USE",
                $"前端端口 {config.FrontendPort} 已被占用{(frontendProcess != null ? $" ({frontendProcess})" : "")}"));
        }
    }

    /// <summary>
    /// 检查端口是否被占用
    /// </summary>
    static bool IsPortInUse(int port, out string? processName)
    {
        processName = null;

        try
        {
            using var listener = new TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return false;
        }
        catch
        {
            // 端口被占用，尝试获取进程名
            try
            {
                using var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netstat.exe",
                        Arguments = $"-ano | findstr :{port}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // 解析 PID
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains($":{port}") && line.Contains("LISTENING"))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            if (int.TryParse(parts[^1], out var pid))
                            {
                                try
                                {
                                    var proc = System.Diagnostics.Process.GetProcessById(pid);
                                    processName = $"{proc.ProcessName} (PID: {pid})";
                                }
                                catch { }
                            }
                        }
                        break;
                    }
                }
            }
            catch { }

            return true;
        }
    }

    /// <summary>
    /// 输出验证结果 JSON
    /// </summary>
    static void OutputValidationResult(ValidationResult result)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        Console.WriteLine(JsonSerializer.Serialize(result, options));
    }

    static void InitLogging(string installPath)
    {
        // 主日志路径
        _logPath = Path.Combine(installPath, "logs", "configurator.log");

        // 备用日志路径（桌面）
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        _fallbackLogPath = Path.Combine(desktopPath, "DataForgeStudio_configurator.log");

        // 尝试创建主日志目录
        try
        {
            var logDir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            // 测试写入
            File.AppendAllText(_logPath, "");
            _logEnabled = true;
            Console.WriteLine($"[日志] 日志文件: {_logPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[警告] 无法创建日志文件: {_logPath}");
            Console.WriteLine($"[警告] 错误: {ex.Message}");
            _logEnabled = false;
        }
    }

    static void Log(string message)
    {
        // 始终输出到控制台
        Console.WriteLine(message);

        if (!_logEnabled) return;

        var timestamp = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ";
        var logLine = timestamp + message;

        // 尝试写入主日志
        try
        {
            if (_logPath != null)
            {
                File.AppendAllText(_logPath, logLine + "\n");
            }
        }
        catch
        {
            // 主日志失败，尝试备用日志
            try
            {
                if (_fallbackLogPath != null)
                {
                    File.AppendAllText(_fallbackLogPath, logLine + " [备用日志]\n");
                }
            }
            catch { }
        }
    }

    static async Task<int> RunConfiguration(Configuration config)
    {
        // 初始化日志
        InitLogging(config.InstallPath);

        Log("========================================");
        Log("DataForgeStudio 配置器");
        Log("========================================");
        Log("");
        Log($"安装路径: {config.InstallPath}");
        Log($"数据库服务器: {config.DbServer}:{config.DbPort}");
        Log($"数据库名: {config.DbName}");
        Log($"认证方式: {config.DbAuth}");
        Log($"后端端口: {config.BackendPort}");
        Log($"前端端口: {config.FrontendPort}");
        Log("");

        try
        {
            // 步骤1: 验证安装路径
            Log("[1/5] 验证安装路径...");
            if (!Directory.Exists(config.InstallPath))
            {
                Log($"错误: 安装路径不存在: {config.InstallPath}");
                return 1;
            }
            Log("✓ 安装路径验证通过");

            // 步骤2: 生成配置文件
            Log("[2/5] 生成配置文件...");
            GenerateAppSettings(config);
            GenerateNginxConfig(config);
            Log("✓ 配置文件生成完成");

            // 步骤3: 初始化数据库
            Log("[3/5] 初始化数据库...");
            await InitializeDatabase(config);
            Log("✓ 数据库初始化完成");

            // 步骤4: 注册 Windows 服务
            Log("[4/5] 注册 Windows 服务...");
            RegisterWindowsService(config);
            Log("✓ Windows 服务注册完成");

            // 步骤5: 创建桌面快捷方式
            Log("[5/5] 创建桌面快捷方式...");
            CreateDesktopShortcut(config);
            Log("✓ 桌面快捷方式创建完成");

            Log("");
            Log("========================================");
            Log("配置完成!");
            Log($"安装路径: {config.InstallPath}");
            Log($"请启动服务后登录系统，首次登录需要设置密码。");
            Log("========================================");

            return 0;
        }
        catch (Exception ex)
        {
            Log($"错误: {ex.Message}");
            Log($"堆栈跟踪: {ex.StackTrace}");
            return 1;
        }
    }

    static string GetConnectionString(Configuration config, string database)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = config.DbPort == 1433
                ? $"tcp:{config.DbServer}"
                : $"tcp:{config.DbServer},{config.DbPort}",
            InitialCatalog = database,
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };

        if (config.DbAuth.Equals("windows", StringComparison.OrdinalIgnoreCase))
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = config.DbUser;
            builder.Password = config.DbPassword;
        }

        return builder.ConnectionString;
    }

    static void GenerateAppSettings(Configuration config)
    {
        var appSettingsPath = Path.Combine(config.InstallPath, "Server", "appsettings.json");
        var connectionString = GetConnectionString(config, config.DbName);
        var masterConnectionString = GetConnectionString(config, "master");

        var appSettings = $$"""
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "{{connectionString.Replace("\\", "\\\\").Replace("\"", "\\\"")}}",
    "MasterConnection": "{{masterConnectionString.Replace("\\", "\\\\").Replace("\"", "\\\"")}}"
  },
  "Kestrel": { "Endpoints": { "Http": { "Url": "http://*:{{config.BackendPort}}" } } },
  "Cors": { "AllowedOrigins": ["http://localhost", "http://localhost:{{config.FrontendPort}}", "http://*"] },
  "Security": { "UseDefaultsForTesting": false }
}
""";
        File.WriteAllText(appSettingsPath, appSettings);
    }

    static void GenerateNginxConfig(Configuration config)
    {
        var nginxConfPath = Path.Combine(config.InstallPath, "WebServer", "conf", "nginx.conf");
        var nginxConf = $$"""
worker_processes  1;
events { worker_connections  1024; }
http {
    include       mime.types;
    default_type  application/octet-stream;
    sendfile        on;
    keepalive_timeout  65;

    server {
        listen       {{config.FrontendPort}};
        server_name  localhost;

        location / {
            root   ../WebSite;
            index  index.html index.htm;
            try_files $uri $uri/ /index.html;
        }

        location /api/ {
            proxy_pass         http://127.0.0.1:{{config.BackendPort}}/api/;
            proxy_http_version 1.1;
            proxy_set_header   Upgrade $http_upgrade;
            proxy_set_header   Connection keep-alive;
            proxy_set_header   Host $host;
            proxy_cache_bypass $http_upgrade;
            proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header   X-Forwarded-Proto $scheme;
        }

        error_page   500 502 503 504  /50x.html;
        location = /50x.html { root   html; }
    }
}
""";
        File.WriteAllText(nginxConfPath, nginxConf);
    }

    static async Task InitializeDatabase(Configuration config)
    {
        var masterConnectionString = GetConnectionString(config, "master");

        using var masterConnection = new SqlConnection(masterConnectionString);
        await masterConnection.OpenAsync();

        // 检查数据库是否存在
        var checkDbCmd = new SqlCommand(
            "SELECT COUNT(*) FROM sys.databases WHERE name = @dbName",
            masterConnection);
        checkDbCmd.Parameters.AddWithValue("@dbName", config.DbName);
        var dbExists = Convert.ToInt32(await checkDbCmd.ExecuteScalarAsync()) > 0;

        if (dbExists)
        {
            Console.WriteLine("  数据库已存在，跳过创建");
            return;
        }

        // 确保数据库文件目录存在
        var dbServerPath = Path.Combine(config.InstallPath, "DBServer");
        if (!Directory.Exists(dbServerPath))
        {
            Directory.CreateDirectory(dbServerPath);
            Console.WriteLine($"  创建数据库目录: {dbServerPath}");
        }

        // 数据库文件路径
        var mdfPath = Path.Combine(dbServerPath, $"{config.DbName}.mdf");
        var ldfPath = Path.Combine(dbServerPath, $"{config.DbName}_log.ldf");

        // 创建数据库（指定文件路径）
        Console.WriteLine($"  创建数据库 {config.DbName}...");
        Console.WriteLine($"  数据文件: {mdfPath}");
        Console.WriteLine($"  日志文件: {ldfPath}");

        var createDbSql = $@"
CREATE DATABASE [{config.DbName}]
ON PRIMARY
(
    NAME = N'{config.DbName}',
    FILENAME = N'{mdfPath.Replace("'", "''")}',
    SIZE = 10MB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 10%
)
LOG ON
(
    NAME = N'{config.DbName}_log',
    FILENAME = N'{ldfPath.Replace("'", "''")}',
    SIZE = 5MB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 10%
)";

        var createDbCmd = new SqlCommand(createDbSql, masterConnection);
        await createDbCmd.ExecuteNonQueryAsync();

        // 切换到新数据库并创建表结构
        masterConnection.ChangeDatabase(config.DbName);

        // 创建表结构
        await ExecuteSqlScriptAsync(masterConnection, GetCreateTablesSql());

        // 插入初始数据
        await ExecuteSqlScriptAsync(masterConnection, GetSeedDataSql());

        Console.WriteLine("  数据库初始化完成");
    }

    static async Task ExecuteSqlScriptAsync(SqlConnection connection, string script)
    {
        var batches = script.Split(new[] { "\nGO", "\ngo", "\r\nGO", "\r\ngo" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var batch in batches)
        {
            var sql = batch.Trim();
            if (string.IsNullOrWhiteSpace(sql)) continue;

            try
            {
                using var cmd = new SqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  SQL 执行警告: {ex.Message}");
            }
        }
    }

    static string GetCreateTablesSql()
    {
        return @"
-- Users Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users](
        [UserId] [int] IDENTITY(1,1) NOT NULL,
        [Username] [nvarchar](50) NOT NULL,
        [PasswordHash] [nvarchar](256) NOT NULL,
        [RealName] [nvarchar](50) NULL,
        [Email] [nvarchar](100) NULL,
        [Phone] [nvarchar](20) NULL,
        [Department] [nvarchar](100) NULL,
        [Position] [nvarchar](50) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [IsLocked] [bit] NOT NULL DEFAULT 0,
        [LastLoginTime] [datetime] NULL,
        [LastLoginIP] [nvarchar](50) NULL,
        [PasswordFailCount] [int] NOT NULL DEFAULT 0,
        [MustChangePassword] [bit] NOT NULL DEFAULT 0,
        [Remark] [nvarchar](500) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [UQ_Users_Username] UNIQUE NONCLUSTERED ([Username] ASC),
        CONSTRAINT [CK_Users_IsSystem] CHECK ([IsSystem] = 0 OR [Username] = 'root')
    );
END

-- Roles Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Roles](
        [RoleId] [int] IDENTITY(1,1) NOT NULL,
        [RoleName] [nvarchar](50) NOT NULL,
        [RoleCode] [nvarchar](50) NOT NULL,
        [Description] [nvarchar](200) NULL,
        [Permissions] [nvarchar](max) NULL,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC),
        CONSTRAINT [UQ_Roles_RoleCode] UNIQUE NONCLUSTERED ([RoleCode] ASC)
    );
END

-- UserRoles Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserRoles](
        [UserRoleId] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NOT NULL,
        [RoleId] [int] NOT NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserRoleId] ASC),
        CONSTRAINT [UQ_UserRoles_User_Role] UNIQUE NONCLUSTERED ([UserId] ASC, [RoleId] ASC)
    );
END

-- RolePermissions Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolePermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RolePermissions](
        [PermissionId] [int] IDENTITY(1,1) NOT NULL,
        [RoleId] [int] NOT NULL,
        [PermissionCode] [nvarchar](100) NOT NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([PermissionId] ASC),
        CONSTRAINT [UQ_RolePermissions_Role_Code] UNIQUE NONCLUSTERED ([RoleId] ASC, [PermissionCode] ASC)
    );
END

-- Permissions Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Permissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Permissions](
        [PermissionId] [int] IDENTITY(1,1) NOT NULL,
        [PermissionName] [nvarchar](50) NOT NULL,
        [PermissionCode] [nvarchar](100) NOT NULL,
        [Category] [nvarchar](50) NOT NULL,
        [Description] [nvarchar](200) NULL,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([PermissionId] ASC),
        CONSTRAINT [UQ_Permissions_Code] UNIQUE NONCLUSTERED ([PermissionCode] ASC)
    );
END

-- DataSources Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataSources]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DataSources](
        [DataSourceId] [int] IDENTITY(1,1) NOT NULL,
        [DataSourceName] [nvarchar](100) NOT NULL,
        [DatabaseType] [nvarchar](50) NOT NULL,
        [ConnectionString] [nvarchar](500) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsDefault] [bit] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_DataSources] PRIMARY KEY CLUSTERED ([DataSourceId] ASC)
    );
END

-- Reports Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Reports]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Reports](
        [ReportId] [int] IDENTITY(1,1) NOT NULL,
        [ReportName] [nvarchar](100) NOT NULL,
        [ReportCode] [nvarchar](50) NOT NULL,
        [DataSourceId] [int] NULL,
        [SqlText] [nvarchar](max) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [Category] [nvarchar](50) NULL,
        [FieldConfig] [nvarchar](max) NULL,
        [ParamConfig] [nvarchar](max) NULL,
        [CacheDuration] [int] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_Reports] PRIMARY KEY CLUSTERED ([ReportId] ASC),
        CONSTRAINT [UQ_Reports_Code] UNIQUE NONCLUSTERED ([ReportCode] ASC)
    );
END

-- Licenses Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Licenses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Licenses](
        [LicenseId] [int] IDENTITY(1,1) NOT NULL,
        [LicenseKey] [nvarchar](max) NOT NULL,
        [MachineCode] [nvarchar](100) NULL,
        [LicenseType] [nvarchar](50) NOT NULL,
        [ProductName] [nvarchar](100) NOT NULL,
        [LicensedTo] [nvarchar](200) NULL,
        [MaxUsers] [int] NOT NULL DEFAULT 0,
        [Features] [nvarchar](max) NULL,
        [IssuedAt] [datetime] NOT NULL,
        [ExpiresAt] [datetime] NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [ActivatedAt] [datetime] NULL,
        [CreatedAt] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] [datetime] NULL,
        CONSTRAINT [PK_Licenses] PRIMARY KEY CLUSTERED ([LicenseId] ASC)
    );
END

-- OperationLogs Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OperationLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OperationLogs](
        [LogId] [bigint] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NULL,
        [Username] [nvarchar](50) NULL,
        [Operation] [nvarchar](100) NOT NULL,
        [Module] [nvarchar](50) NULL,
        [Target] [nvarchar](200) NULL,
        [RequestMethod] [nvarchar](10) NULL,
        [RequestPath] [nvarchar](500) NULL,
        [RequestParams] [nvarchar](max) NULL,
        [ResponseStatus] [int] NULL,
        [IpAddress] [nvarchar](50) NULL,
        [UserAgent] [nvarchar](500) NULL,
        [Duration] [int] NULL,
        [ErrorMessage] [nvarchar](max) NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_OperationLogs] PRIMARY KEY CLUSTERED ([LogId] ASC)
    );
END

-- TrialRecords Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrialRecords]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TrialRecords](
        [TrialRecordId] [int] IDENTITY(1,1) NOT NULL,
        [MachineCode] [nvarchar](100) NOT NULL,
        [FirstRunTime] [datetime] NOT NULL,
        [CreatedAt] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_TrialRecords] PRIMARY KEY CLUSTERED ([TrialRecordId] ASC),
        CONSTRAINT [UQ_TrialRecords_MachineCode] UNIQUE NONCLUSTERED ([MachineCode] ASC)
    );
END
";
    }

    static string GetSeedDataSql()
    {
        return @"
-- 插入默认权限
INSERT INTO [Permissions] (PermissionName, PermissionCode, Category, Description, SortOrder) VALUES
(N'用户管理', 'user:manage', N'用户权限', N'管理用户账户', 1),
(N'角色管理', 'role:manage', N'用户权限', N'管理角色', 2),
(N'报表管理', 'report:manage', N'报表权限', N'管理报表', 3),
(N'报表查询', 'report:view', N'报表权限', N'查询报表', 4),
(N'数据源管理', 'datasource:manage', N'系统设置', N'管理数据源', 5),
(N'系统设置', 'system:settings', N'系统设置', N'系统设置', 6),
(N'操作日志', 'log:view', N'系统设置', N'查看操作日志', 7),
(N'许可证管理', 'license:manage', N'系统设置', N'管理许可证', 8);

-- 插入超级管理员角色
INSERT INTO [Roles] (RoleName, RoleCode, Description, IsSystem, SortOrder, IsActive) VALUES
(N'超级管理员', 'admin', N'拥有所有权限', 1, 1, 1);

-- 为超级管理员分配所有权限
INSERT INTO [RolePermissions] (RoleId, PermissionCode)
SELECT r.RoleId, p.PermissionCode
FROM [Roles] r, [Permissions] p
WHERE r.RoleCode = 'admin';

-- 插入 root 用户 (默认密码: Admin@123，首次登录必须修改)
INSERT INTO [Users] (Username, PasswordHash, RealName, IsActive, IsSystem, MustChangePassword) VALUES
('root', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.VTtYA/7.J6LlZy', N'系统管理员', 1, 1, 1);

-- 为 root 用户分配超级管理员角色
INSERT INTO [UserRoles] (UserId, RoleId)
SELECT u.UserId, r.RoleId
FROM [Users] u, [Roles] r
WHERE u.Username = 'root' AND r.RoleCode = 'admin';
";
    }

    static void RegisterWindowsService(Configuration config)
    {
        var serverExePath = Path.Combine(config.InstallPath, "Server", "DataForgeStudio.Api.exe");
        var serviceName = "DFAppService";

        // 检查服务是否已存在
        var checkInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"query \"{serviceName}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using (var checkProcess = System.Diagnostics.Process.Start(checkInfo))
        {
            if (checkProcess != null)
            {
                checkProcess.WaitForExit();
                if (checkProcess.ExitCode == 0)
                {
                    Console.WriteLine("  Windows 服务已存在，跳过注册");
                    return;
                }
            }
        }

        // 创建服务
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"create \"{serviceName}\" binPath= \"{serverExePath}\" start= auto DisplayName= \"DataForgeStudio API\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using (var process = System.Diagnostics.Process.Start(startInfo))
        {
            if (process == null)
            {
                throw new Exception("无法启动 sc.exe 进程");
            }
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new Exception($"创建 Windows 服务失败: {error}");
            }
        }

        // 设置服务描述
        var descInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"description \"{serviceName}\" \"DataForgeStudio 报表管理系统 API 服务\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var descProcess = System.Diagnostics.Process.Start(descInfo);
        descProcess?.WaitForExit();
    }

    static void CreateDesktopShortcut(Configuration config)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var shortcutPath = Path.Combine(desktopPath, "DataForgeStudio 管理工具.lnk");
        var managerPath = Path.Combine(config.InstallPath, "Manager", "DeployManager.exe");

        var script = $@"
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
$Shortcut.TargetPath = '{managerPath}'
$Shortcut.WorkingDirectory = '{config.InstallPath}'
$Shortcut.Description = 'DataForgeStudio 管理工具'
$Shortcut.Save()
";

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"").Replace("\r\n", " ")}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process?.WaitForExit();
    }
}

class Configuration
{
    public string InstallPath { get; set; } = "";
    public string DbServer { get; set; } = "localhost";
    public int DbPort { get; set; } = 1433;
    public string DbName { get; set; } = "DataForgeStudio";
    public string DbAuth { get; set; } = "windows";
    public string DbUser { get; set; } = "";
    public string DbPassword { get; set; } = "";
    public int BackendPort { get; set; } = 5000;
    public int FrontendPort { get; set; } = 80;
}

/// <summary>
/// 验证结果
/// </summary>
class ValidationResult
{
    public bool Success => Errors.Count == 0;
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
}

/// <summary>
/// 验证错误（阻止安装）
/// </summary>
class ValidationError
{
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";

    public ValidationError(string code, string message)
    {
        Code = code;
        Message = message;
    }
}

/// <summary>
/// 验证警告（可继续）
/// </summary>
class ValidationWarning
{
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";

    public ValidationWarning(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
