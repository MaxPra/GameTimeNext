using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.Diagnostics;
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
        // Set while the launch waits for the profile settings (e.g. HDR) before the launcher is started
        private static CancellationTokenSource? _launchCancellation;

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

        /// <summary>
        /// True if the game can really be started: linked with a launcher, executables selected (needed by the running
        /// game detection) and one of them present in the game folder.
        /// </summary>
        public static bool CanLaunch(T1PROFI t1profi)
        {
            return IsLinked(t1profi) && TFPROFI.HasExecutables(t1profi) && FnExecutables.IsAnyActiveExecutableInstalled(t1profi);
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
        /// Returns false if the game isn't installed or the launch was cancelled before the launcher was started.
        /// </summary>
        public static async Task<bool> StartGameAsync(T1PROFI t1profi, UIXApplication app)
        {
            ILauncherIntegration? launcher = GetLauncher(t1profi);

            if (launcher == null)
            {
                FnLog.AddWarning(nameof(FnLauncher), $"{t1profi.GANA} can't be started: there is no integration for launcher type '{t1profi.LNTY}'");
                return false;
            }

            if (!IsGameInstalledSafe(launcher, t1profi))
            {
                FnLog.AddInfo(nameof(FnLauncher), $"{t1profi.GANA} isn't installed in launcher {launcher.LauncherType} (game ID '{t1profi.LNID}')");

                CFMBOX cfmbox = app.GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
                cfmbox.Show(cfmbox.GetErrorMessage("GLO0017", GetLauncherName(t1profi.LNTY)), CFMBOXResult.Ok, CFMBOXIcon.Info);
                return false;
            }

            CProfileSettings cProfileSettings = new CProfileSettings(t1profi.PRSE).Dezerialize();

            // Apply profile settings first
            CFGameStarter.ActivateProfileSettings(cProfileSettings);

            _launchCancellation = new CancellationTokenSource();

            try
            {
                await Task.Delay(5000, _launchCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                _launchCancellation.Dispose();
                _launchCancellation = null;
            }

            string launchArgs = launcher.SupportsLaunchArguments ? cProfileSettings.LaunchArgs.Trim() : string.Empty;

            FnLog.AddInfo(nameof(FnLauncher), $"Starting {t1profi.GANA} via launcher {launcher.LauncherType} (game ID '{t1profi.LNID}', arguments '{launchArgs}')");

            try
            {
                launcher.StartGame(t1profi.LNID, launchArgs);
            }
            catch (Exception ex)
            {
                FnLog.AddError(nameof(FnLauncher), $"Starting {t1profi.GANA} via launcher {launcher.LauncherType} failed (game ID '{t1profi.LNID}')", ex);

                CFGameStarter.DeactivateProfileSettings(cProfileSettings);

                new ToastMessage("Launch failed", $"{t1profi.GANA} could not be started.\nDetails are in the log.").Show();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Cancels the launch of the game: if the launcher wasn't started yet, the game isn't started at all,
        /// otherwise the already running processes of the game are ended. The profile settings (e.g. HDR) are reset.
        /// </summary>
        public static void CancelLaunch(T1PROFI t1profi)
        {
            FnLog.AddInfo(nameof(FnLauncher), _launchCancellation != null
                ? $"Launch of {t1profi.GANA} cancelled before the launcher was started"
                : $"Launch of {t1profi.GANA} cancelled, ending its running processes");

            _launchCancellation?.Cancel();

            QuitGame(t1profi);

            CFGameStarter.DeactivateProfileSettings(new CProfileSettings(t1profi.PRSE).Dezerialize());
        }

        /// <summary>
        /// Ends all running processes of the game (its active executables in the game folder), like Steam's "Stop".
        /// Child processes outside of these executables (e.g. a crash reporter) are not ended.
        /// The running game detection notices the end and resets the profile settings.
        /// Returns false if a process couldn't be ended (the game keeps running).
        /// </summary>
        public static bool QuitGame(T1PROFI t1profi)
        {
            if (FnString.IsNullEmptyOrWhitespace(t1profi.EXGF))
                return true;

            bool allEnded = true;

            foreach (Process process in FnSystem.GetProcessesWithPathPart(FnExecutables.GetAllActiveExecutablesFromDBObj(t1profi), t1profi.EXGF))
            {
                try
                {
                    // Already ended (e.g. the starter ends together with the game)
                    if (process.HasExited)
                        continue;

                    FnLog.AddInfo(nameof(FnLauncher), $"Quitting {t1profi.GANA}: ending process {process.ProcessName}");

                    // Without the process tree: .NET would read the parent of every process on the system for it and
                    // throws (and catches) internally whenever one of them exits meanwhile. The game's own processes
                    // are its executables in the game folder anyway, and they are all ended here.
                    process.Kill();
                }
                catch (InvalidOperationException)
                {
                    // Exited between the check and the kill: nothing left to end
                }
                catch (Exception ex)
                {
                    // e.g. the game runs elevated
                    FnLog.AddWarning(nameof(FnLauncher), $"Could not end process {process.ProcessName}", ex);
                    allEnded = false;
                }
                finally
                {
                    process.Dispose();
                }
            }

            if (!allEnded)
                new ToastMessage("Quit failed", $"{t1profi.GANA} could not be ended completely.\nDetails are in the log.").Show();

            return allEnded;
        }

        /// <summary>
        /// Brings the window of the running game back to the front (e.g. from the Big Picture mode).
        /// Returns false if no window of the game was found.
        /// </summary>
        public static bool SwitchToGame(T1PROFI t1profi)
        {
            IntPtr gameWindow = FindGameWindow(t1profi);

            if (gameWindow == IntPtr.Zero)
            {
                FnLog.AddInfo(nameof(FnLauncher), $"No window of {t1profi.GANA} found to switch to");
                return false;
            }

            FnForeground.BringToFront(gameWindow);
            return true;
        }

        /// <summary>
        /// Minimizes the window of the running game, so it loses the focus and most games stop reading the controller
        /// (e.g. when the Big Picture mode comes back to the front). Returns false if no window of the game was found.
        /// </summary>
        public static bool MinimizeGame(T1PROFI t1profi)
        {
            IntPtr gameWindow = FindGameWindow(t1profi);

            if (gameWindow == IntPtr.Zero)
                return false;

            FnForeground.Minimize(gameWindow);
            return true;
        }

        /// <summary>
        /// The main window of the running game (largest window of its executables), IntPtr.Zero if it has none (yet).
        /// </summary>
        public static IntPtr FindGameWindow(T1PROFI t1profi)
        {
            if (FnString.IsNullEmptyOrWhitespace(t1profi.EXGF))
                return IntPtr.Zero;

            List<Process> processes = FnSystem.GetProcessesWithPathPart(FnExecutables.GetAllActiveExecutablesFromDBObj(t1profi), t1profi.EXGF);

            try
            {
                return FnForeground.FindMainWindow(processes.Select(p => p.Id).ToList());
            }
            finally
            {
                foreach (Process process in processes)
                    process.Dispose();
            }
        }

        public static void OpenInLauncher(T1PROFI t1profi)
        {
            ILauncherIntegration? launcher = GetLauncher(t1profi);

            if (launcher == null || !launcher.SupportsOpenInLauncher)
                return;

            try
            {
                launcher.OpenInLauncher(t1profi.LNID);
            }
            catch (Exception ex)
            {
                // e.g. the launcher was uninstalled and its URI isn't registered anymore
                FnLog.AddError(nameof(FnLauncher), $"Showing {t1profi.GANA} in launcher {launcher.LauncherType} failed (game ID '{t1profi.LNID}')", ex);

                new ToastMessage("Launcher not available", $"{GetLauncherName(t1profi.LNTY)} could not be opened.\nDetails are in the log.").Show();
            }
        }

        private static bool IsGameInstalledSafe(ILauncherIntegration launcher, T1PROFI t1profi)
        {
            try
            {
                return launcher.IsGameInstalled(t1profi.LNID);
            }
            catch (Exception ex)
            {
                FnLog.AddError(nameof(FnLauncher), $"Checking the installation of {t1profi.GANA} in launcher {launcher.LauncherType} failed (game ID '{t1profi.LNID}')", ex);
                return false;
            }
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
