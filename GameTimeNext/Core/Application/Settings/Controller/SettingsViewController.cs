using System.Globalization;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.Settings.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
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
    internal class SettingsViewController : UIXViewControllerBase<SettingsApp, SettingsView>
    {

        private AppSettings? _appSettings = null;

        private bool _hasIGDBChanged = false;

        public SettingsViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            _appSettings = AppEnvironment.GetAppConfig().AppSettings;
            _appSettings.ResetChangeTracking();
            _appSettings.AcceptChanges();
        }

        protected override void BuildFirstImpl()
        {
            if (FnSystem.IsDebug())
            {
                GetView().cbAutoStartMinimized.IsChecked = false;
                _appSettings!.AutoStartMinimized = false;
                _appSettings!.ResetChangeTracking();
                _appSettings!.AcceptChanges();
            }

            FnControls.SetEnabled(GetView().cbAutoStartMinimized, !FnSystem.IsDebug());

            FillComboboxBackgroundSource();
        }

        protected override void BuildImpl()
        {
            FnControls.SetEnabled(GetView().txbBreakReminderHours, GetView().cbRemindTakeBreak.IsChecked == true);

            FnControls.SetVisible(GetView().pnlMonitoringKey, GetView().cbMonitoringKeyActive.IsChecked == true);

            FnControls.SetEnabled(GetView().cbShowToastNotification, GetView().cbMonitoringKeyActive.IsChecked == true);
            if (!GetView().cbShowToastNotification.IsEnabled)
                GetView().cbShowToastNotification.IsChecked = false;

            FnControls.SetEnabled(GetView().cbBlackoutSideMonitors, GetView().cbMonitoringKeyActive.IsChecked == true);
            if (!GetView().cbBlackoutSideMonitors.IsEnabled)
                GetView().cbBlackoutSideMonitors.IsChecked = false;

            FnControls.SetEnabled(GetView().btnImportBackup, !FnString.IsNullEmptyOrWhitespace(GetView().txbBackupImportPath.Text));
            FnControls.SetEnabled(GetView().btnCreateBackup, !FnString.IsNullEmptyOrWhitespace(GetView().txbBackupExportPath.Text));

            FnControls.SetEnabled(GetView().cbShowTextOnBlackout, GetView().cbActivateBlackoutKeyCombination.IsChecked == true);
            if (!GetView().cbActivateBlackoutKeyCombination.IsChecked == true)
            {
                GetView().cbShowTextOnBlackout.IsChecked = false;
            }


            bool hasBackupExportPath = !FnString.IsNullEmptyOrWhitespace(GetView().txbBackupExportPath.Text);
            bool autoBackupEnabled = GetView().cbAutoBackup.IsChecked == true;

            FnControls.SetEnabled(GetView().cbAutoBackup, hasBackupExportPath);
            FnControls.SetEnabled(GetView().cbAutoDeleteBackups, hasBackupExportPath && autoBackupEnabled);

            if (!GetView().cbAutoDeleteBackups.IsEnabled)
                GetView().cbAutoDeleteBackups.IsChecked = false;

            FnControls.SetEnabled(GetView().txbIgdbClientId, GetView().cbEnableIGDBIntegration.IsChecked == true);
            FnControls.SetEnabled(GetView().btnOpenIgdbClientId, GetView().cbEnableIGDBIntegration.IsChecked == true);
            FnControls.SetEnabled(GetView().txbIgdbClientSecret, GetView().cbEnableIGDBIntegration.IsChecked == true);
            FnControls.SetEnabled(GetView().btnOpenIgdbClientSecret, GetView().cbEnableIGDBIntegration.IsChecked == true);
        }

        protected override void CheckImpl()
        {
            if (!FnControls.ContainsOnlyNumericValue(GetView().txbBreakReminderHours))
            {
                AddViewError(GetView().txbBreakReminderHours, GetErrorMessage("GLO0009"));
            }

            if (!FnControls.ContainsOnlyNumericValue(GetView().txbSessionCleanupSeconds))
            {
                AddViewError(GetView().txbSessionCleanupSeconds, GetErrorMessage("GLO0009"));
            }
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
            CFMBOX.AskUnsavedChanges(GetApp(), args);
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
            GetView().cbShowTextOnBlackout.IsChecked = _appSettings.EnableFullBlackoutText;
            GetView().cbAutoStartMinimized.IsChecked = _appSettings.AutoStartMinimized;
            GetView().cbSealNewGames.IsChecked = _appSettings.SealNewGames;
            GetView().cbAllowProfileSpecificStyleChanges.IsChecked = _appSettings!.AllowProfileSpecificStyleChanges;
            GetView().cbDarkTintedGlass.IsChecked = _appSettings.DarkTintedGlass;
            GetView().cmbBackgroundSource.SelectedValue = _appSettings.BackgroundSource;
            if (GetView().cmbBackgroundSource.SelectedItem == null)
                GetView().cmbBackgroundSource.SelectedValue = string.Empty;
            GetView().txbSteamGridDbApiKey.Text = _appSettings!.SteamGridDbKey;

            // Backup
            GetView().txbBackupExportPath.Text = AppEnvironment.GetAppConfig().AppSettings.BackupExportPath;
            GetView().cbAutoBackup.IsChecked = _appSettings!.AutoBackup == true;
            GetView().cbAutoDeleteBackups.IsChecked = _appSettings.AutoDelete == true;

            // IGDB
            GetView().cbEnableIGDBIntegration.IsChecked = _appSettings.EnableTwitchIGDB;
            GetView().txbIgdbClientId.Text = _appSettings.TwitchIGDBClientID;
            GetView().txbIgdbClientSecret.Text = _appSettings.TwitchIGDBClientSecret;

            GetView().cbRemindTakeBreak.IsChecked = _appSettings.BreakReminder;
            GetView().txbBreakReminderHours.Text = _appSettings.BreakReminderHrs.ToString(CultureInfo.CurrentCulture);
        }

        private void FillComboboxBackgroundSource()
        {
            // Empty entry disables the dynamic background
            UIXManualCodetable uIXManualCodetable = new UIXManualCodetable();
            uIXManualCodetable.AddAdditionalEntry("(None)");

            TXCTABD txctabd = new TXCTABD();
            List<T1CTABD> entrys = txctabd.GetEntries(BackgroundSource.CODETABLE);

            foreach (T1CTABD entry in entrys)
            {
                uIXManualCodetable.AddEntry(entry.TXNUM, entry.DESCR);
            }

            uIXManualCodetable.ApplyTo(GetView().cmbBackgroundSource);
        }

        private string GetSelectedBackgroundSource()
        {
            return GetView().cmbBackgroundSource.SelectedValue as string ?? string.Empty;
        }

        private void FillTabMonitoring()
        {
            GetView().cbMonitoringKeyActive.IsChecked = _appSettings!.MonitoringKeyActive;
            GetView().cbShowToastNotification.IsChecked = _appSettings!.ShowToastNotification;
            GetView().cbShowMonitoringIndicator.IsChecked = _appSettings!.ShowMonitoringIndicator;
            GetView().cbBlackoutSideMonitors.IsChecked = _appSettings!.BlackoutSideMonitors;
            GetView().txbMonitoringKey.Text = _appSettings!.MonitoringKey;
            GetView().cbEnableSessionTimeQuery.IsChecked = _appSettings!.EnableSessionTimeQuery;
            GetView().cbEnableSessionCleanup.IsChecked = _appSettings!.EnableSessionCleanup;
            GetView().txbSessionCleanupSeconds.Text = _appSettings!.SessionCleanupSeconds.ToString(CultureInfo.CurrentCulture);
        }

        private void FillDBOGeneral()
        {
            _appSettings!.ActivateBlackoutKeyCombination = GetView().cbActivateBlackoutKeyCombination.IsChecked == true;
            _appSettings!.EnableFullBlackoutText = GetView().cbShowTextOnBlackout.IsChecked == true;
            _appSettings!.AutoStartMinimized = GetView().cbAutoStartMinimized.IsChecked == true;
            _appSettings!.SealNewGames = GetView().cbSealNewGames.IsChecked == true;
            _appSettings!.AllowProfileSpecificStyleChanges = GetView().cbAllowProfileSpecificStyleChanges.IsChecked == true;
            _appSettings!.BackgroundSource = GetSelectedBackgroundSource();
            _appSettings!.DarkTintedGlass = GetView().cbDarkTintedGlass.IsChecked == true;
            _appSettings!.SteamGridDbKey = GetView().txbSteamGridDbApiKey.Text;
            _appSettings!.BackupExportPath = GetView().txbBackupExportPath.Text;
            _appSettings!.AutoBackup = GetView().cbAutoBackup.IsChecked == true;
            _appSettings!.AutoDelete = GetView().cbAutoDeleteBackups.IsChecked == true;

            _appSettings!.EnableTwitchIGDB = GetView().cbEnableIGDBIntegration.IsChecked == true;
            _appSettings!.TwitchIGDBClientID = GetView().txbIgdbClientId.Text;
            _appSettings!.TwitchIGDBClientSecret = GetView().txbIgdbClientSecret.Text;

            _appSettings!.BreakReminder = GetView().cbRemindTakeBreak.IsChecked == true;
            _appSettings!.BreakReminderHrs = FnConvert.ToDouble(GetView().txbBreakReminderHours.Text);
        }

        private void FillDBOMonitoring()
        {
            _appSettings!.ShowToastNotification = GetView().cbShowToastNotification.IsChecked == true;
            _appSettings!.ShowMonitoringIndicator = GetView().cbShowMonitoringIndicator.IsChecked == true;
            _appSettings!.BlackoutSideMonitors = GetView().cbBlackoutSideMonitors.IsChecked == true;
            _appSettings!.MonitoringKeyActive = GetView().cbMonitoringKeyActive.IsChecked == true;
            _appSettings!.MonitoringKey = GetView().txbMonitoringKey.Text;
            _appSettings!.EnableSessionTimeQuery = GetView().cbEnableSessionTimeQuery.IsChecked == true;
            _appSettings!.EnableSessionCleanup = GetView().cbEnableSessionCleanup.IsChecked == true;
            _appSettings!.SessionCleanupSeconds = FnConvert.ToDouble(GetView().txbSessionCleanupSeconds.Text);
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
            GetView().txVersion.Text = $"v{AppEnvironment.AppVersion.InformationalVersion}";
        }

        protected override void SaveDBOImpl()
        {
            AppEnvironment.SaveAppConfig();
            _appSettings!.AcceptChanges();

            FnSystem.SetAutoStart(_appSettings!.AutoStartMinimized && !FnSystem.IsDebug(), true);
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

        protected void EV_cbAllowProfileSpecificStyleChanges()
        {

        }

        protected void EV_btnOpenSteamGridDB()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.steamgriddb.com/login",
                UseShellExecute = true
            });
        }

        protected void EV_btnOpenIgdbClientId()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://api-docs.igdb.com/#getting-started",
                UseShellExecute = true
            });
        }

        protected void EV_btnOpenIgdbClientSecret()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://api-docs.igdb.com/#getting-started",
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
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0014", "backup path", GetView().txbBackupExportPath.Text), CFMBOXResult.Ok, CFMBOXIcon.Error);
                return;
            }

            string exportPath = GetView().txbBackupExportPath.Text;

            if (FnSystem.IsDebug())
            {
                exportPath = AppConfig.Dev.BackupDirectoryPath;

                FnDirectory.EnsureDirectoryExists(exportPath);
            }

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
                        GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0012", "creating the backup"), CFMBOXResult.Ok, CFMBOXIcon.Error);
                    });

                    success = false;
                }
                finally
                {
                    GetApp().Loader.Stop();
                }
            });

            if (success)
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0011", "The backup"), CFMBOXResult.Ok, CFMBOXIcon.Success);
        }

        protected async Task EV_btnImportBackup()
        {
            if (!File.Exists(GetView().txbBackupImportPath.Text))
            {
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0014", "import file", GetView().txbBackupImportPath.Text), CFMBOXResult.Ok, CFMBOXIcon.Error);
                return;
            }

            CFMBOXResult result = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("SET0001", AppConfig.Root.ApplicationName), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Info);

            if (result == CFMBOXResult.Yes)
            {
                AppEnvironment.GetAppConfig().AppSettings.BackupType = BackupType.IMPORT_BACKUP;
                AppEnvironment.GetAppConfig().AppSettings.BackupImportPath = GetView().txbBackupImportPath.Text;

                // Save current settings
                SaveDBO();

                // Restart GameTimeNext
                AppEnvironment.RestartGTNApplication();
            }
        }

        protected void EV_btnSave()
        {
            // Default accent when disabled, the current profile's accent when enabled
            FnTheme.ApplyProfileAccentColors(AppEnvironment.GetCurrentProfile());

            FnTheme.ApplyWallpaperBackground(GetSelectedBackgroundSource());
            FnTheme.ApplyGlassTint(GetView().cbDarkTintedGlass.IsChecked == true);
        }
    }
}
