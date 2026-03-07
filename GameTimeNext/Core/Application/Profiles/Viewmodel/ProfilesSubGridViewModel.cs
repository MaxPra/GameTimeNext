using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesSubGridViewModel : GTNViewModelBase
    {
        public ObservableCollection<TBL_PROFI> TblProfis { get; set; }

        private TBL_PROFI _selectedProfile;
        public TBL_PROFI SelectedTblProfi
        {
            get => _selectedProfile;
            set => SetProperty(ref _selectedProfile, value, OnSelectedProfileChanged);
        }

        public ProfilesSubGridViewModel()
        {
            TblProfis = new ObservableCollection<TBL_PROFI>();
        }


        private void OnSelectedProfileChanged(TBL_PROFI value)
        {
            if (value == null)
                return;

            AppEnvironment.SetCurrentProfile(value);
        }


    }

    public class ProfileCover : GTNViewModelBase
    {
        private TBL_PROFI _tblProfi;
        private BitmapImage _coverImage;

        public TBL_PROFI TblProfi
        {
            get => _tblProfi;
            set => SetProperty(ref _tblProfi, value);
        }

        public BitmapImage CoverImage
        {
            get => _coverImage;
            set => SetProperty(ref _coverImage, value);
        }

        public ProfileCover()
        {
            _tblProfi = new TBL_PROFI();
        }
    }
}
