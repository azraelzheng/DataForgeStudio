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
            RegisterWebService(config);
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
worker_processes  auto;
error_log  logs/error.log warn;
pid        logs/nginx.pid;

events {
    worker_connections  1024;
    multi_accept on;
}

http {
    include       mime.types;
    default_type  application/octet-stream;

    # 性能优化
    sendfile        on;
    tcp_nopush      on;
    tcp_nodelay     on;
    keepalive_timeout  65;
    types_hash_max_size 2048;

    # 日志格式
    log_format  main  '$remote_addr - $remote_user [$time_local] "$request" '
                      '$status $body_bytes_sent "$http_referer" '
                      '"$http_user_agent" "$http_x_forwarded_for"';
    access_log  logs/access.log  main;

    # Gzip 压缩
    gzip  on;
    gzip_vary on;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types text/plain text/css text/xml application/json application/javascript application/rss+xml application/atom+xml image/svg+xml;

    server {
        listen       {{config.FrontendPort}};
        server_name  localhost;

        # 静态文件缓存
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
            root   ../WebSite;
            expires 1d;
            add_header Cache-Control "public, immutable";
        }

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

            # 代理超时设置
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
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

        if (!dbExists)
        {
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
            Console.WriteLine("  数据库创建完成");
        }
        else
        {
            Console.WriteLine("  数据库已存在");
        }

        // 切换到目标数据库
        masterConnection.ChangeDatabase(config.DbName);

        // 检查是否有必要的表
        var checkTablesCmd = new SqlCommand(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
            masterConnection);
        var tableCount = Convert.ToInt32(await checkTablesCmd.ExecuteScalarAsync());

        if (tableCount > 0)
        {
            Console.WriteLine($"  数据库已有 {tableCount} 个表，跳过表结构创建");
            return;
        }

        Console.WriteLine("  开始创建表结构...");

        // 创建表结构
        await ExecuteSqlScriptAsync(masterConnection, GetCreateTablesSql());

        // 插入初始数据
        await ExecuteSqlScriptAsync(masterConnection, GetSeedDataSql());

        Console.WriteLine("  数据库初始化完成");

        // 如果是 Windows 认证，授权服务账户
        if (config.DbAuth.Equals("windows", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("  授权服务账户...");
            await GrantSystemServicePermissions(masterConnection);
        }
    }

    /// <summary>
    /// 授予 NT AUTHORITY\SYSTEM SQL Server 权限
    /// 使 Windows 服务能够使用 Windows 认证连接数据库
    /// </summary>
    static async Task GrantSystemServicePermissions(SqlConnection connection)
    {
        const string serviceAccount = "NT AUTHORITY\\SYSTEM";

        try
        {
            // 1. 检查登录是否存在
            var checkLoginCmd = new SqlCommand(
                "SELECT 1 FROM sys.server_principals WHERE name = @name",
                connection);
            checkLoginCmd.Parameters.AddWithValue("@name", serviceAccount);

            var loginExists = await checkLoginCmd.ExecuteScalarAsync();

            if (loginExists == null)
            {
                // 2. 创建 Windows 登录
                var createLoginCmd = new SqlCommand(
                    $"CREATE LOGIN [{serviceAccount}] FROM WINDOWS",
                    connection);
                await createLoginCmd.ExecuteNonQueryAsync();
                Console.WriteLine($"  创建登录: {serviceAccount}");
            }

            // 3. 检查是否已在 sysadmin 角色中
            var checkRoleCmd = new SqlCommand(
                @"SELECT 1 FROM sys.server_role_members rm
              JOIN sys.server_principals p ON rm.member_principal_id = p.principal_id
              JOIN sys.server_principals r ON rm.role_principal_id = r.principal_id
              WHERE p.name = @name AND r.name = 'sysadmin'",
                connection);
            checkRoleCmd.Parameters.AddWithValue("@name", serviceAccount);

            var inRole = await checkRoleCmd.ExecuteScalarAsync();

            if (inRole == null)
            {
                // 4. 添加到 sysadmin 角色
                var addRoleCmd = new SqlCommand(
                    $"ALTER SERVER ROLE sysadmin ADD MEMBER [{serviceAccount}]",
                    connection);
                await addRoleCmd.ExecuteNonQueryAsync();
                Console.WriteLine($"  授予 sysadmin 权限: {serviceAccount}");
            }
            else
            {
                Console.WriteLine($"  {serviceAccount} 已拥有 sysadmin 权限");
            }
        }
        catch (Exception ex)
        {
            // 授权失败不中断安装，仅记录警告
            Console.WriteLine($"  ⚠️ 授权服务账户失败: {ex.Message}");
            Console.WriteLine($"     服务启动时可能无法连接数据库，请手动授权");
        }
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
        // 注意: 此 SQL 必须与 EF Core 实体定义完全一致
        // 实体定义文件: DataForgeStudio.Domain/Entities/*.cs
        // DbContext配置: DataForgeStudio.Data/Data/DataForgeStudioDbContext.cs
        return @"
-- =====================================================
-- DataForgeStudio V4 数据库表结构
-- 基于 EF Core 实体定义生成
-- =====================================================

-- Users Table (用户表)
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
    CREATE NONCLUSTERED INDEX [IX_Users_IsActive] ON [dbo].[Users] ([IsActive]);
    CREATE NONCLUSTERED INDEX [IX_Users_IsSystem] ON [dbo].[Users] ([IsSystem]);
END

-- Roles Table (角色表)
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
    CREATE NONCLUSTERED INDEX [IX_Roles_IsActive] ON [dbo].[Roles] ([IsActive]);
END

-- UserRoles Table (用户角色关联表)
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

-- Permissions Table (权限表)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Permissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Permissions](
        [PermissionId] [int] IDENTITY(1,1) NOT NULL,
        [PermissionCode] [nvarchar](100) NOT NULL,
        [PermissionName] [nvarchar](100) NOT NULL,
        [Module] [nvarchar](50) NOT NULL,
        [Action] [nvarchar](50) NOT NULL,
        [Description] [nvarchar](200) NULL,
        [ParentId] [int] NULL,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([PermissionId] ASC),
        CONSTRAINT [UQ_Permissions_Code] UNIQUE NONCLUSTERED ([PermissionCode] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_Permissions_Module] ON [dbo].[Permissions] ([Module]);
    CREATE NONCLUSTERED INDEX [IX_Permissions_ParentId] ON [dbo].[Permissions] ([ParentId]);
END

-- RolePermissions Table (角色权限关联表)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolePermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RolePermissions](
        [RolePermissionId] [int] IDENTITY(1,1) NOT NULL,
        [RoleId] [int] NOT NULL,
        [PermissionId] [int] NOT NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([RolePermissionId] ASC),
        CONSTRAINT [UQ_RolePermissions_Role_Permission] UNIQUE NONCLUSTERED ([RoleId] ASC, [PermissionId] ASC)
    );
END

-- DataSources Table (数据源表)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataSources]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DataSources](
        [DataSourceId] [int] IDENTITY(1,1) NOT NULL,
        [DataSourceName] [nvarchar](100) NOT NULL,
        [DataSourceCode] [nvarchar](50) NOT NULL,
        [DbType] [nvarchar](20) NOT NULL,
        [ServerAddress] [nvarchar](200) NOT NULL,
        [Port] [int] NULL,
        [DatabaseName] [nvarchar](100) NULL,
        [Username] [nvarchar](100) NULL,
        [Password] [nvarchar](500) NULL,
        [IsIntegratedSecurity] [bit] NOT NULL DEFAULT 0,
        [ConnectionTimeout] [int] NOT NULL DEFAULT 30,
        [CommandTimeout] [int] NOT NULL DEFAULT 60,
        [IsDefault] [bit] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [TestSql] [nvarchar](500) NULL,
        [Remark] [nvarchar](500) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        [LastTestTime] [datetime] NULL,
        [LastTestResult] [bit] NULL,
        [LastTestMessage] [nvarchar](500) NULL,
        CONSTRAINT [PK_DataSources] PRIMARY KEY CLUSTERED ([DataSourceId] ASC),
        CONSTRAINT [UQ_DataSources_Code] UNIQUE NONCLUSTERED ([DataSourceCode] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_DataSources_IsActive] ON [dbo].[DataSources] ([IsActive]);
    CREATE NONCLUSTERED INDEX [IX_DataSources_IsDefault] ON [dbo].[DataSources] ([IsDefault]);
END

-- Reports Table (报表定义表)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Reports]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Reports](
        [ReportId] [int] IDENTITY(1,1) NOT NULL,
        [ReportName] [nvarchar](100) NOT NULL,
        [ReportCode] [nvarchar](50) NOT NULL,
        [ReportCategory] [nvarchar](50) NULL,
        [DataSourceId] [int] NOT NULL,
        [SqlStatement] [nvarchar](max) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsPaged] [bit] NOT NULL DEFAULT 1,
        [PageSize] [int] NOT NULL DEFAULT 50,
        [CacheDuration] [int] NOT NULL DEFAULT 0,
        [IsEnabled] [bit] NOT NULL DEFAULT 1,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [ViewCount] [int] NOT NULL DEFAULT 0,
        [LastViewTime] [datetime] NULL,
        [Remark] [nvarchar](500) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        [ChartConfig] [nvarchar](2000) NULL,
        [EnableChart] [bit] NOT NULL DEFAULT 0,
        [QueryConditions] [nvarchar](2000) NULL,
        CONSTRAINT [PK_Reports] PRIMARY KEY CLUSTERED ([ReportId] ASC),
        CONSTRAINT [UQ_Reports_Code] UNIQUE NONCLUSTERED ([ReportCode] ASC),
        CONSTRAINT [CK_Reports_SqlStatement] CHECK ([SqlStatement] LIKE 'SELECT%' OR [SqlStatement] LIKE 'select%')
    );
    CREATE NONCLUSTERED INDEX [IX_Reports_IsEnabled] ON [dbo].[Reports] ([IsEnabled]);
    CREATE NONCLUSTERED INDEX [IX_Reports_ReportCategory] ON [dbo].[Reports] ([ReportCategory]);
    CREATE NONCLUSTERED INDEX [IX_Reports_DataSourceId] ON [dbo].[Reports] ([DataSourceId]);
