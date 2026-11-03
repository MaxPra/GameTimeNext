using GameTimeNext.Core.Application.BigPicture;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.Profiles.Types;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.SteamGridDB;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.Utils;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows.Media;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles
{
    /// <summary>
    /// Automatic import of newly installed launcher games (LauncherAutoImportProcess, settings "Game import").
    /// </summary>
    public partial class CFProfilesApp
    {
        private const char LauncherTypeSeparator = ';';

        // Used if the cover doesn't give three different accent colors (same as the GameTimeX migration)
        private static readonly Color[] FallbackAccentColors = { Colors.SteelBlue, Colors.SlateGray, Colors.LightSteelBlue };

        /// <summary>
        /// Returns the launcher types taking part in the automatic import: all launchers that support an import and
        /// aren't left out, none while the automatic import is switched off.
        /// </summary>
        public static List<string> GetAutoImportLauncherTypes(AppSettings appSettings)
        {
            return GetAutoImportLauncherTypes(appSettings.AutoImportGames, appSettings.AutoImportExcludedLaunchers);
        }

        /// <summary>
        /// Same as GetAutoImportLauncherTypes(AppSettings), for values not saved yet (settings view).
        /// </summary>
        public static List<string> GetAutoImportLauncherTypes(bool autoImportGames, string excludedLaunchers)
        {
            if (!autoImportGames)
                return new List<string>();

            HashSet<string> excluded = SplitLauncherTypes(excludedLaunchers).ToHashSet(StringComparer.Ordinal);

            return FnLauncher.GetImportLauncherTypes().Where(t => !excluded.Contains(t)).ToList();
        }

        /// <summary>
        /// Splits a list of launcher types as stored in the settings (AutoImportExcludedLaunchers).
        /// </summary>
        public static List<string> SplitLauncherTypes(string launcherTypes)
        {
            return launcherTypes.Split(LauncherTypeSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        /// <summary>
        /// Joins launcher types for the settings (AutoImportExcludedLaunchers).
        /// </summary>
        public static string JoinLauncherTypes(IEnumerable<string> launcherTypes)
        {
            return string.Join(LauncherTypeSeparator, launcherTypes);
        }

        /// <summary>
        /// Creates the profile of a launcher game without the edit view, as a new profile with the game linked would
        /// be saved there: name, launcher, game folder with the executables preselected by the executables dialog,
        /// platform PC, cover and accent colors, shrink-wrapped if set and marked with the badge "New".
        /// The cover is the first one of SteamGridDB (if an API key is set), otherwise the placeholder cover; it is
        /// written to the profile covers folder. Doesn't access the database and can run on any thread; the profile
        /// is not saved yet (TFPROFI.SaveNewProfile).
        /// </summary>
        public static async Task<T1PROFI> CreateProfileFromLauncherGameAsync(UIXApplication app, LauncherGame game, CancellationToken cancellationToken)
        {
            T1PROFI t1profi = new TXPROFI().CreateNew();

            t1profi.GANA = game.Name;
            t1profi.PLAFO = "PC";
            t1profi.ETTY = EstimatedTimeTypes.EST_TIME_NONE;

            FnLauncher.LinkGame(t1profi, game);

            if (!FnString.IsNullEmptyOrWhitespace(game.InstallPath))
            {
                t1profi.EXGF = game.InstallPath;
                t1profi.EXEC = GetPreselectedExecutables(game.InstallPath);
            }

            CProfileSettings cProfileSettings = new CProfileSettings
            {
                Sealed = AppEnvironment.GetAppConfig().AppSettings.SealNewGames,
                IsNew = true
            };

            t1profi.PRSE = cProfileSettings.Serialize();

            (string coverFileName, bool isSteamGridDBCover) = await SaveAutoImportCoverAsync(app, t1profi, cancellationToken);

            t1profi.PPFN = coverFileName;

            SetAutoImportAccentColors(app, t1profi, isSteamGridDBCover);

            return t1profi;
        }

        /// <summary>
        /// Shows which games the automatic import added, unless the Big Picture mode is open (the toast would pop up
        /// in front of the game). Call on the UI thread.
        /// </summary>
        public static void ShowAutoImportToast(UIXApplication app, List<string> gameNames)
        {
            if (gameNames.Count == 0 || CFBigPictureApp.IsOpen)
                return;

            string title = gameNames.Count == 1
                ? app.GetErrorMessage("PRO0010")
                : app.GetErrorMessage("PRO0011", gameNames.Count);

            new ToastMessage(title, string.Join("\n", gameNames)).Show();
        }

        /// <summary>
        /// Executables as the executables dialog preselects them for the game folder (all found, non-game ones sorted out).
        /// </summary>
        private static string GetPreselectedExecutables(string gameFolder)
        {
            CExecutables cExecutables = new CExecutables();

            foreach (Executable executable in FnSystem.SortOutExecutables(FnSystem.FindExecutables(gameFolder)))
                cExecutables.KeyValuePairs.TryAdd(executable.Name, executable.IsSelected);

            return cExecutables.Serialize();
        }

        /// <summary>
        /// Saves the cover of the new profile in the profile covers folder. Returns its file name and whether it came
        /// from SteamGridDB (false = placeholder cover).
        /// </summary>
        private static async Task<(string fileName, bool isSteamGridDBCover)> SaveAutoImportCoverAsync(UIXApplication app, T1PROFI t1profi, CancellationToken cancellationToken)
        {
            string fileName = CFProfilesEditApp.GetGUIDCoverName("jpg");
            string path = Path.Join(AppConfig.Storage.ProfileCoversDirectoryPath, fileName);

            string apiKey = AppEnvironment.GetAppConfig().AppSettings.SteamGridDbKey.Trim();

            if (!FnString.IsNullEmptyOrWhitespace(apiKey))
            {
                try
                {
                    SteamGridDBClient client = new SteamGridDBClient(apiKey);
                    string coverUrl = await FindFirstSteamGridDBCoverAsync(client, t1profi, cancellationToken);

                    if (!FnString.IsNullEmptyOrWhitespace(coverUrl))
                    {
                        await client.DownloadCoverAsync(coverUrl, path, cancellationToken);
                        return (fileName, true);
                    }

                    FnLog.AddInfo(app, $"SteamGridDB has no cover for {t1profi.GANA}, the placeholder cover is used");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    FnLog.AddWarning(app, $"The cover of {t1profi.GANA} could not be loaded from SteamGridDB, the placeholder cover is used", ex);
                }
            }

            using (Stream placeholder = OpenPlaceholderCoverStream())
                CFProfilesEditApp.SaveProfileCover(FnImage.Load(placeholder), path);

            return (fileName, false);
        }

        /// <summary>
        /// URL of the first cover of SteamGridDB: by Steam app ID for Steam games (if SteamGridDB has none there, by
        /// name), by name for all others. Empty if there is none.
        /// </summary>
        private static async Task<string> FindFirstSteamGridDBCoverAsync(SteamGridDBClient client, T1PROFI t1profi, CancellationToken cancellationToken)
        {
            long steamAppId = FnLauncher.GetSteamAppId(t1profi);

            IReadOnlyList<SgdbGrid>? grids = null;

            if (steamAppId != 0)
            {
                try
                {
                    grids = await client.GetGridsBySteamAppIdAsync((int)steamAppId, dimensions: "600x900", cancellationToken: cancellationToken);
                }
                catch (HttpRequestException)
                {
                    // Unknown app ID (404): searched by name below
                }
            }

            if (grids == null || !grids.Any(g => !FnString.IsNullEmptyOrWhitespace(g.Url)))
                grids = await client.GetGridsByNameAsync(t1profi.GANA, dimensions: "600x900", cancellationToken: cancellationToken);

            return grids?.FirstOrDefault(g => !FnString.IsNullEmptyOrWhitespace(g.Url))?.Url ?? string.Empty;
        }

        /// <summary>
        /// Sets the accent colors determined from the cover (ACIN, offered in the edit view). A SteamGridDB cover also
        /// uses its first color as profile accent (as the GameTimeX migration does); the placeholder cover doesn't.
        /// </summary>
        private static void SetAutoImportAccentColors(UIXApplication app, T1PROFI t1profi, bool useAccent)
        {
            List<Color> colors = new List<Color>();

            try
            {
                colors = FnImage.GetTopAccentColors(Path.Join(AppConfig.Storage.ProfileCoversDirectoryPath, t1profi.PPFN), 3);
            }
            catch (Exception ex)
            {
                FnLog.AddWarning(app, $"The accent colors of {t1profi.GANA} could not be determined, default colors are used", ex);
            }

            // Three different, opaque colors: the edit view expects exactly three entries
            List<string> accentColors = colors
                .Concat(FallbackAccentColors)
                .Select(c => Color.FromArgb(255, c.R, c.G, c.B).ToString(CultureInfo.InvariantCulture))
                .Distinct(StringComparer.Ordinal)
                .Take(3)
                .ToList();

            Dictionary<string, bool> accentColorsInit = new Dictionary<string, bool>();

            for (int i = 0; i < accentColors.Count; i++)
                accentColorsInit.Add(accentColors[i], useAccent && i == 0);

            t1profi.ACIN = new CAccentColorsInit { AccentColors = accentColorsInit }.Serialize();
            t1profi.ACAC = useAccent;

            if (!useAccent)
                return;

            string[] accentStateColors = FnTheme.CalculateAccentStateColors(accentColors[0]);

            t1profi.ACCO = new CAccentColors
            {
                AccentColors = new Dictionary<string, string>
                {
                    { "accent", accentStateColors[0] },
                    { "hover", accentStateColors[1] },
                    { "pressed", accentStateColors[2] }
                }
            }.Serialize();
        }
    }
}
