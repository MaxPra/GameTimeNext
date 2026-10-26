using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.LauncherIntegration;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesLauncherImportViewController : UIXViewControllerBase
    {

        private ProfilesLauncherImportViewModel? _profilesLauncherImportViewModel;

        // Installed games of all launchers, read once (scanning the launchers is slow)
        private List<LauncherGame>? _installedGames;

        public ProfilesLauncherImportViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfilesLauncherImportViewReturn : UIXViewReturn
        {
            public LauncherGame? Game { get; set; } = null;
        }

        protected override void Init()
        {
            ViewReturn = new ProfilesLauncherImportViewReturn();
        }

        protected override void BuildImpl()
        {
            // Visibility of the import button
            GetView().btnImport.IsEnabled = _profilesLauncherImportViewModel != null && _profilesLauncherImportViewModel.Games.Count > 0
                                                && _profilesLauncherImportViewModel.SelectedGame != null;
        }

        protected override void BuildFirstImpl()
        {
            BuildGameGrid();
        }

        protected override void CheckImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
            GetViewReturn<ProfilesLauncherImportViewReturn>().Canceled = true;
            Exit(false);
        }

        protected override void Event_Maximize()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected void EV_txbSearch()
        {
            BuildGameGrid();
        }

        /// <summary>
        /// Import Button
        /// </summary>
        protected void EV_btnImport()
        {
            GetViewReturn<ProfilesLauncherImportViewReturn>().Canceled = false;
            GetViewReturn<ProfilesLauncherImportViewReturn>().Game = _profilesLauncherImportViewModel!.SelectedGame;
            Exit(true);
        }

        protected void EV_btnCancel()
        {
            GetViewReturn<ProfilesLauncherImportViewReturn>().Canceled = true;

            Exit(true);
        }

        private void BuildGameGrid()
        {
            List<LauncherGame> games = GetGames();

            // Fill view model
            _profilesLauncherImportViewModel = new ProfilesLauncherImportViewModel();
            _profilesLauncherImportViewModel.Games = new System.Collections.ObjectModel.ObservableCollection<LauncherGame>(games);

            if (games.Count > 0)
                _profilesLauncherImportViewModel.SelectedGame = games.FirstOrDefault()!;

            View.DataContext = _profilesLauncherImportViewModel;
        }

        private List<LauncherGame> GetGames()
        {
            _installedGames ??= FnLauncher.GetInstalledGames();

            // Search
            if (FnString.IsNullEmptyOrWhitespace(GetView().txbSearch.Text))
                return _installedGames;

            return _installedGames.Where(g => g.Name.Contains(GetView().txbSearch.Text, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private ProfilesLauncherImportApp GetApp()
        {
            return (ProfilesLauncherImportApp)App;
        }

        private ProfilesLauncherImportView GetView()
        {
            return (ProfilesLauncherImportView)View;
        }
    }
}
