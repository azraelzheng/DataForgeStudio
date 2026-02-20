using System.Diagnostics;
using System.IO;
using System.Text;

namespace DeployManager.Services;

/// <summary>
/// Nginx 管理器实现
/// 使用 System.Diagnostics.Process 管理 Nginx 进程
/// </summary>
public class NginxManager : INginxManager
{
    private string? _nginxExePath;
    private string? _nginxDirectory;

    /// <summary>
    /// 检查 Nginx 是否已安装
    /// 通过检查 nginx.exe 是否存在来确定
    /// </summary>
    /// <returns>如果 Nginx 已安装返回 true，否则返回 false</returns>
    public bool IsNginxInstalled()
    {
        // 常见的 Nginx 安装路径
        var commonPaths = new[]
        {
            @"C:\nginx\nginx.exe",
            @"C:\Program Files\nginx\nginx.exe",
            @"C:\Program Files (x86)\nginx\nginx.exe"
        };

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                _nginxExePath = path;
                _nginxDirectory = Path.GetDirectoryName(path);
                Debug.WriteLine($"[NginxManager] 找到 Nginx: {path}");
                return true;
            }
        }

        // 尝试从 PATH 环境变量中查找
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
                        Debug.WriteLine($"[NginxManager] 从 PATH 找到 Nginx: {nginxPath}");
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NginxManager] 检查 PATH 环境变量时发生异常: {ex.Message}");
        }

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
    /// 更新 Nginx 配置文件
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    /// <param name="port">监听端口</param>
    /// <param name="backendUrl">后端服务地址</param>
    /// <exception cref="ArgumentNullException">参数为空</exception>
    /// <exception cref="FileNotFoundException">配置文件目录不存在</exception>
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
            var configDir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
                Debug.WriteLine($"[NginxManager] 创建配置目录: {configDir}");
            }

            // 生成简单的 Nginx 反向代理配置
            var configContent = GenerateConfig(port, backendUrl);

            File.WriteAllText(configPath, configContent, Encoding.UTF8);
            Debug.WriteLine($"[NginxManager] 配置文件已更新: {configPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NginxManager] 更新配置文件失败: {ex.Message}");
            throw new InvalidOperationException($"更新 Nginx 配置文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 生成 Nginx 配置文件内容
    /// </summary>
    /// <param name="port">监听端口</param>
    /// <param name="backendUrl">后端服务地址</param>
    /// <returns>配置文件内容</returns>
    private string GenerateConfig(int port, string backendUrl)
    {
        return $@"# DataForgeStudio Nginx Configuration
# Generated by DeployManager

worker_processes  1;

events {{
    worker_connections  1024;
}}

http {{
    include       mime.types;
    default_type  application/octet-stream;

    sendfile        on;
    keepalive_timeout  65;

    # Gzip 压缩
    gzip  on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;

    # 后端服务上游
    upstream backend {{
        server {backendUrl};
    }}

    server {{
        listen       {port};
        server_name  localhost;

        # 前端静态文件
        location / {{
            root   html;
            index  index.html index.htm;
            try_files $uri $uri/ /index.html;
        }}

        # API 反向代理
        location /api {{
            proxy_pass http://backend;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection ""upgrade"";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }}

        # 错误页面
        error_page   500 502 503 504  /50x.html;
        location = /50x.html {{
            root   html;
        }}
    }}
}}
";
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