END

-- ReportFields Table (报表字段配置表)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReportFields]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ReportFields](
        [FieldId] [int] IDENTITY(1,1) NOT NULL,
        [ReportId] [int] NOT NULL,
        [FieldName] [nvarchar](100) NOT NULL,
        [DisplayName] [nvarchar](100) NOT NULL,
        [DataType] [nvarchar](20) NOT NULL,
        [Width] [int] NOT NULL DEFAULT 100,
        [IsVisible] [bit] NOT NULL DEFAULT 1,
        [IsSortable] [bit] NOT NULL DEFAULT 1,
        [SummaryType] [nvarchar](10) NULL,
        [SummaryDecimals] [int] NULL,
        [IsFilterable] [bit] NOT NULL DEFAULT 0,
        [IsGroupable] [bit] NOT NULL DEFAULT 0,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [Align] [nvarchar](10) NOT NULL DEFAULT 'left',
        [FormatString] [nvarchar](50) NULL,
        [AggregateFunction] [nvarchar](20) NULL,
        [CssClass] [nvarchar](100) NULL,
        [Remark] [nvarchar](200) NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ReportFields] PRIMARY KEY CLUSTERED ([FieldId] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_ReportFields_ReportId] ON [dbo].[ReportFields] ([ReportId]);
END

