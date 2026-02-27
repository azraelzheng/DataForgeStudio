# V1.0.1 帮助系统与文档整理设计

## 概述

为 DataForgeStudio V1.0.1 添加帮助系统，统一管理版本号显示，并整理分发文档。

## 需求

1. 顶部栏添加"？"帮助按钮，提供下拉菜单访问帮助信息
2. 统一版本号管理，移除硬编码版本号
3. 整理安装目录文档

## 设计方案

### 1. 前端修改

#### 1.1 顶部栏"？"帮助按钮

**位置**: `App.vue` 顶部栏右侧，用户名左边

**下拉菜单选项**:
- 关于 - 显示版本信息对话框
- 帮助文档 - 显示用户操作手册
- 用户协议 - 显示 EULA
- 隐私政策 - 显示隐私政策

#### 1.2 移除硬编码版本号

| 文件 | 修改内容 |
|------|----------|
| `LoginPage.vue` | 移除 "V1.0" 文字 |
| `LicenseManagement.vue` | 从 API 动态获取产品名称 |

### 2. 后端修改

#### 2.1 系统信息 API

```
GET /api/system/info
```

返回:
```json
{
  "productName": "DataForgeStudio",
  "version": "1.0.1",
  "copyright": "Copyright © 2026"
}
```

#### 2.2 文档内容 API

```
GET /api/system/document?type={type}
```

支持的 type: `eula`, `privacy`, `manual`

### 3. 安装目录文档

| 文件 | 说明 |
|------|------|
| `README.txt` | 产品简介 |
| `INSTALL.txt` | 安装说明、运行环境 |
| `EULA.txt` | 用户协议 |
| `PRIVACY.txt` | 隐私政策 |

### 4. DeployManager 修改

移除右下角 "DataForgeStudio V4" 文字

## 实现范围

- 前端: App.vue, LoginPage.vue, LicenseManagement.vue
- 后端: 新增 SystemController
- 文档: 生成 txt 文件
- DeployManager: MainWindow.xaml
