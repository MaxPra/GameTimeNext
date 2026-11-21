using GameTimeNext.Core.Application.TableObjects;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.BigPicture.ViewModels
{
    /// <summary>
    /// Cover in the row of games of a pivot (home, library, search results).
    /// </summary>
    public class BigPictureGameTile : UIXViewModelBase
    {
        private bool _isSelected;
        private bool _isRunning;

        public BigPictureGameTile(T1PROFI profile, BitmapImage? cover)
        {
            Profile = profile;
            Cover = cover;
        }

        public T1PROFI Profile { get; }

        public BitmapImage? Cover { get; }

        public string Name => Profile.GANA;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }
    }
}
