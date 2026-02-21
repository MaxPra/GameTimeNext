using GameTimeNext.Core.Application.General.Controller;
using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.UI.Base;
using System.Windows;
using UIX.ViewController.Engine.DataWrapper;

namespace GameTimeNext
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : GTNWindow
    {

        public ProfileSubViewController ProfilesSubViewController { get => _profileSubViewController; set; }


        MainWindowController? _mainWindowController;

        ProfileSubViewController? _profileSubViewController;
        ProfileDetailSubViewController? _profileDetailSubViewController;

        ProfileDetailSubView? _profileDetailSubView;
        ProfilesSubView? _profilesSubView;

        ProfilesSubViewDataWrapper? _profilesSubViewDataWrapper;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void InitializeViewOutput(object sender, RoutedEventArgs e)
        {
            // Main-Window Controller
            _mainWindowController = new MainWindowController();

            // -- Profiles View
            _profilesSubView = new ProfilesSubView();
            CPProfileView.Content = _profilesSubView;
            _profileSubViewController = new ProfileSubViewController();

            // -- Detailsview
            _profileDetailSubView = new ProfileDetailSubView();
            _profilesSubView.CPProfileDetailView.Content = _profileDetailSubView;
            _profileDetailSubViewController = new ProfileDetailSubViewController();

            // -- Data Wrapper
            _profilesSubViewDataWrapper = new ProfilesSubViewDataWrapper();
            _profilesSubViewDataWrapper.SetDataSource(_profilesSubView.ListBoxProfiles);
            _profileSubViewController.SetCEDataWrapper(_profilesSubViewDataWrapper, UIXDataWrapperType.Source);
            _profileDetailSubViewController.SetCEDataWrapper(_profilesSubViewDataWrapper, UIXDataWrapperType.Target);

            SetController(_mainWindowController);
            _profilesSubView.SetController(_profileSubViewController);
            _profileDetailSubView.SetController(_profileDetailSubViewController);
        }
    }
}