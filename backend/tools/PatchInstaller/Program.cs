using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Reflection;
using System.ServiceProcess;
using System.Text.Json;
using System.Windows.Forms;

namespace PatchInstaller;

/// <summary>
/// DataForgeStudio Patch Installer with GUI
/// </summary>
class Program
{
    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "DataForgeStudio", "Logs", $"patch-install-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    private const string EmbeddedResourceName = "patch-resources.zip";

    internal static string? _installPath;
    internal static string? _dbServer;
    internal static string? _dbName;
    internal static string? _dbUser;
    internal static string? _dbPassword;
    private static bool _embeddedMode = false;
    private static PatchInfo? _patchInfo;
    internal static string _patchVersion = "Unknown";

    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Check embedded resources
        _embeddedMode = HasEmbeddedResources();

        // Try to get patch info for display
        if (_embeddedMode)
        {
            try
            {
                var tempDir = ExtractEmbeddedResources();
                _patchInfo = ReadPatchInfoFromDirectory(tempDir).Result;
                if (_patchInfo != null)
                {
                    _patchVersion = _patchInfo.Version;
                }
                // Clean up temp dir, we'll extract again during install
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch { /* Ignore errors during info extraction */ }
        }

        // Parse command line args for defaults
        ParseArguments(args);

        // Show GUI
        Application.Run(new PatchInstallerForm());
    }

