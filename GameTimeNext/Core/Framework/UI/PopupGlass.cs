using System.Windows;
using System.Windows.Interop;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Glass for popups (context menus, filter popup, ...): the popup window gets an acrylic blur behind it
    /// (see AcrylicBlur), the popup surface draws the translucent tint on top.
    ///
    /// Usage: PopupGlass.IsEnabled="True" on the surface element inside a popup with AllowsTransparency
    /// (context menus always have it). Styles switch to the translucent look via the read only PopupGlass.IsActive,
    /// which is only true where the blur is supported (Windows 11), otherwise the opaque look stays.
    /// </summary>
    public static class PopupGlass
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(PopupGlass),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static readonly DependencyPropertyKey IsActivePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsActive",
                typeof(bool),
                typeof(PopupGlass),
                new PropertyMetadata(false));

        /// <summary>
        /// True when the popup is drawn as glass. The surface then has to be translucent and must not have
        /// an outer margin or shadow effect (the blur covers the whole popup window).
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty = IsActivePropertyKey.DependencyProperty;

        public static bool GetIsActive(DependencyObject obj) => (bool)obj.GetValue(IsActiveProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
                return;

            PresentationSource.RemoveSourceChangedHandler(element, OnSourceChanged);

            bool active = (bool)e.NewValue && AcrylicBlur.IsSupported;
            d.SetValue(IsActivePropertyKey, active);

            if (!active)
                return;

            // Each opening creates a new popup window
            PresentationSource.AddSourceChangedHandler(element, OnSourceChanged);

            if (PresentationSource.FromVisual(element) is HwndSource current)
                ApplyBlur(current);
        }

        private static void OnSourceChanged(object sender, SourceChangedEventArgs e)
        {
            if (e.NewSource is HwndSource source)
                ApplyBlur(source);
        }

        private static void ApplyBlur(HwndSource source)
        {
            // Only popup windows; a surface that ends up directly in a window must not blur the whole window
            if (source.RootVisual is Window)
                return;

            AcrylicBlur.TryEnable(source.Handle);
        }
    }
}
