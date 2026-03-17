using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles
{
    public class ProfilesPlaythroughEditApp : UIXApplication
    {

        private ProfilesPlaythroughEditViewController? _profilesPlaythroughEditViewController = null;
        private ProfilesPlaythroughEditView? _profilesPlaythroughEditView = null;

        public T1PROFI T1profi { get; set; }

        public override void InitializeApplicationOutput()
        {
            _profilesPlaythroughEditView = new ProfilesPlaythroughEditView();
            MainView = _profilesPlaythroughEditView;

            _profilesPlaythroughEditViewController = new ProfilesPlaythroughEditViewController(this);
            _profilesPlaythroughEditView.WndController = _profilesPlaythroughEditViewController;
        }

        public void CreateNew(T1PROFI t1profi, Action<ProfilesPlaythroughEditViewController.ProfilesPlaythroughEditViewReturn> callback)
        {
            T1profi = t1profi;

            _profilesPlaythroughEditView!.ViewIndicator.Add("CN");
            _profilesPlaythroughEditViewController.SetResultCallback(callback);
            _profilesPlaythroughEditViewController!.Show(true);
        }
    }
}
