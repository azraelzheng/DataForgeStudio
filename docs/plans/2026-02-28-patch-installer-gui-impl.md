# PatchInstaller GUI 改进实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 简化补丁安装程序界面，实现中文化，自动从配置文件读取数据库信息。

**Architecture:** 修改现有的 Windows Forms 应用，移除数据库输入控件，添加自动配置读取功能，所有文本改为中文。

**Tech Stack:** C# .NET 8.0, Windows Forms

---

### Task 1: 修改 GUI 文本为中文

**Files:**
- Modify: `backend/tools/PatchInstaller/Program.cs` (PatchInstallerForm 类)

**Step 1: 修改 Form 标题和主标签**

在 `InitializeComponents()` 方法中，找到并修改以下文本：

```csharp
// 原: Text = $"DataForgeStudio Patch Installer - V{Program._patchVersion}";
Text = $"DataForgeStudio 补丁安装程序 - V{Program._patchVersion}";

// 原: Text = $"Patch Version: {Program._patchVersion}";
Text = $"补丁版本：{Program._patchVersion}";

// 原: Text = "Installation Directory:";
Text = "安装目录：";

// 原: Text = "...";
Text = "浏览...";
```

**Step 2: 修改进度条和日志标签**

```csharp
// 原: Text = "Installation Log:";
Text = "安装日志：";
```

**Step 3: 修改按钮文本**

```csharp
// 原: Text = "Install";
Text = "安装";

// 原: Text = "Cancel";
Text = "取消";
```

**Step 4: 验证编译**

Run: `dotnet build backend/tools/PatchInstaller/PatchInstaller.csproj -c Release`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add backend/tools/PatchInstaller/Program.cs
git commit -m "feat: PatchInstaller GUI Chinese localization"
```

---

### Task 2: 移除数据库设置控件

**Files:**
- Modify: `backend/tools/PatchInstaller/Program.cs` (PatchInstallerForm 类)

**Step 1: 移除数据库控件字段**

删除以下字段声明：

```csharp
// 删除这些字段
private TextBox _dbServerTextBox = null!;
private TextBox _dbNameTextBox = null!;
private TextBox _dbUserTextBox = null!;
private TextBox _dbPasswordTextBox = null!;
private GroupBox _dbGroup = null!;
```

**Step 2: 移除 InitializeComponents() 中的数据库控件代码**

删除 `_dbGroup` 及其所有子控件的创建代码（约 50 行）。

**Step 3: 调整后续控件位置**

由于移除了数据库设置区域（约 130px 高度），调整进度条和日志框的 Y 位置：

```csharp
// 进度条位置从 yPos += 140 改为直接在安装目录后
// 减少约 100px 的空间
```

**Step 4: 验证编译**

Run: `dotnet build backend/tools/PatchInstaller/PatchInstaller.csproj -c Release`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add backend/tools/PatchInstaller/Program.cs
git commit -m "refactor: remove database input controls from PatchInstaller GUI"
```

---

### Task 3: 添加自动读取配置功能

**Files:**
- Modify: `backend/tools/PatchInstaller/Program.cs`

**Step 1: 添加配置读取方法**

在 `PatchInstallerForm` 类中添加：

```csharp
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
        using var doc = System.Text.Json.JsonDocument.Parse(json);
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
```

**Step 2: 在目录选择后调用配置读取**

修改 `BrowseButton_Click` 方法：

```csharp
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
        LoadDatabaseConfig(dialog.SelectedPath);  // 添加这行
    }
}
```

**Step 3: 在 LoadDefaultValues 中也调用配置读取**

修改 `LoadDefaultValues()` 方法末尾：

```csharp
void LoadDefaultValues()
{
    // ... 现有代码 ...

    // 如果目录存在，自动读取配置
    if (Directory.Exists(_installPathTextBox.Text))
    {
        LoadDatabaseConfig(_installPathTextBox.Text);
    }
}
```

**Step 4: 验证编译**

