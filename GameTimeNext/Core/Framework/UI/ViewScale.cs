using System.Windows;
using System.Windows.Media;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Scales an element with the height of its container: the element is laid out for DesignHeight and scaled to the
    /// actual height (LayoutTransform), its width follows the container. Meant for views that are read from a distance
    /// (TV) and have to look the same at every resolution and DPI setting.
    /// </summary>
    public static class ViewScale
    {
        // Below this the content would be unreadable anyway; a very small window gets scroll-free clipping instead
        private const double MinScale = 0.5;

        public static readonly DependencyProperty DesignHeightProperty =
            DependencyProperty.RegisterAttached(
                "DesignHeight",
                typeof(double),
                typeof(ViewScale),
                new PropertyMetadata(0d, OnDesignHeightChanged));

        public static double GetDesignHeight(DependencyObject obj)
            => (double)obj.GetValue(DesignHeightProperty);

        public static void SetDesignHeight(DependencyObject obj, double value)
            => obj.SetValue(DesignHeightProperty, value);

        // Container whose size the element follows
        private static readonly DependencyProperty ContainerProperty =
            DependencyProperty.RegisterAttached(
                "Container",
                typeof(FrameworkElement),
                typeof(ViewScale),
                new PropertyMetadata(null));

        private static void OnDesignHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            element.Loaded -= OnLoaded;
            element.Unloaded -= OnUnloaded;
            Detach(element);

            if ((double)e.NewValue <= 0)
            {
                element.LayoutTransform = Transform.Identity;
                return;
            }

            element.Loaded += OnLoaded;
            element.Unloaded += OnUnloaded;

            if (element.IsLoaded)
                Attach(element);
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
                Attach(element);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
                Detach(element);
        }

        private static void Attach(FrameworkElement element)
        {
            Detach(element);

            if (VisualTreeHelper.GetParent(element) is not FrameworkElement container)
                return;

            element.SetValue(ContainerProperty, container);

            SizeChangedEventHandler handler = (_, _) => UpdateScale(element, container);
            container.SizeChanged += handler;
            element.SetValue(HandlerProperty, handler);

            UpdateScale(element, container);
        }

        private static void Detach(FrameworkElement element)
        {
            if (element.GetValue(ContainerProperty) is FrameworkElement container
                && element.GetValue(HandlerProperty) is SizeChangedEventHandler handler)
            {
                container.SizeChanged -= handler;
            }

            element.ClearValue(ContainerProperty);
            element.ClearValue(HandlerProperty);
        }

        private static readonly DependencyProperty HandlerProperty =
            DependencyProperty.RegisterAttached(
                "Handler",
                typeof(SizeChangedEventHandler),
                typeof(ViewScale),
                new PropertyMetadata(null));

        private static void UpdateScale(FrameworkElement element, FrameworkElement container)
        {
            double designHeight = GetDesignHeight(element);

            if (designHeight <= 0 || container.ActualHeight <= 0)
                return;

            double scale = Math.Max(MinScale, container.ActualHeight / designHeight);

            if (element.LayoutTransform is ScaleTransform current && current.ScaleX == scale)
                return;

            element.LayoutTransform = new ScaleTransform(scale, scale);
        }
    }
}
