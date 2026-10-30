using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Screensetups.Controller
{
    public class ScreensetupDataGridRow : UIXDataGridRow
    {
        public UIXDataGridCell COSSNA { get; set; } = "";
        public UIXDataGridCell COSSDE { get; set; } = "";
        public UIXDataGridCell COMONI { get; set; } = "";
        public UIXDataGridCell COAUDI { get; set; } = "";
        public bool COISACT { get; set; } = false;
    }
}
