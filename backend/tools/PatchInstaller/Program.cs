using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.ServiceProcess;
using System.Text.Json;

namespace PatchInstaller;

/// <summary>
/// DataForgeStudio Patch Installer
/// Single EXE with embedded patch resources
/// </summary>
class Program
{
    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "DataForgeStudio", "Logs", $"patch-install-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    // Embedded resource zip name
    private const string EmbeddedResourceName = "patch-resources.zip";

    private static string? _installPath;
    private static string? _dbServer;
    private static string? _dbName;
    private static string? _dbUser;
    private static string? _dbPassword;
    private static bool _embeddedMode = false;

    static async Task<int> Main(string[] args)
    {
        Console.Title = "DataForgeStudio Patch Installer";
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        PrintHeader();

        // Check if we have embedded resources
        _embeddedMode = HasEmbeddedResources();

        if (_embeddedMode)
        {
            Log("Running in embedded mode (single EXE patch)");
        }

        // Parse arguments
        if (!ParseArguments(args))
        {
            PrintUsage();
            return 1;
        }

        try
        {
            // Ensure log directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);

            // Extract embedded resources or read from zip
            string tempPatchDir;
            PatchInfo? patchInfo;

            if (_embeddedMode)
            {
                Log("Extracting embedded patch resources...");
                tempPatchDir = ExtractEmbeddedResources();
                patchInfo = await ReadPatchInfoFromDirectory(tempPatchDir);
            }
            else
            {
                Log("ERROR: No embedded resources found. This patch was not built correctly.");
                return 1;
            }

            if (patchInfo == null)
            {
                Log("ERROR: Invalid patch - missing patch-info.json");
                return 1;
            }

            Log($"Patch Version: {patchInfo.Version}");
            Log($"Patch Type: {patchInfo.Type}");
            Log($"Install Path: {_installPath}");
            Log("");

            // Confirm installation
            if (!args.Contains("-y") && !args.Contains("--yes"))
            {
                Console.Write("Continue with installation? (Y/N): ");
                var response = Console.ReadLine();
                if (response?.ToUpper() != "Y")
                {
                    Log("Installation cancelled by user.");
                    return 0;
                }
            }

            // Step 1: Stop services
            Log("[Step 1/6] Stopping services...");
            await StopServicesAsync();

            // Step 2: Backup current version
            Log("[Step 2/6] Creating backup...");
            var backupPath = await CreateBackupAsync(_installPath!, patchInfo.Version);

            // Step 3: Update files
            Log("[Step 3/6] Updating files...");
            await UpdateFilesFromDirectoryAsync(tempPatchDir, _installPath!);

            // Step 4: Run SQL scripts (if any)
            Log("[Step 4/6] Running database updates...");
            await RunDatabaseUpdatesFromDirectoryAsync(tempPatchDir, patchInfo.Version);

            // Step 5: Update version info
            Log("[Step 5/6] Updating version info...");
            await UpdateVersionInfoAsync(_installPath!, patchInfo.Version);

            // Step 6: Start services
            Log("[Step 6/6] Starting services...");
            await StartServicesAsync();

            // Cleanup temp directory
            if (Directory.Exists(tempPatchDir))
            {
                Directory.Delete(tempPatchDir, true);
            }

            Log("");
            Log("========================================");
            Log("Patch installation completed successfully!");
            Log($"Version: {patchInfo.Version}");
            Log($"Backup: {backupPath}");
            Log($"Log: {LogFile}");
            Log("========================================");

            return 0;
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    static void PrintHeader()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  DataForgeStudio Patch Installer");
        Console.WriteLine("========================================");
        Console.WriteLine();
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: PatchInstaller.exe [options]");
        Console.WriteLine();
        Console.WriteLine("Required options:");
        Console.WriteLine("  -i, --install-path <path> Installation directory");
        Console.WriteLine();
        Console.WriteLine("Database options (required for SQL updates):");
        Console.WriteLine("  --db-server <server>    Database server");
        Console.WriteLine("  --db-name <name>        Database name");
        Console.WriteLine("  --db-user <user>        Database username");
        Console.WriteLine("  --db-password <pass>    Database password");
        Console.WriteLine();
        Console.WriteLine("Other options:");
        Console.WriteLine("  -y, --yes               Skip confirmation prompt");
        Console.WriteLine("  -h, --help              Show this help message");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  PatchInstaller.exe -i \"C:\\Program Files\\DataForgeStudio\"");
        Console.WriteLine();
        Console.WriteLine("With database update:");
        Console.WriteLine("  PatchInstaller.exe -i \"C:\\Program Files\\DataForgeStudio\" --db-server localhost --db-name DataForgeStudio --db-user sa --db-password xxx");
    }

