# 备份路径文件夹选择器实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为备份管理页面添加可视化的文件夹选择器，替代手动输入路径。

**Architecture:** 后端提供目录浏览 API（安全过滤敏感目录），前端使用 el-tree-select 组件懒加载目录树。

**Tech Stack:** ASP.NET Core 8.0, Vue 3, Element Plus

---

## Task 1: 创建后端 DTO 和接口

**Files:**
- Create: `backend/src/DataForgeStudio.Domain/DTOs/DirectoryInfoDto.cs`
- Create: `backend/src/DataForgeStudio.Core/Interfaces/IDirectoryService.cs`

**Step 1: 创建 DirectoryInfoDto**

```csharp
// backend/src/DataForgeStudio.Domain/DTOs/DirectoryInfoDto.cs
namespace DataForgeStudio.Domain.DTOs;

/// <summary>
/// 目录信息 DTO（用于备份路径选择）
/// </summary>
public class DirectoryInfoDto
{
    /// <summary>
    /// 目录名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 完整路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 是否有子目录
    /// </summary>
    public bool HasChildren { get; set; }

    /// <summary>
    /// 是否为驱动器根目录
    /// </summary>
    public bool IsDrive { get; set; }
}
```

**Step 2: 创建 IDirectoryService 接口**

```csharp
// backend/src/DataForgeStudio.Core/Interfaces/IDirectoryService.cs
using DataForgeStudio.Domain.DTOs;

namespace DataForgeStudio.Core.Interfaces;

public interface IDirectoryService
{
    /// <summary>
    /// 获取目录列表
    /// </summary>
    /// <param name="path">父目录路径，为空则返回驱动器列表</param>
    /// <returns>目录信息列表</returns>
    Task<List<DirectoryInfoDto>> GetDirectoriesAsync(string? path = null);
}
```

**Step 3: 提交**

```bash
git add backend/src/DataForgeStudio.Domain/DTOs/DirectoryInfoDto.cs backend/src/DataForgeStudio.Core/Interfaces/IDirectoryService.cs
git commit -m "feat: add DirectoryInfoDto and IDirectoryService interface"
```

---

## Task 2: 实现 DirectoryService 服务

**Files:**
- Create: `backend/src/DataForgeStudio.Core/Services/DirectoryService.cs`

**Step 1: 创建 DirectoryService**

