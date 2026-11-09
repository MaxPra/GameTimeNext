using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.General.Controller
{
    /// <summary>
    /// Row of the favorites start order (RowObject: FavoriteApplication).
    /// </summary>
    public class FavAppsStartOrderDataGridRow : UIXDataGridRow
    {
        public UIXDataGridCell COICON { get; set; } = "";
        public UIXDataGridCell CONAME { get; set; } = "";
        public bool COPRST { get; set; } = false;
    }
}
