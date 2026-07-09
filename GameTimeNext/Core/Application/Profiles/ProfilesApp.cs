using GameTimeNext.Core.Application.Profiles.Batch;
using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
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

        public ProfilesBatchApp? ProfilesBatchApp { get; set; } = null;

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

            // BatchApp mitstarten, um Hintergrundprozesse zu ermöglichen
            ProfilesBatchApp = hostApplication.GetApplication<ProfilesBatchApp>();
            ProfilesBatchApp.Start(this);
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

            Icon = UIXMdlIcons.GameProfile;

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
