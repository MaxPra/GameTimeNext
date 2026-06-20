using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesSubGridViewModel : GTNViewModelBase
    {
        public ObservableCollection<ProfilesListBoxItem> T1Profis { get; set; }

        private ProfilesListBoxItem _selectedProfile;
        public ProfilesListBoxItem SelectedT1Profi
        {
            get => _selectedProfile;
            set => SetProperty(ref _selectedProfile, value, OnSelectedProfileChanged);
        }

        public ProfilesSubGridViewModel()
        {
            T1Profis = new ObservableCollection<ProfilesListBoxItem>();
        }


        private void OnSelectedProfileChanged(ProfilesListBoxItem value)
        {
            if (value == null)
                return;

            AppEnvironment.SetCurrentProfile(value.ItemObject as T1PROFI);
        }


    }

    public class ProfileCover : GTNViewModelBase
    {
        private T1PROFI _t1Profi;
        private BitmapImage _coverImage;

        public T1PROFI T1Profi
        {
            get => _t1Profi;
            set => SetProperty(ref _t1Profi, value);
        }

        public BitmapImage CoverImage
        {
            get => _coverImage;
            set => SetProperty(ref _coverImage, value);
        }

        public ProfileCover()
        {
            _t1Profi = new T1PROFI();
        }
    }
}