Run: `dotnet build backend/tools/PatchInstaller/PatchInstaller.csproj -c Release`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add backend/tools/PatchInstaller/Program.cs
git commit -m "feat: auto-load database config from appsettings.json"
```

---

### Task 4: 修改 LoadDefaultValues 默认值

**Files:**
- Modify: `backend/tools/PatchInstaller/Program.cs`

**Step 1: 删除不再需要的默认值设置**

移除 `LoadDefaultValues()` 中的数据库默认值代码：

```csharp
// 删除以下代码：
// _dbServerTextBox.Text = Program._dbServer ?? "";
// _dbNameTextBox.Text = Program._dbName ?? "DataForgeStudio";
// _dbUserTextBox.Text = Program._dbUser ?? "sa";
// _dbPasswordTextBox.Text = Program._dbPassword ?? "";
```

**Step 2: 修改 InstallButton_Click 移除数据库控件引用**

移除对已删除控件的引用：

```csharp
// 修改前：
var (success, message, backupPath) = await Program.RunInstallationAsync(
    installPath,
    string.IsNullOrWhiteSpace(_dbServerTextBox.Text) ? null : _dbServerTextBox.Text,
    string.IsNullOrWhiteSpace(_dbNameTextBox.Text) ? null : _dbNameTextBox.Text,
    string.IsNullOrWhiteSpace(_dbUserTextBox.Text) ? null : _dbUserTextBox.Text,
    string.IsNullOrWhiteSpace(_dbPasswordTextBox.Text) ? null : _dbPasswordTextBox.Text,
    msg => AppendLog(msg)
);

// 修改后：
var (success, message, backupPath) = await Program.RunInstallationAsync(
    installPath,
    Program._dbServer,
    Program._dbName,
    Program._dbUser,
    Program._dbPassword,
    msg => AppendLog(msg)
);
```

**Step 3: 验证编译**

Run: `dotnet build backend/tools/PatchInstaller/PatchInstaller.csproj -c Release`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add backend/tools/PatchInstaller/Program.cs
git commit -m "refactor: use auto-loaded config instead of form inputs"
```

---

### Task 5: 修改消息框文本为中文

**Files:**
- Modify: `backend/tools/PatchInstaller/Program.cs`

**Step 1: 修改验证错误消息**

```csharp
// 原: MessageBox.Show("Please enter the installation directory.", "Validation Error", ...)
MessageBox.Show("请输入安装目录。", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);

// 原: MessageBox.Show($"Installation directory not found: {installPath}", "Validation Error", ...)
MessageBox.Show($"安装目录不存在：{installPath}", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
```

**Step 2: 修改安装结果消息**

```csharp
// 原: MessageBox.Show(message, "Success", ...)
MessageBox.Show(message, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

// 原: MessageBox.Show(message, "Installation Failed", ...)
MessageBox.Show(message, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error);

// 原: _cancelButton.Text = "Close";
_cancelButton.Text = "关闭";
```

**Step 3: 验证编译**

Run: `dotnet build backend/tools/PatchInstaller/PatchInstaller.csproj -c Release`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add backend/tools/PatchInstaller/Program.cs
git commit -m "feat: localize message boxes to Chinese"
```

---

### Task 6: 重新构建并测试补丁包

**Files:**
- Build: `dist/patches/DataForgeStudio-Patch-1.0.1.exe`

**Step 1: 构建补丁包**

Run: `cd scripts && powershell -ExecutionPolicy Bypass -File build-patch.ps1 -Version "1.0.1"`
Expected: Patch Build Successful

**Step 2: 手动测试 GUI**

双击 `dist/patches/DataForgeStudio-Patch-1.0.1.exe`，验证：
- [ ] 界面显示中文
- [ ] 没有数据库输入框
- [ ] 自动检测安装目录
- [ ] 点击"安装"可以正常执行

**Step 3: 最终 Commit**

```bash
git add dist/patches/
git commit -m "build: rebuild patch v1.0.1 with Chinese GUI"
```

---

## Summary

| Task | Description | Files Modified |
|------|-------------|----------------|
| 1 | GUI 文本中文化 | Program.cs |
| 2 | 移除数据库控件 | Program.cs |
| 3 | 添加配置自动读取 | Program.cs |
| 4 | 修改默认值逻辑 | Program.cs |
| 5 | 消息框中文化 | Program.cs |
| 6 | 重新构建测试 | dist/patches/ |
