using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfileSubViewController : UIXViewControllerBase
    {
        private ProfilesSubViewDataWrapper? dataWrapper;

        ProfilesSubGridViewModel? profilesSubGridViewModel;

        protected override void Init()
        {

            dataWrapper = GetDataWrapper<ProfilesSubViewDataWrapper>();

            TBLM_PROFI tblm_profi = new TBLM_PROFI();
            List<TBL_PROFI> tbl_profis = tblm_profi.ReadAll();

            profilesSubGridViewModel = new ProfilesSubGridViewModel();
            profilesSubGridViewModel.Tbl_Profis = new System.Collections.ObjectModel.ObservableCollection<TBL_PROFI>(tbl_profis);

            if (tbl_profis.Count > 0)
                profilesSubGridViewModel.SelectedTBLPROFI = tbl_profis[0];

            View.DataContext = profilesSubGridViewModel;

            dataWrapper.SetTableObject(profilesSubGridViewModel.SelectedTBLPROFI);

            if (dataWrapper != null)
            {
                AddIdentifier("TBL_PROFI", dataWrapper.GetTypedTableObject());
            }
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {

        }

        protected override void BuildFirst()
        {

        }

        protected override void Build()
        {

        }

        protected override void Check()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {

        }

        private ProfilesSubView GetView()
        {
            return (ProfilesSubView)View;
        }


    }
}
