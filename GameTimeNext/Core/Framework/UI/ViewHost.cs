using System.Windows;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Tells a view where it is hosted. The main TabControl sets IsTabContent (inherited), so views can drop their
    /// own outer margin inside a tab (the tab content area provides it) and keep it when opened in their own window.
    /// </summary>
    public static class ViewHost
    {
        public static readonly DependencyProperty IsTabContentProperty =
            DependencyProperty.RegisterAttached(
                "IsTabContent",
                typeof(bool),
                typeof(ViewHost),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetIsTabContent(DependencyObject obj)
            => (bool)obj.GetValue(IsTabContentProperty);

        public static void SetIsTabContent(DependencyObject obj, bool value)
            => obj.SetValue(IsTabContentProperty, value);
    }
}