-- ReportParameters Table (报表参数配置表)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReportParameters]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ReportParameters](
        [ParameterId] [int] IDENTITY(1,1) NOT NULL,
        [ReportId] [int] NOT NULL,
        [ParameterName] [nvarchar](50) NOT NULL,
        [DisplayName] [nvarchar](100) NOT NULL,
        [DataType] [nvarchar](20) NOT NULL,
        [InputType] [nvarchar](20) NOT NULL,
        [DefaultValue] [nvarchar](500) NULL,
        [IsRequired] [bit] NOT NULL DEFAULT 1,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [Options] [nvarchar](max) NULL,
        [QueryOptions] [nvarchar](max) NULL,
        [Remark] [nvarchar](200) NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ReportParameters] PRIMARY KEY CLUSTERED ([ParameterId] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_ReportParameters_ReportId] ON [dbo].[ReportParameters] ([ReportId]);
END

-- OperationLogs Table (操作日志表)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OperationLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OperationLogs](
        [LogId] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NULL,
        [Username] [nvarchar](50) NULL,
        [Module] [nvarchar](50) NOT NULL,
        [Action] [nvarchar](50) NOT NULL,
        [ActionType] [nvarchar](20) NULL,
        [Description] [nvarchar](500) NULL,
        [IpAddress] [nvarchar](50) NULL,
        [UserAgent] [nvarchar](500) NULL,
        [RequestUrl] [nvarchar](500) NULL,
        [RequestMethod] [nvarchar](10) NULL,
        [RequestData] [nvarchar](max) NULL,
        [ResponseData] [nvarchar](max) NULL,
        [Duration] [int] NULL,
        [IsSuccess] [bit] NOT NULL DEFAULT 1,
        [ErrorMessage] [nvarchar](max) NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_OperationLogs] PRIMARY KEY CLUSTERED ([LogId] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_OperationLogs_UserId] ON [dbo].[OperationLogs] ([UserId]);
    CREATE NONCLUSTERED INDEX [IX_OperationLogs_Module] ON [dbo].[OperationLogs] ([Module]);
    CREATE NONCLUSTERED INDEX [IX_OperationLogs_CreatedTime] ON [dbo].[OperationLogs] ([CreatedTime]);
