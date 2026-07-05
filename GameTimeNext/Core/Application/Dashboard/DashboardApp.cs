using GameTimeNext.Core.Application.Dashboard.Controller;
using GameTimeNext.Core.Application.Dashboard.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Dashboard
{
    public class DashboardApp : UIXApplication, IUIXApplicationStarter
    {

        public DashboardView DashboardView { get; set; }

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

            DashboardView.ViewController.Show(options);
        }

        public override void InitializeApplicationOutput()
        {
            this.DashboardView = new DashboardView();
            this.MainView = DashboardView;
            this.DashboardView.ViewController = new DashboardViewController(this);

            Icon = UIXMdlIcons.Dashboard;
        }

        public override bool CanClose()
        {
            return true;
        }

        public override void OnFocus()
        {
            DashboardView.ViewController.Open(true);
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
        }
    }
}
