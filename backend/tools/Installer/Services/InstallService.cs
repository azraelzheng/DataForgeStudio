using System.Diagnostics;
using System.IO;
using System.Reflection;
using Installer.Models;
using Microsoft.Data.SqlClient;

namespace Installer.Services;

public interface IInstallService
{
    Task<(bool Success, string Message)> TestDatabaseConnectionAsync(DatabaseConfig config);
    Task InstallAsync(InstallConfig config, IProgress<InstallProgress> progress, CancellationToken cancellationToken = default);
    bool IsInstalled(string installPath);
    (bool Valid, string Message) ValidateInstallPath(string path);
}

public class InstallProgress
{
    public int Percentage { get; set; }
    public string CurrentStep { get; set; } = "";
    public string LogMessage { get; set; } = "";
    public bool IsError { get; set; }
}

public class InstallService : IInstallService
{
    public async Task<(bool Success, string Message)> TestDatabaseConnectionAsync(DatabaseConfig config)
    {
        try
        {
            using var connection = new SqlConnection(config.GetMasterConnectionString());
            await connection.OpenAsync();
            return (true, "数据库连接成功");
        }
        catch (Exception ex)
        {
            return (false, $"连接失败: {ex.Message}");
        }
    }

    public async Task InstallAsync(InstallConfig config, IProgress<InstallProgress> progress, CancellationToken cancellationToken = default)
    {
        // 注意: Inno Setup 已经解压了文件到安装目录，这里只需要配置和初始化
        var steps = new List<(string Name, Func<Task> Action)>
        {
            ("验证安装路径", () => ValidatePath(config.InstallPath)),
            ("创建必要目录", () => CreateDirectories(config.InstallPath)),
            ("验证安装文件", () => VerifyInstalledFiles(config.InstallPath)),
            ("初始化数据库", () => InitializeDatabase(config, progress)),
            ("生成配置文件", () => GenerateConfigFiles(config)),
            ("注册 Windows 服务", () => RegisterWindowsService(config)),
            ("创建桌面快捷方式", () => CreateDesktopShortcut(config))
        };

        var totalSteps = steps.Count;
        var currentStep = 0;

        foreach (var step in steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            currentStep++;
            var percentage = (currentStep * 100) / totalSteps;

            progress.Report(new InstallProgress
            {
                Percentage = percentage - 10,
                CurrentStep = step.Name,
                LogMessage = $"正在{step.Name}..."
            });

            try
            {
                await step.Action();
                progress.Report(new InstallProgress
                {
                    Percentage = percentage,
                    CurrentStep = step.Name,
                    LogMessage = $"✓ {step.Name} 完成"
                });
            }
            catch (Exception ex)
            {
                progress.Report(new InstallProgress
                {
                    Percentage = percentage,
                    CurrentStep = step.Name,
                    LogMessage = $"✗ {step.Name} 失败: {ex.Message}",
                    IsError = true
                });
                throw;
            }
            await Task.Delay(100, cancellationToken);
        }
    }

    public bool IsInstalled(string installPath)
    {
        var apiExe = Path.Combine(installPath, "api", "DataForgeStudio.Api.exe");
        return File.Exists(apiExe);
    }

    public (bool Valid, string Message) ValidateInstallPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (false, "安装路径不能为空");

