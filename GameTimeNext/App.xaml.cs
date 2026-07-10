using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Settings;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Files;
using GameTimeNext.Core.Framework.GitHub;
using GameTimeNext.Core.Framework.Logging;
using GameTimeNext.Core.Framework.Utils;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using UIX.ViewController.Engine.Runnables;

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

            FnSystem.ParseStartArguments(e.Args);
            bool startMinimized = AppEnvironment.StartArguments.ContainsKey("minimized");

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            AppEnvironment.AppVersion.Get();
            Core.Application.General.SplashScreen? splash = null;

            if (!startMinimized)
            {
                splash = new Core.Application.General.SplashScreen();
                splash.Show();
            }

            InitializeApp();

            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

            // Erst danach Fenster öffnen
            MainApp mainApp = new MainApp();
            mainApp.SplashScreen = splash;
            mainApp.StartMinimized = startMinimized;
            mainApp.Start(mainApp, new UIXApplicationStartOptions
            {
                Target = UIXApplicationStartTarget.Window,
                ShowHidden = startMinimized
            });
        }

        private void InitializeApp()
        {

            // Ordner erstellen
            FileHandler.CreateApplicationFoldersAndFiles();

            AppEnvironment.LoadAppConfig();

            FnLog.Configure(AppEnvironment.GetAppConfig().LogFilePath);

            FnLog.AddInfo("MainApp", "*** Initializing Application... ***");

            FnLog.AddInfo("MainApp", "Initiating Databasemanager...");
            AppEnvironment.InitiateDataBaseManager();

            FnLog.AddInfo("MainApp", "Initializing database...");
            // Datenbank initialisieren
            AppEnvironment.GetDataBaseManager().Initialize();

            FnLog.AddInfo("MainApp", "Initializing application environment...");
            // AppEnvironment initialisieren
            AppEnvironment.Initalize();

            FnLog.AddInfo("MainApp", "Deleting old backups...");
            // Alte Backups löschen
            FileHandler.DeleteOldBackupFiles();

            FnLog.AddInfo("MainApp", "Checking for new version (Github)...");
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

            FnLog.AddInfo(null, "*** Shutdown initiated ***");

            FnLog.AddInfo(null, "Stopping background processes...");
            AppEnvironment.StopBackgroundProcesses();

            if (AppEnvironment.GetAppConfig().AppSettings.AutoBackup)
            {
                string backupPath = AppEnvironment.GetAppConfig().AppSettings.BackupExportPath;

                if (FnSystem.IsDebug())
                {
                    backupPath = AppEnvironment.GetAppConfig().DevBackupFolderPath;

                    if (!Directory.Exists(backupPath))
                        Directory.CreateDirectory(backupPath);
                }

                FnLog.AddInfo(null, "Creating backup at: " + backupPath);

                FnBackup.CreateBackupSync(backupPath, BackupType.APP_CLOSED_BACKUP);
            }

            FnLog.AddInfo(null, "Closing database connection...");
            if (AppEnvironment.GetDataBaseManager().GetConnection() != null)
                AppEnvironment.GetDataBaseManager().GetConnection().Close();

            FnLog.AddInfo(null, "*** Shutdown completed ***");
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

                    AppEnvironment.ShutdownGTNApplication();

                };

                AppEnvironment.InformationList.Add(infoItem);
            }
        }
    }

}
