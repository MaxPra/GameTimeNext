using GameTimeNext.Core.Application.General.FavAppsReorder.Controller;
using GameTimeNext.Core.Application.General.FavAppsReorder.Views;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.FavAppsReorder
{
    public class FavAppsReorderApp : UIXApplication
    {
        public FavAppsReorderView? FavAppsReorderView { get; set; } = null;
        public FavAppsReorderViewController? FavAppsReorderViewController { get; set; } = null;

        public Action<FavAppsReorderViewController.FavAppsRecorderViewReturn>? AppResult { get; set; } = null;

        public override void InitializeApplicationOutput()
        {
            FavAppsReorderView = new FavAppsReorderView();
            MainView = FavAppsReorderView;

            FavAppsReorderViewController = new FavAppsReorderViewController(this);

            FavAppsReorderView.WndController = FavAppsReorderViewController;
        }

        public void Reorder()
        {
            FavAppsReorderView!.ViewIndicator.Add("ED");
            FavAppsReorderViewController!.SetResultCallback(AppResult!);
            FavAppsReorderViewController!.Show(true);
        }
    }
}
