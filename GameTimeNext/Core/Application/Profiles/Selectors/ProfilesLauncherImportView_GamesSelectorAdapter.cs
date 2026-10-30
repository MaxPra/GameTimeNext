using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.LauncherIntegration;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Selectors
{
    public class ProfilesLauncherImportView_GamesSelectorAdapter : UIXSelectorAdapterBase<ProfilesLauncherImportApp, ProfilesLauncherImportView, UIXDataGrid, GameImportRow>
    {
        // Installed games of all launchers, read once (scanning the launchers is slow)
        private List<LauncherGame>? _installedGames;

        public ProfilesLauncherImportView_GamesSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<GameImportRow>> ExecuteAsync(CancellationToken token)
        {
            string search = GetView().txbSearch.Text;

            _installedGames ??= await Task.Run(FnLauncher.GetInstalledGames, token);

            IEnumerable<LauncherGame> games = FnString.IsNullEmptyOrWhitespace(search)
                ? _installedGames
                : _installedGames.Where(g => g.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

            return BuildRows(games);
        }

        private List<GameImportRow> BuildRows(IEnumerable<LauncherGame> games)
        {
            List<GameImportRow> rows = new List<GameImportRow>();

            foreach (LauncherGame game in games)
            {
                GameImportRow row = GetSelector().CreateNewRow<GameImportRow>();
                row.COGANAM = game.Name;
                row.COLNTY = game.LauncherName;
                row.RowObject = game;

                rows.Add(row);
            }

            return rows;
        }
    }
}
