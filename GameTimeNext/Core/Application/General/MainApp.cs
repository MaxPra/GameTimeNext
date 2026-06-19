using GameTimeNext.Core.Application.General.Controller;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General
{
    public class MainApp : UIXApplication, IUIXApplicationStarter
    {


        MainWindowController? _mainWindowController;
        MainWindow? _mainWindow;

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
            _mainWindowController!.Show(false, true);
        }
    }
}
