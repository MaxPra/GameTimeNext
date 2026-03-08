using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesEditViewModel : GTNViewModelBase
    {
        public ObservableCollection<T1GROUP> T1GROUPs { get; set; }

        private T1GROUP _selectedT1GROUP;

        public T1GROUP SelectedTBLGROUP
        {
            get => _selectedT1GROUP;
            set => SetProperty(ref _selectedT1GROUP, value);
        }

        public ProfilesEditViewModel()
        {
            T1GROUPs = new ObservableCollection<T1GROUP>();

        }
    }
}
