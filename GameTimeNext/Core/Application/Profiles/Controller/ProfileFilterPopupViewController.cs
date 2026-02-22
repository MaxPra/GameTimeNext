using GameTimeNext.Core.Application.DataManagers;
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
    public class ProfileFilterPopupViewController : UIXViewControllerBase
    {

        private ProfilesFilterViewModel? _profilesFilterViewModel;

        protected override void Init()
        {
            TBLM_GROUP tblm_group = new TBLM_GROUP();
            List<TBL_GROUP> tbl_groups = tblm_group.ReadAll();

            // ----- TEST -------
            tbl_groups[0].IsSelected = true;
            tbl_groups[1].IsSelected = false;
            tbl_groups[5].IsSelected = true;

            List<TBL_GROUP> states = tbl_groups.Where(s => s.GTYP == GroupType.Condition).ToList();
            tbl_groups = tbl_groups.Where(s => s.GTYP == GroupType.Tag).ToList();


            _profilesFilterViewModel = new ProfilesFilterViewModel();
            _profilesFilterViewModel.Tbl_Groups = new System.Collections.ObjectModel.ObservableCollection<TBL_GROUP>(tbl_groups);
            _profilesFilterViewModel.States = new System.Collections.ObjectModel.ObservableCollection<TBL_GROUP>(states);

            if (tbl_groups != null && tbl_groups.Count > 0)
                _profilesFilterViewModel.SelectedTBLGROUP = tbl_groups.FirstOrDefault(p => p.IsSelected == true);

            _profilesFilterViewModel.SelectedState = states.FirstOrDefault(s => s.IsSelected == true);

            View.DataContext = _profilesFilterViewModel;
        }

        protected override void Build()
        {
        }

        protected override void BuildFirst()
        {
            Console.WriteLine("BuildFirst");
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
            switch (eventName)
            {
                case UIXEventNames.Button.Click:
                    if (source is Button && source.Name == "BtnApply")
                    {
                        GetView().CloseView();
                    }
                    break;
            }
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        private ProfilesFilterPopupView GetView()
        {
            return (ProfilesFilterPopupView)this.View;
        }
    }
}
