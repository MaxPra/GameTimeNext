using GameTimeNext.Core.Application.General.Controller;
using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Base;
using System.Windows;
using UIX.ViewController.Engine.BuiltInApplications.MetaDataManagerApp.Controller;
using UIX.ViewController.Engine.BuiltInApplications.MetaDataManagerApp.Views;

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
        ProfilesView? _profilesSubView;



        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void InitializeViewOutput(object sender, RoutedEventArgs e)
        {
            // Main-Window Controller
            _mainWindowController = new MainWindowController();

            // -- Metadata View
            _metaDataView = new MetaDataView(AppEnvironment.GetDataBaseManager().GetConnection());
            CpMetadata.Content = _metaDataView;
            MetaDataViewController = new MetaDataViewController();

            // -- Profiles View
            _profilesSubView = new ProfilesView();
            CPProfileView.Content = _profilesSubView;
            _profileSubViewController = new ProfilesViewController();

            SetController(_mainWindowController);
            _metaDataView.SetController(MetaDataViewController);
            _profilesSubView.SetController(_profileSubViewController);
        }
    }
}