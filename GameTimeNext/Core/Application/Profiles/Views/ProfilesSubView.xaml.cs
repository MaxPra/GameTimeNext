using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls;
using UIX.ViewController.Engine.DataWrapper;
using UIX.ViewController.Engine.UserControls;

namespace GameTimeNext.Core.Application.Profiles.Views
{
    /// <summary>
    /// Interaktionslogik für ProfilesSubView.xaml
    /// </summary>
    public partial class ProfilesSubView : UIXUserControlHostBase
    {
        ProfileSubViewController? profileSubViewController;
        ProfileDetailSubViewController? profileDetailSubViewController;

        ProfileDetailSubView? profileDetailSubView;

        ProfilesSubViewDataWrapper? profilesSubViewDataWrapper;

        public ProfilesSubView() : base()
        {
            InitializeComponent();
        }

        protected override void InitializeViewOutput(object sender, RoutedEventArgs e)
        {
            // -- Profiles View
            profileSubViewController = new ProfileSubViewController();

            // -- Data Wrapper
            profilesSubViewDataWrapper = new ProfilesSubViewDataWrapper();
            profilesSubViewDataWrapper.SetDataSource(ListBoxProfiles);

            profileSubViewController.SetCEDataWrapper(profilesSubViewDataWrapper, UIXDataWrapperType.Source);

            // -- Detailsview
            profileDetailSubView = new ProfileDetailSubView();
            this.CPProfileDetailView.Content = profileDetailSubView;

            profileDetailSubViewController = new ProfileDetailSubViewController();
            profileDetailSubViewController.SetCEDataWrapper(profilesSubViewDataWrapper, UIXDataWrapperType.Target);

            // -- Controller setzen
            this.SetController(profileSubViewController);
            profileDetailSubView.SetController(profileDetailSubViewController);
        }
    }
}
