using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Metadata.Controller
{
    public class MetadataPosDataGridRow : UIXDataGridRow
    {
        public string? COMENAM { get; set; } = string.Empty;
        public string? COPONAM { get; set; } = string.Empty;
        public string? CODESCR { get; set; } = string.Empty;
        public string? CODATYP { get; set; } = string.Empty;
        public bool COPRIMK { get; set; }
        public int CODALEN { get; set; } = 0;
    }
}
