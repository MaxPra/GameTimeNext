#include "GameTimeNext_RemoveOldMsiInstall.iss"
#include "GameTimeNext_RemoveSystemWideInnoInstall.iss"

function RemoveOldInstalls(): Boolean;
begin
  Result := True;

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