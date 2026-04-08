using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Settings;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Files;
using GameTimeNext.Core.Framework.GitHub;
using GameTimeNext.Core.Framework.Utils;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace GameTimeNext
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Core.Application.General.SplashScreen splash = new Core.Application.General.SplashScreen();
            splash.Show();

            InitializeApp();

            splash.Close();

            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Erst danach Fenster öffnen
            MainApp mainApp = new MainApp();
            mainApp.Start(null, null);
        }

        private void InitializeApp()
        {
            // Ordner erstellen
            FileHandler.CreateApplicationFoldersAndFiles();

            AppEnvironment.InitiateDataBaseManager();

            // Datenbank initialisieren
            AppEnvironment.GetDataBaseManager().Initialize();

            // AppEnvironment initialisieren
            AppEnvironment.Initalize();

            // Alte Backups löschen
            FileHandler.DeleteOldBackupFiles();

            // Auf neue Version (Github) prüfen
            CheckForNewVersion();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //if (!CanCloseApplication())
            //{
            //    e.ApplicationExitCode = 1;
            //    return;
            //}

            AppEnvironment.StopBackgroundProcesses();

            if (AppEnvironment.GetAppConfig().AppSettings.AutoBackup)
            {
                string backupPath = AppEnvironment.GetAppConfig().AppSettings.BackupExportPath;

                if (FnSystem.IsDebug())
                {
                    backupPath = SpecialDirectories.MyDocuments + @"\GameTimeNext_Backup_dev";

                    if (!Directory.Exists(backupPath))
                        Directory.CreateDirectory(backupPath);
                }

                FnBackup.CreateBackupSync(backupPath, BackupType.APP_CLOSED_BACKUP);
            }


            if (AppEnvironment.GetDataBaseManager().GetConnection() != null)
                AppEnvironment.GetDataBaseManager().GetConnection().Close();
        }

        private bool CanCloseApplication()
        {
            foreach (var app in AppEnvironment.StartedApplications.Values)
            {
                if (!app.CanClose())
                    return false;
            }

            return true;
        }

        private void CheckForNewVersion()
        {
            UpdateCheckResult result = FnGithub.CheckForUpdateAsync(
                AppEnvironment.AppVersion.Version.ToString(),
                "MaxPra",
                "GameTimeNext"
            ).GetAwaiter().GetResult();

            if (result.UpdateAvailable)
            {

                InformationListItem infoItem = new InformationListItem(Core.Framework.UI.Dialogs.CFMBOXIcon.Question, "Version " + result.LatestVersion + " is available.\n\nDo you want to open GitHub to download now?");
                infoItem.Buttons = Core.Framework.UI.Dialogs.CFMBOXResult.Yes | Core.Framework.UI.Dialogs.CFMBOXResult.No;
                infoItem.YesAction = () =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = result.ReleaseUrl,
                        UseShellExecute = true
                    });
                };

                AppEnvironment.InformationList.Add(infoItem);
            }
        }
    }

}
