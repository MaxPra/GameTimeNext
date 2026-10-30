using GameTimeNext.Core.Application.General.AppSearch.Controller;
using GameTimeNext.Core.Application.General.AppSearch.Selectors;
using GameTimeNext.Core.Application.General.AppSearch.Views;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.AppSearch
{
    public class AppSearchApp : UIXApplication
    {

        public AppSearchView? AppSearchView { get; set; } = null;
        public AppSearchViewController? AppSearchViewController { get; set; } = null;

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(AppSearchApp));
        }

        public override void InitializeApplicationOutput()
        {
            AppSearchView = new AppSearchView();
            MainView = AppSearchView;

            AppSearchViewController = new AppSearchViewController(this);

            AppSearchView.ViewController = AppSearchViewController;
            AppSearchView.dgApplications.SelectorAdapter = new AppSearchView_ApplicationsSelectorAdapter(this);
            AppSearchView.dgApplications.SelectorController = new AppSearchView_ApplicationsSelectorController(this);
        }

        public void Search(UIXApplicationStartTarget? startTarget = null)
        {
            AppSearchView!.ViewIndicator.Add("ED");
            AppSearchViewController!.Show();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
