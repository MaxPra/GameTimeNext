using GameTimeNext.Core.Application.Profiles.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;

namespace GameTimeNext.Core.Application.Profiles.Views
{
    /// <summary>
    /// Interaction logic for ProfilesSubView.xaml
    /// </summary>
    public partial class ProfilesView : UIXUserControlBase
    {

        public ProfilesFilterViewController? ProfileFilterPopupViewController { get; set; }

        public ProfilesView()
        {
            InitializeComponent();
        }
    }
}
