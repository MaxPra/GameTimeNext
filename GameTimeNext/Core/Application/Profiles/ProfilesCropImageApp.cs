using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Views;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles
{
    public class ProfilesCropImageApp : UIXApplication
    {
        private ProfilesCropImageView? _profilesCropImageView = null;
        private ProfilesCropImageViewController? _profilesCropImageViewController = null;

        private BitmapImage? _sourceImage = null;

        public BitmapImage? SourceImage
        {
            get
            {
                return _sourceImage;
            }
            set
            {
                _sourceImage = value;
            }
        }

        public override void InitializeApplicationOutput()
        {
            _profilesCropImageView = new ProfilesCropImageView();
            MainView = _profilesCropImageView;

            _profilesCropImageViewController = new ProfilesCropImageViewController(this);
            _profilesCropImageView.WndController = _profilesCropImageViewController;
        }

        public void Crop(BitmapImage image, Action<ProfilesCropImageViewController.ProfilesCropImageViewReturn> callback)
        {
            _sourceImage = image;

            _profilesCropImageViewController!.SetResultCallback(callback);
            _profilesCropImageViewController.Show(true);
        }
    }
}