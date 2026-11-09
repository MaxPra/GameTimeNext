using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Metadata.Controller
{
    public class MetadataDataGridRow : UIXDataGridRow
    {
        public UIXDataGridCell COMENAM { get; set; } = "";

        public UIXDataGridCell CODESCR { get; set; } = "";

        public UIXDataGridCell COMTYPE { get; set; } = "";

        public bool COGENER { get; set; } = false;
    }
}
