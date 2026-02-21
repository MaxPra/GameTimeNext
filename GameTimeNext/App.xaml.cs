using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.GTXMigration;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.DataBase;
using GameTimeNext.Core.Framework.Files;
using System.Configuration;
using System.Data;
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

            // Test Migration GTX => GTNXT
            //GTXMigrationHelper gtxMigHelper = new GTXMigrationHelper("C:\\GameTimeX");
            //gtxMigHelper.MigrateToGTNXT();

            AppEnvironment.SetCurrentProfile(new TBLM_PROFI().Read(10));

            // Erst danach Fenster öffnen
            MainWindow window = new MainWindow();
            window.Show();
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
    }

}
