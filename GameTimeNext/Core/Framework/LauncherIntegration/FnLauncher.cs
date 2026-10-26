using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Globalization;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    /// <summary>
    /// Launcher functions for a profile (T1PROFI.LNTY = launcher type, T1PROFI.LNID = game ID of the launcher).
    /// </summary>
    public static class FnLauncher
    {
        /// <summary>
        /// Returns the integration of the profile's launcher, null if the profile has none.
        /// </summary>
        public static ILauncherIntegration? GetLauncher(T1PROFI t1profi)
        {
            return LauncherRegistry.Get(t1profi.LNTY);
        }

        /// <summary>
        /// Returns the description of the launcher type from the codetable "lT".
        /// </summary>
        public static string GetLauncherName(string launcherType)
        {
            return TFCTABD.GetDescription("lT", launcherType);
        }

        public static bool IsLinked(T1PROFI t1profi)
        {
            return GetLauncher(t1profi) != null && !FnString.IsNullEmptyOrWhitespace(t1profi.LNID);
        }

        public static bool CanLaunch(T1PROFI t1profi)
        {
            return IsLinked(t1profi) && TFPROFI.HasExecutables(t1profi);
        }

        /// <summary>
        /// Returns the Steam app ID of the profile, 0 if it isn't linked with Steam.
        /// </summary>
        public static long GetSteamAppId(T1PROFI t1profi)
        {
            if (t1profi.LNTY != "STEAM")
                return 0;

            return long.TryParse(t1profi.LNID, NumberStyles.None, CultureInfo.InvariantCulture, out long appId) ? appId : 0;
        }

        /// <summary>
        /// Links the profile with the game. If the game has its own folder, the executables are reset:
        /// they belong to the previous game folder.
        /// </summary>
        public static void LinkGame(T1PROFI t1profi, LauncherGame game)
        {
            t1profi.LNTY = game.LauncherType;
            t1profi.LNID = game.GameId;

            if (!FnString.IsNullEmptyOrWhitespace(game.InstallPath))
                t1profi.EXEC = string.Empty;
        }

        public static void Unlink(T1PROFI t1profi)
        {
            t1profi.LNTY = string.Empty;
            t1profi.LNID = string.Empty;
        }

        /// <summary>
        /// Returns the installed games of all launchers that support an import, sorted by name.
        /// </summary>
        public static List<LauncherGame> GetInstalledGames()
        {
            return LauncherRegistry.GetAll()
                .Where(l => l.SupportsImport)
                .SelectMany(GetInstalledGamesSafe)
                .OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Applies the profile settings (e.g. HDR) and starts the game through its launcher.
        /// </summary>
        public static async void StartGame(T1PROFI t1profi, UIXApplication app)
        {
            ILauncherIntegration? launcher = GetLauncher(t1profi);

            if (launcher == null)
                return;

            if (!launcher.IsGameInstalled(t1profi.LNID))
            {
                CFMBOX cfmbox = app.GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
                cfmbox.Show(cfmbox.GetErrorMessage("GLO0017", GetLauncherName(t1profi.LNTY)), CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            CProfileSettings cProfileSettings = new CProfileSettings(t1profi.PRSE).Dezerialize();

            // Apply profile settings first
            CFGameStarter.ActivateProfileSettings(cProfileSettings);

            await Task.Delay(5000);

            launcher.StartGame(t1profi.LNID, launcher.SupportsLaunchArguments ? cProfileSettings.LaunchArgs.Trim() : string.Empty);
        }

        public static void OpenInLauncher(T1PROFI t1profi)
        {
            ILauncherIntegration? launcher = GetLauncher(t1profi);

            if (launcher == null || !launcher.SupportsOpenInLauncher)
                return;

            launcher.OpenInLauncher(t1profi.LNID);
        }

        private static List<LauncherGame> GetInstalledGamesSafe(ILauncherIntegration launcher)
        {
            string launcherName = GetLauncherName(launcher.LauncherType);

            try
            {
                return launcher.GetInstalledGames().Select(g => g with { LauncherName = launcherName }).ToList();
            }
            catch (Exception ex)
            {
                // A broken launcher installation must not prevent the import of the other launchers' games
                FnLog.AddWarning(nameof(FnLauncher), $"Reading the installed games of launcher {launcher.LauncherType} failed", ex);
                return new List<LauncherGame>();
            }
        }
    }
}
