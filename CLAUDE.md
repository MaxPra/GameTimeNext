# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

GameTimeNext is a Windows-only WPF desktop app (.NET 10, `net10.0-windows`, x64) for tracking game playtime, launching games, and managing profiles/playthroughs. Data is stored locally in SQLite. It is the successor to GameTimeX and includes a migration path from it (`Core/Application/GTXMigration`).

Code comments and log messages are largely in German; keep that style when editing nearby code.

## Build & Run

The app depends on the sibling repo **UIX.ViewController.Engine** (`C:\Development\C#\UIX.ViewController.Engine`), referenced as a project (`..\..\UIX.ViewController.Engine\UIX.ViewController.Engine.csproj`). Both are in `GameTimeNext.slnx`. The build fails if that repo isn't checked out next to this one.

```bash
dotnet build GameTimeNext.slnx
dotnet run --project GameTimeNext/GameTimeNext.csproj
dotnet run --project GameTimeNext/GameTimeNext.csproj -- minimized   # start hidden, no splash
```

There is no test project. The installer is built with Inno Setup from `Installer/GameTimeNext.iss`. `RemoteMonitoring/` and `GameTimeNext.Shared/` are untracked work-in-progress folders and are not in the solution.

The version is set in `GameTimeNext.csproj` (`Version`, `VersionTag`, `BuildDate`). User-facing changelogs live in `Core/Application/UpdateChanges/UpdateChanges_v*.txt`. Pending changes go in `UpdateChanges_vNEXT.txt`, and each file has to be listed as `Content` in the csproj.

## Architecture

### UIX framework (Application → ViewController → View)
All UI is built on UIX.ViewController.Engine. Each module in `Core/Application/<Module>/` follows the same pattern:
- **`*App : UIXApplication`**: the entry point. `InitializeApplicationOutput()` creates the View and the ViewController and wires them together. It holds the table objects being edited (e.g. `PlaythroughEditApp.T1plthr`) and exposes entry methods like `Edit(...)` / `CreateNew(...)` that take a result callback.
- **`Controller/*ViewController : UIXViewControllerBase`**: overrides lifecycle hooks (`Init`, `BuildFirstImpl`, `BuildImpl`, `Check`, `FillViewImpl`, `FillDBOImpl`, `SaveDBOImpl`, `TriggeredEvent`, ...). In `Init()` it registers data with `AddIdentifier("T1XXXXX", obj)` and `AddSource(...)`. Validation goes in `Check()` via `AddViewError`. It returns results through a nested `UIXViewReturn` subclass.
- **Event handlers are bound by reflection, by naming convention.** A method named `EV_<x:Name>` (e.g. `EV_btnSave`) handles that control's event, and `EV_<name>_CtxtOpening` handles its context menu opening. Renaming a control in XAML silently breaks its handler.
- **Views (`Views/*.xaml`)** bind controls to table fields with `uixprops:UIXAttachedProperty.Path="T1PLTHR.PTDE"`, and the framework moves the values between view and object. `ViewIndicator` flags (e.g. `"CN"` create new, `"ED"` edit) switch behavior within a single view.
- Top-level launchable modules are registered in `Core/Application/StartableApps.json` (code, name, fully qualified class name).

### Data layer (table naming convention)
Tables are named with 7 characters (`T1PROFI`, `T1PLTHR`, `T1SESSI`, `T1GROUP`, `T1GRPPO`, `T1CTABH/D`, `T1METAH/P`), and columns use 4-letter codes. Each table has:
- `TableObjects/T1xxxxx.cs`: a POCO (`UIXTableObjectBase`) with `[UIXSignatureField(n)]` properties and change tracking (`HasChanged`, `AcceptChanges`, `State`).
- `TableObjects/K1xxxxx.cs`: string constants for the table and field names, used with `UIXQuery`.
- `DataManagers/TXxxxxxBasic.cs`: generated CRUD (`CreateNew`, `Read`, `Save`, `Delete`, ...) using raw `System.Data.SQLite`.
- `DataManagers/TXxxxxx.cs`: an empty subclass for hand-written overrides.
- `DataManagers/TFxxxxx.cs`: static query/helper functions (e.g. `TFPLTHR.GetCurrentPlaythrough`) built with `UIXQuery`.

`T1`/`K1`/`TX*Basic`/`TX` are **generated** from metadata (`T1METAH`/`T1METAP`) by `Core/Application/Metadata/CFMetadataClassGenerator.cs` (with `CFMetadataTableGenerator` for the SQL schema), and you can run this from the in-app Metadata module. Make changes through the generator or keep them consistent with its output. Put custom logic in `TX`/`TF`, not in `TX*Basic`.

### Schema changes and migrations
- `DataBaseManager.Initialize()` creates all tables on first run only.
- Existing installs are upgraded by `Core/Application/MigrationTasks/MigrationManager.MigrateIfNeeded()`. It compares the stored config version with the current version, and each `MigTask_<ver>b_<seq>.cs` runs its own SQL (`ALTER TABLE`, data fixes). **When you add a column, update both the CREATE TABLE in `DataBaseManager` and a migration task.** A missing migration has caused bugs before (e.g. `T1PLTHR.PTPA`).
- The startup order is in `AppEnvironment.Initalize()`: startable apps → backup → changelog → migrations → `DataBaseImporter.Import()` (import packages from `ImportPackages/`) → DevSync CSV import (debug only) → save version.

### DevSync (debug only)
For tables marked `DSYNC=1` in metadata (`IsDevSynced`), `DevSyncCsvSyncService` exports the table to `devsync/<TABLE>.csv` on every save/delete. On startup it re-imports these CSVs, but only when `FnSystem.IsDebug()` is true (a DEBUG build plus an attached debugger or a running `devenv`). This keeps reference data (codetables, metadata) in sync across developers through git. The CSVs under `devsync/` are therefore source-controlled data, not throwaway output.

### Runtime
- `App.xaml.cs` enforces a single instance, parses start args into `AppEnvironment.StartArguments`, initializes config, logging (`FnLog`), and the DB, and then starts `MainApp`.
- `AppEnvironment` is the global static service locator (config, DB manager, current profile, running apps, background processes).
- Background processes: `GameRunningProcess` (polls every 500 ms to detect running game executables and switch profiles) and `GlobalKeyInputProcess` (global hotkeys, e.g. Ctrl+M quick launch). They are stopped on exit, and an auto-backup is created then.
- External integrations live in `Core/Framework/` (SteamGridDB, Igdb/Twitch, GitHub version check, LauncherIntegration).

## Repo conventions

- `.claude/settings.json` denies reading or editing `bin/`, `obj/`, and `.github/`, and denies `git push` and `git reset`.
- The project skill `mark-code` inserts `// MARKEDBYCLAUDE: ...` comments for the Visual Studio Task List when asked to mark or flag code.
