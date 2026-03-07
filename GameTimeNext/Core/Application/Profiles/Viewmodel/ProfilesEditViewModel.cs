using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesEditViewModel : GTNViewModelBase
    {
        public ObservableCollection<TBL_GROUP> Tbl_Groups { get; set; }

        private TBL_GROUP _selectedTBLGROUP;

        public TBL_GROUP SelectedTBLGROUP
        {
            get => _selectedTBLGROUP;
            set => SetProperty(ref _selectedTBLGROUP, value);
        }

        public ProfilesEditViewModel()
        {
            Tbl_Groups = new ObservableCollection<TBL_GROUP>();

        }
    }
}
