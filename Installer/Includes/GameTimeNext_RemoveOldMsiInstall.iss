const
  OldMSIProductCode = '{AA642EDE-2EA3-4C94-8325-0E112FA8FBF5}';

type
  TOldMsiScope = (msNone, msSystemWide, msUserScoped);

// Looks for the old MSI's uninstall entry and reports which scope it was
// registered under, so the caller can decide whether elevation is needed.
function FindOldMSIUninstallKey(var RootKey: Integer; var Scope: TOldMsiScope): Boolean;
var
  SubKeyName: String;
begin
  SubKeyName := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + OldMSIProductCode;

  if RegKeyExists(HKLM64, SubKeyName) then
  begin
    RootKey := HKLM64;
    Scope := msSystemWide;
    Result := True;
    Exit;
  end;

  if RegKeyExists(HKLM32, SubKeyName) then
  begin
    RootKey := HKLM32;
    Scope := msSystemWide;
    Result := True;
    Exit;
  end;

  if RegKeyExists(HKCU, SubKeyName) then
  begin
    RootKey := HKCU;
    Scope := msUserScoped;
    Result := True;
    Exit;
  end;

  Scope := msNone;
  Result := False;
end;

function IsOldMSIInstalled(): Boolean;
var
  RootKey: Integer;
  Scope: TOldMsiScope;
begin
  Result := FindOldMSIUninstallKey(RootKey, Scope);
end;

function UninstallOldMSI(): Boolean;
var
  RootKey: Integer;
  Scope: TOldMsiScope;
  ResultCode: Integer;
  LogSwitch: String;
begin
  Result := False;

  if not FindOldMSIUninstallKey(RootKey, Scope) then
  begin
    Result := True; // Nothing to do.
    Exit;
  end;

  // A persistent log helps diagnose a silent failure after the fact, since
  // msiexec's own exit code alone rarely explains what went wrong.
  // ForceDirectories is a no-op if the folder already exists, and this
  // early in Setup it may not exist yet.
  ForceDirectories(ExpandConstant('{localappdata}\GameTimeNext'));
  LogSwitch :=
    ' /L*v "' + ExpandConstant('{localappdata}') + '\GameTimeNext\OldMsiUninstall.log"';

  if Scope = msSystemWide then
  begin
    // Machine-wide MSI install: this Setup runs unelevated, so elevate
    // just for this step.
    Result :=
      ShellExec(
        'runas',
        ExpandConstant('{sys}\msiexec.exe'),
        '/x ' + OldMSIProductCode + ' /passive /norestart' + LogSwitch,
        '',
        SW_SHOW,
        ewWaitUntilTerminated,
        ResultCode
      );
  end
  else
  begin
    // Per-user MSI install belongs to the same account this Setup is
    // running as - no elevation needed, and requesting it would force an
    // unnecessary UAC prompt that could even fail for a non-admin user.
    Result :=
      Exec(
        ExpandConstant('{sys}\msiexec.exe'),
        '/x ' + OldMSIProductCode + ' /passive /norestart' + LogSwitch,
        '',
        SW_SHOW,
        ewWaitUntilTerminated,
        ResultCode
      );
  end;

  Result := Result and (ResultCode = 0);
end;

function RemoveOldMsiInstall(): Boolean;
var
  RootKey: Integer;
  Scope: TOldMsiScope;
  ScopeWarning: String;
begin
  Result := True;

  if not FindOldMSIUninstallKey(RootKey, Scope) then
    Exit;

  if Scope = msSystemWide then
    ScopeWarning := #13#10#13#10 + 'This was installed for all users, so you may be asked for administrator credentials.'
  else
    ScopeWarning := '';

  if MsgBox(
    'A previous MSI version of GameTimeNext was found.' + #13#10 +
    'It must be removed before continuing installation.' +
    ScopeWarning + #13#10#13#10 +
    'Uninstall it now?',
    mbConfirmation,
    MB_YESNO
  ) <> IDYES then
  begin
    MsgBox(
      'Setup cannot continue while the old version is installed.',
      mbInformation,
      MB_OK
    );

    Result := False;
    Exit;
  end;

  if not UninstallOldMSI() then
  begin
    MsgBox(
      'Failed to uninstall the previous MSI version.',
      mbError,
      MB_OK
    );

    Result := False;
    Exit;
  end;
end;
