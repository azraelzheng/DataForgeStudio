# 安装程序"测试"按钮功能实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在安装程序的数据库配置和端口配置页面添加"测试"按钮

**Architecture:** 在 Inno Setup Pascal 代码中使用 ADO 测试数据库连接，使用 netstat 检测端口占用

**Tech Stack:** Inno Setup 6, Pascal Script, ADO (ADODB.Connection), Windows CMD (netstat)

---

## Task 1: 添加数据库测试按钮 UI

**Files:**
- Modify: `installer/setup.iss` (InitializeWizard 函数，约第 375 行)

**Step 1: 添加测试按钮变量和状态变量**

在 var 区域添加新变量（约第 133 行后）：

```pascal
var
  // ... 现有变量 ...
  DbTestButton: TButton;
  DbTestStatusLabel: TLabel;
  DbTestPassed: Boolean;
```

**Step 2: 在数据库配置页面添加测试按钮**

在 `InitializeWizard` 函数中，ErrorLabel 创建之后（约第 385 行），添加：

```pascal
  // 测试连接按钮
  DbTestButton := TButton.Create(WizardForm);
  with DbTestButton do
  begin
    Parent := DbConfigPage.Surface;
    Left := 100;
    Top := 180;
    Width := 80;
    Height := 25;
    Caption := '测试连接';
    OnClick := @DbTestButtonClick;
  end;

  // 测试状态标签
  DbTestStatusLabel := TLabel.Create(WizardForm);
  with DbTestStatusLabel do
  begin
    Parent := DbConfigPage.Surface;
    Left := 190;
    Top := 185;
    Width := 200;
    Caption := '';
  end;
```

**Step 3: 提交**

```bash
git add installer/setup.iss
git commit -m "feat(installer): add database test button UI"
```

---

## Task 2: 实现数据库连接测试逻辑

**Files:**
- Modify: `installer/setup.iss`

**Step 1: 添加 ADO 常量和变量**

在 [Code] 区域开头添加：

```pascal
// ADO 常量
const
  adStateClosed = 0;

var
  // ... 现有变量 ...
  PortTestButton: TButton;
  PortTestStatusLabel: TLabel;
  PortTestPassed: Boolean;
```

**Step 2: 实现数据库测试函数**

在 `InitializeWizard` 之前添加：

```pascal
// 测试数据库连接
function TestDbConnection(Server: String; Port: Integer; UseWindowsAuth: Boolean; Username, Password: String; out ErrorMsg: String): Boolean;
var
  Conn: Variant;
  ConnStr: String;
begin
  Result := False;
  ErrorMsg := '';

  try
    Conn := CreateOleObject('ADODB.Connection');
    Conn.ConnectionTimeout := 10;
    Conn.CommandTimeout := 10;

    // 构建连接字符串
    if UseWindowsAuth then
    begin
      ConnStr := 'Provider=SQLOLEDB;Data Source=' + Server + ',' + IntToStr(Port) +
                 ';Initial Catalog=master;Integrated Security=SSPI;Connect Timeout=10';
    end
    else
    begin
      ConnStr := 'Provider=SQLOLEDB;Data Source=' + Server + ',' + IntToStr(Port) +
                 ';Initial Catalog=master;User ID=' + Username + ';Password=' + Password +
                 ';Connect Timeout=10';
    end;

    Conn.Open(ConnStr);
    Conn.Close;
    Result := True;
  except
    on E: Exception do
    begin
      ErrorMsg := E.Message;
    end;
  end;
end;
```

**Step 3: 实现按钮点击事件**

