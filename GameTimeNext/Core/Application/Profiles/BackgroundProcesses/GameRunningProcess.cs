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

        public GameRunningProcess() : base()
        {
        }

        public override void Logic()
        {
            ProcessBreakReminder();

            if (ExecutablesToSearch.Count == 0)
                return;

            // Alle Profile durchloopen
            List<T1PROFI> t1profis = T1profis.Values.ToList();

            foreach (var t1profi in t1profis)
            {
                if (FnString.IsNullEmptyOrWhitespace(t1profi.EXGF))
                    continue;

                foreach (string executable in ExecutablesToSearch[t1profi.PFID])
                {
                    if (FnSystem.IsProcessRunningWithPathPart(executable, t1profi.EXGF) && AppEnvironment.IsApplicationRunning(typeof(ProfilesApp).FullName!))
                    {
                        if (_currentProfileRunning.pfid == 0 && !t1profi.ARCH)
                        {
                            // Hier Profil wechseln
                            AppEnvironment.CurrentPfid = t1profi.PFID;

                            CallDispatcher!.Trigger("EXEV_SwitchProfile");
                            CallDispatcher.Trigger("EXEV_GameLaunched");

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
                                // Nebenmonitore ausschwärzen beenden, wenn in Settings aktiviert
                                if (AppEnvironment.GetAppConfig().AppSettings.BlackoutSideMonitors)
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

                            _currentProfileRunning = new CurrentProfileRunning();
                        }

                    }
                }
            }
        }

        public void Initialize(List<T1PROFI> profiles)
        {
            // Neuinitialisierung
            ExecutablesToSearch = new Dictionary<long, List<string>>();

            foreach (T1PROFI profile in profiles)
            {
                if (FnString.IsNullEmptyOrWhitespace(profile.EXGF))
                    continue;

                // Für dieses Profil alle Exes holen
                List<string> exes = FnExecutables.GetAllActiveExecutablesFromDBObj(profile);

                AddT1profi(profile);
                // Hinzufügen
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
