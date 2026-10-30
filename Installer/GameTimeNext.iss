#define AppName "GameTimeNext"
#define AppExeName AppName + ".exe"
#define AppPublisher "CryloudStudios"

#define BuildDirectory "out"
#define BinDirectory "..\GameTimeNext\bin\Release\net10.0-windows10.0.26100.0"
#define AppExePath BinDirectory + "\" + AppExeName
#define BinDirectoryRemoteMonitoring "..\RemoteMonitoring\bin\Release\net10.0\publish"

;Version is taken from the built exe (maintained only in GameTimeNext.csproj)
#define VerMajor
#define VerMinor
#define VerRev
#define VerBuild
#expr GetVersionComponents(AppExePath, VerMajor, VerMinor, VerRev, VerBuild)
#define AppVersionSemantic Str(VerMajor) + "." + Str(VerMinor) + "." + Str(VerRev)

;ProductVersion is the InformationalVersion, e.g. "0.4.0-beta+2026-06-21": the suffix is the part between "-" and "+"
#define ProductVersion GetStringFileInfo(AppExePath, "ProductVersion")
#define AppVersionSuffix Copy(ProductVersion, Pos("-", ProductVersion) + 1)
#if Pos("+", AppVersionSuffix) > 0
  #define AppVersionSuffix Copy(AppVersionSuffix, 1, Pos("+", AppVersionSuffix) - 1)
#endif

#if AppVersionSuffix != ""
  #define AppVersion AppVersionSemantic + "-" + AppVersionSuffix
  #define AppShortcutName AppName + " (" + AppVersionSuffix + ")"
#else
  #define AppVersion AppVersionSemantic
  #define AppShortcutName AppName
#endif

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
Source: "..\GameTimeNext\ImportPackages\*.zip"; DestDir: "{localappdata}\{#AppPublisher}\{#AppName}\temp\import"; Flags: ignoreversion
Source: "{#BinDirectory}\Core\*.*"; DestDir: "{app}\Core"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*\UpdateChanges_vNEXT.md"

;Remote Monitoring
Source: "{#BinDirectoryRemoteMonitoring}\*.exe"; DestDir: "{app}\RemoteMonitoring"; Flags: ignoreversion
Source: "{#BinDirectoryRemoteMonitoring}\*.dll"; DestDir: "{app}\RemoteMonitoring"; Flags: ignoreversion
Source: "{#BinDirectoryRemoteMonitoring}\*.runtimeconfig.json"; DestDir: "{app}\RemoteMonitoring"; Flags: ignoreversion
Source: "{#BinDirectoryRemoteMonitoring}\*.staticwebassets.endpoints.json"; DestDir: "{app}\RemoteMonitoring"; Flags: ignoreversion
Source: "{#BinDirectoryRemoteMonitoring}\*.deps.json"; DestDir: "{app}\RemoteMonitoring"; Flags: ignoreversion
Source: "{#BinDirectoryRemoteMonitoring}\wwwroot\*.*"; DestDir: "{app}\RemoteMonitoring\wwwroot"; Flags: ignoreversion recursesubdirs

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