```csharp
// backend/src/DataForgeStudio.Core/Services/DirectoryService.cs
using System.Runtime.InteropServices;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Domain.DTOs;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// 目录服务 - 提供服务器目录浏览功能
/// </summary>
public class DirectoryService : IDirectoryService
{
    private readonly ILogger<DirectoryService> _logger;

    // 敏感目录黑名单（不显示）
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Windows",
        "Program Files",
        "Program Files (x86)",
        "System Volume Information",
        "$RECYCLE.BIN",
        "ProgramData",
        "Users",
        "PerfLogs",
        "Recovery",
        "Boot",
        "System Reserved"
    };

    public DirectoryService(ILogger<DirectoryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取目录列表
    /// </summary>
    public async Task<List<DirectoryInfoDto>> GetDirectoriesAsync(string? path = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                // 返回可用驱动器列表
                return await GetDrivesAsync();
            }
            else
            {
                // 返回指定路径下的子目录
                return await GetSubDirectoriesAsync(path);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "访问目录被拒绝: {Path}", path);
            return new List<DirectoryInfoDto>();
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "目录不存在: {Path}", path);
            return new List<DirectoryInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取目录列表失败: {Path}", path);
            throw;
        }
    }

    /// <summary>
    /// 获取可用驱动器列表
    /// </summary>
    private Task<List<DirectoryInfoDto>> GetDrivesAsync()
    {
        var result = new List<DirectoryInfoDto>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    result.Add(new DirectoryInfoDto
                    {
                        Name = $"{drive.Name} ({GetDriveLabel(drive)})",
                        Path = drive.RootDirectory.FullName.TrimEnd('\\'),
                        HasChildren = true,
                        IsDrive = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "无法访问驱动器: {Drive}", drive.Name);
            }
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// 获取驱动器标签
    /// </summary>
    private static string GetDriveLabel(DriveInfo drive)
    {
        try
        {
            return string.IsNullOrWhiteSpace(drive.VolumeLabel)
                ? "本地磁盘"
                : drive.VolumeLabel;
        }
        catch
        {
            return "本地磁盘";
        }
    }

    /// <summary>
    /// 获取子目录列表
    /// </summary>
    private Task<List<DirectoryInfoDto>> GetSubDirectoriesAsync(string path)
    {
        var result = new List<DirectoryInfoDto>();

        // 验证路径格式
        if (!IsValidPath(path))
        {
            _logger.LogWarning("无效的路径格式: {Path}", path);
            return Task.FromResult(result);
        }

        var directoryInfo = new DirectoryInfo(path);

        if (!directoryInfo.Exists)
        {
            _logger.LogWarning("目录不存在: {Path}", path);
            return Task.FromResult(result);
        }

        foreach (var dir in directoryInfo.GetDirectories())
        {
            try
            {
                // 过滤敏感目录
                if (ShouldExclude(dir.Name))
                {
                    continue;
                }

                // 检查是否可访问
                bool hasChildren = false;
                try
                {
                    hasChildren = dir.GetDirectories().Length > 0;
                }
                catch (UnauthorizedAccessException)
                {
                    // 无权限访问子目录，跳过
                    continue;
                }

                result.Add(new DirectoryInfoDto
                {
                    Name = dir.Name,
                    Path = dir.FullName,
                    HasChildren = hasChildren,
                    IsDrive = false
                });
            }
            catch (UnauthorizedAccessException)
            {
                // 无权限访问此目录，跳过
                continue;
            }
        }

        // 按名称排序
        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(result);
    }

    /// <summary>
    /// 检查目录是否应该被排除
    /// </summary>
    private static bool ShouldExclude(string directoryName)
    {
        return ExcludedDirectories.Contains(directoryName);
    }

    /// <summary>
    /// 验证路径格式
    /// </summary>
    private static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // 检查是否为合法的 Windows 路径
        try
        {
            // 检查路径中是否包含非法字符
            char[] invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
                return false;

            // 检查路径格式（如 C:\ 或 \\server\share）
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true; // 非 Windows 系统跳过格式检查

            // Windows 路径格式检查
            return path.Length >= 2 && path[1] == ':';
        }
        catch
        {
            return false;
        }
    }
}
```

**Step 2: 提交**

```bash
git add backend/src/DataForgeStudio.Core/Services/DirectoryService.cs
git commit -m "feat: implement DirectoryService for secure directory browsing"
```

---

## Task 3: 注册服务并添加 API 端点

**Files:**
- Modify: `backend/src/DataForgeStudio.Api/Program.cs` (服务注册)
- Modify: `backend/src/DataForgeStudio.Api/Controllers/SystemController.cs` (新增端点)

**Step 1: 在 Program.cs 中注册服务**

找到服务注册区域，添加：

```csharp
// 在 Program.cs 的服务注册部分添加
builder.Services.AddScoped<IDirectoryService, DirectoryService>();
```

**Step 2: 在 SystemController.cs 中添加端点**

找到 `SystemController.cs`，添加新的端点：

```csharp
// 在 SystemController 类中添加

/// <summary>
/// 获取目录列表（用于备份路径选择）
/// </summary>
/// <param name="path">父目录路径，为空则返回驱动器列表</param>
[HttpGet("directories")]
public async Task<ApiResponse<List<DirectoryInfoDto>>> GetDirectories([FromQuery] string? path = null)
{
    try
    {
        var result = await _directoryService.GetDirectoriesAsync(path);
        return ApiResponse<List<DirectoryInfoDto>>.Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取目录列表失败");
        return ApiResponse<List<DirectoryInfoDto>>.Fail("获取目录列表失败: " + ex.Message);
    }
}
```

