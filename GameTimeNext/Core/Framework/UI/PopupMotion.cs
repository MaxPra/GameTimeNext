using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Opening and closing of a popup, out of and back into the point it belongs to (the mouse or its target), while
    /// the popup itself fades (PopupAnimation.Fade).
    ///
    /// Glass: the acrylic blur fills the whole window and can neither scale nor fade, but it has to be there from the
    /// first frame, because the tint alone looks opaque. So the window itself is revealed: its region grows out of that
    /// point and takes the blur with it. Without glass (e.g. tooltips) the surface scales instead.
    ///
    /// Usage: PopupMotion.IsEnabled="True" on a Border inside a popup with AllowsTransparency (set by Popup.Surface).
    /// </summary>
    public static class PopupMotion
    {
        private const double ClosedScale = 0.96;

        // Part of the window that is visible at the start of the reveal, in width and height
        private const double RevealStartWidth = 0.7;
        private const double RevealStartHeight = 0.2;

        // Popup.CornerRadius
        private const double RevealCornerRadius = 8;

        private static readonly Duration OpenDuration = TimeSpan.FromMilliseconds(160);
        private static readonly TimeSpan RevealDuration = TimeSpan.FromMilliseconds(180);

        // Shorter than the fade of the popup (150 ms), after which WPF destroys its window
        private static readonly Duration CloseDuration = TimeSpan.FromMilliseconds(120);

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(PopupMotion),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static readonly DependencyProperty StateProperty =
            DependencyProperty.RegisterAttached(
                "State",
                typeof(MotionState),
                typeof(PopupMotion),
                new PropertyMetadata(null));

        /// <summary>
        /// Bound to Popup.IsOpen: the event Popup.Closed comes too late, it is only raised when the window is destroyed.
        /// </summary>
        private static readonly DependencyProperty PopupIsOpenProperty =
            DependencyProperty.RegisterAttached(
                "PopupIsOpen",
                typeof(bool),
                typeof(PopupMotion),
                new PropertyMetadata(false, OnPopupIsOpenChanged));

        private sealed class MotionState
        {
            public Popup? Popup;
            public HwndSource? Source;
            public ScaleTransform? Scale;
            public bool IsClosing;
            public bool IsGlass;

            /// <summary>Point the popup opens from, relative to its size.</summary>
            public Point Origin;

            /// <summary>How far the window is revealed: 0 = start of the reveal, 1 = whole window.</summary>
            public double Reveal;

            public EventHandler? RevealStep;

            /// <summary>Incremented by every state change; handlers of older animations see that they are outdated.</summary>
            public int Run;
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Border surface)
                return;

            PresentationSource.RemoveSourceChangedHandler(surface, OnSourceChanged);

            if (!(bool)e.NewValue)
                return;

            // Each opening normally creates a new popup window
            PresentationSource.AddSourceChangedHandler(surface, OnSourceChanged);

            if (PresentationSource.FromVisual(surface) is HwndSource current)
                OnWindowCreated(surface, current);
        }

        private static void OnSourceChanged(object sender, SourceChangedEventArgs e)
        {
            if (sender is not Border surface)
                return;

            if (e.NewSource is HwndSource source)
                OnWindowCreated(surface, source);
            else
                OnWindowDestroyed(surface);
        }

        private static void OnWindowCreated(Border surface, HwndSource source)
        {
            // A surface that ends up directly in a window must not blur the whole window
            if (source.RootVisual is Window)
                return;

            bool glass = PopupGlass.GetIsActive(surface) && AcrylicBlur.TryEnable(source.Handle);

            Popup? popup = FindPopup(surface);

            // Windows' setting "Animation effects" is off
            if (popup == null || !SystemParameters.ClientAreaAnimation)
                return;

            if (surface.GetValue(StateProperty) is not MotionState state)
            {
                state = new MotionState();
                surface.SetValue(StateProperty, state);
            }

            state.Source = source;
            state.IsGlass = glass;
            state.Reveal = 0;

            if (!ReferenceEquals(state.Popup, popup))
            {
                state.Popup = popup;
                BindingOperations.SetBinding(surface, PopupIsOpenProperty, new Binding(nameof(Popup.IsOpen)) { Source = popup });
            }

            // Context menus and tooltips otherwise follow a system setting; without the fade the window is destroyed
            // immediately, and the closing movement wouldn't be seen
            popup.PopupAnimation = PopupAnimation.Fade;

            BeginOpen(surface, state);
        }

        private static void OnWindowDestroyed(Border surface)
        {
            if (surface.GetValue(StateProperty) is not MotionState state)
                return;

            state.Run++;
            StopReveal(state);
            state.Source = null;
            state.Scale = null;
            state.IsClosing = false;
            state.IsGlass = false;

            surface.ClearValue(UIElement.RenderTransformProperty);
            surface.ClearValue(UIElement.RenderTransformOriginProperty);
        }

        private static void OnPopupIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Border surface || surface.GetValue(StateProperty) is not MotionState { Source: not null } state)
                return;

            if (!(bool)e.NewValue)
                BeginClose(surface, state);
            else if (state.IsClosing)
                BeginOpen(surface, state); // Opened again while closing: WPF keeps the window, so there is no new source
        }

        private static void BeginOpen(Border surface, MotionState state)
        {
            int run = ++state.Run;
            state.IsClosing = false;
            StopReveal(state);

            // Set at once, so the first frame is already right; the movement starts after the layout (a long list
            // can take a while), otherwise its first part would be skipped
            ScaleTransform? scale = null;

            if (state.IsGlass)
            {
                if (state.Reveal <= 0)
                    HideWindow(state);
            }
            else
            {
                scale = new ScaleTransform(ClosedScale, ClosedScale);
                state.Scale = scale;
                surface.RenderTransform = scale;
            }

            surface.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (run != state.Run)
                    return;

                ApplyOrigin(surface, state);

                if (scale == null)
                {
                    StartReveal(surface, state, 1, RevealDuration, EasingMode.EaseOut, () => ShowWholeWindow(state));
                    return;
                }

                DoubleAnimation scaleX = CreateScaleAnimation(1, OpenDuration, EasingMode.EaseOut);
                DoubleAnimation scaleY = scaleX.Clone();
                scaleX.Completed += (_, _) =>
                {
                    if (run != state.Run)
                        return;

                    state.Scale = null;
                    surface.ClearValue(UIElement.RenderTransformProperty);
                };

                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            }));
        }

        private static void BeginClose(Border surface, MotionState state)
        {
            state.Run++;
            state.IsClosing = true;
            StopReveal(state);

            ApplyOrigin(surface, state);

            if (state.IsGlass)
            {
                // Ends hidden: the blur doesn't fade with the content, so nothing of it may stay until the window is gone
                StartReveal(surface, state, 0, CloseDuration.TimeSpan, EasingMode.EaseIn, () => HideWindow(state));
                return;
            }

            // Interrupted while opening: continues from where it is
            double from = state.Scale?.ScaleX ?? 1;

            ScaleTransform scale = new ScaleTransform(from, from);
            state.Scale = scale;
            surface.RenderTransform = scale;

            DoubleAnimation animation = CreateScaleAnimation(ClosedScale, CloseDuration, EasingMode.EaseIn);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private static DoubleAnimation CreateScaleAnimation(double to, Duration duration, EasingMode mode)
        {
            return new DoubleAnimation
            {
                To = to,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = mode }
            };
        }

        /// <summary>Moves the window region once per frame: it isn't a WPF property, so no animation can do it.</summary>
        private static void StartReveal(Border surface, MotionState state, double to, TimeSpan duration, EasingMode mode, Action completed)
        {
            StopReveal(state);

            double from = state.Reveal;
            Stopwatch watch = Stopwatch.StartNew();

            state.RevealStep = (_, _) =>
            {
                double time = Math.Min(1, watch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
                double eased = mode == EasingMode.EaseOut ? 1 - Math.Pow(1 - time, 3) : Math.Pow(time, 3);

                state.Reveal = from + (to - from) * eased;

                if (time < 1)
                {
                    ApplyRevealRegion(surface, state);
                    return;
                }

                StopReveal(state);
                completed();
            };

            CompositionTarget.Rendering += state.RevealStep;
        }

        private static void StopReveal(MotionState state)
        {
            if (state.RevealStep == null)
                return;

            CompositionTarget.Rendering -= state.RevealStep;
            state.RevealStep = null;
        }

        private static void ApplyRevealRegion(Border surface, MotionState state)
        {
            if (state.Source == null || !GetWindowRect(state.Source.Handle, out RECT window))
                return;

            int windowWidth = window.Right - window.Left;
            int windowHeight = window.Bottom - window.Top;

            double width = windowWidth * (RevealStartWidth + (1 - RevealStartWidth) * state.Reveal);
            double height = windowHeight * (RevealStartHeight + (1 - RevealStartHeight) * state.Reveal);

            int left = (int)Math.Round((windowWidth - width) * state.Origin.X);
            int top = (int)Math.Round((windowHeight - height) * state.Origin.Y);
            int diameter = (int)Math.Round(RevealCornerRadius * 2 * VisualTreeHelper.GetDpi(surface).DpiScaleX);

            // The right and bottom coordinates of a region are exclusive
            SetRegion(state, CreateRoundRectRgn(left, top, left + (int)Math.Round(width) + 1, top + (int)Math.Round(height) + 1, diameter, diameter));
        }

        private static void HideWindow(MotionState state) => SetRegion(state, CreateRectRgn(0, 0, 0, 0));

        /// <summary>Without a region the window is rounded by DWM again, with smooth corners.</summary>
        private static void ShowWholeWindow(MotionState state) => SetRegion(state, IntPtr.Zero);

        private static void SetRegion(MotionState state, IntPtr region)
        {
            // After a successful call the region belongs to the system
            if ((state.Source == null || SetWindowRgn(state.Source.Handle, region, true) == 0) && region != IntPtr.Zero)
                DeleteObject(region);
        }

        /// <summary>The mouse or the target of the popup, as far as that point lies within the surface.</summary>
        private static void ApplyOrigin(Border surface, MotionState state)
        {
            if (state.Popup is not Popup popup || surface.ActualWidth <= 0 || surface.ActualHeight <= 0)
                return;

            Point anchor;

            if (popup.Placement is PlacementMode.Mouse or PlacementMode.MousePoint)
            {
                anchor = Mouse.GetPosition(surface);
            }
            else
            {
                UIElement? target = popup.PlacementTarget ?? VisualTreeHelper.GetParent(popup) as UIElement;

                if (target == null || PresentationSource.FromVisual(target) == null || PresentationSource.FromVisual(surface) == null)
                    return;

                Point center = new Point(target.RenderSize.Width / 2, target.RenderSize.Height / 2);
                anchor = surface.PointFromScreen(target.PointToScreen(center));
            }

            state.Origin = new Point(
                Math.Clamp(anchor.X / surface.ActualWidth, 0, 1),
                Math.Clamp(anchor.Y / surface.ActualHeight, 0, 1));

            surface.RenderTransformOrigin = state.Origin;
        }

        private static Popup? FindPopup(DependencyObject element)
        {
            DependencyObject? current = element;

            while (current != null)
            {
                if (current is Popup popup)
                    return popup;

                current = LogicalTreeHelper.GetParent(current)
                    ?? (current is Visual ? VisualTreeHelper.GetParent(current) : null);
            }

            return null;
        }

        // --- Native ---

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr handle);
    }
}
