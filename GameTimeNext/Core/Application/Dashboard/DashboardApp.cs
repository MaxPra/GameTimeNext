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
            this.HostApplication = hostApplication;
            this.DashboardView.ContentPresenter = presenter;
            this.DashboardView.ViewController.Show(false);
            Loader = hostApplication.Loader;
            CallDispatcher = hostApplication.CallDispatcher;
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
    }
}
