using GameTimeNext.Core.Application.General.Controller;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Framework;
using System.Windows.Controls;
using UIX.ViewController.Engine.BuiltInApplications.MetaDataManagerApp;
using UIX.ViewController.Engine.BuiltInApplications.MetaDataManagerApp.Controller;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General
{
    public class MainApp : UIXApplication, IUIXApplicationStarter
    {
        public MetaDataViewController MetaDataViewController { get; set; }

        public ProfilesApp ProfilesApp { get => _profileApp; private set; }


        MainWindowController? _mainWindowController;
        MainWindow? _mainWindow;

        ProfilesApp _profileApp;
        MetaDataApp _metadataApp;

        public MainApp() : base()
        {

        }

        public override void InitializeApplicationOutput()
        {
            // -- Main-Window
            _mainWindow = new MainWindow();
            _mainWindowController = new MainWindowController(this);
            _mainWindow.WndController = _mainWindowController;
        }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            _profileApp = new ProfilesApp();
            _profileApp.Start(this, _mainWindow.CPProfileView);

            _metadataApp = new MetaDataApp(AppEnvironment.GetDataBaseManager().GetConnection());
            _metadataApp.Start(this, _mainWindow.CpMetadata);

            _mainWindow.Show();
        }
    }
}
