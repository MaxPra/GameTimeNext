#define AppName "GameTimeNext"
#define AppExeName AppName + ".exe"
#define AppVersionSemantic "0.3.1"
#define AppVersionSuffix "beta"
#define AppPublisher "MaxPra"

#if AppVersionSuffix != ""
  #define AppVersion AppVersionSemantic + "-" + AppVersionSuffix
  #define AppShortcutName AppName + " (" + AppVersionSuffix + ")"
#else
  #define AppVersion AppVersionSemantic
  #define AppShortcutName AppName
#endif

#define BuildDirectory "out"
#define BinDirectory "..\GameTimeNext\bin\Release\net10.0-windows"

[Setup]
AppId={{B6D12BF9-6933-4610-BB15-68F30712EEAD}}
AppName={#AppName}
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersionSemantic}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppPublisher}\{#AppName}
DisableDirPage=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName},0
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir={#BuildDirectory}
OutputBaseFilename={#AppName}_v{#AppVersion}_Installer
SetupIconFile=..\GameTimeNext\UI\Ressources\GTN_APP_ICON.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Files]
;General files
Source: "{#BinDirectory}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDirectory}\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDirectory}\*.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDirectory}\*.deps.json"; DestDir: "{app}"; Flags: ignoreversion
;Runtime DLLs
Source: "{#BinDirectory}\runtimes\win-x64\*.dll"; DestDir: "{app}\runtimes\win-x64"; Flags: ignoreversion recursesubdirs
;App Specific
Source: "{#BinDirectory}\Core\*.*"; DestDir: "{app}\Core"; Flags: ignoreversion recursesubdirs; Excludes: "*\UpdateChanges_vNEXT.txt"
Source: "{#BinDirectory}\UI\*.*"; DestDir: "{app}\UI"; Flags: ignoreversion recursesubdirs

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Icons]
Name: "{autoprograms}\{#AppPublisher}\{#AppShortcutName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppShortcutName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent