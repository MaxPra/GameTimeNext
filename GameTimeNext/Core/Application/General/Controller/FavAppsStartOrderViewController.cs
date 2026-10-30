using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Application.General.Views;
using GameTimeNext.Core.Framework;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.General.Controller
{
    public class FavAppsStartOrderViewController : UIXViewControllerBase<MainApp, FavAppsStartOrderView>
    {
        public FavAppsStartOrderViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
            BuildList();
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }
        #endregion

        /// <summary>
        /// A favorite was dragged to another position: saves the new start order.
        /// </summary>
        protected void EV_dgFavApps_DragAndDrop()
        {
            List<FavoriteApplication> favApps = GetView().dgFavApps.Items
                .OfType<FavAppsStartOrderDataGridRow>()
                .Select(row => (FavoriteApplication)row.RowObject!)
                .ToList();

            FnUserSettings.ReorderFavoriteApplications(favApps);
            AppEnvironment.GetAppConfig().UserSettings.FavApps = favApps;
            AppEnvironment.SaveAppConfig();
        }

        private void BuildList()
        {
            List<FavAppsStartOrderDataGridRow> rows = new List<FavAppsStartOrderDataGridRow>();

            foreach (FavoriteApplication favApp in AppEnvironment.GetAppConfig().UserSettings.FavApps.OrderBy(f => f.Order))
            {
                FavAppsStartOrderDataGridRow row = GetView().dgFavApps.CreateNewRow<FavAppsStartOrderDataGridRow>();
                row.COICON = GetApplicationIcon(favApp.FullName);
                row.CONAME = favApp.AppName;
                row.COPRST = favApp.PrimaryStart;
                row.RowObject = favApp;

                rows.Add(row);
            }

            GetView().dgFavApps.ItemsSource = new ObservableCollection<FavAppsStartOrderDataGridRow>(rows);

            FnControls.SetVisible(GetView().dgFavApps, rows.Count > 0);
            FnControls.SetVisible(GetView().txNoFavApps, rows.Count == 0);
        }

        private static string GetApplicationIcon(string fullName)
        {
            Type? type = Type.GetType(fullName);

            return type == null ? string.Empty : UIXAppIconAttribute.GetIcon(type);
        }
    }
}
