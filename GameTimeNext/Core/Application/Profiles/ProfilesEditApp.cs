using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesManualEstTimesEditViewController;

namespace GameTimeNext.Core.Application.Profiles
{
    public class ProfilesEditApp : UIXApplication
    {

        private ProfilesEditView? _profilesEditView = null;
        private ProfilesEditViewController? _profilesEditViewController = null;

        private ProfilesManualEstTimesEditView? _profilesManualEstTimesEditView = null;
        private ProfilesManualEstTimesEditViewController? _profilesManualEstTimesEditViewController = null;

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

        public ProfilesManualEstTimesEditView ProfilesManualEstTimesEditView
        {
            get => _profilesManualEstTimesEditView!;
            set => _profilesManualEstTimesEditView = value;
        }

        public ProfilesManualEstTimesCache ManualEstTimesCache { get; set; } = new ProfilesManualEstTimesCache();

        public override void InitializeApplicationOutput()
        {
            _profilesEditView = new ProfilesEditView();
            MainView = _profilesEditView;

            _profilesEditViewController = new ProfilesEditViewController(this);
            _profilesEditView.WndController = _profilesEditViewController;

            _profilesManualEstTimesEditView = new ProfilesManualEstTimesEditView();
            _profilesManualEstTimesEditViewController = new ProfilesManualEstTimesEditViewController(this);
            _profilesManualEstTimesEditView.WndController = _profilesManualEstTimesEditViewController;
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
