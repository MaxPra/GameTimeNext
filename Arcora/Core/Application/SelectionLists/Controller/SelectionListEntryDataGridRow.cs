using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.SelectionLists.Controller
{
    public class SelectionListEntryDataGridRow : UIXDataGridRow
    {
        public UIXDataGridCell CONAME { get; set; } = "";
        public UIXDataGridCell COPARM1 { get; set; } = "";
        public UIXDataGridCell COPARM2 { get; set; } = "";
    }
}
