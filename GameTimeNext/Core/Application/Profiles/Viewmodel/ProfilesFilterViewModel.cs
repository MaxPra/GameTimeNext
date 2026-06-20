using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesFilterViewModel : GTNViewModelBase
    {
        public ObservableCollection<ProfilesGroupListBoxItem> T1GROUPs { get; set; }
        public ObservableCollection<ProfilesGroupListBoxItem> States { get; set; }

        private ProfilesGroupListBoxItem _selectedT1GROUP;
        private ProfilesGroupListBoxItem _selectedState;

        public ProfilesGroupListBoxItem SelectedT1GROUP
        {
            get => _selectedT1GROUP;
            set => SetProperty(ref _selectedT1GROUP, value);
        }

        public ProfilesGroupListBoxItem SelectedState
        {
            get => _selectedState;
            set => SetProperty(ref _selectedState, value);
        }

        public ProfilesFilterViewModel()
        {
            T1GROUPs = new ObservableCollection<ProfilesGroupListBoxItem>();
            States = new ObservableCollection<ProfilesGroupListBoxItem>();

        }
    }
}
