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
Source: "{#BuildDir}\installer\DataForgeStudioInstaller.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall nocompression
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

[Run]
Filename: "{tmp}\DataForgeStudioInstaller.exe"; Description: "启动安装向导"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop ""DFAppService"""; RunOnceId: "StopAppService"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete ""DFAppService"""; RunOnceId: "DeleteAppService"; Flags: runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\nginx\logs"
Type: filesandordirs; Name: "{app}\nginx\temp"

[Registry]
Root: HKLM; Subkey: "Software\{#AppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKLM; Subkey: "Software\{#AppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#AppVersion}"
