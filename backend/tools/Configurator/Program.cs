using System.CommandLine;
using Microsoft.Data.SqlClient;

namespace Configurator;

class Program
{
    private static string? _logPath;

    static async Task<int> Main(string[] args)
    {
        // 定义命令行参数
        var installPathOption = new Option<string>("--install-path") { IsRequired = true };
        var dbServerOption = new Option<string>("--db-server", () => "localhost");
        var dbPortOption = new Option<int>("--db-port", () => 1433);
        var dbNameOption = new Option<string>("--db-name", () => "DataForgeStudio");
        var dbAuthOption = new Option<string>("--db-auth", () => "windows");
        var dbUserOption = new Option<string>("--db-user", () => "");
        var dbPasswordOption = new Option<string>("--db-password", () => "");
        var backendPortOption = new Option<int>("--backend-port", () => 5000);
        var frontendPortOption = new Option<int>("--frontend-port", () => 80);

        var rootCommand = new RootCommand("DataForgeStudio Configurator")
        {
            installPathOption,
            dbServerOption,
            dbPortOption,
            dbNameOption,
            dbAuthOption,
            dbUserOption,
            dbPasswordOption,
            backendPortOption,
            frontendPortOption
        };

        rootCommand.SetHandler(async (context) =>
        {
            var config = new Configuration
            {
                InstallPath = context.ParseResult.GetValueForOption(installPathOption)!,
                DbServer = context.ParseResult.GetValueForOption(dbServerOption),
                DbPort = context.ParseResult.GetValueForOption(dbPortOption),
                DbName = context.ParseResult.GetValueForOption(dbNameOption),
                DbAuth = context.ParseResult.GetValueForOption(dbAuthOption),
                DbUser = context.ParseResult.GetValueForOption(dbUserOption) ?? "",
                DbPassword = context.ParseResult.GetValueForOption(dbPasswordOption) ?? "",
                BackendPort = context.ParseResult.GetValueForOption(backendPortOption),
                FrontendPort = context.ParseResult.GetValueForOption(frontendPortOption)
            };

            context.ExitCode = await RunConfiguration(config);
        });

        return await rootCommand.InvokeAsync(args);
    }

    static void Log(string message)
    {
        Console.WriteLine(message);
        try
        {
            if (_logPath != null)
            {
                File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
        }
        catch { }
    }

    static async Task<int> RunConfiguration(Configuration config)
    {
        // 设置日志文件路径
        _logPath = Path.Combine(config.InstallPath, "logs", "configurator.log");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        }
        catch { }

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
            GenerateDeployConfig(config);
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
            Log($"默认用户名: root");
            Log($"默认密码: Admin@123");
            Log("请登录后立即修改密码!");
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
        var appSettingsPath = Path.Combine(config.InstallPath, "api", "appsettings.json");
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
        var nginxConfPath = Path.Combine(config.InstallPath, "nginx", "conf", "nginx.conf");
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

    static void GenerateDeployConfig(Configuration config)
    {
        var deployConfigPath = Path.Combine(config.InstallPath, "config", "deploy-config.json");
        var nginxPath = Path.Combine(config.InstallPath, "nginx").Replace("\\", "\\\\");
        var installPathEscaped = config.InstallPath.Replace("\\", "\\\\");

        var deployConfig = $$"""
{
  "version": "1.0.0",
  "installPath": "{{installPathEscaped}}",
  "backend": {
    "port": {{config.BackendPort}},
    "serviceName": "DFAppService"
  },
  "frontend": {
    "port": {{config.FrontendPort}},
    "mode": "nginx",
    "iisSiteName": "DataForgeStudio",
    "nginxPath": "{{nginxPath}}"
  },
  "database": {
    "server": "{{config.DbServer}}",
    "port": {{config.DbPort}},
    "database": "{{config.DbName}}",
    "username": "{{config.DbUser}}",
    "password": "{{config.DbPassword}}",
    "useWindowsAuth": {{config.DbAuth.Equals("windows", StringComparison.OrdinalIgnoreCase).ToString().ToLower()}}
  }
}
""";
        File.WriteAllText(deployConfigPath, deployConfig);
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

        // 创建数据库
        Console.WriteLine($"  创建数据库 {config.DbName}...");
        var createDbCmd = new SqlCommand($"CREATE DATABASE [{config.DbName}]", masterConnection);
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
        var apiExePath = Path.Combine(config.InstallPath, "api", "DataForgeStudio.Api.exe");
        var serviceName = "DFAppService";

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"create \"{serviceName}\" binPath= \"{apiExePath}\" start= auto DisplayName= \"{serviceName}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process?.WaitForExit();

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
        var managerPath = Path.Combine(config.InstallPath, "DeployManager.exe");

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
