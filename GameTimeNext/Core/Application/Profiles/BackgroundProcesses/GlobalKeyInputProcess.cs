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

        private void ProcessNormal()
        {
            ProcessNormalGameTimeMonitoring();
            ProcessNormalBlackoutAllMonitors();
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

            if (!AppEnvironment.IsApplicationRunning(typeof(ProfilesApp).FullName!))
                return;

            if (CFGameTimeMonitoring.IsMonitoring)
            {

                if (AppEnvironment.GetAppConfig().AppSettings.BlackoutSideMonitors)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        CFBlackout.ToggleSecondaryBlackout(System.Windows.Application.Current.MainWindow);
                    });
                }

                if (AppEnvironment.GetAppConfig().AppSettings.ShowToastNotification)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        T1PROFI t1profi = new TXPROFI().Read(AppEnvironment.CurrentPfid);
                        ToastMessage tm = new ToastMessage(t1profi.GANA, "Monitoring stopped...");
                        tm.Show();
                    });
                }

                CFGameTimeMonitoring.StopMonitoring();
                CallDispatcher!.Trigger("EXEV_GameTimeMonitoringStopped");
            }
            else
            {
                // Wenn kein aktiver Playthrough gefunden wurde, nachfragen und
                // Abbruch, falls keiner angelegt werden soll
                if (!CFProfilesApp.AskForNewPlaythroughCreationIfNotActive(AppEnvironment.CurrentPfid, this))
                    return;

                if (AppEnvironment.GetAppConfig().AppSettings.BlackoutSideMonitors)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        CFBlackout.ToggleSecondaryBlackout(System.Windows.Application.Current.MainWindow);
                    });
                }

                if (AppEnvironment.GetAppConfig().AppSettings.ShowToastNotification)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        T1PROFI t1profi = new TXPROFI().Read(AppEnvironment.CurrentPfid);
                        ToastMessage tm = new ToastMessage(t1profi.GANA, "Monitoring started...");
                        tm.Show();
                    });
                }

                CFGameTimeMonitoring.StartMonitoring(AppEnvironment.CurrentPfid);
                CallDispatcher!.Trigger("EXEV_GameTimeMonitoringStarted");
            }
        }

        private void ProcessNormalBlackoutAllMonitors()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.ActivateBlackoutKeyCombination)
                return;

            if (keyPressTracker.IsCombinationPressedOnce(VirtualKey.VK_CONTROL, VirtualKey.VK_B))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
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