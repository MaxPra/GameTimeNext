using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Views;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles
{
    public class ProfilesSteamGridDBApp : UIXApplication
    {

        ProfilesSteamGridDBView? _profilesSteamGridDBView = null;
        ProfilesSteamGridDBViewController? _profilesSteamGridDBViewController = null;

        public long SteamAppId { get; set; }

        public override void InitializeApplicationOutput()
        {
            // View
            _profilesSteamGridDBView = new ProfilesSteamGridDBView();
            MainView = _profilesSteamGridDBView;

            // Controller
            _profilesSteamGridDBViewController = new ProfilesSteamGridDBViewController(this);
            _profilesSteamGridDBView.WndController = _profilesSteamGridDBViewController;
        }

        public void Search(UIXApplication hostApplication, long steamAppId, Action<ProfilesSteamGridDBViewController.ProfilesSteamGridDBViewReturn> callback)
        {
            SteamAppId = steamAppId;

            _profilesSteamGridDBView!.ViewIndicator.Add("ED");
            _profilesSteamGridDBViewController!.SetResultCallback(callback);
            _profilesSteamGridDBViewController!.Show(true);

        }
    }
}
