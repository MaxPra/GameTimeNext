using GameTimeNext.Core.Application.Diagnostics.Views;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Diagnostics.Selectors
{
    /// <summary>
    /// Events of the background process list. Processes can only be viewed, so there are no handlers yet.
    /// </summary>
    public class DiagnosticsView_BackgroundProcessesSelectorController : UIXSelectorControllerBase<DiagnosticsApp, DiagnosticsView, UIXDataGrid>
    {
        public DiagnosticsView_BackgroundProcessesSelectorController(UIXApplication app) : base(app)
        {
        }
    }
}
