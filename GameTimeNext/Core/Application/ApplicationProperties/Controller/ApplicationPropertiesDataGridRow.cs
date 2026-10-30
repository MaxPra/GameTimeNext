using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.ApplicationProperties.Controller
{
    public class ApplicationPropertiesDataGridRow : UIXDataGridRow
    {
        public string? APAPDID { get; set; } = "";
        public string? APAPDES { get; set; } = "";
        public bool APAPMCP { get; set; } = false;
    }
}
