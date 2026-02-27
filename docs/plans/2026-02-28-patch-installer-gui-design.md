# PatchInstaller GUI 改进设计

## 日期
2026-02-28

## 目标
简化补丁安装程序界面，实现中文化，自动读取数据库配置。

## 设计方案

### 界面元素
- 标题：显示补丁版本号（中文）
- 安装目录：文本框 + 浏览按钮
- 进度条：安装过程中显示
- 安装日志：只读文本框
- 按钮：安装 / 取消

### 移除元素
- 数据库服务器输入框
- 数据库名称输入框
- 用户名输入框
- 密码输入框
- 数据库设置分组框

### 行为流程
1. 程序启动时自动检测默认安装路径
2. 选择安装目录后，后台读取 `Server\appsettings.json`
3. 解析连接字符串获取数据库配置
4. 用户点击"安装"，使用自动读取的配置执行
5. 安装过程实时显示日志

### 数据库配置解析
从 `appsettings.json` 的 `ConnectionStrings:DefaultConnection` 读取：
- `Data Source` → 服务器
- `Initial Catalog` → 数据库名（默认 DataForgeStudio）
- `User ID` / `Password` → SQL 认证（如有）
- `Integrated Security=True` → Windows 认证

## 实现要点
1. 修改 `PatchInstallerForm` 类的 `InitializeComponents()` 方法
2. 移除数据库相关控件
3. 所有 UI 文本改为中文
4. 添加 `LoadDatabaseConfig()` 方法，在选择目录时自动调用
5. 修改 `LoadDefaultValues()` 使用 "DataForgeStudio" 作为默认数据库名
