using GameTimeNext.Core.Application.Playthroughs.Controller;
using GameTimeNext.Core.Application.Playthroughs.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Playthroughs
{
    public class PlaythroughsApp : UIXApplication, IUIXApplicationStarter
    {
        public PlaythroughsView? PlaythroughsView { get; set; }
        public PlaythroughAppRunSelections AppRunSelections { get; set; } = new PlaythroughAppRunSelections();

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

            PlaythroughsView!.ViewController.Show(options);
        }

        public void ShowWindow()
        {
            PlaythroughsView!.Title = $"Playthroughs";
            PlaythroughsView.ViewIndicator.Add("ED");
            PlaythroughsView.ViewIndicator.Add("SE");

            UIXApplicationStartOptions options = new UIXApplicationStartOptions
            {
                Target = UIXApplicationStartTarget.Window
            };

            PlaythroughsView!.ViewController.Show(options);
        }

        public override void InitializeApplicationOutput()
        {
            PlaythroughsView = new PlaythroughsView();
            MainView = PlaythroughsView;
            PlaythroughsView.ViewController = new PlaythroughsViewController(this);

            Icon = UIXMdlIcons.Play;
        }

        public override bool CanClose()
        {
            return true;
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
            options.ResizeMode = System.Windows.ResizeMode.NoResize;
        }
    }
}
