using System.Windows;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// State of a game case (style GameCase in TileViewStyle.xaml: ContentControl with the cover image as content).
    ///
    /// The case opens on its own while hovered; IsOpen opens it from outside (e.g. while the profile tile is hovered
    /// or selected). IsSelected opens it a little further and slides the disc out.
    /// </summary>
    public static class GameCase
    {
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.RegisterAttached(
                "IsOpen",
                typeof(bool),
                typeof(GameCase),
                new PropertyMetadata(false));

        public static bool GetIsOpen(DependencyObject obj)
            => (bool)obj.GetValue(IsOpenProperty);

        public static void SetIsOpen(DependencyObject obj, bool value)
            => obj.SetValue(IsOpenProperty, value);

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.RegisterAttached(
                "IsSelected",
                typeof(bool),
                typeof(GameCase),
                new PropertyMetadata(false));

        public static bool GetIsSelected(DependencyObject obj)
            => (bool)obj.GetValue(IsSelectedProperty);

        public static void SetIsSelected(DependencyObject obj, bool value)
            => obj.SetValue(IsSelectedProperty, value);
    }
}
