using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls;
using UIX.ViewController.Engine.DataWrapper;
using UIX.ViewController.Engine.FrameworkElements.UserControls;

namespace GameTimeNext.Core.Application.Profiles.Views
{
    /// <summary>
    /// Interaktionslogik für ProfilesSubView.xaml
    /// </summary>
    public partial class ProfilesSubView : UIXUserControlHostBase
    {

        public ProfileFilterPopupViewController? ProfileFilterPopupViewController { get; set; }

        public ProfilesFilterPopupView? ProfileFilterPopupView { get; set; }

        public ProfilesSubView()
        {
            InitializeComponent();
        }

        protected override void InitializeViewOutput(object sender, RoutedEventArgs e)
        {
            ProfileFilterPopupView = new ProfilesFilterPopupView();
            ProfileFilterPopupView.SetContentPresenter(this.CPFilter);
            ProfileFilterPopupView.SetPopup(this.PopFilter);

            ProfileFilterPopupViewController = new ProfileFilterPopupViewController();

            ProfileFilterPopupView.SetController(ProfileFilterPopupViewController);

        }
    }
}
