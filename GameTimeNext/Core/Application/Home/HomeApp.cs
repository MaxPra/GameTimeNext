using GameTimeNext.Core.Application.Home.Controller;
using GameTimeNext.Core.Application.Home.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Home
{
    /// <summary>
    /// Welcome page: what the user can do next and the help of the main modules. Opened on the first start and from the menu.
    /// </summary>
    [UIXAppIcon(UIXMdlIcons.Home)]
    public class HomeApp : UIXApplication, IUIXApplicationStarter
    {
        public HomeView HomeView { get; set; }

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

            HomeView.ViewController.Show(options);
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(HomeApp));
        }

        public override void InitializeApplicationOutput()
        {
            this.HomeView = new HomeView();
            this.MainView = HomeView;
            this.HomeView.ViewController = new HomeViewController(this);
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
        }
    }
}
