# 许可证系统 Dashboard 功能支持设计

**日期**: 2026-03-03
**作者**: Claude
**状态**: 已批准

---

## 1. 概述

为许可证系统添加 Dashboard（大屏）功能的数量限制和功能权限控制。

### 1.1 目标
- 添加 `MaxDashboards` 字段限制大屏创建数量
- 在功能列表中添加三个 Dashboard 相关权限：大屏设计、大屏展示、全屏模式
- 确保许可证生成工具与后端验证逻辑一致

### 1.2 范围
| 包含 | 不包含 |
|------|--------|
| 后端许可证验证 | 前端功能权限 UI 控制（后续迭代） |
| 许可证生成工具更新 | 数据库迁移（新字段可选） |
| 前端许可证信息展示 | |

---

## 2. 数据结构变更

### 2.1 LicenseData.cs
```csharp
// 新增属性
public int MaxDashboards { get; set; }
```

**迁移策略**: 新字段可选，旧许可证文件仍可使用（默认值 0）

### 2.2 LicenseInfoDto.cs
```csharp
// 新增属性
public int MaxDashboards { get; set; }
```

### 2.3 LicenseUsageStatsDto.cs
```csharp
// 新增属性
public int CurrentDashboards { get; set; }
```

---

## 3. 服务层变更

### 3.1 ILicenseService.cs
```csharp
/// <summary>
/// 检查是否可以创建新的大屏
/// </summary>
Task<ApiResponse> CheckDashboardLimitAsync();
```

### 3.2 LicenseService.cs 变更

| 方法 | 变更内容 |
|------|---------|
| `MapToLicenseInfoDto()` | 映射 `MaxDashboards` 字段 |
| 签名验证 JSON | 添加 `maxDashboards` 字段 |
| `GetUsageStatsAsync()` | 添加 `CurrentDashboards` 统计 |
| `GenerateTrialLicenseAsync()` | 试用许可证默认 `MaxDashboards = 3`，Features 添加大屏功能 |
| `CheckDashboardLimitAsync()` | **新增方法** |

### 3.3 DashboardService.cs 变更
在 `CreateDashboardAsync` 开头添加许可证检查：
```csharp
var limitCheck = await _licenseService.CheckDashboardLimitAsync();
if (!limitCheck.Success)
{
    return ApiResponse<DashboardDto>.Fail(
        limitCheck.Message,
        limitCheck.ErrorCode ?? "DASHBOARD_LIMIT_EXCEEDED"
    );
}
```

---

## 4. 许可证生成工具变更

### 4.1 LicenseInfo 类
```csharp
public int MaxDashboards { get; set; }
```

### 4.2 AvailableFeatures 更新
```csharp
private static readonly string[] AvailableFeatures =
{
    "报表设计", "报表查询", "图表展示", "Excel导出", "PDF导出",
    "数据源管理", "用户管理", "角色管理",
    "大屏设计", "大屏展示", "全屏模式"  // 新增
};
```

### 4.3 各许可证类型默认值
| 类型 | MaxDashboards | 默认功能包含大屏 |
|------|---------------|------------------|
| Trial | 3 | 大屏设计、大屏展示、全屏模式 |
| Standard | 10 | 大屏设计、大屏展示、全屏模式 |
| Professional | 50 | 全部功能 |
| Enterprise | 0 (无限制) | 全部功能 |

---

## 5. 前端变更

### 5.1 stores/license.js
添加 `maxDashboards` 状态字段

### 5.2 许可证管理页面
显示 `MaxDashboards` 和当前使用情况

---

## 6. 需要修改的文件清单
| 文件 | 修改类型 | 优先级 |
|------|---------|--------|
| `backend/.../DTO/LicenseData.cs` | 修改 | P0 |
| `backend/.../DTO/LicenseInfoDto.cs` | 修改 | P0 |
| `backend/.../DTO/LicenseUsageStatsDto.cs` | 修改 | P0 |
| `backend/.../Interfaces/ILicenseService.cs` | 修改 | P0 |
| `backend/.../Services/LicenseService.cs` | 修改 | P0 |
| `backend/.../Services/DashboardService.cs` | 修改 | P1 |
| `backend/.../Controllers/DashboardController.cs` | 修改 | P1 |
| `backend/tools/LicenseGenerator/Program.cs` | 修改 | P1 |
| `frontend/src/stores/license.js` | 修改 | P2 |
| `frontend/许可证管理页面` | 修改 | P2 |

---

## 7. 验收标准
1. 许可证生成工具可输入 MaxDashboards 值
2. 后端正确验证大屏数量限制
3. 试用许可证包含大屏功能
4. 前端正确显示 MaxDashboards 信息
5. 达到限制时无法创建新大屏
