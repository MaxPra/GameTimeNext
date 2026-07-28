const
  OldMSIProductCode = '{AA642EDE-2EA3-4C94-8325-0E112FA8FBF5}';

function IsOldMSIInstalled(): Boolean;
begin
  Result :=
    RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + OldMSIProductCode) or
    RegKeyExists(HKLM32, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + OldMSIProductCode) or
    RegKeyExists(HKCU, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + OldMSIProductCode);
end;

function UninstallOldMSI(): Boolean;
var
  ResultCode: Integer;
begin
  Result :=
    ShellExec(
      'runas',
      ExpandConstant('{sys}\msiexec.exe'),
      '/x ' + OldMSIProductCode + ' /passive /norestart',
      '',
      SW_SHOW,
      ewWaitUntilTerminated,
      ResultCode
    );

  Result := Result and (ResultCode = 0);
end;

function RemoveOldMsiInstall(): Boolean;
begin
  Result := True;

  if not IsOldMSIInstalled() then
    Exit;

  if MsgBox(
    'A previous MSI version of GameTimeNext was found.' + #13#10 +
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