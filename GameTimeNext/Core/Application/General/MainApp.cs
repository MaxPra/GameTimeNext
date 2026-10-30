using GameTimeNext.Core.Application.General.Controller;
using GameTimeNext.Core.Application.General.Views;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Interop;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.General
{
    public class MainApp : UIXApplication, IUIXApplicationStarter
    {


        MainWindowController? _mainWindowController;
        MainWindow? _mainWindow;

        public FavAppsStartOrderView? FavAppsStartOrderView { get; set; } = null;

        public bool StartMinimized { get; set; } = false;
        public SplashScreen? SplashScreen { get; set; } = null;

        public MainApp() : base()
        {

        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(MainApp));
        }

        public override void InitializeApplicationOutput()
        {
            // -- Main-Window
            _mainWindow = new MainWindow();
            System.Windows.Application.Current.MainWindow = _mainWindow;

            MainView = _mainWindow;
            _mainWindowController = new MainWindowController(this);
            _mainWindow.WndController = _mainWindowController;

            // -- Favorites start order (popup of the tab bar)
            FavAppsStartOrderView = new FavAppsStartOrderView();
            FavAppsStartOrderView.ViewController = new FavAppsStartOrderViewController(this);
            FavAppsStartOrderView.ContentPresenter = _mainWindow.cpFavAppsStartOrder;
            FavAppsStartOrderView.Popup = _mainWindow.popFavAppsStartOrder;
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

            // The window is shown once (and hidden again) so the tabs get fully loaded.
            // Cloak it via DWM beforehand so this pre-load does not flicker on screen.
            SetMainWindowCloaked(true);

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

        /// <summary>
        /// Cloaks/uncloaks the main window via DWM. A cloaked window is laid out and
        /// rendered normally but is not drawn on screen.
        /// </summary>
        public void SetMainWindowCloaked(bool cloaked)
        {
            if (_mainWindow is null)
                return;

            try
            {
                IntPtr hwnd = new WindowInteropHelper(_mainWindow).EnsureHandle();
                int value = cloaked ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref value, sizeof(int));
            }
            catch (Exception ex)
            {
                FnLog.AddInfo(this, "Could not change cloak state of main window: " + ex.Message);
            }
        }

        private const int DWMWA_CLOAK = 13;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
