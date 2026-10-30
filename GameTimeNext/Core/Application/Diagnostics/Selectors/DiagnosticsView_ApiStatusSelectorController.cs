using GameTimeNext.Core.Application.Diagnostics.Views;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Diagnostics.Selectors
{
    /// <summary>
    /// Events of the API status list. The status can only be viewed, so there are no handlers yet.
    /// </summary>
    public class DiagnosticsView_ApiStatusSelectorController : UIXSelectorControllerBase<DiagnosticsApp, DiagnosticsView, UIXDataGrid>
    {
        public DiagnosticsView_ApiStatusSelectorController(UIXApplication app) : base(app)
        {
        }
    }
}
