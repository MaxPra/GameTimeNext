using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Diagnostics.Controller
{
    public class BackgroundProcessesDataGridRow : UIXDataGridRow
    {
        public UIXDataGridCell COPROC { get; set; } = "";
        public UIXDataGridCell CODESC { get; set; } = "";
        public UIXDataGridCell COSTAT { get; set; } = "";
        public UIXDataGridCell COSTRT { get; set; } = "";
        public UIXDataGridCell CORUNF { get; set; } = "";
        public UIXDataGridCell COINTV { get; set; } = "";
        public UIXDataGridCell COMODE { get; set; } = "";
        public UIXDataGridCell COCYCL { get; set; } = "";
        public UIXDataGridCell COLAST { get; set; } = "";
        public UIXDataGridCell COLDUR { get; set; } = "";
        public UIXDataGridCell COAVGD { get; set; } = "";
        public UIXDataGridCell COMAXD { get; set; } = "";
        public UIXDataGridCell COTHRD { get; set; } = "";
    }
}
