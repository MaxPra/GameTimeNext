using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.UI;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    /// <summary>
    /// Moves and zooms the section of the cover on the disc. The print is turned like on the selected disc in the library,
    /// so it looks here as it does there.
    /// </summary>
    public class ProfilesDiscPrintViewController : UIXViewControllerBase<ProfilesDiscPrintApp, ProfilesDiscPrintView>
    {
        private const double WheelZoomStep = 0.1;

        private double _coverAspect = 1;
        private double _fullSize = 1;

        private DiscSection _section = new DiscSection(0, 0, 1);
        private DiscSection _dragStart = new DiscSection(0, 0, 1);

        // Nothing was moved yet: the profile keeps the automatic section
        private bool _isAutomatic = true;
        private bool _isSettingZoom = false;

        public ProfilesDiscPrintViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfilesDiscPrintViewReturn : UIXViewReturn
        {
            /// <summary>The chosen section; null: automatic.</summary>
            public DiscSection? Section { get; set; }
        }

        protected override void Init()
        {
            ViewReturn = new ProfilesDiscPrintViewReturn();
        }

        protected override void BuildFirstImpl()
        {
            BitmapSource cover = GetApp().Cover!;

            _coverAspect = (double)cover.PixelHeight / cover.PixelWidth;
            _fullSize = Math.Min(1, _coverAspect);
            _isAutomatic = GetApp().Section == null;

            GetView().thbMove.DragStarted += ThbMove_DragStarted;
            GetView().thbMove.DragDelta += ThbMove_DragDelta;
            GetView().sldZoom.ValueChanged += SldZoom_ValueChanged;
            GetView().brdWorkspace.PreviewMouseWheel += BrdWorkspace_PreviewMouseWheel;

            // Set before the cover, so the disc starts no analysis of its own
            ShowSection(GetApp().Section ?? DiscPrint.GetCenteredSection(cover));
            DiscPrint.SetCover(GetView().elpPrint, cover);

            // Not awaited: the engine would keep the dialog from opening until a new cover is analyzed
            if (_isAutomatic)
                _ = ShowAutomaticSectionAsync();
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
        }

        protected override void Event_Maximize()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        private void ThbMove_DragStarted(object sender, DragStartedEventArgs e)
        {
            _dragStart = _section;
        }

        /// <summary>
        /// The thumb itself doesn't move, so the changes count from the start of the drag. The image follows the mouse: the
        /// section moves the opposite way, and the print is turned a quarter clockwise (print right = screen down, print
        /// down = screen left).
        /// </summary>
        private void ThbMove_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double coverUnitsPerPixel = _dragStart.Size / GetView().thbMove.ActualWidth;

            MoveSection(
                _dragStart.X - e.VerticalChange * coverUnitsPerPixel,
                _dragStart.Y + e.HorizontalChange * coverUnitsPerPixel,
                _dragStart.Size);
        }

        private void SldZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSettingZoom)
                return;

            // Zooms around the center of the disc
            double size = _fullSize / e.NewValue;
            double change = (_section.Size - size) / 2;

            MoveSection(_section.X + change, _section.Y + change, size);
        }

        private void BrdWorkspace_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            GetView().sldZoom.Value += Math.Sign(e.Delta) * WheelZoomStep;
            e.Handled = true;
        }

        /// <summary>Takes over a section changed by the user; it stays completely within the cover.</summary>
        private void MoveSection(double x, double y, double size)
        {
            _isAutomatic = false;

            size = Math.Min(size, _fullSize);

            ShowSection(new DiscSection(
                Math.Clamp(x, 0, 1 - size),
                Math.Clamp(y, 0, _coverAspect - size),
                size));
        }

        private void ShowSection(DiscSection section)
        {
            _section = section;

            DiscPrint.SetSection(GetView().elpPrint, section);

            _isSettingZoom = true;
            GetView().sldZoom.Value = _fullSize / section.Size;
            _isSettingZoom = false;
        }

        private async Task ShowAutomaticSectionAsync()
        {
            DiscSection section = await DiscPrint.GetAutomaticSectionAsync(GetApp().Cover!);

            // Moved while the analysis was running
            if (_isAutomatic)
                ShowSection(section);
        }

        protected async Task EV_btnAutomatic()
        {
            _isAutomatic = true;

            await ShowAutomaticSectionAsync();
        }

        protected void EV_btnApply()
        {
            GetViewReturn<ProfilesDiscPrintViewReturn>().Canceled = false;
            GetViewReturn<ProfilesDiscPrintViewReturn>().Section = _isAutomatic ? null : _section;
            Exit(true);
        }

        protected void EV_btnCancel()
        {
            Exit(true);
        }
    }
}
