# 备份路径文件夹选择器设计

## 概述

为备份管理和备份计划页面添加文件夹选择功能，替代手动输入路径，提升用户体验。

## 背景

- **问题**：当前备份路径需要手动输入，容易出错且不直观
- **约束**：Web 应用无法直接访问服务器文件系统，需要通过后端 API
- **目标**：提供可视化的目录树选择器，让用户方便地选择服务器上的备份路径

## 架构设计

```
┌─────────────────┐     GET /api/system/directories     ┌─────────────────┐
│   前端           │ ──────────────────────────────────► │   后端           │
│  el-tree        │     { path: "D:\\" }                │  DirectoryService│
│  懒加载目录树    │ ◄────────────────────────────────── │  安全过滤        │
└─────────────────┘     [{ name, path, hasChildren }]   └─────────────────┘
```

## 后端实现

### 1. 数据传输对象 (DTO)

```csharp
// DirectoryService.cs
public class DirectoryInfoDto
{
    public string Name { get; set; }       // 目录名称
    public string Path { get; set; }       // 完整路径
    public bool HasChildren { get; set; }  // 是否有子目录
    public bool IsDrive { get; set; }      // 是否为驱动器根目录
}
```

### 2. API 端点

**SystemController.cs**
```csharp
/// <summary>
/// 获取目录列表（用于备份路径选择）
/// </summary>
/// <param name="path">父目录路径，为空则返回驱动器列表</param>
[HttpGet("directories")]
[Authorize]
public async Task<ApiResponse<List<DirectoryInfoDto>>> GetDirectories([FromQuery] string path = null)
{
    var result = await _directoryService.GetDirectoriesAsync(path);
    return ApiResponse<List<DirectoryInfoDto>>.Ok(result);
}
```

### 3. DirectoryService 服务

**DirectoryService.cs**
```csharp
public class DirectoryService : IDirectoryService
{
    // 敏感目录黑名单
    private static readonly string[] ExcludedDirectories = {
        "Windows", "Program Files", "Program Files (x86)",
        "System Volume Information", "$RECYCLE.BIN",
        "ProgramData", "Users"
    };

    public async Task<List<DirectoryInfoDto>> GetDirectoriesAsync(string path)
    {
        // 1. 如果 path 为空，返回可用驱动器
        // 2. 否则返回指定路径下的子目录
        // 3. 过滤敏感目录
        // 4. 检查访问权限
    }
}
```

### 4. 安全考虑

- **黑名单过滤**：排除 Windows、Program Files 等系统目录
- **权限检查**：只返回 SQL Server 服务账户可访问的目录
- **路径验证**：验证路径格式，防止路径遍历攻击

## 前端实现

### 1. 目录选择器组件

**DirectorySelector.vue**（新组件）
```vue
<template>
  <el-tree-select
    v-model="selectedPath"
    :load="loadDirectories"
    lazy
    :props="{ label: 'name', value: 'path', isLeaf: (data) => !data.hasChildren }"
    placeholder="选择备份目录"
    check-strictly
    @node-click="handleNodeClick"
  />
</template>
```

### 2. BackupManagement.vue 改造

**创建备份区域**
- 将路径输入框改为目录选择器
- 添加"选择"按钮触发目录选择弹窗

**备份计划编辑弹窗**
- 备份路径字段使用目录选择器
- 保留手动输入作为备选

### 3. API 调用

**request.js**
```javascript
export const systemApi = {
  // 获取目录列表
  getDirectories: (path) => request.get('/system/directories', { params: { path } }),
  // ... 其他 API
}
```

## 用户体验流程

1. 用户点击"选择"按钮
2. 弹出目录选择器对话框
3. 默认显示服务器驱动器列表（C:, D: 等）
4. 用户点击展开驱动器，懒加载子目录
5. 选择目标目录后，路径自动填充到输入框
6. 也可直接手动输入或修改路径

## 文件清单

### 后端新增/修改
- `backend/src/DataForgeStudio.Core/Interfaces/IDirectoryService.cs` - 新增接口
- `backend/src/DataForgeStudio.Core/Services/DirectoryService.cs` - 新增服务
- `backend/src/DataForgeStudio.Api/Controllers/SystemController.cs` - 新增端点
- `backend/src/DataForgeStudio.Domain/DTOs/DirectoryInfoDto.cs` - 新增 DTO

### 前端新增/修改
- `frontend/src/components/DirectorySelector.vue` - 新增组件
- `frontend/src/views/system/BackupManagement.vue` - 改造路径选择
- `frontend/src/api/request.js` - 新增 API 调用

## 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 目录权限问题 | 用户选择无权限目录 | 后端检查权限，前端显示提示 |
| 服务器目录过多 | 加载缓慢 | 懒加载 + 分页 |
| 安全漏洞 | 暴露敏感目录 | 黑名单过滤 + 路径验证 |

## 验收标准

1. 可以查看服务器驱动器列表
2. 可以展开驱动器浏览子目录
3. 选择目录后路径正确填充
4. 敏感目录不显示
5. 无权限目录有明确提示
