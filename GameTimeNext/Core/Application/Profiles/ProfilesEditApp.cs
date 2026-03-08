using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles
{
    public class ProfilesEditApp : UIXApplication
    {

        private ProfilesEditView? _profilesEditView = null;
        private ProfilesEditViewController? _profilesEditViewController = null;

        private T1PROFI _t1Profi = new T1PROFI();

        public T1PROFI T1Profi
        {
            get
            {
                return _t1Profi;
            }

            set
            {
                _t1Profi = value;
            }
        }

        public override void InitializeApplicationOutput()
        {
            _profilesEditView = new ProfilesEditView();
            MainView = _profilesEditView;

            _profilesEditViewController = new ProfilesEditViewController(this);
            _profilesEditView.WndController = _profilesEditViewController;
        }

        public void CreateNew(Action<ProfilesEditViewController.ProfilesEditViewReturn> callback)
        {
            _t1Profi = new TXPROFI().CreateNew();

            _profilesEditView!.ViewIndicator.Add("CN");
            _profilesEditViewController!.SetResultCallback(callback);
            _profilesEditViewController.Show(true);
        }

        public void Edit(T1PROFI t1profi, Action<ProfilesEditViewController.ProfilesEditViewReturn> callback)
        {
            _t1Profi = t1profi;

            _profilesEditView!.ViewIndicator.Add("ED");
            _profilesEditViewController!.SetResultCallback(callback);
            _profilesEditViewController.Show(true);
        }
    }
}
