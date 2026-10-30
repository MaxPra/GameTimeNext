using GameTimeNext.Core.Application.Screensetups.Views;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Screensetups.Selectors
{
    public class ScreensetupsEditView_MonitorsSelectorController : UIXSelectorControllerBase<ScreensetupsEditApp, ScreensetupsEditView, UIXDataGrid>
    {
        public ScreensetupsEditView_MonitorsSelectorController(UIXApplication app) : base(app)
        {
        }
    }
}
