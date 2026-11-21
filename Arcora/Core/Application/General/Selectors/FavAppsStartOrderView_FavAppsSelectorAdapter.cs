using GameTimeNext.Core.Application.General.Controller;
using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Application.General.Views;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.Selectors
{
    public class FavAppsStartOrderView_FavAppsSelectorAdapter : UIXSelectorAdapterBase<MainApp, FavAppsStartOrderView, UIXDataGrid, FavAppsStartOrderDataGridRow>
    {
        public FavAppsStartOrderView_FavAppsSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override Task<IEnumerable<FavAppsStartOrderDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            // The favorites come from the loaded config, nothing to wait for
            List<FavAppsStartOrderDataGridRow> rows = new List<FavAppsStartOrderDataGridRow>();

            foreach (FavoriteApplication favApp in AppEnvironment.GetAppConfig().UserSettings.FavApps.OrderBy(f => f.Order))
            {
                FavAppsStartOrderDataGridRow row = GetSelector().CreateNewRow<FavAppsStartOrderDataGridRow>();
                row.COICON = GetApplicationIcon(favApp.FullName);
                row.CONAME = favApp.AppName;
                row.COPRST = favApp.PrimaryStart;
                row.RowObject = favApp;

                rows.Add(row);
            }

            return Task.FromResult<IEnumerable<FavAppsStartOrderDataGridRow>>(rows);
        }

        private static string GetApplicationIcon(string fullName)
        {
            Type? type = Type.GetType(fullName);

            return type == null ? string.Empty : UIXAppIconAttribute.GetIcon(type);
        }
    }
}
