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
AppId={{AA642EDE-2EA3-4C94-8325-0E112FA8FBF5}}
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

const OldProductCode = '{AA642EDE-2EA3-4C94-8325-0E112FA8FBF5}';

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
  UninstallOK: Boolean;
begin
  Result := True;
  UninstallOK := False;

  if RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + OldProductCode) or
     RegKeyExists(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\' + OldProductCode) then
  begin
    if MsgBox(
      'A previous MSI version of GameTimeNext was found.' + #13#10 +
      'It must be removed before continuing installation.' + #13#10#13#10 +
      'Uninstall it now?',
      mbConfirmation, MB_YESNO
    ) = IDYES then
    begin
      UninstallOK :=
        Exec(
          ExpandConstant('{sys}\msiexec.exe'),
          '/x ' + OldProductCode + ' /passive /norestart',
          '',
          SW_SHOW,
          ewWaitUntilTerminated,
          ResultCode
        );

      if (not UninstallOK) or (ResultCode <> 0) then
      begin
        MsgBox('Uninstall failed. Setup will abort.', mbError, MB_OK);
        Result := False;
        Exit;
      end;
    end
    else
    begin
      MsgBox('Setup cannot continue while the old version is installed.', mbInformation, MB_OK);
      Result := False;
      Exit;
    end;
  end;
end;