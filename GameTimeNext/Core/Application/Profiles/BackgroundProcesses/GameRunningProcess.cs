using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.Utils;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.BackgroundProcesses
{
    public class GameRunningProcess : UIXBackgroundProcess
    {

        public Dictionary<long, List<string>> ExecutablesToSearch { get; set; } = new Dictionary<long, List<string>>();
        public Dictionary<long, T1PROFI> T1profis { get; set; } = new Dictionary<long, T1PROFI>();

        private CurrentProfileRunning? _currentProfileRunning = new CurrentProfileRunning();

        private int _lastBreakReminderBlock = -1;

        /// <summary>
        /// PFID of the game that is running right now, 0 if none.
        /// </summary>
        public long RunningPfid => _currentProfileRunning?.pfid ?? 0;

        public GameRunningProcess() : base()
        {
        }

        public override void Logic()
        {
            ProcessBreakReminder();

            if (ExecutablesToSearch.Count > 0)
            {
                Dictionary<string, List<string>> runningProcesses = FnSystem.GetRunningProcessesSnapshot(ExecutablesToSearch);

                // Loop through all profiles
                List<T1PROFI> t1profis = T1profis.Values.ToList();

                foreach (T1PROFI t1profi in t1profis)
                {
                    if (FnString.IsNullEmptyOrWhitespace(t1profi.EXGF) || FnString.IsNullEmptyOrWhitespace(t1profi.EXEC))
                    {
                        continue;
                    }


                    if (!ExecutablesToSearch.TryGetValue(t1profi.PFID, out List<string>? executables))
                        continue;

                    foreach (string executable in executables)
                    {
                        if (FnSystem.IsProcessRunningWithPathPart(runningProcesses, executable, t1profi.EXGF) && AppEnvironment.IsApplicationRunning(typeof(ProfilesApp).FullName!) && AppEnvironment.IsApplicationInitialized(typeof(ProfilesApp).FullName!))
                        {
                            if (_currentProfileRunning!.pfid == 0 && !t1profi.ARCH)
                            {
                                // Switch to the profile application
                                // MainWindowController
                                CallDispatcher!.Trigger("EXEV_SwitchToApplication", typeof(ProfilesApp).FullName!);

                                // Switch profile here
                                AppEnvironment.CurrentPfid = t1profi.PFID;

                                // ProfilesViewController
                                CallDispatcher!.Trigger("EXEV_SwitchProfile", t1profi.PFID);
                                CallDispatcher.Trigger("EXEV_GameLaunched");

                                // BigPictureViewController, only registered while the Big Picture mode is open
                                TriggerIfRegistered("EXEV_BigPictureGameLaunched", t1profi.PFID);

                            }

                            if (!_currentProfileRunning.executables.Contains(executable))
                            {
                                _currentProfileRunning!.pfid = t1profi.PFID;
                                _currentProfileRunning.executables.Add(executable);
                                _currentProfileRunning.path = t1profi.EXGF;
                            }
                        }
                        else
                        {
                            if (_currentProfileRunning.pfid == 0)
                                continue;

                            if (!IsAnyExeRunning(_currentProfileRunning, executable, t1profi.EXGF))
                            {
                                if (CFGameTimeMonitoring.IsMonitoring)
                                {
                                    // Stop blacking out secondary monitors if enabled in settings and allowed by the profile
                                    if (CFBlackout.AllowedToToggleBlackout(t1profi))
                                    {
                                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            CFBlackout.ToggleSecondaryBlackout(System.Windows.Application.Current.MainWindow);
                                        });
                                    }

                                    CFGameTimeMonitoring.StopMonitoring();
                                    CallDispatcher!.Trigger("EXEV_GameTimeMonitoringStopped");
                                }

                                CFGameStarter.DeactivateProfileSettings(t1profi.PFID);

                                CallDispatcher!.Trigger("EXEV_GameClosed");

                                // BigPictureViewController, only registered while the Big Picture mode is open
                                TriggerIfRegistered("EXEV_BigPictureGameClosed", _currentProfileRunning.pfid);

                                _currentProfileRunning = new CurrentProfileRunning();
                            }

                        }
                    }
                }
            }
        }

        protected override void OnStop()
        {
        }

        private void TriggerIfRegistered(string methodName, long pfid)
        {
            if (CallDispatcher!.IsRegistered(methodName))
                CallDispatcher.Trigger(methodName, pfid);
        }

        public void Initialize(List<T1PROFI> profiles)
        {
            // Reinitialization
            ExecutablesToSearch = new Dictionary<long, List<string>>();

            foreach (T1PROFI profile in profiles)
            {
                if (FnString.IsNullEmptyOrWhitespace(profile.EXGF))
                    continue;

                // Get all executables for this profile
                List<string> exes = FnExecutables.GetAllActiveExecutablesFromDBObj(profile);

                AddT1profi(profile);
                // Add
                AddExecutables(profile.PFID, exes);
            }
        }

        public void AddExecutable(long pfid, string executable)
        {
            if (ExecutablesToSearch.ContainsKey(pfid))
            {
                if (!ExecutablesToSearch[pfid].Contains(executable))
                    ExecutablesToSearch[pfid].Add(executable);
            }
        }

        public void AddExecutables(long pfid, List<string> executables)
        {

            if (!ExecutablesToSearch.ContainsKey(pfid))
            {
                ExecutablesToSearch.Add(pfid, executables);
            }
            else
            {
                foreach (string executable in executables)
                {
                    AddExecutable(pfid, executable);
                }
            }
        }

        public void RemoveExecutables(long pfid)
        {
            ExecutablesToSearch[pfid] = new List<string>();
        }

        public void RemoveExecutablesAndProfile(long pfid)
        {
            ExecutablesToSearch.Remove(pfid);
        }

        public void AddT1profi(T1PROFI t1profi)
        {
            if (!T1profis.ContainsKey(t1profi.PFID))
                T1profis.Add(t1profi.PFID, t1profi);
            else
            {
                RemoveT1profi(t1profi);
                T1profis.Add(t1profi.PFID, t1profi);
            }

        }

        public void RemoveT1profi(T1PROFI t1profi)
        {
            if (T1profis.ContainsKey(t1profi.PFID))
                T1profis.Remove(t1profi.PFID);
        }

        protected override void InitializeInfos()
        {
            ProcessName = "AutoProfileSwitchingProcess";
        }

        private void ProcessBreakReminder()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.BreakReminder)
                return;

            if (!CFGameTimeMonitoring.IsMonitoring)
                return;

            double currentSessionTimeMinutes = CFGameTimeMonitoring.GetCurrentGameTimeMinutes();
            double intervalMinutes = AppEnvironment.GetAppConfig().AppSettings.BreakReminderHrs * 60.0;

            if (currentSessionTimeMinutes > 0 && intervalMinutes > 0)
            {
                int currentBlock = (int)(currentSessionTimeMinutes / intervalMinutes);

                if (currentBlock > 0 && currentBlock != _lastBreakReminderBlock)
                {
                    _lastBreakReminderBlock = currentBlock;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ToastMessage tm = new ToastMessage(
                            "Break reminder",
                            "You've played " + AppEnvironment.GetAppConfig().AppSettings.BreakReminderHrs + " hours\nTime for a break!");
                        tm.Show();
                    });
                }

            }
        }

        private bool IsAnyExeRunning(CurrentProfileRunning currentProfileRunning, string executable, string exePath)
        {

            if (currentProfileRunning.path != exePath)
                return true;

            foreach (string exe in currentProfileRunning.executables)
            {
                if (exe == executable && currentProfileRunning.path == exePath)
                {
                    currentProfileRunning.countNotRunning++;
                }
            }

            return !(currentProfileRunning.countNotRunning == currentProfileRunning.executables.Count);
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(GameRunningProcess));
        }

        public override void InitializeApplicationOutput()
        {
        }

        private class CurrentProfileRunning
        {
            public long pfid = 0;
            public List<string> executables = new List<string>();
            public int countNotRunning = 0;
            public string path = string.Empty;
        }
    }
}
