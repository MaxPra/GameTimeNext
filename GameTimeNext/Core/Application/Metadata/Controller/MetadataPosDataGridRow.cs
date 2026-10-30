using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Metadata.Controller
{
    public class MetadataPosDataGridRow : UIXDataGridRow
    {
        public UIXDataGridCell COMENAM { get; set; } = "";
        public UIXDataGridCell COPONAM { get; set; } = "";
        public int COPORDE { get; set; } = 0;
        public UIXDataGridCell CODESCR { get; set; } = "";
        public UIXDataGridCell CODATYP { get; set; } = "";
        public bool COPRIMK { get; set; }
        public int CODALEN { get; set; } = 0;
    }
}
