using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesViewController;

namespace GameTimeNext.Core.Application.Profiles
{
    public class ProfilesApp : UIXApplication, IUIXApplicationStarter
    {

        private ProfilesSubViewDataWrapper _dataWrapper;
        private FilterCache _filterCache;

        public ProfilesApp() : base()
        {

        }

        public ProfilesView ProfilesView { get; set; }
        public ProfilesFilterView ProfilesFilterView { get; set; }
        public ProfilesDetailView ProfilesDetailView { get; set; }
        public FilterCache FilterCache { get => _filterCache; set => _filterCache = value; }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            this.HostApplication = hostApplication;
            this.ProfilesView.ContentPresenter = presenter;
            this.ProfilesView.ViewController.Show(false);
            Loader = hostApplication.Loader;
        }

        public override void InitializeApplicationOutput()
        {
            _filterCache = new FilterCache();
            _filterCache = AppEnvironment.GetAppConfig().FilterCache;

            // -- Profiles View (Overview)
            this.ProfilesView = new ProfilesView();
            this.MainView = ProfilesView;
            this.ProfilesView.ViewController = new ProfilesViewController(this);

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


    }
}
