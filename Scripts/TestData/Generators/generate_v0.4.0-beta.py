"""
Generates the mock data set "v0.4.0-beta" for migration tests (Scripts/TestData/v0.4.0-beta).

Source: v0.4.0-beta_empty/ holds the database and config file exactly as the released 0.4.0 beta
(tag GameTimeNext_v0.4.0beta, engine 6a4103f) created them on its first start, including the delivered
codetables, metadata and groups of its import package. This script only adds user data with the
formats of 0.4.0 (see its TX*Basic.ToDbValue, CProfileSettings, CExecutables, CAccentColors).

Output layout (copied over the user folders by Reset-TestInstall.ps1):
    v0.4.0-beta/Documents/GameTimeNXT/AppConfig.gtnconf
    v0.4.0-beta/Documents/GameTimeNXT/Data/GameTimeNextDb.db
    v0.4.0-beta/Documents/GameTimeNXT/Data/profile_covers/*.png

Run: python -I Scripts/TestData/Generators/generate_v0.4.0-beta.py
The data is deterministic (fixed seed), so a rerun gives the same content.
"""

import base64
import json
import random
import shutil
import sqlite3
import struct
import zlib
from datetime import datetime, timedelta
from pathlib import Path

HERE = Path(__file__).resolve().parent
SOURCE = HERE / "v0.4.0-beta_empty"
TARGET = HERE.parent / "v0.4.0-beta" / "Documents" / "GameTimeNXT"

MIN_DATE = "0001-01-01 00:00:00"  # DateTime.MinValue as written by 0.4.0

# Playthrough types of 0.4.0 (PlaythroughType)
INITIAL = "GTN.INITIAL_PLAYTHROUGH"
NEW = "GTN.NEW_PLAYTHROUGH"
DLC = "GTN.DLC"

# Estimated time types of 0.4.0 (EstimatedTimeTypes)
EST_NONE = "GTN.EST_TIME.NONE"
EST_MAIN = "GTN.EST_TIME.MAIN"
EST_MAIN_EXTRA = "GTN.EST_TIME.MAIN_EXTRA"
EST_COMPLETIONIST = "GTN.EST_TIME.COMPLETIONIST"

rng = random.Random(40)


def fmt(dt):
    return dt.strftime("%Y-%m-%d %H:%M:%S")


def profile_settings(hdr=False, steam_args=""):
    # CProfileSettings.Serialize (System.Text.Json); spaces in the Steam arguments were stored as ";"
    return json.dumps({"HDREnabled": hdr, "SteamGameArgs": steam_args}, separators=(",", ":"))


def executables(pairs):
    return json.dumps({"KeyValuePairs": pairs}, separators=(",", ":"))


def accent_colors(accent, hover, pressed):
    return json.dumps({"AccentColors": {"accent": accent, "hover": hover, "pressed": pressed}}, separators=(",", ":"))


def accent_colors_init(colors, checked_index):
    return json.dumps({"AccentColors": {c: i == checked_index for i, c in enumerate(colors)}}, separators=(",", ":"))


def cover_name(index):
    # 0.4.0 names covers by a URL-safe base64 GUID (22 characters) plus the file ending
    raw = random.Random(1000 + index).getrandbits(128).to_bytes(16, "little")
    return base64.b64encode(raw).decode().replace("/", "_").replace("+", "-").rstrip("=") + ".png"


def write_cover(path, top, bottom, width=600, height=900):
    """Writes a vertical two-color gradient as PNG (no third-party packages needed)."""
    rows = bytearray()
    for y in range(height):
        t = y / (height - 1)
        pixel = bytes(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        rows += b"\x00" + pixel * width

    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(rows), 9))
    png += chunk(b"IEND", b"")
    path.write_bytes(png)


# Tags of the empty 0.4.0 database: GRID 6..27 (GTN.TAG), conditions 1..5 are computed and never linked
TAG = {"Singleplayer": 6, "Multiplayer": 7, "Co-op": 8, "PvP": 9, "Action": 10, "Adventure": 11, "RPG": 12,
       "Strategy": 13, "Simulation": 14, "Shooter": 15, "Horror": 16, "Survival": 17, "Open World": 18,
       "Sandbox": 19, "Story Rich": 20, "Exploration": 21, "Crafting": 22, "Building": 23, "First Person": 24,
       "Third Person": 25, "Isometric": 26, "Top-Down": 27}

