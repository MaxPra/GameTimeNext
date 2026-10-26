# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

GameTimeNext is a Windows-only WPF desktop app (.NET `net10.0-windows`, x64) for tracking game playtime, launching games, and managing profiles/playthroughs. Data is stored locally in SQLite. It succeeds GameTimeX (`../GameTimeX`) and includes a migration path from it (`Core/Application/GTXMigration`).

## Build & checks

The solution references **`../UIX.ViewController.Engine`**, a sibling repository that must be checked out next to this one (both are in `GameTimeNext.slnx`); the build fails without it. Much of the base framework (`UIXApplication`, `UIXViewControllerBase`, `UIXTableObjectBase`, `UIXQuery`, `FnString`, `FnLog`, …) lives there, not here.

```bash
dotnet build GameTimeNext.slnx
dotnet run --project GameTimeNext/GameTimeNext.csproj
dotnet run --project GameTimeNext/GameTimeNext.csproj -- minimized   # start hidden, no splash
```

- Code rules check: `python Scripts/cleancode.py` — enforces where certain string literals (`"GameTimeNext"`, `"MaxPra"`, …) and identifiers (`DirectorySeparatorChar`) may appear, with per-file allowed counts. If a new occurrence is legitimate, add it to the allowlist in the script.
- There is no test suite (`GameTimeNext.Tests` contains only build output). `RemoteMonitoring/` and `GameTimeNext.Shared/` are untracked work-in-progress folders and are not in the solution.
- Installer: Inno Setup script at `Installer/GameTimeNext.iss`.
- App icon: the sources are the SVGs in `Branding/`, one per size range (`icon_16.svg` pixel aligned; `icon_small.svg` for 24/32 px, plain G without tile so it fills the taskbar slot; `icon_medium.svg` for 48/64 px; `icon_large.svg` for 256 px). `powershell -ExecutionPolicy Bypass -File Branding/build_icon.ps1` renders them with headless Edge and writes `GameTimeNext/UI/Ressources/GTN_APP_ICON.ico` (exe, taskbar, installer) and `Branding/logo_1024.png`. Never edit the `.ico` directly: change the SVGs and rerun the script. The mark is a game cover frame forming a "G" (dot top right = start of the playtime line), with T and N built into it from 48 px up; small sizes stay G only because T/N are unreadable there. `UI/Ressources/GTN_CLS_ICON.png` is the CryLoud Studios logo, not the app icon.
- Version is set in `GameTimeNext.csproj` (`VersionSemantic`, `VersionTag`, `BuildDate`). Release notes go in `Core/Application/UpdateChanges/UpdateChanges_v*.md` (`_vNEXT.md` for unreleased), written in GitHub Markdown (alerts like `> [!IMPORTANT]` are supported); each file has to be listed as `Content` in the csproj. They are shown after an update in the CFMBOX (`CFMBOXType.Markdown`, rendered by `UIXMarkdownRenderer` in the engine, `FrameworkElements/Markdown/`); older versions are plain `.txt` (shown as memo text). Start profile "Show changelog vNEXT" (`-showchangelog`) always shows `_vNEXT` to check the result.

## Code rules

