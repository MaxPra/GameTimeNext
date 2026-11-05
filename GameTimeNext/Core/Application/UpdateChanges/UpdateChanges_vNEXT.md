# GameTimeNext 1.0.0

> [!IMPORTANT]
> Please iterate through every profile you've added and choose a platform (this is now a required field).
> If you don't have any platforms available, then open the application **Codetables** and add entries to **iM - Images / Symbols** and **pF - Platforms**.

- GameTimeNext is now installed per user (no administrator rights required).
  Previous installations are removed automatically and your data is migrated.

## Improvements

- Cleaner visual style, new app icons, new app bar animations and a new splash screen.
- New application icon (desktop, taskbar, start menu and installer).
- Improved the dashboard.
- Improved performance of the running game detection.
- The application can now only be started once (single instance).
- IGDB integration can now be disabled in the settings.
- The playthrough overview now shows the total playtime of each playthrough.
- Reworked the library filters.

## New features

- **Session Cleanup** lets you clean out sessions that took less than a specified amount of time.
  This is useful for cleaning out sessions that were created by accident or by testing.
- Added a new option to override the global blackout setting (side monitors while monitoring) on profile layer.
- Added a feature that displays an animated particle clock when all monitors are blacked out.
- Added a new **Playthroughs** application: playthroughs can be opened from a profile and created, edited, completed and cancelled there.
- Playthroughs can now be paused and unpaused.
- Added a new **External** filter for profiles.
- Tags are now managed in **Selection Lists** (list *Tags*): add your own tags and hide the delivered ones you don't use. Your existing tags, assignments and filters are migrated automatically.
- Tags of a game are now shown as cards in the profile editor: add a tag from the list (or create a new one right there) and remove it with the cross on its card.
- The **Settings** are reorganized into the pages *General*, *Appearance*, *Integrations*, *Backup* and *Monitoring*, and have a search field that finds a setting by its name or description.
- Full blackout can now be toggled via hotkey while monitoring a console game.
- Added the option **Dynamic Background** (*Settings > Appearance*): a blurred image is used as background of the main window.
  The background source can be chosen: the current Windows wallpaper or the cover of the selected profile.
- Added a new **Screensetups** application: save which monitors are switched on, which one is the primary monitor, the resolution, refresh rate, orientation, placement and scaling of each monitor and which audio device is used, and switch between them with one click.
- Added the **Big Picture mode** for the TV: your library in full screen with large covers, operated with a game controller (Xbox / XInput), the keyboard or the mouse.
  Open it with the controller icon in the title bar, or start GameTimeNext directly in it with the start argument `--bigpicture`.
  The selected game is shown large with its playtimes and current playthrough; LB / RB switch between your recently played games, the whole library and the search.
  Search your library with the on-screen keyboard (Y), start or stop monitoring and quit games from the game options (X), black out all monitors (View), and switch between a running game and Big Picture with the Xbox button or View + Menu.
- **Automatic game import** (*Settings > General > Game import*): newly installed games of your launchers (Steam, Epic, Xbox, GOG, Ubisoft Connect, Rockstar) are added as profiles shortly after their installation finished, with the first cover from SteamGridDB (or the placeholder cover) and the executables preselected.
  When you switch it on, you decide once whether the games installed already are added too. New profiles show the badge **New** until you select them (shrink-wrapped games until you unwrap them). Games that already have a profile, or whose profile you deleted, are never added again.

## Bug fixes

- **Launch game** is only enabled when the game can really be started (its executable exists in the game folder).

- Fixed a bug where timestamps were not being updated correctly when data was saved.
- Fixed a bug regarding detection of currently running games, where the application was not able to detect some games that were running in the background.
- Fixed a bug regarding the automatic switching of the selected application (from menu).
- Fixed a crash when reading the path of certain game processes (e.g. WARDOGS).
- Fixed start arguments being lost when the application restarts.
- Fixed choosing a cover image from disk.
- Fixed the profile filters and the playthrough type selection in the playthrough overview.
