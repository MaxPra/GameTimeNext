using System.Windows;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Glass look for popups (context menus, filter popup, ...): the popup window gets an acrylic blur behind it
    /// (see AcrylicBlur), the popup surface draws the translucent tint on top. PopupMotion switches the blur on,
    /// because it decides how the window opens and closes with it.
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
            d.SetValue(IsActivePropertyKey, (bool)e.NewValue && AcrylicBlur.IsSupported);
        }
    }
}