From `Scripts/unallowed_code.md` and `.github/copilot-instructions.md`:
- Use `FnString.IsNullEmptyOrWhitespace` instead of `string.IsNullOrWhiteSpace` / `string.IsNullOrEmpty`.
- Use `FnDirectory.EnsureDirectoryExists` instead of `if (!Directory.Exists) Directory.CreateDirectory`.
- Paths/app names come from `AppConfig` (`Core/Framework/Config/AppConfig.Paths.cs`), not hard-coded strings.
- Do not suppress validation on ComboBox `SelectionChanged`; tab validation must run together with field validation/adorner checks.
- XML doc comments: concise, no icons/emojis.
- **`x:Name` in XAML** always starts with the control type as a lowercase prefix, followed by a PascalCase name (e.g. `btnSave`, `txbDescription`, `cmbParam`, `chbPaused`, `lblTitle`). Only `PART_*` names required by WPF control templates are exempt. Existing names with an uppercase prefix (`BtnSave`, `TxbDescription`, …) are legacy and are being renamed; when renaming, also rename the matching `EV_<x:Name>` handlers and all code references.
- All comments in source code (C#, XAML, scripts — in this repo and in `../UIX.ViewController.Engine`) must be in English, even when the conversation is in German.
- **No logic in code-behind:** `*.xaml.cs` files contain only `InitializeComponent()`. View logic goes into the module's controller (for `MainWindow`: `MainWindowController`); reusable UI behavior goes into attached properties / helpers in `Core/Framework/UI/`, switched on from XAML or styles. Existing code-behind logic is legacy, not a pattern.
- **Codetable keys** are compared directly as string literals (e.g. `t1profi.LNTY == "STEAM"`); don't create an enum or constant class per codetable entry.
- **Culture:** data (SQLite, CSV, files, IDs) uses `InvariantCulture`, the UI uses `CurrentCulture` explicitly; CA1304/CA1305 are build errors. All value conversions go through `UIXSqliteDataType.Convert` (C# / SQLite / CSV / UI). Numbers typed into the UI are parsed with `ConversionType.UI` (strict, current culture, no thousands separators) — never with `double.TryParse` and friends directly. Numeric TextBoxes bound via `UIXAttachedProperty.Path`, `FnControls.ContainsOnlyNumericValue` and `FnConvert.ToDouble` already use it.

## Architecture

### Layout
- `GameTimeNext/Core/Framework/` — app-wide infrastructure: `AppEnvironment` (global service locator: config, DB manager, current profile, background processes, started apps), `ApplicationLauncher` (opens apps as tabs), `Config/`, `DataBase/`, external integrations (`SteamGridDB`, `Igdb`/Twitch, `GitHub` version check, `LauncherIntegration`), `Utils/` (`Fn*` static helpers).
- `GameTimeNext/Core/Application/<Module>/` — one folder per feature module (Profiles, Playthroughs, Dashboard, Codetables, Settings, Metadata, …).
- `devsync/` — CSV snapshots of dev-synced tables (codetables, metadata) committed to the repo.

### Modules ("applications")
Each module is a `UIXApplication` subclass (`<Name>App.cs`) that creates its views in `InitializeApplicationOutput()` and wires each view to a controller. It holds the table objects being edited (e.g. `PlaythroughEditApp.T1plthr`) and exposes entry methods like `Edit(...)` / `CreateNew(...)` that take a result callback. Modules implementing `IUIXApplicationStarter` can be hosted in a tab; `ApplicationLauncher.LaunchApplication` instantiates them by type name via reflection. Top-level launchable modules are registered in `Core/Application/StartableApps.json` (code, name, fully qualified class name).

Every application (also dialogs, edit apps and background processes) must override `InitializeAppInfo()` and call `ReadFromAppProperties(nameof(<ClassName>))` there; it runs in the constructor before `InitializeApplicationOutput()`. The properties (`AppProperties`: `ApplicationName`, `ApplicationDescription`, `ApplicationHelpText` as Markdown, `IsMenuCallPossible`) are maintained in the module "Application Properties Definition" (table `T1APRDH`, ID = class name). The engine only knows `IUIXAppPropertiesProvider`; GameTimeNext registers `Core/Framework/AppPropertiesProvider.cs` in `App.xaml.cs` after `AppEnvironment.Initalize()`. Applications created before that, or without a definition, get properties that only contain their name.

Error messages work the same way: they are maintained in the module "Errormessages" (table `T1ERMSG`, key = message number `ERMNR`, text `ERMSG`, hint `ERHIN`). The engine only knows `IUIXErrorMessageProvider`; GameTimeNext registers `Core/Framework/ErrorMessageProvider.cs` as `UIXApplication.ErrorMessageProvider` in `App.xaml.cs` after `AppEnvironment.Initalize()`. Call `GetErrorMessage("<number>", p1, p2, ...)` on the application object (instance method of `UIXApplication`; in controllers the shortcut `GetErrorMessage(...)` delegates to `App`, e.g. for `AddViewError`); placeholders `&1`, `&2`, ... are replaced with the parameters in order (current culture) and a typed `\n` becomes a line break, `GetErrorMessageHint` does the same for the hint. An unknown number returns a fallback text and logs a warning.

Inside a module:
- `Views/` — XAML views; `Controller/` — `UIXViewControllerBase` subclasses; `ViewModel(s)/` — WPF view models.
- Controllers follow a fixed lifecycle of overrides: `Init`, `BuildFirstImpl`, `BuildImpl`, `FillViewImpl`, `CheckImpl`, `FillDBOImpl`, `SaveDBOImpl`, `TriggeredEvent`, `DataWrapperSelectionChangedImpl`. `Init` registers data sources/identifiers (`AddSource`, `AddIdentifier("T1XXXXX", obj)`). Validation reports errors via `AddViewError`. Results are returned through a nested `UIXViewReturn` subclass.
- **Event handlers are bound by reflection, by naming convention.** A method named `EV_<x:Name>` (e.g. `EV_btnSave`) handles that control's event, and `EV_<name>_CtxtOpening` handles its context menu opening. Renaming a control in XAML silently breaks its handler.
- Views bind controls to table fields with `uixprops:UIXAttachedProperty.Path="T1PLTHR.PTDE"`; the framework moves values between view and object.
- `ViewIndicator` encodes the view mode: `CN` = create new, `ED` = edit, `SE` = select.
- `CF<Name>App.cs` — static helper functions for a module (formatting, dialogs, etc.).

### UI conventions
- **View margins:** the main window's tab content area (`ModernTabControlStyle`) insets every view by 12 px, aligned with the tab bar. Root grids of views use `Style="{StaticResource View.Root}"` (`UI/Styles/Views/WindowStyle.xaml`) instead of their own outer margin: no margin inside a tab, `20,16,20,20` when the same view is opened in its own window. This works via the inherited attached property `ViewHost.IsTabContent`, set by the tab control.
- **Toolbars** (search fields, filters, action buttons above a list) sit in a `Card` border with `Padding="16"`.
- **Layout logic goes into attached properties in `Core/Framework/UI/`, not into code-behind**, e.g. `ClipBehaviors.CornerRadius` (rounded clip that follows the size), `ScrollFade.Length` (fades scrolled content at the top/bottom edge while more content is beyond it), `TileLayout.AutoSize`, `TextBoxBehaviors`.
- **Accent colors** are profile dependent (`FnTheme.ApplyProfileAccentColors`, falls back to the default accent from `UI/Colors/Colors.xaml`). A `{DynamicResource Color.Accent*}` inside a brush defined in a ResourceDictionary does not follow runtime changes (shared resource, resolved once): either reference `Color.Accent*` directly in the element/template, or register the brush in `FnTheme.AccentResources` and reference it with `DynamicResource` only (FnTheme replaces the instance; a `StaticResource` keeps the old, frozen one — this is what made hover outlines show a previous profile's color). Neutral hover/selection surfaces use `Brush.Item.Hover` / `Brush.Item.Selected` (white glass, not accent tinted).
- **Sub windows are not resizable**: `GTNWindowView.ResizeMode` defaults to `NoResize`; give a view a fixed `Width`/`Height` that fits its content. Only a deliberate exception sets `ResizeMode="CanResize"`. Sub windows can't be moved or resized beyond the main window (`UIXWindowBase` in the engine).
- **Context menus** all share one look (the glass `ModernContextMenuStyle`, `UI/Styles/Controls/ContextMenuStyles.xaml`): it is the implicit default for every `ContextMenu` and pins its own TextBlock/MenuItem/Separator styles, so local styles of the hosting control (e.g. the DataGrid's TextBlock style) don't change it. Don't give context menus their own styles.
- **Markdown fields** (long text, type `07`) use `uixmd:UIXMarkdownEditor` from the engine (`FrameworkElements/Markdown/`): a TextBox with Edit/Preview switch, bound with `UIXAttachedProperty.Path` like any TextBox. Pasted, dropped or inserted images are scaled down (`MaxImageWidth`/`MaxImageHeight`, default 800 px) and embedded into the text as reference definitions with a data URI (`[img-1]: data:image/...;base64,...`) at the end. The engine control holds the behavior; its look is the implicit style in `UI/Styles/Controls/MarkdownEditorStyle.xaml`.
- Spacing that must scroll with the content belongs on the items panel (e.g. `WrapPanel Margin`), not on `ListBox.Padding`: the default template applies the padding outside the scrolled area and shrinks the viewport.

### Data layer (SQLite)
Tables use 7-char names (`T1` prefix + 5 chars, e.g. `T1PROFI`, `T1PLTHR`, `T1SESSI`, `T1CTABH/D`, `T1METAH/P`), with a prefix per class role, and columns use 4-char codes. For table `T1SESSI`:
- `TableObjects/T1SESSI.cs` — generated row object (`UIXTableObjectBase`) with change tracking (`HasChanged`, `AcceptChanges`, `State`), uppercase column properties marked `[UIXSignatureField(n)]`.
- `TableObjects/K1SESSI.cs` — generated constants for table and field names; use these with `UIXQuery` instead of string literals.
- `DataManagers/TXSESSIBasic.cs` — generated CRUD (`CreateNew`, `Read`, `Save`, `Delete`, …).
- `DataManagers/TXSESSI.cs` — subclass of `TX…Basic`; the generator only creates an empty template and never overwrites it. It holds **CRUD functions only** (e.g. `TXSCRSP.DeleteAllWhereSSID`).
- `DataManagers/TFSESSI.cs` — hand-written static query/business functions for the table (e.g. `TFPLTHR.GetCurrentPlaythrough`). Anything beyond CRUD goes here, not into `TX…`.

**Never hand-edit `T1…`, `K1…` and `TX…Basic`** (also the metadata tables' classes in `Core/Application/Metadata/Data/`): the developer regenerates them through GTN (Metadata module / code generator). If one of them needs a change, change the metadata or the generator and regenerate.

Schema is metadata-driven: table definitions live in metadata tables `T1METAH` (header) / `T1METAP` (positions), editable in the Metadata module. `MigrationFactory.Metadata.CodeGenerator.GenerateFor(T1METAH)` generates the T1/K1/TXBasic/TX classes; if a file with that name already exists in the solution it is overwritten in place (except `TX*.cs`), otherwise it is written to `%LOCALAPPDATA%/<Publisher>/GameTimeNext/dev/genClass/`.

The metadata tables themselves are not rows in `T1METAH`/`T1METAP`; their schema is hard-coded in `MigrationFactory.Metadata.METADATA`. `CodeGenerator.GenerateForMetadata()` (button "Generate classes of the metadata tables" in the Metadata view) generates their classes from that definition into `Core/Application/Metadata/Data/`, following the same T1/K1/TXBasic/TX pattern. To change a metadata table, edit `METADATA`, then regenerate. Generated `TX*Basic` classes take an optional `SQLiteConnection` in the constructor (used when migrating into another database), and set `CRUS`/`CHUS` on insert/update when the table has those fields.

`MigrationFactory` (`Core/Framework/DataBase/Migration/`, partial class split by concern) handles schema diff/migration from metadata, CSV import/export (`ToCsv`/`FromCsv`), and import packages.

### DevSync (debug only)
For tables marked as dev-synced in metadata (`IsDevSynced`), saving a table object in debug re-exports its CSV into `devsync/<TABLE>.csv`, and on startup `MigrationFactory.FromCsv.MigrateTables(ImportType.DevSync)` re-imports these CSVs. `ImportType.DevSync` and `MetadataGenerator` only run when `FnSystem.IsDebug()` is true (a DEBUG build plus an attached debugger or a running `devenv`). This keeps reference data (codetables, metadata) in sync across developers through git, so the CSVs under `devsync/` are source-controlled data, not throwaway output.

### Startup & versioned migrations
`App.xaml.cs` `OnStartup`: single-instance check → parse start args into `AppEnvironment.StartArguments` → file-system migration → load `AppConfig` → init logging (`FnLog`) and `DataBaseManager` → `AppEnvironment.Initalize()` → start `MainApp`.

`AppEnvironment.Initalize()` order: backup → `MigrationFactory.Metadata.MigrateTables()` (schema from metadata) → startable apps → changelog → import packages → DevSync CSV import (debug only) → `MigrationManager.Migrate()` → seed `T1GROUP`.

Version-specific data migrations are `MigTask_<version>_<seq>` classes (subclassing `MigTaskBase`) registered in order in `MigrationTasks/MigrationManager.Migrate()`.

### Launcher integration
A profile is linked with a launcher by `T1PROFI.LNTY` (launcher type, key of codetable `lT`) and `T1PROFI.LNID` (game ID in that launcher: Steam app ID, file path for `LINK`, ...); launch arguments are `CProfileSettings.LaunchArgs` in `PRSE`. Each launcher is an `ILauncherIntegration` (`Core/Framework/LauncherIntegration/<Launcher>/`, based on `LauncherIntegrationBase`) that returns its codetable key in `LauncherType` and declares its features (`SupportsImport`, `SupportsLaunchArguments`, `SupportsOpenInLauncher`); new launchers are registered in `LauncherRegistry`. Modules only use the facade `FnLauncher` (link/unlink, installed games of all launchers, start with profile settings such as HDR, open in library) and adapt their UI to the features, never to a specific launcher. Running game detection (`GameRunningProcess`) doesn't depend on the launcher, it uses `EXGF`/`EXEC`. `T1PROFI.SAID` is legacy: `MigTask_100b_010` moves it to `LNTY`/`LNID`.

### Runtime
- Background processes: `GameRunningProcess` (polls every 500 ms to detect running game executables and switch profiles) and `GlobalKeyInputProcess` (global hotkeys, e.g. Ctrl+M quick launch). They are stopped on exit, and an auto-backup is created then.

## Repo conventions

- `.claude/settings.json` denies reading or editing `bin/`, `obj/`, and `.github/`, and denies `git push` and `git reset`.
- Rules agreed with a developer (coding rules, conventions, workflow rules) are always added to this file, in English and in the matching section, so every developer's AI assistant works the same way. Private assistant memory is not enough.
- Commits and branches are done by the developers themselves: do not commit, create branches or offer to do so unless explicitly asked.
- Change only what was asked: when one aspect of a UI element is to be changed, keep its other existing parts instead of removing or replacing them; if something really conflicts, adapt the new part around it or mention the conflict and offer the extra change.
- "Style X like Y" means the visual look of Y (surface, glass, colors, borders), not Y's layout: keep X's own layout and content and put it on Y's surface (e.g. the identify overlay keeps the library's content on the toast glass panel).

## Project skills

- `/mark-code` — mark code locations with `MARKEDBYCLAUDE` comments for the Visual Studio Task List.
