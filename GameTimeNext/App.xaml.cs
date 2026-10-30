using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.MigrationTasks;
using GameTimeNext.Core.Application.Settings;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Files;
using GameTimeNext.Core.Framework.GitHub;
using GameTimeNext.Core.Framework.LauncherIntegration;
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
        private LogProvider _logProvider = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure that only one instance is running
            if (!FnSystem.IsDebug() && !FnSystem.TryAcquireSingleInstance())
            {
                Shutdown();
                return;
            }

            // A crash must not leave a game suspended by the Big Picture mode (OnExit doesn't run then)
            AppDomain.CurrentDomain.UnhandledException += (_, _) => FnLauncher.ResumePausedGame();

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

            // From here on the module "Diagnostics" can be opened: log entries only go to the text file if the log database fails
            _logProvider.StartupCompleted = true;
        }

        private void InitializeApp()
        {
            // Log target of FnLog (engine); first thing, so everything after this is logged
            AppEnvironment.GetLogDataBaseManager().Initialize();
            _logProvider = new LogProvider();
            FnLog.LogProvider = _logProvider;

            // FileSystem Migration
            MigTask_100b_008.MigrateFileSystem();

            // Create folders
            FileHandler.CreateDevAppFolder();

            AppEnvironment.LoadAppConfig();

            // Blurred background image (desktop wallpaper or profile cover) as base of the window background (setting)
            FnTheme.ApplyWallpaperBackground(AppEnvironment.GetAppConfig().AppSettings.BackgroundSource);
            FnTheme.ApplyGlassTint(AppEnvironment.GetAppConfig().AppSettings.DarkTintedGlass);

            FnLog.Add(GTNLogType.AppStart, "MainApp", "*** Initializing Application... ***");

            FnLog.Add(GTNLogType.AppStart, "MainApp", "Initiating Databasemanager...");
            AppEnvironment.InitiateDataBaseManager();

            // Initialize database
            FnLog.Add(GTNLogType.AppStart, "MainApp", "Initializing database...");
            AppEnvironment.GetDataBaseManager().Initialize();

            // Initialize AppEnvironment
            FnLog.Add(GTNLogType.AppStart, "MainApp", "Initializing application environment...");
            AppEnvironment.Initalize();

            // Only now: T1APRDH exists after the metadata migration in Initalize; earlier applications get the fallback properties
            UIXApplication.AppPropertiesProvider = new AppPropertiesProvider();

            // Same for T1ERMSG: error messages are only available from here on
            UIXApplication.ErrorMessageProvider = new ErrorMessageProvider();

            // Delete old backups
            FnLog.Add(GTNLogType.AppStart, "MainApp", "Deleting old backups...");
            FileHandler.DeleteOldBackupFiles();

            // Start argument -optimizecovers: shrink existing profile covers
            if (AppEnvironment.StartArguments.ContainsKey("optimizecovers"))
                OptimizeProfileCovers();

            // Check for new version (GitHub)
            FnLog.Add(GTNLogType.AppStart, "MainApp", "Checking for new version (Github)...");
            CheckForNewVersion();
        }

        private void OptimizeProfileCovers()
        {
            FnLog.Add(GTNLogType.AppStart, "MainApp", "Optimizing profile covers...");

            (int optimizedCount, long savedBytes) = Core.Application.Profiles.CFProfilesEditApp.OptimizeAllProfileCovers();

            string message = $"{optimizedCount} profile cover(s) optimized, {savedBytes / 1024.0 / 1024.0:0.0} MB saved.";

            FnLog.Add(GTNLogType.AppStart, "MainApp", message);
            AppEnvironment.InformationList.Add(InformationListItem.FromErrorMessage(Core.Framework.UI.Dialogs.CFMBOXIcon.Info, "PRO0007", optimizedCount, Math.Round(savedBytes / 1024.0 / 1024.0, 1)));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            FnLog.AddInfo("MainApp", "*** Shutdown initiated ***");

            // A game paused by the Big Picture mode must never stay suspended
            FnLauncher.ResumePausedGame();

            FnLog.AddInfo("MainApp", "Stopping background processes...");
            AppEnvironment.StopBackgroundProcesses();

            if (AppEnvironment.GetAppConfig().AppSettings.AutoBackup)
            {
                string backupPath = AppEnvironment.GetAppConfig().AppSettings.BackupExportPath;

                if (FnSystem.IsDebug())
                {
                    backupPath = AppConfig.Dev.BackupDirectoryPath;
                }

                FnLog.AddInfo("MainApp", "Creating backup at: " + backupPath);

                FnBackup.CreateBackupSync(backupPath, BackupType.APP_CLOSED_BACKUP);
            }

            FnLog.AddInfo("MainApp", "Closing database connection...");
            if (AppEnvironment.GetDataBaseManager().GetConnection() != null)
                AppEnvironment.GetDataBaseManager().GetConnection().Close();

            FnLog.AddInfo("MainApp", "*** Shutdown completed ***");

            // Last: the log provider may write into the log database up to here
            AppEnvironment.GetLogDataBaseManager().Close();
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
