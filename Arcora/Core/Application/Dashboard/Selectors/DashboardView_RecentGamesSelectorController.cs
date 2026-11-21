using GameTimeNext.Core.Application.Dashboard.Views;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Dashboard.Selectors
{
    public class DashboardView_RecentGamesSelectorController : UIXSelectorControllerBase<DashboardApp, DashboardView, UIXListView>
    {
        public DashboardView_RecentGamesSelectorController(UIXApplication app) : base(app)
        {
        }
    }
}
