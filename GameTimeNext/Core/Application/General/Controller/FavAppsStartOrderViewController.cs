using GameTimeNext.Core.Application.General.Views;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
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
        }

        protected override async Task BuildFirstImplAsync()
        {
            await GetView().dgFavApps.LoadDataAsync();

            bool hasFavApps = GetView().dgFavApps.Items.Count > 0;

            FnControls.SetVisible(GetView().dgFavApps, hasFavApps);
            FnControls.SetVisible(GetView().txNoFavApps, !hasFavApps);
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
    }
}
