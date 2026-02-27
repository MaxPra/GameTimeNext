using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles
{
    public class ProfilesApp : UIXApplication, IUIXApplicationStarter
    {

        private ProfilesSubViewDataWrapper _dataWrapper;

        public ProfilesApp() : base()
        {

        }

        public ProfilesView ProfilesView { get; set; }
        public ProfilesFilterView ProfilesFilterView { get; set; }
        public ProfilesDetailView ProfilesDetailView { get; set; }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            this.HostApplication = hostApplication;
            this.ProfilesView.ContentPresenter = presenter;
            this.ProfilesView.ViewController.Show(false);
        }

        public override void InitializeApplicationOutput()
        {
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
