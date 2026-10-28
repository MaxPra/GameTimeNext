using System.Windows;
using System.Windows.Threading;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// State of a game case (style GameCase in TileViewStyle.xaml: ContentControl with the cover image as content).
    ///
    /// The case opens on its own while hovered; IsOpen opens it from outside (e.g. while the profile tile is hovered
    /// or selected). IsSelected opens it a little further and slides the disc out.
    /// IsSealed shows the case shrink-wrapped; turning it off plays the unwrap animation. IsWrapped follows IsSealed but
    /// stays true until that animation is done, so the case only opens once the film is off.
    /// </summary>
    public static class GameCase
    {
        // Until the film is off in SbUnwrap (TileViewStyle.xaml); keep both in sync
        private static readonly TimeSpan UnwrapDuration = TimeSpan.FromSeconds(1.0);

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

        public static readonly DependencyProperty IsSealedProperty =
            DependencyProperty.RegisterAttached(
                "IsSealed",
                typeof(bool),
                typeof(GameCase),
                new PropertyMetadata(false, OnIsSealedChanged));

        public static bool GetIsSealed(DependencyObject obj)
            => (bool)obj.GetValue(IsSealedProperty);

        public static void SetIsSealed(DependencyObject obj, bool value)
            => obj.SetValue(IsSealedProperty, value);

        public static readonly DependencyProperty IsWrappedProperty =
            DependencyProperty.RegisterAttached(
                "IsWrapped",
                typeof(bool),
                typeof(GameCase),
                new PropertyMetadata(false));

        public static bool GetIsWrapped(DependencyObject obj)
            => (bool)obj.GetValue(IsWrappedProperty);

        public static void SetIsWrapped(DependencyObject obj, bool value)
            => obj.SetValue(IsWrappedProperty, value);

        // Timer of a running unwrap animation, ends IsWrapped
        private static readonly DependencyProperty UnwrapTimerProperty =
            DependencyProperty.RegisterAttached(
                "UnwrapTimer",
                typeof(DispatcherTimer),
                typeof(GameCase),
                new PropertyMetadata(null));

        private static void OnIsSealedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d.GetValue(UnwrapTimerProperty) as DispatcherTimer)?.Stop();
            d.ClearValue(UnwrapTimerProperty);

            if ((bool)e.NewValue)
            {
                SetIsWrapped(d, true);
                return;
            }

            if (!GetIsWrapped(d))
                return;

            DispatcherTimer timer = new DispatcherTimer { Interval = UnwrapDuration };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                d.ClearValue(UnwrapTimerProperty);
                SetIsWrapped(d, false);
            };

            d.SetValue(UnwrapTimerProperty, timer);
            timer.Start();
        }
    }
}
