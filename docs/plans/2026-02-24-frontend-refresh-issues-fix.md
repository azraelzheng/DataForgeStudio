# 前端刷新与权限问题修复计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复4个前端问题：权限组刷新、权限码不匹配、许可证验证时序、数据源刷新

**Architecture:**
1. 问题1和4：使用 `onActivated` 钩子或对话框打开时刷新数据
2. 问题2：修正前端权限码与后端数据库一致
3. 问题3：确保异步许可证加载完成后再进行验证

**Tech Stack:** Vue 3, Pinia, Element Plus, Vue Router

---

## 问题根因分析

### 问题1：新建权限组后，用户管理中看不到

**位置**: `frontend/src/views/system/UserManagement.vue`
**原因**: `handleAdd()` 打开新增用户对话框时未刷新角色列表
**对比**: `handleAssignRoles()` 第412-413行已正确实现刷新

### 问题2：权限勾选保存后自动取消

**位置**: `frontend/src/views/system/RoleManagement.vue` 第182行
**原因**: 前端权限码 `report:view` 与后端 `report:query` 不匹配

**前后端权限码对比:**
| 前端 | 后端 | 状态 |
|------|------|------|
| report:view | report:query | ❌ 不匹配 |
| (无) | report:toggle | ❌ 缺失 |

### 问题3：新用户访问报表设计提示许可证无效

**位置**: `frontend/src/router/index.js` 第151-168行
**原因**: 许可证异步加载与验证存在竞态条件

### 问题4：新增数据源后在报表设计器中看不到

**位置**: `frontend/src/views/report/ReportDesigner.vue` 第497-501行
**原因**: 数据源仅在 `onMounted` 加载，缺少 `onActivated` 刷新

---

## 修复任务

### Task 1: 修复权限码不匹配（问题2 - 高优先级）

**Files:**
- Modify: `frontend/src/views/system/RoleManagement.vue:178-189`

**Step 1: 修正报表管理权限码**

将第178-189行的权限树中报表管理部分修改为与后端一致：

```javascript
  {
    key: 'report',
    label: '报表管理',
    children: [
      { key: 'report:query', label: '访问报表查询' },
      { key: 'report:execute', label: '执行报表查询' },
      { key: 'report:design', label: '访问报表设计' },
      { key: 'report:create', label: '创建报表' },
      { key: 'report:edit', label: '编辑报表' },
      { key: 'report:delete', label: '删除报表' },
      { key: 'report:toggle', label: '停用启用报表' },
      { key: 'report:export', label: '导出报表' }
    ]
  },
```

**Step 2: 验证修复**

```bash
cd frontend && npm run build
```
Expected: 构建成功，无错误

**Step 3: 提交**

```bash
git add frontend/src/views/system/RoleManagement.vue
git commit -m "fix: correct permission codes to match backend

- Change report:view to report:query (access report query page)
- Reorder permissions to match backend order
- Add missing report:toggle permission
- Fixes issue where permission checkboxes auto-uncheck after save"
```

---

### Task 2: 修复用户管理角色刷新（问题1）

**Files:**
- Modify: `frontend/src/views/system/UserManagement.vue:336-339`

**Step 1: 在打开新增用户对话框时刷新角色列表**

修改 `handleAdd` 函数：

```javascript
const handleAdd = async () => {
  // 刷新角色列表以确保显示最新创建的角色
  await loadRoles()
  isEdit.value = false
  dialogVisible.value = true
}
```

**Step 2: 在编辑用户对话框打开时也刷新角色列表**

修改 `handleEdit` 函数（约第341-351行）：

```javascript
const handleEdit = async (row) => {
  if (row.username === 'root') {
    ElMessage.warning('root 用户是系统管理员，不能被修改')
    return
  }
  // 刷新角色列表以确保显示最新创建的角色
  await loadRoles()
  isEdit.value = true
  Object.assign(form, row)
  // 转换角色数据
  form.roleIds = row.roles?.map(r => r.roleId) || []
  dialogVisible.value = true
}
```

**Step 3: 验证修复**

```bash
cd frontend && npm run build
```
Expected: 构建成功，无错误

**Step 4: 提交**

```bash
git add frontend/src/views/system/UserManagement.vue
git commit -m "fix: refresh role list when opening user dialog

- Call loadRoles() in handleAdd() to show newly created roles
- Call loadRoles() in handleEdit() for consistency
- Fixes issue where new roles don't appear in user form without page refresh"
```

---

### Task 3: 修复许可证验证时序问题（问题3）

**Files:**
- Modify: `frontend/src/router/index.js:151-168`

**Step 1: 确保许可证完全加载后再验证**

修改路由守卫中的许可证验证逻辑：

```javascript
  if (LICENSE_REQUIRED_ROUTES.some(route => to.path.startsWith(route))) {
    const licenseStore = useLicenseStore()

    // 确保许可证已加载（等待异步操作完成）
    if (!licenseStore.license) {
      await licenseStore.loadLicense()
    }

    // 再次检查状态，确保异步加载已完成
    await nextTick()

    if (licenseStore.isExpired || licenseStore.licenseStatus === 'expired') {
      ElMessage.error('许可证已过期，无法访问此功能。请续费后继续使用。')
      next('/license')
      return
    }

    // 只有当许可证确实无效时才阻止访问（不是 'unknown' 或正在加载中）
    if (licenseStore.licenseStatus === 'invalid') {
      ElMessage.error('许可证无效，请先激活许可证。')
      next('/license')
      return
    }
  }
```

**Step 2: 添加 nextTick 导入**

确保在文件顶部已导入 `nextTick`：

