using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
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
