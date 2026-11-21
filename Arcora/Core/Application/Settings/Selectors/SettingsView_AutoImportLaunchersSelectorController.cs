using GameTimeNext.Core.Application.Settings.Views;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Settings.Selectors
{
    public class SettingsView_AutoImportLaunchersSelectorController : UIXSelectorControllerBase<SettingsApp, SettingsView, UIXListBox>
    {
        public SettingsView_AutoImportLaunchersSelectorController(UIXApplication app) : base(app)
        {
        }
    }
}
