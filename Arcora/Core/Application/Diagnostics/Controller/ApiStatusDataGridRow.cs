using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Diagnostics.Controller
{
    public class ApiStatusDataGridRow : UIXDataGridRow
    {
        public UIXDataGridCell COAPI { get; set; } = "";
        public UIXDataGridCell COSTAT { get; set; } = "";
        public UIXDataGridCell COCONF { get; set; } = "";
        public UIXDataGridCell COCHCK { get; set; } = "";
        public UIXDataGridCell COLREQ { get; set; } = "";
        public UIXDataGridCell COLSUC { get; set; } = "";
        public UIXDataGridCell COREQS { get; set; } = "";
        public UIXDataGridCell COERRS { get; set; } = "";
        public UIXDataGridCell COAVGD { get; set; } = "";
        public UIXDataGridCell COLERR { get; set; } = "";
    }
}
