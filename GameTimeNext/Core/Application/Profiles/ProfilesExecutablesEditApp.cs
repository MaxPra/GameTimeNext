using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Views;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles
{
    internal class ProfilesExecutablesEditApp : UIXApplication
    {

        public string GameFolder { get; set; }

        private ProfilesExecutablesEditView? _profilesEditExecutablesView = null;
        private ProfilesExecutablesEditViewController? _profilesEditExecutablesViewController = null;

        public override void InitializeApplicationOutput()
        {
            _profilesEditExecutablesView = new ProfilesExecutablesEditView();
            MainView = _profilesEditExecutablesView;

            _profilesEditExecutablesViewController = new ProfilesExecutablesEditViewController(this);
            _profilesEditExecutablesView.WndController = _profilesEditExecutablesViewController;
        }

        public void Search(string gameFolder, Action<ProfilesExecutablesEditViewController.ProfilesExecutablesEditViewReturn> callback)
        {
            GameFolder = gameFolder;


            _profilesEditExecutablesView!.ViewIndicator.Add("ED");
            _profilesEditExecutablesViewController!.SetResultCallback(callback);

            _profilesEditExecutablesViewController!.Show(true);
        }
    }
}
