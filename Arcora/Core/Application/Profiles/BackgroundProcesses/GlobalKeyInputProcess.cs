using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.UserInput;
using UIX.ViewController.Engine.Runnables;
using static GameTimeNext.Core.Framework.UserInput.KeyInput;

namespace GameTimeNext.Core.Application.Profiles.BackgroundProcesses
{
    public class GlobalKeyInputProcess : UIXBackgroundProcess
    {
        #region private attributes
        private readonly KeyPressTracker keyPressTracker = new();
        #endregion

        #region public attributes
        public string StartingType { get; set; } = StartingTypes.Normal;
        public Action<VirtualKey>? AfterKeyPressed { get; set; }
        #endregion

        public override void Logic()
        {
            switch (StartingType)
            {
                case StartingTypes.Normal:
                    ProcessNormal();
                    break;

                case StartingTypes.MonitorKey:
                    ProcessMonitorKey();
                    break;
            }
        }

        protected override void OnStop()
        {
        }

        private void ProcessNormal()
        {
            ProcessNormalGameTimeMonitoring();
            ProcessNormalBlackoutAllMonitors();
            ProcessQueryGameTime();
        }

        private void ProcessMonitorKey()
        {
            VirtualKey pressedKey = keyPressTracker.GetPressedKeyOnce();

            if (pressedKey == VirtualKey.VK_NONE || pressedKey == VirtualKey.VK_NORESULT)
                return;

            if (AfterKeyPressed == null)
                return;

            AfterKeyPressed.Invoke(pressedKey);
        }

        private void ProcessNormalGameTimeMonitoring()
        {

            if (!AppEnvironment.GetAppConfig().AppSettings.MonitoringKeyActive)
                return;

            VirtualKey keyToListenFor = KeyInput.GetVirtualKeyFromString(AppEnvironment.GetAppConfig().AppSettings.MonitoringKey);

            if (keyToListenFor == VirtualKey.VK_NONE || keyToListenFor == VirtualKey.VK_NORESULT)
                return;

            bool pressed = keyPressTracker.IsPressedOnce(keyToListenFor);

            if (!pressed)
                return;

            if (!AppEnvironment.IsApplicationRunning(typeof(ProfilesApp).FullName!) || !AppEnvironment.IsApplicationInitialized(typeof(ProfilesApp).FullName!))
                return;

            CallDispatcher!.Trigger("EXEV_SwitchToApplication", typeof(ProfilesApp).FullName!);

            if (CFGameTimeMonitoring.IsMonitoring)
            {

                if (CFBlackout.AllowedToToggleBlackout(GetDataManager<TXPROFI>().Read(AppEnvironment.CurrentPfid)))
                {

                    bool isExternalGame = TFPROFI.IsExternalGame(AppEnvironment.CurrentPfid);

                    CallDispatcher.Invoke(() =>
                    {
                        CFBlackout.ToggleSecondaryBlackout(System.Windows.Application.Current.MainWindow, false, isExternalGame);
                    });
                }

                CFGameTimeMonitoring.StopMonitoring();
                CallDispatcher!.Trigger("EXEV_GameTimeMonitoringStopped");
            }
            else
            {
                // The profile is being edited elsewhere: it can't be monitored (the monitoring locks it)
                if (!CFGameTimeMonitoring.CheckNotLocked(this, AppEnvironment.CurrentPfid))
                    return;

                // If no active playthrough was found, ask and
                // abort if none should be created
                if (!CFProfilesApp.AskForNewPlaythroughCreationIfNotActive(AppEnvironment.CurrentPfid, this))
                    return;

                // Locks the profile; before the blackout and the toast, so nothing happens if it was opened for editing meanwhile
                if (!CFGameTimeMonitoring.StartMonitoring(this, AppEnvironment.CurrentPfid))
                    return;

                if (CFBlackout.AllowedToToggleBlackout(GetDataManager<TXPROFI>().Read(AppEnvironment.CurrentPfid)))
                {

                    bool isExternalGame = TFPROFI.IsExternalGame(AppEnvironment.CurrentPfid);

                    CallDispatcher.Invoke(() =>
                    {
                        CFBlackout.ToggleSecondaryBlackout(System.Windows.Application.Current.MainWindow, false, isExternalGame);
                    });
                }

                CallDispatcher!.Trigger("EXEV_GameTimeMonitoringStarted");
            }
        }

        private void ProcessQueryGameTime()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.EnableSessionTimeQuery)
                return;

            // Combination Ctrl + Shift + T
            if (keyPressTracker.IsCombinationPressedOnce(VirtualKey.VK_CONTROL, VirtualKey.VK_SHIFT, VirtualKey.VK_T))
            {
                CallDispatcher.Invoke(() =>
                {
                    double currentSessionGameTime = CFGameTimeMonitoring.GetCurrentGameTimeMinutes();

                    if (currentSessionGameTime == 0.0)
                        return;

                    CallDispatcher.Invoke(() =>
                    {
                        T1PROFI t1profi = GetDataManager<TXPROFI>().Read(AppEnvironment.CurrentPfid);
                        CFToast.Show(t1profi.GANA, $"Current session game time: {CFProfilesApp.FormatGameTimeMinutes(currentSessionGameTime)}");
                    });
                });
            }
        }

        private void ProcessNormalBlackoutAllMonitors()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.ActivateBlackoutKeyCombination)
                return;

            if (keyPressTracker.IsCombinationPressedOnce(VirtualKey.VK_CONTROL, VirtualKey.VK_B))
            {
                CallDispatcher.Invoke(() =>
                {
                    CFBlackout.ToggleBlackout(System.Windows.Application.Current.MainWindow);
                });
            }
        }

        protected override void InitializeInfos()
        {
        }

        public static VirtualKey GetVirtualKeyByValue(Dictionary<VirtualKey, string> list, string value)
        {
            return list.FirstOrDefault(x => x.Value == value).Key;
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(GlobalKeyInputProcess));
        }

        public override void InitializeApplicationOutput()
        {
        }

        public enum StartType
        {
            MONITORE_KEY,
            GAME_MONITORING,
            BLACKOUT_SCREEN
        }
    }

    public class StartingTypes
    {
        public const string Normal = "GKIP.Normal";
        public const string MonitorKey = "GKIP.MonitorKey";
    }
}
