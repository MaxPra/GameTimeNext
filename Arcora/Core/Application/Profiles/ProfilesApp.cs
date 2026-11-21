using Arcora.Core.Application.Profiles.Controller;
using Arcora.Core.Application.Profiles.DataWrapper;
using Arcora.Core.Application.Profiles.Selectors;
using Arcora.Core.Application.Profiles.Views;
using Arcora.Core.Application.TimeMonitoring;
using Arcora.Core.Framework;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static Arcora.Core.Application.Profiles.Controller.ProfilesViewController;

namespace Arcora.Core.Application.Profiles
{
    [UIXAppIcon(UIXMdlIcons.GameProfile)]
    public class ProfilesApp : UIXApplication, IUIXApplicationStarter
    {

        private ProfilesSubViewDataWrapper _dataWrapper;
        private FilterCache _filterCache;

        public ProfilesApp() : base()
        {
            // Always open (ApplicationLauncher.AlwaysStartedApplications): the running game detection depends on it
            IsClosable = false;
        }
        public ProfilesView ProfilesView { get; set; }
        public ProfilesFilterView ProfilesFilterView { get; set; }
        public ProfilesDetailView ProfilesDetailView { get; set; }
        public FilterCache FilterCache { get => _filterCache; set => _filterCache = value; }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            Start(hostApplication, new UIXApplicationStartOptions
            {
                Target = UIXApplicationStartTarget.ContentPresenter,
                Presenter = presenter
            });
        }

        public void Start(UIXApplication hostApplication, UIXApplicationStartOptions options)
        {
            HostApplication = hostApplication;
            Loader = hostApplication.Loader;
            CallDispatcher = hostApplication.CallDispatcher;

            ProfilesView.ViewController.Show(options);
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ProfilesApp));
        }

        public override void InitializeApplicationOutput()
        {
            _filterCache = AppEnvironment.GetAppConfig().FilterCache ?? new FilterCache();

            // -- Profiles View (Overview)
            this.ProfilesView = new ProfilesView();
            this.MainView = ProfilesView;
            this.ProfilesView.ViewController = new ProfilesViewController(this);
            this.ProfilesView.ListBoxProfiles.SelectorAdapter = new ProfilesView_ProfilesSelectorAdapter(this);
            this.ProfilesView.ListBoxProfiles.SelectorController = new ProfilesView_ProfilesSelectorController(this);

            // -- Profiles Detailview
            this.ProfilesDetailView = new ProfilesDetailView();
            this.ProfilesDetailView.ViewController = new ProfilesDetailSubViewController(this);
            this.ProfilesView.CPProfileDetailView.Content = this.ProfilesDetailView;

            // -- DataWrapper
            _dataWrapper = new ProfilesSubViewDataWrapper(this.ProfilesView.ListBoxProfiles, this.ProfilesView.ViewController, this.ProfilesDetailView.ViewController);
            this.ProfilesDetailView.ViewController.DataWrapper = _dataWrapper;
            this.ProfilesView.ViewController.DataWrapper = _dataWrapper;

            // -- Filter Popup
            this.ProfilesFilterView = new ProfilesFilterView();
            this.ProfilesFilterView.ViewController = new ProfilesFilterViewController(this);
            this.ProfilesFilterView.ContentPresenter = ProfilesView.CPFilter;
            this.ProfilesFilterView.Popup = ProfilesView.PopFilter;

        }

        public override bool CanClose()
        {
            return !CFGameTimeMonitoring.IsMonitoring;
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
        }


    }
}
