using GameTimeNext.Core.Application.General.Controller;
using GameTimeNext.Core.Framework.Logging;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General
{
    public class MainApp : UIXApplication, IUIXApplicationStarter
    {


        MainWindowController? _mainWindowController;
        MainWindow? _mainWindow;

        public bool StartMinimized { get; set; } = false;
        public SplashScreen? SplashScreen { get; set; } = null;

        public MainApp() : base()
        {

        }

        public override void InitializeApplicationOutput()
        {
            // -- Main-Window
            _mainWindow = new MainWindow();
            System.Windows.Application.Current.MainWindow = _mainWindow;

            MainView = _mainWindow;
            _mainWindowController = new MainWindowController(this);
            _mainWindow.WndController = _mainWindowController;
        }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {

            if (StartMinimized)
                FnLog.AddInfo(this, "Starting minimized because of parameter '--minimized'");

            _mainWindowController!.Show(false, true);
        }
    }
}
