using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesEditViewModel : GTNViewModelBase
    {
        public ObservableCollection<ProfilesGroupListBoxItem> T1GROUPs { get; set; }

        private ProfilesGroupListBoxItem _selectedT1GROUP;

        public ProfilesGroupListBoxItem SelectedTBLGROUP
        {
            get => _selectedT1GROUP;
            set => SetProperty(ref _selectedT1GROUP, value);
        }

        public ProfilesEditViewModel()
        {
            T1GROUPs = new ObservableCollection<ProfilesGroupListBoxItem>();

        }
    }
}
