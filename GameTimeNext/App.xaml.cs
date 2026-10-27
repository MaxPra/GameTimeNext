using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.MigrationTasks;
using GameTimeNext.Core.Application.Settings;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Files;
using GameTimeNext.Core.Framework.GitHub;
using GameTimeNext.Core.Framework.Utils;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

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

            // Ensure that only one instance is running
            if (!FnSystem.IsDebug() && !FnSystem.TryAcquireSingleInstance())
            {
                Shutdown();
                return;
            }

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

            // Only open the window afterwards
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
            FnLog.Configure(AppConfig.LogFilePath);

            // FileSystem Migration
            MigTask_100b_008.MigrateFileSystem();

            // Create folders
            FileHandler.CreateDevAppFolder();

            AppEnvironment.LoadAppConfig();

            // Blurred background image (desktop wallpaper or profile cover) as base of the window background (setting)
            FnTheme.ApplyWallpaperBackground(AppEnvironment.GetAppConfig().AppSettings.BackgroundSource);
            FnTheme.ApplyGlassTint(AppEnvironment.GetAppConfig().AppSettings.DarkTintedGlass);

            FnLog.AddInfo("MainApp", "*** Initializing Application... ***");

            FnLog.AddInfo("MainApp", "Initiating Databasemanager...");
            AppEnvironment.InitiateDataBaseManager();

            // Initialize database
            FnLog.AddInfo("MainApp", "Initializing database...");
            AppEnvironment.GetDataBaseManager().Initialize();

            // Initialize AppEnvironment
            FnLog.AddInfo("MainApp", "Initializing application environment...");
            AppEnvironment.Initalize();

            // Only now: T1APRDH exists after the metadata migration in Initalize; earlier applications get the fallback properties
            UIXApplication.AppPropertiesProvider = new AppPropertiesProvider();

            // Same for T1ERMSG: error messages are only available from here on
            UIXApplication.ErrorMessageProvider = new ErrorMessageProvider();

            // Delete old backups
            FnLog.AddInfo("MainApp", "Deleting old backups...");
            FileHandler.DeleteOldBackupFiles();

            // Start argument -optimizecovers: shrink existing profile covers
            if (AppEnvironment.StartArguments.ContainsKey("optimizecovers"))
                OptimizeProfileCovers();

            // Check for new version (GitHub)
            FnLog.AddInfo("MainApp", "Checking for new version (Github)...");
            CheckForNewVersion();
        }

        private void OptimizeProfileCovers()
        {
            FnLog.AddInfo("MainApp", "Optimizing profile covers...");

            (int optimizedCount, long savedBytes) = Core.Application.Profiles.CFProfilesEditApp.OptimizeAllProfileCovers();

            string message = $"{optimizedCount} profile cover(s) optimized, {savedBytes / 1024.0 / 1024.0:0.0} MB saved.";

            FnLog.AddInfo("MainApp", message);
            AppEnvironment.InformationList.Add(InformationListItem.FromErrorMessage(Core.Framework.UI.Dialogs.CFMBOXIcon.Info, "PRO0007", optimizedCount, Math.Round(savedBytes / 1024.0 / 1024.0, 1)));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            FnLog.AddInfo(null, "*** Shutdown initiated ***");

            FnLog.AddInfo(null, "Stopping background processes...");
            AppEnvironment.StopBackgroundProcesses();

            if (AppEnvironment.GetAppConfig().AppSettings.AutoBackup)
            {
                string backupPath = AppEnvironment.GetAppConfig().AppSettings.BackupExportPath;

                if (FnSystem.IsDebug())
                {
                    backupPath = AppConfig.Dev.BackupDirectoryPath;
                }

                FnLog.AddInfo(null, "Creating backup at: " + backupPath);

                FnBackup.CreateBackupSync(backupPath, BackupType.APP_CLOSED_BACKUP);
            }

            FnLog.AddInfo(null, "Closing database connection...");
            if (AppEnvironment.GetDataBaseManager().GetConnection() != null)
                AppEnvironment.GetDataBaseManager().GetConnection().Close();

            FnLog.AddInfo(null, "*** Shutdown completed ***");
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

                InformationListItem infoItem = InformationListItem.FromErrorMessage(Core.Framework.UI.Dialogs.CFMBOXIcon.Question, "GLO0021", result.LatestVersion);
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
