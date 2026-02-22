# 安装配置校验机制设计

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为 DataForgeStudio 安装程序添加配置校验机制，确保用户输入的数据库连接信息和端口配置正确无误。

**Architecture:** Inno Setup 负责实时格式校验和中文界面，Configurator 提供验证子命令进行数据库连接测试和端口占用检测。

**Tech Stack:** Inno Setup Pascal Script, .NET 8.0 (Configurator)

---

## 1. 功能需求

### 1.1 界面简化
- 移除数据库名称输入框（固定为 `DataForgeStudio`）
- 数据库配置页面只保留：服务器地址、端口、认证方式、用户名、密码

### 1.2 实时格式校验
- 端口范围：1-65535
- 必填项非空
- SQL 认证模式下用户名密码必填

### 1.3 安装前验证
- 数据库连接测试（连接 master 数据库）
- 端口占用检测（后端端口、前端端口）
- 区分严重程度：错误阻止安装，警告允许继续

### 1.4 中文界面
- 安装向导使用简体中文

---

## 2. 技术方案

### 2.1 混合方案架构

```
Inno Setup (setup.iss)
├── 实时格式校验 (Pascal)
├── 中文界面 (ChineseSimplified.isl)
└── 安装前调用 Configurator --validate
        │
        ▼
Configurator.exe --validate --config {...}
├── 数据库连接测试
├── 端口占用检测
└── 返回 JSON 结果
        │
        ▼
Inno Setup 处理结果
├── errors 不为空 → 阻止安装
├── warnings 不为空 → 用户确认后继续
└── 全部通过 → 继续安装
```

### 2.2 错误严重程度

| 类型 | 条件 | 处理 |
|------|------|------|
| 错误 | 数据库连接失败 | 阻止安装 |
| 错误 | 认证失败 | 阻止安装 |
| 警告 | 端口被占用 | 确认后继续 |
| 信息 | 数据库已存在 | 仅提示 |

---

## 3. 修改文件清单

| 文件 | 修改内容 |
|------|----------|
| `installer/setup.iss` | 中文界面、移除数据库名输入、添加校验逻辑 |
| `backend/tools/Configurator/Program.cs` | 添加 --validate 模式、数据库连接测试、端口检测 |

---

## 4. 实现任务

### Task 1: 更新 Configurator 添加验证模式
- 添加 `--validate` 命令行参数处理
- 实现数据库连接测试
- 实现端口占用检测
- 返回 JSON 格式结果

### Task 2: 更新 setup.iss 中文界面
- 添加中文语言支持
- 自定义中文消息
- 更新页面标题和描述

### Task 3: 简化数据库配置页面
- 移除数据库名称输入框
- 更新配置传递逻辑（使用固定数据库名）

### Task 4: 添加实时格式校验
- 端口输入校验（数字、范围）
- SQL 认证必填校验
- 错误提示显示

### Task 5: 添加安装前验证
- 构建 JSON 配置
- 调用 Configurator --validate
- 处理验证结果
- 显示错误/警告对话框

### Task 6: 修复其他高优先级问题
- Windows 服务注册错误检查
- 配置器执行结果检查
- 移除硬编码默认密码提示

---

## 5. 验证 JSON 格式

### 输入
```json
{
  "installPath": "C:\\Program Files\\DataForgeStudio",
  "dbServer": "localhost",
  "dbPort": 1433,
  "dbAuth": "windows",
  "dbUser": "",
  "dbPassword": "",
  "backendPort": 5000,
  "frontendPort": 80
}
```

### 输出
```json
{
  "success": false,
  "errors": [
    { "code": "DB_CONNECT_FAILED", "message": "无法连接到数据库服务器 localhost:1433 - 网络连接失败" }
  ],
  "warnings": [
    { "code": "PORT_IN_USE", "message": "端口 80 已被进程 nginx.exe (PID: 1234) 占用" }
  ]
}
```
