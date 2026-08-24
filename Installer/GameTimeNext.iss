#define AppName "GameTimeNext"
#define AppExeName AppName + ".exe"
#define AppVersionSemantic "0.4.0"
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
AppId={{AA642EDE-2EA3-4C94-8325-0E112FA8FBF5}
AppName={#AppName}
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersionSemantic}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\{#AppPublisher}\{#AppName}
DisableDirPage=yes
SetupArchitecture=x64
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName},0
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline
OutputDir={#BuildDirectory}
OutputBaseFilename={#AppName}_v{#AppVersion}_Installer
SetupIconFile=..\GameTimeNext\UI\Ressources\GTN_APP_ICON.ico
Compression=lzma2/max
SolidCompression=yes
LZMANumBlockThreads=8
WizardStyle=dynamic
ChangesAssociations=no
CloseApplications=force
CloseApplicationsFilter={#AppExeName}
RestartApplications=no

[Files]
;General files
Source: "{#BinDirectory}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDirectory}\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDirectory}\*.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDirectory}\*.deps.json"; DestDir: "{app}"; Flags: ignoreversion

;Runtime DLLs
Source: "{#BinDirectory}\runtimes\win-x64\*.dll"; DestDir: "{app}\runtimes\win-x64"; Flags: ignoreversion recursesubdirs createallsubdirs

;App Specific
Source: "{#BinDirectory}\ImportPackages\*.*"; DestDir: "{localappdata}\{#AppPublisher}\{#AppName}\temp\import"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BinDirectory}\Core\*.*"; DestDir: "{app}\Core"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*\UpdateChanges_vNEXT.txt,ImportPackages\*"
Source: "{#BinDirectory}\UI\*.*"; DestDir: "{app}\UI"; Flags: ignoreversion recursesubdirs createallsubdirs

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Icons]
Name: "{autoprograms}\{#AppPublisher}\{#AppShortcutName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppShortcutName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
#include "Includes\GameTimeNext_RemoveOldInstalls.iss"

function InitializeSetup(): Boolean;
begin
  Result := RemoveOldInstalls();
end;