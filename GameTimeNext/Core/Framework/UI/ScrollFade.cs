using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Fades out the scrolled content at the top and bottom edge of a scrollable control (ListBox, ScrollViewer, ...).
    ///
    /// Usage: ScrollFade.Length="80" (fade height in pixels). An edge only fades while there is more content beyond it,
    /// so nothing fades at the very top or bottom. The mask is applied to the content presenter, the scrollbar stays opaque.
    /// </summary>
    public static class ScrollFade
    {
        public static readonly DependencyProperty LengthProperty =
            DependencyProperty.RegisterAttached(
                "Length",
                typeof(double),
                typeof(ScrollFade),
                new PropertyMetadata(0d, OnLengthChanged));

        public static double GetLength(DependencyObject obj)
            => (double)obj.GetValue(LengthProperty);

        public static void SetLength(DependencyObject obj, double value)
            => obj.SetValue(LengthProperty, value);

        /// <summary>ScrollViewer the fade is attached to; prevents duplicate handlers when Loaded fires again (e.g. tab switch).</summary>
        private static readonly DependencyProperty AttachedScrollViewerProperty =
            DependencyProperty.RegisterAttached("AttachedScrollViewer", typeof(ScrollViewer), typeof(ScrollFade), new PropertyMetadata(null));

        private static void OnLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            element.Loaded -= OnLoaded;
            element.Loaded += OnLoaded;

            if (element.IsLoaded)
                Attach(element);
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
                Attach(element);
        }

        private static void Attach(FrameworkElement element)
        {
            ScrollViewer? scrollViewer = element as ScrollViewer ?? FindDescendant<ScrollViewer>(element);
            if (scrollViewer == null)
                return;

            if (element.GetValue(AttachedScrollViewerProperty) is ScrollViewer attached && attached == scrollViewer)
            {
                UpdateMask(element, scrollViewer);
                return;
            }

            element.SetValue(AttachedScrollViewerProperty, scrollViewer);

            // Also raised for viewport and extent changes (resize, items added or filtered)
            scrollViewer.ScrollChanged += (_, _) => UpdateMask(element, scrollViewer);

            UpdateMask(element, scrollViewer);
        }

        private static void UpdateMask(FrameworkElement element, ScrollViewer scrollViewer)
        {
            FrameworkElement? presenter = scrollViewer.Template?.FindName("PART_ScrollContentPresenter", scrollViewer) as FrameworkElement
                ?? FindDescendant<ScrollContentPresenter>(scrollViewer);

            if (presenter == null)
                return;

            double length = GetLength(element);
            double height = presenter.ActualHeight;

            bool fadeTop = scrollViewer.VerticalOffset > 0.5;
            bool fadeBottom = scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight - 0.5;

            if (length <= 0 || height <= 0 || (!fadeTop && !fadeBottom))
            {
                presenter.OpacityMask = null;
                return;
            }

            // Relative to the presenter height, capped so the two fades never overlap
            double share = Math.Min(length / height, 0.5);

            GradientStopCollection stops = new GradientStopCollection
            {
                new GradientStop(fadeTop ? Colors.Transparent : Colors.Black, 0),
                new GradientStop(Colors.Black, fadeTop ? share : 0),
                new GradientStop(Colors.Black, fadeBottom ? 1 - share : 1),
                new GradientStop(fadeBottom ? Colors.Transparent : Colors.Black, 1)
            };

            // Absolute: relative mapping would use the bounding box of the whole (scrolled) content, not the visible area
            presenter.OpacityMask = new LinearGradientBrush(stops, new Point(0, 0), new Point(0, height))
            {
                MappingMode = BrushMappingMode.Absolute
            };
        }

        private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T match)
                    return match;

                if (FindDescendant<T>(child) is T nested)
                    return nested;
            }

            return null;
        }
    }
}
