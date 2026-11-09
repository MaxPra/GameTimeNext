using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
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
        private static readonly TimeSpan UnwrapDuration = TimeSpan.FromSeconds(1.1);

        // Case in base units with the room around it for the blur and the offset of the shadow (keep the transform in TileViewStyle.xaml in sync)
        private const int ShadowCaseWidth = 246;
        private const int ShadowCaseHeight = 366;
        private const int ShadowMargin = 30;

        private static BitmapSource? _shadowImage;

        /// <summary>
        /// The shadow of a case, rendered once and shared by all cases of a library: an effect per case has to be rendered
        /// again whenever the tile size changes, for all cases at once.
        /// </summary>
        public static BitmapSource ShadowImage => _shadowImage ??= RenderShadowImage();

        private static BitmapSource RenderShadowImage()
        {
            // The fill is hidden by the case, the effect needs it to cast a shadow
            var shape = new Border
            {
                Margin = new Thickness(ShadowMargin),
                Background = new SolidColorBrush(Colors.Black),
                CornerRadius = new CornerRadius(6),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 18,
                    Direction = 270,
                    Opacity = 1,
                    RenderingBias = RenderingBias.Quality,
                    ShadowDepth = 6,
                    Color = Colors.Black
                }
            };

            var size = new Size(ShadowCaseWidth + 2 * ShadowMargin, ShadowCaseHeight + 2 * ShadowMargin);
            shape.Measure(size);
            shape.Arrange(new Rect(size));

            var image = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
            image.Render(shape);
            image.Freeze();

            return image;
        }

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

        /// <summary>Set by the triggers of the template on its root: the case is open (hovered or IsOpen, not wrapped). Starts the movement.</summary>
        public static readonly DependencyProperty MotionOpenProperty =
            DependencyProperty.RegisterAttached(
                "MotionOpen",
                typeof(bool),
                typeof(GameCase),
                new PropertyMetadata(false, OnMotionChanged));

        public static bool GetMotionOpen(DependencyObject obj)
            => (bool)obj.GetValue(MotionOpenProperty);

        public static void SetMotionOpen(DependencyObject obj, bool value)
            => obj.SetValue(MotionOpenProperty, value);

        /// <summary>Set by the triggers of the template on its root: the case is selected and not wrapped. Starts the movement.</summary>
        public static readonly DependencyProperty MotionSelectedProperty =
            DependencyProperty.RegisterAttached(
                "MotionSelected",
                typeof(bool),
                typeof(GameCase),
                new PropertyMetadata(false, OnMotionChanged));

        public static bool GetMotionSelected(DependencyObject obj)
            => (bool)obj.GetValue(MotionSelectedProperty);

        public static void SetMotionSelected(DependencyObject obj, bool value)
            => obj.SetValue(MotionSelectedProperty, value);

        private static readonly DependencyProperty MotionProperty =
            DependencyProperty.RegisterAttached(
                "Motion",
                typeof(GameCaseMotion),
                typeof(GameCase),
                new PropertyMetadata(null));

        private static void OnMotionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement host)
                return;

            if (host.GetValue(MotionProperty) is not GameCaseMotion motion)
            {
                motion = new GameCaseMotion(host);
                host.SetValue(MotionProperty, motion);
            }

            motion.Update();
        }

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
