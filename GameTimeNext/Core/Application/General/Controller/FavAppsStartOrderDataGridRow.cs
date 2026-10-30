using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.General.Controller
{
    /// <summary>
    /// Row of the favorites start order (RowObject: FavoriteApplication).
    /// </summary>
    public class FavAppsStartOrderDataGridRow : UIXDataGridRow
    {
        public string COICON { get; set; } = "";
        public string CONAME { get; set; } = "";
        public bool COPRST { get; set; } = false;
    }
}
