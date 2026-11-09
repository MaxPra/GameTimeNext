using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Diagnostics.Controller
{
    public class LogDataGridRow : UIXDataGridRow
    {
        public UIXDataGridCell COLGTS { get; set; } = "";
        public UIXDataGridCell COLGTY { get; set; } = "";
        public UIXDataGridCell COLGSR { get; set; } = "";
        public UIXDataGridCell COLGMS { get; set; } = "";
        //public UIXDataGridCell COLGEX { get; set; } = "";
    }
}
