using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesSteamGridDBViewModel : GTNViewModelBase
    {
        public ObservableCollection<SteamGridDBImage>? Images { get; set; }

        private SteamGridDBImage? _selectedImage;

        public SteamGridDBImage? SelectedImage
        {
            get => _selectedImage;
            set => SetProperty(ref _selectedImage, value);
        }

        public ProfilesSteamGridDBViewModel()
        {
            Images = new ObservableCollection<SteamGridDBImage>();

        }
    }

    public class SteamGridDBImage : GTNViewModelBase
    {
        private BitmapImage? _bitmapImage;
        private string? _path;

        public BitmapImage BitmapImage
        {
            get => _bitmapImage!;
            set => SetProperty(ref _bitmapImage, value);
        }

        public string Path
        {
            get => _path!;
            set => SetProperty(ref _path, value);
        }

        public SteamGridDBImage()
        {
            BitmapImage = new BitmapImage();
            Path = string.Empty;
        }
    }
}
