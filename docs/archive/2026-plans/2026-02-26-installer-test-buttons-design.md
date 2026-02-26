# 安装程序"测试"按钮功能设计

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在安装程序的数据库配置和端口配置页面添加"测试"按钮，验证通过才能进行下一步

**Architecture:** 在 Inno Setup Pascal 代码中直接实现测试逻辑，不依赖外部工具

**Tech Stack:** Inno Setup 6, Pascal Script, Windows API (ADO for database, netstat for ports)

---

## 1. 数据库配置页面

### UI 布局
```
服务器地址:   [localhost        ]
端口:         [1433             ]
认证方式:     ○ Windows身份验证  ○ SQL Server身份验证
用户名:       [sa               ] (SQL认证时启用)
密码:         [********         ] (SQL认证时启用)

[测试连接]  ← 新增按钮

错误/状态提示: (红色/绿色文字)
```

### 测试逻辑

1. 点击"测试连接"按钮
2. 验证输入完整性（服务器地址、端口、SQL认证时的用户名密码）
3. 构建 ADO 连接字符串连接到 `master` 数据库
4. 尝试打开连接
5. 结果显示：
   - 成功: 绿色 "✓ 数据库连接成功"
   - 失败: 红色错误信息

### 连接字符串格式

**Windows 认证:**
```
Provider=SQLOLEDB;Data Source=localhost,1433;Initial Catalog=master;Integrated Security=SSPI;Connect Timeout=10
```

**SQL Server 认证:**
```
Provider=SQLOLEDB;Data Source=localhost,1433;Initial Catalog=master;User ID=sa;Password=xxx;Connect Timeout=10
```

---

## 2. 端口配置页面

### UI 布局
```
后端 API 端口:  [5000    ]  (API 服务监听端口)
前端 Web 端口:  [80      ]  (Web 访问端口)

[检测端口]  ← 新增按钮

错误/状态提示: (红色/绿色文字)
```

### 测试逻辑

1. 点击"检测端口"按钮
2. 验证端口范围（1-65535）
3. 调用 `netstat -ano` 获取已占用端口列表
4. 解析输出，检查两个端口是否在监听列表中
5. 结果显示：
   - 全部可用: 绿色 "✓ 端口 5000 可用，端口 80 可用"
   - 部分占用: 红色 "✗ 端口 80 已被占用 (PID: 1234)"

---

## 3. 技术实现

### 数据库连接测试

使用 ADO COM 对象进行连接测试：

```pascal
function TestDbConnection: Boolean;
var
  Conn: Variant;
  ConnStr: String;
begin
  Result := False;
  try
    Conn := CreateOleObject('ADODB.Connection');
    Conn.ConnectionTimeout := 10;
    // 构建连接字符串...
    Conn.Open(ConnStr);
    Conn.Close;
    Result := True;
  except
    // 捕获错误信息
  end;
end;
```

### 端口占用检测

调用 netstat 命令并解析输出：

```pascal
function IsPortInUse(Port: Integer; out PID: String): Boolean;
var
  ResultCode: Integer;
  Output: String;
begin
  // 执行 netstat -ano | findstr :端口号
  Exec('cmd', '/c netstat -ano | findstr :' + IntToStr(Port), ..., ResultCode);
  // 解析输出判断是否被占用
end;
```

---

## 4. 用户体验

- 测试按钮始终可用
- 测试期间按钮禁用，防止重复点击
- 测试结果用颜色区分（绿色成功，红色失败）
- "下一步"按钮不强制要求测试通过（仅做提示）

---

## 5. 文件变更

| 文件 | 操作 |
|------|------|
| `installer/setup.iss` | 修改 - 添加测试按钮和逻辑 |