# Each profile covers a case of the migration to 1.0 (see "case").
PROFILES = [
    dict(name="Hollow Peaks", case="Steam with launch arguments (SAID + SteamGameArgs -> LNTY/LNID + LaunchArgs), HDR, accent colors",
         said=1145360, prse=profile_settings(True, "-novid;-high;-windowed"), accent=True,
         playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "completed"), (NEW, "Playthrough #2", "active")],
         sessions=40, tags=["Singleplayer", "Action", "Exploration"], est=(EST_MAIN, 22.5, 35.0, 60.0, False)),
    dict(name="Ember Tactics", case="Steam without arguments, estimated time main",
         said=1066780, prse=profile_settings(), playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "active")],
         sessions=25, tags=["Strategy", "Isometric"], est=(EST_MAIN, 30.0, 45.0, 80.0, False)),
    dict(name="Starfall Drift", case="external game folder + executables, no Steam link",
         exgf=r"C:\Games\Starfall Drift", exec_={"StarfallDrift.exe": True, "CrashReporter.exe": False},
         prse=profile_settings(), playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "active")],
         sessions=18, tags=["Simulation", "Open World", "Sandbox"], est=(EST_NONE, 0, 0, 0, False)),
    dict(name="Quiet Harbor", case="never played (FIPL/LAPL = MinValue, no sessions)",
         said=2379780, prse=profile_settings(), playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "active")],
         sessions=0, tags=["Adventure"], est=(EST_NONE, 0, 0, 0, False)),
    dict(name="Iron Lanterns", case="initial playthrough completed, no active playthrough (asks for a new one)",
         said=1593500, prse=profile_settings(), playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "completed")],
         sessions=30, tags=["Third Person", "Story Rich", "Action"], est=(EST_COMPLETIONIST, 20.0, 28.0, 40.0, False)),
    dict(name="Moss & Rust", case="initial playthrough canceled, no active playthrough",
         prse=profile_settings(), exgf=r"D:\Games\MossAndRust", exec_={"MossRust.exe": True},
         playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "canceled")],
         sessions=6, tags=["Survival", "Crafting"], est=(EST_NONE, 0, 0, 0, False)),
    dict(name="Cinder Run", case="archived profile",
         said=774361, prse=profile_settings(), archived=True,
         playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "completed")],
         sessions=12, tags=["Shooter", "First Person", "PvP", "Multiplayer"], est=(EST_NONE, 0, 0, 0, False)),
    dict(name="Deepwater Echo", case="initial completed + active DLC playthrough",
         said=1091500, prse=profile_settings(steam_args="-dx12"),
         playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "completed"), (DLC, "Abyssal Tides DLC", "active")],
         sessions=35, tags=["RPG", "Open World", "Story Rich"], est=(EST_MAIN_EXTRA, 40.0, 70.0, 120.0, False)),
    dict(name="Pixel Orchard", case="cover file name set but the file is missing (placeholder fallback)",
         said=413150, prse=profile_settings(), missing_cover=True,
         playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "active")],
         sessions=50, tags=["Simulation", "Co-op", "Top-Down"], est=(EST_NONE, 0, 0, 0, False)),
    dict(name="Glass Meridian", case="no cover at all (empty PPFN)",
         prse=profile_settings(), no_cover=True, playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "active")],
         sessions=8, tags=["Adventure", "Horror"], est=(EST_NONE, 0, 0, 0, False)),
    dict(name="Northbound", case="several new playthroughs, the newest active",
         said=1245620, prse=profile_settings(),
         playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "completed"), (NEW, "Playthrough #2", "completed"),
                       (NEW, "Playthrough #3", "active")],
         sessions=60, tags=["RPG", "Third Person", "Roguelike"], est=(EST_MAIN, 55.0, 90.0, 140.0, False)),
    dict(name="Signal Lost", case="empty PRSE / EXEC, manual estimated time, user tag",
         prse="", playthroughs=[(INITIAL, "Playthrough #1 (Initial)", "active")],
         sessions=10, tags=["Roguelike", "Singleplayer"], est=(EST_MAIN, 12.0, 0, 0, True)),
]

USER_TAG = "Roguelike"  # created by the user in 0.4.0 -> becomes a "U_" entry in 1.0

ACCENTS = [("#3A7BD5", "#5A94DE", "#2E62AA"), ("#D5793A", "#DE925A", "#AA602E"), ("#7B3AD5", "#945ADE", "#622EAA")]

