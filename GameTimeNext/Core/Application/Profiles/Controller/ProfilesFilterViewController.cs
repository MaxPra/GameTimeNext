using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesFilterViewController : UIXViewControllerBase
    {
        private ProfilesFilterViewModel? _profilesFilterViewModel;

        public ProfilesFilterViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfileFilterViewReturn : UIXViewReturn
        {
            public bool Applied { get; set; } = false;
            public List<TBL_GROUP> TblGroups { get; set; } = new List<TBL_GROUP>();
            public List<TBL_GROUP> States { get; set; } = new List<TBL_GROUP>();
        }

        protected override void Init()
        {

            ViewReturn = new ProfileFilterViewReturn();
        }

        protected override void Build()
        {
        }

        protected override void BuildFirst()
        {
            // Gruppen-Liste (Tags u. States) befüllen
            BuildGroupList(string.Empty);
        }

        protected override void Check()
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        /// <summary>
        /// Button Apply
        /// </summary>
        protected void EV_BtnApply()
        {

            // View Return setzen
            GetViewReturn<ProfileFilterViewReturn>().TblGroups = _profilesFilterViewModel.Tbl_Groups.Where(g => (bool)g.IsSelected).ToList();
            GetViewReturn<ProfileFilterViewReturn>().States = _profilesFilterViewModel.States.Where(g => (bool)g.IsSelected).ToList();
            GetViewReturn<ProfileFilterViewReturn>().Applied = true;

            Exit();
        }

        /// <summary>
        /// Button Cancel
        /// </summary>
        protected void EV_BtnCancel()
        {
            GetViewReturn<ProfileFilterViewReturn>().Applied = false;

            Exit();
        }



        protected void EV_TxtSearchTag()
        {
            BuildGroupList(GetView().TxtSearchTag.Text);
        }

        /// <summary>
        /// Befüllt die Listbox Groups u. States
        /// </summary>
        private void BuildGroupList(string searchText)
        {
            TBLM_GROUP tblm_group = new TBLM_GROUP();

            List<TBL_GROUP> states = new List<TBL_GROUP>();
            List<TBL_GROUP> tbl_groups = tblm_group.ReadAll();

            // Filtern
            states = tbl_groups.Where(s => s.GTYP == GroupType.Condition).ToList();
            tbl_groups = tbl_groups.Where(s => s.GTYP == GroupType.Tag).ToList();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                tbl_groups = tbl_groups.Where(st => st.GRNA.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Viewmodel befüllen
            _profilesFilterViewModel = new ProfilesFilterViewModel();
            _profilesFilterViewModel.Tbl_Groups = new System.Collections.ObjectModel.ObservableCollection<TBL_GROUP>(tbl_groups);
            _profilesFilterViewModel.States = new System.Collections.ObjectModel.ObservableCollection<TBL_GROUP>(states);


            if (tbl_groups != null && tbl_groups.Count > 0)
                _profilesFilterViewModel.SelectedTBLGROUP = tbl_groups.FirstOrDefault(p => p.IsSelected == true);

            _profilesFilterViewModel.SelectedState = states.FirstOrDefault(s => s.IsSelected == true);

            View.DataContext = _profilesFilterViewModel;
        }

        private ProfilesFilterView GetView()
        {
            return (ProfilesFilterView)this.View;
        }
    }
}
