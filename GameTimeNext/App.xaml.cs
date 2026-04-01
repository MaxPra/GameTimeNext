using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Settings;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Files;
using GameTimeNext.Core.Framework.Utils;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.Windows;

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
        }

        protected override void OnExit(ExitEventArgs e)
        {
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
    }

}
