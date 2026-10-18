#include "GameTimeNext_RemoveOldMsiInstall.iss"
#include "GameTimeNext_RemoveSystemWideInnoInstall.iss"

// Best-effort: if an old (or currently running) copy of the app is open,
// its files will be locked and any old uninstaller we try to run below
// could fail for reasons unrelated to our own logic. This is silent by
// design (mirrors this Setup's own CloseApplications=force behavior) and
// deliberately ignores whether an instance was actually running.
procedure CloseRunningAppInstances();
var
  ResultCode: Integer;
begin
  Exec(
    ExpandConstant('{sys}\taskkill.exe'),
    '/IM "{#AppExeName}" /F',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode
  );
end;

function RemoveOldInstalls(): Boolean;
begin
  Result := True;

  CloseRunningAppInstances();

  if not RemoveOldMsiInstall() then
  begin
    Result := False;
    Exit;
  end;

  if not RemoveOldInnoInstall() then
  begin
    Result := False;
    Exit;
  end;
end;
