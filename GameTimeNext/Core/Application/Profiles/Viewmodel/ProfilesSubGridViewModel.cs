using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesSubGridViewModel : GTNViewModelBase
    {
        public ObservableCollection<TBL_PROFI> Tbl_Profis { get; set; }

        private TBL_PROFI _selectedTBLPROFI;
        public TBL_PROFI SelectedTBLPROFI
        {
            get => _selectedTBLPROFI;
            set => SetProperty(ref _selectedTBLPROFI, value, OnSelectedProfileChanged);
        }

        public ProfilesSubGridViewModel()
        {
            Tbl_Profis = new ObservableCollection<TBL_PROFI>();
        }


        private void OnSelectedProfileChanged(TBL_PROFI value)
        {
            if (value == null)
                return;

            AppEnvironment.SetCurrentProfile(value);
        }


    }
}
