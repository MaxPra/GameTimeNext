using GameTimeNext.Core.Application.General.Controller;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.Settings;
using System.Windows.Controls;
using UIX.ViewController.Engine.BuiltInApplications.MetaDataManagerApp;
using UIX.ViewController.Engine.BuiltInApplications.MetaDataManagerApp.Controller;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General
{
    public class MainApp : UIXApplication, IUIXApplicationStarter
    {
        public MetaDataViewController MetaDataViewController { get; set; }

        public ProfilesApp ProfilesApp { get => _profileApp; set => _profileApp = value; }
        public SettingsApp SettingsApp { get => _settingsApp; set => _settingsApp = value; }



        MainWindowController? _mainWindowController;
        MainWindow? _mainWindow;

        ProfilesApp _profileApp;
        SettingsApp _settingsApp;

        MetaDataApp _metadataApp;

        public MainApp() : base()
        {

        }

        public override void InitializeApplicationOutput()
        {
            // -- Main-Window
            _mainWindow = new MainWindow();
            MainView = _mainWindow;
            _mainWindowController = new MainWindowController(this);
            _mainWindow.WndController = _mainWindowController;
        }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            _mainWindowController.Show(false);
        }
    }
}
