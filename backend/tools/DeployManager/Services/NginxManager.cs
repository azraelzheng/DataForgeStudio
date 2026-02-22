using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DeployManager.Models;

namespace DeployManager.Services;

/// <summary>
/// Nginx 管理器实现
/// 使用 System.Diagnostics.Process 管理 Nginx 进程
/// </summary>
public class NginxManager : INginxManager
{
    private readonly IConfigService _configService;
    private string? _nginxExePath;
    private string? _nginxDirectory;

    /// <summary>
    /// 初始化 Nginx 管理器
    /// </summary>
    /// <param name="configService">配置服务</param>
    public NginxManager(IConfigService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        FileLogger.Info("[NginxManager] 初始化完成");
    }

    /// <summary>
    /// 检查 Nginx 是否已安装
    /// 通过检查 nginx.exe 是否存在来确定
    /// </summary>
    /// <returns>如果 Nginx 已安装返回 true，否则返回 false</returns>
    public bool IsNginxInstalled()
    {
        FileLogger.Info("=== IsNginxInstalled() 开始 ===");
        Debug.WriteLine("[NginxManager] === IsNginxInstalled() 开始 ===");

        // 1. 首先检查捆绑的 Nginx（在安装目录下）
        try
        {
            FileLogger.Info("步骤1: 使用 InstallPath 检查捆绑的 Nginx...");
            var installPath = _configService.InstallPath;

            FileLogger.Info($"安装路径: {installPath ?? "null"}");
            Debug.WriteLine($"[NginxManager] 安装路径: {installPath ?? "null"}");

            if (!string.IsNullOrEmpty(installPath))
            {
                var bundledNginxPath = Path.Combine(installPath, "WebServer", "nginx.exe");
                FileLogger.Info($"检查捆绑的 Nginx 路径: {bundledNginxPath}");
                FileLogger.Info($"文件存在: {File.Exists(bundledNginxPath)}");
                Debug.WriteLine($"[NginxManager] 检查捆绑的 Nginx: {bundledNginxPath}");
                Debug.WriteLine($"[NginxManager] 文件存在: {File.Exists(bundledNginxPath)}");

                if (File.Exists(bundledNginxPath))
                {
                    _nginxExePath = bundledNginxPath;
                    _nginxDirectory = Path.GetDirectoryName(bundledNginxPath);
                    FileLogger.Info($"找到捆绑的 Nginx: {bundledNginxPath}");
                    Debug.WriteLine($"[NginxManager] 找到捆绑的 Nginx: {bundledNginxPath}");
                    return true;
                }

                // 检查目录是否存在
                var nginxDir = Path.Combine(installPath, "WebServer");
                FileLogger.Info($"WebServer 目录存在: {Directory.Exists(nginxDir)}");
                if (Directory.Exists(nginxDir))
                {
                    var files = Directory.GetFiles(nginxDir, "*.exe");
                    FileLogger.Info($"WebServer 目录中的 exe 文件: {string.Join(", ", files)}");
                }
            }
            else
            {
                FileLogger.Warning("安装路径为空!");
                Debug.WriteLine("[NginxManager] 警告: 安装路径为空!");
            }
        }
        catch (Exception ex)
        {
            FileLogger.Error("检查捆绑 Nginx 时发生异常", ex);
            Debug.WriteLine($"[NginxManager] 检查捆绑 Nginx 时发生异常: {ex.Message}");
            Debug.WriteLine($"[NginxManager] 堆栈跟踪: {ex.StackTrace}");
        }

        // 2. 使用 GetNginxPath 检查
        try
        {
            FileLogger.Info("步骤2: 使用 GetNginxPath() 检查...");
            var nginxPath = _configService.GetNginxPath();

            if (!string.IsNullOrEmpty(nginxPath))
            {
                var nginxExePath = Path.Combine(nginxPath, "nginx.exe");
                FileLogger.Info($"检查 NginxPath 中的 nginx.exe: {nginxExePath}");
                FileLogger.Info($"文件存在: {File.Exists(nginxExePath)}");

                if (File.Exists(nginxExePath))
                {
                    _nginxExePath = nginxExePath;
                    _nginxDirectory = nginxPath;
                    FileLogger.Info($"从 GetNginxPath() 找到 Nginx: {nginxExePath}");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            FileLogger.Error("检查 GetNginxPath() 时发生异常", ex);
        }

        // 3. 检查常见的独立 Nginx 安装路径
        FileLogger.Info("步骤3: 检查常见安装路径...");
        var commonPaths = new[]
        {
            @"C:\nginx\nginx.exe",
            @"C:\Program Files\nginx\nginx.exe",
            @"C:\Program Files (x86)\nginx\nginx.exe"
        };

        foreach (var path in commonPaths)
        {
            FileLogger.Info($"检查: {path}, 存在: {File.Exists(path)}");
            if (File.Exists(path))
            {
                _nginxExePath = path;
                _nginxDirectory = Path.GetDirectoryName(path);
                FileLogger.Info($"找到 Nginx: {path}");
                Debug.WriteLine($"[NginxManager] 找到 Nginx: {path}");
                return true;
            }
        }

        // 4. 尝试从 PATH 环境变量中查找
        FileLogger.Info("步骤4: 检查 PATH 环境变量...");
        try
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                var paths = pathEnv.Split(Path.PathSeparator);
                foreach (var p in paths)
                {
                    var nginxPath = Path.Combine(p.Trim('"'), "nginx.exe");
                    if (File.Exists(nginxPath))
                    {
                        _nginxExePath = nginxPath;
                        _nginxDirectory = p.Trim('"');
                        FileLogger.Info($"从 PATH 找到 Nginx: {nginxPath}");
                        Debug.WriteLine($"[NginxManager] 从 PATH 找到 Nginx: {nginxPath}");
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            FileLogger.Error("检查 PATH 环境变量时发生异常", ex);
        }

        FileLogger.Warning("未找到 Nginx 安装");
        Debug.WriteLine("[NginxManager] 未找到 Nginx 安装");
        return false;
    }

    /// <summary>
    /// 启动 Nginx 服务
    /// </summary>
    /// <param name="configPath">配置文件路径（可选，使用默认配置则传 null）</param>
    /// <exception cref="InvalidOperationException">Nginx 未安装或启动失败</exception>
    public async Task StartAsync(string configPath)
    {
        try
        {
            if (string.IsNullOrEmpty(_nginxExePath))
            {
                IsNginxInstalled();
            }

            if (string.IsNullOrEmpty(_nginxExePath) || !File.Exists(_nginxExePath))
            {
                throw new InvalidOperationException("Nginx 未安装或找不到 nginx.exe");
            }

            // 检查是否已有 Nginx 进程在运行
            if (IsNginxRunning())
            {
                Debug.WriteLine("[NginxManager] Nginx 已在运行中");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _nginxExePath,
                WorkingDirectory = _nginxDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // 如果指定了配置文件路径
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                startInfo.Arguments = $"-c \"{configPath}\"";
                Debug.WriteLine($"[NginxManager] 使用配置文件: {configPath}");
            }

            Debug.WriteLine($"[NginxManager] 正在启动 Nginx: {_nginxExePath}");

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("无法启动 Nginx 进程");
            }

            // 等待进程启动
            await Task.Run(() => process.WaitForExit(5000));

            // 检查是否启动成功
            await Task.Delay(1000); // 等待 Nginx 完全启动

            if (IsNginxRunning())
            {
                Debug.WriteLine("[NginxManager] Nginx 启动成功");
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Nginx 启动失败: {error}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NginxManager] 启动 Nginx 失败: {ex.Message}");
            throw new InvalidOperationException($"启动 Nginx 失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 停止 Nginx 服务
    /// 使用 quit 信号优雅地停止 Nginx
    /// </summary>
    /// <exception cref="InvalidOperationException">Nginx 未安装或停止失败</exception>
    public async Task StopAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_nginxExePath))
            {
                IsNginxInstalled();
            }

            if (string.IsNullOrEmpty(_nginxExePath) || !File.Exists(_nginxExePath))
            {
                throw new InvalidOperationException("Nginx 未安装或找不到 nginx.exe");
            }

            if (!IsNginxRunning())
            {
                Debug.WriteLine("[NginxManager] Nginx 未运行");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _nginxExePath,
                Arguments = "-s quit", // 优雅停止
                WorkingDirectory = _nginxDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Debug.WriteLine("[NginxManager] 正在停止 Nginx...");

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("无法执行 Nginx 停止命令");
            }

            await Task.Run(() => process.WaitForExit(10000));

            // 等待进程完全停止
            var maxWait = 10;
            while (IsNginxRunning() && maxWait > 0)
            {
                await Task.Delay(1000);
                maxWait--;
            }

            if (IsNginxRunning())
            {
                // 强制终止 Nginx 进程
                Debug.WriteLine("[NginxManager] 优雅停止超时，尝试强制停止...");
                ForceKillNginx();
            }

            Debug.WriteLine("[NginxManager] Nginx 已停止");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NginxManager] 停止 Nginx 失败: {ex.Message}");
            throw new InvalidOperationException($"停止 Nginx 失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 更新 Nginx 配置文件（保留原有配置结构，只更新端口和后端地址）
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    /// <param name="port">监听端口</param>
    /// <param name="backendUrl">后端服务地址（如 http://127.0.0.1:5000）</param>
    /// <exception cref="ArgumentNullException">参数为空</exception>
    /// <exception cref="FileNotFoundException">配置文件不存在</exception>
    public void UpdateConfig(string configPath, int port, string backendUrl)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            throw new ArgumentNullException(nameof(configPath));
        }

        if (port <= 0 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "端口号必须在 1-65535 之间");
        }

        if (string.IsNullOrWhiteSpace(backendUrl))
        {
            throw new ArgumentNullException(nameof(backendUrl));
        }

        try
        {
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Nginx 配置文件不存在: {configPath}");
            }

            var content = File.ReadAllText(configPath, Encoding.UTF8);

            // 更新 listen 端口（匹配 "listen 80;" 或 "listen       80;" 等格式）
            content = Regex.Replace(content, @"listen\s+\d+", $"listen       {port}");

            // 更新 proxy_pass 后端地址（匹配 proxy_pass http://127.0.0.1:5000/api/; 格式）
            content = Regex.Replace(
                content,
                @"proxy_pass\s+http://127\.0\.0\.1:\d+",
                $"proxy_pass         {backendUrl}");

            File.WriteAllText(configPath, content, Encoding.UTF8);
            FileLogger.Info($"Nginx 配置已更新: 端口={port}, 后端={backendUrl}");
            Debug.WriteLine($"[NginxManager] 配置文件已更新: {configPath}");
        }
        catch (Exception ex)
        {
            FileLogger.Error($"更新 Nginx 配置失败: {ex.Message}");
            Debug.WriteLine($"[NginxManager] 更新配置文件失败: {ex.Message}");
            throw new InvalidOperationException($"更新 Nginx 配置文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 检查 Nginx 进程是否正在运行
    /// </summary>
    /// <returns>如果 Nginx 正在运行返回 true，否则返回 false</returns>
    private bool IsNginxRunning()
    {
        try
        {
            var processes = Process.GetProcessesByName("nginx");
            var isRunning = processes.Length > 0;

            // 释放进程资源
            foreach (var process in processes)
            {
                process.Dispose();
            }

            return isRunning;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NginxManager] 检查 Nginx 进程时发生异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 强制终止所有 Nginx 进程
    /// </summary>
    private void ForceKillNginx()
    {
        try
        {
            var processes = Process.GetProcessesByName("nginx");
            foreach (var process in processes)
            {
                process.Kill();
                process.Dispose();
            }

            Debug.WriteLine("[NginxManager] 已强制终止所有 Nginx 进程");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NginxManager] 强制终止 Nginx 进程失败: {ex.Message}");
        }
    }
}
