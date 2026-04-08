using GameTimeNext.Core.Application.General.AppSearch.Controller;
using GameTimeNext.Core.Application.General.AppSearch.Views;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.AppSearch
{
    public class AppSearchApp : UIXApplication
    {

        public AppSearchView? AppSearchView { get; set; } = null;
        public AppSearchViewController? AppSearchViewController { get; set; } = null;

        public override void InitializeApplicationOutput()
        {
            AppSearchView = new AppSearchView();
            MainView = AppSearchView;

            AppSearchViewController = new AppSearchViewController(this);

            AppSearchView.WndController = AppSearchViewController;
        }

        public void Search()
        {
            AppSearchView!.ViewIndicator.Add("ED");
            AppSearchViewController!.Show(true);
        }
    }
}
