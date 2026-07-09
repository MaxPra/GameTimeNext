<div align="center">

# GameTimeNext

### Your all-in-one companion for gaming — track, launch, and organize your playtime

*Successor to GameTimeX*

<br>

<img width="100%" alt="GameTimeNext Banner" src="https://github.com/user-attachments/assets/9de07a22-b098-499a-8755-e5c1e5ce13f0" />

<br>
<br>

![Status](https://img.shields.io/badge/status-beta-orange?style=for-the-badge)
![Version](https://img.shields.io/badge/version-0.4.0-blue?style=for-the-badge)
![Platform](https://img.shields.io/badge/platform-Windows-informational?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-WPF-purple?style=for-the-badge)

</div>

<br>

## Download

<div align="center">

### [Download Installer (v0.4.0 Beta)](https://github.com/MaxPra/GameTimeNext/releases/download/GameTimeNext_v0.4.0beta/GameTimeNext_v0.4.0-beta_Installer.exe)

</div>

<br>

## Overview

**GameTimeNext** is more than a playtime tracker — it's built to be the central hub for your gaming library. Launch your games directly from the app, track playtime with precision, and keep every playthrough organized in one clean interface.

| | |
|---|---|
| Launcher | Start and manage your games directly from GameTimeNext |
| Tracking | Manual, precise playtime tracking |
| Automation | Automatic game detection & profile switching |
| Playthroughs | Structured playthrough management |
| Parametrization | Fully customizable via code tables |
| Screen Care | OLED-friendly screen blackout |

> Original project: [GameTimeX](https://github.com/MaxPra/GameTimeX)

<br>

## Applications

GameTimeNext is organized into dedicated modules, each accessible from within the app:

| Module | Purpose |
|---|---|
| **Dashboard** | Central overview and statistics |
| **Profiles** | Create and manage game profiles |
| **Playthroughs** | Manage individual playthroughs per profile |
| **Codetables** | Extend and customize dropdown values (e.g. platforms) |
| **Settings** | Configure API keys, monitors, and app behavior |

<br>

## Features

<table>
<tr>
<td width="50%" valign="top">

### Tracking
- Manual start/stop system — no automatic time tracking, full control over sessions
- Precise session control
- Query currently tracked time since start
- Automatic game detection with automatic profile switching

### Playthroughs
- Multiple playthroughs per profile — replay a game and track each run separately
- Total playtime aggregated across all playthroughs per profile

</td>
<td width="50%" valign="top">

### Overview & Progress
- Total playtime per profile
- Archived profiles
- Daily tracking
- Estimated completion progress bar based on HowLongToBeat or Twitch data

### Parametrization
- **Codetables** module to extend selectable values (e.g. platforms) used throughout the app
- Fully adaptable dropdowns and categories without code changes

</td>
</tr>
</table>

### Visuals
- SteamGridDB integration — select cover art directly from SteamGridDB when creating a profile
- Local image support via built-in cropping tool
- Cover previews across the app

### Screen Blackout (OLED Care)
- Blackout secondary monitors while tracking — ideal for OLED setups
- Full blackout of all screens, e.g. while AFK, to protect OLED panels

### Playtime Data Integration
- **HowLongToBeat** — manual lookup and linking of expected completion time
- **Twitch** — automatic retrieval of game completion data
- Displays an estimated completion percentage on the selected profile

### Analytics
- Statistics dashboard

<br>

## Setup

### SteamGridDB Integration

To enable cover artwork:

1. Open **Settings**
2. Enter your **SteamGridDB API Key**
3. Save

> Without an API key, artwork features are disabled in the current beta. Local images remain available via the cropping tool.

<br>

## Usage

### Quick Launch Menu

Press **Ctrl + M** anywhere in the app to open the quick launch menu — from here you can start any of your configured applications instantly, without navigating through the UI.

### Navigation

- Use **Search Application** to find modules
- Open directly from search results

### Startup Configuration

Right-click any application:

- Add to Favorites
- Set as Primary Start Application

<br>

## Roadmap — v1.0.0

| Area | Planned Features |
|---|---|
| **Playthroughs** | Full playthrough management application — rename, complete, and organize playthroughs directly (playthroughs already existed in earlier versions; full management UI arrives in v1.0.0) |
| **Remote Monitoring** | Manage GameTimeMonitoring remotely via phone — starts a local server, accessible via a website within your home network |
| **UI/UX** | New UI refinements for a cleaner, more modern look |
| **Platforms** | Epic Games integration, additional launcher support |
| **Usability** | Built-in help system |
| **Personalization** | Local game rating system |

<br>

## Migration

- Automatic detection of existing GameTimeX installation
- Guided migration process
- Automatic data import

<br>

## Beta Notice

> This release is in beta. Features may change and minor issues are possible. Feedback is always welcome!

<br>

## Tech Stack

- **.NET / WPF**
- Local data storage
- External APIs: **SteamGridDB**, **HowLongToBeat**, **Twitch**

<br>

## Feedback

Found an issue or have a suggestion? [Open an issue on GitHub](https://github.com/MaxPra/GameTimeNext/issues).

<div align="center">
<br>

Made with ❤️ by Cryloud-Studios

</div>