FIRST_DAY = datetime(2026, 1, 5)
LAST_DAY = datetime(2026, 6, 20)  # the day before the 0.4.0 release


def main():
    if TARGET.exists():
        shutil.rmtree(TARGET)
    covers = TARGET / "Data" / "profile_covers"
    (covers / "temp_covers").mkdir(parents=True)
    shutil.copy2(SOURCE / "AppConfig.gtnconf", TARGET / "AppConfig.gtnconf")
    db_path = TARGET / "Data" / "GameTimeNextDb.db"
    shutil.copy2(SOURCE / "GameTimeNextDb.db", db_path)

    con = sqlite3.connect(db_path)
    cur = con.cursor()
    now = fmt(LAST_DAY)

    cur.execute("INSERT INTO T1GROUP (GRNA, GTYP, CRAT, CHAT) VALUES (?, 'GTN.TAG', ?, ?)", (USER_TAG, now, now))
    tags = dict(TAG, **{USER_TAG: cur.lastrowid})

    for index, p in enumerate(PROFILES):
        # Cover
        ppfn = ""
        if not p.get("no_cover"):
            ppfn = cover_name(index)
            if not p.get("missing_cover"):
                top = tuple(rng.randrange(40, 200) for _ in range(3))
                write_cover(covers / ppfn, top, tuple(c // 4 for c in top))

        accent, acco, acin = False, "", ""
        if p.get("accent"):
            accent = True
            acco = accent_colors(*ACCENTS[0])
            acin = accent_colors_init([a[0] for a in ACCENTS], 0)

        ettype, etma, etme, etco, etml = p["est"]
        created = FIRST_DAY - timedelta(days=rng.randrange(1, 30))
        cur.execute(
            "INSERT INTO T1PROFI (GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, CRAT, CHAT, ACCO, ACIN, ACAC, CUPT, "
            "ETMA, ETME, ETCO, ETTY, ETML, ARCH) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 0, ?, ?, ?, ?, ?, ?)",
            (p["name"], MIN_DATE, MIN_DATE, ppfn, p.get("exgf", ""), p.get("said", 0), p["prse"],
             executables(p["exec_"]) if p.get("exec_") else "", fmt(created), fmt(created), acco, acin,
             int(accent), etma, etme, etco, ettype, int(etml), int(p.get("archived", False))))
        pfid = cur.lastrowid

        # Playthroughs, sessions spread over them in their order
        ptids = []
        for ptty, ptde, state in p["playthroughs"]:
            cur.execute("INSERT INTO T1PLTHR (PFID, PTTY, PTDE, PTCO, PTCA, CRAT, CHAT) VALUES (?, ?, ?, ?, ?, ?, ?)",
                        (pfid, ptty, ptde, int(state == "completed"), int(state == "canceled"), fmt(created), fmt(created)))
            ptids.append(cur.lastrowid)

        starts = sorted(FIRST_DAY + timedelta(minutes=rng.randrange(0, int((LAST_DAY - FIRST_DAY).total_seconds() // 60)))
                        for _ in range(p["sessions"]))
        for n, start in enumerate(starts):
            minutes = round(rng.uniform(12, 240), 2)
            end = start + timedelta(minutes=minutes)
            ptid = ptids[min(n * len(ptids) // max(len(starts), 1), len(ptids) - 1)]
            cur.execute("INSERT INTO T1SESSI (PFID, PTID, PLFR, PLTO, PLTI, CRAT, CHAT) VALUES (?, ?, ?, ?, ?, ?, ?)",
                        (pfid, ptid, fmt(start), fmt(end), minutes, fmt(end), fmt(end)))
        if starts:
            last_end = max(s for s in starts) + timedelta(minutes=1)
            cur.execute("UPDATE T1PROFI SET FIPL = ?, LAPL = ?, CHAT = ? WHERE PFID = ?",
                        (fmt(starts[0]), fmt(last_end), fmt(last_end), pfid))

        for tag in p["tags"]:
            cur.execute("INSERT INTO T1GRPPO (GRID, PFID, CRAT, CHAT) VALUES (?, ?, ?, ?)", (tags[tag], pfid, now, now))

    con.commit()
    con.execute("VACUUM")
    con.close()

    print(f"Written to {TARGET}")
    for p in PROFILES:
        print(f"  {p['name']:<16} {p['case']}")


if __name__ == "__main__":
    main()