    static void ParseArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i":
                case "--install-path":
                    _installPath = args.ElementAtOrDefault(i + 1);
                    i++;
                    break;
                case "--db-server":
                    _dbServer = args.ElementAtOrDefault(i + 1);
                    i++;
                    break;
                case "--db-name":
                    _dbName = args.ElementAtOrDefault(i + 1);
                    i++;
                    break;
                case "--db-user":
                    _dbUser = args.ElementAtOrDefault(i + 1);
                    i++;
                    break;
                case "--db-password":
                    _dbPassword = args.ElementAtOrDefault(i + 1);
                    i++;
                    break;
            }
        }
    }

    static bool HasEmbeddedResources()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        return resourceNames.Any(n => n.EndsWith(".patch.zip") || n.Contains("patch"));
    }

    static string ExtractEmbeddedResources()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var tempDir = Path.Combine(Path.GetTempPath(), $"DataForgeStudio-Patch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
        {
            if (resourceName.EndsWith(".patch.zip") || resourceName.Contains("patch"))
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                using var archive = new ZipArchive(stream);
                archive.ExtractToDirectory(tempDir);
                break;
            }
        }

        return tempDir;
    }

    static async Task<PatchInfo?> ReadPatchInfoFromDirectory(string directory)
    {
        var infoFile = Path.Combine(directory, "patch-info.json");
        if (!File.Exists(infoFile)) return null;

        var json = await File.ReadAllTextAsync(infoFile);
        return JsonSerializer.Deserialize<PatchInfo>(json);
    }

    static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logMessage = $"[{timestamp}] {message}";

        try
        {
            File.AppendAllText(LogFile, logMessage + Environment.NewLine);
        }
        catch { /* Ignore log file errors */ }
    }

    // Installation logic - called from form
    public static async Task<(bool Success, string Message, string? BackupPath)> RunInstallationAsync(
        string installPath,
        string? dbServer, string? dbName, string? dbUser, string? dbPassword,
        Action<string> logCallback)
    {
        _installPath = installPath;
        _dbServer = dbServer;
        _dbName = dbName;
        _dbUser = dbUser;
        _dbPassword = dbPassword;

        void LogBoth(string msg)
        {
            Log(msg);
            logCallback(msg);
        }

        try
        {
            // Ensure log directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);

            if (!_embeddedMode)
            {
                LogBoth("ERROR: No embedded resources found.");
                return (false, "This patch was not built correctly.", null);
            }

            // Extract resources
            LogBoth("正在解压补丁资源...");
            var tempPatchDir = ExtractEmbeddedResources();
            var patchInfo = await ReadPatchInfoFromDirectory(tempPatchDir);

            if (patchInfo == null)
            {
                LogBoth("错误：无效的补丁文件 - 缺少 patch-info.json");
                return (false, "无效的补丁文件。", null);
            }

            LogBoth($"补丁版本：{patchInfo.Version}");
            LogBoth($"安装路径：{installPath}");

            // Step 1: Stop services
            LogBoth("[1/6] 停止服务...");
            await StopServicesAsync(LogBoth);

            // Step 2: Backup
            LogBoth("[2/6] 创建备份...");
            var backupPath = await CreateBackupAsync(installPath, patchInfo.Version, LogBoth);

            // Step 3: Update files
            LogBoth("[3/6] 更新文件...");
            await UpdateFilesFromDirectoryAsync(tempPatchDir, installPath, LogBoth);

            // Step 4: Run SQL scripts
            LogBoth("[4/6] 执行数据库更新...");
            await RunDatabaseUpdatesFromDirectoryAsync(tempPatchDir, patchInfo.Version, LogBoth);

            // Step 5: Update version
            LogBoth("[5/6] 更新版本信息...");
            await UpdateVersionInfoAsync(installPath, patchInfo.Version, LogBoth);

            // Step 6: Start services
            LogBoth("[6/6] 启动服务...");
            await StartServicesAsync(LogBoth);

            // Cleanup
            if (Directory.Exists(tempPatchDir))
            {
                Directory.Delete(tempPatchDir, true);
            }

            return (true, $"补丁 {patchInfo.Version} 安装成功！", backupPath);
        }
        catch (Exception ex)
        {
            LogBoth($"错误：{ex.Message}");
            return (false, $"安装失败：{ex.Message}", null);
        }
    }

    static async Task StopServicesAsync(Action<string> log)
    {
        var services = new[] { "DFAppService", "DFWebService" };

        foreach (var serviceName in services)
        {
            try
            {
                using var controller = new ServiceController(serviceName);
                if (controller.Status == ServiceControllerStatus.Running)
                {
                    log($"  正在停止 {serviceName}...");
                    controller.Stop();
                    await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));
                    log($"  {serviceName} 已停止。");
                }
                else
                {
                    log($"  {serviceName} 未运行");
                }
            }
            catch (InvalidOperationException)
            {
                log($"  {serviceName} 未找到，跳过...");
            }
            catch (Exception ex)
            {
                log($"  警告：{ex.Message}");
            }
        }
    }

    static async Task<string> CreateBackupAsync(string installPath, string version, Action<string> log)
    {
        var backupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DataForgeStudio", "Backups", $"pre-patch-{version}-{DateTime.Now:yyyyMMdd-HHmmss}");

        Directory.CreateDirectory(backupDir);

        var serverPath = Path.Combine(installPath, "Server");
        if (Directory.Exists(serverPath))
        {
            var backupServerPath = Path.Combine(backupDir, "Server");
            await Task.Run(() => CopyDirectory(serverPath, backupServerPath, "*.dll"));
            log($"  已备份服务端文件");
        }

        var webSitePath = Path.Combine(installPath, "WebSite");
        if (Directory.Exists(webSitePath))
        {
            var backupWebPath = Path.Combine(backupDir, "WebSite");
            await Task.Run(() => CopyDirectory(webSitePath, backupWebPath));
            log($"  已备份网站文件");
        }

        var docsPath = Path.Combine(installPath, "docs");
        if (Directory.Exists(docsPath))
        {
            var backupDocsPath = Path.Combine(backupDir, "docs");
            await Task.Run(() => CopyDirectory(docsPath, backupDocsPath));
            log($"  已备份文档文件");
        }

        var managerPath = Path.Combine(installPath, "Manager");
        if (Directory.Exists(managerPath))
        {
            var backupManagerPath = Path.Combine(backupDir, "Manager");
            await Task.Run(() => CopyDirectory(managerPath, backupManagerPath, "*.dll"));
            log($"  已备份管理工具文件");
        }

        return backupDir;
    }

    static async Task UpdateFilesFromDirectoryAsync(string sourceDir, string installPath, Action<string> log)
    {
        var serverSource = Path.Combine(sourceDir, "Server");
        if (Directory.Exists(serverSource))
        {
            var serverTarget = Path.Combine(installPath, "Server");
            foreach (var file in Directory.GetFiles(serverSource, "*.dll"))
            {
                var targetFile = Path.Combine(serverTarget, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
                log($"  已更新：{Path.GetFileName(file)}");
            }
        }

        var webSiteSource = Path.Combine(sourceDir, "WebSite");
        if (Directory.Exists(webSiteSource))
        {
            var webSiteTarget = Path.Combine(installPath, "WebSite");
            await Task.Run(() => CopyDirectory(webSiteSource, webSiteTarget));
            log($"  已更新网站文件");
        }

        // Update docs folder (help documents)
        var docsSource = Path.Combine(sourceDir, "docs");
        if (Directory.Exists(docsSource))
        {
            var docsTarget = Path.Combine(installPath, "docs");
            await Task.Run(() => CopyDirectory(docsSource, docsTarget));
            log($"  已更新文档文件");
        }

        // Update Manager folder (DeployManager)
        var managerSource = Path.Combine(sourceDir, "Manager");
        if (Directory.Exists(managerSource))
        {
            var managerTarget = Path.Combine(installPath, "Manager");
            foreach (var file in Directory.GetFiles(managerSource, "*.dll"))
            {
                var targetFile = Path.Combine(managerTarget, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
                log($"  已更新：Manager/{Path.GetFileName(file)}");
            }
        }
    }

    static async Task RunDatabaseUpdatesFromDirectoryAsync(string sourceDir, string version, Action<string> log)
    {
        if (string.IsNullOrEmpty(_dbServer) || string.IsNullOrEmpty(_dbName))
        {
            log("  无数据库连接信息，跳过 SQL 更新");
            return;
        }

        var sqlDir = Path.Combine(sourceDir, "sql");
        if (!Directory.Exists(sqlDir))
        {
            log("  未找到 SQL 脚本");
            return;
        }

        var connectionString = $"Server={_dbServer};Database={_dbName};" +
                               $"User Id={_dbUser};Password={_dbPassword};" +
                               "TrustServerCertificate=True;Connection Timeout=30;";

        log($"  正在连接：{_dbServer}/{_dbName}");

        var sqlFiles = Directory.GetFiles(sqlDir, "*.sql").OrderBy(f => f);

        foreach (var sqlFile in sqlFiles)
        {
            try
            {
                var sql = await File.ReadAllTextAsync(sqlFile);
                log($"  正在执行：{Path.GetFileName(sqlFile)}");
                await ExecuteSqlScriptAsync(connectionString, sql);
                log($"  成功");
            }
            catch (Exception ex)
            {
                log($"  ERROR: {ex.Message}");
                throw;
            }
        }
    }

    static async Task ExecuteSqlScriptAsync(string connectionString, string sql)
    {
        using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync();

        var batches = sql.Split(new[] { "\nGO", "\nGo", "\ngo" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var batch in batches)
        {
            var trimmedBatch = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmedBatch)) continue;

            using var command = connection.CreateCommand();
            command.CommandText = trimmedBatch;
            command.CommandTimeout = 300;
            await command.ExecuteNonQueryAsync();
        }
    }

    static async Task UpdateVersionInfoAsync(string installPath, string version, Action<string> log)
    {
        var versionFile = Path.Combine(installPath, "version.txt");
        await File.WriteAllTextAsync(versionFile, $"Version: {version}{Environment.NewLine}Installed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        log($"  版本文件已更新为 {version}");
    }

    static async Task StartServicesAsync(Action<string> log)
    {
        var services = new[] { "DFAppService", "DFWebService" };

        foreach (var serviceName in services)
        {
            try
            {
                using var controller = new ServiceController(serviceName);
                if (controller.Status == ServiceControllerStatus.Stopped)
                {
                    log($"  正在启动 {serviceName}...");
                    controller.Start();
                    await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));
                    log($"  {serviceName} 已启动。");
                }
                else
                {
                    log($"  {serviceName} 已在运行");
                }
            }
            catch (InvalidOperationException)
            {
                log($"  {serviceName} 未找到");
            }
            catch (Exception ex)
            {
                log($"  警告：{ex.Message}");
            }
        }
    }

    static void CopyDirectory(string sourceDir, string destDir, string? searchPattern = null)
    {
        Directory.CreateDirectory(destDir);

        var files = searchPattern != null
            ? Directory.GetFiles(sourceDir, searchPattern, SearchOption.TopDirectoryOnly)
            : Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir, searchPattern);
        }
    }
}

class PatchInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [System.Text.Json.Serialization.JsonPropertyName("buildDate")]
    public string BuildDate { get; set; } = "";

    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

/// <summary>
/// Main GUI Form for Patch Installer
/// </summary>
class PatchInstallerForm : Form
{
    private TextBox _installPathTextBox = null!;
    private TextBox _logTextBox = null!;
    private Button _installButton = null!;
    private Button _cancelButton = null!;
    private Button _browseButton = null!;
    private ProgressBar _progressBar = null!;

    public PatchInstallerForm()
    {
        InitializeComponents();
        LoadDefaultValues();
    }

    void InitializeComponents()
    {
        // Form settings
        Text = $"DataForgeStudio 补丁安装程序 - V{Program._patchVersion}";
        Size = new Size(600, 420);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var yPos = 10;

        // Title
        var titleLabel = new Label
        {
            Text = $"补丁版本：{Program._patchVersion}",
            Location = new Point(20, yPos),
            Size = new Size(540, 25),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 158, 255)
        };
        mainPanel.Controls.Add(titleLabel);
        yPos += 35;

        // Install path
        var pathLabel = new Label
        {
            Text = "安装目录：",
            Location = new Point(20, yPos),
            Size = new Size(150, 20)
        };
        mainPanel.Controls.Add(pathLabel);

