using GameTimeNext.Core.Framework.UI;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameTimeNext.Core.Application.BigPicture
{
    public static class CFBigPictureApp
    {
        // The backdrop is blurred once at this small width and stretched over the screen; strong enough that the low
        // resolution of the covers doesn't show when they are stretched
        private const int BackdropWidth = 120;
        private const double BackdropBlurRadius = 12;

        // The key art (cover large on the right) is stretched several times over its decoded size (300 px): blurred just
        // enough that its pixels don't show, its shapes stay recognizable
        private const int KeyArtWidth = 300;
        private const double KeyArtBlurRadius = 4;

        /// <summary>
        /// True while the Big Picture mode is open (set by BigPictureViewController). Other modules leave out their
        /// own notifications then, e.g. toasts that would pop up in front of a game started from the mode.
        /// </summary>
        public static bool IsOpen { get; internal set; }

        /// <summary>
        /// Label of a button on the input device: letter, PlayStation symbol or key.
        /// </summary>
        public static string GetButtonLabel(BigPictureInputDevice device, BigPictureButton button)
        {
            return device switch
            {
                BigPictureInputDevice.PlayStation => button switch
                {
                    BigPictureButton.A => "✕",
                    BigPictureButton.B => "○",
                    BigPictureButton.X => "□",
                    BigPictureButton.Y => "△",
                    BigPictureButton.View => "CREATE",
                    BigPictureButton.LeftBumper => "L1",
                    _ => "R1"
                },
                BigPictureInputDevice.Keyboard => button switch
                {
                    BigPictureButton.A => "Enter",
                    BigPictureButton.B => "Esc",
                    BigPictureButton.X => "Tab",
                    BigPictureButton.Y => "F3",
                    BigPictureButton.View => "F5",
                    BigPictureButton.LeftBumper => "PgUp",
                    _ => "PgDn"
                },
                _ => button switch
                {
                    BigPictureButton.A => "A",
                    BigPictureButton.B => "B",
                    BigPictureButton.X => "X",
                    BigPictureButton.Y => "Y",
                    BigPictureButton.View => "VIEW",
                    BigPictureButton.LeftBumper => "LB",
                    _ => "RB"
                }
            };
        }

        /// <summary>
        /// Resource key of the color of a round face button; null for buttons shown as a flat key (keys of the
        /// keyboard, View, bumpers).
        /// </summary>
        public static string? GetButtonBrushKey(BigPictureInputDevice device, BigPictureButton button)
        {
            if (device == BigPictureInputDevice.Keyboard)
                return null;

            bool isPlayStation = device == BigPictureInputDevice.PlayStation;

            return button switch
            {
                BigPictureButton.A => isPlayStation ? "Brush.BigPicture.ButtonCross" : "Brush.BigPicture.ButtonA",
                BigPictureButton.B => isPlayStation ? "Brush.BigPicture.ButtonCircle" : "Brush.BigPicture.ButtonB",
                BigPictureButton.X => isPlayStation ? "Brush.BigPicture.ButtonSquare" : "Brush.BigPicture.ButtonX",
                BigPictureButton.Y => isPlayStation ? "Brush.BigPicture.ButtonTriangle" : "Brush.BigPicture.ButtonY",
                _ => null
            };
        }

        /// <summary>
        /// Blurred backdrop of a cover for the background of the mode. A live BlurEffect over the whole screen is
        /// rendered again for every frame in which anything in front of it moves (covers, scrolling), which made the
        /// mode slow; the small pre-blurred bitmap only has to be stretched. Frozen.
        /// </summary>
        public static BitmapSource CreateBackdrop(BitmapSource cover)
        {
            return Blur(cover, BackdropWidth, BackdropBlurRadius);
        }

        /// <summary>
        /// Softly blurred key art of a cover (see KeyArtWidth). Frozen.
        /// </summary>
        public static BitmapSource CreateKeyArt(BitmapSource cover)
        {
            return Blur(cover, KeyArtWidth, KeyArtBlurRadius);
        }

        private static BitmapSource Blur(BitmapSource cover, int width, double radius)
        {
            double scale = (double)width / Math.Max(1, cover.PixelWidth);

            TransformedBitmap scaled = new TransformedBitmap(cover, new ScaleTransform(scale, scale));
            scaled.Freeze();

            return DesktopWallpaper.Blur(scaled, radius);
        }

        /// <summary>
        /// Formats when a game was played last, readable from the couch: "Today", "Yesterday", "3 days ago", otherwise the date.
        /// Empty if it was never played.
        /// </summary>
        public static string FormatLastPlayed(DateTime lastPlayed)
        {
            if (lastPlayed == DateTime.MinValue)
                return string.Empty;

            int days = (DateTime.Today - lastPlayed.Date).Days;

            return days switch
            {
                <= 0 => "Played today",
                1 => "Played yesterday",
                < 7 => string.Format(CultureInfo.CurrentCulture, "Played {0} days ago", days),
                _ => "Last played " + lastPlayed.ToString("d", CultureInfo.CurrentCulture)
            };
        }
    }
}