    static bool ParseArguments(string[] args)
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
                case "-h":
                case "--help":
                    return false;
            }
        }

        if (string.IsNullOrEmpty(_installPath))
        {
            Console.WriteLine("ERROR: Installation path is required");
            return false;
        }

        if (!Directory.Exists(_installPath))
        {
            Console.WriteLine($"ERROR: Installation directory not found: {_installPath}");
            return false;
        }

        return true;
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
                Log($"  Extracted embedded resources");
                break;
            }
        }

        return tempDir;
    }

    static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logMessage = $"[{timestamp}] {message}";
        Console.WriteLine(logMessage);

        try
        {
            File.AppendAllText(LogFile, logMessage + Environment.NewLine);
        }
        catch { /* Ignore log file errors */ }
    }

    static async Task<PatchInfo?> ReadPatchInfoFromDirectory(string directory)
    {
        var infoFile = Path.Combine(directory, "patch-info.json");
        if (!File.Exists(infoFile)) return null;

        var json = await File.ReadAllTextAsync(infoFile);
        return JsonSerializer.Deserialize<PatchInfo>(json);
    }

    static async Task StopServicesAsync()
    {
        var services = new[] { "DFAppService", "DFWebService" };

        foreach (var serviceName in services)
        {
            try
            {
                using var controller = new ServiceController(serviceName);
                if (controller.Status == ServiceControllerStatus.Running)
                {
                    Log($"  Stopping {serviceName}...");
                    controller.Stop();
                    await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));
                    Log($"  {serviceName} stopped.");
                }
                else
                {
                    Log($"  {serviceName} is not running (status: {controller.Status})");
                }
            }
            catch (InvalidOperationException)
            {
                Log($"  {serviceName} not found, skipping...");
            }
            catch (Exception ex)
            {
                Log($"  Warning: Failed to stop {serviceName}: {ex.Message}");
            }
        }
    }

    static async Task<string> CreateBackupAsync(string installPath, string version)
    {
        var backupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DataForgeStudio", "Backups", $"pre-patch-{version}-{DateTime.Now:yyyyMMdd-HHmmss}");

        Directory.CreateDirectory(backupDir);

        // Backup Server directory
        var serverPath = Path.Combine(installPath, "Server");
        if (Directory.Exists(serverPath))
        {
            var backupServerPath = Path.Combine(backupDir, "Server");
            await Task.Run(() => CopyDirectory(serverPath, backupServerPath, "*.dll"));
            Log($"  Backed up Server files");
        }

        // Backup WebSite directory
        var webSitePath = Path.Combine(installPath, "WebServer", "html");
        if (Directory.Exists(webSitePath))
        {
            var backupWebPath = Path.Combine(backupDir, "WebSite");
            await Task.Run(() => CopyDirectory(webSitePath, backupWebPath));
            Log($"  Backed up WebSite files");
        }

        return backupDir;
    }

    static async Task UpdateFilesFromDirectoryAsync(string sourceDir, string installPath)
    {
        // Update Server files
        var serverSource = Path.Combine(sourceDir, "Server");
        if (Directory.Exists(serverSource))
        {
            var serverTarget = Path.Combine(installPath, "Server");
            foreach (var file in Directory.GetFiles(serverSource, "*.dll"))
            {
                var targetFile = Path.Combine(serverTarget, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
                Log($"  Updated: {Path.GetFileName(file)}");
            }
        }

        // Update WebSite files
        var webSiteSource = Path.Combine(sourceDir, "WebSite");
        if (Directory.Exists(webSiteSource))
        {
            var webSiteTarget = Path.Combine(installPath, "WebServer", "html");
            await Task.Run(() => CopyDirectory(webSiteSource, webSiteTarget));
            Log($"  Updated WebSite files");
        }

        await Task.CompletedTask;
    }

    static async Task RunDatabaseUpdatesFromDirectoryAsync(string sourceDir, string version)
    {
        // Check if SQL connection info is provided
        if (string.IsNullOrEmpty(_dbServer) || string.IsNullOrEmpty(_dbName))
        {
            Log("  No database connection info provided, skipping SQL updates");
            return;
        }

        var sqlDir = Path.Combine(sourceDir, "sql");
        if (!Directory.Exists(sqlDir))
        {
            Log("  No SQL scripts found in patch");
            return;
        }

        // Build connection string
        var connectionString = $"Server={_dbServer};Database={_dbName};" +
                               $"User Id={_dbUser};Password={_dbPassword};" +
                               "TrustServerCertificate=True;Connection Timeout=30;";

        Log($"  Connecting to database: {_dbServer}/{_dbName}");

        var sqlFiles = Directory.GetFiles(sqlDir, "*.sql").OrderBy(f => f);

        foreach (var sqlFile in sqlFiles)
        {
            try
            {
                var sql = await File.ReadAllTextAsync(sqlFile);
                Log($"  Executing: {Path.GetFileName(sqlFile)}");
                await ExecuteSqlScriptAsync(connectionString, sql);
                Log($"  Executed successfully");
            }
            catch (Exception ex)
            {
                Log($"  ERROR: {ex.Message}");
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

    static async Task UpdateVersionInfoAsync(string installPath, string version)
    {
        var versionFile = Path.Combine(installPath, "version.txt");
        await File.WriteAllTextAsync(versionFile, $"Version: {version}{Environment.NewLine}Installed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log($"  Version file updated to {version}");
    }

    static async Task StartServicesAsync()
    {
        var services = new[] { "DFAppService", "DFWebService" };

        foreach (var serviceName in services)
        {
            try
            {
                using var controller = new ServiceController(serviceName);
                if (controller.Status == ServiceControllerStatus.Stopped)
                {
                    Log($"  Starting {serviceName}...");
                    controller.Start();
                    await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));
                    Log($"  {serviceName} started.");
                }
                else
                {
                    Log($"  {serviceName} is not stopped (status: {controller.Status})");
                }
            }
            catch (InvalidOperationException)
            {
                Log($"  {serviceName} not found, skipping...");
            }
            catch (Exception ex)
            {
                Log($"  Warning: Failed to start {serviceName}: {ex.Message}");
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
