using GameTimeNext.Core.Application.General.Controller;
using GameTimeNext.Core.Application.Metadata;
using GameTimeNext.Core.Application.Metadata.Controller;
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

        public ProfilesViewController ProfilesSubViewController { get => _profileSubViewController; set; }
        public MetaDataViewController MetaDataViewController { get; set; }


        MainWindowController? _mainWindowController;

        MetaDataView? _metaDataView;

        ProfilesViewController? _profileSubViewController;
        ProfilesDetailSubViewController? _profileDetailSubViewController;

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

            // -- Metadata View
            _metaDataView = new MetaDataView();
            CpMetadata.Content = _metaDataView;
            MetaDataViewController = new MetaDataViewController();

            // -- Profiles View
            _profilesSubView = new ProfilesSubView();
            CPProfileView.Content = _profilesSubView;
            _profileSubViewController = new ProfilesViewController();

            // -- Detailsview
            _profileDetailSubView = new ProfileDetailSubView();
            _profilesSubView.CPProfileDetailView.Content = _profileDetailSubView;
            _profileDetailSubViewController = new ProfilesDetailSubViewController();

            // -- Data Wrapper
            _profilesSubViewDataWrapper = new ProfilesSubViewDataWrapper();
            _profilesSubViewDataWrapper.SetDataSource(_profilesSubView.ListBoxProfiles);
            _profileSubViewController.SetCEDataWrapper(_profilesSubViewDataWrapper, UIXDataWrapperType.Source);
            _profileDetailSubViewController.SetCEDataWrapper(_profilesSubViewDataWrapper, UIXDataWrapperType.Target);

            SetController(_mainWindowController);
            _metaDataView.SetController(MetaDataViewController);
            _profilesSubView.SetController(_profileSubViewController);
            _profileDetailSubView.SetController(_profileDetailSubViewController);
        }
    }
}