END

-- LoginLogs Table (登录日志表)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LoginLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LoginLogs](
        [LogId] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NULL,
        [Username] [nvarchar](50) NULL,
        [LoginTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [LogoutTime] [datetime] NULL,
        [IpAddress] [nvarchar](50) NULL,
        [UserAgent] [nvarchar](500) NULL,
        [LoginStatus] [nvarchar](20) NULL,
        [FailureReason] [nvarchar](200) NULL,
        [SessionId] [nvarchar](100) NULL,
        CONSTRAINT [PK_LoginLogs] PRIMARY KEY CLUSTERED ([LogId] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_LoginLogs_UserId] ON [dbo].[LoginLogs] ([UserId]);
    CREATE NONCLUSTERED INDEX [IX_LoginLogs_LoginTime] ON [dbo].[LoginLogs] ([LoginTime]);
END

-- SystemConfigs Table (系统配置表)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SystemConfigs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SystemConfigs](
        [ConfigId] [int] IDENTITY(1,1) NOT NULL,
        [ConfigKey] [nvarchar](100) NOT NULL,
        [ConfigValue] [nvarchar](max) NULL,
        [ConfigType] [nvarchar](20) NOT NULL DEFAULT 'String',
        [Description] [nvarchar](200) NULL,
        [IsSystem] [bit] NOT NULL DEFAULT 0,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_SystemConfigs] PRIMARY KEY CLUSTERED ([ConfigId] ASC),
        CONSTRAINT [UQ_SystemConfigs_Key] UNIQUE NONCLUSTERED ([ConfigKey] ASC)
    );
END

