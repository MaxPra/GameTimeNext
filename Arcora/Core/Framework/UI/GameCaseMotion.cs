using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Moves the parts of a game case (style GameCase in TileViewStyle.xaml) with springs: the cover on its hinge, the
    /// shadow and the disc. Interrupting a movement (the mouse passing over a row of cases) keeps its velocity.
    ///
    /// Rendering: a closed case at rest is flat (ClosedFace), without anything of the 3D case, so a library of closed cases
    /// costs next to nothing, also when the tile size changes. The 3D case is only shown while the case moves or is open.
    /// Its three surfaces have their own caches (from the first movement of the case on): a frame then only draws three
    /// finished textures. The shadow is one image shared by all cases, only its opacity changes.
    /// </summary>
    internal sealed class GameCaseMotion
    {
        // Cover: opens with a hint of overshoot, closes without one (it can't move behind the case)
        private const double OpenResponse = 0.36;
        private const double OpenDamping = 0.82;
        private const double CloseResponse = 0.3;
        private const double CloseDamping = 1;

        // Disc: follows the cover shortly after it starts to move, and goes back quicker than the cover closes
        private const double DiscOutResponse = 0.42;
        private const double DiscOutDamping = 0.9;
        private const double DiscInResponse = 0.26;
        private const double DiscInDamping = 1;
        private const double DiscOutDelay = 0.04;

        private const double CloseDelayWhileDiscOut = 0.06;

        /// <summary>Spin of the disc sliding out; ends the print on the visible right side together with DiscPrint.PrintAngle -30.</summary>
        private const double DiscSpinAngle = -120;

        private const double MaxStepSeconds = 1.0 / 20;

        private static readonly List<GameCaseMotion> Running = new List<GameCaseMotion>();
        private static TimeSpan? _lastTick;

        private readonly FrameworkElement _host;
        private readonly double _openAngle;
        private readonly double _selectedExtraAngle;
        private readonly double _discOutOffset;
        private readonly double _shadowNormal;
        private readonly double _shadowOpen;

        private readonly SpringValue _hinge = new SpringValue(double.NegativeInfinity, 0, 0.05);
        private readonly SpringValue _hingeSelected = new SpringValue(double.NegativeInfinity, 0, 0.05);
        private readonly SpringValue _discOut = new SpringValue(0, double.PositiveInfinity, 0.0002);
        private readonly SpringValue _discSpin = new SpringValue(double.NegativeInfinity, double.PositiveInfinity, 0.05);

        private AxisAngleRotation3D? _partHinge;
        private AxisAngleRotation3D? _partHingeSelected;
        private TranslateTransform3D? _partDiscSlide;
        private AxisAngleRotation3D? _partDiscSpin;
        private UIElement? _partShadow;
        private UIElement? _partClosedFace;
        private UIElement? _partViewport;

        private bool _isSelected;
        private bool _isRunning;
        private bool _isMoving;
        private bool _hasParts;

        public GameCaseMotion(FrameworkElement host)
        {
            _host = host;
            _openAngle = ReadResource("Case.OpenAngle", -38);
            _selectedExtraAngle = ReadResource("Case.SelectedExtraAngle", -8);
            _discOutOffset = ReadResource("Case.DiscOutOffset", 0.15);
            _shadowNormal = ReadResource("Case.ShadowOpacityNormal", 0.5);
            _shadowOpen = ReadResource("Case.ShadowOpacityOpen", 0.8);
        }

        private double ReadResource(string key, double fallback)
            => _host.TryFindResource(key) is double value ? value : fallback;

        /// <summary>Takes over the state of the case (GameCase.MotionOpen / MotionSelected) as new targets.</summary>
        public void Update()
        {
            bool isOpen = GameCase.GetMotionOpen(_host);
            bool isSelected = GameCase.GetMotionSelected(_host);
            bool wasSelected = _isSelected;
            _isSelected = isSelected;

            // A disc going back is inside before the cover passes over it
            double closeDelay = !isSelected && _discOut.Value > 0.01 ? CloseDelayWhileDiscOut : 0;

            if (isOpen)
                _hinge.MoveTo(_openAngle, OpenResponse, OpenDamping);
            else
                _hinge.MoveTo(0, CloseResponse, CloseDamping, closeDelay);

            // Only on a change: the mouse leaving a selected case must not delay its running disc again
            if (isSelected && !wasSelected)
            {
                _hingeSelected.MoveTo(_selectedExtraAngle, OpenResponse, OpenDamping);
                _discOut.MoveTo(_discOutOffset, DiscOutResponse, DiscOutDamping, DiscOutDelay);

                // Spins out from its start every time; under the cover this jump can't be seen
                _discSpin.Reset(0);
                _discSpin.MoveTo(DiscSpinAngle, DiscOutResponse, DiscOutDamping, DiscOutDelay);
            }
            else if (!isSelected && wasSelected)
            {
                _hingeSelected.MoveTo(0, CloseResponse, CloseDamping, closeDelay);
                _discOut.MoveTo(0, DiscInResponse, DiscInDamping);
            }

            if (_isRunning)
                return;

            _isRunning = true;

            if (Running.Count == 0)
            {
                _lastTick = null;
                CompositionTarget.Rendering += OnRendering;
            }

            Running.Add(this);
        }

        private static void OnRendering(object? sender, EventArgs e)
        {
            TimeSpan now = ((RenderingEventArgs)e).RenderingTime;

            // Rendering can be raised more than once per frame
            if (_lastTick == now)
                return;

            double seconds = _lastTick is TimeSpan last ? Math.Min((now - last).TotalSeconds, MaxStepSeconds) : 1.0 / 120;
            _lastTick = now;

            for (int i = Running.Count - 1; i >= 0; i--)
            {
                if (!Running[i].Step(seconds))
                    Running.RemoveAt(i);
            }

            if (Running.Count == 0)
                CompositionTarget.Rendering -= OnRendering;
        }

        private bool IsSettled
            => _hinge.IsSettled && _hingeSelected.IsSettled && _discOut.IsSettled && _discSpin.IsSettled;

        /// <summary>One frame; false when the movement is over.</summary>
        private bool Step(double seconds)
        {
            if (!_isMoving)
            {
                // Nothing to move, e.g. hovered and left again within one frame while the UI thread was busy
                if (IsSettled || (!_hasParts && !FindParts()))
                {
                    _isRunning = false;
                    return false;
                }

                SetFlat(false);
                _isMoving = true;
            }

            _hinge.Step(seconds);
            _hingeSelected.Step(seconds);
            _discOut.Step(seconds);
            _discSpin.Step(seconds);

            _partHinge!.Angle = _hinge.Value;
            _partHingeSelected!.Angle = _hingeSelected.Value;
            _partDiscSlide!.OffsetX = _discOut.Value;
            _partDiscSpin!.Angle = _discSpin.Value;

            double openShare = _openAngle != 0 ? Math.Clamp(_hinge.Value / _openAngle, 0, 1) : 0;
            _partShadow!.Opacity = _shadowNormal + (_shadowOpen - _shadowNormal) * openShare;

            if (!IsSettled)
                return true;

            if (_hinge.Value == 0 && _hingeSelected.Value == 0 && _discOut.Value == 0)
                SetFlat(true);

            _isMoving = false;
            _isRunning = false;

            return false;
        }

        // Hidden, not collapsed: no layout pass for the swap
        private void SetFlat(bool isFlat)
        {
            _partClosedFace!.Visibility = isFlat ? Visibility.Visible : Visibility.Hidden;
            _partViewport!.Visibility = isFlat ? Visibility.Hidden : Visibility.Visible;
        }

        /// <summary>Finds the parts in the template (not possible earlier: the state is set while it is applied).</summary>
        private bool FindParts()
        {
            if (_host.TemplatedParent is not Control control || control.Template is not ControlTemplate template)
                return false;

            _partHinge = template.FindName("CoverHinge", control) as AxisAngleRotation3D;
            _partHingeSelected = template.FindName("CoverHingeSelected", control) as AxisAngleRotation3D;
            _partDiscSlide = template.FindName("DiscSlide", control) as TranslateTransform3D;
            _partDiscSpin = template.FindName("DiscSpin", control) as AxisAngleRotation3D;
            _partShadow = template.FindName("ShadowHost", control) as UIElement;
            _partClosedFace = template.FindName("ClosedFace", control) as UIElement;

            if (_partHinge == null || _partHingeSelected == null || _partDiscSlide == null || _partDiscSpin == null
                || _partShadow == null || _partClosedFace == null
                || template.FindName("CaseViewport", control) is not Viewport3D viewport)
                return false;

            _partViewport = viewport;

            // Stays set: taking it away again before the layout it triggers has run crashes WPF
            // (Target of the internal BitmapCacheBrush is null then, NullReferenceException in its OnLayoutUpdated)
            var surfaceCache = new BitmapCache(VisualTreeHelper.GetDpi(_host).DpiScaleX) { SnapsToDevicePixels = true };

            foreach (Visual3D child in viewport.Children)
            {
                if (child is Viewport2DVisual3D surface)
                    surface.CacheMode = surfaceCache;
            }

            _hasParts = true;

            return true;
        }
    }
}
