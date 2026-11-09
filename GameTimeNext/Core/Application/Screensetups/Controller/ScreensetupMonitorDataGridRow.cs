using UIX.ViewController.Engine.Viewmodel;
using UIXMonitorTweaks;

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
        public UIXDataGridCell COMODE { get => _comode; set => SetProperty(ref _comode, value); }
        public UIXDataGridCell COPLAC { get => _coplac; set => SetProperty(ref _coplac, value); }

        // Display mode of the screensetup, 0 = Windows chooses
        public long ModeWidth { get; set; } = 0;
        public long ModeHeight { get; set; } = 0;
        public long ModeRefreshRate { get; set; } = 0;

        // Codetable keys (mO / mP), empty = unchanged
        public string ModeOrientation { get; set; } = "";
        public string ModePlacement { get; set; } = "";

        // Scaling in percent, 0 = unchanged
        public long ModeScale { get; set; } = 0;

        // Current mode of the monitor (not rotated, like the supported modes); modes are only known while it is on
        public int CurrentWidth { get; set; } = 0;
        public int CurrentHeight { get; set; } = 0;
        public IReadOnlyList<UIXDisplayMode> SupportedModes { get; set; } = Array.Empty<UIXDisplayMode>();

        // Scaling steps allowed at the current resolution
        public IReadOnlyList<int> SupportedScales { get; set; } = Array.Empty<int>();

        private bool _comoen;
        private bool _comopr;
        private UIXDataGridCell _comode = "";
        private UIXDataGridCell _coplac = "";
    }
}
