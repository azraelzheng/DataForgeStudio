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
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\DeployManager.exe
UninstallDisplayName={#AppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#BuildDir}\configurator\Configurator.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall nocompression
Source: "{#BuildDir}\api\*"; DestDir: "{app}\api"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\frontend\*"; DestDir: "{app}\WebSite"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\nginx\*"; DestDir: "{app}\nginx"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\manager\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\config"; Permissions: users-modify
Name: "{app}\keys"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify
Name: "{app}\nginx\logs"; Permissions: users-modify
Name: "{app}\nginx\temp"; Permissions: users-modify

[Registry]
Root: HKLM; Subkey: "Software\{#AppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKLM; Subkey: "Software\{#AppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#AppVersion}"

[Run]
; 安装完成后运行配置器
Filename: "{tmp}\Configurator.exe"; Parameters: "--install-path ""{app}"" --db-server ""{code:GetDbServer}"" --db-port {code:GetDbPort} --db-name ""{code:GetDbName}"" --db-auth ""{code:GetDbAuth}"" --db-user ""{code:GetDbUser}"" --db-password ""{code:GetDbPassword}"" --backend-port {code:GetBackendPort} --frontend-port {code:GetFrontendPort}"; Flags: runhidden waituntilterminated

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop ""DFAppService"""; RunOnceId: "StopAppService"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete ""DFAppService"""; RunOnceId: "DeleteAppService"; Flags: runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\nginx\logs"
Type: filesandordirs; Name: "{app}\nginx\temp"

[Code]
var
  DbServerEdit: TEdit;
  DbPortEdit: TEdit;
  DbNameEdit: TEdit;
  DbAuthRadioWindows: TRadioButton;
  DbAuthRadioSql: TRadioButton;
  DbUserEdit: TEdit;
  DbPasswordEdit: TPasswordEdit;
  BackendPortEdit: TEdit;
  FrontendPortEdit: TEdit;
  DbConfigPage: TWizardPage;
  PortConfigPage: TWizardPage;

procedure InitializeWizard;
begin
  // 创建数据库配置页面
  DbConfigPage := CreateCustomPage(wpSelectDir, '数据库配置', '配置 SQL Server 数据库连接');

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
  end;

  // 数据库名
  with TLabel.Create(WizardForm) do
  begin
    Parent := DbConfigPage.Surface;
    Caption := '数据库名:';
    Left := 0;
    Top := 60;
  end;
  DbNameEdit := TEdit.Create(WizardForm);
  with DbNameEdit do
  begin
    Parent := DbConfigPage.Surface;
    Left := 100;
    Top := 58;
    Width := 200;
    Text := 'DataForgeStudio';
    Enabled := False;
    Color := clBtnFace;
  end;

  // 认证方式
  with TLabel.Create(WizardForm) do
  begin
    Parent := DbConfigPage.Surface;
    Caption := '认证方式:';
    Left := 0;
    Top := 90;
  end;
  DbAuthRadioWindows := TRadioButton.Create(WizardForm);
  with DbAuthRadioWindows do
  begin
    Parent := DbConfigPage.Surface;
    Left := 100;
    Top := 88;
    Width := 150;
    Caption := 'Windows 身份验证';
    Checked := True;
  end;
  DbAuthRadioSql := TRadioButton.Create(WizardForm);
  with DbAuthRadioSql do
  begin
    Parent := DbConfigPage.Surface;
    Left := 250;
    Top := 88;
    Width := 150;
    Caption := 'SQL Server 身份验证';
  end;

  // 用户名
  with TLabel.Create(WizardForm) do
  begin
    Parent := DbConfigPage.Surface;
    Caption := '用户名:';
    Left := 0;
    Top := 120;
  end;
  DbUserEdit := TEdit.Create(WizardForm);
  with DbUserEdit do
  begin
    Parent := DbConfigPage.Surface;
    Left := 100;
    Top := 118;
    Width := 200;
    Text := 'sa';
    Enabled := False;
  end;

  // 密码
  with TLabel.Create(WizardForm) do
  begin
    Parent := DbConfigPage.Surface;
    Caption := '密码:';
    Left := 0;
    Top := 145;
  end;
  DbPasswordEdit := TPasswordEdit.Create(WizardForm);
  with DbPasswordEdit do
  begin
    Parent := DbConfigPage.Surface;
    Left := 100;
    Top := 143;
    Width := 200;
    Enabled := False;
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
  end;
  with TLabel.Create(WizardForm) do
  begin
    Parent := PortConfigPage.Surface;
    Caption := '(API 服务监听端口)';
    Left := 210;
    Top := 22;
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
  end;
  with TLabel.Create(WizardForm) do
  begin
    Parent := PortConfigPage.Surface;
    Caption := '(Web 访问端口)';
    Left := 210;
    Top := 52;
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  // 根据认证方式启用/禁用用户名密码输入
  if CurPageID = DbConfigPage.ID then
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
end;

function GetDbServer(Param: String): String;
begin
  Result := DbServerEdit.Text;
end;

function GetDbPort(Param: String): String;
begin
  Result := DbPortEdit.Text;
end;

function GetDbName(Param: String): String;
begin
  Result := DbNameEdit.Text;
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
