using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Playthroughs.ViewModel
{
    public class PlaythroughDataGridRow : UIXDataGridRow
    {

        public string COGANA { get; set; } = string.Empty;

        public string COPTDE { get; set; } = string.Empty;
        public bool COPTCO { get; set; } = false;

        public bool COPTCA { get; set; } = false;
        public string COPTTY { get; set; } = string.Empty;
    }
}
