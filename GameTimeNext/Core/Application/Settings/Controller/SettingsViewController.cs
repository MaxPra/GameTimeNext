using System.Globalization;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.General.BackgroundProcesses;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.Settings.Viewmodels;
using GameTimeNext.Core.Application.Settings.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Files;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.UserInput;
using GameTimeNext.Core.Framework.Utils;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Settings.Controller
{
    internal class SettingsViewController : UIXViewControllerBase<SettingsApp, SettingsView>
    {

        private AppSettings? _appSettings = null;

        private bool _hasIGDBChanged = false;

        // Launchers taking part in the automatic game import as saved; a launcher added to them asks once for its installed games
        private List<string> _autoImportLauncherTypes = new List<string>();

        // "Add new games shrink-wrapped" was switched off while games are still sealed: they are unwrapped on save
        private bool _unsealAllOnSave = false;

        // The remote monitoring was switched on: its web app is allowed in the Windows firewall on save
        private bool _allowRemoteMonitoringOnSave = false;

        // Firewall rules are system wide, but every Windows user has their own installation (per-user install) and the
        // development build its own web app: one rule each, otherwise switching it on replaces the rule of the other one
        private static string RemoteMonitoringFirewallRule => FnSystem.IsDebug()
            ? $"{AppConfig.Root.ApplicationName} Remote Monitoring (Dev)"
            : $"{AppConfig.Root.ApplicationName} Remote Monitoring ({Environment.UserName})";

        // Added to the hint of the extended remote monitoring logging in debug, where it can't be switched off
        private const string DebugLoggingHint = " Always on in the development build.";

        public SettingsViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            // Set loader texts
            GetApp().Loader.SetRandomTexts(
                                            "Loading settings...",
                                            "Adjusting the knobs...",
                                            "Checking your preferences...",
                                            "Fine-tuning everything for you...",
                                            "Reading your personal setup...",
                                            "Making everything just the way you like it..."
                                            );

            _appSettings = AppEnvironment.GetAppConfig().AppSettings;
            _appSettings.ResetChangeTracking();
            _appSettings.AcceptChanges();

            _autoImportLauncherTypes = CFProfilesApp.GetAutoImportLauncherTypes(_appSettings);

            GetApp().CallDispatcher.Register(this, nameof(EXEV_RemoteMonitoringPortChanged));
        }

        protected override async Task BuildFirstImplAsync()
        {
            await GetView().lbAutoImportLaunchers.LoadDataAsync();
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
            FillComboboxProfileDetailViewPosition();
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

            FnControls.SetEnabled(GetView().cbRemoteMonitoringBlackout, GetView().cbRemoteMonitoringActive.IsChecked == true);

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

            FnControls.SetEnabled(GetView().lbAutoImportLaunchers, GetView().cbAutoImportGames.IsChecked == true);
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

            if (GetView().cbAutoImportGames.IsChecked == true && !GetAutoImportLauncherItems().Any(i => i.COISSEL))
            {
                AddViewError(GetView().lbAutoImportLaunchers, GetErrorMessage("GLO0003", "A launcher"));
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
            FillDBOSealNewGames();
            FillDBOGeneral();
            FillDBOMonitoring();
            FillDBOAdvanced();
            FillDBOAbout();


        }

        protected override async Task FillDBOImplAsync()
        {
            await FillDBOAutoImportAsync();
        }

        /// <summary>
        /// Settings of the automatic game import. A launcher that takes part now but didn't before asks once what
        /// happens with its installed games without a profile (add them all, or only games installed from now on).
        /// The answer is registered (TFLNGAM) before the settings change, so LauncherAutoImportProcess never sees
        /// the launcher without it.
        /// </summary>
        private async Task FillDBOAutoImportAsync()
        {
            bool autoImportGames = GetView().cbAutoImportGames.IsChecked == true;
            string excludedLaunchers = CFProfilesApp.JoinLauncherTypes(GetAutoImportLauncherItems().Where(i => !i.COISSEL).Select(i => i.LauncherType));

            List<string> addedLauncherTypes = CFProfilesApp.GetAutoImportLauncherTypes(autoImportGames, excludedLaunchers)
                .Except(_autoImportLauncherTypes)
                .ToList();

            if (addedLauncherTypes.Count > 0)
                await RegisterInstalledGamesAsync(addedLauncherTypes);

            _appSettings!.AutoImportGames = autoImportGames;
            _appSettings!.AutoImportExcludedLaunchers = excludedLaunchers;
        }

        /// <summary>
        /// Switching off "Add new games shrink-wrapped" unwraps the games that are still sealed (on save), because
        /// without the setting they can't be unwrapped any more. The user is told how many games that are.
        /// </summary>
        private void FillDBOSealNewGames()
        {
            _unsealAllOnSave = false;

            if (!_appSettings!.SealNewGames || GetView().cbSealNewGames.IsChecked == true)
                return;

            int sealedCount = TFPROFI.GetSealedProfiles(GetApp()).Count;

            if (sealedCount == 0)
                return;

            _unsealAllOnSave = true;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
            cfmbox.Show(GetErrorMessage("SET0003", sealedCount), CFMBOXResult.Ok, CFMBOXIcon.Info);
        }

        /// <summary>
        /// Unwraps all sealed games and lets an open profiles view unwrap their tiles.
        /// </summary>
        private void UnsealAllProfiles()
        {
            TFPROFI.UnsealAll(GetApp());

            // The current profile is saved elsewhere too (e.g. playtime), it must not bring the shrink wrap back
            T1PROFI? currentProfile = AppEnvironment.GetCurrentProfile();
            if (currentProfile != null)
                TFPROFI.RemoveSealedFlag(currentProfile, clearNewMark: false);

            if (GetApp().CallDispatcher.IsRegistered("EXEV_ProfilesUnsealed"))
                GetApp().CallDispatcher.Trigger("EXEV_ProfilesUnsealed");
        }

        private async Task RegisterInstalledGamesAsync(List<string> launcherTypes)
        {
            List<LauncherGame> installedGames;

            GetApp().Loader.Begin("Looking for your installed games...");

            try
            {
                installedGames = await Task.Run(() => FnLauncher.GetInstalledGames(launcherTypes));
            }
            finally
            {
                GetApp().Loader.Stop();
            }

            int unregisteredCount = TFLNGAM.GetUnregisteredGames(installedGames).Count;
            string status = "SKIP";

            if (unregisteredCount > 0)
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
                CFMBOXResult result = cfmbox.Show(GetErrorMessage("PRO0009", unregisteredCount, AppConfig.Root.ApplicationName), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

                if (result == CFMBOXResult.Yes)
                    status = "PEND";
            }

            TFLNGAM.RegisterGames(GetApp(), installedGames, status);
        }

        private List<SettingsAutoImportLauncherListItem> GetAutoImportLauncherItems()
        {
            return GetView().lbAutoImportLaunchers.Items.OfType<SettingsAutoImportLauncherListItem>().ToList();
        }

        protected override void FillViewImpl()
        {
            FillTabGeneral();
            FillTabMonitoring();
            FillTabAdvanced();
            FillTabAbout();
        }

        private void FillTabGeneral()
        {
            GetView().cbActivateBlackoutKeyCombination.IsChecked = _appSettings!.ActivateBlackoutKeyCombination;
            GetView().cbShowTextOnBlackout.IsChecked = _appSettings.EnableFullBlackoutText;
            GetView().cbAutoStartMinimized.IsChecked = _appSettings.AutoStartMinimized;
            GetView().cbSealNewGames.IsChecked = _appSettings.SealNewGames;
            GetView().cbPauseGameInBigPicture.IsChecked = _appSettings.PauseGameInBigPicture;
            GetView().cbAutoImportGames.IsChecked = _appSettings.AutoImportGames;
            GetView().cbAllowProfileSpecificStyleChanges.IsChecked = _appSettings!.AllowProfileSpecificStyleChanges;
            GetView().cbDarkTintedGlass.IsChecked = _appSettings.DarkTintedGlass;
            GetView().cmbBackgroundSource.SelectedValue = _appSettings.BackgroundSource;
            if (GetView().cmbBackgroundSource.SelectedItem == null)
                GetView().cmbBackgroundSource.SelectedValue = string.Empty;
            GetView().cmbProfileDetailViewPosition.SelectedValue = _appSettings.ProfileDetailViewPosition;
            if (GetView().cmbProfileDetailViewPosition.SelectedItem == null)
                GetView().cmbProfileDetailViewPosition.SelectedValue = "02";
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
            // The additional entry "(None)" (set in the view) disables the dynamic background
            UIXManualCodetable uIXManualCodetable = new UIXManualCodetable();

            TXCTABD txctabd = GetApp().GetDataManager<TXCTABD>();
            List<T1CTABD> entrys = txctabd.GetEntries(BackgroundSource.CODETABLE);

            foreach (T1CTABD entry in entrys)
            {
                uIXManualCodetable.AddEntry(entry.TXNUM, entry.DESCR);
            }

            uIXManualCodetable.ApplyTo(GetView().cmbBackgroundSource);
        }

        private void FillComboboxProfileDetailViewPosition()
        {
            UIXManualCodetable uIXManualCodetable = new UIXManualCodetable();

            TXCTABD txctabd = GetApp().GetDataManager<TXCTABD>();
            List<T1CTABD> entrys = txctabd.GetEntries("dP");

            foreach (T1CTABD entry in entrys)
            {
                uIXManualCodetable.AddEntry(entry.TXNUM, entry.DESCR);
            }

            uIXManualCodetable.ApplyTo(GetView().cmbProfileDetailViewPosition);
        }

        private string GetSelectedBackgroundSource()
        {
            return GetView().cmbBackgroundSource.SelectedValue as string ?? string.Empty;
        }

        private void FillTabMonitoring()
        {
            GetView().cbMonitoringKeyActive.IsChecked = _appSettings!.MonitoringKeyActive;
            GetView().cbRemoteMonitoringActive.IsChecked = _appSettings!.RemoteMonitoring;
            GetView().cbRemoteMonitoringBlackout.IsChecked = _appSettings!.RemoteMonitoringBlackout;
            GetView().cbShowToastNotification.IsChecked = _appSettings!.ShowToastNotification;
            GetView().cbShowMonitoringIndicator.IsChecked = _appSettings!.ShowMonitoringIndicator;
            GetView().cbBlackoutSideMonitors.IsChecked = _appSettings!.BlackoutSideMonitors;
            GetView().txbMonitoringKey.Text = _appSettings!.MonitoringKey;
            GetView().cbEnableSessionTimeQuery.IsChecked = _appSettings!.EnableSessionTimeQuery;
            GetView().cbEnableSessionCleanup.IsChecked = _appSettings!.EnableSessionCleanup;
            GetView().txbSessionCleanupSeconds.Text = _appSettings!.SessionCleanupSeconds.ToString(CultureInfo.CurrentCulture);

            FillRemoteMonitoringPairing();
        }

        /// <summary>
        /// QR code and address of the remote monitoring (only while it is on), see CFRemoteMonitoring.GetPairing.
        /// </summary>
        private void FillRemoteMonitoringPairing(int? port = null)
        {
            SettingsView view = GetView();
            FnControls.SetVisible(view.grdRemoteMonitoringPairing, _appSettings!.RemoteMonitoring);

            if (!_appSettings.RemoteMonitoring)
                return;

            // Port just reported by the web app's start, otherwise the current one (null while it isn't running)
            int? webAppPort = port ?? CFRemoteMonitoring.GetWebAppPort();

            CFRemoteMonitoring.RemoteMonitoringPairing pairing = CFRemoteMonitoring.GetPairing(webAppPort);
            FnControls.SetVisible(view.brdRemoteMonitoringQr, pairing.QrCode is not null);
            view.imgRemoteMonitoringQr.Source = pairing.QrCode;
            view.txtRemoteMonitoringAddress.Text = pairing.Text;
            view.txtRemoteMonitoringPairingCode.Text = pairing.PairingCode;
        }

        protected void EV_btnResetPairing()
        {
            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
            if (cfmbox.Show(GetErrorMessage("SET0006"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question) != CFMBOXResult.Yes)
                return;

            CFRemoteMonitoring.ResetPairingCode();
            FnLog.AddInfo(GetApp(), "Pairing of the remote monitoring reset: every phone has to scan the new QR code.");

            FillRemoteMonitoringPairing();
        }

        private void FillDBOGeneral()
        {
            _appSettings!.ActivateBlackoutKeyCombination = GetView().cbActivateBlackoutKeyCombination.IsChecked == true;
            _appSettings!.EnableFullBlackoutText = GetView().cbShowTextOnBlackout.IsChecked == true;
            _appSettings!.AutoStartMinimized = GetView().cbAutoStartMinimized.IsChecked == true;
            _appSettings!.SealNewGames = GetView().cbSealNewGames.IsChecked == true;
            _appSettings!.PauseGameInBigPicture = GetView().cbPauseGameInBigPicture.IsChecked == true;
            _appSettings!.AllowProfileSpecificStyleChanges = GetView().cbAllowProfileSpecificStyleChanges.IsChecked == true;
            _appSettings!.BackgroundSource = GetSelectedBackgroundSource();
            _appSettings!.DarkTintedGlass = GetView().cbDarkTintedGlass.IsChecked == true;
            _appSettings!.ProfileDetailViewPosition = GetView().cmbProfileDetailViewPosition.SelectedValue as string ?? "02";
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
            bool remoteMonitoring = GetView().cbRemoteMonitoringActive.IsChecked == true;
            _allowRemoteMonitoringOnSave = remoteMonitoring && !_appSettings!.RemoteMonitoring;

            // Switching on waits for the firewall rule in SaveDBO: RemoteMonitoringProcess starts the web app as soon as
            // the setting is on, and a web app listening before the rule exists gets the Windows firewall prompt
            if (!_allowRemoteMonitoringOnSave)
                _appSettings!.RemoteMonitoring = remoteMonitoring;
            _appSettings!.RemoteMonitoringBlackout = GetView().cbRemoteMonitoringBlackout.IsChecked == true;
            _appSettings!.MonitoringKey = GetView().txbMonitoringKey.Text;
            _appSettings!.EnableSessionTimeQuery = GetView().cbEnableSessionTimeQuery.IsChecked == true;
            _appSettings!.EnableSessionCleanup = GetView().cbEnableSessionCleanup.IsChecked == true;
            _appSettings!.SessionCleanupSeconds = FnConvert.ToDouble(GetView().txbSessionCleanupSeconds.Text);
        }

        private void FillDBOAdvanced()
        {
            // Always on in debug: the switch only shows that, the stored setting of the release stays as it is
            if (!FnSystem.IsDebug())
                _appSettings!.ExtendedRemoteMonitoringLogging = GetView().cbExtendedRemoteMonitoringLogging.IsChecked == true;
        }

        private void FillTabAdvanced()
        {
            SettingsView view = GetView();
            bool isDebug = FnSystem.IsDebug();

            view.cbExtendedRemoteMonitoringLogging.IsChecked = isDebug || _appSettings!.ExtendedRemoteMonitoringLogging;
            FnControls.SetEnabled(view.cbExtendedRemoteMonitoringLogging, !isDebug);

            if (isDebug && !view.txtExtendedRemoteMonitoringLoggingHint.Text.EndsWith(DebugLoggingHint, StringComparison.Ordinal))
                view.txtExtendedRemoteMonitoringLoggingHint.Text += DebugLoggingHint;
        }

        private void FillDBOAbout()
        {
        }

        private void FillTabAbout()
        {
            GetView().txVersion.Text = $"v{AppEnvironment.AppVersion.InformationalVersion}";
        }

        protected override void SaveDBOImpl()
        {
            // Rule first, then the setting: the web app only starts once it is allowed in the firewall. "Cancel" on the
            // Windows prompt would even add a block rule for it, which beats the allow rule.
            if (_allowRemoteMonitoringOnSave)
            {
                _allowRemoteMonitoringOnSave = false;
                AllowRemoteMonitoringInFirewall();
                _appSettings!.RemoteMonitoring = true;
            }

            AppEnvironment.SaveAppConfig();
            _appSettings!.AcceptChanges();

            FnSystem.SetAutoStart(_appSettings!.AutoStartMinimized && !FnSystem.IsDebug(), true);

            // Pausing switched off: a game paused by the Big Picture mode right now must not stay suspended
            if (!_appSettings.PauseGameInBigPicture)
                FnLauncher.ResumePausedGame();

            _autoImportLauncherTypes = CFProfilesApp.GetAutoImportLauncherTypes(_appSettings);

            if (_unsealAllOnSave)
            {
                _unsealAllOnSave = false;
                UnsealAllProfiles();
            }

            // Switched on or off: show or hide the QR code, here and in the title bar of the main window
            FillRemoteMonitoringPairing();
            GetApp().CallDispatcher.Trigger("EXEV_RemoteMonitoringSwitched", _appSettings.RemoteMonitoring); // MainWindowController
        }

        private void AllowRemoteMonitoringInFirewall()
        {
            string? webAppPath = RemoteMonitoringProcess.FindWebAppExecutable();
            if (webAppPath is null)
            {
                FnLog.AddWarning(GetApp(), "No firewall rule for the remote monitoring: its web app was not found.");
                return;
            }

            if (FnFirewall.IsProgramAllowed(RemoteMonitoringFirewallRule, webAppPath))
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
            cfmbox.Show(GetErrorMessage("SET0004", AppConfig.Root.ApplicationName), CFMBOXResult.Ok, CFMBOXIcon.Info);

            FirewallRuleResult result = FnFirewall.AllowProgramInbound(RemoteMonitoringFirewallRule, webAppPath, "private");

            if (result is FirewallRuleResult.Cancelled or FirewallRuleResult.Failed)
            {
                CFMBOX failed = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
                failed.Show(GetErrorMessage("SET0005", AppConfig.Root.ApplicationName), CFMBOXResult.Ok, CFMBOXIcon.Warning);
            }
        }

        protected void EXEV_RemoteMonitoringPortChanged(int port)
        {
            FillRemoteMonitoringPairing(port);
        }

        protected void EV_btnCaptureMonitoringKey()
        {

            GetApp().Loader.Begin("Enter desired monitor key");

            GlobalKeyInputProcess globalKeyInputProcess = GetApp().GetBackgroundProcess<GlobalKeyInputProcess>();
            globalKeyInputProcess.StartingType = StartingTypes.MonitorKey;

            globalKeyInputProcess.AfterKeyPressed = (k) =>
            {
                GetApp().CallDispatcher.Invoke(() =>
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
                    GetApp().CallDispatcher.Invoke(() =>
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

            // The import replaces the whole data folder (FnBackup.ImportBackup): can't be undone, the button has to be held
            if (GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).ConfirmDestructive(GetErrorMessage("SET0001", AppConfig.Root.ApplicationName), "Import Backup"))
            {
                AppEnvironment.GetAppConfig().AppSettings.BackupType = BackupType.IMPORT_BACKUP;
                AppEnvironment.GetAppConfig().AppSettings.BackupImportPath = GetView().txbBackupImportPath.Text;

                // Save current settings
                SaveDBO();

                // Restart GameTimeNext
                AppEnvironment.RestartGTNApplication();
            }
        }

        /// <summary>
        /// Search field: shows only the pages, cards and settings that match.
        /// </summary>
        protected void EV_txbSearch()
        {
            bool anyShown = SettingsSearch.Apply(GetView().tbcSettings, GetView().txbSearch.Text);

            FnControls.SetVisible(GetView().tbcSettings, anyShown);
            FnControls.SetVisible(GetView().txSearchEmpty, !anyShown);
        }

        /// <summary>
        /// Refresh: shows the saved settings again, unsaved changes are dropped (the settings object only changes on save).
        /// </summary>
        protected async Task EV_btnRefresh()
        {
            FillViewImpl();
            await GetView().lbAutoImportLaunchers.LoadDataAsync();
            BuildImpl();
        }

        protected void EV_btnSave()
        {
            // Default accent when disabled, the current profile's accent when enabled
            FnTheme.ApplyProfileAccentColors(AppEnvironment.GetCurrentProfile());

            FnTheme.ApplyWallpaperBackground(GetSelectedBackgroundSource());
            FnTheme.ApplyGlassTint(GetView().cbDarkTintedGlass.IsChecked == true);

            // An open profiles view moves its detail view to the chosen side
            if (GetApp().CallDispatcher.IsRegistered("EXEV_ProfileDetailViewPositionChanged"))
                GetApp().CallDispatcher.Trigger("EXEV_ProfileDetailViewPositionChanged");
        }
    }
}
