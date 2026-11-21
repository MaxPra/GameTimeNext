using GameTimeNext.Core.Application.General.Controller;
using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Application.General.Views;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.Selectors
{
    public class FavAppsStartOrderView_FavAppsSelectorController : UIXSelectorControllerBase<MainApp, FavAppsStartOrderView, UIXDataGrid>
    {
        public FavAppsStartOrderView_FavAppsSelectorController(UIXApplication app) : base(app)
        {
        }

        /// <summary>
        /// A favorite was dragged to another position: saves the new start order.
        /// </summary>
        protected void EV_DragAndDrop()
        {
            List<FavoriteApplication> favApps = GetSelector().Items
                .OfType<FavAppsStartOrderDataGridRow>()
                .Select(row => (FavoriteApplication)row.RowObject!)
                .ToList();

            FnUserSettings.ReorderFavoriteApplications(favApps);
            AppEnvironment.GetAppConfig().UserSettings.FavApps = favApps;
            AppEnvironment.SaveAppConfig();
        }
    }
}
