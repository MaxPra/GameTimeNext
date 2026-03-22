using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.Settings.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Files;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.UserInput;
using GameTimeNext.Core.Framework.Utils;
using System.Diagnostics;
using System.IO;
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
            FnControls.SetVisible(GetView().pnlMonitoringKey, GetView().cbMonitoringKeyActive.IsChecked == true);

            FnControls.SetEnabled(GetView().cbShowToastNotification, GetView().cbMonitoringKeyActive.IsChecked == true);
            if (!GetView().cbShowToastNotification.IsEnabled)
                GetView().cbShowToastNotification.IsChecked = false;

            FnControls.SetEnabled(GetView().cbBlackoutSideMonitors, GetView().cbMonitoringKeyActive.IsChecked == true);
            if (!GetView().cbBlackoutSideMonitors.IsEnabled)
                GetView().cbBlackoutSideMonitors.IsChecked = false;

            FnControls.SetEnabled(GetView().btnImportBackup, !FnString.IsNullEmptyOrWhitespace(GetView().txbBackupImportPath.Text));
            FnControls.SetEnabled(GetView().btnCreateBackup, !FnString.IsNullEmptyOrWhitespace(GetView().txbBackupExportPath.Text));

            bool hasBackupExportPath = !FnString.IsNullEmptyOrWhitespace(GetView().txbBackupExportPath.Text);
            bool autoBackupEnabled = GetView().cbAutoBackup.IsChecked == true;

            FnControls.SetEnabled(GetView().cbAutoBackup, hasBackupExportPath);
            FnControls.SetEnabled(GetView().cbAutoDeleteBackups, hasBackupExportPath && autoBackupEnabled);

            if (!GetView().cbAutoDeleteBackups.IsEnabled)
                GetView().cbAutoDeleteBackups.IsChecked = false;
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

            // Backup
            GetView().txbBackupExportPath.Text = AppEnvironment.GetAppConfig().AppSettings.BackupExportPath;
            GetView().cbAutoBackup.IsChecked = _appSettings!.AutoBackup == true;
            GetView().cbAutoDeleteBackups.IsChecked = _appSettings.AutoDelete == true;
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
            _appSettings!.BackupExportPath = GetView().txbBackupExportPath.Text;
            _appSettings!.AutoBackup = GetView().cbAutoBackup.IsChecked == true;
            _appSettings!.AutoDelete = GetView().cbAutoDeleteBackups.IsChecked == true;
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

        protected void EV_btnBrowseBackupExportPath()
        {
            string backupPath = FnSystemDialogs.ShowFolderDialog("Choose backup folder", false);

            if (!FnString.IsNullEmptyOrWhitespace(backupPath))
            {
                GetView().txbBackupExportPath.Text = backupPath;
            }
        }

        protected void EV_btnBrowseBackupImportPath()
        {
            string importPath = FnSystemDialogs.ShowFileDialog("Choose backup file", "GTN backup file (*.gtnbkp) | *.gtnbkp", false);

            if (!FnString.IsNullEmptyOrWhitespace(importPath))
            {
                GetView().txbBackupImportPath.Text = importPath;
            }
        }

        protected async Task EV_btnCreateBackup()
        {
            if (!Directory.Exists(GetView().txbBackupExportPath.Text))
            {
                GetApp().GetApplication<CFMBOX>().Show("Error", "Chosen backup path doesn't exist!", CFMBOXResult.Ok, CFMBOXIcon.Error);
                return;
            }

            string exportPath = GetView().txbBackupExportPath.Text;

            GetApp().Loader.Begin("Creating backup...");

            bool success = true;

            await Task.Run(async () =>
            {
                try
                {
                    await FnBackup.CreateBackupAsync(exportPath);
                }
                catch (Exception e)
                {
                    GetView().Dispatcher.Invoke(() =>
                    {
                        GetApp().GetApplication<CFMBOX>().Show("Error", "Something went wrong while backup creation!", CFMBOXResult.Ok, CFMBOXIcon.Error);
                    });

                    success = false;
                }
                finally
                {
                    GetApp().Loader.Stop();
                }
            });

            if (success)
                GetApp().GetApplication<CFMBOX>().Show("Success", "Backup was created successfully!", CFMBOXResult.Ok, CFMBOXIcon.Success);
        }

        protected async Task EV_btnImportBackup()
        {
            if (!File.Exists(GetView().txbBackupImportPath.Text))
                GetApp().GetApplication<CFMBOX>().Show("Error", "Chosen import file doesn't exist!", CFMBOXResult.Ok, CFMBOXIcon.Error);

            CFMBOXResult result = GetApp().GetApplication<CFMBOX>().Show("Info", "The application will be restarted now and will then importing the backup.\nYour current Settings will be saved now.\nDo you want to proceed?", CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Info);

            if (result == CFMBOXResult.Yes)
            {
                AppEnvironment.GetAppConfig().AppSettings.BackupType = BackupType.IMPORT_BACKUP;
                AppEnvironment.GetAppConfig().AppSettings.BackupImportPath = GetView().txbBackupImportPath.Text;

                // Aktuelle Einstellungen speichern
                SaveDBO();

                // GameTimeNext neustarten
                AppEnvironment.RestartGTNApplication();
            }
        }
    }
}