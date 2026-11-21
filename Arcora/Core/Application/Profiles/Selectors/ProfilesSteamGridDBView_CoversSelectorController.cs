using Arcora.Core.Application.Profiles.Views;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace Arcora.Core.Application.Profiles.Selectors
{
    public class ProfilesSteamGridDBView_CoversSelectorController : UIXSelectorControllerBase<ProfilesSteamGridDBApp, ProfilesSteamGridDBView, UIXListBox>
    {
        public ProfilesSteamGridDBView_CoversSelectorController(UIXApplication app) : base(app)
        {
        }
    }
}
