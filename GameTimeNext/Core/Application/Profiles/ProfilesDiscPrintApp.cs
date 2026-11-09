using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.UI;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles
{
    /// <summary>
    /// Dialog that chooses the section of a cover printed on the disc of a profile.
    /// </summary>
    public class ProfilesDiscPrintApp : UIXApplication
    {
        private ProfilesDiscPrintView? _profilesDiscPrintView = null;
        private ProfilesDiscPrintViewController? _profilesDiscPrintViewController = null;

        public BitmapSource? Cover { get; private set; }

        /// <summary>The section chosen so far; null: automatic.</summary>
        public DiscSection? Section { get; private set; }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ProfilesDiscPrintApp));
        }

        public override void InitializeApplicationOutput()
        {
            _profilesDiscPrintView = new ProfilesDiscPrintView();
            MainView = _profilesDiscPrintView;

            _profilesDiscPrintViewController = new ProfilesDiscPrintViewController(this);
            _profilesDiscPrintView.ViewController = _profilesDiscPrintViewController;
        }

        public void Edit(BitmapSource cover, DiscSection? section, Action<ProfilesDiscPrintViewController.ProfilesDiscPrintViewReturn> callback)
        {
            Cover = cover;
            Section = section;

            _profilesDiscPrintViewController!.SetResultCallback(callback);
            _profilesDiscPrintViewController.Show();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