**Step 3: 添加必要的 using 和依赖注入**

在 SystemController.cs 顶部添加：

```csharp
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Domain.DTOs;
```

在 SystemController 构造函数中添加 IDirectoryService 依赖：

```csharp
private readonly IDirectoryService _directoryService;

public SystemController(
    // ... 现有依赖
    IDirectoryService directoryService)
{
    // ... 现有赋值
    _directoryService = directoryService;
}
```

**Step 4: 提交**

```bash
git add backend/src/DataForgeStudio.Api/Program.cs backend/src/DataForgeStudio.Api/Controllers/SystemController.cs
git commit -m "feat: add directories API endpoint for backup path selection"
```

---

## Task 4: 前端 - 添加 API 调用

**Files:**
- Modify: `frontend/src/api/request.js`

**Step 1: 在 systemApi 对象中添加 getDirectories 方法**

找到 `request.js` 中的 `systemApi` 对象，添加：

```javascript
export const systemApi = {
  // ... 现有方法

  // 获取目录列表（用于备份路径选择）
  getDirectories: (path = null) =>
    request.get('/system/directories', { params: { path } }),
}
```

**Step 2: 提交**

```bash
git add frontend/src/api/request.js
git commit -m "feat: add getDirectories API method"
```

---

## Task 5: 前端 - 创建 DirectorySelector 组件

**Files:**
- Create: `frontend/src/components/DirectorySelector.vue`

**Step 1: 创建 DirectorySelector 组件**

```vue
<!-- frontend/src/components/DirectorySelector.vue -->
<template>
  <div class="directory-selector">
    <el-tree-select
      ref="treeSelectRef"
      v-model="selectedPath"
      :data="treeData"
      :props="treeProps"
      :load="loadDirectories"
      lazy
      clearable
      filterable
      :placeholder="placeholder"
      check-strictly
      :render-after-expand="false"
      @node-click="handleNodeClick"
      @clear="handleClear"
    >
      <template #default="{ data }">
        <span class="tree-node">
          <el-icon v-if="data.isDrive" style="margin-right: 4px;"><FolderOpened /></el-icon>
          <el-icon v-else style="margin-right: 4px;"><Folder /></el-icon>
          <span>{{ data.name }}</span>
        </span>
      </template>
    </el-tree-select>
  </div>
</template>

<script setup>
import { ref, watch, computed } from 'vue'
import { Folder, FolderOpened } from '@element-plus/icons-vue'
import { systemApi } from '../api/request'
import { ElMessage } from 'element-plus'

const props = defineProps({
  modelValue: {
    type: String,
    default: ''
  },
  placeholder: {
    type: String,
    default: '选择备份目录'
  }
})

const emit = defineEmits(['update:modelValue', 'change'])

const treeSelectRef = ref(null)
const selectedPath = ref(props.modelValue)
const treeData = ref([])

const treeProps = {
  label: 'name',
  value: 'path',
  children: 'children',
  isLeaf: (data) => !data.hasChildren
}

// 监听外部 v-model 变化
watch(() => props.modelValue, (newVal) => {
  selectedPath.value = newVal
})

// 监听内部选择变化
watch(selectedPath, (newVal) => {
  emit('update:modelValue', newVal)
})

// 懒加载目录
const loadDirectories = async (node, resolve) => {
  try {
    const path = node.level === 0 ? null : node.data?.path
    const res = await systemApi.getDirectories(path)

    if (res.success && res.data) {
      // 如果是根节点加载驱动器，直接返回
      if (node.level === 0) {
        treeData.value = res.data
      }
      resolve(res.data)
    } else {
      resolve([])
    }
  } catch (error) {
    console.error('加载目录失败:', error)
    ElMessage.warning('加载目录失败，请检查服务器权限')
    resolve([])
  }
}

// 节点点击
const handleNodeClick = (data) => {
  selectedPath.value = data.path
  emit('change', data.path)
}

// 清除选择
const handleClear = () => {
  selectedPath.value = ''
  emit('change', '')
}

// 初始加载根目录（驱动器列表）
const initTree = async () => {
  try {
    const res = await systemApi.getDirectories(null)
    if (res.success && res.data) {
      treeData.value = res.data
    }
  } catch (error) {
    console.error('初始化目录树失败:', error)
  }
}

// 组件挂载时初始化
initTree()

// 暴露刷新方法
defineExpose({
  refresh: initTree
})
</script>

<style scoped>
.directory-selector {
  width: 100%;
}

.tree-node {
  display: flex;
  align-items: center;
}

.directory-selector :deep(.el-tree-select) {
  width: 100%;
}
</style>
```

