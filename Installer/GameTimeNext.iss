#define AppName "GameTimeNext"
#define AppExeName AppName + ".exe"
#define AppVersionSemantic "0.4.1"
#define AppVersionSuffix "beta"
#define AppPublisher "MaxPra"

;Old Setups
#define OldPublisher "MaxPra"
#define OldMsiProductCode "{AA642EDE-2EA3-4C94-8325-0E112FA8FBF5}"
#define OldInnoAppId "{B6D12BF9-6933-4610-BB15-68F30712EEAD}"

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
AppId={{D4A9F2C1-7E11-4A7B-9C2D-9C6A1B2F77AA}}
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
WizardStyle=dynamic

[Files]
;General files
Source: "{#BinDirectory}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDirectory}\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDirectory}\*.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDirectory}\*.deps.json"; DestDir: "{app}"; Flags: ignoreversion

;Runtime DLLs
Source: "{#BinDirectory}\runtimes\win-x64\*.dll"; DestDir: "{app}\runtimes\win-x64"; Flags: ignoreversion recursesubdirs

;App Specific
Source: "{#BinDirectory}\ImportPackages\*.*"; DestDir: "{localappdata}\GameTimeNext\Import"; Flags: ignoreversion recursesubdirs
Source: "{#BinDirectory}\Core\*.*"; DestDir: "{app}\Core"; Flags: ignoreversion recursesubdirs; Excludes: "*\UpdateChanges_vNEXT.txt,ImportPackages\*"
Source: "{#BinDirectory}\UI\*.*"; DestDir: "{app}\UI"; Flags: ignoreversion recursesubdirs

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Icons]
Name: "{autoprograms}\{#AppPublisher}\{#AppShortcutName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppShortcutName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]

function UninstallMSI(): Boolean;
var
  ResultCode: Integer;
begin
  Result :=
    Exec(
      ExpandConstant('{sys}\msiexec.exe'),
      '/x {#OldMsiProductCode} /passive /norestart',
      '',
      SW_HIDE,
      ewWaitUntilTerminated,
      ResultCode
    );

  Result := Result and (ResultCode = 0);
end;

function UninstallOldInno(): Boolean;
var
  UninstallPath: string;
  ResultCode: Integer;
begin
  Result := False;

  if RegQueryStringValue(
    HKLM,
    'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#OldInnoAppId}_is1',
    'UninstallString',
    UninstallPath
  ) then
  begin
    Exec(RemoveQuotes(UninstallPath), '', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Result := (ResultCode = 0);
  end
  else
    Result := True; // not installed is fine
end;

procedure RemoveOldFolders();
begin
  DelTree(ExpandConstant('{autopf}\{#OldPublisher}\{#AppName}'), True, True, True);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;

  // 1. MSI uninstall
  if RegKeyExists(HKLM,
    'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#OldMsiProductCode}') then
  begin
    if MsgBox('Old MSI version detected. Uninstall it?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      if not UninstallMSI() then
      begin
        MsgBox('MSI uninstall failed. Setup aborted.', mbError, MB_OK);
        Result := False;
        Exit;
      end;
    end
    else
    begin
      Result := False;
      Exit;
    end;
  end;

  // 2. Old Inno uninstall
  if MsgBox('Check for old Inno version (MaxPra)?', mbConfirmation, MB_YESNO) = IDYES then
  begin
    UninstallOldInno();
  end;

  // 3. Cleanup leftovers
  RemoveOldFolders();
end;