using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using System.Windows;
using UIX.ViewController.Engine.DataWrapper;
using UIX.ViewController.Engine.FrameworkElements.UserControls;

namespace GameTimeNext.Core.Application.Profiles.Views
{
    /// <summary>
    /// Interaktionslogik für ProfilesSubView.xaml
    /// </summary>
    public partial class ProfilesView : UIXUserControlHostBase
    {

        public ProfilesFilterViewController? ProfileFilterPopupViewController { get; set; }

        public ProfilesFilterView? ProfileFilterPopupView { get; set; }

        ProfilesDetailSubViewController? _profileDetailSubViewController;
        ProfilesDetailView? _profileDetailSubView;
        ProfilesSubViewDataWrapper? _profilesSubViewDataWrapper;

        public ProfilesView()
        {
            InitializeComponent();
        }

        protected override void InitializeViewOutput(object sender, RoutedEventArgs e)
        {

            // -- Detailsview
            _profileDetailSubView = new ProfilesDetailView();
            this.CPProfileDetailView.Content = _profileDetailSubView;
            _profileDetailSubViewController = new ProfilesDetailSubViewController();

            // -- Data Wrapper
            _profilesSubViewDataWrapper = new ProfilesSubViewDataWrapper();
            _profilesSubViewDataWrapper.SetDataSource(this.ListBoxProfiles);
            _profileDetailSubViewController.SetCEDataWrapper(_profilesSubViewDataWrapper, UIXDataWrapperType.Target);
            this.ViewController.SetCEDataWrapper(_profilesSubViewDataWrapper, UIXDataWrapperType.Source);

            // -- Profile Filter
            ProfileFilterPopupView = new ProfilesFilterView();
            ProfileFilterPopupView.SetContentPresenter(this.CPFilter);
            ProfileFilterPopupView.SetPopup(this.PopFilter);

            ProfileFilterPopupViewController = new ProfilesFilterViewController();

            // -- set Subview Controllers
            _profileDetailSubView.SetController(_profileDetailSubViewController);
            ProfileFilterPopupView.SetController(ProfileFilterPopupViewController);

        }
    }
}
