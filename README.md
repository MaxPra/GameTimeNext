<div align="center">

# 🎮 arcora

### Your all-in-one companion for gaming — track, launch, and organize your playtime

*Formerly GameTimeNext · Successor to [GameTimeX](https://github.com/MaxPra/GameTimeX)*

<br>

<br>
<br>

![Status](https://img.shields.io/badge/status-stable-brightgreen?style=for-the-badge)
![Version](https://img.shields.io/badge/version-1.0.0-blue?style=for-the-badge)
![Platform](https://img.shields.io/badge/platform-Windows-informational?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-WPF-purple?style=for-the-badge)

<br>

> 🎉 **arcora 1.0.0 is here** — a fresh look, a Big Picture mode for the TV, playtime tracking from your phone and much more. See [what's new](#-whats-new-in-v100).

<br>

### ⬇️ [Download Installer (v1.0.0)](https://github.com/MaxPra/GameTimeNext/releases/download/v1.0.0/arcora_v1.0.0_Installer.exe)

<br>

[Overview](#-overview) •
[Applications](#-applications) •
[Features](#-features) •
[Setup](#%EF%B8%8F-setup) •
[Usage](#-usage) •
[What's New](#-whats-new-in-v100) •
[Migration](#-migration) •
[Feedback](#-feedback)

</div>

<br>

---

## 📌 Overview

**arcora** is more than a playtime tracker — it's built to be the central hub for your gaming library. Launch your games directly from the app, track playtime with precision, and keep every playthrough organized in one clean interface.

| | Feature | Description |
|:---:|---|---|
| 🚀 | **Launcher** | Start and manage your games directly from arcora |
| ⏱️ | **Tracking** | Manual, precise playtime tracking |
| 🤖 | **Automation** | Automatic game detection & profile switching |
| 🔁 | **Playthroughs** | Structured playthrough management |
| 📺 | **Big Picture** | Full screen library for the TV, operated with a game controller |
| 📱 | **Remote Monitoring** | Track the playtime of your console games from your phone |
| 🖥️ | **Screensetups** | Switch monitors and audio device with one click |
| ⚙️ | **Parametrization** | Fully customizable via selection lists |
| 🌑 | **Screen Care** | OLED-friendly screen blackout |

<br>

---

## 🧩 Applications

arcora is organized into dedicated modules, each accessible from within the app:

| Module | Purpose |
|---|---|
| **Dashboard** | Central overview and statistics |
| **Profiles** | Create and manage game profiles |
| **Playthroughs** | Create, edit, pause, complete and cancel playthroughs |
| **Big Picture** | Full screen library for the TV, operated with a game controller, the keyboard or the mouse |
| **Screensetups** | Saved monitor and audio setups, e.g. *desk with two monitors* or *TV only* |
| **Selection Lists** | Extend and customize dropdown values (e.g. platforms, tags) |
| **Settings** | Configure API keys, monitors, and app behavior — sorted into pages and searchable |

<br>

---

## ✨ Features

<table>
<tr>
<td width="50%" valign="top">

### ⏱️ Tracking
- Manual start/stop system — no automatic time tracking, full control over sessions
- Precise session control
- Query currently tracked time since start
- Automatic game detection with automatic profile switching

### 🔁 Playthroughs
- Multiple playthroughs per profile — replay a game and track each run separately
- Total playtime aggregated across all playthroughs per profile

### 🖼️ Visuals
- SteamGridDB integration — select cover art directly from SteamGridDB when creating a profile
- Local image support via built-in cropping tool
- Cover previews across the app

### 📊 Analytics
- Statistics dashboard

### 📺 Big Picture Mode
- Your library in full screen for the TV, operated with a game controller, the keyboard or the mouse
- Recently played games, the whole library and the search; start games and the monitoring
- <kbd>Xbox</kbd> button or <kbd>View</kbd> + <kbd>Menu</kbd> switches between the game and Big Picture — the game is paused meanwhile

### 📥 Automatic Game Import
- Games installed with **Steam, Epic, Xbox, GOG, Ubisoft Connect or Rockstar** are added as profiles by themselves, with a cover

</td>
<td width="50%" valign="top">

### 📈 Overview & Progress
- Total playtime per profile
- Archived profiles
- Daily tracking
- Estimated completion progress bar based on HowLongToBeat or Twitch data

### ⚙️ Parametrization
- **Selection Lists** module to extend selectable values (e.g. platforms, tags) used throughout the app
- Fully adaptable dropdowns and categories without code changes

### 🖥️ Screen Blackout (OLED Care)
- Blackout secondary monitors while tracking — ideal for OLED setups
- Full blackout of all screens, e.g. while AFK, to protect OLED panels

### 🔗 Playtime Data Integration
- **HowLongToBeat** — manual lookup and linking of expected completion time
- **Twitch** — automatic retrieval of game completion data
- Displays an estimated completion percentage on the selected profile

### 📱 Remote Monitoring
- Pick a game on your phone and start or stop the monitoring — ideal for consoles in another room
- All screens of the PC go black meanwhile

</td>
</tr>
</table>

<br>

---

## 🛠️ Setup

### SteamGridDB Integration

To enable cover artwork:

1. Open **Settings**
2. Enter your **SteamGridDB API Key**
3. Save

> [!NOTE]
> Without an API key, artwork features are disabled. Local images remain available via the cropping tool.

### Remote Monitoring

1. Open **Settings → Monitoring** and switch on **Remote Monitoring**
2. Scan the **QR code** with your phone

> [!NOTE]
> Your home network must be set to *Private* in Windows.

<br>

---

## 🎯 Usage

| Action | How |
|---|---|
| **Quick Launch Menu** | Press <kbd>Ctrl</kbd> + <kbd>M</kbd> anywhere in the app to start any configured application instantly |
| **Find modules** | Use **Search Application** and open directly from the search results |
| **Add to Favorites** | Right-click an application → *Add to Favorites* |
| **Set start application** | Right-click an application → *Set as Primary Start Application* |

<br>

---

## 🆕 What's New in v1.0.0

| Area | What's new |
|---|---|
| **Name** | GameTimeNext is now **arcora** — new icon, splash screen and animations |
| **Big Picture** | Full screen mode for the TV, operated with a game controller |
| **Remote Monitoring** | Track the playtime of your console games from your phone |
| **Screensetups** | Switch monitors and audio device with one click |
| **Game Import** | Automatic import from Steam, Epic, Xbox, GOG, Ubisoft Connect and Rockstar |
| **Playthroughs** | New application to create, edit, pause, complete and cancel playthroughs |
| **Tags** | Own tags in **Selection Lists**, shown as cards in the profile |
| **Usability** | Built-in help system, settings sorted into pages and searchable |
| **Personalization** | Local game rating system, dynamic background, disc image |
| **Installation** | Installed per user without administrator rights; a previous installation is removed and its data taken over automatically |

> [!IMPORTANT]
> **Choose a platform for every profile.** The platform is now a required field. No platforms to choose from? Open **Selection Lists** and add entries to **Platforms**.

<br>

---

## 📦 Migration

- Automatic detection of existing GameTimeX installation
- Guided migration process
- Automatic data import

<br>

---

## 🧰 Tech Stack

| Category | Technology |
|---|---|
| **Framework** | .NET / WPF |
| **Storage** | Local data storage |
| **External APIs** | SteamGridDB, HowLongToBeat, Twitch |

<sub>Development is assisted by AI tools. Most of the code is written by hand, and AI-generated code is reviewed before it is merged.</sub>

<br>

---

## 💬 Feedback

Found an issue or have a suggestion? [Open an issue on GitHub](https://github.com/MaxPra/GameTimeNext/issues).

<div align="center">
<br>

Made with ❤️ by **Cryloud-Studios**

</div>
