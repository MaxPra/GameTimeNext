using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Files;
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

            InitializeApp();

            // Erst danach Fenster öffnen
            MainApp mainApp = new MainApp();
            mainApp.Start(null, null);
        }

        private void InitializeApp()
        {
            // Ordner erstellen
            FileHandler.CreateApplicationFoldersAndFiles();

            // AppEnvironment initialisieren
            AppEnvironment.Initalize();

            // Datenbank initialisieren
            AppEnvironment.GetDataBaseManager().Initialize();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppEnvironment.StopBackgroundProcesses();

            if (AppEnvironment.GetDataBaseManager().GetConnection() != null)
                AppEnvironment.GetDataBaseManager().GetConnection().Close();
        }
    }

}
