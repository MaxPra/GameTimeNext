using GameTimeNext.Core.Application.BigPicture.Controller;
using GameTimeNext.Core.Application.BigPicture.Views;
using System.Windows.Controls;
using System.Windows.Input;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.BigPicture
{
    /// <summary>
    /// Big Picture mode: the game library in full screen, made for a TV and a game controller.
    /// Shown by the main window as a layer over its tabs (MainWindowController.EnterBigPicture).
    /// </summary>
    public class BigPictureApp : UIXApplication, IUIXApplicationStarter
    {
        public BigPictureView BigPictureView { get; set; } = null!;

        public bool IsOpen => BigPictureView.IsShown;

        public BigPictureApp() : base()
        {
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(BigPictureApp));
        }

        public override void InitializeApplicationOutput()
        {
            BigPictureView = new BigPictureView();
            MainView = BigPictureView;
            BigPictureView.ViewController = new BigPictureViewController(this);
        }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            Start(hostApplication, new UIXApplicationStartOptions
            {
                Target = UIXApplicationStartTarget.ContentPresenter,
                Presenter = presenter
            });
        }

        public void Start(UIXApplication hostApplication, UIXApplicationStartOptions options)
        {
            HostApplication = hostApplication;
            Loader = hostApplication.Loader;
            CallDispatcher = hostApplication.CallDispatcher;

            BigPictureView.ViewController.Show(options);
        }

        /// <summary>
        /// Closes the Big Picture view (the main window switches back to its normal mode).
        /// </summary>
        public void Close()
        {
            GetController().Deactivate();
            BigPictureView.CloseView();
        }

        /// <summary>
        /// Keyboard input of the main window while the mode is open. Returns true if the key was used.
        /// </summary>
        public bool HandleKey(KeyEventArgs e)
        {
            return GetController().HandleKey(e);
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
        }

        private BigPictureViewController GetController()
        {
            return (BigPictureViewController)BigPictureView.ViewController;
        }
    }
}
