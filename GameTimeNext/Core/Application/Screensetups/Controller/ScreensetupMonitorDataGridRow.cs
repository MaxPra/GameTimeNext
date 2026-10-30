using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Screensetups.Controller
{
    public class ScreensetupMonitorDataGridRow : UIXDataGridRow
    {
        public UIXDataGridCell COMOID { get; set; } = "";
        public UIXDataGridCell COMONA { get; set; } = "";
        public UIXDataGridCell CONUMB { get; set; } = "";
        public UIXDataGridCell COSTAT { get; set; } = "";
        public bool COISCON { get; set; } = false;

        public bool COMOEN { get => _comoen; set => SetProperty(ref _comoen, value); }
        public bool COMOPR { get => _comopr; set => SetProperty(ref _comopr, value); }

        private bool _comoen;
        private bool _comopr;
    }
}