        _installPathTextBox = new TextBox
        {
            Location = new Point(20, yPos + 22),
            Size = new Size(460, 25)
        };
        mainPanel.Controls.Add(_installPathTextBox);

        _browseButton = new Button
        {
            Text = "浏览...",
            Location = new Point(490, yPos + 20),
            Size = new Size(70, 28)
        };
        _browseButton.Click += BrowseButton_Click;
        mainPanel.Controls.Add(_browseButton);
        yPos += 55;

        // Progress bar
        _progressBar = new ProgressBar
        {
            Location = new Point(20, yPos),
            Size = new Size(540, 25),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Visible = false
        };
        mainPanel.Controls.Add(_progressBar);
        yPos += 30;

        // Log text box
        var logLabel = new Label
        {
            Text = "安装日志：",
            Location = new Point(20, yPos),
            Size = new Size(100, 20)
        };
        mainPanel.Controls.Add(logLabel);
        yPos += 22;

        _logTextBox = new TextBox
        {
            Location = new Point(20, yPos),
            Size = new Size(540, 130),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 9),
            BackColor = Color.FromArgb(245, 245, 245)
        };
        mainPanel.Controls.Add(_logTextBox);
        yPos += 140;

        // Buttons
        _installButton = new Button
        {
            Text = "安装",
            Location = new Point(370, yPos),
            Size = new Size(90, 32),
            BackColor = Color.FromArgb(64, 158, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _installButton.Click += InstallButton_Click;
        mainPanel.Controls.Add(_installButton);

        _cancelButton = new Button
        {
            Text = "取消",
            Location = new Point(470, yPos),
            Size = new Size(90, 32)
        };
        _cancelButton.Click += (s, e) => Close();
        mainPanel.Controls.Add(_cancelButton);

        Controls.Add(mainPanel);
    }

    void LoadDefaultValues()
    {
        // Default install path
        var defaultPath = Program._installPath ?? @"C:\Program Files\DataForgeStudio";
        _installPathTextBox.Text = defaultPath;

        // Check if path exists, if not try other common locations
        if (!Directory.Exists(defaultPath))
        {
            var altPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DataForgeStudio"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "DataForgeStudio")
            };

            foreach (var path in altPaths)
            {
                if (Directory.Exists(path))
                {
                    _installPathTextBox.Text = path;
                    break;
                }
            }
        }

        // 如果目录存在，自动读取配置
        if (Directory.Exists(_installPathTextBox.Text))
        {
            LoadDatabaseConfig(_installPathTextBox.Text);
        }
    }

    /// <summary>
    /// 从安装目录的 appsettings.json 读取数据库配置
    /// </summary>
    void LoadDatabaseConfig(string installPath)
    {
        var configPath = Path.Combine(installPath, "Server", "appsettings.json");
        if (!File.Exists(configPath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("ConnectionStrings", out var connStrings) &&
                connStrings.TryGetProperty("DefaultConnection", out var defaultConn))
            {
                var connectionString = defaultConn.GetString() ?? "";
                ParseConnectionString(connectionString);
            }
        }
        catch
        {
            // 忽略解析错误，使用默认值
        }
    }

    /// <summary>
    /// 解析连接字符串，提取数据库配置
    /// </summary>
    void ParseConnectionString(string connectionString)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;

            var key = kv[0].Trim().ToLower();
            var value = kv[1].Trim();

            switch (key)
            {
                case "data source":
                case "server":
                    Program._dbServer = value.Replace("tcp:", "");
                    break;
                case "initial catalog":
                case "database":
                    Program._dbName = value;
                    break;
                case "user id":
                case "uid":
                    Program._dbUser = value;
                    break;
                case "password":
                case "pwd":
                    Program._dbPassword = value;
                    break;
            }
        }
    }

    void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择 DataForgeStudio 安装目录",
            ShowNewFolderButton = false
        };

        if (Directory.Exists(_installPathTextBox.Text))
        {
            dialog.SelectedPath = _installPathTextBox.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _installPathTextBox.Text = dialog.SelectedPath;
            LoadDatabaseConfig(dialog.SelectedPath);  // 自动读取数据库配置
        }
    }

    async void InstallButton_Click(object? sender, EventArgs e)
    {
        var installPath = _installPathTextBox.Text.Trim();

        // Validate
        if (string.IsNullOrEmpty(installPath))
        {
            MessageBox.Show("请输入安装目录。", "验证错误",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Directory.Exists(installPath))
        {
            MessageBox.Show($"安装目录不存在：{installPath}", "验证错误",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Disable controls
        _installButton.Enabled = false;
        _cancelButton.Enabled = false;
        _installPathTextBox.Enabled = false;
        _browseButton.Enabled = false;
        _progressBar.Visible = true;
        _logTextBox.Clear();

        // Run installation
        var (success, message, backupPath) = await Program.RunInstallationAsync(
            installPath,
            Program._dbServer,
            Program._dbName,
            Program._dbUser,
            Program._dbPassword,
            msg => AppendLog(msg)
        );

        // Show result
        _progressBar.Visible = false;

        if (success)
        {
            AppendLog("");
            AppendLog("========================================");
            AppendLog("安装成功完成！");
            if (!string.IsNullOrEmpty(backupPath))
            {
                AppendLog($"备份位置：{backupPath}");
            }
            AppendLog("========================================");

            MessageBox.Show(message, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Close the program after successful installation
            Close();
        }
        else
        {
            AppendLog("");
            AppendLog($"ERROR: {message}");

            MessageBox.Show(message, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // Re-enable controls to allow retry
            _installButton.Enabled = true;
            _cancelButton.Enabled = true;
            _installPathTextBox.Enabled = true;
            _browseButton.Enabled = true;
        }
    }

    void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendLog(message));
            return;
        }

        _logTextBox.AppendText(message + Environment.NewLine);
        _logTextBox.ScrollToCaret();
    }
}
