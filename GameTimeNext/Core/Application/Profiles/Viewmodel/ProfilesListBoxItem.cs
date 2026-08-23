using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesListBoxItem : UIXListItem
    {
        public BitmapImage? COCOVIM { get; set; }
        public bool COISPLA { get; set; }

        public BitmapImage? COPLFPA { get; set; }
        
        // Calculated Playtime
        public string COCPLTI { get; set; }
    }
}
