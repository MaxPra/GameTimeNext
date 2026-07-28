const
  // GameTimeNext.iss's [Setup] AppId directive was originally written as
  // {{...}} with a double closing brace - an escaping mistake, since only
  // the opening brace needs doubling ({{ -> literal "{"). That made every
  // install built before the fix (system-wide betas, and any per-user
  // builds tested during this migration) register itself under an AppId
  // ending in a double "}}", e.g. "...\{AA642EDE-...-FBF5}}_is1".
  //
  // The directive has since been corrected to a single closing brace, so
  // installs built going forward use the clean, textbook-correct AppId.
  // Machines may still have an old install registered under either form
  // depending on which build they last ran, so both are checked here.
  OldInnoAppIdBuggy = '{AA642EDE-2EA3-4C94-8325-0E112FA8FBF5}}'; // pre-fix, double closing brace
  OldInnoAppIdFixed = '{AA642EDE-2EA3-4C94-8325-0E112FA8FBF5}';  // post-fix, single closing brace

type
  TOldInnoScope = (scNone, scSystemWide, scUserScoped);

// Looks for an old Inno Setup uninstall entry under either the pre-fix or
// post-fix AppId, checking system-wide locations (HKLM64/HKLM32) as well as
// a possible old per-user install (HKCU). Returns the scope it was found in,
// if any. System-wide is checked first (in either AppId form), then
// per-user, so a machine with leftovers from both scopes gets its
// machine-wide one handled first.
function FindOldInnoUninstallKey(var RootKey: Integer; var SubKeyName: String; var Scope: TOldInnoScope): Boolean;
var
  AppIds: array[0..1] of String;
  i: Integer;
  CandidateSubKey: String;
begin
  AppIds[0] := OldInnoAppIdBuggy;
  AppIds[1] := OldInnoAppIdFixed;

  // System-wide: any leftover install under EITHER AppId form must go,
  // since the whole point of this migration is ending up user-scoped only,
  // regardless of which script version created the old install.
  for i := 0 to GetArrayLength(AppIds) - 1 do
  begin
    CandidateSubKey := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + AppIds[i] + '_is1';

    if RegKeyExists(HKLM64, CandidateSubKey) then
    begin
      RootKey := HKLM64;
      SubKeyName := CandidateSubKey;
      Scope := scSystemWide;
      Result := True;
      Exit;
    end;

    if RegKeyExists(HKLM32, CandidateSubKey) then
    begin
      RootKey := HKLM32;
      SubKeyName := CandidateSubKey;
      Scope := scSystemWide;
      Result := True;
      Exit;
    end;
  end;

  // User-scoped: only the pre-fix (buggy) AppId counts as "old" here. A
  // user-scoped entry under the current (fixed) AppId is just this app's
  // own previous version - Inno's normal upgrade behavior already handles
  // that in place, so it must NOT be routed through this forced-uninstall
  // flow, or every future update would wrongly demand a full reinstall.
  CandidateSubKey := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + OldInnoAppIdBuggy + '_is1';

  if RegKeyExists(HKCU, CandidateSubKey) then
  begin
    RootKey := HKCU;
    SubKeyName := CandidateSubKey;
    Scope := scUserScoped;
    Result := True;
    Exit;
  end;

  Scope := scNone;
  Result := False;
end;

function IsOldInnoInstalled(): Boolean;
var
  RootKey: Integer;
  SubKeyName: String;
  Scope: TOldInnoScope;
begin
  Result := FindOldInnoUninstallKey(RootKey, SubKeyName, Scope);
end;

function UninstallOldInno(): Boolean;
var
  RootKey: Integer;
  SubKeyName: String;
  Scope: TOldInnoScope;
  UninstallString: String;
  ResultCode: Integer;
begin
  Result := False;

  if not FindOldInnoUninstallKey(RootKey, SubKeyName, Scope) then
  begin
    Result := True; // Nothing to do.
    Exit;
  end;

  if not RegQueryStringValue(RootKey, SubKeyName, 'UninstallString', UninstallString) then
  begin
    MsgBox(
      'Could not determine how to remove the previous Inno Setup installation.',
      mbError,
      MB_OK
    );
    Exit;
  end;

  // The registry value is normally quoted, e.g. "C:\...\unins000.exe"
  UninstallString := RemoveQuotes(UninstallString);

  if not FileExists(UninstallString) then
  begin
    // The registry entry points at an uninstaller that's no longer on disk
    // (e.g. the install folder was deleted by hand rather than uninstalled
    // properly). There's nothing left to actually uninstall - just clear
    // the stale registry entry so Setup doesn't keep tripping over it.
    // Note: deleting a system-wide (HKLM) key needs admin rights, which
    // this unelevated Setup doesn't have, so that case can still fail.
    if RegDeleteKeyIncludingSubkeys(RootKey, SubKeyName) then
      Result := True
    else
      MsgBox(
        'Found a leftover registry entry for a previous installation, ' +
        'but could not remove it. It may require administrator rights.',
        mbError,
        MB_OK
      );
    Exit;
  end;

  if Scope = scSystemWide then
  begin
    // Machine-wide install lives in an admin-only location, but this Setup
    // itself runs with lowest privileges - elevate just for this step.
    Result :=
      ShellExec(
        'runas',
        UninstallString,
        '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART',
        '',
        SW_SHOW,
        ewWaitUntilTerminated,
        ResultCode
      );
  end
  else
  begin
    // Per-user install belongs to the same account this Setup is running
    // as, so no elevation is needed (and shouldn't be requested).
    Result :=
      Exec(
        UninstallString,
        '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART',
        '',
        SW_SHOW,
        ewWaitUntilTerminated,
        ResultCode
      );
  end;

  Result := Result and (ResultCode = 0);
end;

function RemoveOldInnoInstall(): Boolean;
var
  RootKey: Integer;
  SubKeyName: String;
  Scope: TOldInnoScope;
  ScopeDescription: String;
begin
  Result := True;

  // Loop in case both an old system-wide install AND an old per-user install
  // are present on this machine at the same time.
  while FindOldInnoUninstallKey(RootKey, SubKeyName, Scope) do
  begin
    if Scope = scSystemWide then
      ScopeDescription := 'system-wide'
    else
      ScopeDescription := 'user-scoped';

    if MsgBox(
      'A previous ' + ScopeDescription + ' installation of GameTimeNext was found.' + #13#10 +
      'It must be removed before continuing installation.' + #13#10#13#10 +
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

    if not UninstallOldInno() then
    begin
      MsgBox(
        'Failed to uninstall the previous ' + ScopeDescription + ' version.',
        mbError,
        MB_OK
      );

      Result := False;
      Exit;
    end;
  end;
end;
