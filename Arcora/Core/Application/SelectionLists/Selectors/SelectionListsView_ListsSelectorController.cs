using Arcora.Core.Application.SelectionLists.Controller;
using Arcora.Core.Application.SelectionLists.Views;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace Arcora.Core.Application.SelectionLists.Selectors
{
    public class SelectionListsView_ListsSelectorController : UIXSelectorControllerBase<SelectionListsApp, SelectionListsView, UIXDataGrid>
    {
        public SelectionListsView_ListsSelectorController(UIXApplication app) : base(app)
        {
        }

        protected async Task EV_SelectionChanged()
        {
            await GetViewController<SelectionListsViewController>().ShowSelectedListAsync();
        }
    }
}
