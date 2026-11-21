// Empties the program folder before the files of this version are copied, so files an
// older version shipped and this one doesn't (e.g. renamed assemblies) never stay behind,
// without naming them here. The app never writes into the program folder (its data lives
// in %APPDATA% / %LOCALAPPDATA%), so it only holds files the installer ships.
// The uninstaller (unins*) is kept, so the uninstall entry keeps working.
// Only runs when the folder already contains the app's exe: a wrong /DIR= on the command
// line can never empty a foreign folder.
procedure CleanAppDirectory();
var
  AppDir: String;
  FindRec: TFindRec;
  Entries: TStringList;
  EntryPath: String;
  i: Integer;
begin
  AppDir := AddBackslash(ExpandConstant('{app}'));

  if not FileExists(AppDir + '{#AppExeName}') then
  begin
    Log('Program folder not cleaned, no previous installation in it: ' + AppDir);
    Exit;
  end;

  // Collect first, delete afterwards, so the search isn't changed while it runs
  Entries := TStringList.Create;
  try
    if FindFirst(AppDir + '*', FindRec) then
    begin
      try
        repeat
          if (FindRec.Name <> '.') and (FindRec.Name <> '..') and (Pos('unins', Lowercase(FindRec.Name)) <> 1) then
            Entries.Add(FindRec.Name);
        until not FindNext(FindRec);
      finally
        FindClose(FindRec);
      end;
    end;

    for i := 0 to Entries.Count - 1 do
    begin
      EntryPath := AppDir + Entries[i];

      if DirExists(EntryPath) then
      begin
        if not DelTree(EntryPath, True, True, True) then
          Log('Could not remove folder from the program folder: ' + EntryPath);
      end
      else if not DeleteFile(EntryPath) then
        Log('Could not remove file from the program folder: ' + EntryPath);
    end;

    Log('Program folder cleaned: ' + IntToStr(Entries.Count) + ' entries removed from ' + AppDir);
  finally
    Entries.Free;
  end;
end;