**Step 2: 提交**

```bash
git add frontend/src/components/DirectorySelector.vue
git commit -m "feat: create DirectorySelector component with lazy loading"
```

---

## Task 6: 前端 - 改造 BackupManagement.vue

**Files:**
- Modify: `frontend/src/views/system/BackupManagement.vue`

**Step 1: 导入 DirectorySelector 组件**

在 `<script setup>` 部分添加导入：

```javascript
import DirectorySelector from '../../components/DirectorySelector.vue'
```

**Step 2: 修改创建备份区域的路径输入**

将现有的路径输入框（约第12-23行）替换为：

```vue
<!-- 替换原来的 el-input 为 DirectorySelector -->
<DirectorySelector
  v-model="backupForm.backupPath"
  placeholder="选择备份路径"
  style="width: 280px;"
/>
```

**Step 3: 修改备份计划编辑弹窗的路径输入**

在计划编辑对话框中（约第223-231行），将路径输入框替换为：

```vue
<el-form-item label="备份路径" required>
  <DirectorySelector
    v-model="scheduleForm.backupPath"
    placeholder="选择备份路径"
  />
</el-form-item>
```

**Step 4: 提交**

```bash
git add frontend/src/views/system/BackupManagement.vue
git commit -m "feat: integrate DirectorySelector into BackupManagement"
```

---

## Task 7: 测试验证

**Step 1: 启动后端服务**

```bash
cd backend && dotnet run --project src/DataForgeStudio.Api
```

**Step 2: 测试 API 端点**

使用浏览器或 Postman 测试：
- `GET /api/system/directories` - 应返回驱动器列表
- `GET /api/system/directories?path=C:` - 应返回 C: 盘下的目录

**Step 3: 启动前端服务**

```bash
cd frontend && npm run dev
```

**Step 4: 验证功能**

1. 登录系统
2. 进入备份管理页面
3. 点击路径选择器，应显示驱动器列表
4. 展开驱动器，应显示子目录
5. 选择目录后，路径应正确填充
6. 创建备份计划，验证路径选择器正常工作

**Step 5: 最终提交**

```bash
git add -A
git commit -m "feat: complete backup path selector implementation"
```

---

## 文件变更摘要

| 文件 | 操作 |
|------|------|
| `backend/src/DataForgeStudio.Domain/DTOs/DirectoryInfoDto.cs` | 新增 |
| `backend/src/DataForgeStudio.Core/Interfaces/IDirectoryService.cs` | 新增 |
| `backend/src/DataForgeStudio.Core/Services/DirectoryService.cs` | 新增 |
| `backend/src/DataForgeStudio.Api/Program.cs` | 修改 |
| `backend/src/DataForgeStudio.Api/Controllers/SystemController.cs` | 修改 |
| `frontend/src/api/request.js` | 修改 |
| `frontend/src/components/DirectorySelector.vue` | 新增 |
| `frontend/src/views/system/BackupManagement.vue` | 修改 |
