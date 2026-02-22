using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesFilterViewModel : GTNViewModelBase
    {
        public ObservableCollection<TBL_GROUP> Tbl_Groups { get; set; }
        public ObservableCollection<TBL_GROUP> States { get; set; }

        private TBL_GROUP _selectedTBLGROUP;
        private TBL_GROUP _selectedState;

        public TBL_GROUP SelectedTBLGROUP
        {
            get => _selectedTBLGROUP;
            set => SetProperty(ref _selectedTBLGROUP, value);
        }

        public TBL_GROUP SelectedState
        {
            get => _selectedState;
            set => SetProperty(ref _selectedState, value);
        }

        public ProfilesFilterViewModel()
        {
            Tbl_Groups = new ObservableCollection<TBL_GROUP>();
            States = new ObservableCollection<TBL_GROUP>();

        }
    }
}
