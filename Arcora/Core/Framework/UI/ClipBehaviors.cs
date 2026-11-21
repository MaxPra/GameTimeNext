using System.Windows;
using System.Windows.Media;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Clips an element to a rounded rectangle that follows its size (ClipToBounds only clips square corners).
    /// </summary>
    public static class ClipBehaviors
    {
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached(
                "CornerRadius",
                typeof(double),
                typeof(ClipBehaviors),
                new PropertyMetadata(0d, OnCornerRadiusChanged));

        public static double GetCornerRadius(DependencyObject obj)
            => (double)obj.GetValue(CornerRadiusProperty);

        public static void SetCornerRadius(DependencyObject obj, double value)
            => obj.SetValue(CornerRadiusProperty, value);

        private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            element.SizeChanged -= OnSizeChanged;

            if ((double)e.NewValue > 0)
            {
                element.SizeChanged += OnSizeChanged;
                UpdateClip(element);
            }
            else
                element.Clip = null;
        }

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is FrameworkElement element)
                UpdateClip(element);
        }

        private static void UpdateClip(FrameworkElement element)
        {
            double radius = GetCornerRadius(element);
            element.Clip = new RectangleGeometry(new Rect(0, 0, element.ActualWidth, element.ActualHeight), radius, radius);
        }
    }
}
