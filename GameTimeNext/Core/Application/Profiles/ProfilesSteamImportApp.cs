using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Views;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles
{
    public class ProfilesSteamImportApp : UIXApplication
    {

        private ProfilesSteamImportView? _profilesSteamImportView = null;
        private ProfilesSteamImportViewController? _profilesSteamImportViewController = null;

        public override void InitializeApplicationOutput()
        {
            _profilesSteamImportView = new ProfilesSteamImportView();
            MainView = _profilesSteamImportView;

            _profilesSteamImportViewController = new ProfilesSteamImportViewController(this);

            _profilesSteamImportView.ViewController = _profilesSteamImportViewController;
        }

        public void Search(Action<ProfilesSteamImportViewController.ProfilesSteamImportViewRetrun> callback)
        {
            _profilesSteamImportView!.ViewIndicator.Add("ED");

            _profilesSteamImportViewController!.SetResultCallback(callback);
            _profilesSteamImportViewController.Show();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }

    }
}