-- BackupRecords Table (备份记录表)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BackupRecords]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BackupRecords](
        [BackupId] [int] IDENTITY(1,1) NOT NULL,
        [BackupName] [nvarchar](200) NOT NULL,
        [BackupType] [nvarchar](20) NOT NULL DEFAULT 'Manual',
        [BackupPath] [nvarchar](500) NOT NULL,
        [DatabaseName] [nvarchar](100) NULL,
        [Description] [nvarchar](500) NULL,
        [FileSize] [bigint] NULL,
        [BackupTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [IsSuccess] [bit] NOT NULL DEFAULT 1,
        [ErrorMessage] [nvarchar](max) NULL,
        [CreatedBy] [int] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_BackupRecords] PRIMARY KEY CLUSTERED ([BackupId] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_BackupRecords_BackupTime] ON [dbo].[BackupRecords] ([BackupTime]);
END

-- BackupSchedules Table (备份计划表)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BackupSchedules]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BackupSchedules](
        [ScheduleId] [int] IDENTITY(1,1) NOT NULL,
        [ScheduleName] [nvarchar](100) NOT NULL,
        [ScheduleType] [nvarchar](20) NOT NULL DEFAULT 'Recurring',
        [RecurringDays] [nvarchar](50) NULL,
        [ScheduledTime] [nvarchar](10) NULL,
        [OnceDate] [datetime] NULL,
        [RetentionCount] [int] NOT NULL DEFAULT 10,
        [IsEnabled] [bit] NOT NULL DEFAULT 1,
        [LastRunTime] [datetime] NULL,
        [NextRunTime] [datetime] NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedTime] [datetime] NULL,
        CONSTRAINT [PK_BackupSchedules] PRIMARY KEY CLUSTERED ([ScheduleId] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_BackupSchedules_NextRunTime] ON [dbo].[BackupSchedules] ([NextRunTime]);
    CREATE NONCLUSTERED INDEX [IX_BackupSchedules_IsEnabled] ON [dbo].[BackupSchedules] ([IsEnabled]);
END

