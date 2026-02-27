using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesViewController : UIXViewControllerBase
    {
        private ProfilesSubViewDataWrapper? dataWrapper;

        ProfilesSubGridViewModel? _profilesSubGridViewModel;

        public ProfilesViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            dataWrapper = GetDataWrapper<ProfilesSubViewDataWrapper>();
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirst()
        {
            TBLM_PROFI tblm_profi = new TBLM_PROFI();
            List<TBL_PROFI> tbl_profis = tblm_profi.ReadAll();

            _profilesSubGridViewModel = new ProfilesSubGridViewModel();
            _profilesSubGridViewModel.Tbl_Profis = new System.Collections.ObjectModel.ObservableCollection<TBL_PROFI>(tbl_profis);

            if (tbl_profis.Count > 0)
                _profilesSubGridViewModel.SelectedTBLPROFI = tbl_profis.FirstOrDefault(p => p.PFID == AppEnvironment.GetCurrentProfile().PFID) ?? tbl_profis.FirstOrDefault();

            dataWrapper.TableObject = _profilesSubGridViewModel.SelectedTBLPROFI;

            View.DataContext = _profilesSubGridViewModel;

        }

        protected override void Build()
        {

        }

        protected override void Check()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {

        }

        /// <summary>
        /// Button Filter
        /// </summary>
        public void EV_BtnFilter()
        {
            List<TBL_GROUP> groups = new List<TBL_GROUP>();
            bool applied = false;

            if (!GetApp().ProfilesFilterView.IsShown)
            {
                GetApp().ProfilesFilterView.ShowView(false);

                // Geschlossen per Apply
                GetApp().ProfilesFilterView.ViewController.SetResultCallback<ProfilesFilterViewController.ProfileFilterViewReturn>(r =>
                {
                    groups = r.TblGroups;
                    applied = r.Applied;

                    if (applied)
                        MessageBox.Show("Applied!!!");
                    else
                        MessageBox.Show("Canceled :(");
                });
            }

            else
                GetApp().ProfilesFilterView.CloseView();
        }

        private ProfilesApp GetApp()
        {
            return (ProfilesApp)App;
        }
    }
}
