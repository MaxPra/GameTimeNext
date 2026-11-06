using GameTimeNext.Core.Framework.Utils;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// The mouse wheel moves every ScrollViewer of the app by a spring instead of in steps: further notches add to the target
    /// and the content keeps its velocity, so it glides. Register once at startup; nothing to do in the views.
    /// Lists scroll by pixels for this (VirtualizingPanel.ScrollUnit), virtualization stays.
    ///
    /// While the content is moving fast it takes no mouse input, so items passing under the resting cursor don't react to
    /// hover (a game case would open and close). A real mouse movement or the content slowing down ends that at once.
    /// A press of a mouse button stops the movement, e.g. to take over with the scrollbar.
    /// </summary>
    public static class SmoothScroll
    {
        private const double DefaultWheelStep = 120;

        /// <summary>A notch never moves further than this share of the visible height (small lists, drop downs).</summary>
        private const double MaxStepShareOfViewport = 0.75;

        private const double Response = 0.3;
        private const double Damping = 1;
        private const double MaxStepSeconds = 1.0 / 20;

        /// <summary>Below this speed (pixels per second) the content reacts to the mouse again.</summary>
        private const double HoverSpeedLimit = 500;

        /// <summary>Pixels the mouse has to move to count as moved: WPF also raises MouseMove for moved content.</summary>
        private const double MouseMoveThreshold = 2;

        /// <summary>Pixels per wheel notch for a control and what's inside it; 0 = the default of the app.</summary>
        public static readonly DependencyProperty WheelStepProperty =
            DependencyProperty.RegisterAttached(
                "WheelStep",
                typeof(double),
                typeof(SmoothScroll),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.Inherits));

        public static double GetWheelStep(DependencyObject obj)
            => (double)obj.GetValue(WheelStepProperty);

        public static void SetWheelStep(DependencyObject obj, double value)
            => obj.SetValue(WheelStepProperty, value);

        private static readonly DependencyProperty MotionProperty =
            DependencyProperty.RegisterAttached("Motion", typeof(Motion), typeof(SmoothScroll), new PropertyMetadata(null));

        /// <summary>Switches it on for the whole app; once, before the first window is created.</summary>
        public static void Register()
        {
            // Not on ItemsControl: its static constructor sets its own metadata for this property, a second one throws. It has
            // to have run before: the metadata below is merged with it (its callbacks would be missing otherwise).
            // The selector controls of the engine derive from these; a TreeView scrolls by pixels anyway.
            RuntimeHelpers.RunClassConstructor(typeof(ItemsControl).TypeHandle);

            foreach (Type listType in new[] { typeof(ListBox), typeof(DataGrid), typeof(ComboBox) })
                VirtualizingPanel.ScrollUnitProperty.OverrideMetadata(listType, new FrameworkPropertyMetadata(ScrollUnit.Pixel));

            // Runs before ScrollViewer.OnMouseWheel, and not when something inside already handled the wheel
            EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.MouseWheelEvent, new MouseWheelEventHandler(OnMouseWheel));
        }

        private static void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer || scrollViewer.ScrollableHeight <= 0 || !ScrollsByPixels(scrollViewer))
                return;

            if (scrollViewer.GetValue(MotionProperty) is not Motion motion)
            {
                motion = new Motion(scrollViewer);
                scrollViewer.SetValue(MotionProperty, motion);
            }

            motion.AddWheel(e);
            e.Handled = true;
        }

        /// <summary>False for a list that explicitly scrolls by items: its offset counts items, it keeps its normal steps.</summary>
        private static bool ScrollsByPixels(ScrollViewer scrollViewer)
        {
            return !scrollViewer.CanContentScroll
                || scrollViewer.TemplatedParent is not ItemsControl owner
                || VirtualizingPanel.GetScrollUnit(owner) == ScrollUnit.Pixel;
        }

        private sealed class Motion
        {
            private readonly ScrollViewer _scrollViewer;
            private readonly SpringValue _offset = new SpringValue(double.NegativeInfinity, double.PositiveInfinity, 0.25);
            private readonly MouseWheelEventHandler _onRootWheel;
            private readonly MouseEventHandler _onRootMouseMove;
            private readonly MouseButtonEventHandler _onRootMouseDown;

            private UIElement? _content;
            private UIElement? _root;
            private double _target;
            private bool _isRunning;
            private bool _isHoverSuppressed;
            private TimeSpan? _lastTick;
            private Point _lastMouse;

            public Motion(ScrollViewer scrollViewer)
            {
                _scrollViewer = scrollViewer;
                _onRootWheel = OnRootWheel;
                _onRootMouseMove = OnRootMouseMove;
                _onRootMouseDown = (_, _) => Stop();
                scrollViewer.Unloaded += (_, _) => Stop();
            }

            public void AddWheel(MouseWheelEventArgs e)
            {
                // Whatever else moved the content meanwhile (scrollbar, keyboard, ScrollIntoView) is the new start
                if (!_isRunning)
                {
                    _offset.Reset(_scrollViewer.VerticalOffset);
                    _target = _scrollViewer.VerticalOffset;
                }

                double configured = GetWheelStep(_scrollViewer);
                double step = Math.Min(configured > 0 ? configured : DefaultWheelStep, _scrollViewer.ViewportHeight * MaxStepShareOfViewport);
                double notches = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;

                _target = Math.Round(Math.Clamp(_target - notches * step, 0, _scrollViewer.ScrollableHeight));
                _offset.MoveTo(_target, Response, Damping);

                _lastMouse = Mouse.GetPosition(_scrollViewer);
                SetHoverSuppressed(true);

                if (!_isRunning)
                    Start();
            }

            private void Start()
            {
                _isRunning = true;
                _lastTick = null;
                CompositionTarget.Rendering += OnRendering;

                // On the root: with the content not taking mouse input, these events don't pass the ScrollViewer any more
                _root = PresentationSource.FromVisual(_scrollViewer)?.RootVisual as UIElement;
                _root?.AddHandler(UIElement.PreviewMouseWheelEvent, _onRootWheel);
                _root?.AddHandler(UIElement.PreviewMouseMoveEvent, _onRootMouseMove);
                _root?.AddHandler(UIElement.PreviewMouseDownEvent, _onRootMouseDown);
            }

            private void Stop()
            {
                SetHoverSuppressed(false);

                if (!_isRunning)
                    return;

                _isRunning = false;
                CompositionTarget.Rendering -= OnRendering;

                _root?.RemoveHandler(UIElement.PreviewMouseWheelEvent, _onRootWheel);
                _root?.RemoveHandler(UIElement.PreviewMouseMoveEvent, _onRootMouseMove);
                _root?.RemoveHandler(UIElement.PreviewMouseDownEvent, _onRootMouseDown);
                _root = null;
            }

            private void OnRendering(object? sender, EventArgs e)
            {
                TimeSpan now = ((RenderingEventArgs)e).RenderingTime;

                // Rendering can be raised more than once per frame
                if (_lastTick == now)
                    return;

                double seconds = _lastTick is TimeSpan last ? Math.Min((now - last).TotalSeconds, MaxStepSeconds) : 1.0 / 120;
                _lastTick = now;

                _offset.Step(seconds);

                // Whole device pixels: covers and texts stay sharp while moving
                double dpi = VisualTreeHelper.GetDpi(_scrollViewer).DpiScaleY;
                _scrollViewer.ScrollToVerticalOffset(Math.Round(_offset.Value * dpi) / dpi);

                if (Math.Abs(_offset.Velocity) < HoverSpeedLimit)
                    SetHoverSuppressed(false);

                if (_offset.IsSettled)
                    Stop();
            }

            private void OnRootWheel(object sender, MouseWheelEventArgs e)
            {
                // Otherwise the wheel reaches the ScrollViewer on its normal way
                if (!_isHoverSuppressed || !new Rect(_scrollViewer.RenderSize).Contains(Mouse.GetPosition(_scrollViewer)))
                    return;

                AddWheel(e);
                e.Handled = true;
            }

            private void OnRootMouseMove(object sender, MouseEventArgs e)
            {
                if (_isHoverSuppressed && (Mouse.GetPosition(_scrollViewer) - _lastMouse).Length > MouseMoveThreshold)
                    SetHoverSuppressed(false);
            }

            private void SetHoverSuppressed(bool suppressed)
            {
                if (_isHoverSuppressed == suppressed)
                    return;

                // Left on with a mouse capture (an open drop down would close) and in a text box (the cursor would flicker)
                if (suppressed && (Mouse.Captured != null || _scrollViewer.TemplatedParent is TextBoxBase))
                    return;

                // The presenter of a list's own ScrollViewer template doesn't always have the usual name
                _content ??= FnVisualTree.FindDescendant<ScrollContentPresenter>(_scrollViewer);

                if (_content == null)
                    return;

                _isHoverSuppressed = suppressed;
                _content.IsHitTestVisible = !suppressed;
            }
        }
    }
}