-- Licenses Table (许可证表 - 零信任架构)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Licenses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Licenses](
        [LicenseId] [int] IDENTITY(1,1) NOT NULL,
        [LicenseKey] [nvarchar](max) NOT NULL,
        [Signature] [nvarchar](512) NOT NULL,
        [MachineCode] [nvarchar](64) NOT NULL,
        [ActivatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [ActivatedIP] [nvarchar](50) NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Licenses] PRIMARY KEY CLUSTERED ([LicenseId] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_Licenses_MachineCode] ON [dbo].[Licenses] ([MachineCode]);
END
";
    }

    static string GetSeedDataSql()
    {
        // 注意: 此种子数据必须与 DbInitializer.CreateAllPermissionsAsync() 保持一致
        // 实际运行时，DbInitializer 会创建完整的权限列表
        // 这里的种子数据仅作为后备
        return @"
-- 插入默认权限 (与 DbInitializer.CreateAllPermissionsAsync 保持一致)
INSERT INTO [Permissions] (PermissionCode, PermissionName, Module, Action, Description, SortOrder, IsSystem, CreatedTime) VALUES
-- 用户管理权限
('user:view', N'查看用户', N'User', N'View', N'查看用户列表', 1, 1, GETUTCDATE()),
('user:create', N'创建用户', N'User', N'Create', N'创建新用户', 2, 1, GETUTCDATE()),
('user:edit', N'编辑用户', N'User', N'Edit', N'编辑用户信息', 3, 1, GETUTCDATE()),
('user:delete', N'删除用户', N'User', N'Delete', N'删除用户', 4, 1, GETUTCDATE()),
('user:resetPassword', N'重置密码', N'User', N'ResetPassword', N'重置用户密码', 5, 1, GETUTCDATE()),
-- 角色管理权限
('role:view', N'查看角色', N'Role', N'View', N'查看角色列表', 6, 1, GETUTCDATE()),
('role:create', N'创建角色', N'Role', N'Create', N'创建新角色', 7, 1, GETUTCDATE()),
('role:edit', N'编辑角色', N'Role', N'Edit', N'编辑角色信息', 8, 1, GETUTCDATE()),
('role:delete', N'删除角色', N'Role', N'Delete', N'删除角色', 9, 1, GETUTCDATE()),
('role:assignPermissions', N'分配权限', N'Role', N'AssignPermissions', N'为角色分配权限', 10, 1, GETUTCDATE()),
-- 报表查询权限
('report:query', N'访问报表查询', N'Report', N'Query', N'访问报表查询页面', 11, 1, GETUTCDATE()),
('report:execute', N'执行报表查询', N'Report', N'Execute', N'执行报表查询并查看结果', 12, 1, GETUTCDATE()),
-- 报表设计权限
('report:design', N'访问报表设计', N'Report', N'Design', N'访问报表设计管理页面', 13, 1, GETUTCDATE()),
('report:create', N'创建报表', N'Report', N'Create', N'创建新报表', 14, 1, GETUTCDATE()),
('report:edit', N'编辑报表', N'Report', N'Edit', N'编辑报表配置', 15, 1, GETUTCDATE()),
('report:delete', N'删除报表', N'Report', N'Delete', N'删除报表', 16, 1, GETUTCDATE()),
('report:toggle', N'停用启用报表', N'Report', N'Toggle', N'停用或启用报表', 17, 1, GETUTCDATE()),
('report:export', N'导出报表', N'Report', N'Export', N'导出报表数据', 18, 1, GETUTCDATE()),
-- 数据源管理权限
('datasource:view', N'查看数据源', N'DataSource', N'View', N'查看数据源列表', 19, 1, GETUTCDATE()),
('datasource:create', N'创建数据源', N'DataSource', N'Create', N'创建新数据源', 20, 1, GETUTCDATE()),
('datasource:edit', N'编辑数据源', N'DataSource', N'Edit', N'编辑数据源', 21, 1, GETUTCDATE()),
('datasource:delete', N'删除数据源', N'DataSource', N'Delete', N'删除数据源', 22, 1, GETUTCDATE()),
('datasource:test', N'测试连接', N'DataSource', N'Test', N'测试数据源连接', 23, 1, GETUTCDATE()),
-- 日志管理权限
('log:view', N'查看日志', N'Log', N'View', N'查看操作日志', 24, 1, GETUTCDATE()),
('log:clear', N'清空日志', N'Log', N'Clear', N'清空操作日志', 25, 1, GETUTCDATE()),
('log:export', N'导出日志', N'Log', N'Export', N'导出操作日志', 26, 1, GETUTCDATE()),
-- 备份管理权限
('backup:view', N'查看备份', N'Backup', N'View', N'查看备份列表', 27, 1, GETUTCDATE()),
('backup:create', N'创建备份', N'Backup', N'Create', N'创建数据备份', 28, 1, GETUTCDATE()),
('backup:restore', N'恢复备份', N'Backup', N'Restore', N'恢复数据备份', 29, 1, GETUTCDATE()),
('backup:delete', N'删除备份', N'Backup', N'Delete', N'删除备份', 30, 1, GETUTCDATE()),
-- 许可管理权限
('license:view', N'查看许可', N'License', N'View', N'查看许可证信息', 31, 1, GETUTCDATE()),
('license:activate', N'激活许可', N'License', N'Activate', N'激活许可证', 32, 1, GETUTCDATE()),
-- 系统设置权限
('system:view', N'查看系统设置', N'System', N'View', N'查看系统配置', 33, 1, GETUTCDATE()),
('system:edit', N'编辑系统设置', N'System', N'Edit', N'编辑系统配置', 34, 1, GETUTCDATE());

-- 插入超级管理员角色 (使用 ROLE_SUPER_ADMIN 与 DbInitializer 一致)
INSERT INTO [Roles] (RoleName, RoleCode, Description, IsSystem, SortOrder, IsActive, CreatedTime) VALUES
(N'超级管理员', 'ROLE_SUPER_ADMIN', N'系统超级管理员，拥有所有权限', 1, 1, 1, GETUTCDATE());

-- 为超级管理员分配所有权限
INSERT INTO [RolePermissions] (RoleId, PermissionId, CreatedTime)
SELECT r.RoleId, p.PermissionId, GETUTCDATE()
FROM [Roles] r, [Permissions] p
WHERE r.RoleCode = 'ROLE_SUPER_ADMIN';

-- 插入 root 用户 (固定密码: Admin@123)
-- BCrypt hash generated with work factor 12
INSERT INTO [Users] (Username, PasswordHash, RealName, Email, IsActive, IsSystem, MustChangePassword, CreatedTime) VALUES
('root', '$2a$12$w1KCXdjrxtSC5pdR5MCeQOxH1DvD/3q4Vu/KnJlvit0PuwC3YqZhO', N'系统管理员', 'root@dataforge.com', 1, 1, 0, GETUTCDATE());

-- 为 root 用户分配超级管理员角色
INSERT INTO [UserRoles] (UserId, RoleId, CreatedTime)
SELECT u.UserId, r.RoleId, GETUTCDATE()
FROM [Users] u, [Roles] r
WHERE u.Username = 'root' AND r.RoleCode = 'ROLE_SUPER_ADMIN';
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

    static void RegisterWebService(Configuration config)
    {
        var serviceName = "DFWebService";
        var nginxExePath = Path.Combine(config.InstallPath, "WebServer", "nginx.exe");
        var nssmPath = Path.Combine(config.InstallPath, "tools", "nssm", "nssm.exe");

        // 检查 NSSM 是否存在
        if (!File.Exists(nssmPath))
        {
            Console.WriteLine("  警告: NSSM 未找到，跳过 Web 服务注册");
            return;
        }

        // 检查 Nginx 是否存在
        if (!File.Exists(nginxExePath))
        {
            Console.WriteLine("  警告: Nginx 未找到，跳过 Web 服务注册");
            return;
        }

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
                    Console.WriteLine("  Web 服务已存在，跳过注册");
                    return;
                }
            }
        }

        Console.WriteLine("  正在注册 Web 服务 (DFWebService)...");

        // 使用 NSSM 创建服务
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = nssmPath,
            Arguments = $"install \"{serviceName}\" \"{nginxExePath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.Combine(config.InstallPath, "WebServer")
        };

        using (var process = System.Diagnostics.Process.Start(startInfo))
        {
            if (process == null)
            {
                Console.WriteLine("  警告: 无法启动 NSSM 进程");
                return;
            }
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                Console.WriteLine($"  警告: 创建 Web 服务失败: {error}");
                return;
            }
        }

        // 设置服务显示名称
        RunCommand(nssmPath, $"set \"{serviceName}\" DisplayName \"DataForge Studio Web Service\"");

        // 设置服务描述
        RunCommand(nssmPath, $"set \"{serviceName}\" Description \"DataForgeStudio 前端服务 (Nginx)\"");

        // 设置服务启动类型为自动
        RunCommand(nssmPath, $"set \"{serviceName}\" Start SERVICE_AUTO_START");

        // 设置工作目录
        RunCommand(nssmPath, $"set \"{serviceName}\" AppDirectory \"{Path.Combine(config.InstallPath, "WebServer")}\"");

        // 设置日志输出
        var logPath = Path.Combine(config.InstallPath, "WebServer", "logs");
        RunCommand(nssmPath, $"set \"{serviceName}\" AppStdout \"{Path.Combine(logPath, "service-out.log")}\"");
        RunCommand(nssmPath, $"set \"{serviceName}\" AppStderr \"{Path.Combine(logPath, "service-err.log")}\"");

        // 设置日志轮转
        RunCommand(nssmPath, $"set \"{serviceName}\" AppRotateFiles 1");
        RunCommand(nssmPath, $"set \"{serviceName}\" AppRotateBytes 1048576");

        Console.WriteLine("  Web 服务注册完成 (DFWebService)");
    }

    static void RunCommand(string fileName, string arguments)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();
        }
        catch
        {
            // 忽略错误，继续执行
        }
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
