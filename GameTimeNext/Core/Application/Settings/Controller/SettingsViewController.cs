using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.Settings.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UserInput;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Settings.Controller
{
    internal class SettingsViewController : UIXViewControllerBase
    {

        private AppSettings? _appSettings = null;

        public SettingsViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            _appSettings = AppEnvironment.GetAppConfig().AppSettings;
        }

        protected override void BuildFirst()
        {

        }

        protected override void Build()
        {
            // Sichtbarkeitssteuerung Monitoring Key Panel
            FnControls.SetVisible(GetView().pnlMonitoringKey, GetView().cbMonitoringKeyActive.IsChecked == true);

            // Enabledsteuerung Toastmessage
            FnControls.SetEnabled(GetView().cbShowToastNotification, GetView().cbMonitoringKeyActive.IsChecked == true);
            if (!GetView().cbShowToastNotification.IsEnabled)
                GetView().cbShowToastNotification.IsChecked = false;

            // Enabledsteuerung Blackout Side Monitors
            FnControls.SetEnabled(GetView().cbBlackoutSideMonitors, GetView().cbMonitoringKeyActive.IsChecked == true);
            if (!GetView().cbBlackoutSideMonitors.IsEnabled)
                GetView().cbBlackoutSideMonitors.IsChecked = false;
        }

        protected override void Check()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void FillDBOImpl()
        {
            FillDBOGeneral();
            FillDBOMonitoring();
            FillDBOTags();
            FillDBOAbout();
        }

        protected override void FillViewImpl()
        {
            FillTabGeneral();
            FillTabMonitoring();
            FillTabTags();
            FillTabAbout();
        }

        private void FillTabGeneral()
        {
            GetView().cbActivateBlackoutKeyCombination.IsChecked = _appSettings!.ActivateBlackoutKeyCombination;
            GetView().cbAllowProfileSpecificStyleChanges.IsChecked = _appSettings!.AllowProfileSpecificStyleChanges;
            GetView().txbSteamGridDbApiKey.Text = _appSettings!.SteamGridDbKey;
        }

        private void FillTabMonitoring()
        {
            GetView().cbShowToastNotification.IsChecked = _appSettings!.ShowToastNotification;
            GetView().cbShowMonitoringIndicator.IsChecked = _appSettings!.ShowMonitoringIndicator;
            GetView().cbBlackoutSideMonitors.IsChecked = _appSettings!.BlackoutSideMonitors;
            GetView().cbMonitoringKeyActive.IsChecked = _appSettings!.MonitoringKeyActive;
            GetView().txbMonitoringKey.Text = _appSettings!.MonitoringKey;
        }

        private void FillDBOGeneral()
        {
            _appSettings!.ActivateBlackoutKeyCombination = GetView().cbActivateBlackoutKeyCombination.IsChecked == true;
            _appSettings!.AllowProfileSpecificStyleChanges = GetView().cbAllowProfileSpecificStyleChanges.IsChecked == true;
            _appSettings!.SteamGridDbKey = GetView().txbSteamGridDbApiKey.Text;
        }

        private void FillDBOMonitoring()
        {
            _appSettings!.ShowToastNotification = GetView().cbShowToastNotification.IsChecked == true;
            _appSettings!.ShowMonitoringIndicator = GetView().cbShowMonitoringIndicator.IsChecked == true;
            _appSettings!.BlackoutSideMonitors = GetView().cbBlackoutSideMonitors.IsChecked == true;
            _appSettings!.MonitoringKeyActive = GetView().cbMonitoringKeyActive.IsChecked == true;
            _appSettings!.MonitoringKey = GetView().txbMonitoringKey.Text;
        }

        private void FillDBOTags()
        {
        }

        private void FillDBOAbout()
        {
        }

        private void FillTabTags()
        {
        }

        private void FillTabAbout()
        {
            GetView().txVersion.Text = AppEnvironment.AppVersion.InformationalVersion;
        }

        protected override void SaveDBOImpl()
        {
            AppEnvironment.SaveAppConfig();
        }

        private SettingsView GetView()
        {
            return (SettingsView)View;
        }

        private SettingsApp GetApp()
        {
            return (SettingsApp)App;
        }

        protected void EV_btnCaptureMonitoringKey()
        {

            GetApp().Loader.Begin("Enter desired monitor key");

            GlobalKeyInputProcess globalKeyInputProcess = GetApp().GetBackgroundProcess<GlobalKeyInputProcess>();
            globalKeyInputProcess.StartingType = StartingTypes.MonitorKey;

            globalKeyInputProcess.AfterKeyPressed = (k) =>
            {
                GetView().Dispatcher.Invoke(() =>
                {
                    GetView().txbMonitoringKey.Text = KeyInput.virtualKeyMap[k];
                });

                globalKeyInputProcess.Stop();

                GetApp().Loader.Stop();
            };

            globalKeyInputProcess.Start(20);
        }

        protected void EV_btnOpenSteamGridDB()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.steamgriddb.com/login",
                UseShellExecute = true
            });
        }
    }
}