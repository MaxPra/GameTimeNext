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
            public List<T1GROUP> TblGroups { get; set; } = new List<T1GROUP>();
            public List<T1GROUP> States { get; set; } = new List<T1GROUP>();
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
            GetViewReturn<ProfileFilterViewReturn>().TblGroups = _profilesFilterViewModel.T1GROUPs.Where(g => (bool)g.IsSelected).ToList();
            GetViewReturn<ProfileFilterViewReturn>().States = _profilesFilterViewModel.States.Where(g => (bool)g.IsSelected).ToList();
            GetViewReturn<ProfileFilterViewReturn>().Applied = true;

            Exit(true);
        }

        /// <summary>
        /// Button Cancel
        /// </summary>
        protected void EV_BtnCancel()
        {
            GetViewReturn<ProfileFilterViewReturn>().Applied = false;

            Exit(true);
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
            TXGROUP TXGROUP = new TXGROUP();

            List<T1GROUP> states = new List<T1GROUP>();
            List<T1GROUP> T1GROUPs = TXGROUP.ReadAll();

            // Filtern
            states = T1GROUPs.Where(s => s.GTYP == GroupType.Condition).ToList();
            T1GROUPs = T1GROUPs.Where(s => s.GTYP == GroupType.Tag).ToList();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                T1GROUPs = T1GROUPs.Where(st => st.GRNA.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Viewmodel befüllen
            _profilesFilterViewModel = new ProfilesFilterViewModel();
            _profilesFilterViewModel.T1GROUPs = new System.Collections.ObjectModel.ObservableCollection<T1GROUP>(T1GROUPs);
            _profilesFilterViewModel.States = new System.Collections.ObjectModel.ObservableCollection<T1GROUP>(states);


            if (T1GROUPs != null && T1GROUPs.Count > 0)
                _profilesFilterViewModel.SelectedT1GROUP = T1GROUPs.FirstOrDefault(p => p.IsSelected == true);

            _profilesFilterViewModel.SelectedState = states.FirstOrDefault(s => s.IsSelected == true);

            View.DataContext = _profilesFilterViewModel;
        }

        private ProfilesFilterView GetView()
        {
            return (ProfilesFilterView)this.View;
        }
    }
}
