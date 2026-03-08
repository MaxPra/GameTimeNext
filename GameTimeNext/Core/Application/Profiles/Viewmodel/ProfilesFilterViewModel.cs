using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesFilterViewModel : GTNViewModelBase
    {
        public ObservableCollection<T1GROUP> T1GROUPs { get; set; }
        public ObservableCollection<T1GROUP> States { get; set; }

        private T1GROUP _selectedT1GROUP;
        private T1GROUP _selectedState;

        public T1GROUP SelectedT1GROUP
        {
            get => _selectedT1GROUP;
            set => SetProperty(ref _selectedT1GROUP, value);
        }

        public T1GROUP SelectedState
        {
            get => _selectedState;
            set => SetProperty(ref _selectedState, value);
        }

        public ProfilesFilterViewModel()
        {
            T1GROUPs = new ObservableCollection<T1GROUP>();
            States = new ObservableCollection<T1GROUP>();

        }
    }
}
