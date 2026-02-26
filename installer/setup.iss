; DataForgeStudio V1.0 安装脚本 for Inno Setup
#define AppName "DataForgeStudio"
#define AppVersion "1.0.0"
#define AppPublisher "DataForgeStudio"

#ifndef ProjectRoot
  #define ProjectRoot ".."
#endif
#ifndef BuildDir
  #define BuildDir "..\build\installer"
#endif
#ifndef DistDir
  #define DistDir "..\dist"
#endif

[Setup]
AppId={{8F3B9D7A-1234-4567-89AB-CDEF01234567}}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName=C:\Program Files\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir={#DistDir}
OutputBaseFilename=DataForgeStudio-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\Manager\DeployManager.exe
UninstallDisplayName={#AppName}
SetupLogging=yes
SetupIconFile={#ProjectRoot}\resources\icons\app.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
english.BeveledLabel=DataForgeStudio
english.WelcomeLabel1=DataForgeStudio 安装向导
english.WelcomeLabel2=本向导将引导您完成 DataForgeStudio 的安装过程。%n%n建议您在继续之前关闭所有其他应用程序。
english.SelectDirDesc=请选择安装位置
english.SelectDirLabel3=程序将安装到以下文件夹：
english.ReadyLabel1=安装程序已准备好将 DataForgeStudio 安装到您的计算机。
english.InstallingLabel=正在安装 DataForgeStudio，请稍候...
english.FinishedHeadingLabel=DataForgeStudio 安装完成
english.FinishedLabel=安装程序已在您的计算机上安装了 DataForgeStudio。%n%n点击 [完成(F)] 退出安装程序。
english.ExitSetupTitle=退出安装
english.ExitSetupMessage=安装程序尚未完成安装。如果您现在退出，程序将无法安装。%n%n您要退出安装吗？
english.ConfirmUninstall=您确定要完全删除 %1 及其所有组件吗？

[CustomMessages]
english.DbConfigTitle=数据库配置
english.DbConfigDesc=配置 SQL Server 数据库连接信息
english.DbServerLabel=服务器地址:
english.DbPortLabel=端口:
english.DbAuthLabel=认证方式:
english.DbWindowsAuth=Windows 身份验证
english.DbSqlAuth=SQL Server 身份验证
english.DbUserLabel=用户名:
english.DbPasswordLabel=密码:
english.PortConfigTitle=端口配置
english.PortConfigDesc=配置服务端口
english.BackendPortLabel=后端 API 端口:
english.FrontendPortLabel=前端 Web 端口:
english.BackendPortHint=(API 服务监听端口)
english.FrontendPortHint=(Web 访问端口)
english.ErrorPortRange=端口必须在 1-65535 之间
english.ErrorPortNumber=端口必须是有效的数字
english.ErrorSqlAuthRequired=SQL Server 身份验证需要填写用户名和密码
english.ErrorServerEmpty=服务器地址不能为空
english.ValidatingConfig=正在验证配置...
english.ValidationFailed=配置验证失败
english.ValidationWarning=配置验证警告
english.ContinueAnyway=是否仍要继续安装？

[Files]
Source: "{#BuildDir}\configurator\*"; DestDir: "{tmp}\configurator"; Flags: deleteafterinstall ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\Server\*"; DestDir: "{app}\Server"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\WebSite\*"; DestDir: "{app}\WebSite"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\WebServer\*"; DestDir: "{app}\WebServer"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\manager\*"; DestDir: "{app}\Manager"; Flags: ignoreversion recursesubdirs createallsubdirs
; nssm.exe 已经包含在 manager 目录中（由 build-installer.ps1 复制）
; tools\scripts 目录已移除（不再需要）

[Dirs]
; keys folder is no longer needed at root level - keys are stored in Server\keys
Name: "{app}\logs"; Permissions: users-modify
Name: "{app}\DBServer"; Permissions: users-modify
Name: "{app}\WebServer\logs"; Permissions: users-modify
Name: "{app}\WebServer\temp"; Permissions: users-modify

[Registry]
Root: HKLM; Subkey: "Software\{#AppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKLM; Subkey: "Software\{#AppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#AppVersion}"

[Run]
; 安装完成后运行配置器
Filename: "{tmp}\configurator\Configurator.exe"; Parameters: "install --install-path ""{app}"" --db-server ""{code:GetDbServer}"" --db-port {code:GetDbPort} --db-auth ""{code:GetDbAuth}"" --db-user ""{code:GetDbUser}"" --db-password ""{code:GetDbPassword}"" --backend-port {code:GetBackendPort} --frontend-port {code:GetFrontendPort}"; Flags: waituntilterminated; StatusMsg: "正在配置系统..."
; 安装完成后自动启动 API 服务
Filename: "{app}\Manager\nssm.exe"; Parameters: "start DFAppService"; Flags: runhidden waituntilterminated; StatusMsg: "正在启动 DataForgeStudio API 服务..."
; 安装完成后自动启动 Web 服务
Filename: "{app}\Manager\nssm.exe"; Parameters: "start DFWebService"; Flags: runhidden waituntilterminated; StatusMsg: "正在启动 DataForgeStudio Web 服务..."
; 可选：打开管理界面
Filename: "http://localhost:{code:GetFrontendPort}"; Flags: shellexec postinstall skipifsilent nowait; Description: "打开 DataForgeStudio 管理界面"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\WebServer\logs"
Type: filesandordirs; Name: "{app}\WebServer\temp"
Type: filesandordirs; Name: "{app}\keys"
Type: filesandordirs; Name: "{app}\Server\keys"
Type: filesandordirs; Name: "{app}\DBServer"

[Code]

// 导入 Windows API 函数
function GetTickCount: Cardinal;
  external 'GetTickCount@kernel32.dll stdcall';

var
  DbServerEdit: TEdit;
  DbPortEdit: TEdit;
  DbAuthRadioWindows: TRadioButton;
  DbAuthRadioSql: TRadioButton;
  DbUserEdit: TEdit;
  DbPasswordEdit: TPasswordEdit;
  BackendPortEdit: TEdit;
  FrontendPortEdit: TEdit;
  DbConfigPage: TWizardPage;
  PortConfigPage: TWizardPage;
  ErrorLabel: TLabel;
  DbTestButton: TButton;
  DbTestStatusLabel: TLabel;
  DbTestPassed: Boolean;
  PortTestButton: TButton;
  PortTestStatusLabel: TLabel;
  PortTestPassed: Boolean;

// 更新用户名密码输入框的启用状态
procedure UpdateAuthFields;
begin
  DbUserEdit.Enabled := DbAuthRadioSql.Checked;
  DbPasswordEdit.Enabled := DbAuthRadioSql.Checked;
  if DbAuthRadioSql.Checked then
  begin
    DbUserEdit.Color := clWindow;
    DbPasswordEdit.Color := clWindow;
  end
  else
  begin
    DbUserEdit.Color := clBtnFace;
    DbPasswordEdit.Color := clBtnFace;
  end;
end;

// 验证端口输入（只允许数字）
procedure PortEditKeyPress(Sender: TObject; var Key: Char);
begin
  if not ((Key >= '0') and (Key <= '9') or (Key = #8)) then
    Key := #0;
end;

// Windows 认证单选按钮点击事件
procedure DbAuthRadioWindowsClick(Sender: TObject);
begin
  UpdateAuthFields;
  ErrorLabel.Caption := '';
end;

// SQL Server 认证单选按钮点击事件
procedure DbAuthRadioSqlClick(Sender: TObject);
begin
  UpdateAuthFields;
  ErrorLabel.Caption := '';
end;

// 输入框变化时清除错误
procedure EditChange(Sender: TObject);
begin
  ErrorLabel.Caption := '';
end;

// 检查字符串是否为有效数字
function IsNumeric(const S: String): Boolean;
var
  I: Integer;
begin
  Result := False;
  if Length(S) = 0 then Exit;
  for I := 1 to Length(S) do
  begin
    if (S[I] < '0') or (S[I] > '9') then Exit;
  end;
  Result := True;
end;

// 验证端口范围
function ValidatePort(const S: String): Boolean;
var
  Port: Integer;
begin
  Result := False;
  if not IsNumeric(S) then Exit;
  Port := StrToIntDef(S, 0);
  Result := (Port >= 1) and (Port <= 65535);
end;

// 测试数据库连接
function TestDbConnection(const Server, Port: String; UseWindowsAuth: Boolean;
  const Username, Password: String; out ErrorMsg: String): Boolean;
var
  Connection: Variant;
  ConnectionString: String;
  DataSource: String;
begin
  Result := False;
  ErrorMsg := '';

  // 构建数据源字符串
  DataSource := Server;
  if Port <> '' then
    DataSource := DataSource + ',' + Port;

  try
    // 创建 ADO 连接对象
    Connection := CreateOleObject('ADODB.Connection');
    Connection.ConnectionTimeout := 10;

    // 构建连接字符串
    if UseWindowsAuth then
    begin
      ConnectionString := 'Provider=SQLOLEDB;Data Source=' + DataSource +
        ';Initial Catalog=master;Integrated Security=SSPI;Connect Timeout=10';
    end
    else
    begin
      ConnectionString := 'Provider=SQLOLEDB;Data Source=' + DataSource +
        ';Initial Catalog=master;User ID=' + Username + ';Password=' + Password +
        ';Connect Timeout=10';
    end;

    // 尝试打开连接
    Connection.Open(ConnectionString);
    Connection.Close;
    Result := True;
  except
    on E: Exception do
    begin
      ErrorMsg := E.Message;
    end;
  end;
end;

// 数据库测试按钮点击事件
procedure DbTestButtonClick(Sender: TObject);
var
  Server, Port, Username, Password: String;
  UseWindowsAuth: Boolean;
  ErrorMsg: String;
  Success: Boolean;
begin
  // 禁用按钮，显示测试中状态
  DbTestButton.Enabled := False;
  DbTestButton.Caption := '测试中...';
  DbTestStatusLabel.Caption := '正在连接数据库...';
  DbTestStatusLabel.Font.Color := clGray;
  DbTestPassed := False;

  // 获取输入值
  Server := Trim(DbServerEdit.Text);
  Port := Trim(DbPortEdit.Text);
  UseWindowsAuth := DbAuthRadioWindows.Checked;
  Username := Trim(DbUserEdit.Text);
  Password := DbPasswordEdit.Text;

  // 执行连接测试
  Success := TestDbConnection(Server, Port, UseWindowsAuth, Username, Password, ErrorMsg);

  // 显示结果
  if Success then
  begin
    DbTestStatusLabel.Caption := #10003 + ' 数据库连接成功';
    DbTestStatusLabel.Font.Color := clGreen;
    DbTestPassed := True;
  end
  else
  begin
    DbTestStatusLabel.Caption := #10007 + ' 连接失败: ' + ErrorMsg;
    DbTestStatusLabel.Font.Color := clRed;
    DbTestPassed := False;
  end;

  // 恢复按钮状态
  DbTestButton.Enabled := True;
  DbTestButton.Caption := '测试连接';
end;

// 验证数据库配置页面输入
function ValidateDbConfigPage: Boolean;
begin
  Result := False;
  ErrorLabel.Caption := '';

  // 检查服务器地址
  if Trim(DbServerEdit.Text) = '' then
  begin
    ErrorLabel.Caption := '服务器地址不能为空';
    Exit;
  end;

  // 检查端口
  if not ValidatePort(DbPortEdit.Text) then
  begin
    ErrorLabel.Caption := '端口必须在 1-65535 之间';
    Exit;
  end;

  // SQL 认证时检查用户名密码
  if DbAuthRadioSql.Checked then
  begin
    if (Trim(DbUserEdit.Text) = '') or (DbPasswordEdit.Text = '') then
    begin
      ErrorLabel.Caption := 'SQL Server 身份验证需要填写用户名和密码';
      Exit;
    end;
  end;

  Result := True;
end;

// 验证端口配置页面输入
function ValidatePortConfigPage: Boolean;
begin
  Result := False;
  ErrorLabel.Caption := '';

  // 检查后端端口
  if not ValidatePort(BackendPortEdit.Text) then
  begin
    ErrorLabel.Caption := '端口必须在 1-65535 之间';
    Exit;
  end;

  // 检查前端端口
  if not ValidatePort(FrontendPortEdit.Text) then
  begin
    ErrorLabel.Caption := '端口必须在 1-65535 之间';
    Exit;
  end;

  Result := True;
end;

procedure InitializeWizard;
begin
  // 创建数据库配置页面
  DbConfigPage := CreateCustomPage(wpSelectDir, '数据库配置', '配置 SQL Server 数据库连接信息');

  // 服务器
  with TLabel.Create(WizardForm) do
  begin
    Parent := DbConfigPage.Surface;
    Caption := '服务器地址:';
    Left := 0;
    Top := 10;
  end;
  DbServerEdit := TEdit.Create(WizardForm);
  with DbServerEdit do
  begin
    Parent := DbConfigPage.Surface;
    Left := 100;
    Top := 8;
    Width := 200;
    Text := 'localhost';
    OnChange := @EditChange;
  end;

  // 端口
  with TLabel.Create(WizardForm) do
  begin
    Parent := DbConfigPage.Surface;
    Caption := '端口:';
    Left := 0;
    Top := 35;
  end;
  DbPortEdit := TEdit.Create(WizardForm);
  with DbPortEdit do
  begin
    Parent := DbConfigPage.Surface;
    Left := 100;
    Top := 33;
    Width := 80;
    Text := '1433';
    OnKeyPress := @PortEditKeyPress;
    OnChange := @EditChange;
  end;

  // 认证方式
  with TLabel.Create(WizardForm) do
  begin
    Parent := DbConfigPage.Surface;
    Caption := '认证方式:';
    Left := 0;
    Top := 65;
  end;
  DbAuthRadioWindows := TRadioButton.Create(WizardForm);
  with DbAuthRadioWindows do
  begin
    Parent := DbConfigPage.Surface;
    Left := 100;
    Top := 63;
    Width := 150;
    Caption := 'Windows 身份验证';
    Checked := True;
    OnClick := @DbAuthRadioWindowsClick;
  end;
  DbAuthRadioSql := TRadioButton.Create(WizardForm);
  with DbAuthRadioSql do
  begin
    Parent := DbConfigPage.Surface;
    Left := 250;
    Top := 63;
    Width := 150;
    Caption := 'SQL Server 身份验证';
    OnClick := @DbAuthRadioSqlClick;
  end;

  // 用户名
  with TLabel.Create(WizardForm) do
  begin
    Parent := DbConfigPage.Surface;
    Caption := '用户名:';
    Left := 0;
    Top := 95;
  end;
  DbUserEdit := TEdit.Create(WizardForm);
  with DbUserEdit do
  begin
    Parent := DbConfigPage.Surface;
    Left := 100;
    Top := 93;
    Width := 200;
    Text := 'sa';
    Enabled := False;
    Color := clBtnFace;
    OnChange := @EditChange;
  end;

  // 密码
  with TLabel.Create(WizardForm) do
  begin
    Parent := DbConfigPage.Surface;
    Caption := '密码:';
    Left := 0;
    Top := 120;
  end;
  DbPasswordEdit := TPasswordEdit.Create(WizardForm);
  with DbPasswordEdit do
  begin
    Parent := DbConfigPage.Surface;
    Left := 100;
    Top := 118;
    Width := 200;
    Enabled := False;
  end;

  // 错误提示标签
  ErrorLabel := TLabel.Create(WizardForm);
  with ErrorLabel do
  begin
    Parent := DbConfigPage.Surface;
    Left := 0;
    Top := 155;
    Width := 350;
    Font.Color := clRed;
    Caption := '';
  end;

  // 数据库测试按钮
  DbTestButton := TButton.Create(WizardForm);
  with DbTestButton do
  begin
    Parent := DbConfigPage.Surface;
    Caption := '测试连接';
    Left := 100;
    Top := 180;
    Width := 80;
    Height := 25;
    OnClick := @DbTestButtonClick;
  end;

  // 数据库测试状态标签
  DbTestStatusLabel := TLabel.Create(WizardForm);
  with DbTestStatusLabel do
  begin
    Parent := DbConfigPage.Surface;
    Left := 190;
    Top := 185;
    Width := 200;
    Caption := '';
  end;

  // 创建端口配置页面
  PortConfigPage := CreateCustomPage(DbConfigPage.ID, '端口配置', '配置服务端口');

  // 后端端口
  with TLabel.Create(WizardForm) do
  begin
    Parent := PortConfigPage.Surface;
    Caption := '后端 API 端口:';
    Left := 0;
    Top := 20;
  end;
  BackendPortEdit := TEdit.Create(WizardForm);
  with BackendPortEdit do
  begin
    Parent := PortConfigPage.Surface;
    Left := 120;
    Top := 18;
    Width := 80;
    Text := '5000';
    OnKeyPress := @PortEditKeyPress;
    OnChange := @EditChange;
  end;
  with TLabel.Create(WizardForm) do
  begin
    Parent := PortConfigPage.Surface;
    Caption := '(API 服务监听端口)';
    Left := 210;
    Top := 22;
    Font.Color := clGray;
  end;

  // 前端端口
  with TLabel.Create(WizardForm) do
  begin
    Parent := PortConfigPage.Surface;
    Caption := '前端 Web 端口:';
    Left := 0;
    Top := 50;
  end;
  FrontendPortEdit := TEdit.Create(WizardForm);
  with FrontendPortEdit do
  begin
    Parent := PortConfigPage.Surface;
    Left := 120;
    Top := 48;
    Width := 80;
    Text := '80';
    OnKeyPress := @PortEditKeyPress;
    OnChange := @EditChange;
  end;
  with TLabel.Create(WizardForm) do
  begin
    Parent := PortConfigPage.Surface;
    Caption := '(Web 访问端口)';
    Left := 210;
    Top := 52;
    Font.Color := clGray;
  end;

  // 错误提示标签（端口页）
  ErrorLabel := TLabel.Create(WizardForm);
  with ErrorLabel do
  begin
    Parent := PortConfigPage.Surface;
    Left := 0;
    Top := 85;
    Width := 350;
    Font.Color := clRed;
    Caption := '';
  end;

  // 端口测试按钮
  PortTestButton := TButton.Create(WizardForm);
  with PortTestButton do
  begin
    Parent := PortConfigPage.Surface;
    Caption := '检测端口';
    Left := 120;
    Top := 110;
    Width := 80;
    Height := 25;
    OnClick := @PortTestButtonClick;
  end;

  // 端口测试状态标签
  PortTestStatusLabel := TLabel.Create(WizardForm);
  with PortTestStatusLabel do
  begin
    Parent := PortConfigPage.Surface;
    Left := 210;
    Top := 115;
    Width := 250;
    Caption := '';
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  // 进入数据库配置页面时更新认证字段状态
  if CurPageID = DbConfigPage.ID then
  begin
    UpdateAuthFields;
    ErrorLabel.Caption := '';
  end
  else if CurPageID = PortConfigPage.ID then
  begin
    ErrorLabel.Caption := '';
  end;
end;

// 验证当前页面
function ValidateCurrentPage(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID = DbConfigPage.ID then
    Result := ValidateDbConfigPage
  else if CurPageID = PortConfigPage.ID then
    Result := ValidatePortConfigPage;
end;

// 点击"下一步"时验证
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := ValidateCurrentPage(CurPageID);
end;

function GetDbServer(Param: String): String;
begin
  Result := DbServerEdit.Text;
end;

function GetDbPort(Param: String): String;
begin
  Result := DbPortEdit.Text;
end;

function GetDbAuth(Param: String): String;
begin
  if DbAuthRadioWindows.Checked then
    Result := 'windows'
  else
    Result := 'sql';
end;

function GetDbUser(Param: String): String;
begin
  Result := DbUserEdit.Text;
end;

function GetDbPassword(Param: String): String;
begin
  Result := DbPasswordEdit.Text;
end;

function GetBackendPort(Param: String): String;
begin
  Result := BackendPortEdit.Text;
end;

function GetFrontendPort(Param: String): String;
begin
  Result := FrontendPortEdit.Text;
end;

// 检查服务是否存在
function ServiceExists(const ServiceName: String): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('sc.exe', 'query "' + ServiceName + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

// 等待服务状态变为指定状态（最多等待 MaxWaitMs 毫秒）
function WaitForServiceStatus(const ServiceName, TargetState: String; MaxWaitMs: Integer): Boolean;
var
  ResultCode: Integer;
  StartTime: Cardinal;
  CheckCmd: String;
begin
  Result := False;
  StartTime := GetTickCount;
  CheckCmd := 'query "' + ServiceName + '" | findstr /i "' + TargetState + '"';

  while (GetTickCount - StartTime) < Cardinal(MaxWaitMs) do
  begin
    if Exec('sc.exe', CheckCmd, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0) then
    begin
      Result := True;
      Exit;
    end;
    Sleep(500);
  end;
end;

// 强制终止服务进程
function KillServiceProcess(const ServiceName: String): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;

  // 使用 sc qc 获取服务路径，然后用 taskkill 终止
  // 直接使用 taskkill 终止服务主进程
  Exec('taskkill.exe', '/F /IM DataForgeStudio.Api.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;

// 卸载前执行：停止并删除服务
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
  RetryCount: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    // 1. 停止 Nginx 进程（如果运行中）
    Exec('taskkill.exe', '/F /IM nginx.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    // 2. 检查并删除 DFWebService (Nginx Windows 服务)
    if ServiceExists('DFWebService') then
    begin
      // 停止服务
      Exec('sc.exe', 'stop DFWebService', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      WaitForServiceStatus('DFWebService', 'STOPPED', 10000);
      Sleep(1000);

      // 删除服务（带重试机制）
      for RetryCount := 1 to 3 do
      begin
        Exec('sc.exe', 'delete DFWebService', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
        if not ServiceExists('DFWebService') then
          Break;
        Sleep(2000);
      end;
      Sleep(1000);
    end;

    // 3. 检查服务是否存在
    if ServiceExists('DFAppService') then
    begin
      // 4. 发送停止命令
      Exec('sc.exe', 'stop DFAppService', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

      // 5. 等待服务停止（最多等待 10 秒）
      WaitForServiceStatus('DFAppService', 'STOPPED', 10000);

      // 6. 强制终止服务进程（确保进程完全退出）
      KillServiceProcess('DFAppService');
      Sleep(1000);

      // 7. 删除 Windows 服务（带重试机制）
      for RetryCount := 1 to 3 do
      begin
        Exec('sc.exe', 'delete DFAppService', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
        if not ServiceExists('DFAppService') then
          Break;
        Sleep(2000);
      end;

      // 8. 再等待一下确保服务删除生效
      Sleep(1000);
    end;

    // 9. 删除注册表项
    RegDeleteKeyIncludingSubkeys(HKLM, 'Software\DataForgeStudio');

    // 10. 删除试用期跟踪数据（存储在隐藏的注册表位置）
    RegDeleteValue(HKLM, 'SOFTWARE\Microsoft\CryptoAPI\v2\machine', 'CacheData');

    // 11. 删除试用期跟踪文件（ProgramData）
    DeleteFile(ExpandConstant('{commonappdata}\Microsoft\Crypto\RSA\MachineKeys\DataForgeStudio_trial.dat'));

    // 12. 删除废弃的根级 keys 文件夹（如果存在）
    DelTree(ExpandConstant('{app}\keys'), True, True, True);

    // 12b. 删除 Server\keys 文件夹（当前密钥存储位置）
    DelTree(ExpandConstant('{app}\Server\keys'), True, True, True);

    // 13. 删除桌面快捷方式
    DeleteFile(ExpandConstant('{userdesktop}\DataForgeStudio 管理工具.lnk'));
  end;
end;
