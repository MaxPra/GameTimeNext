using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.LauncherIntegration;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesSteamImportViewController : UIXWindowControllerBase
    {

        private ProfilesSteamImportViewModel? _profilesSteamImportViewModel;

        public ProfilesSteamImportViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfilesSteamImportViewRetrun : UIXViewReturn
        {
            public SteamGame? SteamGame { get; set; } = null;
        }

        protected override void Init()
        {
            ViewReturn = new ProfilesSteamImportViewRetrun();
        }

        protected override void Build()
        {
            // Sichtbarkeitssteuerung Import-Button
            GetView().BtnImport.IsEnabled = _profilesSteamImportViewModel != null && _profilesSteamImportViewModel.SteamGames.Count > 0
                                                && _profilesSteamImportViewModel.SelectedSteamGame != null;
        }

        protected override void BuildFirst()
        {
            BuildSteamGameGrid();
        }

        protected override void Check()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
            GetViewReturn<ProfilesSteamImportViewRetrun>().Canceled = true;
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

        protected void EV_TxbSearch()
        {
            BuildSteamGameGrid();
        }

        /// <summary>
        /// Import Button
        /// </summary>
        protected void EV_BtnImport()
        {
            GetViewReturn<ProfilesSteamImportViewRetrun>().Canceled = false;
            GetViewReturn<ProfilesSteamImportViewRetrun>().SteamGame = _profilesSteamImportViewModel!.SelectedSteamGame;
            Exit(true);
        }

        protected void EV_BtnCancel()
        {
            GetViewReturn<ProfilesSteamImportViewRetrun>().Canceled = true;

            Exit(true);
        }

        private void BuildSteamGameGrid()
        {
            List<SteamGame> steamGames = new List<SteamGame>();

            steamGames = GetSteamGames();

            // Viewmodel befüllen
            _profilesSteamImportViewModel = new ProfilesSteamImportViewModel();
            _profilesSteamImportViewModel.SteamGames = new System.Collections.ObjectModel.ObservableCollection<SteamGame>(steamGames);


            if (steamGames != null && steamGames.Count > 0)
                _profilesSteamImportViewModel.SelectedSteamGame = steamGames.FirstOrDefault()!;

            View.DataContext = _profilesSteamImportViewModel;
        }

        private List<SteamGame> GetSteamGames()
        {

            List<SteamGame> games = new List<SteamGame>();

            var root = SteamLocatorService.GetSteamRoot();
            if (string.IsNullOrEmpty(root))
            {
                // Todo Messagebox
            }

            var libs = SteamLibrariesHelper.GetLibraryPaths(root);
            games = SteamManifestHelper.ScanAllGames(libs);

            // Tools/Redistributables ausblenden
            string[] noiseTokens =
            {
                    "steamworks", "redistributable", "proton", "steam linux runtime",
                    "shader", "benchmark", "dedicated server"
                };

            games = games
                .Where(g => !noiseTokens.Any(t =>
                           g.Name?.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            games = games.Where(g => SteamManifestHelper.ResolveInstallPath(g) != null).ToList();

            // Suche
            if (!string.IsNullOrWhiteSpace(GetView().TxbSearch.Text))
                games = games.Where(g => g.Name?.IndexOf(GetView().TxbSearch.Text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            // Duplikate entfernen (pro AppID ein Eintrag; bevorzugt mit gültigem InstallPath)
            games = games
                .GroupBy(g => g.AppId)
                .Select(grp =>
                    grp.OrderByDescending(x => !string.IsNullOrEmpty(SteamManifestHelper.ResolveInstallPath(x)))
                       .ThenByDescending(x => !string.IsNullOrEmpty(x.InstallDir))
                       .ThenBy(x => x.LibraryPath)
                       .First())
                .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return games;
        }

        private ProfilesSteamImportApp GetApp()
        {
            return (ProfilesSteamImportApp)App;
        }

        private ProfilesSteamImportView GetView()
        {
            return (ProfilesSteamImportView)View;
        }
    }
}