```pascal
// 数据库测试按钮点击事件
procedure DbTestButtonClick(Sender: TObject);
var
  Server: String;
  Port: Integer;
  UseWindowsAuth: Boolean;
  Username, Password, ErrorMsg: String;
  Success: Boolean;
begin
  // 禁用按钮，防止重复点击
  DbTestButton.Enabled := False;
  DbTestButton.Caption := '测试中...';
  DbTestStatusLabel.Caption := '正在连接数据库...';
  DbTestStatusLabel.Font.Color := clGray;
  DbTestPassed := False;

  // 获取输入值
  Server := Trim(DbServerEdit.Text);
  Port := StrToIntDef(DbPortEdit.Text, 1433);
  UseWindowsAuth := DbAuthRadioWindows.Checked;
  Username := Trim(DbUserEdit.Text);
  Password := DbPasswordEdit.Text;

  // 执行测试
  Success := TestDbConnection(Server, Port, UseWindowsAuth, Username, Password, ErrorMsg);

  // 显示结果
  if Success then
  begin
    DbTestStatusLabel.Caption := '✓ 数据库连接成功';
    DbTestStatusLabel.Font.Color := clGreen;
    DbTestPassed := True;
  end
  else
  begin
    DbTestStatusLabel.Caption := '✗ 连接失败: ' + ErrorMsg;
    DbTestStatusLabel.Font.Color := clRed;
    DbTestPassed := False;
  end;

  // 恢复按钮
  DbTestButton.Enabled := True;
  DbTestButton.Caption := '测试连接';
end;
```

**Step 4: 提交**

```bash
git add installer/setup.iss
git commit -m "feat(installer): implement database connection test logic"
```

---

## Task 3: 添加端口测试按钮 UI

**Files:**
- Modify: `installer/setup.iss` (InitializeWizard 函数，端口配置页面部分)

**Step 1: 在端口配置页面添加测试按钮**

在端口配置页面的 ErrorLabel 创建之后（约第 456 行），添加：

```pascal
  // 检测端口按钮
  PortTestButton := TButton.Create(WizardForm);
  with PortTestButton do
  begin
    Parent := PortConfigPage.Surface;
    Left := 120;
    Top := 110;
    Width := 80;
    Height := 25;
    Caption := '检测端口';
    OnClick := @PortTestButtonClick;
  end;

  // 检测状态标签
  PortTestStatusLabel := TLabel.Create(WizardForm);
  with PortTestStatusLabel do
  begin
    Parent := PortConfigPage.Surface;
    Left := 210;
    Top := 115;
    Width := 250;
    Caption := '';
  end;
```

**Step 2: 提交**

```bash
git add installer/setup.iss
git commit -m "feat(installer): add port test button UI"
```

---

## Task 4: 实现端口占用检测逻辑

**Files:**
- Modify: `installer/setup.iss`

**Step 1: 添加文件执行函数声明**

在 [Code] 区域添加 Windows API 声明：

```pascal
// 用于执行命令行并获取输出
function ExecAndGetOutput(const Cmd, Params: String): String;
var
  TempFile: String;
  ResultCode: Integer;
  Lines: TStringList;
begin
  Result := '';
  TempFile := ExpandConstant('{tmp}\portcheck.txt');

  // 删除旧文件
  DeleteFile(TempFile);

  // 执行命令，输出到临时文件
  Exec(Cmd, Params + ' > "' + TempFile + '" 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  // 读取输出
  if FileExists(TempFile) then
  begin
    Lines := TStringList.Create;
    try
      Lines.LoadFromFile(TempFile);
      Result := Lines.Text;
    finally
      Lines.Free;
    end;
  end;
end;

// 检查端口是否被占用
function IsPortInUse(Port: Integer; out PID: String): Boolean;
var
  Output, SearchStr: String;
  Lines: TStringList;
  I: Integer;
  Line: String;
begin
  Result := False;
  PID := '';

  // 执行 netstat -ano 查找端口
  Output := ExecAndGetOutput('cmd', '/c netstat -ano | findstr ":' + IntToStr(Port) + '"');

  if Output = '' then
    Exit;

  Lines := TStringList.Create;
  try
    Lines.Text := Output;
    for I := 0 to Lines.Count - 1 do
    begin
      Line := Trim(Lines[I]);
      // 检查是否是 LISTENING 状态
      if (Pos(':' + IntToStr(Port), Line) > 0) and (Pos('LISTENING', Line) > 0) then
      begin
        Result := True;
        // 提取 PID (最后一列)
        PID := Trim(Copy(Line, Length(Line) - 10, 11));
        while (Length(PID) > 0) and (PID[1] = ' ') do
          Delete(PID, 1, 1);
        Break;
      end;
    end;
  finally
    Lines.Free;
  end;
end;
```

