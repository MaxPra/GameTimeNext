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

        private TBL_PROFI _tblProfi = new TBL_PROFI();

        public TBL_PROFI TblProfi
        {
            get
            {
                return _tblProfi;
            }

            set
            {
                _tblProfi = value;
            }
        }

        public override void InitializeApplicationOutput()
        {
            _profilesEditView = new ProfilesEditView();
            MainView = _profilesEditView;

            _profilesEditViewController = new ProfilesEditViewController(this);
            _profilesEditView.WndController = _profilesEditViewController;
        }

        public void CreateNew(UIXApplication hostApplication)
        {
            _tblProfi = new TBLM_PROFI().CreateNew();

            _profilesEditView.ViewIndicator.Add("CN");
            _profilesEditViewController.Show(true);
        }
    }
}
