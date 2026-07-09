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
            Start(hostApplication, new UIXApplicationStartOptions
            {
                Target = UIXApplicationStartTarget.Window,
                ShowHidden = StartMinimized
            });
        }

        public void Start(UIXApplication hostApplication, UIXApplicationStartOptions options)
        {
            UIXApplication startupHost = hostApplication ?? this;

            HostApplication = startupHost;
            Loader = startupHost.Loader;
            CallDispatcher = startupHost.CallDispatcher;

            if (StartMinimized)
                FnLog.AddInfo(this, "Starting minimized because of parameter '--minimized'");

            _mainWindowController!.Show(new UIXApplicationStartOptions
            {
                Target = UIXApplicationStartTarget.Window,
                Dialog = options.Dialog,
                ShowHidden = StartMinimized || options.ShowHidden,
                Owner = options.Owner,
                WindowTitle = options.WindowTitle
            }, showHidden: true);
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
        }
    }
}