```javascript
import { nextTick } from 'vue'
```

或使用 Vue 3 的方式（如果已经在组件上下文中）：

```javascript
// 在 router.beforeEach 中使用
import { nextTick } from 'vue'
```

**Step 3: 验证修复**

```bash
cd frontend && npm run build
```
Expected: 构建成功，无错误

**Step 4: 提交**

```bash
git add frontend/src/router/index.js
git commit -m "fix: resolve license validation race condition

- Ensure loadLicense() completes before validation
- Add nextTick to ensure reactive state updates
- Only block access when status is explicitly 'invalid'
- Fixes issue where new users see 'invalid license' error briefly"
```

---

### Task 4: 修复报表设计器数据源刷新（问题4）

**Files:**
- Modify: `frontend/src/views/report/ReportDesigner.vue:245-507`

**Step 1: 添加 onActivated 导入**

在第245行的导入语句中添加 `onActivated`：

```javascript
import { ref, reactive, onMounted, onActivated, computed, h } from 'vue'
```

**Step 2: 添加 onActivated 钩子**

在 `onMounted` 钩子后（约第507行后）添加：

```javascript
onMounted(async () => {
  await loadDataSources()
  const reportId = route.query.id
  if (reportId) {
    loadReport(reportId)
  }
})

// 当组件被激活时（从其他页面返回），重新加载数据源
onActivated(async () => {
  await loadDataSources()
})
```

**Step 3: 提取数据源加载函数**

将 `onMounted` 中的数据源加载逻辑提取为独立函数：

```javascript
// 加载数据源列表
const loadDataSources = async () => {
  const res = await dataSourceApi.getActiveDataSources()
  if (res.success) {
    dataSources.value = res.data || []
  }
}

onMounted(async () => {
  await loadDataSources()
  const reportId = route.query.id
  if (reportId) {
    loadReport(reportId)
  }
})

// 当组件被激活时（从其他页面返回），重新加载数据源
onActivated(async () => {
  await loadDataSources()
})
```

**Step 4: 验证修复**

```bash
cd frontend && npm run build
```
Expected: 构建成功，无错误

**Step 5: 提交**

```bash
git add frontend/src/views/report/ReportDesigner.vue
git commit -m "fix: refresh datasource list when returning to report designer

- Extract loadDataSources() as reusable function
- Add onActivated hook to reload datasources
- Fixes issue where new datasources don't appear without page refresh"
```

---

### Task 5: 构建并验证所有修复

**Step 1: 完整前端构建**

```bash
cd frontend && npm run build
```
Expected: 构建成功

**Step 2: 运行后端测试（如有）**

```bash
cd backend && dotnet test
```
Expected: 所有测试通过

**Step 3: 验证构建产物**

```bash
ls -la frontend/dist/
```
Expected: index.html 和 assets 目录存在

---

### Task 6: 发布到生产版本

**Step 1: 发布后端 API**

```bash
cd backend/src/DataForgeStudio.Api && dotnet publish -c Release -o ../../publish/api
```

**Step 2: 复制前端构建到发布目录**

```bash
rm -rf publish/Server/wwwroot/* 2>/dev/null || true
cp -r frontend/dist/* publish/Server/wwwroot/ 2>/dev/null || mkdir -p publish/Server/wwwroot && cp -r frontend/dist/* publish/Server/wwwroot/
```

**Step 3: 验证发布**

```bash
ls -la publish/api/DataForgeStudio.Api.dll
ls -la publish/Server/wwwroot/index.html
```

**Step 4: 提交发布**

```bash
git add -A
git commit -m "build: publish v1.0.3 with frontend refresh fixes

- Fix permission code mismatch (report:view -> report:query)
- Fix role list refresh in user management
- Fix license validation race condition
- Fix datasource list refresh in report designer"
```

---

### Task 7: 重新生成安装包

**Step 1: 运行安装包构建脚本**

```bash
cd scripts && build-installer.bat
```

Expected: 生成 `dist/DataForgeStudio-Setup.exe`

**Step 2: 验证安装包**

```bash
ls -la dist/DataForgeStudio-Setup.exe
```

**Step 3: 提交最终版本**

```bash
git add -A
git commit -m "build: generate installer v1.0.3 with all frontend fixes"
```

---

## 测试验证清单

### 问题1 验证（权限组刷新）
- [ ] 登录系统
- [ ] 进入权限组管理，创建新权限组
- [ ] 进入用户管理，点击新增用户
- [ ] 验证角色下拉框中显示新创建的权限组

### 问题2 验证（权限保存）
- [ ] 进入权限组管理
- [ ] 选择一个权限组，点击配置权限
- [ ] 勾选"访问报表查询"权限
- [ ] 点击确定保存
- [ ] 再次点击配置权限
- [ ] 验证"访问报表查询"仍为勾选状态

### 问题3 验证（许可证验证）
- [ ] 使用新创建的用户登录
- [ ] 立即点击报表设计
- [ ] 验证不会出现"许可证无效"错误
- [ ] 验证能正常进入报表设计页面

### 问题4 验证（数据源刷新）
- [ ] 进入数据源管理，创建新数据源
- [ ] 进入报表设计，点击新增报表
- [ ] 验证数据源下拉框中显示新创建的数据源

### 问题5 验证（停用数据源 - 已修复）
- [ ] 进入数据源管理，停用一个数据源
- [ ] 进入报表设计，点击新增报表
- [ ] 验证停用的数据源不出现在下拉框中

---

## 相关文档

- `docs/PROJECT_STATUS.md` - 项目状态
- `CLAUDE.md` - 项目指南
