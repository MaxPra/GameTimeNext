using Arcora.Core.Application.Profiles.Controller;
using Arcora.Core.Application.Profiles.Views;
using UIX.ViewController.Engine.Runnables;

namespace Arcora.Core.Application.Profiles
{
    internal class ProfilesExecutablesEditApp : UIXApplication
    {

        public string GameFolder { get; set; }

        private ProfilesExecutablesEditView? _profilesEditExecutablesView = null;
        private ProfilesExecutablesEditViewController? _profilesEditExecutablesViewController = null;

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ProfilesExecutablesEditApp));
        }

        public override void InitializeApplicationOutput()
        {
            _profilesEditExecutablesView = new ProfilesExecutablesEditView();
            MainView = _profilesEditExecutablesView;

            _profilesEditExecutablesViewController = new ProfilesExecutablesEditViewController(this);
            _profilesEditExecutablesView.ViewController = _profilesEditExecutablesViewController;
        }

        public void Search(string gameFolder, Action<ProfilesExecutablesEditViewController.ProfilesExecutablesEditViewReturn> callback)
        {
            GameFolder = gameFolder;


            _profilesEditExecutablesView!.ViewIndicator.Add("ED");
            _profilesEditExecutablesViewController!.SetResultCallback(callback);

            _profilesEditExecutablesViewController.Show();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