        try
        {
            var fullPath = Path.GetFullPath(path);
            var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (fullPath.StartsWith(systemRoot, StringComparison.OrdinalIgnoreCase))
                return (false, "不能安装到 Windows 系统目录");

            var drive = Path.GetPathRoot(fullPath);
            if (!Directory.Exists(drive))
                return (false, $"驱动器 {drive} 不存在");

            return (true, "路径有效");
        }
        catch (Exception ex)
        {
            return (false, $"路径无效: {ex.Message}");
        }
    }

    private Task ValidatePath(string installPath)
    {
        var (valid, message) = ValidateInstallPath(installPath);
        if (!valid)
            throw new InvalidOperationException(message);
        return Task.CompletedTask;
    }

    private Task CreateDirectories(string installPath)
    {
        // 创建必要的目录（Inno Setup 可能没有创建的）
        var directories = new[]
        {
            Path.Combine(installPath, "config"),
            Path.Combine(installPath, "keys"),
            Path.Combine(installPath, "logs"),
            Path.Combine(installPath, "nginx", "logs"),
            Path.Combine(installPath, "nginx", "temp")
        };

        foreach (var dir in directories)
            Directory.CreateDirectory(dir);

        return Task.CompletedTask;
    }

    private Task VerifyInstalledFiles(string installPath)
    {
        // 验证 Inno Setup 解压的文件是否存在
        var requiredFiles = new[]
        {
            Path.Combine(installPath, "api", "DataForgeStudio.Api.exe"),
            Path.Combine(installPath, "WebSite", "index.html"),
            Path.Combine(installPath, "nginx", "nginx.exe"),
            Path.Combine(installPath, "DeployManager.exe")
        };

        var missingFiles = new List<string>();
        foreach (var file in requiredFiles)
        {
            if (!File.Exists(file))
                missingFiles.Add(file);
        }

        if (missingFiles.Count > 0)
            throw new InvalidOperationException($"安装文件不完整，缺少: {string.Join(", ", missingFiles)}");

        return Task.CompletedTask;
    }

    private async Task InitializeDatabase(InstallConfig config, IProgress<InstallProgress> progress)
    {
        var databaseName = config.Database.Database;

        using var masterConnection = new SqlConnection(config.Database.GetMasterConnectionString());
        await masterConnection.OpenAsync();

        // 检查数据库是否存在
        var checkDbCmd = new SqlCommand(
            $"SELECT COUNT(*) FROM sys.databases WHERE name = @dbName",
            masterConnection);
        checkDbCmd.Parameters.AddWithValue("@dbName", databaseName);
        var dbExists = Convert.ToInt32(await checkDbCmd.ExecuteScalarAsync()) > 0;

        if (dbExists)
        {
            progress.Report(new InstallProgress { LogMessage = "  数据库已存在，跳过创建" });
            return;
        }

        // 创建数据库
        progress.Report(new InstallProgress { LogMessage = $"  创建数据库 {databaseName}..." });
        var createDbCmd = new SqlCommand($"CREATE DATABASE [{databaseName}]", masterConnection);
        await createDbCmd.ExecuteNonQueryAsync();

        // 切换到新数据库并创建表结构
        masterConnection.ChangeDatabase(databaseName);

        // 创建表结构
        await ExecuteSqlScriptAsync(masterConnection, GetCreateTablesSql(), progress);

        // 插入初始数据
        await ExecuteSqlScriptAsync(masterConnection, GetSeedDataSql(), progress);

        progress.Report(new InstallProgress { LogMessage = "  数据库初始化完成" });
    }

    private async Task ExecuteSqlScriptAsync(SqlConnection connection, string script, IProgress<InstallProgress> progress)
    {
        // 分割 SQL 语句（按 GO 分隔）
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
                progress.Report(new InstallProgress { LogMessage = $"  SQL 执行警告: {ex.Message}" });
            }
        }
    }

    private string GetCreateTablesSql()
    {
        // 返回创建表结构的 SQL
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

-- TrialRecords Table (防重置)
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

    private string GetSeedDataSql()
    {
        // 返回初始数据 SQL - 创建 root 用户和基础角色
        // 密码是临时密码，用户首次登录时需要修改
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
-- BCrypt hash for 'Admin@123'
INSERT INTO [Users] (Username, PasswordHash, RealName, IsActive, IsSystem, MustChangePassword) VALUES
('root', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.VTtYA/7.J6LlZy', N'系统管理员', 1, 1, 1);

-- 为 root 用户分配超级管理员角色
INSERT INTO [UserRoles] (UserId, RoleId)
SELECT u.UserId, r.RoleId
FROM [Users] u, [Roles] r
WHERE u.Username = 'root' AND r.RoleCode = 'admin';
";
    }

    private Task GenerateConfigFiles(InstallConfig config)
    {
        GenerateAppSettings(config);
        GenerateNginxConfig(config);
        GenerateDeployConfig(config);
        return Task.CompletedTask;
    }

    private void GenerateAppSettings(InstallConfig config)
    {
        var appSettingsPath = Path.Combine(config.InstallPath, "api", "appsettings.json");
        var appSettings = $@"{{
  ""Logging"": {{ ""LogLevel"": {{ ""Default"": ""Information"", ""Microsoft.AspNetCore"": ""Warning"" }} }},
  ""AllowedHosts"": ""*"",
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""{config.Database.GetConnectionString()}"",
    ""MasterConnection"": ""{config.Database.GetMasterConnectionString()}""
  }},
  ""Kestrel"": {{ ""Endpoints"": {{ ""Http"": {{ ""Url"": ""http://*:{config.Backend.Port}"" }} }} }},
  ""Cors"": {{ ""AllowedOrigins"": [""http://localhost"", ""http://localhost:{config.Frontend.Port}"", ""http://*""] }},
  ""Security"": {{ ""UseDefaultsForTesting"": false }}
}}";
        File.WriteAllText(appSettingsPath, appSettings);
    }

    private void GenerateNginxConfig(InstallConfig config)
    {
        var nginxConfPath = Path.Combine(config.InstallPath, "nginx", "conf", "nginx.conf");
        var nginxConf = $@"worker_processes  1;
events {{ worker_connections  1024; }}
http {{
    include       mime.types;
    default_type  application/octet-stream;
    sendfile        on;
    keepalive_timeout  65;

    server {{
        listen       {config.Frontend.Port};
        server_name  localhost;

        location / {{
            root   ../WebSite;
            index  index.html index.htm;
            try_files $uri $uri/ /index.html;
        }}

        location /api/ {{
            proxy_pass         http://127.0.0.1:{config.Backend.Port}/api/;
            proxy_http_version 1.1;
            proxy_set_header   Upgrade $http_upgrade;
            proxy_set_header   Connection keep-alive;
            proxy_set_header   Host $host;
            proxy_cache_bypass $http_upgrade;
            proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header   X-Forwarded-Proto $scheme;
        }}

        error_page   500 502 503 504  /50x.html;
        location = /50x.html {{ root   html; }}
    }}
}}";
        File.WriteAllText(nginxConfPath, nginxConf);
    }

    private void GenerateDeployConfig(InstallConfig config)
    {
        var deployConfigPath = Path.Combine(config.InstallPath, "config", "deploy-config.json");
        var nginxPath = Path.Combine(config.InstallPath, "nginx").Replace("\\", "\\\\");
        var deployConfig = $@"{{
  ""version"": ""1.0.0"",
  ""installPath"": ""{config.InstallPath.Replace("\\", "\\\\")}"",
  ""backend"": {{
    ""port"": {config.Backend.Port},
    ""serviceName"": ""{config.Backend.ServiceName}""
  }},
  ""frontend"": {{
    ""port"": {config.Frontend.Port},
    ""mode"": ""nginx"",
    ""iisSiteName"": ""DataForgeStudio"",
    ""nginxPath"": ""{nginxPath}""
  }},
  ""database"": {{
    ""server"": ""{config.Database.Server}"",
    ""port"": {config.Database.Port},
    ""database"": ""{config.Database.Database}"",
    ""username"": ""{config.Database.Username}"",
    ""password"": ""{config.Database.Password}"",
    ""useWindowsAuth"": {config.Database.UseWindowsAuth.ToString().ToLower()}
  }}
}}";
        File.WriteAllText(deployConfigPath, deployConfig);
    }

    private Task RegisterWindowsService(InstallConfig config)
    {
        var apiExePath = Path.Combine(config.InstallPath, "api", "DataForgeStudio.Api.exe");
        var serviceName = config.Backend.ServiceName;

        var startInfo = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"create \"{serviceName}\" binPath= \"{apiExePath}\" start= auto DisplayName= \"{serviceName}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo);
        process?.WaitForExit();

        if (process?.ExitCode != 0)
            throw new InvalidOperationException($"注册服务失败");

        var descInfo = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"description \"{serviceName}\" \"DataForgeStudio 报表管理系统 API 服务\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var descProcess = Process.Start(descInfo);
        descProcess?.WaitForExit();

        return Task.CompletedTask;
    }

    private Task CreateDesktopShortcut(InstallConfig config)
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

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"").Replace("\r\n", " ")}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        process?.WaitForExit();

        return Task.CompletedTask;
    }
}
