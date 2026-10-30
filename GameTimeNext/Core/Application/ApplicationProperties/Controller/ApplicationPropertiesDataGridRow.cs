using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.ApplicationProperties.Controller
{
    public class ApplicationPropertiesDataGridRow : UIXDataGridRow
    {
        public UIXDataGridCell APAPDID { get; set; } = "";
        public UIXDataGridCell APAPDES { get; set; } = "";
        public bool APAPMCP { get; set; } = false;
    }
}
