# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

GameTimeNext is a Windows-only WPF desktop app (.NET `net10.0-windows`, x64) for tracking game playtime, launching games, and managing playthroughs. It succeeds GameTimeX (`../GameTimeX`).

## Build & checks

- Build: `dotnet build GameTimeNext.slnx` (from repo root). The solution references **`../UIX.ViewController.Engine`**, a sibling repository that must be checked out next to this one; much of the base framework (`UIXApplication`, `UIXViewControllerBase`, `UIXTableObjectBase`, `UIXQuery`, `FnString`, `FnLog`, …) lives there, not here.
- Run: `dotnet run --project GameTimeNext/GameTimeNext.csproj`.
- Code rules check: `python Scripts/cleancode.py` — enforces where certain string literals (`"GameTimeNext"`, `"MaxPra"`, …) and identifiers (`DirectorySeparatorChar`) may appear, with per-file allowed counts. If a new occurrence is legitimate, add it to the allowlist in the script.
- There is no test suite (`GameTimeNext.Tests` contains only build output).
- Installer: Inno Setup script at `Installer/GameTimeNext.iss`. Version is set in `GameTimeNext.csproj` (`VersionSemantic`, `VersionTag`, `BuildDate`); release notes go in `Core/Application/UpdateChanges/UpdateChanges_v*.txt` (`_vNEXT.txt` for unreleased).

## Code rules

From `Scripts/unallowed_code.md` and `.github/copilot-instructions.md`:
- Use `FnString.IsNullEmptyOrWhitespace` instead of `string.IsNullOrWhiteSpace` / `string.IsNullOrEmpty`.
- Use `FnDirectory.EnsureDirectoryExists` instead of `if (!Directory.Exists) Directory.CreateDirectory`.
- Paths/app names come from `AppConfig` (`Core/Framework/Config/AppConfig.Paths.cs`), not hard-coded strings.
- Do not suppress validation on ComboBox `SelectionChanged`; tab validation must run together with field validation/adorner checks.
- XML doc comments: concise, no icons/emojis.
- All comments in source code (C#, XAML, scripts — in this repo and in `../UIX.ViewController.Engine`) must be in English, even when the conversation is in German.

## Architecture

### Layout
- `GameTimeNext/Core/Framework/` — app-wide infrastructure: `AppEnvironment` (global service locator: config, DB manager, background processes, started apps), `ApplicationLauncher` (opens apps as tabs), `Config/`, `DataBase/`, external integrations (`SteamGridDB`, `Igdb`, `GitHub`, `LauncherIntegration`), `Utils/` (`Fn*` static helpers).
- `GameTimeNext/Core/Application/<Module>/` — one folder per feature module (Profiles, Playthroughs, Dashboard, Codetables, Settings, Metadata, …).
- `devsync/` — CSV snapshots of dev-synced tables (codetables, metadata) committed to the repo.

### Modules ("applications")
Each module is a `UIXApplication` subclass (`<Name>App.cs`) that creates its views in `InitializeApplicationOutput()` and wires each view to a controller. Modules implementing `IUIXApplicationStarter` can be hosted in a tab; `ApplicationLauncher.LaunchApplication` instantiates them by type name via reflection. Inside a module:
- `Views/` — XAML views; `Controller/` — `UIXViewControllerBase` subclasses; `ViewModel(s)/` — WPF view models.
- Controllers follow a fixed lifecycle of overrides: `Init`, `BuildFirstImpl`, `BuildImpl`, `FillViewImpl`, `CheckImpl`, `FillDBOImpl`, `SaveDBOImpl`, `TriggeredEvent`, `DataWrapperSelectionChangedImpl`. `Init` registers data sources/identifiers (`AddSource`, `AddIdentifier`).
- `ViewIndicator` encodes the view mode: `CN` = create new, `ED` = edit, `SE` = select.
- `CF<Name>App.cs` — static helper functions for a module (formatting, dialogs, etc.).

### Data layer (SQLite)
Tables use 6-char names with a prefix per class role, e.g. for table `T1SESSI`:
- `TableObjects/T1SESSI.cs` — row object (`UIXTableObjectBase`), 4-char uppercase column properties marked `[UIXSignatureField(n)]`.
- `TableObjects/K1SESSI.cs` — constants for table and field names; use these with `UIXQuery` instead of string literals.
- `DataManagers/TXSESSIBasic.cs` — generated CRUD (`CreateNew`, `Read`, `Save`, `Delete`, …). **Do not hand-edit**; it is regenerated.
- `DataManagers/TXSESSI.cs` — hand-written subclass of `TX…Basic` for overrides; never overwritten by the generator.
- `DataManagers/TFSESSI.cs` — hand-written static query/business functions for the table.

Schema is metadata-driven: table definitions live in metadata tables `T1METAH` (header) / `T1METAP` (positions), editable in the Metadata module. `MigrationFactory.Metadata.CodeGenerator.GenerateFor(T1METAH)` generates the T1/K1/TXBasic/TX classes; if a file with that name already exists in the solution it is overwritten in place (except `TX*.cs`), otherwise it is written to `%LOCALAPPDATA%/<Publisher>/GameTimeNext/dev/genClass/`.

The metadata tables themselves are not rows in `T1METAH`/`T1METAP`; their schema is hard-coded in `MigrationFactory.Metadata.METADATA`. `CodeGenerator.GenerateForMetadata()` (button "Generate classes of the metadata tables" in the Metadata view) generates their classes from that definition into `Core/Application/Metadata/Data/`, following the same T1/K1/TXBasic/TX pattern. To change a metadata table, edit `METADATA`, then regenerate. Generated `TX*Basic` classes take an optional `SQLiteConnection` in the constructor (used when migrating into another database), and set `CRUS`/`CHUS` on insert/update when the table has those fields.

`MigrationFactory` (`Core/Framework/DataBase/Migration/`, partial class split by concern) handles schema diff/migration from metadata, CSV import/export (`ToCsv`/`FromCsv`), and import packages. `ImportType.DevSync` and `MetadataGenerator` only run in debug builds. In debug, saving a table object with `IsDevSynced == true` re-exports its CSV into `devsync/`.

### Startup & versioned migrations
`App.xaml.cs` `OnStartup`: file-system migration → load `AppConfig` → init `DataBaseManager` → `AppEnvironment.Initalize()` → start `MainApp`. Version-specific data migrations are `MigTask_<version>_<seq>` classes (subclassing `MigTaskBase`) registered in order in `MigrationTasks/MigrationManager.Migrate()`.

## Project skills

- `/mark-code` — mark code locations with `MARKEDBYCLAUDE` comments for the Visual Studio Task List.
