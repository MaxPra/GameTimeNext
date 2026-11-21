using GameTimeNext.Core.Application.General.AppSearch.Views;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.AppSearch.Selectors
{
    public class AppSearchView_ApplicationsSelectorController : UIXSelectorControllerBase<AppSearchApp, AppSearchView, UIXTreeGrid>
    {
        public AppSearchView_ApplicationsSelectorController(UIXApplication app) : base(app)
        {
        }
    }
}