**Step 2: 实现端口测试按钮点击事件**

```pascal
// 端口测试按钮点击事件
procedure PortTestButtonClick(Sender: TObject);
var
  BackendPort, FrontendPort: Integer;
  BackendInUse, FrontendInUse: Boolean;
  BackendPID, FrontendPID: String;
  Msg: String;
begin
  // 禁用按钮
  PortTestButton.Enabled := False;
  PortTestButton.Caption := '检测中...';
  PortTestStatusLabel.Caption := '正在检测端口...';
  PortTestStatusLabel.Font.Color := clGray;
  PortTestPassed := False;

  // 获取端口值
  BackendPort := StrToIntDef(BackendPortEdit.Text, 0);
  FrontendPort := StrToIntDef(FrontendPortEdit.Text, 0);

  // 检测端口
  BackendInUse := IsPortInUse(BackendPort, BackendPID);
  FrontendInUse := IsPortInUse(FrontendPort, FrontendPID);

  // 构建结果消息
  Msg := '';
  if not BackendInUse and not FrontendInUse then
  begin
    Msg := '✓ 端口 ' + IntToStr(BackendPort) + ' 可用，端口 ' + IntToStr(FrontendPort) + ' 可用';
    PortTestStatusLabel.Font.Color := clGreen;
    PortTestPassed := True;
  end
  else
  begin
    if BackendInUse then
      Msg := '✗ 端口 ' + IntToStr(BackendPort) + ' 已被占用 (PID: ' + BackendPID + ')'
    else
      Msg := '✓ 端口 ' + IntToStr(BackendPort) + ' 可用';

    Msg := Msg + '，';

    if FrontendInUse then
      Msg := Msg + '✗ 端口 ' + IntToStr(FrontendPort) + ' 已被占用 (PID: ' + FrontendPID + ')'
    else
      Msg := Msg + '✓ 端口 ' + IntToStr(FrontendPort) + ' 可用';

    PortTestStatusLabel.Font.Color := clRed;
    PortTestPassed := False;
  end;

  PortTestStatusLabel.Caption := Msg;

  // 恢复按钮
  PortTestButton.Enabled := True;
  PortTestButton.Caption := '检测端口';
end;
```

**Step 3: 提交**

```bash
git add installer/setup.iss
git commit -m "feat(installer): implement port detection test logic"
```

---

## Task 5: 构建并测试安装包

**Step 1: 构建安装包**

```bash
cd H:/DataForge/scripts
./build-installer.ps1
```

**Step 2: 手动测试**

1. 运行 `H:\DataForge\dist\DataForgeStudio-Setup.exe`
2. 在数据库配置页面点击"测试连接"，验证：
   - 正确连接时显示绿色成功
   - 错误信息时显示红色错误
3. 在端口配置页面点击"检测端口"，验证：
   - 未占用端口显示绿色可用
   - 已占用端口显示红色占用信息

**Step 3: 最终提交**

```bash
git add -A
git commit -m "feat(installer): complete test buttons feature"
git push origin master
```

---

## 文件变更摘要

| 文件 | 操作 | 任务 |
|------|------|------|
| `installer/setup.iss` | 修改 | Task 1-4 |

## 依赖项

- Windows ADO (ADODB.Connection) - 系统内置
- netstat 命令 - Windows 系统内置
