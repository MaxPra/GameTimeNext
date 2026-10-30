using GameTimeNext.Core.Application.Screensetups.Controller;
using GameTimeNext.Core.Application.Screensetups.Selectors;
using GameTimeNext.Core.Application.Screensetups.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Screensetups
{
    [UIXAppIcon(UIXMdlIcons.Monitor)]
    public class ScreensetupsApp : UIXApplication, IUIXApplicationStarter
    {

        public ScreensetupsView? ScreensetupsView { get; set; }

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

            ScreensetupsView!.ViewIndicator.Clear();
            ScreensetupsView!.ViewIndicator.Add("ED");
            ScreensetupsView!.ViewController.Show(options);
        }

        public void StartWindowed()
        {
            ScreensetupsView!.ViewIndicator.Clear();
            ScreensetupsView!.ViewIndicator.Add("ED");

            // Fixed width: the view sizes itself (Width Auto) and the sub window is not resizable
            ScreensetupsView!.ViewController.Show(
                new UIXApplicationStartOptions
                {
                    Target = UIXApplicationStartTarget.Window,
                    WindowWidth = 1100
                });
        }


        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ScreensetupsApp));
        }

        public override void InitializeApplicationOutput()
        {
            ScreensetupsView = new ScreensetupsView();
            MainView = ScreensetupsView;
            ScreensetupsView!.ViewController = new ScreensetupsViewController(this);
            ScreensetupsView.DgScreensetups.SelectorAdapter = new ScreensetupsView_ScreensetupsSelectorAdapter(this);
            ScreensetupsView.DgScreensetups.SelectorController = new ScreensetupsView_ScreensetupsSelectorController(this);
        }

        public override bool CanClose()
        {
            return base.CanClose();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
