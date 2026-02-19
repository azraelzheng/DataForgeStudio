using System.Diagnostics;
using System.IO;
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
    private const string ResourcePath = "embedded";

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
        var steps = new List<(string Name, Func<Task> Action)>
        {
            ("验证安装路径", () => ValidatePath(config.InstallPath)),
            ("创建目录结构", () => CreateDirectories(config.InstallPath)),
            ("复制 API 文件", () => CopyApiFiles(config.InstallPath, progress)),
            ("复制前端文件", () => CopyFrontendFiles(config.InstallPath, progress)),
            ("复制 Nginx 文件", () => CopyNginxFiles(config.InstallPath, progress)),
            ("复制管理工具", () => CopyDeployManager(config.InstallPath, progress)),
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
        var directories = new[]
        {
            installPath,
            Path.Combine(installPath, "api"),
            Path.Combine(installPath, "WebSite"),
            Path.Combine(installPath, "nginx"),
            Path.Combine(installPath, "nginx", "conf"),
            Path.Combine(installPath, "nginx", "logs"),
            Path.Combine(installPath, "nginx", "temp"),
            Path.Combine(installPath, "config"),
            Path.Combine(installPath, "keys")
        };

        foreach (var dir in directories)
            Directory.CreateDirectory(dir);

        return Task.CompletedTask;
    }

    private Task CopyApiFiles(string installPath, IProgress<InstallProgress> progress)
    {
        var sourcePath = Path.Combine(ResourcePath, "api");
        var targetPath = Path.Combine(installPath, "api");
        if (Directory.Exists(sourcePath))
            CopyDirectory(sourcePath, targetPath, progress);
        return Task.CompletedTask;
    }

    private Task CopyFrontendFiles(string installPath, IProgress<InstallProgress> progress)
    {
        var sourcePath = Path.Combine(ResourcePath, "frontend");
        var targetPath = Path.Combine(installPath, "WebSite");
        if (Directory.Exists(sourcePath))
            CopyDirectory(sourcePath, targetPath, progress);
        return Task.CompletedTask;
    }

    private Task CopyNginxFiles(string installPath, IProgress<InstallProgress> progress)
    {
        var sourcePath = Path.Combine(ResourcePath, "nginx");
        var targetPath = Path.Combine(installPath, "nginx");
        if (Directory.Exists(sourcePath))
            CopyDirectory(sourcePath, targetPath, progress);
        return Task.CompletedTask;
    }

    private Task CopyDeployManager(string installPath, IProgress<InstallProgress> progress)
    {
        var sourcePath = Path.Combine(ResourcePath, "manager");
        var targetPath = installPath;
        if (Directory.Exists(sourcePath))
            CopyDirectory(sourcePath, targetPath, progress);
        return Task.CompletedTask;
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
        var deployConfig = $@"{{
  ""version"": ""1.0.0"",
  ""installPath"": ""{config.InstallPath.Replace("\\", "\\\\")}"",
  ""backend"": {{
    ""port"": {config.Backend.Port},
    ""serviceName"": ""{config.Backend.ServiceName}"",
    ""executablePath"": ""{Path.Combine(config.InstallPath, "api", "DataForgeStudio.Api.exe").Replace("\\", "\\\\")}""
  }},
  ""frontend"": {{
    ""port"": {config.Frontend.Port},
    ""mode"": ""nginx"",
    ""frontendPath"": ""{Path.Combine(config.InstallPath, "WebSite").Replace("\\", "\\\\")}""
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

    private void CopyDirectory(string sourceDir, string targetDir, IProgress<InstallProgress>? progress = null)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var targetFile = Path.Combine(targetDir, fileName);
            File.Copy(file, targetFile, true);
            progress?.Report(new InstallProgress { LogMessage = $"  复制: {fileName}" });
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            var targetSubDir = Path.Combine(targetDir, dirName);
            CopyDirectory(dir, targetSubDir, progress);
        }
    }
}
