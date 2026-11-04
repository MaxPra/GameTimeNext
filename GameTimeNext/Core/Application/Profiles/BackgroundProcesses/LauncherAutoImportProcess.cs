using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.Utils;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.BackgroundProcesses
{
    /// <summary>
    /// Adds newly installed launcher games as profiles (settings "Game import"). Every cycle reads the installed games
    /// of the launchers taking part and the status of the registered games (T1LNGAM, two columns); profiles are only
    /// read and created if a game isn't registered yet or is waiting (PEND). The status is read again every cycle,
    /// not cached: the settings register games too (question when a launcher is switched on).
    /// Every game is registered in T1LNGAM (TFLNGAM) and imported at most once, a few per cycle.
    /// </summary>
    public class LauncherAutoImportProcess : UIXBackgroundProcess
    {
        private const int MaxImportsPerCycle = 5;

        // Games whose import failed in this session: retried after a restart, so they don't block the others
        private readonly HashSet<(string, string)> _failedImports = new HashSet<(string, string)>();

        public override void Logic()
        {
            try
            {
                ImportNewGames();
            }
            catch (Exception ex)
            {
                // Stopped while the cycle was running (exit): the failure is a consequence of the closed databases / dispatcher
                if (Cts?.IsCancellationRequested == true)
                    return;

                FnLog.AddError(this, "The automatic game import failed", ex);
            }
        }

        protected override void OnStop()
        {
        }

        protected override void InitializeInfos()
        {
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(LauncherAutoImportProcess));
        }

        public override void InitializeApplicationOutput()
        {
        }

        private void ImportNewGames()
        {
            List<string> launcherTypes = CFProfilesApp.GetAutoImportLauncherTypes(AppEnvironment.GetAppConfig().AppSettings);

            if (launcherTypes.Count == 0)
                return;

            List<LauncherGame> installedGames = FnLauncher.GetInstalledGames(launcherTypes);

            if (installedGames.Count == 0)
                return;

            Dictionary<(string, string), string> statuses = InvokeOnUI(TFLNGAM.GetRegisteredStatuses);

            // Nothing new installed and nothing waiting: done
            if (installedGames.All(g => statuses.TryGetValue((g.LauncherType, g.GameId), out string? status)
                                        && (status != "PEND" || _failedImports.Contains((g.LauncherType, g.GameId)))))
                return;

            List<(T1LNGAM t1lngam, LauncherGame game)> pendingGames = InvokeOnUI(() =>
            {
                TFLNGAM.RegisterGames(installedGames, "PEND");
                return TFLNGAM.GetPendingGames(installedGames);
            });

            // Not retried in this session; the rest follows with the next cycles
            pendingGames = pendingGames.Where(p => !_failedImports.Contains((p.game.LauncherType, p.game.GameId))).ToList();

            List<T1PROFI> importedProfiles = new List<T1PROFI>();

            foreach ((T1LNGAM t1lngam, LauncherGame game) in pendingGames.Take(MaxImportsPerCycle))
            {
                if (Cts!.IsCancellationRequested)
                    return;

                try
                {
                    T1PROFI t1profi = CFProfilesApp.CreateProfileFromLauncherGameAsync(this, game, Cts.Token).GetAwaiter().GetResult();

                    InvokeOnUI(() =>
                    {
                        TFPROFI.SaveNewProfile(t1profi);
                        TFLNGAM.SetImported(t1lngam, t1profi.PFID);
                        return true;
                    });

                    importedProfiles.Add(t1profi);

                    FnLog.AddInfo(this, $"{game.Name} ({game.LauncherType}) was added as profile");
                }
                catch (Exception ex)
                {
                    _failedImports.Add((game.LauncherType, game.GameId));
                    FnLog.AddError(this, $"{game.Name} ({game.LauncherType}) could not be added as profile, it is tried again after the next start", ex);
                }
            }

            if (importedProfiles.Count > 0)
                InvokeOnUI(() => PublishImportedProfiles(importedProfiles));
        }

        /// <summary>
        /// Hands the new profiles to the running game detection, updates an open profiles view and shows the toast.
        /// </summary>
        private bool PublishImportedProfiles(List<T1PROFI> importedProfiles)
        {
            if (AppEnvironment.StartedBackgroundProcesses.TryGetValue(typeof(GameRunningProcess).FullName!, out UIXBackgroundProcess? process)
                && process is GameRunningProcess gameRunningProcess)
            {
                foreach (T1PROFI t1profi in importedProfiles)
                {
                    gameRunningProcess.AddT1profi(t1profi);
                    gameRunningProcess.AddExecutables(t1profi.PFID, FnExecutables.GetAllActiveExecutablesFromDBObj(t1profi));
                }
            }

            if (CallDispatcher.IsRegistered("EXEV_ProfilesAutoImported"))
                CallDispatcher.Trigger("EXEV_ProfilesAutoImported");

            CFProfilesApp.ShowAutoImportToast(this, importedProfiles.Select(p => p.GANA).ToList());

            return true;
        }

        /// <summary>
        /// Runs database work and UI updates on the UI thread, like the other background processes.
        /// </summary>
        private static T InvokeOnUI<T>(Func<T> func)
        {
            return System.Windows.Application.Current.Dispatcher.Invoke(func);
        }
    }
}
