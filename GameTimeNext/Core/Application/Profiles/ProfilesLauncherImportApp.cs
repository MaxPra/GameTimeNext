using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Selectors;
using GameTimeNext.Core.Application.Profiles.Views;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles
{
    public class ProfilesLauncherImportApp : UIXApplication
    {

        private ProfilesLauncherImportView? _profilesLauncherImportView = null;
        private ProfilesLauncherImportViewController? _profilesLauncherImportViewController = null;

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ProfilesLauncherImportApp));
        }

        public override void InitializeApplicationOutput()
        {
            _profilesLauncherImportView = new ProfilesLauncherImportView();
            MainView = _profilesLauncherImportView;

            _profilesLauncherImportViewController = new ProfilesLauncherImportViewController(this);

            _profilesLauncherImportView.ViewController = _profilesLauncherImportViewController;
            _profilesLauncherImportView.dgGames.SelectorAdapter = new ProfilesLauncherImportView_GamesSelectorAdapter(this);
            _profilesLauncherImportView.dgGames.SelectorController = new ProfilesLauncherImportView_GamesSelectorController(this);
        }

        public void Search(Action<ProfilesLauncherImportViewController.ProfilesLauncherImportViewReturn> callback)
        {
            _profilesLauncherImportView!.ViewIndicator.Add("ED");

            _profilesLauncherImportViewController!.SetResultCallback(callback);
            _profilesLauncherImportViewController.Show();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }

    }
}
