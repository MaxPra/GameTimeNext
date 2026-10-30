using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Scrolls an element into view of its ScrollViewer(s) as soon as When turns true (e.g. bound to the selection of an
    /// item that is moved by a controller instead of the keyboard focus).
    /// Padding keeps that much space around the element visible, so the neighbours stay in sight while moving along a row.
    /// With Smooth the nearest ScrollViewer glides there instead of jumping.
    /// </summary>
    public static class BringIntoViewBehaviors
    {
        private static readonly Duration SmoothDuration = TimeSpan.FromMilliseconds(220);

        public static readonly DependencyProperty WhenProperty =
            DependencyProperty.RegisterAttached(
                "When",
                typeof(bool),
                typeof(BringIntoViewBehaviors),
                new PropertyMetadata(false, OnWhenChanged));

        public static bool GetWhen(DependencyObject obj)
            => (bool)obj.GetValue(WhenProperty);

        public static void SetWhen(DependencyObject obj, bool value)
            => obj.SetValue(WhenProperty, value);

        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.RegisterAttached(
                "Padding",
                typeof(Thickness),
                typeof(BringIntoViewBehaviors),
                new PropertyMetadata(new Thickness(0)));

        public static Thickness GetPadding(DependencyObject obj)
            => (Thickness)obj.GetValue(PaddingProperty);

        public static void SetPadding(DependencyObject obj, Thickness value)
            => obj.SetValue(PaddingProperty, value);

        public static readonly DependencyProperty SmoothProperty =
            DependencyProperty.RegisterAttached(
                "Smooth",
                typeof(bool),
                typeof(BringIntoViewBehaviors),
                new PropertyMetadata(false));

        public static bool GetSmooth(DependencyObject obj)
            => (bool)obj.GetValue(SmoothProperty);

        public static void SetSmooth(DependencyObject obj, bool value)
            => obj.SetValue(SmoothProperty, value);

        // Animated offsets of a ScrollViewer (ScrollViewer.HorizontalOffset / VerticalOffset can't be animated)
        private static readonly DependencyProperty AnimatedHorizontalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "AnimatedHorizontalOffset",
                typeof(double),
                typeof(BringIntoViewBehaviors),
                new PropertyMetadata(0d, (d, e) => (d as ScrollViewer)?.ScrollToHorizontalOffset((double)e.NewValue)));

        private static readonly DependencyProperty AnimatedVerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "AnimatedVerticalOffset",
                typeof(double),
                typeof(BringIntoViewBehaviors),
                new PropertyMetadata(0d, (d, e) => (d as ScrollViewer)?.ScrollToVerticalOffset((double)e.NewValue)));

        private static void OnWhenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            element.Loaded -= OnLoaded;

            if (!(bool)e.NewValue)
                return;

            // Not laid out yet (e.g. selected while the list is being built): as soon as it is
            if (!element.IsLoaded)
            {
                element.Loaded += OnLoaded;
                return;
            }

            BringIntoView(element);
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;

            element.Loaded -= OnLoaded;

            if (GetWhen(element))
                BringIntoView(element);
        }

        private static void BringIntoView(FrameworkElement element)
        {
            Thickness padding = GetPadding(element);

            if (GetSmooth(element) && FindScrollViewer(element) is ScrollViewer scrollViewer)
            {
                ScrollSmoothly(element, scrollViewer, padding);
                return;
            }

            element.BringIntoView(new Rect(
                -padding.Left,
                -padding.Top,
                element.ActualWidth + padding.Left + padding.Right,
                element.ActualHeight + padding.Top + padding.Bottom));
        }

        /// <summary>
        /// Glides the scroll viewer just far enough that the element and its padding are visible.
        /// </summary>
        private static void ScrollSmoothly(FrameworkElement element, ScrollViewer scrollViewer, Thickness padding)
        {
            // Position in the viewport; layout position only (a growing selected element must not scroll further)
            Point topLeft = element.TranslatePoint(new Point(0, 0), scrollViewer);
            Rect bounds = new Rect(topLeft, new Size(element.ActualWidth, element.ActualHeight));

            double horizontal = GetTargetOffset(scrollViewer.HorizontalOffset, scrollViewer.ViewportWidth, scrollViewer.ScrollableWidth,
                bounds.Left - padding.Left, bounds.Right + padding.Right);

            double vertical = GetTargetOffset(scrollViewer.VerticalOffset, scrollViewer.ViewportHeight, scrollViewer.ScrollableHeight,
                bounds.Top - padding.Top, bounds.Bottom + padding.Bottom);

            Animate(scrollViewer, AnimatedHorizontalOffsetProperty, scrollViewer.HorizontalOffset, horizontal);
            Animate(scrollViewer, AnimatedVerticalOffsetProperty, scrollViewer.VerticalOffset, vertical);
        }

        /// <summary>
        /// Offset that shows the range [start, end] of the viewport (current offset if it is visible already).
        /// </summary>
        private static double GetTargetOffset(double offset, double viewport, double scrollable, double start, double end)
        {
            double target = offset;

            if (start < 0)
                target += start;
            else if (end > viewport)
                target += Math.Min(end - viewport, start);

            return Math.Clamp(target, 0, Math.Max(0, scrollable));
        }

        private static void Animate(ScrollViewer scrollViewer, DependencyProperty property, double from, double to)
        {
            if (Math.Abs(to - from) < 0.5)
                return;

            scrollViewer.BeginAnimation(property, new DoubleAnimation(from, to, SmoothDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject element)
        {
            DependencyObject? current = VisualTreeHelper.GetParent(element);

            while (current != null && current is not ScrollViewer)
                current = VisualTreeHelper.GetParent(current);

            return current as ScrollViewer;
        }
    }
}
