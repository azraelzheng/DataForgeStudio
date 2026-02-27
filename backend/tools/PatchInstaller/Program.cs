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
            LogBoth("Extracting patch resources...");
            var tempPatchDir = ExtractEmbeddedResources();
            var patchInfo = await ReadPatchInfoFromDirectory(tempPatchDir);

            if (patchInfo == null)
            {
                LogBoth("ERROR: Invalid patch - missing patch-info.json");
                return (false, "Invalid patch file.", null);
            }

            LogBoth($"Patch Version: {patchInfo.Version}");
            LogBoth($"Install Path: {installPath}");

            // Step 1: Stop services
            LogBoth("[1/6] Stopping services...");
            await StopServicesAsync(LogBoth);

            // Step 2: Backup
            LogBoth("[2/6] Creating backup...");
            var backupPath = await CreateBackupAsync(installPath, patchInfo.Version, LogBoth);

            // Step 3: Update files
            LogBoth("[3/6] Updating files...");
            await UpdateFilesFromDirectoryAsync(tempPatchDir, installPath, LogBoth);

            // Step 4: Run SQL scripts
            LogBoth("[4/6] Running database updates...");
            await RunDatabaseUpdatesFromDirectoryAsync(tempPatchDir, patchInfo.Version, LogBoth);

            // Step 5: Update version
            LogBoth("[5/6] Updating version info...");
            await UpdateVersionInfoAsync(installPath, patchInfo.Version, LogBoth);

            // Step 6: Start services
            LogBoth("[6/6] Starting services...");
            await StartServicesAsync(LogBoth);

            // Cleanup
            if (Directory.Exists(tempPatchDir))
            {
                Directory.Delete(tempPatchDir, true);
            }

            return (true, $"Patch {patchInfo.Version} installed successfully!", backupPath);
        }
        catch (Exception ex)
        {
            LogBoth($"ERROR: {ex.Message}");
            return (false, $"Installation failed: {ex.Message}", null);
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
                    log($"  Stopping {serviceName}...");
                    controller.Stop();
                    await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));
                    log($"  {serviceName} stopped.");
                }
                else
                {
                    log($"  {serviceName} is not running");
                }
            }
            catch (InvalidOperationException)
            {
                log($"  {serviceName} not found, skipping...");
            }
            catch (Exception ex)
            {
                log($"  Warning: {ex.Message}");
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
            log($"  Backed up Server files");
        }

        var webSitePath = Path.Combine(installPath, "WebSite");
        if (Directory.Exists(webSitePath))
        {
            var backupWebPath = Path.Combine(backupDir, "WebSite");
            await Task.Run(() => CopyDirectory(webSitePath, backupWebPath));
            log($"  Backed up WebSite files");
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
                log($"  Updated: {Path.GetFileName(file)}");
            }
        }

        var webSiteSource = Path.Combine(sourceDir, "WebSite");
        if (Directory.Exists(webSiteSource))
        {
            var webSiteTarget = Path.Combine(installPath, "WebSite");
            await Task.Run(() => CopyDirectory(webSiteSource, webSiteTarget));
            log($"  Updated WebSite files");
        }
    }

    static async Task RunDatabaseUpdatesFromDirectoryAsync(string sourceDir, string version, Action<string> log)
    {
        if (string.IsNullOrEmpty(_dbServer) || string.IsNullOrEmpty(_dbName))
        {
            log("  No database connection info, skipping SQL updates");
            return;
        }

        var sqlDir = Path.Combine(sourceDir, "sql");
        if (!Directory.Exists(sqlDir))
        {
            log("  No SQL scripts found");
            return;
        }

        var connectionString = $"Server={_dbServer};Database={_dbName};" +
                               $"User Id={_dbUser};Password={_dbPassword};" +
                               "TrustServerCertificate=True;Connection Timeout=30;";

        log($"  Connecting to: {_dbServer}/{_dbName}");

        var sqlFiles = Directory.GetFiles(sqlDir, "*.sql").OrderBy(f => f);

        foreach (var sqlFile in sqlFiles)
        {
            try
            {
                var sql = await File.ReadAllTextAsync(sqlFile);
                log($"  Executing: {Path.GetFileName(sqlFile)}");
                await ExecuteSqlScriptAsync(connectionString, sql);
                log($"  Success");
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
        log($"  Version file updated to {version}");
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
                    log($"  Starting {serviceName}...");
                    controller.Start();
                    await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));
                    log($"  {serviceName} started.");
                }
                else
                {
                    log($"  {serviceName} already running");
                }
            }
            catch (InvalidOperationException)
            {
                log($"  {serviceName} not found");
            }
            catch (Exception ex)
            {
                log($"  Warning: {ex.Message}");
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
    public string Version { get; set; } = "";
    public string BuildDate { get; set; } = "";
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
    }

    void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select DataForgeStudio installation directory",
            ShowNewFolderButton = false
        };

        if (Directory.Exists(_installPathTextBox.Text))
        {
            dialog.SelectedPath = _installPathTextBox.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _installPathTextBox.Text = dialog.SelectedPath;
        }
    }

    async void InstallButton_Click(object? sender, EventArgs e)
    {
        var installPath = _installPathTextBox.Text.Trim();

        // Validate
        if (string.IsNullOrEmpty(installPath))
        {
            MessageBox.Show("Please enter the installation directory.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Directory.Exists(installPath))
        {
            MessageBox.Show($"Installation directory not found: {installPath}", "Validation Error",
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
            AppendLog("Installation completed successfully!");
            if (!string.IsNullOrEmpty(backupPath))
            {
                AppendLog($"Backup: {backupPath}");
            }
            AppendLog("========================================");

            MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _cancelButton.Text = "Close";
            _cancelButton.Enabled = true;
        }
        else
        {
            AppendLog("");
            AppendLog($"ERROR: {message}");

            MessageBox.Show(message, "Installation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

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
